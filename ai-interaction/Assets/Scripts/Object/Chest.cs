using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    private EnvController m_EnvController;
    Animator animator;
    public GameObject treasure;
    [SerializeField] bool closed = true;
    // Start is called before the first frame update
    void Awake()
    {
        m_EnvController = GetComponentInParent<EnvController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other) 
    {
        if (other.gameObject.CompareTag("Adventurer"))
        {
            if (other.gameObject.GetComponent<AdventurerAgent>().m_Class == Class.Rogue && this.closed)
            {
                animator.SetTrigger("Unlock");
                closed = false;
                m_EnvController.m_NumberOfRemainingResources--;
                StartCoroutine(TreasureSpawn(0.5f));
            }
        }
    }

    private IEnumerator TreasureSpawn(float waitTime) 
    {
        yield return new WaitForSeconds(waitTime);
        if (treasure)
        {
            var item = Instantiate(treasure, this.transform.position, this.transform.rotation);
            item.transform.SetParent(m_EnvController.transform);
        }
    }

    public void Reset()
    {
        animator.ResetTrigger("Unlock");
        closed = true;
    }

}
