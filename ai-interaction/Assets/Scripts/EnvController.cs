using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class EnvController : MonoBehaviour
{
    [System.Serializable]
    public class AdventurerInfo
    {
        public AdventurerAgent Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    Material m_GroundMaterial; //cached on Awake()
    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    public List<AdventurerInfo> agentsInfoList = new List<AdventurerInfo>();
    // private Dictionary<BarbarianAgent, AdventurerInfo> m_AdventurerDict = new Dictionary<BarbarianAgent, AdventurerInfo>();
    
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    private GameSetting m_GameSetting;

    private int m_NumberOfRemainingAdventurers;
    private SimpleMultiAgentGroup m_AgentGroup;
    void Start()
    {
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Catch the starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_GameSetting = FindObjectOfType<GameSetting>();

        // Initialize Resources

        // Initialize Monsters

        // Reset Adventurers Remaining
        m_NumberOfRemainingAdventurers = agentsInfoList.Count;
        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var e in agentsInfoList)
        {
            e.StartingPos = e.Agent.transform.position;
            e.StartingRot = e.Agent.transform.rotation;
            e.Rb = e.Agent.GetComponent<Rigidbody>();
            e.Col = e.Agent.GetComponent<Collider>();
            // Add to team manager
            m_AgentGroup.RegisterAgent(e.Agent);
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }

        //Hurry Up Penalty
        m_AgentGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
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

            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }
    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
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


    void ResetScene()
    {
        // Reset counter
        m_ResetTimer = 0;

        // Reset Players Remaining
        m_NumberOfRemainingAdventurers = agentsInfoList.Count;

        // Random platform rot
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        // Reset Agents
        foreach (var e in agentsInfoList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : e.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : e.StartingRot;

            e.Agent.transform.SetPositionAndRotation(pos, rot);
            e.Rb.velocity = Vector3.zero;
            e.Rb.angularVelocity = Vector3.zero;
            
            // Reborn Agents
            e.Agent.gameObject.SetActive(true);
            m_AgentGroup.RegisterAgent(e.Agent);
        }
    }
}
