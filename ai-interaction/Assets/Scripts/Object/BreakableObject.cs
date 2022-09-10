using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
// using Inventory.Model;

public class BreakableObject : MonoBehaviour
{
    private EnvController m_EnvController;
    [SerializeField] int timesHit = 0;
    public bool HitImmunity = false;

    [SerializeField] MeshFilter currentMesh; 
    private Mesh defaultMesh;
    [SerializeField] Mesh[] afterHit;
    [SerializeField] GameObject resource;

    void Awake()
    {
        m_EnvController = GetComponentInParent<EnvController>();
    }
    void Start()
    {
        defaultMesh = currentMesh.mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Respawn()
    {
        this.gameObject.SetActive(true);
        timesHit = 0;
        currentMesh.mesh = defaultMesh;
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
            Debug.LogError("Tree mesh is missing from array" + this.gameObject.name);
        }
    }

    private void HandleHit(AdventurerAgent agent) 
    {
        timesHit++;
        int maxHits = afterHit.Length + 1;
        if (timesHit >= maxHits)
        {
            print("Successfully chopped a tree");
            agent.AddReward(0.5f);
            
            m_EnvController.m_NumberOfRemainingResources--;
            this.gameObject.SetActive(false);
            if (resource)
                    Instantiate(resource, 
                        transform.position + resource.transform.position, 
                        Quaternion.identity);
        }
        else
        {
            agent.AddReward(0.1f);
            ShowNextHit();
        }
    }

    public bool CloseTo(GameObject target)
    {
        if ((target.transform.position - this.transform.position).magnitude < 1f)
        {
            return true;
        }
        else return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tag == "Breakable")
        {
            if (HitImmunity) return;
            other.transform.parent.
                TryGetComponent<AdventurerAgent>(out AdventurerAgent agent);
            if (!agent) return;
            HandleHit(agent);
        }
    }
}

