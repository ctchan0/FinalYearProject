using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] Image itemImage;
    [SerializeField] TMP_Text quantityTxt;

    private void Awake() 
    {
        ResetData();
    }

    public void SetData(Sprite sprite, int quantity)
    {
        itemImage.gameObject.SetActive(true);
        itemImage.sprite = sprite;
        quantityTxt.text = quantity + "";
    }

    public void ResetData()
    {
        itemImage.gameObject.SetActive(false);
    }

    public bool Empty()
    {
        return itemImage.gameObject.activeSelf == false;
    }
}
