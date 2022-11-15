using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType type;
    public int colour; // 0: blue, 1: red, 2: yellow

    public Collider[] detector { get; set; }
    public Board board; 
    public int index;

    protected virtual void Awake()
    {
        detector = GetComponents<Collider>();
        EnableDetector(true); 
    }

    private void EnableDetector(bool enable)
    {
        if (detector == null) return;
        foreach (var d in detector)
        {
            d.enabled = enable;
        }
    }
}

public enum BlockType { monster, adventurer}
