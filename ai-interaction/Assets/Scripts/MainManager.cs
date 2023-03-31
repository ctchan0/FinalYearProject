using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance { get; private set; }
    public Class selectedClass;
    public int spawnT;
    public int initN; // number of monsters at start
    public bool hasWave; // number of monsters each wave
    public int healthIncrement; // power of monsters

    private void Awake()
    {
        // Allow one main manager only
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

    }
}
