using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Monster : MonoBehaviour
{
    [SerializeField] Transform target;
    private Rigidbody rb;
    [SerializeField] float walkSpeed = 1;
    private Vector3 dirToGo;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        target = FindObjectOfType<BarbarianAgent>().transform;
    }
    void Update()
    {
    }

    void FixedUpdate()
    {
        dirToGo = target.position - transform.position;
        dirToGo.y = 0;
        rb.rotation = Quaternion.LookRotation(dirToGo);
        rb.MovePosition(transform.position + transform.forward * walkSpeed * Time.deltaTime);
    }

    public void SetRandomWalkSpeed()
    {
        walkSpeed = Random.Range(1f, 7f);
    }
}
