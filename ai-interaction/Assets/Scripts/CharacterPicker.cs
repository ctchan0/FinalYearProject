using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPicker : MonoBehaviour
{
    public void SelectBarbarian()
    {
        MainManager.Instance.selectedClass = Class.Barbarian;
    }
    public void SelectKnight()
    {
        MainManager.Instance.selectedClass = Class.Knight;
    }
    public void SelectMage()
    {
        MainManager.Instance.selectedClass = Class.Mage;
    }
    public void SelectRogue()
    {
        MainManager.Instance.selectedClass = Class.Rogue;
    }

}
