﻿using UnityEngine;
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
        public List<token> tokens = new List<token>();
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
            Transform tokenObject = (Transform)Instantiate(discs[player - 1], new Vector3(pos.x * tileSize, pos.y * tileSize + yOffset, 0), Quaternion.identity);
            token tok = new token();
            tok.tokenObject = tokenObject;
            tok.ownerPlayer = player;
            targetTile.tokens.Add(tok);
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
