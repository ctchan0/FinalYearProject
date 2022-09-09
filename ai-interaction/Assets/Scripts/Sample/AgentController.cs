using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AgentController : MonoBehaviour
{
    [SerializeField] InputAction agentControls;

    Vector2 moveDirection = Vector2.zero;

    private void OnEnable()
    {
        agentControls.Enable();
    }

    private void OnDisbale()
    {
        agentControls.Disable();
    }

    void Update()
    {
        moveDirection = agentControls.ReadValue<Vector2>();
    }

    public Vector2 GetVector()
    {
        return moveDirection;
    }
}
