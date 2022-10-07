using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    public Block ghostBlockPrefab;
    public Block[] ghostBlocks;
    private PieceAgent piece;
    private Board board;
    private Block[] blocks;
    private int maxMatch = 0;

    private void Awake()
    {
        board = FindObjectOfType<Board>();
    }

    public void Initialize(PieceAgent piece)
    {
        this.piece = piece;

        this.transform.position = piece.transform.position;

        ghostBlocks = new Block[piece.trapData.route.Length];
        for (int i = 0; i < piece.route.Length; i++) // Instantiate the ghost
        {
            Vector3 newPos = piece.route[i] + this.transform.position;
            Block block = Instantiate(ghostBlockPrefab, newPos, Quaternion.identity);
            block.colour = piece.trapBlocks[i].GetComponent<Block>().colour;
            ghostBlocks[i] = block;
            block.transform.SetParent(this.transform);
        }

        this.maxMatch = 0;

        UpdatePos();
    }

    public void UpdatePos() // always update position
    {
        if (piece == null || board.gameOver) return;
        Vector3Int pos = piece.FindBottom();
        this.transform.position = pos;

        int prev_match = piece.match; // Observe the change of match number
        piece.match = GetNumberOfMatch();
        //if (prev_match != piece.match)
        //    Debug.Log("Match: " + piece.match);

        if (piece.match > this.maxMatch)
        {
            this.maxMatch = piece.match;
            piece.AddReward(0.1f);
        }
        else
        {
            piece.AddReward(-0.1f);
        }
    }

    public void Reset()
    {
        if (ghostBlocks != null)
        {
            foreach (var block in ghostBlocks)
            {
                Destroy(block.gameObject);
            }
            ghostBlocks = null;
        }
        piece = null;
        blocks = null;
    }

    public int GetNumberOfMatch()
    {
        blocks = board.GetBlocksList();
        if (blocks == null) return 0;

        var matchSeq = new Queue<int>();

        for (int i = 0; i < ghostBlocks.Length; i++) 
        {
            Vector3Int blockPos = Vector3Int.FloorToInt(this.transform.localPosition) + piece.route[i];
            int index = board.GetBlockIndexAt(blockPos.z, blockPos.x);
            blocks[index] = ghostBlocks[i];
            ghostBlocks[i].index = index;
            matchSeq.Enqueue(index);
        } 
        return MatchTest(matchSeq, 0);
    }

    public int MatchTest(Queue<int> matchSeq, int numberOfMatch)
    {
        if (matchSeq.Count == 0) 
            return numberOfMatch;

        if (blocks == null) return 0;

        var nextMatchSeq = new Queue<int>();
        var matchedBlocks = new List<int>();
        while (matchSeq.Count > 0)
        {
            int index = matchSeq.Dequeue();
            int col = index % board.boardWidth;
            int row = index / board.boardWidth;
            if (blocks[index] == null) continue;

            int colour = blocks[index].colour;
            List<int> match = board.BFS(blocks, row, col, colour); // a list of blocks that match
            // skip the block that has checked for a match already
            matchSeq = new Queue<int>(matchSeq.Where(x => !match.Contains(x)));

            if (match.Count >= 2)
                numberOfMatch += match.Count;

            if (match.Count >= 5)
            {
                //numberOfMatch += match.Count;
                foreach (var blockIndex in match)
                {
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
            Drop(blockIndex + board.boardWidth, ref nextMatchSeq);
        }

        return MatchTest(nextMatchSeq, numberOfMatch);
    }

    private void Drop(int index, ref Queue<int> dropBlocks)
    {
        while (index < blocks.Length && blocks[index] != null) // for each block
        {
            int startIndex = index;
            int x = index % board.boardWidth;   // make a copy to calculate new index
            int y = index / board.boardWidth;
            while (y - 1 >= 0 && blocks[board.GetBlockIndexAt(y-1, x)] == null) 
            {
                y--;
            }
            int endIndex = board.GetBlockIndexAt(y, x);
            if (startIndex != endIndex)
            {
                var block = blocks[startIndex];
                blocks[startIndex] = null;
                blocks[endIndex] = block;
            }
            
            dropBlocks.Enqueue(endIndex);

            index += board.boardWidth; // check the next upper block
        }
    }


}
