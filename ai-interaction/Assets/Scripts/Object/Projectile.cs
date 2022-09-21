using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 40f;
    public bool shoot { get; set;}
    public AdventurerAgent belonger { get; set;}
    // Start is called before the first frame update
    void Awake()
    {
        shoot = false;
        GetComponent<Collider>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (shoot)
            transform.Translate(Vector3.forward * Time.deltaTime * speed);
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("Monster"))
        {
            var monster = other.gameObject.GetComponent<MonsterAgent>();
            belonger.DealDamage(monster, 1);
        }
        if (shoot)
            Destroy(this.gameObject);
    }
}
