using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AgentController : MonoBehaviour
{
    public InputAction moveControls;
    public InputAction action;
    Vector2 dir = Vector2.zero;

    private void OnEnable()
    {
        moveControls.Enable();
        action.Enable();
    }

    private void OnDisbale()
    {
        moveControls.Disable();
        action.Disable();
    }

    void Update()
    {
        
    }

    public Vector2 GetVector()
    {
        return moveControls.ReadValue<Vector2>();
    }

    public bool ActionIsTriggered()
    {
        return action.ReadValue<float>() > 0;
    }
}
