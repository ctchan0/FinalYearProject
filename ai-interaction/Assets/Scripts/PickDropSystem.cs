using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class PickDropSystem : MonoBehaviour
{
    [SerializeField] Transform throwPos;
    public float throwForce = 2000f;
    [SerializeField] private InventorySO inventoryData;

    private void OnTriggerEnter(Collider other)
    {
        Item item = other.GetComponent<Item>();
        PickUpItem(item);
    }

    private void PickUpItem(Item item)
    {
        if (item != null && item.canPick)
        {
            if (inventoryData)
            {
                int reminder = inventoryData.AddItem(item.InventoryItem, item.Quantity);
                if (reminder == 0)
                    item.DestroyItem();
                else
                    item.Quantity = reminder;
            }
            else
                item.DestroyItem();
        }
    }

    public void ThrowItem(InventoryItem inventoryItem, int quantity)
    {
        var item = Instantiate(inventoryItem.item.ItemPrefab, throwPos.position, 
                                inventoryItem.item.ItemPrefab.transform.rotation);

        item.GetComponent<Item>().Quantity = quantity;
        
        item.GetComponent<Item>().AnimateItemThrow(throwPos, throwForce);
    }
    
}
