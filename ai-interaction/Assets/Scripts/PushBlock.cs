using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushBlock : MonoBehaviour
{
    private EnvController env;  //

    private void Awake()
    {
        env = GetComponentInParent<EnvController>();
    }

    void OnCollisionEnter(Collision col)
    {
        // Touched goal.
        if (col.gameObject.CompareTag("Goal"))
        {
            // check if all blocks are pushed to goal
            env.AddPushedBlock(this);
            Destroy(this.gameObject);
        }
    }
}
