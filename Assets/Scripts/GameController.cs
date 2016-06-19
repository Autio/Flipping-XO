using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameController : MonoBehaviour {
    enum states {starting, playing, moving, transitioning, ending}
    enum moves { place, flip, move}
    int tileSize = 16;
    int player = 1;
    public int playerLimit = 4;
    public int humanPlayers = 1; // others are AI
    states state = states.playing;
    moves currentMove = moves.place;
    public Transform[] backgroundTiles = new Transform[2];
    public Transform debugTile;
    public Transform highlightTile;
    public Transform selector;
    public Sprite[] selectorSprites = new Sprite[5];
    private Vector2 selectorPos;
    public Sprite[] placeArrowSprites = new Sprite[4];
    public Sprite[] flipSprites = new Sprite[4];
    public Sprite[] moveStackSprites = new Sprite[4];
    public Transform ActionSprite;

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
        // update which action sprite is shown, type and colour
        UpdateActionSprite();


        // AI heatmap
        InitialBoardValues();

        ToggleHeatmap(false);


        UpdateActionSprite();
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

    void UpdateActionSprite()
    {
        // switch all action sprites off


        // check current move type to determine which sprite to activate 
        if(currentMove == moves.place)
        {
           
            ActionSprite.GetComponent<SpriteRenderer>().sprite = placeArrowSprites[player - 1];
         
        } else if (currentMove == moves.flip)
        {
            ActionSprite.GetComponent<SpriteRenderer>().sprite = flipSprites[player - 1];

        } else if (currentMove == moves.move)
        {

            ActionSprite.GetComponent<SpriteRenderer>().sprite = moveStackSprites[player - 1];

        }

        // check player number to determine colour


    }

    tile FindTileByPos(Vector2 pos, List<tile> tList)
    {
        tile foundTile = null;

        foreach (tile t in tList)
        {
            if(t.tilePos == pos)
            {
                foundTile = t;
                return foundTile;
            }
        }

        return foundTile;
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

    bool DoAction(Vector2 pos)
    {
        if(currentMove == moves.place)
        {
            if(PlaceToken(pos))
            {
                return true;
            }
        } 
        if(currentMove == moves.flip)
        {
            if(FlipStack(pos))
            {
                return true;
            }
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

            selectedTile.stack = 0;

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

    IEnumerator FinishTurn()
    {
        // delay
        state = states.transitioning;
        yield return new WaitForSeconds(0.5f);

        player += 1;
        if(player > playerLimit)
        {
            player = 1;
        }

        // update selector sprite and action sprite
        selector.FindChild("Selector1").GetComponent<SpriteRenderer>().sprite = selectorSprites[player];
        UpdateActionSprite();
        if (!CheckEnd())
        {
            // rethink the map
            UpdateBoardValues();
            DrawDebugHeatmap();
            state = states.playing;
        }
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

    bool CheckEnd()
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
                                                            return true;
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
        return false;
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
        Color colorEnd = Color.yellow;
        Color colorStart2 = Color.yellow;
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
                    float lerp = 0.0f;
                    int moveWeight = t.moveWeights[player - 1];
                    if (moveWeight > -1 && moveWeight < 6)
                    {
                        // 1 to 5 should go from 0.2 to 1.0
                        lerp = moveWeight / 10.0f * 2.0f;
                        Debug.Log("moveweight " + moveWeight);
                        Debug.Log("first lerp " + lerp);
                        dt.tileObject.FindChild("debugTile").GetComponent<Renderer>().material.color = Color.Lerp(colorStart, colorEnd, lerp);
                    } 
                    else if (moveWeight > 5)
                    {
                        // 6 to 10 should also go from 0.2 to 1.0
                        lerp = (moveWeight - 5.0f) / 10.0f * 2.0f;
                        // 6 / 20 = 3 / 10 = 0.3
                        Debug.Log("moveweight " + moveWeight);
                        Debug.Log("second lerp " + lerp);
                        dt.tileObject.FindChild("debugTile").GetComponent<Renderer>().material.color = Color.Lerp(colorStart2, colorEnd2, lerp);

                    }
                    

                   
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
                    t.moveWeights[p] = 2;
                }
            }
        }

    }

    
    // Main AI brain 
    void UpdateBoardValues()
    {
        // update the weightings of each tile on the board for the current player based on some heuristics

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

            if (t.tokens.Count >= 4)
            {
                t.moveWeights[player - 1] = 0;

                // except if you should be moving the stack...
            }

            if (t.tokens.Count > 1 && t.tokens[0].ownerPlayer == player)  
            {
                // bottommost tile is yours, you should consider flipping it
                t.moveWeights[player - 1] = 4;
                // how to mark it to be flipped?

            } 
            else
            {

                // check all neighbours within board bounds
                for (int d = 0; d < directions.Length; d++)
                {
                    if (CheckInBounds(t.tilePos + directions[d]))
                    {
                        // find the neighbouring tile in question
                        tile neighbourTile;
                        neighbourTile = FindTileByPos(t.tilePos + directions[d], tileList);

                        // look at topmost token of neighbour tile
                        if (neighbourTile.tokens.Count != 0 && neighbourTile.tokens[neighbourTile.tokens.Count - 1].ownerPlayer == player)
                        {
                            // if neighbouring tile has your token on it, you want to consider placing your token next to it
                            t.moveWeights[player - 1] = 5;

                            // iterate further in the same direction to see if there's two in a line
                            if (CheckInBounds(t.tilePos + (directions[d] * 2)))
                            {
                                // find the neighbouring tile in question
                                neighbourTile = FindTileByPos(t.tilePos + (directions[d] * 2), tileList);

                                // look at topmost token of neighbour tile
                                if (neighbourTile.tokens.Count != 0 && neighbourTile.tokens[neighbourTile.tokens.Count - 1].ownerPlayer == player)
                                {
                                    // this move will win
                                    t.moveWeights[player - 1] = 10;
                                }
                            }

                            // check the opposite direction
                            int od;
                            if (d < 4)
                            {
                                od = d + 4;
                            } else
                            {
                                od = d - 4;
                            }

                            if (CheckInBounds(t.tilePos + (directions[od])))
                            {
                                // find the neighbouring tile in question
                                neighbourTile = FindTileByPos(t.tilePos + (directions[od]), tileList);

                                // look at topmost token of neighbour tile
                                if (neighbourTile.tokens.Count != 0 && neighbourTile.tokens[neighbourTile.tokens.Count - 1].ownerPlayer == player)
                                {
                                    // this move will win
                                    t.moveWeights[player - 1] = 10;
                                }
                            }

                        }




                        // check the opposite direction to see if there's two in a line
                        // opposite directions: 0, 4; 1, 5; 2, 6; 3, 7;

                    }
                }
            }
            
            // if the tile already has the player's tile at the top of its stack, set desirability to minimum
            if (t.tokens.Count != 0 && t.tokens[t.tokens.Count - 1].ownerPlayer == player)
            {
                t.moveWeights[player - 1] = 0;
            }

        }
        

    }

    void ChangeCurrentMove()
    {
        if (currentMove == moves.place)
        {
            currentMove = moves.flip;
        }
        else if (currentMove == moves.flip)
        {
            currentMove = moves.move;
        }
        else if (currentMove == moves.move)
        {
            currentMove = moves.place;
        }

        UpdateActionSprite();
    }

    bool HumanPlayerActive()
    {
        // if the player number is greater than the amount of human players in the game, and there are more players than there are human players, then it must be an AI
        if (player > humanPlayers && player <= playerLimit)
        {
            return false;
        }
        return true;
    }

    // Update is called once per frame
	void Update () {
        // work whether or not active player is human
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
            UpdateActionSprite();
        }
        if(Input.GetKeyDown(KeyCode.H))
        {
            AI_move();
        }

	    if(state == states.playing)
        {
            // only if human
            if (HumanPlayerActive())
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
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Period))
                {
                    if (currentMove != moves.move)
                    {
                        if (DoAction(selectorPos))
                        {
                            StartCoroutine("FinishTurn");
                        }
                    }
                    else if (currentMove == moves.move)
                    {
                        if (MoveStack(selectorPos))
                        {
                            state = states.moving;
                        }
                    }
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // switch between move types
                    ChangeCurrentMove();
                }
            }
            else
            {
                // AI player active
                Debug.Log("AI player moving");
                AI_move();
                //StartCoroutine("FinishTurn");


            }
        }
        
        if(state == states.moving)
        {
            if (HumanPlayerActive())
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
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (PlaceStack(selectorPos))
                    {
                        StartCoroutine("FinishTurn");
                    }
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // back to game mode
                    Destroy(selectedTileTransform.gameObject, 0.1f);
                    ChangeCurrentMove();
                    state = states.playing;

                }
            }
            else
            {


            }

        }
        

	}

    // AI 
    public void AI_move()
    {
        tile chosenTile;

        // use Linq to order tile selection
        List<tile> sortedTiles= tileList.OrderByDescending(t=>t.moveWeights[player - 1]).ToList();

        foreach (tile f in sortedTiles)
        {
            Debug.Log("sorted tile list: tile " + f.tilePos + " " + f.moveWeights[player - 1]);
        }

        // how many of the best moves will you consider?
        int movesToConsider = 4;

        // choose the best move with a high likelyhood
        float choiceBias = 0.9f;

        // choose move
        int choice = 0;
        if(Random.Range(0.0f, 1.0f) < choiceBias)
        {
            chosenTile = sortedTiles[choice];
        } else
        {
            choice = Random.Range(1, movesToConsider);
            chosenTile = sortedTiles[choice];
        }

        // execute move 
        StartCoroutine(MakeAIMove(chosenTile, moves.place, sortedTiles, choice));

    }

    IEnumerator MakeAIMove(tile chosenTile, moves moveChoice, List<tile> sortedTiles, int moveChoiceIndex = 0)
    {
        if (state != states.ending)
        {
            Debug.Log("AI aiming to go to " + chosenTile.tilePos.x + ", " + chosenTile.tilePos.y);
            float defaultWait = 0.02f;
            state = states.transitioning;
            yield return new WaitForSeconds(defaultWait);
            // is current tile the right tile? 
            bool targetReached = false;
            while (!targetReached)
            {
                if (chosenTile.tilePos == selectorPos)
                {
                    Debug.Log("AI target tile reached");
                    targetReached = true;
                    // in the right tile, so make the right move
                    yield return new WaitForSeconds(defaultWait);
                    currentMove = moveChoice;
                    UpdateActionSprite();
                    yield return new WaitForSeconds(defaultWait);
                    if (currentMove != moves.move)
                    {
                        if (DoAction(selectorPos))
                        {
                            Debug.Log("AI " + player + " move made");
                        }
                        else
                        {
                            // for some reason, this move was invalid - so choose the next move
                            targetReached = false;
                            moveChoiceIndex++;
                            chosenTile = sortedTiles[moveChoiceIndex];
                        }
                    }
                    else if (currentMove == moves.move)
                    {
                        // Do something clever for actually moving stacks...
                        if (MoveStack(selectorPos))
                        {
                            state = states.moving;
                        }
                    }

                }
                else
                {
                    // figure out x and y differences
                    int xDiff = (int)(selectorPos.x - chosenTile.tilePos.x);
                    int yDiff = (int)(selectorPos.y - chosenTile.tilePos.y);
                    Debug.Log("xDiff " + xDiff + ", yDiff " + yDiff);

                    // make appropriate move and then wait
                    Debug.Log("AI " + player + " making move...");
                    if (xDiff < 0)
                    {
                        MakeMove(new Vector2(1, 0));
                    }
                    else if (xDiff > 0)
                    {
                        MakeMove(new Vector2(-1, 0));
                    }
                    else if (yDiff < 0)
                    {
                        MakeMove(new Vector2(0, 1));
                    }
                    else if (yDiff > 0)
                    {
                        MakeMove(new Vector2(0, -1));
                    }

                    yield return new WaitForSeconds(defaultWait);

                }
            }
            StartCoroutine("FinishTurn");
        }
        
    }
}
