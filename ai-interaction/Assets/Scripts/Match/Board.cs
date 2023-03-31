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
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase markTile;


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
    public GameObject blocks; // the origin

    // game state / parameters
    public int numberOfMonsters;
    public int numberOfBlocks;
    public bool gameOver = false;
    public bool enableSpawner = false;
    public bool autoSpawner = true;
    public int numberOfMonstersEachWave = 3;
    private bool canSpawn = false;
    public bool independentPlay = true;
    public int spawnInterval = 15000;
    private bool spawnTrigger = false;
    private int m_ResetTimer;

    private void Awake()
    {
        this.gridLayout = GetComponent<Grid>();

        blockManager = new BlockManager(this, boardDepth, boardWidth);

        // construct shape of traps
        for (int i = 0; i < this.traps.Length; i++)
        {
            this.traps[i].Initialize();
        }

        if (GameObject.Find("MainManager"))
        {
            spawnInterval = MainManager.Instance.spawnT;
        }
    }

    private void FixedUpdate() 
    {
        if (!gameOver)
            m_ResetTimer += 1;

        if (m_ResetTimer != 0 && m_ResetTimer % spawnInterval == 0)
        {
            spawnTrigger = true;
        }
        if (!activePiece.inactive && spawnTrigger)
        {
            spawnTrigger = false;
            StartMonsterTurn();
        }
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
        m_ResetTimer = 0;
        spawnTrigger = false;
        gameOver = false;

        yield return new WaitForSeconds(1f);
        activePiece.EndEpisode();
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

    #region Monster
    public void StartMonsterTurn() // human-playable
    {
        if (enableSpawner)
        {
            blockManager.AllBlocksMoveUp(1);
            if (autoSpawner)
            {
                SpawnMonsterInAuto(1); // should always turn canSpawn to be true
                // if (this.active)
                //    this.activePiece.StartAdventurerTurn();
            }
            else
                StartCoroutine(SpawnMonster());
        }
        // else
        //    this.activePiece.StartAdventurerTurn();
        /*
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
        */
    } 

    public IEnumerator SpawnMonster()
    {
        canSpawn = true;
        Debug.Log("Monster's turn: you can spawn Monster at the bottom now by clicking!");
        yield return new WaitForSeconds(10f);
        canSpawn = false;
        Debug.Log("End of Monster's turn");

        activePiece.ghost.UpdatePos();
        this.activePiece.StartAdventurerTurn();
    }

    public void SpawnMonsterByClick(Vector3 position)
    {
        if (!canSpawn) return;
        int index = (int)position.x;
        if (blockManager.blocks[index] != null) return;

        SpawnMonsterAt(index);
    }

    public void SpawnMonsterAt(int index, int n = -1) // n == -1 : randomly spawn
    {
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

    public void SpawnMonsterInAuto(int numOfRow)
    {
        List<int> numberList = new List<int>();
        for (int i = 0; i < boardWidth; i++)
            numberList.Add(i);

        for (int i = 0; i < numberOfMonstersEachWave; i++)
        {
            int index = Random.Range(0, numberList.Count);
            int spawn = numberList[index];
            numberList.RemoveAt(index);

            SpawnMonsterAt(spawn, -1);
        }
        activePiece.ghost.UpdatePos();
    }

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
    #endregion

    #region Blocks
    public void AddBlock()
    {
        numberOfBlocks++;
    }

    public void DestroyBlock(Block block)
    {
        if (block)
        {
            numberOfBlocks--;
            Destroy(block.gameObject);
        }
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
    #endregion

    #region Trap Request
    public Trap GetRandomTrap()
    {
        int random = Random.Range(0, this.traps.Length);
        return this.traps[random];
    }

    public Trap GetTrap(int index)
    {
        return this.traps[index]; 
    }
    #endregion

    #region Match
    public void Match(Queue<int> matchSeq)
    {
        /* No match and drop anymore */
        if (matchSeq.Count == 0) 
        {
            activePiece.inactive = false;
            if (!gameOver && independentPlay) // if it is independent, end until no move (win condition depends on itself)
                activePiece.StartNewTurn();
            else
                this.blockManager.CheckOutBoundBlocks(); // else, create space for next move (win condition depends on battlefield)
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

    #endregion

    #region Tile
    public void MarkLocation(Vector3Int localPos)
    {
        Vector3Int pos = new Vector3Int(localPos.x - (boardWidth / 2), localPos.z - (boardDepth / 2), localPos.y);
        tilemap.SetTile(pos, markTile);
    }

    public void RemoveMark()
    {
        tilemap.ClearAllTiles();
    }
    #endregion
}
