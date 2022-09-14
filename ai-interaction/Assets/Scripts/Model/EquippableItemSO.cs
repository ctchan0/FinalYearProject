using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Inventory.Model
{
    [CreateAssetMenu]
    public class EquippableItemSO : ItemSO, IItemAction
    {
        public string ActionName => "Equip";

        [field: SerializeField]
        public AudioClip actionSFX { get; private set;}

        public bool PerformAction(GameObject adventurer, List<ItemParameter> itemState = null)
        {
            // Equip weapon
            return false; 
        }
    }
}