using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Piece : MonoBehaviour // data of the current active piece
{
    public Board board { get; private set; }
    public Trap trapData { get; private set; }
    public GameObject[] trapBlockPrefab;
    public GameObject[] trapBlocks;
    public Vector3Int[] route { get; private set; }

    public GameObject ghost { get; private set; }
    public GameObject ghostBlockPrefab;
    public GameObject[] ghostBlocks { get; private set; }

    public InputAction moveLeft;
    public InputAction moveRight;
    public InputAction rotate;
    public int rotationIndex { get; private set; }

    private GameObject boardManager;


    private void OnEnable()
    {
        moveLeft.Enable();
        moveRight.Enable();
        rotate.Enable();
    }

    private void OnDisable() // disable when piece is placed
    {
        moveLeft.Disable();
        moveRight.Disable();
        rotate.Disable();
    }

    private void Start()
    {
        boardManager = GameObject.Find("BoardManager");
        StartCoroutine(Push());
    }

    private IEnumerator Push()
    {
        do 
        {
            yield return new WaitForSeconds(0.5f);
        }
        while (Move(-Vector3Int.forward)); // drop: moving back

        Place();

        this.board.ContinueToNextPiece();
    }

    private void Place()  // and occupy
    {
        OnDisable();
        for (int i = 0; i < route.Length; i++) 
        {
            this.board.Occupy(this, i); 
        }
        for (int i = 0 ; i < trapBlocks.Length ; i++)
        {
            if (trapBlocks[i] && trapBlocks[i].TryGetComponent<Block>(out Block block))
                this.board.Match(block.index);
        }

        Destroy(ghost);
        
        if (!boardManager)
        {
            print("Missing parent");
            return;
        }
        for (int i = 0 ; i < trapBlocks.Length ; i++)
        {
            trapBlocks[i].transform.SetParent(boardManager.transform);
        }
        Destroy(this.gameObject);
    }

    public bool IsEmptyPiece()
    {
        for (int i = 0 ; i < trapBlocks.Length ; i++)
        {
            if (trapBlocks[i] != null)
                return false;
        }
        return true;
    }

    public void InstantiateGhost()
    {
        Vector3Int pos = FindBottom();

        ghost = new GameObject("Ghost");
        ghost.transform.position = pos;
        ghostBlocks = new GameObject[trapData.route.Length];
        for (int i = 0; i < route.Length; i++) // Instantiate the ghost
        {
            Vector3Int newPos = route[i] + pos;
            var block = Instantiate(ghostBlockPrefab, (Vector3)newPos, Quaternion.identity);
            ghostBlocks[i] = block;
            block.transform.SetParent(ghost.transform);
        }
    }

    private void UpdateGhost() // for every manual move
    {
        Vector3Int pos = FindBottom();
        ghost.transform.position = pos;
    }

    private Vector3Int FindBottom()
    {
        Vector3Int ghostPos = Vector3Int.FloorToInt(transform.position);
        do
        {
            ghostPos -= Vector3Int.forward;
        }
        while (board.IsValidPosition(this, ghostPos, this.route));

        ghostPos += Vector3Int.forward;
        return ghostPos;
    }

    public void Initialize(Board board, Vector3Int startPos, Trap trapData)
    {
        this.board = board;
        this.trapData = trapData;

        trapBlocks = new GameObject[trapData.route.Length];

        if (this.route == null)
        {
            this.route = new Vector3Int[trapData.route.Length];
        }
        for (int i = 0; i < trapData.route.Length; i++)
        {
            this.route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        if (!this.board.IsValidPosition(this, startPos, this.route))
        {
            this.board.GameOver();
            Destroy(this.gameObject);
            return;
        }

        for (int i = 0; i < route.Length; i++)
        {
            Vector3Int pos = route[i] + startPos; // route[i]: offset from start position
            int n = Random.Range(0, trapBlockPrefab.Length);
            var block = Instantiate(trapBlockPrefab[n], (Vector3)pos, Quaternion.identity);
            trapBlocks[i] = block;
            block.transform.SetParent(this.transform);
        }

        this.rotationIndex = 0;

        InstantiateGhost();
    }

    private bool Move(Vector3Int translation)
    {
        Vector3 newPos = this.transform.position + translation; // get new position
        if (board.IsValidPosition(this, newPos, this.route)) // check position valid
        {
            this.transform.position = newPos; // move
            for (int i = 0; i < route.Length; i++) 
            {
                Vector3Int pos = route[i] + Vector3Int.FloorToInt(newPos);
            }  
            return true; 
        }
        
        return false;
        
    }

    private void Rotate()
    {
        int originalRotationIndex = this.rotationIndex;
        this.rotationIndex = Wrap(this.rotationIndex + 1, 0, 4);
        
        var newRoute = new Vector3Int[trapData.route.Length]; // get new route
        for (int i = 0; i < route.Length; i++) 
        {
            Vector3 cell = this.route[i];

            int x, z;
            switch (this.trapData.shape) // apply rotation matrix
            {
                case TrapShape.I:
                case TrapShape.O:
                    cell.x -= 0.5f;
                    cell.z -= 0.5f;
                    x = Mathf.CeilToInt(cell.x * TrapData.cos_90  + cell.z * TrapData.sin_90);
                    z = Mathf.CeilToInt(cell.x * -TrapData.sin_90 + cell.z * TrapData.cos_90);
                    break;
                default:
                    x = Mathf.RoundToInt(cell.x * TrapData.cos_90  + cell.z * TrapData.sin_90);
                    z = Mathf.RoundToInt(cell.x * -TrapData.sin_90 + cell.z * TrapData.cos_90);
                    break;
            }

            newRoute[i] = new Vector3Int(x, 0, z);
        }

        if (!TestWallKicks(this.rotationIndex, newRoute)) // check rotate valid
        {
            this.rotationIndex = originalRotationIndex;
        }
        else
        {
            for (int i = 0; i < route.Length; i++) // rotate
            {
                route[i] = newRoute[i];
                this.trapBlocks[i].transform.localPosition = newRoute[i];
                this.ghostBlocks[i].transform.localPosition = newRoute[i];
            }
        }
    }

    private bool TestWallKicks(int rotationIndex, Vector3Int[] newRoute)
    {
        int wallKickIndex = rotationIndex * 2;
        wallKickIndex = Wrap(wallKickIndex, 0, this.trapData.wallKicks.GetLength(0));

        for (int i = 0; i < this.trapData.wallKicks.GetLength(1); i++)
        {
            Vector2Int wallKick = this.trapData.wallKicks[wallKickIndex, i];
            Vector3Int translation = new Vector3Int(wallKick.x, 0, wallKick.y);
            Vector3 newPos = this.transform.position + translation;
            if (board.IsValidPosition(this, newPos, newRoute)) // check valid with the new route
            {
                this.transform.position = newPos;
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (moveLeft.triggered)
        {
            Move(Vector3Int.left);
            UpdateGhost();
        }
        else if (moveRight.triggered)
        {
            Move(Vector3Int.right);
            UpdateGhost();
        }

        if (rotate.triggered)
        {
            Rotate();
            UpdateGhost();
        }
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
            return max - (min - input) % (max-min);
        else
            return min + (input - min) % (max-min);
    }

}
