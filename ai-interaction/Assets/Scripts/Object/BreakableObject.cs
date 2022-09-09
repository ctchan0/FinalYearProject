using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
// using Inventory.Model;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] int timesHit = 0;
    public bool HitImmunity = true;

    [SerializeField] MeshFilter currentMesh; 
    [SerializeField] Mesh[] afterHit;

    [SerializeField] GameObject resource;

    // public ItemSO exploitTool;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ShowNextHit()
    {
        int meshIndex = timesHit - 1;
        if (afterHit[meshIndex] != null)
        {
            currentMesh.mesh = afterHit[meshIndex];
        }
        else
        {
            Debug.LogError("Tree mesh is missing from array" + gameObject.name);
        }
    }

    private void HandleHit()
    {
        timesHit++;
        int maxHits = afterHit.Length + 1;
        if (timesHit >= maxHits)
        {
            Destroy(gameObject);
            if (resource)
                Instantiate(resource, transform.position + resource.transform.position, Quaternion.identity);
        }
        else
        {
            ShowNextHit();
        }
    }
}

