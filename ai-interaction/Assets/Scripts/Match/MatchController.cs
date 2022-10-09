using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchController : MonoBehaviour
{
    public bool DisableInput = false;
    private MatchControls inputActions;
    private MatchControls.PieceActions actionMap;

    void Awake()
    {
        inputActions = new MatchControls();
        actionMap = inputActions.Piece;
    }
    void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }

    public bool MoveLeft()
    {
        if (DisableInput) return false;
        return actionMap.MoveLeft.ReadValue<float>() > 0;
    }
    public bool MoveRight()
    {
        if (DisableInput) return false;
        return actionMap.MoveRight.ReadValue<float>() > 0;
    }
    public bool RotateAnticlockwise()
    {
        if (DisableInput) return false;
        return actionMap.RotateAnticlockwise.ReadValue<float>() > 0;
    }
    public bool RotateClockwise()
    {
        if (DisableInput) return false;
        return actionMap.RotateClockwise.ReadValue<float>() > 0;
    }
    public bool MoveDown()
    {
        if (DisableInput) return false;
        return actionMap.MoveDown.ReadValue<float>() > 0;
    }
    
}
