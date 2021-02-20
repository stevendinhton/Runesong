﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using static PathfindingSystem;

namespace Map {

    public struct MapTile {

        public bool traversable;
        public int locationY;
        public int locationX;

        public ushort materialGround;  // type of tile on the ground (dirt, rock, etc.)
        public ushort materialSurface; // type of tile on the surface (wood floor, stone floor, etc.)

        public int healthSurface;
    }
    
    public struct MapRegion {

        // number of mapTiles is regionSize ^ 2
        public MapTile[] mapTiles; 
        public int regionSize;

        // bottom-left-most location
        public int locationY; 
        public int locationX;

        private int GetTileIndex(int x, int y) {
            return (y - locationY) * regionSize + x - locationX;
        }

        public MapTile GetMapTileAt(int x, int y) {
            return mapTiles[GetTileIndex(x, y)];
        }

        public void SetTileAt(MapTile newTile, int x, int y) {
            mapTiles[GetTileIndex(x, y)] = newTile;
        }
    }

    public struct MapWorld {

        public MapRegion[] mapRegions;
        public int regionSize;
        public int mapWidthByRegions;
        public int mapHeightByRegions;
        
        private int GetRegionIndex(int x, int y) {
            int regionLocationX = x / regionSize;
            int regionLocationY = y / regionSize;

            return regionLocationY * mapWidthByRegions + regionLocationX;
        }

        public int GetTotalNumMapRegions() {
            return mapWidthByRegions * mapHeightByRegions;
        }

        public int GetTotalNumMapTiles() {
            return GetTotalNumMapRegions() * regionSize * regionSize;
        }

        public MapRegion GetMapRegionAt(int x, int y) {
            return mapRegions[GetRegionIndex(x, y)];
        }

        public MapTile GetMapTileAt(int x, int y) {
            return GetMapRegionAt(x, y).GetMapTileAt(x, y);
        }

        public void SetTileAt(MapTile newTile, int x, int y) {
            MapRegion region = GetMapRegionAt(x, y);
            region.SetTileAt(newTile, x, y);

            mapRegions[GetRegionIndex(x, y)] = region;
        }

        public PathNode[] GetPathNodes() {
            PathNode[] pathNodes = new PathNode[regionSize * regionSize]; // todo multiple regions

            // set up all path nodes
            for (int x = 0; x < regionSize; x++) {
                for (int y = 0; y < regionSize; y++) {
                    PathNode pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = x + y * regionSize;
                    pathNode.walkable = GetMapTileAt(x, y).traversable;
                    pathNode.cameFromNodeIndex = -1;

                    pathNodes[pathNode.index] = pathNode;
                }
            }

            //Debug.Log("Time(getPathNodeArray total): " + ((UnityEngine.Time.realtimeSinceStartup - startTime) * 1000f));
            return pathNodes;
        }
    }
}
