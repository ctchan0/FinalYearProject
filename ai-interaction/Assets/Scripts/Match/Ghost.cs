using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    public Block ghostBlockPrefab;
    public Block[] ghostBlocks;

    private PieceAgent piece;

    // private int prevMatch = 0;
    // private float prevPosition = 0;
    // public bool hasMatch { get; set;}
    // public bool prevHasMatch { get; set;}
    // public int numberOfRightMatches { get; set; }
    // public bool hasRightMatch { get; set; }
    // public int numberOfLeftMatches { get; set; }
    // public bool hasLeftMatch { get; set; }

    private void Awake()
    {
        
    }

    public void Initialize(PieceAgent piece)
    {
        this.piece = piece;

        this.transform.position = piece.transform.position;

        // Instantiate the ghost
        ghostBlocks = new Block[piece.trapData.route.Length];
        for (int i = 0; i < piece.route.Length; i++) 
        {
            Vector3 newPos = piece.route[i] + this.transform.position;
            Block block = Instantiate(ghostBlockPrefab, newPos, Quaternion.identity);
            block.colour = piece.trapBlocks[i].GetComponent<Block>().colour; // assign the colour
            ghostBlocks[i] = block;
            block.transform.SetParent(this.transform);
        }

        // this.prevMatch = 0;
        // this.prevPosition = piece.board.boardDepth;
        // this.hasMatch = false;
        // this.prevHasMatch = false;
        // this.numberOfLeftMatches = 0; 
        // this.numberOfRightMatches = 0;

        UpdatePos();
    }

    public void UpdatePos() // always update position and inspect surroundings
    {
        if (piece == null || piece.board.gameOver) return;
        

        Vector3Int centerPos = piece.FindBottomFrom((int)piece.transform.localPosition.x, piece.route);
        this.transform.localPosition = centerPos;
        // Vector3Int rightPos = piece.FindBottomFrom(1);
        // Vector3Int leftPos = piece.FindBottomFrom(-1);
        // Debug.Log("Center position: " + centerPos.z); (ticked)
        // Debug.Log("Right position: " + rightPos.z); (ticked)
        // Debug.Log("Left position: " + leftPos.z); (ticked)
    
        // piece.AddReward((this.prevPosition - this.transform.localPosition.z) / piece.board.boardDepth);
        // this.prevPosition = this.transform.localPosition.z;


        // piece.matches = GetMatchesAt(centerPos, piece.route);
        // piece.matches.RemoveAt(piece.matches.Count - 1);
        // piece.numberOfMatches = GetNumberOfMatch(piece.matches);
        // var rigthMatches = GetMatchesAt(rightPos);
        // numberOfRightMatches = GetNumberOfMatch(rigthMatches);
        // hasRightMatch = HasAMatch(rigthMatches);
        // var leftMatches = GetMatchesAt(leftPos);
        // numberOfLeftMatches = GetNumberOfMatch(leftMatches);
        // hasLeftMatch = HasAMatch(leftMatches);
        // Debug.Log("Center match: " + piece.numberOfMatches); (ticked)
        // Debug.Log("Rigth match: " + numberOfRightMatches); (ticked)
        // Debug.Log("Left match: " + numberOfLeftMatches); (ticked)
        
        //print("MatchList:");
        //foreach (var match in piece.matches)
        //{
        //   Debug.Log(match);
        //}
        // prevHasMatch = hasMatch;
        // if (HasAMatch(piece.matches))
        //{
        //    hasMatch = true;
        //}
        //else
        //{
        //    if (prevHasMatch == true) // if has match before, but not notice -> heavy penalty
        //    {
                // piece.AddReward(-1f);
                // print("High penalty");
        //    } 
        //    hasMatch = false;
        //}

        // float boardSize = piece.board.boardDepth * piece.board.boardWidth;
        // piece.AddReward((piece.numberOfMatches - this.prevMatch) / boardSize);
        
        // this.prevMatch = piece.numberOfMatches;
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
    }

}
