using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType type;
    public int colour;
    public int index { get; set;}
}

public enum BlockType { monster, adventurer}
