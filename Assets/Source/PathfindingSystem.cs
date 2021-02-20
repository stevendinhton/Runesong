using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Map;

public class PathfindingSystem : ComponentSystem {

    protected override void OnUpdate() {
        int2 gridSize = new int2(WorldManager.MapWorld.regionSize, WorldManager.MapWorld.regionSize);

        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.Temp);

        Entities.ForEach((Entity entity, DynamicBuffer<PathfindingRoute> pathRoute, ref PathfindingParams pathfindingParams) => {
            float startTime = UnityEngine.Time.realtimeSinceStartup;
            NativeArray<PathNode> pathNodes = new NativeArray<PathNode>(WorldManager.PathNodes, Allocator.TempJob);
            Debug.Log("Time(creating nativearray): " + ((UnityEngine.Time.realtimeSinceStartup - startTime) * 1000f));
            PathFinderJob pathfinderJob = new PathFinderJob {
                positionStart = pathfindingParams.startPosition,
                positionEnd = pathfindingParams.endPosition,
                gridSize = gridSize,
                entity = entity,
                routeFollow = GetComponentDataFromEntity<PathfindingRouteFollow>(),
                pathRoute = pathRoute,
                pathNodes = pathNodes
            };

            jobHandles.Add(pathfinderJob.Schedule());

            PostUpdateCommands.RemoveComponent<PathfindingParams>(entity);
        });

        JobHandle.CompleteAll(jobHandles);
    }

    [BurstCompile]
    private struct PathFinderJob : IJob {

        public int2 positionStart;
        public int2 positionEnd;
        public int2 gridSize;

        public Entity entity;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<PathfindingRouteFollow> routeFollow;

        [NativeDisableContainerSafetyRestriction]
        public DynamicBuffer<PathfindingRoute> pathRoute;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<PathNode> pathNodes;

        public void Execute() {

            int endNodeIndex = getNodeIndex(positionEnd.x, positionEnd.y, gridSize);

            if (!pathNodes[endNodeIndex].walkable) {
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = -1 };
                return;
            }

            NativeList<PathNode> openList = new NativeList<PathNode>(Allocator.Temp);
            NativeList<PathNode> closedList = new NativeList<PathNode>(Allocator.Temp);

            PathNode startNode = pathNodes[getNodeIndex(positionStart.x, positionStart.y, gridSize)];

            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(new int2(startNode.x, startNode.y), positionEnd);
            startNode.UpdateFCost();
            openList.Add(startNode);

            while (openList.Length > 0) {
                PathNode currentNode = getNodeWithLowestFCost(ref openList);
                closedList.Add(currentNode);

                if (currentNode.index == endNodeIndex) {
                    break;
                }

                NativeList<PathNode> eligibleNeighbours = getEligibleNeighbours(currentNode, pathNodes, gridSize);

                for (int i = 0; i < eligibleNeighbours.Length; i++) {
                    PathNode neighbourNode = eligibleNeighbours[i];

                    if (containsNode(closedList, neighbourNode)) {
                        continue;
                    }

                    int2 currentPosition = new int2(currentNode.x, currentNode.y);
                    int2 neighbourPosition = new int2(neighbourNode.x, neighbourNode.y);

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentPosition, neighbourPosition);
                    if (!containsNode(openList, neighbourNode) || tentativeGCost < neighbourNode.gCost) {
                        neighbourNode.cameFromNodeIndex = currentNode.index;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateDistanceCost(new int2(neighbourNode.x, neighbourNode.y), positionEnd);
                        neighbourNode.UpdateFCost();

                        if (!containsNode(openList, neighbourNode)) {
                            openList.Add(neighbourNode);
                        }
                    }
                }
            }

            pathRoute.Clear();
            
            if (!containsNode(closedList, pathNodes[endNodeIndex])) {
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = -1 };
            } else {
                setPath(closedList, endNodeIndex, pathRoute);
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = pathRoute.Length - 1 };
            }
            
            openList.Dispose();
            closedList.Dispose();
        }

        private void setPath(NativeList<PathNode> pathNodes, int endNodeIndex, DynamicBuffer<PathfindingRoute> route) {
            PathNode currentNode = getNodeInList(pathNodes, endNodeIndex);
            route.Add(new PathfindingRoute { position = new int2(currentNode.x, currentNode.y) });

            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = getNodeInList(pathNodes, currentNode.cameFromNodeIndex);
                route.Add(new PathfindingRoute { position = new int2(cameFromNode.x, cameFromNode.y) });
                currentNode = cameFromNode;
            }
        }

        private NativeList<PathNode> getEligibleNeighbours(PathNode centerNode, NativeArray<PathNode> pathNodes, int2 gridSize) {
            NativeArray<int2> adjacentOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            adjacentOffsetArray[0] = new int2(-1, 0); // Left
            adjacentOffsetArray[1] = new int2(+1, 0); // Right
            adjacentOffsetArray[2] = new int2(0, +1); // Up
            adjacentOffsetArray[3] = new int2(0, -1); // Down

            NativeArray<int2> diagonalOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            diagonalOffsetArray[0] = new int2(+1, +1); // Top Right
            diagonalOffsetArray[1] = new int2(-1, +1); // Top Left
            diagonalOffsetArray[2] = new int2(+1, -1); // Bottom Right
            diagonalOffsetArray[3] = new int2(-1, -1); // Bottom Left 

            NativeList<PathNode> eligibleNeighbours = new NativeList<PathNode>(Allocator.Temp);

            // iterate through all neighbours
            for (int i = 0; i < adjacentOffsetArray.Length; i++) {
                int2 offset = adjacentOffsetArray[i];
                int2 adjacentPosition = new int2(centerNode.x + offset.x, centerNode.y + offset.y);

                if (!IsPositionInsideGrid(adjacentPosition, gridSize)) {
                    continue;
                }

                int neighbourIndex = getNodeIndex(adjacentPosition.x, adjacentPosition.y, gridSize);

                if (pathNodes[neighbourIndex].walkable) {
                    eligibleNeighbours.Add(pathNodes[neighbourIndex]);
                }
            }

            for (int i = 0; i < diagonalOffsetArray.Length; i++) {
                int2 offset = diagonalOffsetArray[i];
                int2 diagonalPosition = new int2(centerNode.x + offset.x, centerNode.y + offset.y);
                int2 cornerPosition1 = new int2(centerNode.x, centerNode.y + offset.y);
                int2 cornerPosition2 = new int2(centerNode.x + offset.x, centerNode.y);

                if (!IsPositionInsideGrid(diagonalPosition, gridSize)) {
                    continue;
                }

                int neighbourIndex = getNodeIndex(diagonalPosition.x, diagonalPosition.y, gridSize);
                int cornerIndex1 = getNodeIndex(cornerPosition1.x, cornerPosition1.y, gridSize);
                int cornerIndex2 = getNodeIndex(cornerPosition2.x, cornerPosition2.y, gridSize);

                if (pathNodes[neighbourIndex].walkable &&
                    pathNodes[cornerIndex1].walkable &&
                    pathNodes[cornerIndex2].walkable) {
                    eligibleNeighbours.Add(pathNodes[neighbourIndex]);
                }
            }

            return eligibleNeighbours;
        }

        // will remove the node from nodes list
        private PathNode getNodeWithLowestFCost(ref NativeList<PathNode> nodes) {
            int lowest = 0;

            for (int i = 1; i < nodes.Length; i++) {
                if (nodes[i].fCost < nodes[lowest].fCost) {
                    lowest = i;
                }
            }

            PathNode cheapestNode = nodes[lowest];
            nodes.RemoveAtSwapBack(lowest);

            return cheapestNode;
        }

        private bool containsNode(NativeList<PathNode> list, PathNode node) {
            for (int i = 0; i < list.Length; i++) {
                if (list[i].index == node.index) {
                    return true;
                }
            }
            return false;
        }

        private PathNode getNodeInList(NativeList<PathNode> list, int nodeIndex) {
            for (int i = 0; i < list.Length; i++) {
                if (list[i].index == nodeIndex) {
                    return list[i];
                }
            }
            throw new Exception();
        }

        private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) {
            return
                gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }
    }

    private static int getNodeIndex(int x, int y, int2 gridSize) {
        return x + y * gridSize.x;
    }

    private static int CalculateDistanceCost(int2 aPosition, int2 bPosition) {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return 14 * math.min(xDistance, yDistance) + 10 * remaining;
    }

    public struct PathNode {
        public int x;
        public int y;
        public int index;

        public int gCost; // distance from starting node
        public int hCost; // heuristic distance from end node
        public int fCost; // g + h
        public bool walkable;

        public int cameFromNodeIndex;

        public void UpdateFCost() {
            fCost = gCost + hCost;
        }

        public void SetWalkable(bool walkable) {
            this.walkable = walkable;
        }
    }
}
