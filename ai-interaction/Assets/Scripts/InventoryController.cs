using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Inventory.Model;
using System.Linq;

public class InventoryController : MonoBehaviour
{
    public InputAction inventoryControls;
    public InventoryUI inventoryUI;
    private bool display = true;

    public List<InventoryItem> inventoryItemsList = new List<InventoryItem>();
    public int Size = 3;

    private void Awake()
    {
        Initialize();
        if (inventoryUI)
        {
            inventoryControls.performed += _ => Display();
            inventoryUI.GetComponent<InventoryUI>().Initialize(Size);
        }
    }

    #region DisplayInvenotry
    private void OnEnable()
    {
        inventoryControls.Enable();
    }

    private void OnDisbale()
    {
        inventoryControls.Disable();
    }
    private void Display()
    {
        if (!display)
            inventoryUI.Show();
        else 
            inventoryUI.Hide();
        display = !display;
    }

    #endregion

    public void Initialize()
    {
        int init = inventoryItemsList.Count;
        if (init == 0)
            InformAboutChanges();
        for (int i = init; i <= Size - init; i++) {
            inventoryItemsList.Add(InventoryItem.GetEmptyItem());
        }
    }

    #region AddItem

    public int AddItem(ItemSO item, int quantity, List<ItemParameter> itemState = null) 
    {
        if (item.IsStackable == false) 
        {
            // Assign to empty inventory slot
            while (quantity > 0 && !IsInventoryFull())
            {
                quantity -= AddItemToFirstFreeSlot(item, 1, itemState);
            }
            InformAboutChanges();
            return quantity;         
        }
        // else;
        quantity = AddStackableItem(item, quantity);
        InformAboutChanges();
        return quantity;
    }
    private bool IsInventoryFull()
        => inventoryItemsList.Where(item => item.IsEmpty).Any() == false;

    private int AddItemToFirstFreeSlot(ItemSO item, int quantity, List<ItemParameter> itemState = null)
    {
        InventoryItem newItem = new InventoryItem
        {
            item = item,
            quantity = quantity,
            itemState = new List<ItemParameter>(itemState == null ? item.DefaultParametersList : itemState)
        };
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].IsEmpty)
            {
                inventoryItemsList[i] = newItem;
                return quantity;
            }
        }
        return 0;
    }

    private int AddStackableItem(ItemSO item, int quantity)
    {
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].IsEmpty) 
                continue;
            if (inventoryItemsList[i].item.ID == item.ID)
            {
                int amountPossiblleToTake = inventoryItemsList[i].item.MaxStackSize - inventoryItemsList[i].quantity;

                if (quantity > amountPossiblleToTake)
                {
                    inventoryItemsList[i] = inventoryItemsList[i].ChangeQuantity(inventoryItemsList[i].item.MaxStackSize);
                    quantity -= amountPossiblleToTake;
                }
                else
                {
                    inventoryItemsList[i] = inventoryItemsList[i].ChangeQuantity(inventoryItemsList[i].quantity + quantity);
                    InformAboutChanges();
                    return 0;
                }
            }
        }
        while (quantity > 0 && !IsInventoryFull())
        {
            int newQuantity = Mathf.Clamp(quantity, 0, item.MaxStackSize);
            quantity -= newQuantity;
            AddItemToFirstFreeSlot(item, newQuantity);
        }
        return quantity;
    }

    #endregion

    #region RemoveItem
    public void RemoveItem(int itemIndex, int amount)
    {
        if (inventoryItemsList.Count > itemIndex)
        {
            if (inventoryItemsList[itemIndex].IsEmpty)
                return;
            int reminder = inventoryItemsList[itemIndex].quantity - amount;
            if (reminder <= 0)
                inventoryItemsList[itemIndex] = InventoryItem.GetEmptyItem();
            else
                inventoryItemsList[itemIndex] = inventoryItemsList[itemIndex]
                    .ChangeQuantity(reminder);
            InformAboutChanges();
        }
    }

    public void RemoveItem(ItemSO item, int quantity)
    {
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].item.ID == item.ID)
            {
                if (quantity <= inventoryItemsList[i].quantity)
                {
                    RemoveItem(i, quantity);
                    quantity = 0;
                }
                else
                {
                    RemoveItem(i, inventoryItemsList[i].quantity);
                    quantity -= inventoryItemsList[i].quantity;
                }
            }
            if (quantity == 0)
                return;
        }
        Debug.Log("Cannot remove all items");
    }

    #endregion

    public void SwapItems(int itemIndex_1, int itemIndex_2)
    {
        InventoryItem item1 = inventoryItemsList[itemIndex_1];
        inventoryItemsList[itemIndex_1] = inventoryItemsList[itemIndex_2];
        inventoryItemsList[itemIndex_2] = item1;
        InformAboutChanges();
    }
    public Dictionary<int, InventoryItem> GetCurrentInventoryState()
    {
        Dictionary<int, InventoryItem> returnValue = new Dictionary<int, InventoryItem>();
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].IsEmpty) continue;
            returnValue[i] = inventoryItemsList[i];
        }
        return returnValue;
    }

    public InventoryItem GetItemAt(int itemIndex)
    {
        return inventoryItemsList[itemIndex];
    }

    public int ExistsInInventory(ItemSO item, int quantity)
    {
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].IsEmpty) continue;

            if (inventoryItemsList[i].item.ID == item.ID)
            {
                if (inventoryItemsList[i].quantity >= quantity)
                    return i;
                else 
                    quantity -= inventoryItemsList[i].quantity;
            }
        }
        return -1;
    }

    private void InformAboutChanges()
    {
        int i = 0;
        foreach (var item in inventoryItemsList)
        {
            if (!item.IsEmpty)
                inventoryUI.UpdateData(i, item.item.ItemImage, item.quantity);
            i++;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<Item>(out Item item))
        {
            item.PickUp();
            AddItem(item.InventoryItem, item.Quantity);
        }
    }

}
[System.Serializable]
public struct InventoryItem // Struct can not be set to null, but can set to be empty by method
{
    public int quantity;
    public ItemSO item;
    public List<ItemParameter> itemState;
    public bool IsEmpty => item == null;

    public InventoryItem ChangeQuantity(int newQuantity)
    {
        return new InventoryItem
        {
            item = this.item,
            quantity = newQuantity,
            itemState = new List<ItemParameter>(this.itemState),
        };
    }

    public static InventoryItem GetEmptyItem()
    {
        return new InventoryItem
        {
            item = null,
            quantity = 0,
            itemState = new List<ItemParameter>(),
        };
    }
}


