using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;


public class Board : MonoBehaviour
{
    private GridLayout gridLayout;
    public Tilemap tilemap { get; private set; }
    [SerializeField] private TileBase gridTile;

    public PieceAgent activePiece;

    public Trap[] traps;

    public int boardWidth = 10;
    public int boardDepth = 20;

    public GameObject blockManager;
    public Block[] blocks;
    public Block[] monsterBlocks;

    public bool gameOver = false;

    public Block[] GetBlocksList() // shadow copy
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
        StartCoroutine(SpawnMonsterBlock());
    }

    public IEnumerator SpawnMonsterBlock()
    {
        while (true)
        {
            while(activePiece.CloseToGhost())
            {
                yield return new WaitForSeconds(0.1f);
            }

            AllBlocksMoveUp();
            // spawn monster
            for (int j = 0; j < boardWidth; j++)
            {
                int n = Random.Range(-1, monsterBlocks.Length);
                if (n == -1) continue;
                Block monsterBlock = Instantiate(monsterBlocks[n], 
                                                blockManager.transform.position + new Vector3(j, 0, 0), 
                                                monsterBlocks[n].transform.rotation);
                PlaceBlock(monsterBlock, 0, j);
            }

            activePiece.ghost.UpdatePos();
            yield return new WaitForSeconds(30f);
        }
    }

    public void PlaceBlock(Block block, int row, int col)
    {
        block.index = GetBlockIndexAt(row, col);
        blocks[block.index] = block;
        block.transform.SetParent(blockManager.transform);

        SetTile(blocks[block.index].transform.position);
    }

    private void AllBlocksMoveUp()
    {
        for (int i = boardDepth-2; i >= 0; i--) // for each row except the top
        {
            for (int j = 0; j < boardWidth; j++) // for each column
            {
                int index = i * boardWidth + j;
                if (blocks[index] == null) continue;

                if (blocks[index + boardWidth] == null)
                    TranslateBlock(i, j, i+1, j);
            }
        }
    }

    public Trap GetRandomTrap()
    {
        int random = Random.Range(0, this.traps.Length);
        return this.traps[random];
    }

    public void GameOver()
    {
        print("Game Over!");
        gameOver = true;

        Reset();
    }

    public void Reset()
    {
        for (int i = blocks.Length - 1; i >= 0; i--)
        {
            if (blocks[i] == null) continue;
            var block = blocks[i];
            ClearTile(block.transform.position);
            Destroy(block.gameObject);
            blocks[i] = null;
        }
        for (int i = 0; i < activePiece.trapBlocks.Length; i++)
        {
            if (activePiece.trapBlocks[i] == null) continue;
            var block = activePiece.trapBlocks[i];
            Destroy(block.gameObject);
            activePiece.trapBlocks[i] = null;
        }
        gameOver = false;

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
                    ClearTile(blocks[blockIndex].transform.position);
                    Destroy(blocks[blockIndex].gameObject);
                    blocks[blockIndex] = null;

                    matchedBlocks.Add(blockIndex);
                }
            }
        }
        foreach (var blockIndex in matchedBlocks)
        {
            Drop(blockIndex + boardWidth, ref nextMatchSeq);
        }

        if (matchedBlocks.Count <= 10)
            activePiece.AddReward(matchedBlocks.Count * 0.1f);
        else 
            activePiece.AddReward(1f);

        StartCoroutine(PrepareForMatch(nextMatchSeq));
    }

    public IEnumerator PrepareForMatch(Queue<int> matchSeq)
    {
        yield return new WaitForSeconds(1f);
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
        Vector3Int cellPos = gridLayout.WorldToCell(pos);
        this.tilemap.SetTile(cellPos, gridTile); // tile is displayed if not occupied
    }

    public void SetTile(Vector3 pos)
    {
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
