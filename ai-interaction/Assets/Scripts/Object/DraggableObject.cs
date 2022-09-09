using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* Object can only be dragged when initialize during gameplay */
public class DraggableObject : MonoBehaviour
{
    private Vector3 offset;
    private CursorController cursorController;
    private WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    private void Awake()
    {
        cursorController = FindObjectOfType<CursorController>();
    }

    public void DragObject()
    {
        offset = transform.position - cursorController.GetMouseWorldPosition();
        StartCoroutine(DragUpdate(this.gameObject));
    }

    private IEnumerator DragUpdate(GameObject clickedObject)
    {
        while (cursorController.controls.Mouse.Click.ReadValue<float>() != 0)
        {
            Vector3 pos = cursorController.GetMouseWorldPosition() + offset;
            transform.position = BuildingSystem.current.SnapCoordinateToGrid(pos);
            yield return waitForFixedUpdate;
        }
    }

}
