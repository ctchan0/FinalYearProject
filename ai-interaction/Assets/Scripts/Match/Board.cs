using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;


public class Board : MonoBehaviour
{
    // tile 
    private GridLayout gridLayout;
    public Tilemap tilemap { get; private set; }
    [SerializeField] private TileBase gridTile;

    // board
    public int boardWidth = 10;
    public int boardDepth = 20;

    // piece
    public PieceAgent activePiece;
    public Trap[] traps; // prefab data
    public Block[] trapBlocks;
    public MonsterBlock[] monsterBlocks; // prefab data

    // blocks management
    public BlockManager blockManager {get; set;}
    public GameObject blocks;

    // game state / parameters
    public int numberOfMonsters;
    public int numberOfBlocks;
    public bool gameOver = false;

    private void Awake()
    {
        this.gridLayout = GetComponent<Grid>();
        this.tilemap = GetComponentInChildren<Tilemap>();

        blockManager = new BlockManager(this, boardDepth, boardWidth);

        // construct shape of traps
        for (int i = 0; i < this.traps.Length; i++)
        {
            this.traps[i].Initialize();
        }
    }

    private void Start()
    {
        
    }

    /*
    public void StartMonsterTurn() // handle the monsters' effect after one turn pass
    {
        List<MonsterBlock> mBlocks = new List<MonsterBlock>();
        foreach (var block in blockManager.blocks)
        {
            if (block == null)
                continue;
            if (block.type == BlockType.monster)
            {
                mBlocks.Add(block as MonsterBlock);
            }
        }
        StartCoroutine(HandleMonsterBlockTurn(mBlocks));
    } */

    /*
    private IEnumerator HandleMonsterBlockTurn(List<MonsterBlock> blocks)
    {
        if (blocks.Count != 0)
        {
            if (this.blockManager.blocks[blocks[0].index] != null) // if still exists in the board
            {
                // blocks[0].IMMUNITY = false;
                // blocks[0].TURN--;
            }
            blocks.RemoveAt(0);
        }
        if (blocks.Count == 0)
        {
            yield return null;
            activePiece.StartNewTurn();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(HandleMonsterBlockTurn(blocks));
        }
    } */

    public void SpawnMonsterAt(int index, int n) // n == -1 : randomly spawn
    {
        //if (n != -1)
        //    Debug.Log(index);
        if (n == -1)
            n = Random.Range(0, monsterBlocks.Length);
        int row = index / boardWidth;
        int col = index % boardWidth;

        // if there is no empty space for spawning, push the block upwards
        if (blockManager.blocks[index] != null)
        {
            blockManager.AllBlocksMoveUpStartFrom(index);
        }
        MonsterBlock monsterBlock = Instantiate(monsterBlocks[n], 
                                            blocks.transform.position + new Vector3(col, 0, row), 
                                            monsterBlocks[n].transform.rotation);
        monsterBlock.board = this;
        numberOfMonsters++;
        blockManager.PlaceBlock(monsterBlock, row, col);
    }

    public void SpawnBlockAt(int index, int n) // n == -1 : randomly spawn
    {
        if (n == -1)
            n = Random.Range(0, trapBlocks.Length);
        int row = index / boardWidth;
        int col = index % boardWidth;

        // if there is no empty space for spawning, push the block upwards
        if (blockManager.blocks[index] != null)
        {
            blockManager.AllBlocksMoveUpStartFrom(index);
        }
        Block trapBlock = Instantiate(trapBlocks[n], 
                                        blocks.transform.position + new Vector3(col, 0, row), 
                                        trapBlocks[n].transform.rotation);

        blockManager.PlaceBlock(trapBlock, row, col);
    }

    public void SpawnMonster(int numOfRow)
    {
        for (int i = 0; i < numOfRow * boardWidth; i++)
        {
            int spawn = Random.Range(0, 2);
            if (spawn == 0) 
                continue; // chance of not spawning
            SpawnMonsterAt(i, -1);
        }
        activePiece.ghost.UpdatePos();
    }

    public void DestroyBlock(Block block)
    {
        if (block)
        {
            numberOfBlocks--;
            Destroy(block.gameObject);
        }
    }

    /*
    public IEnumerator SpawnMonsterBlock(int spawnInterval)
    {
        while(activePiece.CloseToGhost())
        {
            yield return new WaitForSeconds(0.1f);
        }

        AllBlocksMoveUp(1);
        activePiece.ghost.UpdatePos();
        SpawnMonster(1);
        
        yield return new WaitForSeconds(30f);
    } */


    public Trap GetRandomTrap()
    {
        int random = Random.Range(0, this.traps.Length);
        return this.traps[random];
    }

    public void GameOver(bool win)
    {
        this.activePiece.Reset();
        gameOver = true;
        if (win)
        {
            print("Win");
            activePiece.SetReward(1);
        }
        else
        {
            print("Lose");
            activePiece.SetReward(-1);
        }

        StartCoroutine(Reset());
    }

    public IEnumerator Reset()
    {
        yield return new WaitForSeconds(1f);

        blockManager.Clear();
        foreach (Transform child in blocks.transform) 
        {
            GameObject.Destroy(child.gameObject);
        }
        numberOfBlocks = 0;
        numberOfMonsters = 0;
        gameOver = false;

        yield return new WaitForSeconds(1f);
        activePiece.EndEpisode();
    }

    public Queue<int> Occupy(PieceAgent piece) 
    {
        if (piece.route.Length == 0)
        {
            print("An empty piece cannot occupy.");
            return null;
        }
        Queue<int> q = blockManager.PlacePiece(piece.trapBlocks, 
                                                Vector3Int.FloorToInt(piece.transform.localPosition),
                                                piece.route);
        
        return q;
    }

    public void Match(Queue<int> matchSeq)
    {
        if (matchSeq.Count == 0) 
        {
            if (!gameOver)
            {
                activePiece.Turn++;
                activePiece.SetNewTrap();
            }
            return;
        }

        var nextMatchSeq = new Queue<int>();
        var matchedBlocks = new List<int>();
        while (matchSeq.Count > 0)
        {
            int index = matchSeq.Dequeue(); // get next index
            if (blockManager.blocks[index] == null) continue;

            int colour = blockManager.blocks[index].colour;
            List<int> match = blockManager.MatchSearch(index, colour); // a list of blocks that match
            matchSeq = new Queue<int>(matchSeq.Where(x => !match.Contains(x)));
            
            if (match.Count >= 5)
            {
                activePiece.HasAMatch(match.Count);
                foreach (var blockIndex in match)
                {
                    // how the matched block does
                    blockManager.RemoveMatchBlock(blockIndex);
                    matchedBlocks.Add(blockIndex);
                }
            }
        }
        foreach (var blockIndex in matchedBlocks)
        {
            blockManager.DropBlockAround(blockIndex, ref nextMatchSeq);
            activePiece.ghost.UpdatePos();
        }

        StartCoroutine(PrepareForMatch(nextMatchSeq));
    }

    public IEnumerator PrepareForMatch(Queue<int> matchSeq)
    {
        yield return new WaitForSeconds(0.5f);
        Match(matchSeq);
    } 

    public void AddBlock()
    {
        numberOfBlocks++;
    }

    public bool IsValidPos(Vector3Int localPos, Vector3Int[] route)
    {
        for (int i = 0; i < route.Length; i++)
        {
            Vector3Int pos = route[i] + localPos;
           
            if (pos.x < 0 || pos.x >= boardWidth)
                return false;
            if (pos.z < 0 || pos.z >= boardDepth)
                return false;

            int index = pos.z * boardWidth + pos.x;

            if (blockManager.blocks[index] != null) // occupied
                return false;
            
        }
        return true;
    }
    

}
