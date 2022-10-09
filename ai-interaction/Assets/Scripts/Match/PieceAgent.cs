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
    // Decisions
    private MatchController input;
    // public bool disableInputCollectionInHeuristicCallback;
    private bool m_IsDecisionStep;
    private bool actionEnabled = true;
    private int m_Move;
    private int m_Rotate; 
    public bool EnableMoveDown = true;
    private bool m_MoveDown;

    public BufferSensorComponent m_TrapBuffer;
    public BufferSensorComponent m_MatchBuffer;
    public Board board;
    public Trap trapData { get; private set; }
    public int rotationIndex { get; private set; }
    public Block[] trapBlockPrefab;
    public Block[] trapBlocks;
    public Vector3Int[] route { get; private set; }
    public Ghost ghost;

    // Initial Status
    public Vector3Int spawnPosition; // local position relative to the board
    public float dropSpeed = 1f;

    public List<int> matches {get; set;}
    public int numberOfMatches {get; set;}

    EnvironmentParameters m_ResetParams;
    public int DefaultSpawningRange;
    public int m_SpawningRange {get; set;} // input the number of rows

    public override void Initialize()
    {
        input = GetComponent<MatchController>();

        var buffer = GetComponents<BufferSensorComponent>();
        m_TrapBuffer = buffer[0];
        m_MatchBuffer = buffer[1];

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
        if (level > board.boardDepth - 2)
            AddReward(-0.3f);
        AddReward((board.boardDepth - level)/board.boardDepth);
        if (board.numberOfMonsters > 0)
            AddReward(1/board.numberOfMonsters);
        if (board.numberOfBlocks > 0)
            AddReward(1/board.numberOfBlocks);
    }

    public void HasAMatch()
    {
        AddReward(0.3f);
    }

    public void LinkBlock()
    {
        AddReward(0.1f);
    }

    public void ClearMonster()
    {
        AddReward(0.5f);
    }

    private void ResetTrap()
    {
        this.transform.position = spawnPosition + board.transform.position;
        foreach (var block in trapBlocks)
            block.owner = null;
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
                block.owner = this;

                pos = route[i+1] + this.transform.position; // route[i]: offset from start position
                block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i+1] = block;
                block.transform.SetParent(this.transform);
                block.owner = this;
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
        {
            matchSeq.Enqueue(block.index);
            block.EnableDetector(true);
        }

        StartCoroutine(this.board.PrepareForMatch(matchSeq));  
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(ghost.transform.localPosition.z);
        sensor.AddObservation(board.GetCurrentLevel());
        
        sensor.AddObservation(board.numberOfMonsters);
        sensor.AddObservation(board.numberOfBlocks); 

        sensor.AddObservation(rotationIndex);
        sensor.AddOneHotObservation((int)trapData.shape, 7); // 7 represents the size of trap type enum
        if (trapBlocks != null)
        {
            for (int i = 0; i < trapBlocks.Length; i++) 
            {
                if (trapBlocks[i])
                    m_TrapBuffer.AppendObservation(GetTrapBlockData(i));
            }  
        }

        if (matches != null)
        {
            foreach (var match in matches)
            {
                float[] a = new float[1];
                a[0] = match;
                m_MatchBuffer.AppendObservation(a);
            }
        }
        sensor.AddObservation(numberOfMatches);
        sensor.AddObservation(ghost.HasAMatch(this.matches));
        
        sensor.AddObservation(this.transform.localPosition.z - ghost.transform.localPosition.z); // when it will be placed

        //sensor.AddObservation(this.transform.position - goal.transform.position);
    }

    public float[] GetTrapBlockData(int i)
    {
        var blockData = new float[3];
        blockData[0] = trapBlocks[i].colour;
        blockData[1] = this.transform.localPosition.x + route[i].x;
        blockData[2] = this.transform.localPosition.z + route[i].z;

        return blockData;
    }

    private int m_AgentStepCount; // current agent step
    private void FixedUpdate()
    {
        if (StepCount % 5 == 0)
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        m_Move = (int)discreteActions[0];
        m_Rotate = (int)discreteActions[1];
        m_MoveDown = (int)discreteActions[2] > 0;

        if (m_IsDecisionStep && actionEnabled)
        {
            m_IsDecisionStep = false;
            if (m_Move == 1)
            {
                Move(Vector3Int.left);
                ghost.UpdatePos();
            }
            else if (m_Move == 2)
            {
                Move(Vector3Int.right);
                ghost.UpdatePos();
            }
            // else no any movement

            if (m_Rotate == 1)
            {
                Rotate(1);
                ghost.UpdatePos();
            }
            else if (m_Rotate == 2)
            {
                Rotate(-1);
                ghost.UpdatePos();
            }
            // else no any rotation
        }

        if (m_MoveDown && EnableMoveDown)
        {
            Move(-Vector3Int.forward);
        }
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

    private void Rotate(int direction)
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
                    x = Mathf.CeilToInt(cell.x * TrapData.cos_90 + cell.z * TrapData.sin_90 * direction);
                    z = Mathf.CeilToInt(cell.x * -TrapData.sin_90 * direction + cell.z * TrapData.cos_90);
                    break;
                default:
                    x = Mathf.RoundToInt(cell.x * TrapData.cos_90 + cell.z * TrapData.sin_90 * direction);
                    z = Mathf.RoundToInt(cell.x * -TrapData.sin_90 * direction + cell.z * TrapData.cos_90);
                    break;
            }

            newRoute[i] = new Vector3Int(x, 0, z);
        }

        if (!TestWallKicks(this.rotationIndex, direction, newRoute)) // check rotate valid
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

    private bool TestWallKicks(int rotationIndex, int rotationDirection, Vector3Int[] newRoute)
    {
        int wallKickIndex = rotationIndex * 2;
        if (rotationDirection < 0)
            wallKickIndex--;
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

        if (input.MoveLeft())
            discreteActionsOut[0] = 1;
        else if (input.MoveRight())
            discreteActionsOut[0] = 2;
        else 
            discreteActionsOut[0] = 0;

        if (input.RotateAnticlockwise())
            discreteActionsOut[1] = 1;
        else if (input.RotateClockwise())
            discreteActionsOut[1] = 2;
        else 
            discreteActionsOut[1] = 0;
        
        discreteActionsOut[2] = input.MoveDown() ? 1 : 0;
    }

}
