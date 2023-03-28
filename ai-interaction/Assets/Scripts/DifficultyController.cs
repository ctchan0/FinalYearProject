using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Difficulty
{
    Easy,
    Intermediate,
    Hard,
}

public class DifficultyController : MonoBehaviour
{
    public void SelectDifficulty(int index)
    {
        switch (index)
        {
            case 0:
                MainManager.Instance.difficulty = Difficulty.Easy;
                break;
            case 1:
                MainManager.Instance.difficulty = Difficulty.Intermediate;
                break;
            case 2:
                MainManager.Instance.difficulty = Difficulty.Hard;
                break;
            default:
                break;
        }
    }
}
