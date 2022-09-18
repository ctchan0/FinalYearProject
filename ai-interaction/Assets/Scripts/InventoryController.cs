using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Inventory.Model;
using System.Linq;

public class InventoryController : MonoBehaviour
{
    public InputAction inventoryControls;
    private InventoryUI m_InventoryUI;
    private bool display = true;

    public int Size = 3;
    public List<InventoryItem> inventoryItemsList = new List<InventoryItem>();

    private AudioSource m_AudioSource;

    private EnvController m_EnvController;

    private void Awake()
    {
        Initialize();
        m_InventoryUI = GetComponentInChildren<InventoryUI>();
        if (m_InventoryUI)
        {
            inventoryControls.performed += _ => Display();
            m_InventoryUI.Initialize(Size);
            InformAboutChanges();
        }
        else
        {
            print(this.gameObject + ": Missing InventoryUI");
        }

        m_AudioSource = GetComponent<AudioSource>();
        m_EnvController = GetComponentInParent<EnvController>();
        if (!m_AudioSource)
            print(this.gameObject + ": Missing AudioSource");
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
            m_InventoryUI.Show();
        else 
            m_InventoryUI.Hide();
        display = !display;
    }

    #endregion

    public void Initialize()
    {
        int init = inventoryItemsList.Count;
        for (int i = init; i < Size; i++) {
            inventoryItemsList.Add(InventoryItem.GetEmptyItem());
        }
    }

    public void Reset()
    {
        if (inventoryItemsList.Count == 0) return;
        for (int i = 0; i < Size; i++) {
            inventoryItemsList[i] = InventoryItem.GetEmptyItem();
        }
        InformAboutChanges();
    }

    public void Clear() // clear inventory when adventurer is dead
    {
        foreach (var item in inventoryItemsList)
        {
            if (!item.IsEmpty) // always check if item is empty
            {
                var randomPosX = Random.Range(-0.5f, 0.5f);
                var randomPosZ = Random.Range(-0.5f, 0.5f);
                var dropItem = Instantiate(item.item.ItemPrefab, 
                                            this.transform.position + new Vector3(randomPosX, 0f, randomPosZ), 
                                            Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0));
                dropItem.transform.SetParent(m_EnvController.transform);
                dropItem.GetComponent<Item>().Quantity = item.quantity;
            }
        }
        Reset();
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

    public void PerformItemAction(int itemIndex)
    {
        InventoryItem inventoryItem = GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty) return;

        IDestroyableItem destroyableItem = inventoryItem.item as IDestroyableItem;
        if (destroyableItem != null)
        {
            RemoveItem(itemIndex, 1);
        }
        
        IItemAction itemAction = inventoryItem.item as IItemAction;
        if (itemAction != null)
        {
            itemAction.PerformAction(this.gameObject, inventoryItem.itemState);
            m_AudioSource.PlayOneShot(itemAction.actionSFX);
        }
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

    public bool CanUseItem(int itemIndex)
    {
        InventoryItem inventoryItem = GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty) return false;
        IItemAction itemAction = inventoryItem.item as IItemAction;
        if (itemAction == null) return false;
        else return true;
    }

    public int ExistsInInventory(ItemSO item, int quantity)
    {
        int count = quantity;
        for (int i = 0; i < inventoryItemsList.Count; i++)
        {
            if (inventoryItemsList[i].IsEmpty) continue;

            if (inventoryItemsList[i].item.ID == item.ID)
            {
                if (inventoryItemsList[i].quantity >= quantity)
                    return 0; // all items are found
                else 
                    count -= inventoryItemsList[i].quantity;
            }
        }
        return count; // return remaining number of items not found
    }

    private void InformAboutChanges()
    {
        if (!m_InventoryUI)
        {
            print(this.gameObject + ": Missing InventoryUI");
            return;
        }
        int i = 0;
        foreach (var item in inventoryItemsList)
        {
            if (!item.IsEmpty)
                m_InventoryUI.UpdateData(i, item.item.ItemImage, item.quantity);
            else 
                m_InventoryUI.ClearData(i);
            i++;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<Item>(out Item item))
        {
            if (item.canPick && GetComponent<AdventurerAgent>().isDead == false)
            {
                this.GetComponent<AdventurerAgent>().CollectResources();
                int reminder = AddItem(item.InventoryItem, item.Quantity);
                if (reminder > 0)
                {
                    item.Quantity = reminder;
                }
                else
                {
                    item.PickUp();
                }
            }
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


