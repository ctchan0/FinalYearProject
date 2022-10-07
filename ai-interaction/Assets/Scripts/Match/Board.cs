using System;
using System.Collections;
using System.Collections.Generic;
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
    public Block[] monsterBlocks; // prefab data

    // blocks management
    public GameObject blockManager;
    public Block[] blocks;

    // game state / parameters
    public int numberOfMonsters;
    public int numberOfBlocks {get; set; }
    public bool gameOver = false;

    public Block[] GetBlocksList() // <= shadow copy
    {
        var blocksCopy = new Block[boardDepth * boardWidth]; 
        for (int i = 0; i < blocks.Length; i++)
        {
            blocksCopy[i] = blocks[i];
        }
        return blocksCopy;
    }

    private void Awake()
    {
        this.gridLayout = GetComponent<Grid>();
        this.tilemap = GetComponentInChildren<Tilemap>();

        blocks = new Block[boardDepth * boardWidth];

        // construct shape of traps
        for (int i = 0; i < this.traps.Length; i++)
        {
            this.traps[i].Initialize();
        }
    }

    private void Start()
    {
        
    }

    public void SpawnMonsterAt(int index)
    {
        int spawn = Random.Range(0, 2);
        if (spawn == 0) return; // chance of not spawning

        int n = Random.Range(0, monsterBlocks.Length);
        int row = index / boardWidth;
        int col = index % boardWidth;
        Block monsterBlock = Instantiate(monsterBlocks[n], 
                                        blockManager.transform.position + new Vector3(col, 0, row), 
                                        monsterBlocks[n].transform.rotation);
        numberOfMonsters++;
        PlaceBlock(monsterBlock, row, col);
    }

    public void SpawnMonster(int numOfRow)
    {
        for (int i = 0; i < numOfRow * boardWidth; i++)
        {
            SpawnMonsterAt(i);
        }
        activePiece.ghost.UpdatePos();
    }

    /*
    public IEnumerator SpawnMonsterBlock(int spawnInterval)
    {
        while(activePiece.CloseToGhost())
        {
            yield return new WaitForSeconds(0.1f);
        }

        AllBlocksMoveUp(1);
        SpawnMonster(1);
        
        yield return new WaitForSeconds(30f);
    } */

    public void PlaceBlock(Block block, int row, int col)
    {
        block.index = GetBlockIndexAt(row, col);
        blocks[block.index] = block;
        block.transform.SetParent(blockManager.transform);

        SetTile(blocks[block.index].transform.position);
    }

    public int GetCurrentLevel()
    {
        int index = blocks.Length - 1;
        while (index >= 0 && blocks[index] == null)
        {
            index = index - 1;
        }
        return (int)(index / boardWidth) + 1;
    }

    private void AllBlocksMoveUp(int degreeOfRow)
    {
        for (int i = boardDepth-1-degreeOfRow; i >= 0; i--) // for each row except the top
        {
            for (int j = 0; j < boardWidth; j++) // for each column
            {
                int index = i * boardWidth + j;
                if (blocks[index] == null) continue;

                if (blocks[index + (boardWidth*degreeOfRow)] == null) // won't out of bound
                    TranslateBlock(i, j, i+degreeOfRow, j);
            }
        }
    }

    public Trap GetRandomTrap()
    {
        int random = Random.Range(0, this.traps.Length);
        return this.traps[random];
    }

    public void GameOver(bool win)
    {
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
        gameOver = true;

        Reset();
    }

    public void Reset()
    {
        for (int i = blocks.Length - 1; i >= 0; i--) // clear the board
        {
            if (blocks[i] == null) continue;
            var block = blocks[i];
            ClearTile(block.transform.position);
            Destroy(block.gameObject);
            blocks[i] = null;
        }
        
        numberOfBlocks = 0;
        numberOfMonsters = 0;

        gameOver = false; // restart
        activePiece.EndEpisode();
    }

    public void Occupy(PieceAgent piece) 
    {
        if (piece.route.Length == 0)
        {
            print("An empty piece cannot occupy.");
            return;
        }
        for (int i = 0 ; i < piece.route.Length; i++)
        {
            if (piece.trapBlocks[i])
            {
                Vector3Int blockPos = Vector3Int.FloorToInt(piece.transform.localPosition) + piece.route[i];
                PlaceBlock(piece.trapBlocks[i], blockPos.z, blockPos.x);
            }
        }
    }

    public void Match(Queue<int> matchSeq)
    {
        if (matchSeq.Count == 0) 
        {
            activePiece.AddReward((boardDepth - GetCurrentLevel()) / boardDepth);
            activePiece.SetNewTrap();
            return;
        }

        var nextMatchSeq = new Queue<int>();
        var matchedBlocks = new List<int>();
        while (matchSeq.Count > 0)
        {
            int index = matchSeq.Dequeue();
            int col = index % boardWidth;
            int row = index / boardWidth;
            if (blocks[index] == null) continue;

            int colour = blocks[index].colour;
            List<int> match = BFS(blocks, row, col, colour); // a list of blocks that match

            if (match.Count >= 5)
            {
                foreach (var blockIndex in match)
                {
                    if (blocks[blockIndex].type == BlockType.monster)
                    {
                        numberOfMonsters--;
                    }
                    ClearTile(blocks[blockIndex].transform.position);
                    Destroy(blocks[blockIndex].gameObject);
                    blocks[blockIndex] = null;

                    matchedBlocks.Add(blockIndex);
                }
            }
            else
            {
                //
            }
        }
        foreach (var blockIndex in matchedBlocks)
        {
            Drop(blockIndex + boardWidth, ref nextMatchSeq);
        }

        StartCoroutine(PrepareForMatch(nextMatchSeq));
    }

    public IEnumerator PrepareForMatch(Queue<int> matchSeq)
    {
        yield return new WaitForSeconds(0.5f);
        Match(matchSeq);
    } 

    public void Drop(int index, ref Queue<int> dropBlocks)
    {
        while (index < blocks.Length && blocks[index] != null) // for each block
        {
            int x = index % boardWidth;   // make a copy to calculate new index
            int y = index / boardWidth;
            while (y - 1 >= 0 && blocks[GetBlockIndexAt(y-1, x)] == null) 
            {
                y--;
            }
            
            int newIndex = TranslateBlock(index / boardWidth, index % boardWidth, y, x);
            dropBlocks.Enqueue(newIndex);

            index += boardWidth; // check the next upper block
        }

        activePiece.ghost.UpdatePos();
    }

    public int GetBlockIndexAt(int row, int col)
    {
        return row * boardWidth + col;
    }

    public int TranslateBlock(int startRow, int startCol, int endRow, int endCol)
    {
        int startIndex = GetBlockIndexAt(startRow, startCol);
        int endIndex = GetBlockIndexAt(endRow, endCol);
        if (startIndex != endIndex)
        {
            var block = blocks[startIndex];

            ClearTile(blocks[startIndex].transform.position); // clear original position
            blocks[startIndex] = null;

            block.transform.Translate(new Vector3(endCol-startCol, 0, endRow-startRow));
            blocks[endIndex] = block;
            SetTile(blocks[endIndex].transform.position);
        }

        return endIndex;
    }

    public List<int> BFS(Block[] blocks, int row, int col, int colour)
    {
        List<int> matchBlocks = new List<int>();
        Queue<int> searchBlocks = new Queue<int>();
        
        matchBlocks.Add(GetBlockIndexAt(row, col));
        searchBlocks.Enqueue(GetBlockIndexAt(row, col));

        while (searchBlocks.Count > 0)
        {
            int index = searchBlocks.Dequeue();
            int x = index % boardWidth;
            int y = index / boardWidth;

            int searchIndex;

            searchIndex = index + boardWidth;
            if (y + 1 < boardDepth && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index + 1;
            if (x + 1 < boardWidth && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index - boardWidth;
            if (y-1 >= 0 && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index - 1;
            if (x-1 >= 0 && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }


        }

        return matchBlocks;
    }

    public void ClearTile(Vector3 pos)
    {
        numberOfBlocks--;

        Vector3Int cellPos = gridLayout.WorldToCell(pos);
        this.tilemap.SetTile(cellPos, gridTile); // tile is displayed if not occupied
    }

    public void SetTile(Vector3 pos)
    {
        numberOfBlocks++;

        Vector3Int cellPos = gridLayout.WorldToCell(pos); 
        this.tilemap.SetTile(cellPos, null); // tile is removed if occupied
    }

    public bool IsValidPosition(PieceAgent piece, Vector3 position, Vector3Int[] route)
    {
        for (int i = 0; i < route.Length; i++)
        {
            Vector3Int pos = route[i] + Vector3Int.FloorToInt(position);
            Vector3Int cellPos = gridLayout.WorldToCell(pos); // pos (x, y, z) -> cellPos (x, z, y) !!!

            if (!this.tilemap.HasTile(cellPos)) 
                return false;
        }
        return true;
    }
    

}
