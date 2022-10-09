using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType type;
    public int colour;
    public int index { get; set;}
    public int tempMatch = 0;
    public Collider[] detector { get; set; }
    public PieceAgent owner; 

    private void Awake()
    {
        detector = GetComponents<Collider>();
        if (type == BlockType.adventurer)
            EnableDetector(false);
        else if (type == BlockType.monster)
            EnableDetector(true);
    }

    public void EnableDetector(bool enable)
    {
        if (detector == null) return;
        foreach (var d in detector)
        {
            d.enabled = enable;
        }
    }
    
    private void OnTriggerEnter(Collider other) 
    {
        if (owner)
        {
            if (this.colour == 1)
            {
                if (other.gameObject.CompareTag("Blue") || other.gameObject.CompareTag("BlueMonster"))
                {
                    tempMatch++;
                    owner.LinkBlock();
                }
            }
            else if (this.colour == 2)
            {
                if (other.gameObject.CompareTag("Red") || other.gameObject.CompareTag("RedMonster"))
                {
                    tempMatch++;
                    owner.LinkBlock();
                }
            }  
            else if (this.colour == 3)
            {
                if (other.gameObject.CompareTag("Yellow") || other.gameObject.CompareTag("YellowMonster"))
                {
                    tempMatch++;
                    owner.LinkBlock();
                }
            }  
        }
    }


}

public enum BlockType { monster, adventurer}
