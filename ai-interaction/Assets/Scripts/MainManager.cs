using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance { get; private set; }
    public Class selectedClass = Class.None;
    public int spawnT = 1500;
    public int initN = 2; // number of monsters at start
    public int waveNumber = 0; // number of monsters each wave
    public int healthIncrement = 0; // power of monsters

    public float volume = 1f;

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
