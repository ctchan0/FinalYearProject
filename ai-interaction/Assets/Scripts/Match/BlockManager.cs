using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager
{
    public Block[] blocks;
    public List<Block> outBoundBlocks = new List<Block>();

    private readonly Board board; // temporary control should not have the board attribute
    private readonly int rowSize;
    private readonly int colSize;

    public BlockManager(Board board, int rowSize, int colSize)
    {
        this.board = board;
        this.rowSize = rowSize;
        this.colSize = colSize;
        blocks = new Block[rowSize * colSize];
    }

    public void Clear()
    {
        blocks = new Block[rowSize * colSize];
        outBoundBlocks = new List<Block>();
        /*
        for (int i = blocks.Length - 1; i >= 0; i--) // clear the board from the top
        {
            if (blocks[i] == null) continue;
            var block = blocks[i];
            board.ClearBlock();
            board.DestroyBlock(block);
            blocks[i] = null;
        } */
    } 

    public int GetBlockIndexAt(int row, int col)
    {
        return row * colSize + col;
    }

    public Block[] GetBlocksList() // <= shadow copy
    {
        var blocksClone = new Block[blocks.Length]; 
        for (int i = 0; i < blocks.Length; i++)
        {
            blocksClone[i] = blocks[i];
        }
        return blocksClone;
    }

    public List<int> GetNeighborsAt(int index)
    {
        List<int> neighbors = new List<int>();
        if (!AtLeftBound(index) && blocks[index - 1] != null)
            neighbors.Add(index - 1);
        if (!AtRightBound(index) && blocks[index + 1] != null)
            neighbors.Add(index + 1);
        if (!AtTopBound(index) && blocks[index + colSize] != null)
            neighbors.Add(index + colSize);
        if (!AtBottomBound(index) && blocks[index - colSize] != null)
            neighbors.Add(index - colSize);
    
        return neighbors;
    }

    public bool AtLeftBound(int index) => index % colSize == 0;
    public bool AtRightBound(int index) => index % colSize == colSize- 1;
    public bool AtTopBound(int index) => index + colSize >= colSize * rowSize;
    public bool AtBottomBound(int index) => index - colSize < 0;
    

    public int GetCurrentLevel()
    {
        int index = blocks.Length - 1; // trace back from the top-right one by one
        while (index >= 0 && blocks[index] == null)
        {
            index = index - 1;
        }
        return (int)(index / colSize) + 1;
    }

    public float GetSmoothness()
    {
        int checkThreshold = (GetCurrentLevel() - 1) * colSize;
        if (checkThreshold <= 0)
            return 1f;
        int count = 0; // count of non-hole cells
        for (int i = 0; i < checkThreshold; i++)
        {
            if(blocks[i] != null)
                count++;
        }
        return (float)count / checkThreshold;
    }

    #region Block Controls

    private void AllBlocksMoveUp(int degreeOfRow)
    {
        for (int i = rowSize - 1; i >= 0; i--) // for each row from the top
        {
            for (int j = 0; j < colSize; j++) // for each column
            {
                int index = GetBlockIndexAt(i, j);
                if (blocks[index] == null) 
                    continue;

                int newIndex = GetBlockIndexAt(i + degreeOfRow, j);
                if (newIndex >= blocks.Length) // destroy the block out of bound after move
                {
                    RemoveOutBoundBlock(index);
                    continue;
                }

                if (blocks[newIndex] == null) // won't out of bound
                    TranslateBlock(index, newIndex);
            }
        }
    }

    public void AllBlocksMoveUpStartFrom(int index)
    {
        int start = index;
        while (index < blocks.Length && blocks[index] != null) // check the top empty space
        {
            index += colSize;
        }

        if (index >= blocks.Length) // if exceed the top bound
        {
            index -= colSize;
            RemoveOutBoundBlock(index);
        }
        
        while (index > start) // try to move all blocks up to create an empty space under
        {
            TranslateBlock(index - colSize, index);
            index -= colSize;
        }
    }

    public Queue<int> PlacePiece(Block[] blocks, Vector3Int localPos, Vector3Int[] route)
    {
        var matchSeq = new Queue<int>();
        for (int i = 0; i < route.Length; i++) 
        {
            Vector3Int blockPos = localPos + route[i];
            int index = this.PlaceBlock(blocks[i], blockPos);
            matchSeq.Enqueue(index);
        } 
        return matchSeq;
    }

    public int PlaceBlock(Block block, Vector3Int localPosition)
    {
        int index = GetBlockIndexAt(localPosition.z, localPosition.x);

        if (board && block != null)
        {
            block.transform.SetParent(board.blocks.transform);
            board.AddBlock();
        }

        blocks[index] = block;

        return index;
    }

    public int PlaceBlock(Block block, int row, int col)
    {
        int index = GetBlockIndexAt(row, col);

        if (board)
        {
            block.transform.SetParent(board.blocks.transform);
            block.index = index;
            board.AddBlock();
        }

        blocks[index] = block;

        return index;
    }

    public void RemoveMatchBlock(int index)
    {
        if (blocks[index].type == BlockType.monster)
        {
            // MonsterBlock m = blocks[index] as MonsterBlock;
            // if (m != null && m.IMMUNITY == false)
            // {
            if (board)
            {
                board.activePiece.ClearMonster();
                board.DestroyBlock(blocks[index]);
            }
            blocks[index] = null;
            // }
        }
        else
        {
            if (board)
                board.DestroyBlock(blocks[index]);
            blocks[index] = null;
        }  
    }

    public void RemoveOutBoundBlock(int index)
    {
        if (board)
        {
            if (blocks[index].type == BlockType.monster)
                board.numberOfMonsters--;
            board.DestroyBlock(blocks[index]);

            var block = blocks[index];
            outBoundBlocks.Add(block);
        }
        blocks[index] = null;
    }

    public void RemoveBlock(int index)
    {
        //if (blocks[index] == null)
        //    return;
        if (board)
        {
            if (blocks[index].type == BlockType.monster)
                board.numberOfMonsters--;
            board.DestroyBlock(blocks[index]);
        }
        blocks[index] = null;
    }

    public void TranslateBlock(int startIndex, int endIndex)
    {
        if (startIndex == endIndex)
            return;
        
        var block = blocks[startIndex];

        if (board)
        {
            block.transform.Translate(new Vector3((endIndex-startIndex)%colSize, 0, (endIndex-startIndex)/colSize));
            block.index = endIndex;
        }

        blocks[startIndex] = null;
        blocks[endIndex] = block;
        
    }

    public void DropBlockAround(int index, ref Queue<int> variedBlocks)
    {
        int upperIndex = -1; // -1: not exists
        upperIndex = index + colSize;
        int leftIndex = -1;
        if (index % colSize != 0)
            leftIndex = index - 1;
        int rightIndex = -1;
        if (index % colSize != (colSize - 1))
            rightIndex = index + 1;

        if (upperIndex != -1)
            DropBlock(upperIndex, ref variedBlocks);
        if (leftIndex != -1)
            DropBlock(leftIndex, ref variedBlocks);
        if (rightIndex != -1)
            DropBlock(rightIndex, ref variedBlocks);
    }

    private void DropBlock(int index, ref Queue<int> variedBlocks)
    {
        while (index < blocks.Length && blocks[index] != null) // for each block
        {
            int newIndex = index;
            while (newIndex - colSize >= 0 && blocks[newIndex - colSize] == null)
            {
                newIndex -= colSize;
            }

            TranslateBlock(index, newIndex);

            variedBlocks.Enqueue(newIndex);

            index += colSize; // check the next upper block
        }
    }

    #endregion

    #region Match Evaluator

    public MatchEvaluation GetMatchEvaluationAt(PieceAgent piece, Vector3Int pos, Vector3Int[] route)
    {
        // Create a block manager
        if (piece == null || piece.trapBlocks.Length == 0) 
            return MatchEvaluation.GetEmpty();

        // Before match
        this.blocks = piece.board.blockManager.GetBlocksList();
        float prevLevel = GetCurrentLevel(); 

        // Create a match sequence
        var matchSeq = PlacePiece(piece.trapBlocks, pos, route);
        var matchedList = Match(matchSeq);

        // After match
        int n = matchedList[matchedList.Count - 1]; // n: number of eliminated monsters
        matchedList.RemoveAt(matchedList.Count - 1);

        return new MatchEvaluation
        {
            _matchedList = matchedList,
            _hasMatch = HasAMatch(matchedList),
            _matchNumber = GetNumberOfMatch(matchedList),
            _eliminatedMonsterNumber = n,
            _smoothness = GetSmoothness(),
            _levelReduction = (prevLevel - GetCurrentLevel()) / rowSize,
        };
    }

    public int GetNumberOfMatch(List<int> matches)
    {
        if (matches == null || matches.Count == 0) 
            return 0;
        int n = 0;
        foreach (var match in matches)
        {
            n += match;
        }
        return n;
    }

    public bool HasAMatch(List<int> matches)
    {
        if (matches == null || matches.Count == 0) 
            return false;
        if (matches[0] >= 5)
            return true;
        else
            return false;
    }

    public List<int> Match(Queue<int> matchSeq, List<int> matches = null, int numberOfEliminatedMonsters = 0, int count = 0)
    {
        if (matches == null)
            matches = new List<int>();

        if (matchSeq.Count == 0 || count >= 5) 
        {
            matches.Add(numberOfEliminatedMonsters); // matches last item contains the number of elimainated monsters
            return matches;
        }

        if (this.blocks == null) 
        {
            matches.Add(numberOfEliminatedMonsters); 
            return matches;
        }

        var nextMatchSeq = new Queue<int>();
        var matchedBlocks = new List<int>();
        while (matchSeq.Count > 0)
        {
            int index = matchSeq.Dequeue();
            if (this.blocks[index] == null) continue;

            int colour = this.blocks[index].colour;
            List<int> match = MatchSearch(index, colour); 
            // skip the block that has checked for a match already
            matchSeq = new Queue<int>(matchSeq.Where(x => !match.Contains(x)));
            
            if (match.Count > 2)
                matches.Add(match.Count);

            if (match.Count >= 5) 
            {
                foreach (var blockIndex in match)
                {
                    RemoveMatchBlock(blockIndex);
                    matchedBlocks.Add(blockIndex);
                }
            }
        }
        foreach (var blockIndex in matchedBlocks)
        {  
            DropBlockAround(blockIndex, ref nextMatchSeq);
        }

        return Match(nextMatchSeq, matches, numberOfEliminatedMonsters, count + 1);
    }
    
    public List<int> MatchSearch(int startIndex, int colour) // search by BFS
    {
        List<int> matchBlocks = new List<int>();
        Queue<int> searchBlocks = new Queue<int>();
        
        matchBlocks.Add(startIndex);
        searchBlocks.Enqueue(startIndex);

        while (searchBlocks.Count > 0)
        {
            int index = searchBlocks.Dequeue();
            
            int searchIndex;

            searchIndex = index + colSize;
            if (searchIndex < blocks.Length && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            if (index % colSize != (colSize - 1))
            {
                searchIndex = index + 1;
                if (searchIndex < blocks.Length && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
                {
                    if (blocks[searchIndex].colour == colour)
                    {
                        matchBlocks.Add(searchIndex);
                        searchBlocks.Enqueue(searchIndex);
                    }
                }
            }

            searchIndex = index - colSize;
            if (searchIndex >= 0 && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
            {
                if (blocks[searchIndex].colour == colour)
                {
                    matchBlocks.Add(searchIndex);
                    searchBlocks.Enqueue(searchIndex);
                }
            }

            if (index % colSize != 0)
            {
                searchIndex = index - 1;
                if (searchIndex >= 0 && blocks[searchIndex] && !matchBlocks.Contains(searchIndex))
                {
                    if (blocks[searchIndex].colour == colour)
                    {
                        matchBlocks.Add(searchIndex);
                        searchBlocks.Enqueue(searchIndex);
                    }
                }
            }


        }

        return matchBlocks;
    }

    #endregion
}


