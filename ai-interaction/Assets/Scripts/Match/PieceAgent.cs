using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Random = UnityEngine.Random;

public enum AgentStates
{
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
    Create,
    Push,
    Match,
    Drop,
    Reset,
}

// Path: a list of states 
// State: a location and an action
public class Path : IHeapItem<Path>
{
    public List<State> path;
    public int gCost = 0;
    public int hCost = 0;

    int heapIndex;

    public Path(State startState)
    {
        path = new List<State>();
        path.Add(startState);
        gCost++;
        hCost = (int)startState._costToGoal;
    }

    public Path(List<State> states) // deep copy of path
    {
        this.path = states.ConvertAll(s => s.Clone()).ToList();
        gCost = path.Count;
        hCost = (int)path[path.Count -1]._costToGoal;
    }

    public State GetLastState()
    {
        return path[path.Count -1];
    }

    public int fCost 
    {
        get {
            return gCost + hCost;
        }
    }

    public void Add(State nextState) 
    {
        path.Add(nextState);
        gCost++;
        hCost = (int)nextState._costToGoal;
    }
    public int HeapIndex { 
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Path otherPath)
    {
        int compare = fCost.CompareTo(otherPath.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(otherPath.hCost);
        }
        return -compare;
    }
}
public struct State
{
    public Location _location;
    public Action _action;
    public float _costToGoal;
    public State(Location location, Action action, float costToGoal)
    {
        _location = location;
        _action = action;
        _costToGoal = costToGoal;
    }

    public State Clone() {
        return new State {_location = this._location, 
                            _action = this._action, 
                            _costToGoal = this._costToGoal};
    }
}
public struct Location
{
    public Vector3Int _pos;
    public Vector3Int[] _route;
    public int _rotIndex;
    public MatchEvaluation _score;
    public Board _board;
    // public int _index;

    public bool valid => _board.IsValidPos(_pos, _route);

    public Location(Board board, Vector3Int pos, Vector3Int[] route, int rotIndex, MatchEvaluation score)
    {
        _board = board;
        _pos = pos;
        _route = route;
        _rotIndex = rotIndex;

        _score = score;
    }

    public void AssignValue(MatchEvaluation score)
    {
        _score = score;
    }

    public bool Equal(Location other)
    {
        if (this._pos.Equals(other._pos) && this._rotIndex == other._rotIndex)
            return true;
        return false;
    }

    public bool ContainIn(List<Location> visitedLocations)
    {
        foreach (var loc in visitedLocations)
        {
            if (loc.Equal(this))
                return true;
        }
        return false;
    }
}
public enum Action
{
    MoveLeft,
    MoveRight,
    MoveDown,
    TurnLeft,
    TurnRight,
    None,
}

public struct MatchEvaluation
{
    public List<int> _matchedList;
    public bool _hasMatch;
    public int _linkNumber;
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
            _linkNumber = 0,
            _eliminatedMonsterNumber = 0,
            _smoothness = 0,
            _levelReduction = 0,
        };
    }

    public void PrintInformation()
    {
        Debug.Log("Score Assessment");
        Debug.Log("HasMatch: " + _hasMatch);
        Debug.Log("MatchNumber: " + _linkNumber);
        Debug.Log("ElimainedMonsterNumber: " + _eliminatedMonsterNumber);
        Debug.Log("Smoothness: " + _smoothness);
    }
}

public class PieceAgent : Agent 
{
    public EnvController m_EnvController;
    /* Controls */
    private MatchController input;
    public bool humanPlayable = false;
    private bool actionEnabled = true;
    private bool m_IsDecisionStep = false;
    // public bool disableInputCollectionInHeuristicCallback;
    // private int m_Move;
    // private int m_Rotate; 
    // private bool validMove = true;
    public bool isRandom = false;
    public bool hasMark = true;

    /* Trap */
    // private BufferSensorComponent m_TrapBuffer;
    private BufferSensorComponent m_LocationBuffer;
    public Board board;
    public TrapShape spawnTrap; // allows to choose a specific trap
    public Trap trapData { get; private set; }
    public int rotationIndex { get; set; }
    public bool isMonochrome { get; set; }
    public Block[] trapBlockPrefab; // block with different colors
    public Block[] trapBlocks;
    public Vector3Int[] route { get; private set; }
    public Ghost ghost;

    /* Setting */
    public Vector3Int spawnPosition; // local position relative to the board
    public Location start;
    public float dropSpeed = 1f;
    EnvironmentParameters m_ResetParams;
    public int DefaultSpawningRange;
    public int m_SpawningRange {get; set;} // input the number of rows

    /* States of Agent */
    private List<Location> locations;
    private List<int> possibleIndex = new List<int>();
    // private List<float> StatesOfLoc = new List<float>();
    // private int currentLoc;
    // public List<int> matches {get; set;}
    // public int numberOfMatches {get; set;}
    private float level; 
    private int totalMatch = 0;
    // public int matchColour { get; set; }
    public int Turn = 0;
    // private bool canPlace = false;

    #region Reset
    public void Reset() // reset only the game ends
    {
        Turn = 0;
        totalMatch = 0;
        // matchColour = 0;
        // matches = null;
        // numberOfMatches = 0;
        ResetTrap();
    }
    private void ResetTrap() // reset every turn
    {
        this.trapBlocks = null;
        this.route = null;
        this.transform.localPosition = this.spawnPosition;
        possibleIndex.Clear();
    }
    #endregion

    #region Create & Reset
     public override void Initialize() // Call only once!!!
    {
        input = GetComponent<MatchController>();

        var buffer = GetComponents<BufferSensorComponent>();
        m_LocationBuffer = buffer[0];
        // m_TrapBuffer = buffer[1];

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        this.transform.localPosition =  spawnPosition;
    }
    public override void OnEpisodeBegin()
    {
        // Initialize a number of monsters in the beginning
        m_SpawningRange = (int)m_ResetParams.GetWithDefault("spawn_range", DefaultSpawningRange);
        board.SpawnMonsterInAuto(m_SpawningRange);
    
        if (board.active)
            StartNewTurn();
        else 
            Debug.Log("Board is not active now!!!");
    }
    public void StartNewTurn(List<int> colorList = null)
    {
        ResetTrap();

        if (Turn != 0)
            EvaluateMove();
        ConsumeOneTurn();
        Turn++;

        if (Turn % board.spawnInterval == 0)
            board.StartMonsterTurn();
        else
            StartAdventurerTurn(colorList);
    }

    public void StartAdventurerTurn(List<int> colorList = null)
    {
        if (spawnTrap == TrapShape.None)
            trapData = board.GetRandomTrap();
        else trapData = board.GetTrap((int)spawnTrap);
        Debug.Log("Trap " + spawnTrap + " is created!");

        /* Check win & lose condition */
        int state = GetConditionState();
        if (state == 0)
            CreateTrap(colorList);
        if (state == 1)
            this.board.GameOver(true);
        if (state == -1)
            this.board.GameOver(false);
    }

    public int GetConditionState()
    {
        Debug.Log("Checking game conditions...");

        /* Construct the route of new trap first */
        this.route = new Vector3Int[trapData.route.Length];
        for (int i = 0; i < trapData.route.Length; i++) 
        {
            this.route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        // no space to put new trap
        if (!this.board.IsValidPos(spawnPosition, this.route))
        {
            Debug.Log("Cannot put new trap anymore at " + spawnPosition);
            return -1;
        }
        else if (this.board.blockManager.outBoundBlocks.Count > 0)
        {
            Debug.Log("Monsters is reaching");
            return -1;
        }
        else if (board.numberOfMonsters == 0)
        {
            Debug.Log("Turns to win: " + Turn);
            return 1;
        }

        return 0;
    }
    public void CreateTrap(List<int> colorList = null)
    {
        this.trapBlocks = new Block[trapData.route.Length];

        // Instantiate trap blocks
        if (colorList != null)
        {
            for (int i = 0; i < this.route.Length; i++)
            {
                int n = colorList[i];
                Vector3 pos = this.route[i] + this.transform.position; 
                var block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i] = block;
                block.transform.SetParent(this.transform);
            }
        }
        else 
        {
            for (int i = 0; i < this.route.Length; i+=2)
            {
                int n = Random.Range(0, trapBlockPrefab.Length);
                Vector3 pos = this.route[i] + this.transform.position; 
                var block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i] = block;
                block.transform.SetParent(this.transform);
                // block.owner = this;
                            
                pos = this.route[i+1] + this.transform.position; 
                block = Instantiate(trapBlockPrefab[n], pos, Quaternion.identity);
                trapBlocks[i+1] = block;
                block.transform.SetParent(this.transform);
                // block.owner = this;  
            }
        }

        if (trapBlocks[0].colour == trapBlocks[2].colour)
            isMonochrome = true;
        else
            isMonochrome = false;

        this.rotationIndex = 0;

        ghost.Initialize(this);

        actionEnabled = true;
        if (!humanPlayable)
            MakeDecision(); // choose a valid location

        // StartCoroutine(Push());
    }
    #endregion

    #region Get Possible Location
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

        // Print List results
        string result = "All possible cols: ";
        foreach (var item in cols)
        {
            result += item.ToString() + ", ";
        }
        Debug.Log(result);

        return cols;
    }
    public List<Location> GetAllBaseLocations(Trap trapData)
    {
        var locations = new List<Location>();
        var route = new Vector3Int[trapData.route.Length];
        for (int i = 0; i < trapData.route.Length; i++) 
        {
            route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

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
                    Location loc = new Location(this.board, pos, route, rotIndex, score);
                    locations.Add(loc);
                    // Debug.Log("RotIndex " + rotIndex + ", Col " + col + ": " + pos);
                    // score.PrintInformation();
                    // if (match.Count != 0 && match[match.Count - 1] != 0)
                    //    Debug.Log("Number of elimanated monsters: " + match[match.Count - 1]);
                    // Debug.Log(ghost.HasAMatch(match));
                }
                else
                {
                    Location loc = new Location(this.board, pos, route, rotIndex, MatchEvaluation.GetEmpty());
                    locations.Add(loc);
                }      
            }

            // Continue to next rotation shape
            route = GetNewRoute(trapData, 1, route);
        }
        return locations;
    }
    public List<Location> GetAllPossibleGoalLocations(Trap trapData, Location startLoc)
    {
        var locations = new List<Location>();
        var route = new Vector3Int[trapData.route.Length];
        for (int i = 0; i < trapData.route.Length; i++) 
        {
            route[i] = new Vector3Int(trapData.route[i].x, 0, trapData.route[i].y);
        }

        // Debug.Log("All Possible Locations:");
        int n = 0;
        for (int rotIndex = 0;  rotIndex < trapData.route.Length; rotIndex++)
        {
            for (int row = 0; row < board.boardDepth; row++)
            {
                for (int col = 0; col < board.boardWidth; col++)
                {
                    Vector3Int pos = new Vector3Int(col, 0, row);
                    Location loc = new Location(this.board, pos, route, rotIndex, MatchEvaluation.GetEmpty());

                    if (this.board.IsValidPos(pos, route) 
                        && !this.board.IsValidPos(pos - Vector3Int.forward, route))
                    {                    
                        // var path = GetOptimalPath(trapData, startLoc, loc);
                        // if (path.Count != 0) 
                        // {
                            MatchEvaluation score = new MatchEvaluation();
                            var scanner = new BlockManager(null, board.boardDepth, board.boardWidth);
                            score = scanner.GetMatchEvaluationAt(this, pos, route);
                            loc.AssignValue(score);
                            // Debug.Log("Trap can move to position (" + pos.x + ", " 
                            //            + pos.z + "), with rotation index " + rotIndex);
                            possibleIndex.Add(n);
                        // }                      
                    }
                    locations.Add(loc);
                    n++;
                }
            }

            // Continue to next rotation
            route = GetNewRoute(trapData, 1, route);
        }
        return locations;
    }
    #endregion

    #region Action: Evaluate & Choose Location to move to
    private void MakeDecision()
    {
        start = new Location(this.board, spawnPosition, this.route, 0, MatchEvaluation.GetEmpty());
        locations = GetAllPossibleGoalLocations(trapData, start);
        //Debug.Log("Total number of locations: " + locations.Count);
        //locations = GetAllBaseLocations(trapData);

        //StatesOfLoc = new List<float>();
        //foreach (var loc in locations)
        //{
        //    float[] state = GetStateFromLoc(loc._pos, loc._rotIndex, loc._score);
        //    StatesOfLoc.AddRange(state);
        //}

        RequestDecision();
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

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!humanPlayable)
        {
            if (locations == null || locations.Count == 0)
            {
                print("Missing locations to choose");
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
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        // var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        /* AI: Control the movement once only at start */ 
        if (!humanPlayable)
        {
            /* model3 actions
            int rotIndex = discreteActions[0] / board.boardWidth;
            int col = discreteActions[0] % board.boardWidth;
            if (IsValidInput(rotIndex, col))
                MoveTrap(rotIndex, col);
            */

            /* new model actions : Choose location -> construct path -> move according to path */
            int locationIndex = discreteActions[0];
            if (isRandom)
                locationIndex = possibleIndex[Random.Range(0, possibleIndex.Count)];
            
            if (locations[locationIndex]._score.Exists)
            {
                var selectedLoc = locations[locationIndex];

                /* Mark the locations */
                if (hasMark)
                {
                    for (int i = 0; i < selectedLoc._route.Length; i++)
                    {
                        board.MarkLocation(selectedLoc._pos + selectedLoc._route[i]);
                    }
                }

                /* Get the path and start to move */
                var path = GetOptimalPath(trapData, start, selectedLoc);
                StartCoroutine(MoveTrap(path));
            }
            else
                Debug.Log("Invalid input size");
        }

        /* Human: Full Control of Piece (Note: Need Decision Requester!!!!!!!!!!!!!!!) */
        else 
        {
            // m_MoveDown = (int)discreteActions[2] > 0;
            if (m_IsDecisionStep && actionEnabled)
            {
                if ((int)discreteActions[0] == 1)
                {
                    Move(Vector3Int.left);
                    // m_Move = -1;
                }
                else if ((int)discreteActions[0] == 2)
                {
                    Move(Vector3Int.right);
                    // m_Move = 1;
                }
                else
                {
                    // m_Move = 0;
                }

                if ((int)discreteActions[1] == 1)
                {
                    Rotate(1);
                    // m_Rotate = 1;
                }
                else if ((int)discreteActions[1] == 2)
                {
                    Rotate(-1);
                    // m_Rotate = -1;
                }
                else 
                {
                    // m_Rotate = 0;
                }
                m_IsDecisionStep = false;
            }
            /*
            if (m_MoveDown && EnableMoveDown)
            {
                Move(-Vector3Int.forward);
            } */
        }
    }

    /* For human play */
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

    #endregion

    #region Construct Path
    // Get optimal path by aStar Search
    public List<Action> GetOptimalPath(Trap trapData, Location startLoc, Location endLoc)
    {
        List<Action> actions = new List<Action>();
        List<Location> visited = new List<Location>();

        Path path = new Path(new State(startLoc, Action.None, GetDistanceCost(startLoc, endLoc)));

        Heap<Path> paths = new Heap<Path>(1000000);
        paths.Add(path);

        while (paths.Count > 0)
        {
            // get the path with the lowest cost (i.e. optimal path)
            var optimalPath = paths.RemoveFirst();
            
            // get the current state and add to the visited list
            var currentState = optimalPath.GetLastState();
            visited.Add(currentState._location);

            // check if current state is the goal 
            if (currentState._location.Equal(endLoc))
            {
                optimalPath.path.RemoveAt(0); // remove the first state (i.e. start) since it has no actions
                foreach (var s in optimalPath.path)
                    actions.Add(s._action);
                Debug.Log("There is a path");
                return actions;
            }

            foreach (var nextState in GetNextStates(trapData, currentState._location, endLoc))
            {
                if (nextState._location.ContainIn(visited))
                    continue;
                // construct new path with successors (need deep copy!!!)
                var newPath = new Path(optimalPath.path);
                newPath.Add(nextState);

                paths.Add(newPath);
            }
        }

        Debug.Log("There is no optimal path");
        return actions; // actions will be none if cannot reach the goal
    }

    List<State> GetNextStates(Trap trapData, Location loc, Location endLoc)
    {
        List<State> nextStates = new List<State>();

        Vector3Int left = new Vector3Int(loc._pos.x - 1, loc._pos.y, loc._pos.z);
        Location newLoc = new Location(this.board, left, loc._route, loc._rotIndex, 
                               MatchEvaluation.GetEmpty());
        if (newLoc.valid)
            nextStates.Add(new State(newLoc, Action.MoveLeft, GetDistanceCost(newLoc, endLoc)));

        Vector3Int right = new Vector3Int(loc._pos.x + 1, loc._pos.y, loc._pos.z);
        newLoc = new Location(this.board, right, loc._route, loc._rotIndex, 
                                            MatchEvaluation.GetEmpty());
        if (newLoc.valid)
            nextStates.Add(new State(newLoc, Action.MoveRight, GetDistanceCost(newLoc, endLoc)));

        Vector3Int down = new Vector3Int(loc._pos.x, loc._pos.y, loc._pos.z - 1);
        newLoc = new Location(this.board, down, loc._route, loc._rotIndex, 
                                            MatchEvaluation.GetEmpty());
        if (newLoc.valid)
            nextStates.Add(new State(newLoc, Action.MoveDown, GetDistanceCost(newLoc, endLoc)));
      
        var leftRoute = new Vector3Int[trapData.route.Length];
        leftRoute = GetNewRoute(trapData, -1, loc._route);
        newLoc = new Location(this.board, loc._pos, leftRoute, Wrap(loc._rotIndex - 1, 0, 4), 
                                                MatchEvaluation.GetEmpty());
        if (newLoc.valid)
            nextStates.Add(new State(newLoc, Action.TurnLeft, GetDistanceCost(newLoc, endLoc)));

        var rightRoute = new Vector3Int[trapData.route.Length];
        rightRoute = GetNewRoute(trapData, 1, loc._route);
        newLoc = new Location(this.board, loc._pos, rightRoute, Wrap(loc._rotIndex + 1, 0, 4), 
                                                MatchEvaluation.GetEmpty());
        if (newLoc.valid)
            nextStates.Add(new State(newLoc, Action.TurnRight, GetDistanceCost(newLoc, endLoc)));

        return nextStates;
    }

    // Gets the difference between two states. 
    float GetDistanceCost(Location loc1, Location loc2)
    {
        float dist = 0;
        // Local Difference (i.e. orientation difference)
        dist = Mathf.Abs(loc1._rotIndex - loc2._rotIndex);

        // Global Difference (i.e. distance) 
        dist += Vector3Int.Distance(loc1._pos, loc2._pos);

        return dist;
    }
    #endregion

    #region Move

    public IEnumerator MoveTrap(List<Action> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.Log("Invalid path provided");
            StartCoroutine(Push());
        }
        else
        {
            foreach (var action in path)
            {
                // Debug.Log(action);
                switch (action)
                {
                    case Action.MoveLeft:
                        Move(Vector3Int.left);
                        break;
                    case Action.MoveRight:
                        Move(Vector3Int.right);
                        break;
                    case Action.MoveDown:
                        Move(-Vector3Int.forward);
                        break;
                    case Action.TurnLeft:
                        Rotate(-1);
                        break;
                    case Action.TurnRight:
                        Rotate(1);
                        break;
                    default:
                        Debug.Log("Cannot move the trap!");
                        break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("Trap is placed");
            Place();
        }
    }

    private bool Move(Vector3Int translation)
    {
        Vector3Int newPos = Vector3Int.FloorToInt(this.transform.localPosition) + translation;
        if (board.IsValidPos(newPos, this.route)) // check position valid
        {
            this.transform.position += translation; // move
            ghost.UpdatePos();
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
        ghost.UpdatePos();
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

    private void MoveTrap(int rotIndex, int col) // Exact move !!!!!!!
    {
        // check valid again!
        this.transform.localPosition = new Vector3(col, 0, this.transform.localPosition.z);
        this.rotationIndex = rotIndex;
        for (int i = 0; i < rotIndex; i++)
            Rotate(1);
        ghost.UpdatePos();
    }
    private IEnumerator Push()
    {
        yield return new WaitForSeconds(0.5f);
        // canPlace = false;
        do 
        {
            ghost.UpdatePos();
            yield return new WaitForSeconds(dropSpeed);
        }
        while (Move(-Vector3Int.forward) && !board.gameOver); // drop: moving back

        Place();
    }
    private void HeavyPush() // playable only for human
    {
        this.transform.position = ghost.transform.position;
        Place();
    }
    #endregion
    
    #region Place & Match
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
        this.board.RemoveMark();
        actionEnabled = false;
        // m_Move = 0;
        // m_Rotate = 0;
        ghost.Reset(); // reset the ghost before occupy

        var matchSeq = this.board.Occupy(this); // will enable the detector once occupied
        
        // Match procedure is done by board
        StartCoroutine(this.board.PrepareForMatch(matchSeq));  
    }
    #endregion

    #region Reward System
    public void EvaluateMove()
    {
        /* Position */
        level = board.blockManager.GetCurrentLevel();
        if (level > (board.boardDepth - 3))
            AddReward(-1f);
        // Debug.Log("Level: " + -level / board.boardDepth); 
        else
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
        // AddReward(matchCount / board.boardWidth * board.boardDepth);
    }
    public void LinkUpBlock(int type)
    {
        // float boardSize = board.boardDepth * board.boardWidth;
        // AddReward(type / boardSize); 
    }
    public void ClearMonster(int colour)
    {
        switch (colour)
        {
            case 0: // blue
                m_EnvController.HealingGroup();
                break;
            case 1: // red
                break;
            case 2: // yellow
                break;
            default:
                break;
        }
        board.numberOfMonsters--;
        AddReward(1f);
    }

    public void ConsumeOneTurn()
    {
        AddReward(-0.01f);
    }
    #endregion

    #region Observation
    public override void CollectObservations(VectorSensor sensor)
    {
        
        sensor.AddOneHotObservation((int)trapData.shape, 7); // 7 represents the size of trap type enum
        sensor.AddObservation(isMonochrome);
        foreach (var loc in locations)
            m_LocationBuffer.AppendObservation(GetStateFromLoc(loc._pos, loc._rotIndex, loc._score));
    }

    private float[] GetStateFromLoc(Vector3Int pos, int rotIndex, MatchEvaluation score)
    {
        // Should normalizes the value between -1 and 1
        var locData = new float[6];
        if (score.Exists)
        {
            int boardSize = board.boardDepth * board.boardWidth;
            locData[0] = score._hasMatch? 1 : 0; // whether has match or not
            locData[1] = (float)score._linkNumber / boardSize; // match number
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
    
    #endregion

    /* Placement of Ghost */
    public Vector3Int FindBottomFrom(int col, Vector3Int[] route)
    {
        Vector3Int pos = new Vector3Int(col, 0, (int)this.transform.localPosition.z);
        // if is invalid position: -1
        if (!board.IsValidPos(pos, route))
            return new Vector3Int(col, 0, -1);

        while (board.IsValidPos(pos - Vector3Int.forward, route))
        {
            pos -= Vector3Int.forward;
        }

        return pos; // return local position
    }
}
