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
    [SerializeField] GameObject[] resources;

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

    public void HandleHit(AdventurerAgent adventurer) 
    {
        timesHit++;
        int maxHits = afterHit.Length + 1;
        if (timesHit >= maxHits)
        {
            // able to get a resource
            adventurer.DiscoverResources();
            this.gameObject.SetActive(false);
            if (resources != null && resources.Length != 0)
            {
                int n = Random.Range(0, resources.Length);
                var item = Instantiate(resources[n], 
                            transform.position + resources[n].transform.position, 
                            Quaternion.identity);
                item.GetComponent<PushBlock>().env = m_EnvController;
                item.GetComponent<PushBlock>().color = n;
                item.transform.SetParent(m_EnvController.resource.transform);
            }
            else 
            {
                print(this.gameObject + ": Missing resource");
            }
        }
        else
        {
            m_EnvController.AddGroupReward(1, -0.1f);
            adventurer.AddReward(0.3f);
            ShowNextHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HitImmunity) return;
        other.transform.parent.
            TryGetComponent<AdventurerAgent>(out AdventurerAgent adventurer);
        if (!adventurer) return;
        HandleHit(adventurer);
    }
}

