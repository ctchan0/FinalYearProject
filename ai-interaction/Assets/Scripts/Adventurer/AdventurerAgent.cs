using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class AdventurerAgent : Agent
{
    // public GameObject weapon;
    protected EnvController m_EnvController;
    // [SerializeField] Transform danger;
    protected AgentController agentControls;
    public bool canControl = true;

    // [SerializeField] List<GameObject> TargetsList = new List<GameObject>();

    protected EnvironmentParameters m_ResetParams;

    protected Rigidbody rb;
    public float turnSpeed = 300f;
    public float moveSpeed = 2f;

    public int maxHealth = 3;
    [SerializeField] protected int currentHealth;
    public bool isDead = false;

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<EnvController>();

        agentControls = GetComponent<AgentController>();
        rb = GetComponent<Rigidbody>();

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
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void GetDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            isDead = true;
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
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (canControl)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[2] = agentControls.GetVector().x;
            continuousActionsOut[0] = agentControls.GetVector().y;
        }
    }
}
