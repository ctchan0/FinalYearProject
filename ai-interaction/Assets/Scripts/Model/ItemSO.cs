using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu]
    public class ItemSO : ScriptableObject, IDestroyableItem
    {
        [field: SerializeField]
        public bool IsStackable { get; set; }

        public int ID => GetInstanceID(); // a key to identify

        [field: SerializeField]
        public int MaxStackSize { get; set; } = 1;

        [field: SerializeField]
        public string Name { get; set; }

        [field: SerializeField]
        [field: TextArea]
        public string Description { get; set; }

        [field: SerializeField]
        public Sprite ItemImage { get; set; }

        [field: SerializeField]
        public GameObject ItemPrefab { get; set; }

        [field: SerializeField]
        public List<ItemParameter> DefaultParametersList { get; set; }
    }

    public interface IDestroyableItem
    {
    }

    [Serializable]
    public struct ItemParameter : IEquatable<ItemParameter>
    {
        public ItemParameterSO itemParameter;  // => parameter name
        public float value;

        public bool Equals(ItemParameter other)
        {
            return other.itemParameter == itemParameter;
        }
    }
}
