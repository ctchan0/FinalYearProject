using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;

/* Reference from https://www.youtube.com/watch?v=rKp9fWvmIww&t=53s */

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem current;
    public GridLayout gridLayout;
    private Grid grid;
    [SerializeField] private Tilemap mainTilemap;
    [SerializeField] private TileBase whiteTile;
    public GameObject[] prefabs;
    [SerializeField] private int currentPrefab;
    private PlaceableObject objectToPlace;
    private BuildingSystemControls buildingSystemControls;
    private CursorController cursorController;
    private GridGraph gridGraph;
    [SerializeField] private Transform aboveGround;
    [SerializeField] private float projectDistance = 0.5f; 

    private void Awake()
    {
        current = this;
        grid = gridLayout.gameObject.GetComponent<Grid>();
        buildingSystemControls = new BuildingSystemControls();
        cursorController = FindObjectOfType<CursorController>();

        gridGraph = AstarPath.active.data.gridGraph;
        mainTilemap.origin = Vector3Int.zero;
        mainTilemap.size = new Vector3Int(gridGraph.width, gridGraph.depth, 1);
        mainTilemap.ResizeBounds();

        currentPrefab = 0;
    }
    
    private void OnEnable()
    {
        buildingSystemControls.Enable();
    }

    private void OnDisable()
    {
        buildingSystemControls.Disable();
    }

    private void Update()
    {
        if (currentPrefab % prefabs.Length == 0)
            currentPrefab = 0;
            
        if (buildingSystemControls.BuildingSystem.CreateObject.triggered && !objectToPlace)
        {
            InitializeWithObject(prefabs[currentPrefab]);
        }

        if (!objectToPlace) // no any trigger 
        {
            return;
        }
        // else 
        if (buildingSystemControls.BuildingSystem.RotateObject.triggered)
        {
            objectToPlace.Rotate();
        }
        else if (buildingSystemControls.BuildingSystem.PlaceObject.triggered)
        {
            if (CanBePlaced(objectToPlace))
            {
                objectToPlace.Place();
                objectToPlace.gameObject.transform.SetParent(aboveGround);
                objectToPlace.gameObject.transform.position -= new Vector3(0, projectDistance, 0);

                Vector3Int start = gridLayout.WorldToCell(objectToPlace.GetStartPosition());
                TakeArea(start, objectToPlace.Size);
                objectToPlace = null;
            }
            else 
            {
                Destroy(objectToPlace.gameObject);
            }
        }
        else if (buildingSystemControls.BuildingSystem.DeleteObject.triggered)
        {
            Destroy(objectToPlace.gameObject);
        }
    }
    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
    {
        TileBase[] tileArray = new TileBase[area.size.x * area.size.y * area.size.z];
        int counter = 0;

        foreach (var pos in area.allPositionsWithin)
        {
            Vector3Int position = new Vector3Int(pos.x, pos.y, 0);
            tileArray[counter] = tilemap.GetTile(position);
            counter++;
        }
        return tileArray;
    }

    private bool CanBePlaced(PlaceableObject placeableObject)
    {
        BoundsInt area = new BoundsInt();
        area.position = gridLayout.WorldToCell(objectToPlace.GetStartPosition());
        area.size = placeableObject.Size;
        area.size = new Vector3Int(area.size.x + 1, area.size.y + 1, area.size.z);

        TileBase[] baseArray = GetTilesBlock(area, mainTilemap);

        foreach (var b in baseArray)
        {
            if (b == whiteTile)
            {
                return false;
            }
        }

        return true;
    }

    public void TakeArea(Vector3Int start, Vector3Int size)
    {
        mainTilemap.BoxFill(start, whiteTile, start.x, start.y, 
                            start.x + size.x, start.y + size.y);
    }

    public Vector3 SnapCoordinateToGrid(Vector3 position)
    {
        Vector3Int cellPos = gridLayout.WorldToCell(position);
        return grid.GetCellCenterWorld(cellPos) + new Vector3(0, 1f + projectDistance, 0);
    }

    public void InitializeWithObject(GameObject prefab)
    {
        Vector3 position = SnapCoordinateToGrid(cursorController.GetMouseWorldPosition());

        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        objectToPlace = obj.GetComponent<PlaceableObject>();
        obj.AddComponent<DraggableObject>();
    }

}
