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
    public float worth = 0.3f; // life value in game 

    public int maxHealth = 3;
    public int currentHealth { get; set;}
    public bool isDead = false;
    private HealthBar m_HealthBar;

    private InventoryController m_InventoryController;

    /* Mage skills */
    [Header("Mage's Properties:")]
    public GameObject laser;
    float laserLength;

    [Header("Knight's Properties:")]
    public GameObject shield;

    [Header("Rogue's Properties:")]
    public GameObject arrowPrefab;
    GameObject currentArrow;
    Transform shootPos;
    bool m_Shoot = true;

    bool m_Use = true;

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        rb = GetComponent<Rigidbody>();

        m_HealthBar = GetComponentInChildren<HealthBar>();
        if (!m_HealthBar)
            print(this.gameObject + ": Missing health bar");

        m_InventoryController = GetComponent<InventoryController>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;

        SetResetParameters();
    }

    #region Reset
    protected void SetResetParameters()
    {
        SetAgentScale();
        if (m_Class == Class.Mage)
            SetLaserLength();
        SetHealth();
        SetSkills();
        SetInventory();
    }

    public void SetInventory()
    {
        m_InventoryController.Reset();
    }

    public void SetSkills()
    {
        m_Shoot = true;

        if (arrowPrefab != null && currentArrow == null)
        {
            currentArrow = Instantiate(arrowPrefab, arrowPrefab.transform.position, arrowPrefab.transform.rotation);
            currentArrow.SetActive(true);
            currentArrow.GetComponent<Projectile>().belonger = this;
            currentArrow.transform.SetParent(this.transform);
        }
    }
    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }
    public void SetLaserLength()
    {
        laserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
    }
    private void SetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        m_HealthBar.SetMaxHealth(maxHealth);
    }

    #endregion

    #region Health

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

            m_EnvController.AddGroupReward(0, -1f / m_EnvController.AdventurersList.Count);
        }
        else
        {
            AddReward(-1f / this.maxHealth);
        }
    }

    public void GetCure(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        m_HealthBar.SetHealth(currentHealth);
    }

    #endregion

    #region Action
    private void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;

        // movement
        var forward = Mathf.Clamp(continuousActions[0], -0.5f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        dirToGo = transform.worldToLocalMatrix.MultiplyVector(transform.forward) * forward;
        dirToGo += transform.worldToLocalMatrix.MultiplyVector(transform.right) * right;
        rotateDir = transform.up * rotate;

        transform.Translate(dirToGo * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
    }

    private void UseBasicAttack(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;

        bool actionCommand = discreteActions[0] > 0;
        if (actionCommand)
        {
            // perform action with space key
            switch (m_Class)
            {
                case Class.Barbarian:
                    break;

                case Class.Mage:
                    if (m_Shoot)
                        StartCoroutine(UseSkill("ShootLaser", 2f));
                    break;
                case Class.Knight:
                    break;

                case Class.Rogue:
                    if (m_Shoot)
                        StartCoroutine(UseSkill("ShootArrow", 2f));
                    break;

                default:
                    Debug.Log(this.gameObject + " has unknown class ");
                    break;
            }
        }  
    }

    private void UseItem(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        int itemIndex = discreteActions[1];  // The size of discreteActions[1] must same as the size of inventory + 1 !!!!!!!!!!!!
        
        if (itemIndex == 0) return;

        if (m_Use && m_InventoryController.CanUseItem(itemIndex-1))
        {
            // use what item and add it to observation
            // AddReward(0.2f);
            StartCoroutine(UseItem(itemIndex-1, 1f)); 
        }
    }

    private IEnumerator UseItem(int itemIndex, float coolDownTime)
    {
        m_Use = false;
        m_InventoryController.PerformItemAction(itemIndex); 
        yield return new WaitForSeconds(coolDownTime);
        m_Use = true;
    }

    private IEnumerator UseSkill(string skill, float coolDownTime)
    {
        if (skill == "ShootLaser")
        {
            m_Shoot = false;
            
            // Shoot Laser
            laser.SetActive(true);
            laser.transform.localScale = new Vector3(1f, 1f, laserLength);
            var rayDir = 5.0f * transform.forward;
            
            // Debug.DrawRay(transform.position + new Vector3(0, 0.5f, 0), rayDir, Color.green, 0.5f, true);
            RaycastHit hit;
            if (Physics.SphereCast(transform.position + new Vector3(0, 0.5f, 0), 1f, rayDir, out hit, 5f))
            {
                if (hit.collider.gameObject.CompareTag("Adventurer"))
                {
                    // heal
                    var target = hit.collider.gameObject.GetComponent<AdventurerAgent>();
                    target.GetCure(2);
                    if (target.maxHealth >= 2)
                        AddReward(2f / target.maxHealth); // assure reward within range <= 1f
                    else 
                        AddReward(1f);
                }
                else if (hit.collider.gameObject.CompareTag("Monster"))
                {
                    // damage
                    var target = hit.collider.gameObject.GetComponent<MonsterAgent>();
                    DealDamage(target, 1);
                }
            }
            yield return new WaitForSeconds(0.1f);
            laser.SetActive(false);

            yield return new WaitForSeconds(coolDownTime);
            m_Shoot = true;
        }
        else if (skill == "ShootArrow")
        {
            m_Shoot = false;

            if (currentArrow)
                currentArrow.GetComponent<Projectile>().shoot = true;

            yield return new WaitForSeconds(coolDownTime);
            currentArrow = Instantiate(arrowPrefab, arrowPrefab.transform.position, arrowPrefab.transform.rotation);
            currentArrow.SetActive(true);
            currentArrow.GetComponent<Projectile>().belonger = this;
            currentArrow.transform.SetParent(this.transform);

            m_Shoot = true;
        }
        else
        {
            print("No such skill");
        }
    }

    #endregion

    #region Reward
    public void DealDamage(MonsterAgent target, int damage)
    {
        if (damage > target.maxHealth)
            damage = target.maxHealth;
        target.GetDamage(damage);
        AddReward(damage / target.maxHealth);

        if (target.isDead)
        {
            AddReward(0.3f);
            AddReward(1f / m_EnvController.MonstersList.Count);
        }
    }

    private void StayAlive()
    {
        if (!isDead)
        {
            switch (m_Class)
            {
                case Class.Barbarian:
                    AddReward(worth / m_EnvController.MaxEnvironmentSteps);
                    break;

                case Class.Mage:
                    AddReward(worth / m_EnvController.MaxEnvironmentSteps);
                    break;

                case Class.Rogue:
                    AddReward(worth / m_EnvController.MaxEnvironmentSteps);
                    break;

                default:
                    break;
            }
        }
    }

    public void CollectResources()
    {
        AddReward(0.5f);
    }

    #endregion

    public override void CollectObservations(VectorSensor sensor)
    {
        switch (m_Class)
        {
            case Class.Barbarian:
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingResources);
                break;

            case Class.Mage:
                sensor.AddObservation(m_Shoot);
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingMonsters);
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingAdventurers);
                foreach (var item in m_EnvController.AdventurersList)
                {
                    sensor.AddObservation(item.Adventurer.currentHealth);
                }
                break;

            case Class.Knight:
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingMonsters);
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingAdventurers);
                sensor.AddObservation(shield.transform.position);
                break;

            case Class.Rogue:
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingMonsters);
                sensor.AddObservation(m_Shoot);
                sensor.AddObservation(this.currentHealth);
                break;

            default:
                Debug.Log(this.gameObject + " has unknown class ");
                break;
        }
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers);
        UseBasicAttack(actionBuffers);
        UseItem(actionBuffers);
        
        StayAlive();
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
            discreteActionsOut[0] = agentControls.AttackIsTriggered() ? 1 : 0;
            discreteActionsOut[1] = agentControls.GetItemIndex();
        }
    }
}
