using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    private GridLayout gridLayout;
    public Tilemap tilemap { get; private set; }
    [SerializeField] private TileBase gridTile;

    public Piece piecePrefab; // contain a trap
    public Piece activePiece { get; private set; }

    public Trap[] traps;
    public Vector3Int spawnPosition;

    public int boardWidth = 10;
    public int boardDepth = 20;

    public Block[,] blocks;
    public GameObject blockManager;
    public Block[] monsterBlocks;

    public bool gameOver = false;

    private void Awake()
    {
        this.gridLayout = GetComponent<Grid>();
        this.tilemap = GetComponentInChildren<Tilemap>();
        blockManager = GameObject.Find("BlockManager");

        blocks = new Block[boardDepth, boardWidth];

        this.activePiece = GetComponent<Piece>();

        // construct shape of traps
        for (int i = 0; i < this.traps.Length; i++)
        {
            this.traps[i].Initialize();
        }
    }

    private void Start()
    {
        this.spawnPosition += Vector3Int.FloorToInt(this.transform.position);
        SpawnTrap();
        StartCoroutine(SpawnMonsterBlock());
    }

    public IEnumerator SpawnMonsterBlock()
    {
        while (gameOver == false)
        {
            AllBlocksMoveUp();
            // spawn monster
            for (int j = 0; j < boardWidth; j++)
            {
                int n = UnityEngine.Random.Range(-1, monsterBlocks.Length);
                if (n == -1) continue;
                Block monsterBlock = Instantiate(monsterBlocks[n], 
                                                blockManager.transform.position + new Vector3(j, 0, 0), 
                                                monsterBlocks[n].transform.rotation);
                blocks[0, j] = monsterBlock;
                monsterBlock.index = j;

                SetTile(blocks[0, j].transform.position);
            }

            activePiece.UpdateGhost();
            yield return new WaitForSeconds(30f);
        }
    }

    private void AllBlocksMoveUp()
    {
        for (int i = boardDepth-2; i >= 0; i--) // for each row except the top
        {
            for (int j = 0; j < boardWidth; j++) // for each column
            {
                if (blocks[i, j] == null) continue;

                if (blocks[i+1, j] == null)
                    TranslateBlock(i, j, i+1, j);
            }
        }
    }

    public void SpawnTrap()
    {
        if (activePiece) return;

        int random = UnityEngine.Random.Range(0, this.traps.Length);
        Trap trap = this.traps[random];

        this.activePiece = Instantiate(piecePrefab, this.spawnPosition, Quaternion.identity); // assign new piece
        this.activePiece.Initialize(this, this.spawnPosition, trap);
    }

    public void ContinueToNextPiece()
    {
        activePiece = null;
        if (!gameOver)
            SpawnTrap();
    }

    public void GameOver()
    {
        print("Game Over!");
        gameOver = true;
    }

    public void Occupy(Piece piece, int i) 
    {
        if (piece.trapBlocks[i].TryGetComponent<Block>(out Block block))
        {
            SetTile(piece.transform.position + piece.route[i]);  

            Vector3Int blockPos = Vector3Int.FloorToInt(piece.transform.localPosition) + piece.route[i];
            blocks[blockPos.z, blockPos.x] = block;
            block.index = blockPos.z * boardWidth + blockPos.x;
        }
    }

    public void Match(int index)
    {
        int col = index % boardWidth;
        int row = index / boardWidth;
        if (blocks[row, col] == null) return;
        int colour = blocks[row, col].colour;
        List<int> match = BFS(row, col, colour);
        Debug.Log("row:" + row + ", col:" + col + ", match:" + match.Count);
        if (match.Count >= 5)
        {
            foreach (var blockIndex in match)
            {
                int x = blockIndex % boardWidth;
                int y = blockIndex / boardWidth;
                // Debug.Log(blockIndex);
                ClearTile(blocks[y, x].transform.position);
                Destroy(blocks[y, x].gameObject);
                blocks[y, x] = null;
            }

            var dropBlocks =new List<int>();
            foreach (var blockIndex in match)
            {
                int x = blockIndex % boardWidth;
                int y = blockIndex / boardWidth;
                if (!match.Contains(blockIndex + boardWidth)) // if the upper block is not in the match
                    Drop(y+1, x, ref dropBlocks);
            }

            if (dropBlocks.Count != 0)
            {
                foreach (var blockIndex in dropBlocks)
                    Match(blockIndex);
            }
        }
    }

    public void Drop(int row, int col, ref List<int> drop)
    {
        while (blocks[row, col] != null) // for each block
        {
            int x = col;   // calculate new row
            int y = row;
            while (y - 1 >= 0 && blocks[y-1, col] == null)
            {
                y--;
            }
            
            int index = TranslateBlock(row, col, y, x);
            drop.Add(index);

            row++; // check the next upper block
        }
    }

    public int TranslateBlock(int startRow, int startCol, int endRow, int endCol)
    {
        var block = blocks[startRow, startCol];

        ClearTile(blocks[startRow, startCol].transform.position); // clear original position
        blocks[startRow, startCol] = null;

        block.transform.Translate(new Vector3(endCol-startCol, 0, endRow-startRow));
        blocks[endRow, endCol] = block;
        SetTile(blocks[endRow, endCol].transform.position);

        block.index = endRow * boardWidth + endCol;
        return block.index;
    }

    public List<int> BFS(int row, int col, int colour)
    {
        List<int> matchBlocks = new List<int>();
        Queue<int> searchBlocks = new Queue<int>();
        matchBlocks.Add(row * boardWidth + col);
        searchBlocks.Enqueue(row * boardWidth + col);

        while (searchBlocks.Count > 0)
        {
            int index = searchBlocks.Dequeue();
            int x = index % boardWidth;
            int y = index / boardWidth;

            int searchIndex;

            searchIndex = index + boardWidth;
            if (y + 1 < boardDepth && blocks[y+1, x] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[y+1, x].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index + 1;
            if (x + 1 < boardWidth && blocks[y, x+1] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[y, x+1].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index - boardWidth;
            if (y-1 >= 0 && blocks[y-1, x] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[y-1, x].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            searchIndex = index - 1;
            if (x-1 >= 0 && blocks[y, x-1] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[y, x-1].colour == colour)
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

    public bool IsValidPosition(Piece piece, Vector3 position, Vector3Int[] route)
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
