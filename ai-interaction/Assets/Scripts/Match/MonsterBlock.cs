using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonsterBlock : Block
{
    /*
    private bool immunity = false; // give some effect later
    public bool IMMUNITY{
        get { return immunity; }
        set {
            immunity = value;
            if (countTxt)
            {
                if (immunity)
                    countTxt.color = new Color(255, 164, 0, 255);
                else
                    countTxt.color = new Color(255, 255, 255, 255);
            }
        }
    } */
    /*
    private int hp = 1;
    public int HP{
        get { return hp;}
        set {
            hp = value;
            if (hp <= 0)
                Destroy(gameObject);
        }
    } */

    /*
    private int turn = 3;
    public int TURN{
        get { return turn; }
        set {
            turn = value;
            if (turn <= 0)
            {
                turn = Random.Range(6, 9);
                if (actionToPerform != null)
                    actionToPerform();
            }
            if (countTxt)
                countTxt.text = turn + "";
        }
    }
    [SerializeField] TMP_Text countTxt;

    public delegate void PerformAction();
    public event PerformAction actionToPerform;

    protected override void Awake()
    {
        base.Awake();
        if (type == BlockType.monster)
        {
            turn = Random.Range(6, 9);
            if (countTxt)
                countTxt.text = turn + "";

            switch (this.colour)
            {
                case 0: // blue
                    actionToPerform += Shield;
                    break;
                case 1:
                    actionToPerform += Spawn;
                    break;
                case 2:
                    actionToPerform += Immune;
                    break;
                default:
                    break;
            }
        }
    }

    public void Shield() // construct a shield in front of it
    {
        // print("Open Shield At: " + index);
        int n = Random.Range(0, 3);
        if (n > 0)
            return;
        if (index + board.boardWidth < board.boardWidth * board.boardDepth)
        {
            int colour = Random.Range(0, 3);
            board.SpawnBlockAt(index + board.boardWidth, colour);
        }
    }

    public void Immune() // surrounding immunity one turn
    {
        // print("Immune Surroundings At: " + index);
        List<int> neighbors = board.blockManager.GetNeighborsAt(index);
        foreach (var n in neighbors)
        {
            // downcasting
            MonsterBlock m = board.blockManager.blocks[n] as MonsterBlock;
            if (m != null)
                m.IMMUNITY = true;
        }
    }

    public void Spawn() // spawn at the diagonals
    {
        // Debug.Log("Spawn At: " + index);
        int n = Random.Range(0, 3);
        if (n > 0)
            return;
        if (!board.blockManager.AtLeftBound(index)) 
        {
            board.SpawnMonsterAt(index - 1, this.colour);
        }
        if (!board.blockManager.AtRightBound(index))
        {
            board.SpawnMonsterAt(index + 1, this.colour);
        }
        
        board.blockManager.RemoveBlock(index);
    }
    */
}
