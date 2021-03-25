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

    public int regionSize;
    public int worldSizeByRegions;
    public Tilemap groundTilemap;
    public Tilemap growthTilemap;
    public TileBase[] tileForGround;
    public TileBase[] tileForGrowth;
    public int2[] walls;
    public Transform selectionArea;

    // Start is called before the first frame update
    void Start() {
        float startTime = Time.realtimeSinceStartup;
        CreateWorld();
        Debug.Log("Time(CreateWorld): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetWalls();
        Debug.Log("Time(SetWalls): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetGroundTilemap();
        Debug.Log("Time(SetGroundTilemap): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        SetGrowthTilemap();
        SetPathNodes();
        Debug.Log("Time(SetPathNodes): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log("E Button Down");

            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log(string.Format("Mouse click at [X: {0} Y: {0}]", pos.x, pos.y));

            float startTime = Time.realtimeSinceStartup;
            MapTile clickedTile = MapWorld.GetMapTileAt((int)pos.x, (int)pos.y);
            clickedTile.traversable = !clickedTile.traversable;
            clickedTile.materialSurface = clickedTile.traversable ? (ushort) 1 : (ushort) 2;
            Debug.Log("Time(1): " + ((Time.realtimeSinceStartup - startTime) * 1000f));

            UpdateGroundTile(clickedTile);
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            Debug.Log("R Button Down");

            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log(string.Format("Mouse click at [X: {0} Y: {0}]", pos.x, pos.y));

            UpdateGrowthTile(new MapGrowth {
                locationX = (int) pos.x,
                locationY = (int) pos.y,
                quantity = 1,
                growthCode = 0
            });
        }
    }

    private void UpdateGroundTile(MapTile newTile) {
        float startTime = Time.realtimeSinceStartup;
        MapWorld.SetTile(newTile);
        Debug.Log("Time(SetTile): " + ((Time.realtimeSinceStartup - startTime) * 1000f));

        UpdateGroundTilemap(newTile);
        Debug.Log("Time(UpdateTileMap): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        PathNodesNA.Dispose();
        SetPathNodes();
        Debug.Log("Time(SetPathNodes): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }

    private void UpdateGrowthTile(MapGrowth newGrowth) {
        float startTime = Time.realtimeSinceStartup;
        MapWorld.SetGrowth(newGrowth);
        Debug.Log("Time(SetTile): " + ((Time.realtimeSinceStartup - startTime) * 1000f));

        UpdateGrowthTilemap(newGrowth);
        Debug.Log("Time(UpdateGrowthTilemap): " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }

    private MapTile[] GetDefaultMapTiles(int regionSize) {
        int numTiles = regionSize * regionSize;

        MapTile[] mapTiles = new MapTile[numTiles];
        
        for (int i = 0; i < numTiles; i++) {
            MapTile baseTile = new MapTile();
            baseTile.traversable = true;
            baseTile.materialGround = 0;
            baseTile.materialSurface = 1;
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
            mapWidthByRegions = 1,
            mapGrowths = new List<MapGrowth>()
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

    private void UpdateGroundTilemap(MapTile newTile) {
        groundTilemap.SetTile(new Vector3Int(newTile.locationX, newTile.locationY, 0), tileForGround[newTile.materialSurface]);
    }
    private void UpdateGrowthTilemap(MapGrowth newGrowth) {
        growthTilemap.SetTile(new Vector3Int(newGrowth.locationX, newGrowth.locationY, 0), tileForGrowth[newGrowth.growthCode]);
    }

    private void SetGroundTilemap() {
        Vector3Int[] locations = new Vector3Int[MapWorld.GetTotalNumMapTiles()];
        TileBase[] tileArray = new TileBase[MapWorld.GetTotalNumMapTiles()];
        
        for (int i = 0; i < MapWorld.mapRegions.Length; i++) {
            MapTile[] mapTilesInRegion = MapWorld.mapRegions[i].mapTiles;

            for (int j = 0; j < mapTilesInRegion.Length; j++) {
                locations[i * mapTilesInRegion.Length + j] = new Vector3Int(mapTilesInRegion[j].locationX, mapTilesInRegion[j].locationY, 0);
                tileArray[i * mapTilesInRegion.Length + j] = tileForGround[mapTilesInRegion[j].materialSurface];
            }
        }

        groundTilemap.GetComponent<Tilemap>().SetTiles(locations, tileArray);
    }

    private void SetGrowthTilemap() {
        Vector3Int[] locations = new Vector3Int[MapWorld.mapGrowths.Count];
        TileBase[] tileArray = new TileBase[MapWorld.mapGrowths.Count];

        for (int i = 0; i < MapWorld.mapGrowths.Count; i++) {
            locations[i] = new Vector3Int(MapWorld.mapGrowths[i].locationX, MapWorld.mapGrowths[i].locationY, 0);
            tileArray[i] = tileForGrowth[MapWorld.mapGrowths[i].growthCode];
        }

        groundTilemap.GetComponent<Tilemap>().SetTiles(locations, tileArray);
    }

    private void SetPathNodes() {
        PathNodesNA = new NativeArray<PathNode>(MapWorld.GetPathNodes(), Allocator.Persistent);
    }
}
