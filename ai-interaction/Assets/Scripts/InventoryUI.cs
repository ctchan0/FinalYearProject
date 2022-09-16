using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public InventoryItemUI itemPrefab;
    List<InventoryItemUI> listOfUIItems = new List<InventoryItemUI>();
    public void Initialize(int inventorysize)
    {
        for (int i = 0; i < inventorysize; i++)
        {
            var uiItem = Instantiate(itemPrefab, itemPrefab.transform.position, itemPrefab.transform.rotation);
            uiItem.transform.SetParent(this.transform);
            uiItem.transform.localScale = Vector3.one;
            listOfUIItems.Add(uiItem);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateData(int itemIndex, Sprite itemImage, int itemQuantity)
    {
        if (listOfUIItems.Count > itemIndex)
        {
            listOfUIItems[itemIndex].SetData(itemImage, itemQuantity);
        }
    }

    

}
