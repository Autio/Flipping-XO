using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour {
    enum states {starting, playing, ending}
    int tileSize = 16;
    int player = 1;
    int playerLimit = 4;
    states state = states.playing;
    public Transform[] backgroundTiles = new Transform[2];
    public Transform selector;
    private Vector2 selectorPos;
    private List<tile> tileList = new List<tile>();

    public Transform[] discs = new Transform[4];
	// Use this for initialization
	void Start () {
        selectorPos = new Vector2(0, 0);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tile newTile = new tile();
                Transform t;
                if (i % 2 == 0 && j % 2 == 1 || i % 2 == 1 && j % 2 == 0)
                {
                    t = (Transform)Instantiate(backgroundTiles[0], new Vector2(i * tileSize, j * tileSize), Quaternion.identity);
                }
                else
                {
                    t = (Transform)Instantiate(backgroundTiles[1], new Vector2(i * tileSize, j * tileSize), Quaternion.identity);
                }
                newTile.tileObject = t;
                newTile.tilePos = new Vector2(i, j);
                tileList.Add(newTile);
            }
        }
	}

    class tile
    {
        public Vector2 tilePos;
        public int stack;
        public Transform tileObject;
        public List<Transform> tokens = new List<Transform>();
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
        Debug.Log(selectorPos);
        selector.transform.position = new Vector3(selectorPos.x * tileSize, selectorPos.y * tileSize, 0);
    }
	
    void PlaceToken(Vector2 pos)
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
            Transform token = (Transform)Instantiate(discs[player - 1], new Vector3(pos.x * tileSize, pos.y * tileSize + yOffset, 0), Quaternion.identity);
            targetTile.tokens.Add(token);
            targetTile.stack += 1;
        }
        
    }


    void FinishTurn()
    {
        player += 1;
        if(player > playerLimit)
        {
            player = 1;
        }
    }

    // Update is called once per frame
	void Update () {
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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlaceToken(selectorPos);
                FinishTurn();
            }
        }
        

	}
}
