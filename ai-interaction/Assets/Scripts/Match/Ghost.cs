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
    private int prevMatch = 0;
    private int prevPosition = 0;

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

        this.prevMatch = 0;
        this.prevPosition = board.boardDepth;
        // this.hasAMatch = false;

        UpdatePos();
    }

    public void UpdatePos() // always update position and check optimal !!!!!!!!
    {
        if (piece == null || board.gameOver) return;
        
        Vector3Int pos = piece.FindBottom();
        this.transform.position = pos;
        if (this.transform.localPosition.z < this.prevPosition)
        {
            piece.AddReward(0.2f);
            // print("This is a better position!");
        }
        else if (this.transform.localPosition.z > this.prevPosition)
        {
            piece.AddReward(-0.2f);
        }
        else 
        {
            piece.AddReward(0.1f);
        }
        this.prevPosition = (int)this.transform.localPosition.z;

        piece.matches = GetMatches();
        piece.numberOfMatches = GetNumberOfMatch(piece.matches);
        if (HasAMatch(piece.matches))
            piece.AddReward(0.2f);
        //print("MatchList:");
        //foreach (var match in piece.matches)
        //{
        //    Debug.Log(match);
        //}
        if (piece.numberOfMatches > this.prevMatch)
        {
            piece.AddReward(0.2f);
            // print("Here is a better match!");
        }
        else if (piece.numberOfMatches < this.prevMatch)
        {
            piece.AddReward(-0.2f);
        }
        else
        {
            piece.AddReward(0.1f);
        }
        this.prevMatch = piece.numberOfMatches;

    }

    public int GetNumberOfMatch(List<int> matches)
    {
        if (matches == null) return 0;
        int n = 0;
        foreach (var match in matches)
        {
            n += match;
        }
        return n;
    }

    public bool HasAMatch(List<int> matches)
    {
        if (matches == null) return false;
        foreach (var match in matches)
        {
            if (match >= 5)
                return true;
        }
        return false;
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

    public List<int> GetMatches()
    {
        blocks = board.GetBlocksList();
        if (blocks == null) return null;

        var matchSeq = new Queue<int>();

        for (int i = 0; i < ghostBlocks.Length; i++) 
        {
            Vector3Int blockPos = Vector3Int.FloorToInt(this.transform.localPosition) + piece.route[i];
            int index = board.GetBlockIndexAt(blockPos.z, blockPos.x);
            blocks[index] = ghostBlocks[i];
            ghostBlocks[i].index = index;
            matchSeq.Enqueue(index);
        } 
        return MatchTest(matchSeq);
    }

    public List<int> MatchTest(Queue<int> matchSeq, List<int> matches = null)
    {
        if (matches == null)
            matches = new List<int>();

        if (matchSeq.Count == 0) 
            return matches;

        if (blocks == null) return null;

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
                matches.Add(match.Count);

            if (match.Count >= 5)
            {
                // hasAMatch = true;
                foreach (var blockIndex in match)
                {
                    blocks[blockIndex] = null;
                    matchedBlocks.Add(blockIndex);
                }
            }
        }
        foreach (var blockIndex in matchedBlocks)
        {  
            Drop(blockIndex + board.boardWidth, ref nextMatchSeq);
        }

        return MatchTest(nextMatchSeq, matches);
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
