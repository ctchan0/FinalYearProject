using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inventory.UI;

public class MouseFollower : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] UIInventoryItem item;
    private CursorController cursorController;

    private void Awake() 
    {
        canvas = transform.root.GetComponent<Canvas>();
        item = GetComponentInChildren<UIInventoryItem>();
        cursorController = FindObjectOfType<CursorController>();
    }

    public void SetData(Sprite sprite, int quantity)
    {
        item.SetData(sprite, quantity);
    }

    private void Update() 
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            cursorController.GetCursorPosition(),
            canvas.worldCamera,
            out position
        );
        
        transform.position = canvas.transform.TransformPoint(position);
    }

    public void Toggle(bool val) 
    {
        // Debug.Log($"Item toggled {val}");
        gameObject.SetActive(val);
    }


}
