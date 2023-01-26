using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TrapShape
{
    I,
    O,
    T,
    J,
    L,
    S,
    Z,
    None,
}

[System.Serializable]
public struct Trap
{
    public TrapShape shape;
    public Vector2Int[] route { get; set;}
    public Vector2Int[,] wallKicks { get; set;}

    public void Initialize()
    {
        this.route = TrapData.Route[this.shape];
        this.wallKicks = TrapData.WallKicks[this.shape];
    }

    // set empty 
}