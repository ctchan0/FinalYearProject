using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class Item : MonoBehaviour
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }

    [field: SerializeField]
    public int Quantity { get; set; } = 1;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private float pickDuration = 0.3f;
    // [SerializeField] private float dropDuration = 0.7f;

    public bool canPick = true; // prevent pick up trigger

    private void Awake()
    {
        
    }
    private void Start() 
    {
        
    }

    public void PickUp()
    {
        GetComponent<Collider>().enabled = false;
        audioSource.Play();
        StartCoroutine(AnimateItemScale(transform.localScale, Vector3.zero, pickDuration));
    }

    private IEnumerator AnimateItemScale(Vector3 startScale, Vector3 endScale, float duration)
    {
        float currentTime = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, currentTime / duration);
            yield return null;
        }
        if (endScale == Vector3.zero)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("Projectile")) return;
        
        if (other.TryGetComponent<AdventurerAgent>(out AdventurerAgent adventurer))
        {
            if (canPick && adventurer.isDead == false)
            {
                adventurer.CollectResources();
                int reminder = adventurer.GetComponent<InventoryController>().AddItem(this.InventoryItem, this.Quantity);
                if (reminder > 0)
                {
                    this.Quantity = reminder;
                }
                else
                {
                    PickUp();
                }
            }
        }
    }


    public void ClearItem()
    {
        Destroy(this.gameObject);
    }
}
