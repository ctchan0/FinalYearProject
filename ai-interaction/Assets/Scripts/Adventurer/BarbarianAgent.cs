using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System;
using System.Linq;

public class BarbarianAgent : AdventurerAgent
{
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_EnvController.m_NumberOfRemainingResources);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers);
    }
}
