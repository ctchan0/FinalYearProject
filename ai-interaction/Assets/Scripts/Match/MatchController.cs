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
        return actionMap.MoveLeft.ReadValue<float>() > 0;
    }
    public bool MoveRight()
    {
        return actionMap.MoveRight.ReadValue<float>() > 0;
    }
    public bool Rotate()
    {
        return actionMap.Rotate.ReadValue<float>() > 0;
    }

    
}
