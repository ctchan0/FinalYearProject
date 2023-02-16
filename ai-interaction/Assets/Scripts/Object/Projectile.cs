using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 40f;
    public AdventurerAgent belonger { get; set;}
    // Start is called before the first frame update
    void Awake()
    {
        // GetComponent<Collider>().enabled = false;
    }

    void Start()
    {
        // Destroy once shoot
        Destroy(this.gameObject, 5f);
    }

    // Update is called once per frame
    void Update()
    { 
        transform.Translate(Vector3.forward * Time.deltaTime * speed);
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("Monster"))
        {
            var monster = other.gameObject.GetComponent<MonsterAgent>();
            belonger.HitTarget();
            belonger.DealDamage(monster, belonger.attack);
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            Destroy(this.gameObject);
        }
    }
}
