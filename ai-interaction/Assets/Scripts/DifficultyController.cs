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
            case 0: // Easy
                MainManager.Instance.spawnT = 1500;
                MainManager.Instance.initN = 2;
                MainManager.Instance.waveNumber = 0;
                MainManager.Instance.healthIncrement = 0;
                break;
            case 1: // Intermediate
                MainManager.Instance.spawnT = 1200;
                MainManager.Instance.initN = 2;
                MainManager.Instance.waveNumber = 1;
                MainManager.Instance.healthIncrement = 1;
                break;
            case 2: // Hard
                MainManager.Instance.spawnT = 900;
                MainManager.Instance.initN = 3;
                MainManager.Instance.waveNumber = 2;
                MainManager.Instance.healthIncrement = 2;
                break;
            default:
                break;
        }
    }
}
