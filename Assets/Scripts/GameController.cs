using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {
    enum states {starting, playing, moving, ending}
    int tileSize = 16;
    int player = 1;
    public int playerLimit = 4;
    states state = states.playing;
    public Transform[] backgroundTiles = new Transform[2];
    public Transform debugTile;
    public Transform highlightTile;
    public Transform selector;
    public Sprite[] selectorSprites = new Sprite[5];
    private Vector2 selectorPos;
    private List<tile> tileList = new List<tile>();
    private List<tile> debugTileList = new List<tile>();
    private Transform selectedTileTransform;
    private tile selectedTile;

    public Transform[] discs = new Transform[4];
    Vector2[] directions = new Vector2[8];
    
    
    // Use this for initialization

    void Start () {


        directions[0] = new Vector2(0, 1);
        directions[1] = new Vector2(1, 1);
        directions[2] = new Vector2(1, 0);
        directions[3] = new Vector2(1, -1);
        directions[4] = new Vector2(0, -1);
        directions[5] = new Vector2(-1, -1);
        directions[6] = new Vector2(-1, -0);
        directions[7] = new Vector2(-1, 1);

        selectorPos = new Vector2(0, 0);

        GameObject tileHolder = GameObject.Find("TileHolder");
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tile newTile = new tile();
                tile newDebugTile = new tile();

                // AI - set initial move weights to -1 for all tiles
                for (int p = 0; p < 4; p++)
                {
                    newTile.moveWeights[p] = -1;
                }

                Transform t, dt;
                if (i % 2 == 0 && j % 2 == 1 || i % 2 == 1 && j % 2 == 0)
                {
                    t = (Transform)Instantiate(backgroundTiles[0], new Vector2(i * tileSize, j * tileSize), Quaternion.identity);
                }
                else
                {
                    t = (Transform)Instantiate(backgroundTiles[1], new Vector2(i * tileSize, j * tileSize), Quaternion.identity);
                }
                dt = (Transform)Instantiate(debugTile, new Vector2(i * tileSize, j * tileSize), Quaternion.identity);
                newTile.tileObject = t;
                newTile.tilePos = new Vector2(i, j);
                newDebugTile.tileObject = dt;
                newDebugTile.tilePos = new Vector2(i, j);
                tileList.Add(newTile);
                debugTileList.Add(newDebugTile);

                t.SetParent(tileHolder.transform);
                dt.SetParent(tileHolder.transform);
            }
        }

        // update selector sprite
        selector.FindChild("Selector1").GetComponent<SpriteRenderer>().sprite = selectorSprites[player];

        // AI heatmap
        InitialBoardValues();
        ToggleHeatmap(false);

    }

    class tile
    {
        public Vector2 tilePos;
        public int stack;
        public Transform tileObject;
        public List<token> tokens = new List<token>();
        // value for each player
        public int[] moveWeights = new int[4];
    }

    class token
    {
        public Transform tokenObject;
        public int ownerPlayer;

    }

    void MakeMove(Vector2 dir)
    {
        // move on x-axis
        if (dir.x != 0)
        {
            if (selectorPos.x < 4 && selectorPos.x >= 0)
            {
                selectorPos.x += dir.x;
                selectorPos.x = Mathf.Clamp(selectorPos.x, 0, 3);
            }
        }
        if (dir.y != 0)
        {
            if (selectorPos.y < 4 && selectorPos.y >= 0)
            {
                selectorPos.y += dir.y;
                selectorPos.y = Mathf.Clamp(selectorPos.y, 0, 3);
            }
        }
      //  Debug.Log(selectorPos);
        selector.transform.position = new Vector3(selectorPos.x * tileSize, selectorPos.y * tileSize, 0);
    }
	
    bool PlaceToken(Vector2 pos)
    {
        // find right tile
        tile targetTile = null;
        foreach(tile t in tileList)
        {
            if(t.tilePos == pos)
            {
                targetTile = t;
                break;
            }
        }
        
        // check if legal to place
        if (targetTile.stack < 4)
        {
            int yOffset = -3 + targetTile.stack * 2;
            Transform tokenObject = (Transform)Instantiate(discs[player - 1], new Vector3(pos.x * tileSize, pos.y * tileSize + yOffset, 0), Quaternion.identity);
            token tok = new token();
            tok.tokenObject = tokenObject;
            tok.ownerPlayer = player;
            tok.tokenObject.transform.FindChild("sprite").GetComponent<SpriteRenderer>().sortingOrder = targetTile.stack;

            targetTile.tokens.Add(tok);
            targetTile.stack += 1;
            return true;
        }
        return false;
    }

    bool FlipStack(Vector2 pos)
    {
        // check in bounds
        if (!CheckInBounds(pos))
        {
            return false;
        }
        else
        {
            tile targetTile = null;
            // find tile in pos
            foreach (tile t in tileList)
            {
                if (t.tilePos == pos)
                {
                    targetTile = t;
                    break;
                }
            }

            // are there more than one tokens on the stack
            if (targetTile.tokens.Count < 1)
            {
                return false;
            }

            // reverse stack  
            targetTile.tokens.Reverse();
                                                                                                                                                                          
            // place pieces in reverse order
            for (int k = 0; k < targetTile.tokens.Count; k++)
            {
                int yOffset = -3 + k * 2;
                Transform tTrans = targetTile.tokens[k].tokenObject.transform;
                //  Transform tTrans = targetTile.tokens[targetTile.tokens.Count - 1 - k].tokenObject.transform;
                tTrans.position = new Vector3(targetTile.tilePos.x * tileSize, targetTile.tilePos.y * tileSize + yOffset, 0);

                // adjust z-depth 
                tTrans.FindChild("sprite").GetComponent<SpriteRenderer>().sortingOrder = k;
            }

            return true; 
        }
    }

    bool MoveStack(Vector2 pos)
    {
        // check in bounds
        if (!CheckInBounds(pos))
        {
            return false;
        }
        else
        {

            tile targetTile = null;

            // find tile in pos
            foreach (tile t in tileList)
            {
                if (t.tilePos == pos)
                {
                    targetTile = t;
         

                    // highlight tile
                    selectedTileTransform = (Transform)Instantiate(highlightTile, new Vector3(pos.x * tileSize, pos.y * tileSize, 0), Quaternion.identity);
                    selectedTile = t;
                    break;
                }
            }

            // are there more than one tokens on the stack
            if (targetTile.tokens.Count < 1)
            {
                return false;
            }

        }
        return true;
    }

    bool PlaceStack(Vector2 pos)
    {
        // see if valid position
        // check in bounds
        if (!CheckInBounds(pos))
        {
            return false;
        }

        tile targetTile = null;
        // check target tile empty
        foreach (tile t in tileList)
        {
            if (t.tilePos == pos)
            {
                targetTile = t;
                break;
            }

        }

        if(targetTile.stack > 0)
        {
            return false;
        }

        // place stack, i.e. move from existing location
        // change ownership of tile
        try
        {
            for (int k = 0; k < selectedTile.tokens.Count; k++)
            {
                int yOffset = -3 + k * 2;

                selectedTile.tokens[k].tokenObject.transform.position = new Vector3(pos.x * tileSize, pos.y * tileSize + yOffset, 0);

            }
            Debug.Log("tiles moved");
            targetTile.tokens = selectedTile.tokens;
            targetTile.stack = selectedTile.stack;
            //selectedTile.tokens;
            selectedTile.stack = 0;
            Debug.Log("Stacks rejigged");
            
            Debug.Log("Stack destroyed");
            // back to game mode
            targetTile.tokens = new List<token>(selectedTile.tokens);
            selectedTile.tokens.Clear();
            Debug.Log(targetTile.tokens[0].tokenObject.transform.position.x);
            state = states.playing;

        }
        catch
        {
            
            return false;
        }


        // remove highlighted tile
        selectedTileTransform.gameObject.SetActive(false) ;
        return true;
    }

    void FinishTurn()
    {
        player += 1;
        if(player > playerLimit)
        {
            player = 1;
        }

        // update selector sprite
        selector.FindChild("Selector1").GetComponent<SpriteRenderer>().sprite = selectorSprites[player];

        CheckEnd();
    }

    bool CheckInBounds(Vector2 pos)
    {
        if ((pos.x >= 0 && pos.x < 4) && (pos.y >= 0 && pos.y < 4))
        {
            return true;
        } else
        {
            return false;
        }
    }

    void CheckEnd()
    {
        // cycle through all tiles
        // see if there is a token
        // check the topmost token 
        // then look in all directions if there is another similar topmost token
        // if yes, then check in the same direction if there is a third

        // if there is a combo of three, game over

        foreach (tile t in tileList)
        {
            if(t.tokens.Count > 0)
            {
                int tokenType = t.tokens[t.tokens.Count - 1].ownerPlayer;
                Debug.Log("The topmost token on tile at position " + t.tilePos + " belongs to player " + tokenType);
                // then check all legitimate neighbouring tiles
                foreach (Vector2 d in directions)
                {
                    Vector2 targetTilePos = t.tilePos + d;
                    Debug.Log("target tile pos " + targetTilePos);
                    // check in bounds
                    if (CheckInBounds(targetTilePos))
                    {
                        // in bounds, so find the tile in the position and check the topmost tile
                        foreach (tile t2 in tileList)
                        {
                            if (t2.tilePos == targetTilePos)
                            {
                                if(t2.tokens.Count > 0)
                                {
                                    if(t2.tokens[t2.tokens.Count - 1].ownerPlayer == tokenType)
                                    {
                                        Debug.Log("Two in a row");
                                        // second tile matches so check third in the same dir
                                        targetTilePos += d;
                                        if (CheckInBounds(targetTilePos))
                                        {
                                            foreach (tile t3 in tileList)
                                            {
                                                if(t3.tilePos == targetTilePos)
                                                {
                                                    if(t3.tokens.Count > 0)
                                                    {
                                                        if(t3.tokens[t3.tokens.Count - 1].ownerPlayer == tokenType)
                                                        {
                                                            EndGame();
                                                            // highlight winning combination of tiles
                                                            Debug.Log(t.tilePos.x);
                                                            Instantiate(highlightTile, new Vector3(t.tilePos.x * tileSize, t.tilePos.y * tileSize, 0), Quaternion.identity);
                                                            Instantiate(highlightTile, new Vector3(t2.tilePos.x * tileSize, t2.tilePos.y * tileSize, 0), Quaternion.identity);
                                                            Instantiate(highlightTile, new Vector3(t3.tilePos.x * tileSize, t3.tilePos.y * tileSize, 0), Quaternion.identity);
                                                            Debug.Log("Player " + tokenType + " wins!");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void EndGame()
    {
        state = states.ending;
    }

    void ToggleHeatmap(bool on)
    {
            foreach (tile t in debugTileList)
            {
                t.tileObject.gameObject.SetActive(on);
            }

    }

    void DrawDebugHeatmap()
    {
        Color colorStart = Color.gray;
        Color colorEnd = Color.green;
        Color colorStart2 = Color.green;
        Color colorEnd2 = Color.red;
        // AI move valuation heatmap
        // go through grid
        // read active player
        // adjust transparent squares based on value in tile weight for that player number
        foreach(tile t in tileList)
        {
            foreach(tile dt in debugTileList)
            {
                if(t.tilePos == dt.tilePos)
                {
                    float lerp;
                    int moveWeight = t.moveWeights[player - 1];
                    if (moveWeight > -1 && moveWeight < 6)
                    {
                        // 1 to 5 should go from 0.2 to 1.0
                        lerp = moveWeight / 10 * 2;
                        Debug.Log("first lerp " + lerp);
                    } 
                    else
                    {
                        // 6 to 10 should also go from 0.2 to 1.0
                        lerp = (moveWeight - 5) / 10 * 2;
                        // 6 / 20 = 3 / 10 = 0.3
                        Debug.Log("second lerp " + lerp);
                        
                    }
                    

                    dt.tileObject.FindChild("debugTile").GetComponent<Renderer>().material.color = Color.Lerp(colorStart, colorEnd, lerp); 
                }
            }


        }





    }

    void InitialBoardValues()
    {
        // edge tiles have a value of 1, core tiles more
        foreach (tile t in tileList)
        {
            bool isEdge = false;
            foreach (Vector2 d in directions)
            {
                if(!CheckInBounds(t.tilePos + d))
                {
                    isEdge = true;
                }
            }
            for (int p = 0; p < playerLimit; p++)
            {
                if(isEdge)
                {
                    t.moveWeights[p] = 1;
                }
                else
                {
                    t.moveWeights[p] = 9;
                }
            }
        }

    }

    void UpdateBoardValues()
    {
        // update the weightings of each tile on the board for each player based on some heuristics

        // have some kind of debug mode which shows the heatmap of valuable moves by player

        // things to take into account:
        // is there an imminent victory about to happen? player order matters here
        // bottom and top tiles in stacks
        // gaps in threes
        // how to mark that flipping the stack is the right move?
        // how to mark that moving the stack is the right move? 


        // move values from 1 to 10, if 10 it's a winning move
        // the number should denote which tiles to pay attention to
        // when checking: if you yourself don't have a 10, check the next person and block their 10

        // cycle through all tiles

       

        // view neighbouring tiles, if they are present

        // awareness of top and bottom tile

        // 
        foreach (tile t in tileList)
        {
            // check all neighbours within board bounds
            for (int d = 0; d < directions.Length; d++)
            {
                if(CheckInBounds(t.tilePos + directions[d]))
                {
                    // cycle through all players
                    for (int p = 1; p <= playerLimit; p++)
                    {
                        // look at topmost token
                        if(t.tokens[t.tokens.Count].ownerPlayer == p)
                        {

                        }

                        // look at bottommost token, if not the top one

                    }
                }
            }

        }
        

    }

    // Update is called once per frame
	void Update () {
        if(Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        }
        if(Input.GetKeyDown(KeyCode.Y))
        {
            ToggleHeatmap(true);
            DrawDebugHeatmap();
        } 
        if(Input.GetKeyDown(KeyCode.U))
        {
            ToggleHeatmap(false);
        }

	    if(state == states.playing)
        {
            // Keyboard controls
            if(Input.GetKeyDown(KeyCode.A))
            {
                MakeMove(new Vector2(-1, 0));
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                MakeMove(new Vector2(1, 0));
            } 
            if (Input.GetKeyDown(KeyCode.W))
            {
                MakeMove(new Vector2(0, 1));
            } 
            if (Input.GetKeyDown(KeyCode.S))
            {
                MakeMove(new Vector2(0, -1));
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Period))
            {
                if (PlaceToken(selectorPos))
                {
                    FinishTurn();
                }
            }
            if(Input.GetKeyDown(KeyCode.F))
            {
                if(FlipStack(selectorPos))
                {
                    FinishTurn();// FlipStack(selectorPos);        
                }
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (MoveStack(selectorPos))
                {
                    state = states.moving;
                }
            }

        }
        
        if(state == states.moving)
        {
            // Keyboard controls
            if (Input.GetKeyDown(KeyCode.A))
            {
                MakeMove(new Vector2(-1, 0));
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                MakeMove(new Vector2(1, 0));
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                MakeMove(new Vector2(0, 1));
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                MakeMove(new Vector2(0, -1));
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Period))
            {
                if (PlaceStack(selectorPos))
                {
                    FinishTurn();
                }
            }

        }
        

	}

    // AI 
    public void AI_move()
    {
        // think about best move

        // 


    }
}
