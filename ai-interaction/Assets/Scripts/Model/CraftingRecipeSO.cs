using System.Collections;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;

namespace Craft.Model
{
    [CreateAssetMenu]
    public class CraftingRecipeSO : ScriptableObject
    {
		[SerializeField] Recipe[] recipes;

		[System.Serializable]
		public class Recipe
		{
			public ItemSO item;
			public Ingredient[] ingredients;
		}
 
		[System.Serializable]
		public class Ingredient
		{
			public ItemSO ingredient;
			public int quantity;
		}

		public Recipe[] GetCraftingRecipes()
		{
			return recipes;
		}

        public Recipe GetRecipe(ItemSO item)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i].item.ID == item.ID)
                {
                    return recipes[i];
                }
            }
            return null;
        }
    }
}

