using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class AdventurerAgent : Agent
{
    public enum Class
    {
        Barbarian,
        Mage
    };
    public Class m_Class;

    private EnvController m_EnvController;
    private EnvironmentParameters m_ResetParams;

    private AgentController agentControls;
    public bool canControl = true;

    private Rigidbody rb;
    public float turnSpeed = 300f;
    public float moveSpeed = 2f;

    public readonly int maxHealth = 3;
    [SerializeField] private int currentHealth;
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

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;

        SetResetParameters();
    }
    protected void SetResetParameters()
    {
        SetAgentScale();
        currentHealth = maxHealth;
        m_HealthBar.SetMaxHealth(maxHealth);
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void GetDamage(int damage)
    {
        currentHealth -= damage;
        m_HealthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            isDead = true;
            AddReward(-0.3f);
        }
        else
        {
            AddReward(-0.1f);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    protected void MoveAgent(ActionBuffers actionBuffers)
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

    /*
        bool actionCommand = discreteActions[0] > 0;
        if (actionCommand)
        {
            // perform action with space key
           
        }
        else
        {
           
        }  */
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        switch (m_Class)
        {
            case Class.Barbarian:
                sensor.AddObservation(m_EnvController.m_NumberOfRemainingResources);
                break;

            case Class.Mage:
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
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (canControl)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[2] = agentControls.GetVector().x;
            continuousActionsOut[0] = agentControls.GetVector().y;

            // Remember to add discrete action when using !!!!!!!!
            // var discreteActionsOut = actionsOut.DiscreteActions;
            // discreteActionsOut[0] = agentControls.ActionIsTriggered() ? 1 : 0;
        }
    }
}
