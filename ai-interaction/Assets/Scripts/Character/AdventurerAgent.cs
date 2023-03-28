using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public enum Class
{
    Barbarian,
    Mage,
    Knight,
    Rogue
};

public class AdventurerAgent : Agent
{
    public Class m_Class;

    private EnvController m_EnvController;
    private EnvironmentParameters m_ResetParams;

    private AgentController agentControls;
    public bool canControl = true;

    private Rigidbody rb;
    public float turnSpeed = 300f;
    public float moveSpeed = 2f;
    public float currentSpeed = 2f;
    public float worth = 0.3f; // life value [0, 1] in game 

    public int attack = 1;
    public int currentAttack = 0;

    public int maxHealth = 3;
    public int currentHealth { get; set;}
    public bool isDead = false;
    private HealthBar m_HealthBar;

    private InventoryController m_InventoryController;

    [Header("Barbarian")]
    public GameObject axe;

    [Header("Mage")]
    public GameObject laser;
    float laserLength;

    [Header("Knight")]
    public GameObject sword;
    public GameObject shield;

    [Header("Rogue")]
    public GameObject arrowPrefab;
    
    private bool m_IsDecisionStep;
    bool m_Attack = true;
    public bool pushing { get; set; }
    public GoalDetectTrigger pushingBlock;
    public GameObject goal;
    public bool isStrengthReset { get; set; }
    public bool isSpeedReset { get; set; }

    int m_Goal;
    float[] m_GoalOneHot;
    private BufferSensorComponent m_EnvBuffer;
    // private BufferSensorComponent m_BlockBuffer;

    public enum Mission 
    {
        pushBlock, // 0
        survive, // 1
        getResource, // 2
    }

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        rb = GetComponent<Rigidbody>();

        m_HealthBar = GetComponentInChildren<HealthBar>();
        if (!m_HealthBar)
            print(this.gameObject + ": Missing health bar");

        m_InventoryController = GetComponent<InventoryController>();

        var buffer = GetComponents<BufferSensorComponent>();
        m_EnvBuffer = buffer[0];
        //m_BlockBuffer = buffer[1];

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    public override void OnEpisodeBegin()
    {
        // SetResetParameters();
    }

    #region Reset
    public void SetResetParameters()
    {
        this.gameObject.SetActive(true);
        SetAgentScale();
        if (m_Class == Class.Mage)
            SetLaserLength();
        SetHealth();
        SetSkills();
        SetInventory();

        rb.velocity = Vector3.zero;
        turnSpeed = 300f;
        currentSpeed = moveSpeed;
        isSpeedReset = true;
        currentAttack = attack;
        rb.mass = 1f;
        isStrengthReset = true;

        pushing = false;
        pushingBlock = null;

    }

    public void SetInventory()
    {
        m_InventoryController.Reset();
    }

    public void SetSkills()
    {
        m_Attack = true;
    }
    public void SetAgentScale()
    {
        // float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        float agentScale = 1f;
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }
    public void SetLaserLength()
    {
        // laserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
        laserLength = 1f;
    }
    private void SetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        m_HealthBar.SetMaxHealth(maxHealth);
    }

    private int m_AgentStepCount; //current agent step
    void FixedUpdate()
    {
        if (StepCount % 5 == 0)
        {
            m_IsDecisionStep = true;
            m_AgentStepCount++;
        }
    }
    
    #endregion

    #region Action
    private void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;

        // movement
        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        // transform.forward & transform.right are relative to world space
        // while dirToGo.z and dirToGo.x are relative to local space
        dirToGo = new Vector3(right, 0, forward); 
        rotateDir = transform.up * rotate;
        
        transform.Translate(dirToGo * currentSpeed * Time.fixedDeltaTime); // relative to local space
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
    }

    private void UseBasicAttack(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;

        bool actionCommand = discreteActions[0] > 0;
        if (actionCommand)
        {
            // attack with space key
            if (m_Attack)
            {
                StartCoroutine(Attack(1f));
            }
        }  
    }

    private void UseItem(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        int itemIndex = discreteActions[1];  // The size of discreteActions[1] must same as the size of inventory + 1 !!!!!!!!!!!!
        
        if (itemIndex == 0) return; // 0 respresents doing nothing
        else itemIndex--; // else get the corresponding item index

        if (m_InventoryController.IsItemAvailable(itemIndex))
            m_InventoryController.PerformItemAction(itemIndex); 
    }

    private IEnumerator Attack(float coolDownTime)
    {
        m_Attack = false;
        if (m_Class == Class.Barbarian)
        {
            axe.GetComponent<Animator>().SetBool("Attack", true);
            yield return new WaitForSeconds(0.1f);
            axe.GetComponent<Animator>().SetBool("Attack", false);
            yield return new WaitForSeconds(coolDownTime);
        }
        else if (m_Class == Class.Mage)
        {
            // shoot
            laser.SetActive(true);
            laser.transform.localScale = new Vector3(1f, 1f, laserLength);
            var rayDir = 5.0f * transform.forward;
            
            // Debug.DrawRay(transform.position + new Vector3(0, 0.5f, 0), rayDir, Color.green, 0.5f, true);
            RaycastHit hit;
            if (Physics.SphereCast(transform.position + new Vector3(0, 0.5f, 0), 1f, rayDir, out hit, 5f))
            {
                var hitObject = hit.collider.gameObject;
                if (hitObject.CompareTag("Adventurer"))
                {
                    // heal
                    var target = hitObject.GetComponent<AdventurerAgent>();
                    target.GetCure(2);
                    HitTarget();
                }
                else if (hitObject.CompareTag("Monster"))
                {
                    // deal damage
                    var target = hitObject.GetComponent<MonsterAgent>();
                    GetCure(currentAttack);
                    DealDamage(target, currentAttack);
                }
                else if (hitObject.CompareTag("Breakable"))
                {
                    var target = hitObject.GetComponent<BreakableObject>();
                    target.HandleHit(this);
                }
            }
            yield return new WaitForSeconds(0.1f);
            laser.SetActive(false);

            yield return new WaitForSeconds(coolDownTime);
        }
        else if (m_Class == Class.Knight)
        {
            sword.GetComponent<Animator>().SetBool("Attack", true);
            yield return new WaitForSeconds(0.1f);
            sword.GetComponent<Animator>().SetBool("Attack", false);
            yield return new WaitForSeconds(coolDownTime);
        }
        else if (m_Class == Class.Rogue)
        {
            // reload the arrow after a certain period of time
            for (int i = 0; i < 3; i++)
            {
                var arrow = Instantiate(arrowPrefab, arrowPrefab.transform.position, arrowPrefab.transform.rotation);
                arrow.SetActive(true);
                arrow.GetComponent<Projectile>().belonger = this;
                arrow.transform.SetParent(this.transform);
                yield return new WaitForSeconds(0.25f);
            }
            yield return new WaitForSeconds(coolDownTime);
        }
        else
        {
            print("No such skill");
        }
        m_Attack = true;
    }

    #endregion

    #region Rewards

    /* Survive (health system) */
    public void HitTarget()
    {
        AddReward(0.1f);
    }
    public void DealDamage(MonsterAgent target, int damage)
    {
        if (damage > target.maxHealth)
            damage = target.maxHealth;
        target.GetDamage(damage);
        AddReward((float)damage / target.maxHealth);

        if (target.isDead)
        {
            // AddReward(0.3f); // price of a monster
            // print($"{m_Class} killed a  monster");
            GetCure(1);

            AddReward(1f / m_EnvController.MonstersList.Count); // bonus price
        }
    }
    public void GetDamage(int damage)
    {
        currentHealth -= damage;
        m_HealthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            isDead = true; // always come first 
            
            m_InventoryController.Clear();
            m_EnvController.Eliminate(this.gameObject);
            AddReward(-worth);
        }
        else
        {
            AddReward((float)-damage / this.maxHealth);
        }
    }
    public void GetCure(int healAmount)
    {
        int prvHealth = currentHealth;

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (currentHealth != prvHealth)
            AddReward((float)(currentHealth - prvHealth) / maxHealth); // add rewards depend the amount of healing

        m_HealthBar.SetHealth(currentHealth);
    }
    private void StayAlive()
    {
        if (!isDead)
        {
           AddReward(worth / m_EnvController.MaxEnvironmentSteps);      
        }
    }
 
    /* Get resource */
    public void PushBlocks()
    {
        var block = this.pushingBlock;
        float distance = Vector3.Distance(block.transform.localPosition, m_EnvController.goal.transform.localPosition);

        if (distance <= 1.5f) // close enough
        {
            block.distance = distance;
            return;
        }

        float diff = block.distance - distance; // check distance
        block.distance = distance;
        AddReward(diff * 10); // 10 is a multiplier
    } 

    public void DiscoverResources()
    {
        m_EnvController.m_NumberOfRemainingResources--;
        AddReward(0.5f);
        m_EnvController.AddGroupReward(1, -1f / m_EnvController.ResourcesList.Count); // monsters will lose some advantages
    }
    public void CollectResources()
    {
        AddReward(0.5f);
    }

    /* Push blocks (get ideas from mlagent examples: push blocks)*/
    // rewards are given in m_EnvController (get a block)

    #endregion

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_Attack); // frequency of attack
        sensor.AddObservation(pushing);

        sensor.AddObservation((float)this.currentHealth / this.maxHealth);

        // sensor.AddObservation(m_EnvController.m_NumberOfRemainingAdventurers);
        sensor.AddObservation(m_EnvController.m_NumberOfRemainingMonsters);
        // sensor.AddObservation(m_EnvController.m_NumberOfRemainingResources / m_EnvController.ResourcesList.Count);
    
        // sensor.AddObservation(Vector3.Dot(rb.velocity, rb.transform.forward));
        // sensor.AddObservation(Vector3.Dot(rb.velocity, rb.transform.right));

        // float[] inventorySlot = new float[3];
        // inventorySlot[0] = m_InventoryController.IsItemAvailable(0) ? 1f : 0f;
        // inventorySlot[1] = m_InventoryController.IsItemAvailable(1) ? 1f : 0f;
        // inventorySlot[2] = m_InventoryController.IsItemAvailable(2) ? 1f : 0f;
        // sensor.AddObservation(inventorySlot);

        int count = 0;
        foreach (var resource in m_EnvController.ResourcesList)
        {
            // if (resource.Resource.activeInHierarchy)
            var block = resource.Resource.GetComponent<GoalDetectTrigger>();
            if (block.toGoal)
                count++;
            sensor.AddObservation(block.toGoal);
            sensor.AddObservation(block.GetDistance() / 10f);   
        } 
        sensor.AddObservation((float)count / 4);

        // Add team buffer, and the goal
        List<EnvController.AdventurerInfo> teamList;
        teamList = m_EnvController.AdventurersList;
        List<EnvController.MonsterInfo> opponentList;
        opponentList = m_EnvController.MonstersList;

        /* Team Info */ 
        foreach (var info in teamList)
        {
            if (info.Adventurer != this && info.Adventurer.gameObject.activeInHierarchy)
            {
                m_EnvBuffer.AppendObservation(GetAllyData(info));
            }
        }

        /* Opponent Info */
        foreach (var info in opponentList)
        {
            if (info.Monster.gameObject.activeInHierarchy)
            {
                
                m_EnvBuffer.AppendObservation(GetOpponentData(info));
            }
        }

        /*
        switch (m_Class)
        {
            case Class.Barbarian:
                sensor.AddObservation(axe.transform.localRotation.z); 
                break;

            case Class.Knight:
                sensor.AddObservation(sword.transform.localPosition.z); 
                break;      

            default:
                break;
        } */
    }

    private float[] GetAllyData(EnvController.AdventurerInfo info)
    {
        var data = new float[3];

        data[0] = 0f; // 0: Adventurer, 1: Monster 
        data[1] = Vector3.Dot(transform.forward, 
                            Vector3.Normalize(info.Adventurer.gameObject.transform.position 
                            - this.transform.position)); // the direction of the teammates
        // data[2] = Vector3.Distance(transform.localPosition, info.Adventurer.gameObject.transform.localPosition) / 20f;
        data[2] = (float)info.Adventurer.currentHealth / info.Adventurer.maxHealth;
        return data;
    }

    private float[] GetOpponentData(EnvController.MonsterInfo info)
    {
        var data = new float[3];

        data[0] = 1f; // 0: Adventurer, 1: Monster 
        data[1] = Vector3.Dot(transform.forward, 
                            Vector3.Normalize(info.Monster.gameObject.transform.position 
                            - this.transform.position)); // the direction of the teammates
        // Debug.Log(data[1]);
        // data[2] = Vector3.Distance(transform.localPosition, info.Monster.gameObject.transform.localPosition) / 20f;
        data[2] = (float)info.Monster.currentHealth / info.Monster.maxHealth;
        return data;
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers);
        if (m_IsDecisionStep)
        {
            m_IsDecisionStep = false;
            UseBasicAttack(actionBuffers);
            // UseItem(actionBuffers);
        }
        
        StayAlive();

        if (pushing)
            PushBlocks();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (canControl)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[2] = agentControls.GetVector().x;
            continuousActionsOut[0] = agentControls.GetVector().y;

            // Remember to add discrete action when using !!!!!!!!
            var discreteActionsOut = actionsOut.DiscreteActions;
            // Normal Attack
            discreteActionsOut[0] = agentControls.AttackIsTriggered() ? 1 : 0;
            // Item Usage
            // discreteActionsOut[1] = agentControls.GetItemIndex();
        }
    }
}
