using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map;
using UnityEngine.Tilemaps;
using Unity.Mathematics;
using Unity.Collections;
using static PathfindingSystem;

public class WorldManager : MonoBehaviour {
    
    public static MapWorld MapWorld { private set; get; }
    //public static NativeArray<PathNode> PathNodes { private set; get; } 
    public static PathNode[] PathNodes { private set; get; } 

    public int regionSize;
    public int worldSizeByRegions;
    public GameObject tilemap;
    public TileBase[] tile;
    public int2[] walls;

    private Tile _baseTile;

    // Start is called before the first frame update
    void Start() {
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

            MapWorld.SetTileAt(tile, walls[i].x, walls[i].y);
        }
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
        //PathNodes = new NativeArray<PathNode>(MapWorld.GetPathNodes(), Allocator.Persistent);
        PathNodes = MapWorld.GetPathNodes();
    }
}
