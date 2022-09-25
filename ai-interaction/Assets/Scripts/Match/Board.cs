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

    public bool gameOver = false;

    private GameObject boardManager;

    private void Awake()
    {
        this.gridLayout = GetComponent<Grid>();
        this.tilemap = GetComponentInChildren<Tilemap>();
        boardManager = GameObject.Find("BoardManager");

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
            Vector3Int cellPos = gridLayout.WorldToCell(Vector3Int.FloorToInt(piece.transform.position) + piece.route[i]); 
            this.tilemap.SetTile(cellPos, null);  // tile is removed when occupied

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
                Clear(blocks[y, x].transform.position);
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
            var block = blocks[row,col];
            Clear(blocks[row, col].transform.position); // clear the orginal position
            blocks[row, col] = null;
            int x = col;
            int y = row;
            while (y - 1 >= 0 && blocks[y-1, col] == null)
            {
                y--;
            }
            
            block.transform.position = boardManager.transform.position + new Vector3(x, 0, y); // need to change to local
            blocks[y, x] = block;
            block.index = y * boardWidth + x;
            drop.Add(block.index);

            Vector3Int cellPos = gridLayout.WorldToCell(Vector3Int.FloorToInt(block.transform.position)); // occupy
            this.tilemap.SetTile(cellPos, null);

            row++; // check the next upper block
        }
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



    public void Clear(Vector3 pos)
    {
        Vector3Int cellPos = gridLayout.WorldToCell(pos);
        this.tilemap.SetTile(cellPos, gridTile); // tile is displayed if not occupied
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
