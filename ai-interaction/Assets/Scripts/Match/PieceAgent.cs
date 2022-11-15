using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Random = UnityEngine.Random;

// The state diagram is
//                +-------+
//      ----------| Reset |<-----------
//      |         |       |           |
//      |         +-------+           |
//      v                             |
//  +-------+      +-------+      +-------+      +-------+
//  |Create | ---> |Push   | ---> |Match  | ---> |Drop   |
//  |       |      |       |      |       | <--- |       |
//  +-------+      +-------+      +-------+      +-------+
//


public struct Location
{
    public Vector3Int _pos;
    public Vector3Int[] _route;
    public int _rotIndex;
    public MatchEvaluation _score;

    public void Initialize(Vector3Int pos, Vector3Int[] route, int rotIndex, MatchEvaluation score)
    {
        _pos = pos;
        _route = route;
        _rotIndex = rotIndex;
        _score = score;
    }
}

public struct MatchEvaluation
{
    public List<int> _matchedList;
    public bool _hasMatch;
    public int _matchNumber;
    public int _eliminatedMonsterNumber;
    public float _smoothness;
    public float _levelReduction;
    public bool Exists => _matchedList != null;

    public static MatchEvaluation GetEmpty() // used without creating a new instance
    {
        return new MatchEvaluation
        {
            _matchedList = null,
            _hasMatch = false,
            _matchNumber = 0,
            _eliminatedMonsterNumber = 0,
            _smoothness = 0,
            _levelReduction = 0,
        };
    }

    public void PrintInformation()
    {
        Debug.Log("Score Assessment");
        Debug.Log("HasMatch: " + _hasMatch);
        Debug.Log("MatchNumber: " + _matchNumber);
        Debug.Log("ElimainedMonsterNumber: " + _eliminatedMonsterNumber);
        Debug.Log("Smoothness: " + _smoothness);
    }
}


public class PieceAgent : Agent 
{
    // Decisions & Controls
    private MatchController input;
    public bool humanPlayable = false;
    // public bool disableInputCollectionInHeuristicCallback;
    // private bool m_IsDecisionStep = false;
    // private bool actionEnabled = true;
    // private int m_Move;
    // private int m_Rotate; 
    // private bool validMove = true;

    // Trap & Ghost
    // private BufferSensorComponent m_TrapBuffer;
    // private BufferSensorComponent m_LocationBuffer;
    public Board board;
    public Trap trapData { get; private set; }
    public int rotationIndex;
    public Block[] trapBlockPrefab;
    public Block[] trapBlocks;
    public Vector3Int[] route { get; private set; }
    public Ghost ghost;

    // Initial Status
    public Vector3Int spawnPosition; // local position relative to the board
    public float dropSpeed = 1f;

    private List<Location> locations;
    private List<float> StatesOfLoc = new List<float>();
    // private int currentLoc;
    // public List<int> matches {get; set;}
    // public int numberOfMatches {get; set;}
    private float level; 
    private int totalMatch = 0;
    // public int matchColour { get; set; }

    EnvironmentParameters m_ResetParams;
    public int DefaultSpawningRange;
    public int m_SpawningRange {get; set;} // input the number of rows

    public int Turn = 0;
    // private bool canPlace = false;

    public List<Location> GetAllLocations(Trap trapData)
    {
        var locations = new List<Location>();
        var route = new Vector3Int[trapData.route.Length];
        for (int i = 0; i < trapData.route.Length; i++) 
        {
            route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        // print("All Possible Locations:");
        for (int rotIndex = 0;  rotIndex < trapData.route.Length; rotIndex++)
        {
            for (int col = 0; col < board.boardWidth; col++)
            {
                Vector3Int pos = FindBottomFrom(col, route);
                if (pos.z != -1) // if pos is valid
                {
                    MatchEvaluation score = new MatchEvaluation();
                    var scanner = new BlockManager(null, board.boardDepth, board.boardWidth);
                    score = scanner.GetMatchEvaluationAt(this, pos, route);
                    Location loc = new Location();
                    loc.Initialize(pos, route, rotIndex, score);
                    locations.Add(loc);
                    // Debug.Log("RotIndex " + rotIndex + ", Col " + col + ": " + pos);
                    // score.PrintInformation();
                    // if (match.Count != 0 && match[match.Count - 1] != 0)
                    //    Debug.Log("Number of elimanated monsters: " + match[match.Count - 1]);
                    // Debug.Log(ghost.HasAMatch(match));
                }
                else
                {
                    Location loc = new Location();
                    loc.Initialize(pos, route, rotIndex, MatchEvaluation.GetEmpty());
                    locations.Add(loc);
                }      
            }
            route = GetNewRoute(trapData, 1, route);
        }
        return locations;
    }

    private bool IsValidInput(int rotIndex, int col)
    {
        List<int> possibleCols = GetAllPossibleCols(locations, rotIndex);
        if (possibleCols.Contains(col))
            return true;
        else
            return false;
    }
    
    private List<int> GetAllPossibleCols(List<Location> locations, int rotIndex)
    {
        if (locations == null || locations.Count == 0)
            return null;
        List<int> cols = new List<int>();
        foreach (var loc in locations)
        {
            if (loc._rotIndex == rotIndex && loc._score.Exists)
            {
                cols.Add(loc._pos.x);
            }
        }
        return cols;
    }

    public override void Initialize()
    {
        input = GetComponent<MatchController>();

        var buffer = GetComponents<BufferSensorComponent>();
        // m_LocationBuffer = buffer[0];
        // m_TrapBuffer = buffer[1];

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        this.transform.localPosition =  spawnPosition;
    }

    public void Reset()
    {
        Turn = 0;
        totalMatch = 0;
        // matchColour = 0;
        // matches = null;
        // numberOfMatches = 0;
    }


    public override void OnEpisodeBegin()
    {
        m_SpawningRange = (int)m_ResetParams.GetWithDefault("spawn_range", DefaultSpawningRange);
        board.SpawnMonster(m_SpawningRange);

        SetNewTrap();
    }

    public void SetNewTrap()
    {
        if (Turn != 0)
        {
            // board.StartMonsterTurn();
            EvaluateMove();
            StartNewTurn();
        }
        else
        {
            StartNewTurn();
        }
    }

    public void StartNewTurn()
    {
        ResetTrap();
        trapData = board.GetRandomTrap();

        int state = GetConditionState();
        if (state == 0)
            CreateTrap();
        if (state == 1)
            this.board.GameOver(true);
        if (state == -1)
            this.board.GameOver(false);
    }

    public int GetConditionState()
    {
        this.route = new Vector3Int[trapData.route.Length];
        for (int i = 0; i < trapData.route.Length; i++) 
        {
            this.route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        // no space to put new trap
        if (!this.board.IsValidPos(Vector3Int.FloorToInt(this.transform.localPosition), this.route))
            return -1;
        else if (this.board.blockManager.outBoundBlocks.Count > 0)
            return -1;
        else if (board.numberOfMonsters == 0)
        {
            Debug.Log("Turns to win: " + Turn);
            return 1;
        }

        return 0;
    }

    private void MakeDecision()
    {
        StatesOfLoc = new List<float>();
        foreach (var loc in locations)
        {
            float[] state = GetStateFromLoc(loc._pos, loc._rotIndex, loc._score);
            StatesOfLoc.AddRange(state);
        }
        RequestDecision();
    }

    private float[] GetStateFromLoc(Vector3Int pos, int rotIndex, MatchEvaluation score)
    {
        // Should normalizes the value between -1 and 1
        var locData = new float[6];
        if (score.Exists)
        {
            int boardSize = board.boardDepth * board.boardWidth;
            locData[0] = score._hasMatch? 1 : 0; // whether has match or not
            locData[1] = (float)score._matchNumber / boardSize; // match number
            locData[2] = (float)score._eliminatedMonsterNumber / boardSize; 
            locData[3] = (float)(board.boardDepth - (pos.z + 1)) / board.boardDepth; // height of piece
            locData[4] = score._smoothness; // smoothness
            locData[5] = score._levelReduction;
        }
        else
        {
            locData[0] = -1;
            locData[1] = -1;
            locData[2] = -1;
            locData[3] = -1;
            locData[4] = -1;
            locData[5] = -1;
        }

        return locData;
    }

    private void MoveTrap(int rotIndex, int col) // decision output !!!!!!!
    {
        this.transform.localPosition = new Vector3(col, 0, this.transform.localPosition.z);
        this.rotationIndex = rotIndex;
        for (int i = 0; i < rotIndex; i++)
            Rotate(1);
        ghost.UpdatePos();
    }

    // Reward System
    public void EvaluateMove()
    {
        /* Position */
        level = board.blockManager.GetCurrentLevel();
        // if (level > (board.boardDepth - 3))
        //    AddReward(-1f);
        // Debug.Log("Level: " + -level / board.boardDepth); 
        AddReward(-level / board.boardDepth);
        // Debug.Log("Placed Position: " + (-this.transform.localPosition.z / board.boardDepth));
        // AddReward(-this.transform.localPosition.z / (board.boardDepth - 1));

        // float boardSize = board.boardDepth * board.boardWidth;
        // Debug.Log("Number of monsters:  " + (-board.numberOfMonsters / boardSize));
        // Debug.Log("Number of blocks:  " + (-board.numberOfBlocks / boardSize)); 
        //if (board.numberOfMonsters > 0)
        //    AddReward(-board.numberOfMonsters / boardSize);
        //if (board.numberOfBlocks > 0)
        //    AddReward(-board.numberOfBlocks / boardSize);
    }
    public void HasAMatch(int matchCount)
    {
        totalMatch += matchCount;
        AddReward(matchCount / board.boardWidth * board.boardDepth);
    }
    public void LinkUpBlock(int type)
    {
        // float boardSize = board.boardDepth * board.boardWidth;
        // AddReward(type / boardSize); 
    }
    public void ClearMonster()
    {
        board.numberOfMonsters--;
        AddReward(1f);
    }

    public void ConsumeOneTurn()
    {
        AddReward(-0.01f);
    }

    private void ResetTrap()
    {
        this.trapBlocks = null;
        this.route = null;
        this.transform.localPosition = this.spawnPosition;
    }

    public void CreateTrap()
    {
        // set up 
        this.trapBlocks = new Block[trapData.route.Length];
          
        ConsumeOneTurn();

        // Instantiate trap blocks
        for (int i = 0; i < route.Length; i+=2)
        {
            int n = Random.Range(0, trapBlockPrefab.Length);
            Vector3 pos = route[i] + this.transform.position; 
            var block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
            trapBlocks[i] = block;
            block.transform.SetParent(this.transform);
            // block.owner = this;
                        
            pos = route[i+1] + this.transform.position; 
            block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
            trapBlocks[i+1] = block;
            block.transform.SetParent(this.transform);
            // block.owner = this;  
        }

        this.rotationIndex = 0;

        ghost.Initialize(this);
        locations = GetAllLocations(trapData);

        // actionEnabled = true;
        MakeDecision();

        StartCoroutine(Push());
    }

    private IEnumerator Push()
    {
        yield return new WaitForSeconds(0.5f);
        // canPlace = false;
        do 
        {
            ghost.UpdatePos();
            // locations =  GetAllPossibleLoc();
            yield return new WaitForSeconds(dropSpeed);
        }
        while (Move(-Vector3Int.forward) && !board.gameOver); // drop: moving back

        Place();
    }

    /*
    private IEnumerator PlaceWithTimeCheck()  // and occupy
    {
        yield return new WaitForSeconds(0.5f); // time to assure the final position
        // canPlace = true;

        if ((int)ghost.transform.localPosition.z == (int)this.transform.localPosition.z)
        {
            StartCoroutine(Place);
        }
        else
        {
            StartCoroutine(Push());
        }
    } */

    private void Place()
    {
        // actionEnabled = false;
        // m_Move = 0;
        // m_Rotate = 0;
        ghost.Reset(); // reset the ghost before occupy

        var matchSeq = this.board.Occupy(this); // will enable the detector once occupied
        
        StartCoroutine(this.board.PrepareForMatch(matchSeq));  
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /* Board Status */
        // sensor.AddObservation(level / board.boardDepth);
        // float boardSize = board.boardDepth * board.boardWidth;
        // sensor.AddObservation(board.numberOfMonsters);
        // sensor.AddObservation(board.numberOfBlocks); 

        /* Piece */
        // decision
        // sensor.AddObservation(m_IsDecisionStep);
        // sensor.AddObservation(validMove);

        // location
        // sensor.AddObservation(this.transform.localPosition.x / (board.boardWidth - 1));
        // sensor.AddObservation((float)rotationIndex / 3);
        // Debug.Log("RotIndex " + (float)rotationIndex / 3 + ", Col " + this.transform.localPosition.x / (board.boardWidth - 1));

        // movement
        // sensor.AddObservation((float)m_Move / (board.boardWidth - 1));
        // sensor.AddObservation((float)m_Rotate / 3);
        // Debug.Log("Move: " + (float)m_Move / (board.boardWidth - 1) + ", Rotate: " + (float)m_Rotate / 3);

        // type
        sensor.AddOneHotObservation((int)trapData.shape, 7); // 7 represents the size of trap type enum
        sensor.AddObservation(StatesOfLoc);
        // sensor.AddObservation(this.transform.localPosition.z / board.boardDepth);
        
        // sensor.AddObservation((float)FindBottomFrom(1).z / board.boardDepth); 
        // sensor.AddObservation(this.transform.localPosition.z >= FindBottomFrom(1).z);
        // sensor.AddObservation((float)FindBottomFrom(-1).z / board.boardDepth); 
        // sensor.AddObservation(this.transform.localPosition.z >= FindBottomFrom(-1).z);
        
        /* Trap of Current Piece */
        /*
        if (trapBlocks != null)
        {
            for (int i = 0; i < trapBlocks.Length; i++) 
            {
                if (trapBlocks[i])
                    m_TrapBuffer.AppendObservation(GetTrapBlockData(i));
            }  
        } */

        /* Evaluate All Possible Locations */

       

        /* Match */
        // print("Match Information :");
        /*
        if (matches != null)
        {
            // print("match list :");
            foreach (var match in matches)
            {
                float[] a = new float[1];
                // Debug.Log(match);
                a[0] = match;
                m_MatchBuffer.AppendObservation(a);
            }
        } */
        
        // sensor.AddObservation(matches.Count == 0 ? 0 : matches[0]);  // first match count
        // sensor.AddObservation(numberOfMatches);
        // sensor.AddObservation(ghost.prevHasMatch);
        // sensor.AddObservation(ghost.hasMatch);

        // sensor.AddObservation(ghost.numberOfLeftMatches);
        // sensor.AddObservation(ghost.numberOfLeftMatches >= numberOfMatches);
        // sensor.AddObservation(ghost.hasRightMatch);

        // sensor.AddObservation(ghost.numberOfRightMatches);
        // sensor.AddObservation(ghost.numberOfRightMatches >= numberOfMatches);
        // sensor.AddObservation(ghost.hasLeftMatch);


        // Debug.Log(ghost.hasMatch); (ticked)

        /* Blocks Info */
        //print("Block Information :");
        /*
        var blocks = board.blockManager.GetBlocksList();
        for (int i = 0; i < blocks.Length; i++)
        {
            float[] a = new float[1];
            a[0] = i;
            if (blocks[i] != null)
                m_BlockBuffer.AppendObservation(i);
        } */

        /* Place State */
        // float distance = this.transform.localPosition.z - ghost.transform.localPosition.z;
        // sensor.AddObservation(distance / (board.boardDepth - 1)); 
        // sensor.AddObservation(distance == 0? true : false); 
        // sensor.AddObservation(canPlace);
    }

    /*
    private float[] GetTrapBlockData(int i)
    {
        var blockData = new float[1];
        blockData[0] = trapBlocks[i].colour;
        Vector3 blockPos = Vector3Int.FloorToInt(transform.localPosition) + route[i];
        blockData[1] = blockPos.x / board.boardWidth;
        blockData[2] = blockPos.z / board.boardDepth;

        return blockData;
    } */

    /*
    private int m_AgentStepCount; // current agent step
    private void FixedUpdate()
    {
        if (StepCount % 5 == 0)
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;
        }
    } */

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (locations == null || locations.Count == 0)
        {
            print("Missing locations");
            return;
        }
        for (int i = 0; i < locations.Count; i++)
        {
            if (!locations[i]._score.Exists)
            {
                // Debug.Log("Invalid " + i);
                actionMask.SetActionEnabled(0, i, false);
            }
            else
            {
                // Debug.Log(i);
                actionMask.SetActionEnabled(0, i, true);
            }
        }
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        // var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        /* Control the movement once obly at start */ 
        // Debug.Log("Input :" + discreteActions[0]);
        int rotIndex = discreteActions[0] / board.boardWidth;
        int col = discreteActions[0] % board.boardWidth;

        // Debug.Log("Create Trap with rotIndex: " + rotIndex + " and col: " + col);
        if (IsValidInput(rotIndex, col))
            MoveTrap(rotIndex, col);
        
        // else print("This is an invalid input!");

        /* Full Control of Piece (Note: Need Decision Requester!!!!!!!!!!!!!!!) */
        // m_MoveDown = (int)discreteActions[2] > 0;
        /*
        if (m_IsDecisionStep && actionEnabled)
        {
            if ((int)discreteActions[0] == 1)
            {
                validMove = Move(Vector3Int.left);
                m_Move = -1;
                ghost.UpdatePos();
            }
            else if ((int)discreteActions[0] == 2)
            {
                validMove = Move(Vector3Int.right);
                m_Move = 1;
                ghost.UpdatePos();
            }
            else
            {
                m_Move = 0;
            }

            if ((int)discreteActions[1] == 1)
            {
                Rotate(1);
                m_Rotate = 1;
                ghost.UpdatePos();
            }
            else if ((int)discreteActions[1] == 2)
            {
                Rotate(-1);
                m_Rotate = -1;
                ghost.UpdatePos();
            }
            else 
            {
                m_Rotate = 0;
            }
            m_IsDecisionStep = false;
        }
        */
        /*
        if (m_MoveDown && EnableMoveDown)
        {
            Move(-Vector3Int.forward);
        } */
    }

    public Vector3Int FindBottomFrom(int col, Vector3Int[] route)
    {
        Vector3Int pos = new Vector3Int(col, 0, (int)this.transform.localPosition.z);
        if (!board.IsValidPos(pos, route))
            return new Vector3Int(col, 0, -1);

        while (board.IsValidPos(pos - Vector3Int.forward, route))
        {
            pos -= Vector3Int.forward;
        }

        return pos; // return local position
    }

    private bool Move(Vector3Int translation)
    {
        Vector3Int newPos = Vector3Int.FloorToInt(this.transform.localPosition) + translation;
        if (board.IsValidPos(newPos, this.route)) // check position valid
        {
            this.transform.position += translation; // move
            return true; 
        }
        
        return false;  
    }

    private void Rotate(int direction)
    {
        int originalRotationIndex = this.rotationIndex;
        this.rotationIndex = Wrap(this.rotationIndex + direction, 0, 4);
        
        var newRoute = GetNewRoute(this.trapData, direction, this.route);
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

    private Vector3Int[] GetNewRoute(Trap trap, int direction, Vector3Int[] route) 
    {
        var newRoute = new Vector3Int[trap.route.Length]; // get new route
        for (int i = 0; i < route.Length; i++) 
        {
            Vector3 cell = route[i];

            int x, z;
            switch (trap.shape) // apply rotation matrix
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
        return newRoute;
    }

    private void HeavyPush() // playable only for human
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
            Vector3Int newPos = Vector3Int.FloorToInt(this.transform.localPosition) + translation;
            if (board.IsValidPos(newPos, newRoute)) // check valid with the new route
            {
                this.transform.position += translation;
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

        if (humanPlayable)
        {
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
        }
        
        
        // discreteActionsOut[2] = input.MoveDown() ? 1 : 0;
    }

   

}
