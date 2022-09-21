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
    Material m_GroundMaterial; //cached on Awake()
    Renderer m_GroundRenderer;

    private GameSetting m_GameSetting;
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;

    public GridLayout m_GridLayout;
    private Grid grid;

    public List<InventoryItem> ItemCollectionList = new List<InventoryItem>(); // adventurers need to fulfil the list reuirement to win
    public List<InventoryItem> ItemInitiatorList = new List<InventoryItem>(); // items provided at the start

    public List<AdventurerInfo> AdventurersList = new List<AdventurerInfo>();
    // private Dictionary<AdventurerAgent, AdventurerInfo> m_AdventurerDict = new Dictionary<AdventurerAgent, AdventurerInfo>();
    public List<MonsterInfo> MonstersList = new List<MonsterInfo>();
    public List<ResourceInfo> ResourcesList = new List<ResourceInfo>();
    public int m_NumberOfRemainingAdventurers { get; set; }
    public int m_NumberOfRemainingMonsters { get; set; }
    public int m_NumberOfRemainingResources { get; set; }
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

        // Hurry Up Penalty
        m_MonsterGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
        m_AdventurerGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_GameSetting.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_GameSetting.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_GameSetting.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_GameSetting.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);

            if (Physics.CheckBox(randomSpawnPos, new Vector3(0.5f, 0.01f, 0.5f)) == false)
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
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
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
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
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
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = item.StartingRot;
            item.Resource.transform.SetPositionAndRotation(pos, rot);

            if (item.Resource.TryGetComponent<BreakableObject>(out BreakableObject breakableObject))
                breakableObject.Respawn();
            else if (item.Resource.TryGetComponent<Chest>(out Chest chest))
                chest.Reset();
        }

        foreach (var item in ItemInitiatorList)
        {
            if (!item.IsEmpty)
            {
                var i = Instantiate(item.item.ItemPrefab, GetRandomSpawnPos(), GetRandomRot());
                i.transform.SetParent(this.transform); // remember to set parent to the corresponding environment
                i.GetComponent<Item>().Quantity = item.quantity;
            }
        }

        StartCoroutine(CheckAllResourcesGathered(1f));
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

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

    public void GatherAllResources() // adventurers win
    {
        m_AdventurerGroup.AddGroupReward(0.5f);
        m_MonsterGroup.AddGroupReward(-0.5f);
        StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.goalScoredMaterial, 0.5f));

        print("Gather all resources");
        m_AdventurerGroup.EndGroupEpisode();
        m_MonsterGroup.EndGroupEpisode();

        StartCoroutine(ResetScene());
    }

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
            m_MonsterGroup.AddGroupReward(-0.5f);
            m_AdventurerGroup.AddGroupReward(0.5f);
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
            
            if (m_NumberOfRemainingAdventurers == 0 || adventurer.m_Class == Class.Rogue || adventurer.m_Class == Class.Barbarian)
            {
                AceGroup(0);
            }
        }
        else if (character.TryGetComponent<MonsterAgent>(out MonsterAgent monster))
        {
            m_NumberOfRemainingMonsters--;
            character.SetActive(false);
            
            if (m_NumberOfRemainingMonsters == 0)
            {
                AceGroup(1);
            }
        }
    }

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

}
