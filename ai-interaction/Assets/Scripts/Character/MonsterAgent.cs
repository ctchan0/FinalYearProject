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

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        rb = GetComponent<Rigidbody>();
        
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

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Adventurer"))
        {
            // attack behaviour
            var adventurerAgent = other.gameObject.GetComponent<AdventurerAgent>();
            adventurerAgent.GetDamage(1);
            AddReward(0.3f);
            m_EnvController.AddGroupReward(0, -0.1f);
            if (adventurerAgent.isDead)
            {
                AddReward(0.4f);
                m_EnvController.AddGroupReward(0, -0.2f);
                m_EnvController.KilledByMonster(adventurerAgent);
            }
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
            discreteActionsOut[0] = agentControls.ActionIsTriggered() ? 1 : 0;
        }
    }
}
