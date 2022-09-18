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

    [SerializeField] string toolName;

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

    private void HandleHit(AdventurerAgent adventurer) 
    {
        timesHit++;
        int maxHits = afterHit.Length + 1;
        if (timesHit >= maxHits)
        {
            // able to get a resource
            adventurer.DiscoverResources();
            this.gameObject.SetActive(false);
            if (resource)
            {
                var item = Instantiate(resource, 
                            transform.position + resource.transform.position, 
                            Quaternion.identity);
                item.transform.SetParent(m_EnvController.transform);
            }
            else 
            {
                print(this.gameObject + ": Missing resource");
            }
        }
        else
        {
            m_EnvController.AddGroupReward(1, -0.1f);
            adventurer.AddReward(0.2f);
            ShowNextHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(toolName))
        {
            if (HitImmunity) return;
            other.transform.parent.
                TryGetComponent<AdventurerAgent>(out AdventurerAgent adventurer);
            if (!adventurer) return;
            HandleHit(adventurer);
        }
    }
}

