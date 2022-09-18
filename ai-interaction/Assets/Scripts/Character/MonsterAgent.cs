using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class MonsterAgent : Agent
{
    private EnvController m_EnvController;
    EnvironmentParameters m_ResetParams;

    private Animator m_Animator;

    AgentController agentControls;
    public bool canControl = true;

    private Rigidbody rb;
    public float turnSpeed = 300f;
    public float moveSpeed = 2f;

    public int damage = 1;

    public int maxHealth = 3;
    public int currentHealth { get; set;}
    public bool isDead = false;
    private HealthBar m_HealthBar;

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        rb = GetComponent<Rigidbody>();

        m_HealthBar = GetComponentInChildren<HealthBar>();
        if (!m_HealthBar)
            print("Missing health bar");
        
        m_Animator = GetComponent<Animator>();
            
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }
    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;

        SetResetParameters();
    }

    private void SetResetParameters()
    {
        SetAgentScale();
        SetHealth();
    }

    private void SetHealth()
    {
        currentHealth = maxHealth;
        m_HealthBar.SetMaxHealth(maxHealth);
    }

    public void GetDamage(int damage)
    {
        currentHealth -= damage;
        m_HealthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            m_EnvController.Eliminate(this.gameObject);
            m_EnvController.AddGroupReward(1, -1f / m_EnvController.MonstersList.Count);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }


    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        var forward = Mathf.Clamp(continuousActions[0], 0f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        dirToGo = transform.worldToLocalMatrix.MultiplyVector(transform.forward) * forward;
        dirToGo += transform.worldToLocalMatrix.MultiplyVector(transform.right) * right;
        rotateDir = transform.up * rotate;

        transform.Translate(dirToGo * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        bool actionCommand = discreteActions[0] > 0;
        if (actionCommand)
        {
            // perform action with space key
            m_Animator.SetBool("attack", true);
        }
        else
        {
            m_Animator.SetBool("attack", false);
        }
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private void OnCollisionEnter(Collision other) 
    {
        if (other.gameObject.CompareTag("Adventurer"))
        {
            var target = other.gameObject.GetComponent<AdventurerAgent>();
            target.GetDamage(damage);
            AddReward(1f / target.maxHealth);
            if (target.isDead)
                AddReward(target.worth);
        }
        else if (other.gameObject.CompareTag("Shield")) // attack ineffective
        {
            var adventurerAgent = other.transform.parent.GetComponent<AdventurerAgent>();
            AddReward(-0.1f);
            adventurerAgent.AddReward(0.5f); // successful defend
        }
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("Sword"))
        {
            var adventurerAgent = other.gameObject.transform.parent.GetComponent<AdventurerAgent>();
            adventurerAgent.DealDamage(this, 1);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_EnvController.m_NumberOfRemainingAdventurers);
    }
    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (canControl)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[2] = agentControls.GetVector().x;
            continuousActionsOut[0] = agentControls.GetVector().y;

            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = agentControls.AttackIsTriggered() ? 1 : 0;
        }
    }
}
