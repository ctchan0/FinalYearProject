using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushBlock : MonoBehaviour
{
    public EnvController env;  //
    public int color;

    private void OnTriggerEnter(Collider other) 
    {
        // Touched goal.
        if (other.gameObject.CompareTag("Goal"))
        {
            // check if all blocks are pushed to goal
            env.AddPushedBlock(this);
            Destroy(this.gameObject);
        }
    }
}
