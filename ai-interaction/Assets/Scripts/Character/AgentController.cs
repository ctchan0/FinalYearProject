using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AgentController : MonoBehaviour
{
    public InputAction moveControls;
    public InputAction attack;
    public InputAction useItem_1;
    public InputAction useItem_2;
    public InputAction useItem_3;

    private void OnEnable()
    {
        moveControls.Enable();
        attack.Enable();
        useItem_1.Enable();
        useItem_2.Enable();
        useItem_3.Enable();
    }

    private void OnDisbale()
    {
        moveControls.Disable();
        attack.Disable();
        useItem_1.Disable();
        useItem_2.Disable();
        useItem_3.Disable();
    }

    void Update()
    {
        
    }

    public Vector2 GetVector()
    {
        return moveControls.ReadValue<Vector2>();
    }

    public bool AttackIsTriggered()
    {
        return attack.ReadValue<float>() > 0;
    }

    public int GetItemIndex()
    {
        if (useItem_1.ReadValue<float>() > 0)
        {
            return 1;
        }
        if (useItem_2.ReadValue<float>() > 0)
        {
            return 2;
        }
        if (useItem_3.ReadValue<float>() > 0)
        {
            return 3;
        }
        return 0; // don't use item
    }
}
