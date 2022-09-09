using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class PlaceableObject : MonoBehaviour
{
    [Header("(Set true if exists before play)")]
    public bool isPlaced; 
    public Vector3Int Size { get; private set; }
    private Vector3[] Vertices;
    [SerializeField] private BoxCollider boxCollider;

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (isPlaced)
            boxCollider.enabled = true;
        GetColliderVertexPositionsLocal();
        CalculateSizeInCells();
    }

    private void GetColliderVertexPositionsLocal()
    {
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        Vertices = new Vector3[4];
        Vertices[0] = collider.center + new Vector3(-collider.size.x, -collider.size.y, -collider.size.z) * 0.5f;
        Vertices[1] = collider.center + new Vector3(collider.size.x, -collider.size.y, -collider.size.z) * 0.5f;
        Vertices[2] = collider.center + new Vector3(collider.size.x, -collider.size.y, collider.size.z) * 0.5f;
        Vertices[3] = collider.center + new Vector3(-collider.size.x, -collider.size.y, collider.size.z) * 0.5f;
    }

    private void CalculateSizeInCells()
    {
        Vector3Int[] vertices = new Vector3Int[Vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(Vertices[i]);
            vertices[i] = BuildingSystem.current.gridLayout.WorldToCell(worldPos);

            Size = new Vector3Int(Mathf.Abs((vertices[0] - vertices[1]).x), 
                                    Mathf.Abs((vertices[0] - vertices[3]).y),
                                    1);
        }
    }

    public Vector3 GetStartPosition()
    {
        return transform.TransformPoint(Vertices[0]);
    }

    public virtual void Place()
    {
        DraggableObject drag = gameObject.GetComponent<DraggableObject>();
        Destroy(drag);

        isPlaced = true;
        boxCollider.enabled = true;
        AstarPath.active.UpdateGraphs(boxCollider.bounds);

        // invoke event of Placement
    }

    public void Rotate()
    {
        transform.Rotate(new Vector3(0, 90, 0));
        Size = new Vector3Int(Size.y, Size.x, 1);

        Vector3[] vertices = new Vector3[Vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vertices[(i + 1) % Vertices.Length];
        }
        Vertices = vertices;
    }

}
