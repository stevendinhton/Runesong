using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map;
using UnityEngine.Tilemaps;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using static PathfindingSystem;

public class WorldManager : MonoBehaviour {
    
    public static MapWorld MapWorld { private set; get; }
    public static NativeArray<PathNode> PathNodesNA { private set; get; }
    public static Transform SelectionArea { private set; get;  }

    public int regionSize;
    public int worldSizeByRegions;
    public GameObject tilemapGameObject;
    public TileBase[] tile;
    public int2[] walls;
    public Transform selectionArea;

    private Tilemap tilemap;

    // Start is called before the first frame update
    void Start() {
        tilemap = tilemapGameObject.GetComponent<Tilemap>();
        SelectionArea = selectionArea;

        float startTime = Time.realtimeSinceStartup;
        CreateWorld();
        Debug.Log("Time(CreateWorld): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetWalls();
        Debug.Log("Time(SetWalls): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetTileMap();
        Debug.Log("Time(SetTileMap): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetPathNodes();
        Debug.Log("Time(SetPathNodes): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetMouseButtonDown(2)) {
            Debug.Log("Middle Mouse Button Down");

            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log(string.Format("Mouse click at [X: {0} Y: {0}]", pos.x, pos.y));

            float startTime = Time.realtimeSinceStartup;
            MapTile clickedTile = MapWorld.GetMapTileAt((int)pos.x, (int)pos.y);
            clickedTile.traversable = !clickedTile.traversable;
            clickedTile.materialSurface = clickedTile.traversable ? (ushort) 1 : (ushort) 2;
            Debug.Log("Time(1): " + ((Time.realtimeSinceStartup - startTime) * 1000f));

            UpdateTile(clickedTile);
        }
    }

    public void UpdateTile(MapTile newTile) {
        float startTime = Time.realtimeSinceStartup;
        int x = newTile.locationX;
        int y = newTile.locationY;
        MapWorld.SetTile(newTile);
        Debug.Log("Time(SetTile): " + ((Time.realtimeSinceStartup - startTime) * 1000f));

        UpdateTileMap(newTile);
        Debug.Log("Time(UpdateTileMap): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        UpdatePathNodes();
        Debug.Log("Time(UpdatePathNodes): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }

    private MapTile[] GetDefaultMapTiles(int regionSize) {
        int numTiles = regionSize * regionSize;

        MapTile[] mapTiles = new MapTile[numTiles];
        
        for (int i = 0; i < numTiles; i++) {
            MapTile baseTile = new MapTile();
            baseTile.traversable = true; // wallIndices.Contains(i)
            baseTile.materialGround = 0;
            baseTile.materialSurface = 1; // wallIndices.Contains(i) ? (ushort)2 : (ushort)1
            baseTile.locationX = i % regionSize;
            baseTile.locationY = i / regionSize;

            mapTiles[i] = baseTile;
        }

        return mapTiles;
    }

    private void CreateWorld() {
        MapRegion[] mapRegions = new MapRegion[worldSizeByRegions * worldSizeByRegions];

        for (int i = 0; i < mapRegions.Length; i++) {
            mapRegions[i] = new MapRegion {
                mapTiles = GetDefaultMapTiles(regionSize),
                regionSize = regionSize,
                locationX = 0,
                locationY = 0
            };
        }

        MapWorld = new MapWorld {
            mapRegions = mapRegions,
            regionSize = regionSize,
            mapHeightByRegions = 1,
            mapWidthByRegions = 1
        };
    }

    private void SetWalls() {
        for (int i = 0; i < walls.Length; i++) {
            MapTile tile = MapWorld.GetMapTileAt(walls[i].x, walls[i].y);
            tile.traversable = false;
            tile.materialSurface = 2;

            MapWorld.SetTile(tile);
        }
    }

    private void UpdateTileMap(MapTile newTile) {
        tilemap.SetTile(new Vector3Int(newTile.locationX, newTile.locationY, 0), tile[newTile.materialSurface]);
    }


    private void SetTileMap() {
        Vector3Int[] locations = new Vector3Int[MapWorld.GetTotalNumMapTiles()];
        TileBase[] tileArray = new TileBase[MapWorld.GetTotalNumMapTiles()];
        
        for (int i = 0; i < MapWorld.mapRegions.Length; i++) {
            MapTile[] mapTilesInRegion = MapWorld.mapRegions[i].mapTiles;

            for (int j = 0; j < mapTilesInRegion.Length; j++) {
                locations[i * mapTilesInRegion.Length + j] = new Vector3Int(mapTilesInRegion[j].locationX, mapTilesInRegion[j].locationY, 0);
                tileArray[i * mapTilesInRegion.Length + j] = tile[mapTilesInRegion[j].materialSurface];
            }
        }

        tilemap.GetComponent<Tilemap>().SetTiles(locations, tileArray);
    }

    private void SetPathNodes() {
        PathNodesNA = new NativeArray<PathNode>(MapWorld.GetPathNodes(), Allocator.Persistent);
    }

    private void UpdatePathNodes() {
        PathNodesNA.Dispose();
        SetPathNodes();
    }
}
