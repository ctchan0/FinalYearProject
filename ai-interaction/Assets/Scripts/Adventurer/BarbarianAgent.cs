using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System;

public class BarbarianAgent : AdventurerAgent
{
    // public GameObject weapon;
    private GameSetting m_GameSetting;
    private EnvController m_EnvController;
    public GameObject area;
    [SerializeField] Transform danger;
    AgentController agentControls;

    private Rigidbody m_AgentRb;
    public float turnSpeed = 300f;
    public float moveSpeed = 2f;

    // public GameObject myLaser;
    // private float m_LaserLength;
    
    public bool contribute;
    public bool useVectorObs;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_GameSetting = FindObjectOfType<GameSetting>();
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        m_AgentRb = GetComponent<Rigidbody>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void OnEpisodeBegin()
    {
        m_AgentRb.velocity = Vector3.zero;
        // myLaser.transform.localScale = new Vector3(0f, 0f, 0f);

        SetResetParameters();
    }
    private void SetResetParameters()
    {
        // SetLaserLengths();
        SetAgentScale();
    }

    public void SetLaserLengths()
    {
        // m_LaserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
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

        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
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

    void OnCollisionEnter(Collision col)
    {
        
    }

    void OnTriggerEnter(Collider col)
    {
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[2] = agentControls.GetVector().x;
        continuousActionsOut[0] = agentControls.GetVector().y;
    }
}
