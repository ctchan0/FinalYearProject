using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnvController : MonoBehaviour
{
    [System.Serializable]
    public class AdventurerInfo
    {
        public AdventurerAgent Adventurer;
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
        public Monster Monster;
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
        public BreakableObject Resource;
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

    public List<AdventurerInfo> AdventurersList = new List<AdventurerInfo>();
    // private Dictionary<BarbarianAgent, AdventurerInfo> m_AdventurerDict = new Dictionary<BarbarianAgent, AdventurerInfo>();
    public List<MonsterInfo> MonstersList = new List<MonsterInfo>();
    public List<ResourceInfo> ResourcesList = new List<ResourceInfo>();

    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    private GameSetting m_GameSetting;

    public GridLayout m_GridLayout;
    private Grid grid;

    public int m_NumberOfRemainingAdventurers { get; set; }
    public int m_NumberOfRemianingMonsters { get; set; }
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

        // Initialize Resources
        m_NumberOfRemainingResources = ResourcesList.Count;
        foreach (var item in ResourcesList)
        {
            item.StartingPos = item.Resource.transform.position;
            item.StartingRot = item.Resource.transform.rotation;
            item.Col = item.Resource.GetComponent<Collider>();
        }

        // Initialize Monsters
        m_NumberOfRemianingMonsters = MonstersList.Count;
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

        // Initialize TeamManager
        m_NumberOfRemainingAdventurers = AdventurersList.Count;
        m_AdventurerGroup = new SimpleMultiAgentGroup();
        foreach (var item in AdventurersList)
        {
            item.StartingPos = item.Adventurer.transform.position;
            item.StartingRot = item.Adventurer.transform.rotation;
            item.Rb = item.Adventurer.GetComponent<Rigidbody>();
            item.Col = item.Adventurer.GetComponent<Collider>();
            // Add to team manager
            m_AdventurerGroup.RegisterAgent(item.Adventurer);
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AdventurerGroup.GroupEpisodeInterrupted();
            m_MonsterGroup.GroupEpisodeInterrupted();
            ResetScene();
        }

        //Hurry Up Penalty
        m_AdventurerGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
    }

    void Update()
    {
        // Goal of adventurer
        if (EmptyResources())
            GatherAllResources();
    }

    private bool EmptyResources()
    {
        return m_NumberOfRemainingResources == 0;
    }

    public void KilledByMonster(AdventurerAgent adventurer)
    {
        m_NumberOfRemainingAdventurers--;
        adventurer.gameObject.SetActive(false);
        
        if (m_NumberOfRemainingAdventurers == 0)
        {
            AceAdventurerGroup();
        }
    }
    private void AceAdventurerGroup()
    {
        m_MonsterGroup.AddGroupReward(1f);
        m_AdventurerGroup.AddGroupReward(-1f);
        StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.failMaterial, 0.5f));

        print("Gather all resources");
        m_AdventurerGroup.EndGroupEpisode();
        m_MonsterGroup.EndGroupEpisode();
        ResetScene();
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

    void ResetScene()
    {
        // Reset counter
        m_ResetTimer = 0;

        // Reset Remaining
        m_NumberOfRemainingAdventurers = AdventurersList.Count;
        m_NumberOfRemianingMonsters = MonstersList.Count;
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
            m_AdventurerGroup.RegisterAgent(item.Monster);
        }

        // Reset Resources
        foreach (var item in ResourcesList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = item.StartingRot;
            item.Resource.transform.SetPositionAndRotation(pos, rot);

            item.Resource.Respawn();
        }
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

    public void GatherAllResources()
    {
        m_AdventurerGroup.AddGroupReward(1f);
        m_MonsterGroup.AddGroupReward(-1f);
        StartCoroutine(GoalScoredSwapGroundMaterial(m_GameSetting.goalScoredMaterial, 0.5f));

        print("Gather all resources");
        m_AdventurerGroup.EndGroupEpisode();
        m_MonsterGroup.EndGroupEpisode();

        ResetScene();
    }
}
