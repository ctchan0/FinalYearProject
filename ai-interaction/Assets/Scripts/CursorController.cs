using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    public Texture2D cursor;
    public Texture2D cursorClicked;

    public CursorControls controls;

    private Camera mainCamera;

    [SerializeField] GameObject clickedObject; 

    private void Awake() {
        controls = new CursorControls();
        ChangeCursor(cursor);
        Cursor.lockState = CursorLockMode.Confined;

        mainCamera = Camera.main;
    }

    private void OnEnable() {
        controls?.Enable();
    }

    private void OnDisable() {
        controls?.Disable();
    }

    private void Start() {
        controls.Mouse.Click.started += _ => StartedClick();
        // set to be trigger when mouse released (trigger behaviour in input action)
        controls.Mouse.Click.performed += _ => EndedClick(); 
    }

    private void StartedClick() {
        ChangeCursor(cursorClicked);

        clickedObject = GetClickedObject();
        if (clickedObject)
        {
            /* Case1 */
            if (clickedObject.tag == "MonsterSpawner")
            {
                Debug.Log("Spawn monster at " + GetMouseWorldPosition());
                clickedObject.GetComponentInParent<Board>().SpawnMonsterByClick(GetMouseWorldPosition());
            }
        }
    }

    private void EndedClick() {
        ChangeCursor(cursor);
        clickedObject = null;
    }

    private void ChangeCursor(Texture2D cursorType) {
        // Vector2 hotspot = new Vector2(cursorType.width/2, cursorType.height/2);
        Cursor.SetCursor(cursorType, Vector2.zero, CursorMode.Auto);
    }

    public GameObject GetClickedObject() {
        Ray ray = mainCamera.ScreenPointToRay(controls.Mouse.Position.ReadValue<Vector2>());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            if (hit.collider) {
                return hit.collider.gameObject;
            }
            return null;
        }
        return null;
    }

    public Vector3 GetMouseWorldPosition() {
        Ray ray = mainCamera.ScreenPointToRay(controls.Mouse.Position.ReadValue<Vector2>());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            if (hit.collider) {
                return hit.point;
            }
            return Vector3.zero;
        }
        return Vector3.zero;
    }

    public Vector2 GetCursorPosition() {
        return controls.Mouse.Position.ReadValue<Vector2>();
    }
}
