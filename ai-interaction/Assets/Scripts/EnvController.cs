using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Inventory.Model;
using UnityEngine.Tilemaps;

public class EnvController : MonoBehaviour
{
    [System.Serializable]
    public class AdventurerInfo
    {
        public AdventurerAgent Adventurer;
        [HideInInspector]
        public Class Class;
        [HideInInspector]
        public InventoryController InventoryController; // take reference
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }
    [System.Serializable]
    public class MonsterInfo
    {
        public MonsterAgent Monster;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }
    [System.Serializable]
    public class ResourceInfo
    {
        public GameObject Resource;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Collider Col;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;
    public float size = 2f; // size of ground
    Material m_GroundMaterial; //cached on Awake()
    Renderer m_GroundRenderer;
    public GameObject goal;

    private GameSetting m_GameSetting;
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    // private bool ready = false;

    public GridLayout m_GridLayout;
    private Grid grid;

    public List<GoalDetectTrigger> blockLists = new List<GoalDetectTrigger>();
    public PieceAgent activePiece;
    
    public List<InventoryItem> ItemCollectionList = new List<InventoryItem>(); // adventurers need to fulfil the list reuirement to win
    public List<InventoryItem> ItemInitiatorList = new List<InventoryItem>(); // items provided at the start

    public List<AdventurerInfo> AdventurersList = new List<AdventurerInfo>();
    // private Dictionary<AdventurerAgent, AdventurerInfo> m_AdventurerDict = new Dictionary<AdventurerAgent, AdventurerInfo>();
    public List<MonsterInfo> MonstersList = new List<MonsterInfo>();
    public List<ResourceInfo> ResourcesList = new List<ResourceInfo>();
    public int m_NumberOfRemainingAdventurers { get; set; }
    public int m_NumberOfRemainingMonsters { get; set; }
    public int m_NumberOfRemainingResources { get; set; }
    public GameObject resource; // to gather all resources together
    public GameObject monsterPrefab;
    private SimpleMultiAgentGroup m_AdventurerGroup;
    private SimpleMultiAgentGroup m_MonsterGroup;
    void Start()
    {
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Catch the starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_GameSetting = FindObjectOfType<GameSetting>();

        if (m_GridLayout)
            grid = m_GridLayout.GetComponent<Grid>();

        m_NumberOfRemainingAdventurers = AdventurersList.Count;
        m_AdventurerGroup = new SimpleMultiAgentGroup();
        foreach (var item in AdventurersList)
        {
            item.Class = item.Adventurer.m_Class;
            item.InventoryController = item.Adventurer.GetComponent<InventoryController>();
            item.StartingPos = item.Adventurer.transform.position;
            item.StartingRot = item.Adventurer.transform.rotation;
            item.Rb = item.Adventurer.GetComponent<Rigidbody>();
            item.Col = item.Adventurer.GetComponent<Collider>();
            // Add to team manager
            m_AdventurerGroup.RegisterAgent(item.Adventurer);
        }

        m_NumberOfRemainingMonsters = MonstersList.Count;
        m_MonsterGroup = new SimpleMultiAgentGroup(); 
        foreach (var item in MonstersList)
        {
            item.StartingPos = item.Monster.transform.position;
            item.StartingRot = item.Monster.transform.rotation;
            item.Rb = item.Monster.GetComponent<Rigidbody>();
            item.Col = item.Monster.GetComponent<Collider>();
            // Add to team manager
            m_MonsterGroup.RegisterAgent(item.Monster);
        }

        m_NumberOfRemainingResources = ResourcesList.Count;
        foreach (var item in ResourcesList)
        {
            item.StartingPos = item.Resource.transform.position;
            item.StartingRot = item.Resource.transform.rotation;
            item.Col = item.Resource.GetComponent<Collider>();
        }

        StartCoroutine(ResetScene());
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AdventurerGroup.GroupEpisodeInterrupted();
            m_MonsterGroup.GroupEpisodeInterrupted();
            StartCoroutine(ResetScene());
        }
        if (m_ResetTimer % 1000 == 0) // Adventurers: hold blocks within goal area
        {
            AddGroupReward(0, (float)blockLists.Count / 4);
        }

        // Hurry Up Penalty
        // m_MonsterGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
        m_AdventurerGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
    }

    #region Random Spawn
    public Vector3 GetRandomSpawnPos(Bounds bounds)
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-bounds.extents.x * m_GameSetting.spawnAreaMarginMultiplier,
                bounds.extents.x * m_GameSetting.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-bounds.extents.z * m_GameSetting.spawnAreaMarginMultiplier,
                bounds.extents.z * m_GameSetting.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);

            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false) // ensure there is a distance from others
            {
                foundNewSpawnLocation = true;
            }
        }
        return SnapCoordinateToGrid(randomSpawnPos);
    }
    public Vector3 SnapCoordinateToGrid(Vector3 position)
    {
        Vector3Int cellPos = m_GridLayout.WorldToCell(position);
        return grid.GetCellCenterWorld(cellPos);
    }
    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }
    #endregion
    
    private IEnumerator ResetScene() 
    {
        yield return new WaitForSeconds(0.5f); // give more time to prepare new scene

        // Reset counter
        m_ResetTimer = 0;

        // Clear all items
        var remainingItems = GetComponentsInChildren<Item>();
        foreach (var item in remainingItems)
        {
            item.ClearItem();
        }

        // Reset Remaining
        m_NumberOfRemainingAdventurers = AdventurersList.Count;
        m_NumberOfRemainingMonsters = MonstersList.Count;
        m_NumberOfRemainingResources = ResourcesList.Count;

        // Random platform rot
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        // Reset Agents
        foreach (var item in AdventurersList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos(areaBounds) : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;
            item.Adventurer.transform.SetPositionAndRotation(pos, rot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            
            // Reborn Agents
            item.Adventurer.gameObject.SetActive(true);
            m_AdventurerGroup.RegisterAgent(item.Adventurer);
        }

        // Reset Monsters
        foreach (var item in MonstersList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos(areaBounds) : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;
            item.Monster.transform.SetPositionAndRotation(pos, rot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            
            // Reborn Agents
            item.Monster.gameObject.SetActive(true);
            m_MonsterGroup.RegisterAgent(item.Monster);
        }

        // Reset Resources
        foreach (var item in ResourcesList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos(areaBounds) : item.StartingPos;
            var rot = item.StartingRot;
            item.Resource.transform.SetPositionAndRotation(pos, rot);

            if (item.Resource.TryGetComponent<BreakableObject>(out BreakableObject breakableObject))
                breakableObject.Respawn();
            else if (item.Resource.TryGetComponent<Chest>(out Chest chest))
                chest.Reset();
            else if (item.Resource.TryGetComponent<GoalDetectTrigger>(out GoalDetectTrigger pushBlock))
                pushBlock.Reset();
        }

        foreach (var item in ItemInitiatorList)
        {
            if (!item.IsEmpty)
            {
                var i = Instantiate(item.item.ItemPrefab, GetRandomSpawnPos(areaBounds), GetRandomRot());
                i.transform.SetParent(this.transform); // remember to set parent to the corresponding environment
                i.GetComponent<Item>().Quantity = item.quantity;
            }
        }

        blockLists = new List<GoalDetectTrigger>();
        // StartCoroutine(CheckAllResourcesGathered(1f));
    }

    #region Item Collection
    IEnumerator CheckAllResourcesGathered(float checkTimeInterval)
    {
        while(true)
        {
            yield return new WaitForSeconds(checkTimeInterval);
            if (CollectionCompleted(ItemCollectionList))
            {
                GatherAllResources();
                break;
            }
        }
    }

    public bool CollectionCompleted(List<InventoryItem> collectionList) // can display the progress of completion later
    {
        foreach (var item in collectionList)
        {
            if (!ResourcesChecked(item.item, item.quantity)) 
                return false;
        }
        return true;
    }

    public bool ResourcesChecked(ItemSO item, int quantity) // return true if satisfy the quantity requirements
    {
        int count = quantity;
        foreach (var adventurer in AdventurersList)
        {
            count = adventurer.InventoryController.ExistsInInventory(item, count);
            if (count == 0)
                return true;
        }
        return false;
    }

    #endregion
    
    #region Block Collection
    public void GetABlock(GoalDetectTrigger block, float score)
    {
        print($"Scored {score} on {gameObject.name}");

        blockLists.Add(block);

        AddGroupReward(0, score);
        AddGroupReward(1, -score);

        if (blockLists.Count == 4)
        {
            List<int> colorList = new List<int>();
            foreach (var b in blockLists)
            {
                colorList.Add(b.color);
                b.Destroy();
            }
            blockLists.Clear();
            GatherAllResources();
            // Debug.Log("A Trap is available for initialization");
    
            // Reset and then give to the piece agent
            if (activePiece)
                activePiece.StartNewTurn(colorList); 
        }
    }

    public void MissABlock(GoalDetectTrigger block, float score)
    {
        blockLists.Remove(block);

        AddGroupReward(0, -score);
        AddGroupReward(1, score);

        if (blockLists.Count < 0)
            Debug.Log("Invalid number of push blocks ");
    }
    #endregion

    public void GatherAllResources() // adventurers win
    {
        m_AdventurerGroup.AddGroupReward(1f);
        m_MonsterGroup.AddGroupReward(-1f);
        StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.goalScoredMaterial, 0.5f));

        print("Gather all resources");
        m_AdventurerGroup.EndGroupEpisode();
        m_MonsterGroup.EndGroupEpisode();

        StartCoroutine(ResetScene());
    }

    #region Team state
    public void AceGroup(int teamId) 
    {
        if (teamId == 0) // adventurer's team
        {
            m_MonsterGroup.AddGroupReward(1f);
            m_AdventurerGroup.AddGroupReward(-1f);
            StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.failMaterial, 0.5f));
            print("All adventurers are dead");

            m_AdventurerGroup.EndGroupEpisode();
            m_MonsterGroup.EndGroupEpisode();
            StartCoroutine(ResetScene());
        }
        else if (teamId == 1) // monster's team
        {
            m_MonsterGroup.AddGroupReward(-1f);
            m_AdventurerGroup.AddGroupReward(1f);
            StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.goalScoredMaterial, 0.5f));
            // print("All Monsters are dead");
        }
    }
    public void Eliminate(GameObject character)
    {
        if (character.TryGetComponent<AdventurerAgent>(out AdventurerAgent adventurer))
        {
            m_NumberOfRemainingAdventurers--;
            character.SetActive(false);
            AddGroupReward(1, 0.5f);
            
            if (m_NumberOfRemainingAdventurers == 0)
                AceGroup(0);
        }
        else if (character.TryGetComponent<MonsterAgent>(out MonsterAgent monster))
        {
            m_NumberOfRemainingMonsters--;
            character.SetActive(false);
            AddGroupReward(0, 0.5f);
            
            if (m_NumberOfRemainingMonsters == 0)
                AceGroup(1);
        }
    }

    public void SpawnMonster()
    {
        m_NumberOfRemainingMonsters++;
        /* Spawn monsters from goal point */
        var monster = Instantiate(monsterPrefab, GetRandomSpawnPos(goal.GetComponent<Collider>().bounds), GetRandomRot());
    }
    #endregion

    #region Upgrade
    public void GetCure()
    {
        foreach (var item in AdventurersList)
        {
            if (!item.Adventurer.isDead)
            {
                item.Adventurer.GetCure(1);
            }
        }
    }

    public void GetStrength()
    {
        foreach (var item in AdventurersList)
        {
            if (!item.Adventurer.isDead)
            {
                StartCoroutine(TemporaryUpgrade(item.Adventurer, item.Rb.mass, 0.5f, 3f));
            }
        }
    }

    public void GetSpeed()
    {
        foreach (var item in AdventurersList)
        {
            if (!item.Adventurer.isDead)
            {
                StartCoroutine(TemporaryUpgrade(item.Adventurer, item.Adventurer.currentSpeed, 0.5f, 3f));
                StartCoroutine(TemporaryUpgrade(item.Adventurer, item.Adventurer.turnSpeed, 50f, 3f));
            }
        }
    }

    IEnumerator TemporaryUpgrade(AdventurerAgent adventurer, float value, float increment, float time)
    {
        value = value + increment;
        yield return new WaitForSeconds(time);
        if (!adventurer.isDead)
            value = value - increment;
    }

    #endregion

    public void AddGroupReward(int teamId, float point)
    {
        switch(teamId)
        {
            case 0: // adventurers group
                m_AdventurerGroup.AddGroupReward(point);
                break;
            case 1: // monsters group
                m_MonsterGroup.AddGroupReward(point);
                break;
            default:
                print("No such group");
                break;
        }
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }
}
