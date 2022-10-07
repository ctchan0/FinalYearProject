using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Random = UnityEngine.Random;


public class PieceAgent : Agent 
{
    private MatchController input;
    // public bool disableInputCollectionInHeuristicCallback;
    private bool actionCommand = true;
    private bool actionEnabled = true;
    private bool m_MoveLeft;
    private bool m_MoveRight;
    private bool m_Rotate; 
    private bool m_MoveDown;

    public Board board;
    public Trap trapData { get; private set; }

    public Vector3Int spawnPosition; // local position relative to the board
    public float dropSpeed = 1f;

    public Block[] trapBlockPrefab;
    public Block[] trapBlocks;
    public Vector3Int[] route { get; private set; }

    public Ghost ghost;

    public int rotationIndex { get; private set; }

    public int match { get; set; } // number of current match if push to the end

    public bool EnableMoveDown = true;

    public BufferSensorComponent m_AgentBuffer;

    EnvironmentParameters m_ResetParams;
    public int DefaultSpawningRange;
    public int m_SpawningRange {get; set;} // input the number of rows

    public override void Initialize()
    {
        input = GetComponent<MatchController>();

        m_AgentBuffer = GetComponent<BufferSensorComponent>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        m_SpawningRange = (int)m_ResetParams.GetWithDefault("spawn_range", DefaultSpawningRange);
        board.SpawnMonster(m_SpawningRange);

        SetNewTrap();
    }

    public void SetNewTrap()
    {
        EvaluateMove();
        ResetTrap();
        trapData =  board.GetRandomTrap();
        CreateTrap();
    }

    // Reward System
    public void EvaluateMove()
    {
        // Debug.Log("Current Level: " + board.GetCurrentLevel());
        // Debug.Log("Number of Monsters: " + board.numberOfMonsters);
        // Debug.Log("Total number of blocks: " + board.numberOfBlocks);

        int level = board.GetCurrentLevel();
        if (level > board.boardDepth - 3)
            AddReward(-0.3f);
        AddReward((board.boardDepth - level)/board.boardDepth);
        if (board.numberOfMonsters > 0)
            AddReward(1/board.numberOfMonsters);
        if (board.numberOfBlocks > 0)
            AddReward(1/board.numberOfBlocks);
    }

    private void ResetTrap()
    {
        this.transform.position = spawnPosition + board.transform.position;
        this.trapBlocks = null;
        this.route = null;
    }

    public void CreateTrap()
    {
        this.trapBlocks = new Block[trapData.route.Length];
        this.route = new Vector3Int[trapData.route.Length];

        for (int i = 0; i < trapData.route.Length; i++) 
        {
            this.route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        if (!this.board.IsValidPosition(this, this.transform.position, this.route))
        {
            this.board.GameOver(false);  //  Game Over !!!!!
        }
        else if (board.numberOfMonsters == 0)
        {
            this.board.GameOver(true);
        }
        else
        {
            AddReward(0.1f);
            
            for (int i = 0; i < route.Length; i+=2)
            {
                int n = Random.Range(0, trapBlockPrefab.Length);

                Vector3 pos = route[i] + this.transform.position; // route[i]: offset from start position
                var block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i] = block;
                block.transform.SetParent(this.transform);

                pos = route[i+1] + this.transform.position; // route[i]: offset from start position
                block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i+1] = block;
                block.transform.SetParent(this.transform);
            }

            this.rotationIndex = 0;

            ghost.Initialize(this);

            StartCoroutine(Push());

            actionEnabled = true;
        }
    }

    private IEnumerator Push()
    {
        do 
        {
            yield return new WaitForSeconds(dropSpeed);
        }
        while (Move(-Vector3Int.forward) && !board.gameOver); // drop: moving back

        Place(); 
    }
    private void Place()  // and occupy
    {
        actionEnabled = false;

        ghost.Reset(); // reset the ghost before occupy

        this.board.Occupy(this); 
        
        var matchSeq = new Queue<int>();
        foreach (var block in trapBlocks) 
            matchSeq.Enqueue(block.index);

        StartCoroutine(this.board.PrepareForMatch(matchSeq));  
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(match);
        sensor.AddObservation(board.GetCurrentLevel());
        
        sensor.AddObservation(board.numberOfMonsters);
        sensor.AddObservation(board.numberOfBlocks); 

        sensor.AddObservation(actionCommand);
        sensor.AddObservation(m_MoveLeft);
        sensor.AddObservation(m_MoveRight);
        sensor.AddObservation(m_Rotate);
        sensor.AddObservation(m_MoveDown);

        if (trapBlocks != null)
        {
            for (int i = 0; i < trapBlocks.Length; i++) // make it dynamic
            {
                if (trapBlocks[i])
                    m_AgentBuffer.AppendObservation(GetTrapBlockData(i));
            }  
        }
    }

    public float[] GetTrapBlockData(int i)
    {
        var blockData = new float[4];
        blockData[0] = this.transform.localPosition.x + trapBlocks[i].transform.localPosition.x;
        blockData[1] = ghost.transform.localPosition.x + trapBlocks[i].transform.localPosition.x;
        blockData[2] = this.transform.localPosition.z + trapBlocks[i].transform.localPosition.z;
        blockData[3] = ghost.transform.localPosition.z + trapBlocks[i].transform.localPosition.z;
        return blockData;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        m_MoveLeft = (int)discreteActions[0] > 0;
        m_MoveRight = (int)discreteActions[1] > 0;
        m_Rotate = (int)discreteActions[2] > 0;
        m_MoveDown = (int)discreteActions[3] > 0;

        if (actionCommand && actionEnabled)
        {
            if (m_MoveLeft)
            {
                Move(Vector3Int.left);
                ghost.UpdatePos();
            }
            if (m_MoveRight)
            {
                Move(Vector3Int.right);
                ghost.UpdatePos();
            }

            if (m_Rotate)
            {
                Rotate();
                ghost.UpdatePos();
            }

            StartCoroutine(CoolDown(0.1f));
        }

        if (m_MoveDown && EnableMoveDown)
        {
            Move(-Vector3Int.forward);
        }
    }

    private IEnumerator CoolDown(float coolDownTime)
    {
        actionCommand = false;
        yield return new WaitForSeconds(coolDownTime);
        actionCommand = true;
    }

    public Vector3Int FindBottom()
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
                ghost.ghostBlocks[i].transform.localPosition = newRoute[i];
            }
        }
    }

    private void FastPush() // playable only for human
    {
        this.transform.position = ghost.transform.position;
        Place();
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

    public int DistanceToGhost()
    {
        return (int)Vector3.Distance(this.transform.position, ghost.transform.position);
    }

    public bool CloseToGhost()
    {
        return DistanceToGhost() <= 2;
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
            return max - (min - input) % (max-min);
        else
            return min + (input - min) % (max-min);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = input.MoveLeft() ? 1 : 0;
        discreteActionsOut[1] = input.MoveRight() ? 1 : 0;
        discreteActionsOut[2] = input.Rotate() ? 1 : 0;
        discreteActionsOut[3] = input.MoveDown() ? 1 : 0;
    }

}
