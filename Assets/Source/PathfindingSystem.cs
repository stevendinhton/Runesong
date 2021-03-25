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
using Pathfinding;
using Map;

public class PathfindingSystem : ComponentSystem {

    private const int DiagonalCost = 14;
    private const int AdjacentCost = 10;
    private static readonly int2[] AdjacentOffsets = { new int2(1, 0), new int2(0, 1), new int2(-1, 0), new int2(0, -1) };
    private static readonly int2[] DiagonalOffsets = { new int2(1, 1), new int2(1, -1), new int2(-1, 1), new int2(-1, -1) };

    protected override void OnUpdate() {
        int2 gridSize = new int2(WorldManager.MapWorld.regionSize, WorldManager.MapWorld.regionSize);

        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.Temp);

        Entities.ForEach((Entity entity, DynamicBuffer<PathfindingRoute> pathRoute, ref PathfindingParams pathfindingParams) => {
            List<MapGrowth> mapGrowths = WorldManager.MapWorld.GetMapGrowths(pathfindingParams.growthCode);
            NativeArray<int2> targetLocations = new NativeArray<int2>(GetAllTargetsFromGrowth(mapGrowths), Allocator.TempJob);

            PathFinderJob pathfinderJob = new PathFinderJob {
                pathfindingType = pathfindingParams.pathfindingType,
                positionStart = pathfindingParams.startPosition,
                positionEnd = pathfindingParams.endPosition,
                gridSize = gridSize,
                entity = entity,
                routeFollow = GetComponentDataFromEntity<PathfindingRouteFollow>(),
                pathRoute = pathRoute,
                pathNodes = WorldManager.PathNodesNA,
                targets = targetLocations
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
        public PathfindingType pathfindingType; 

        public Entity entity;

        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<PathfindingRouteFollow> routeFollow;

        [NativeDisableContainerSafetyRestriction]
        public DynamicBuffer<PathfindingRoute> pathRoute;

        [ReadOnly]
        public NativeArray<PathNode> pathNodes;

        [DeallocateOnJobCompletion]
        public NativeArray<int2> targets;

        public void Execute() {

            if (pathfindingType == PathfindingType.ToSpot && 
                !pathNodes[getNodeIndex(positionEnd.x, positionEnd.y)].walkable) {
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = -1 };
                return;
            }

            if (SuccessfullyReachedTarget(positionStart)) {
                pathRoute.Clear();
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = -1 };
                return;
            }

            bool foundPath = false;

            NativeList<PathNode> openList = new NativeList<PathNode>(Allocator.Temp);
            NativeList<PathNode> closedList = new NativeList<PathNode>(Allocator.Temp);

            PathNode startNode = pathNodes[getNodeIndex(positionStart.x, positionStart.y)];

            startNode.gCost = 0;
            startNode.hCost = CalculateHeuristicCost(startNode.toInt2());
            startNode.UpdateFCost();
            openList.Add(startNode);

            while (openList.Length > 0) {
                PathNode currentNode = popNodeWithLowestFCost(ref openList);
                closedList.Add(currentNode);

                if (SuccessfullyReachedTarget(currentNode.toInt2())) {
                    foundPath = true;
                    break;
                }

                NativeList<PathNode> eligibleNeighbours = getEligibleNeighbours(currentNode, gridSize);

                for (int i = 0; i < eligibleNeighbours.Length; i++) {
                    PathNode neighbourNode;

                    if (containsNode(openList, eligibleNeighbours[i])) {
                        neighbourNode = getNodeInList(openList, eligibleNeighbours[i].toInt2());
                    } else {
                        neighbourNode = eligibleNeighbours[i];
                    }

                    if (containsNode(closedList, neighbourNode)) {
                        continue;
                    }

                    int2 currentPosition = new int2(currentNode.x, currentNode.y);
                    int2 neighbourPosition = new int2(neighbourNode.x, neighbourNode.y);

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentPosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.gCost) {
                        neighbourNode.cameFromNodeIndex = getNodeIndex(currentNode.x, currentNode.y);
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateHeuristicCost(neighbourNode.toInt2());
                        neighbourNode.UpdateFCost();

                        if (!containsNode(openList, neighbourNode)) {
                            openList.Add(neighbourNode);
                        } else {
                            updateNodeInList(ref openList, neighbourNode);
                        }
                    }
                }

                eligibleNeighbours.Dispose();
            }

            pathRoute.Clear();

            if (foundPath) {
                updatePathing(closedList);
            } else {
                routeFollow[entity] = new PathfindingRouteFollow { routeIndex = -1 };
            }
            openList.Dispose();
            closedList.Dispose();
        }
        private void setPathBuffer(NativeList<PathNode> closedList, DynamicBuffer<PathfindingRoute> route) {
            PathNode endNode = closedList[closedList.Length - 1];

            PathNode currentNode = getNodeInList(closedList, endNode.toInt2());
            route.Add(new PathfindingRoute { position = currentNode.toInt2() });

            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = getNodeInList(closedList, getPositionFromIndex(currentNode.cameFromNodeIndex));
                route.Add(new PathfindingRoute { position = new int2(cameFromNode.x, cameFromNode.y) });
                currentNode = cameFromNode;
            }
        }

        private void updatePathing(NativeList<PathNode> closedList) {
            setPathBuffer(closedList, pathRoute);
            routeFollow[entity] = new PathfindingRouteFollow { routeIndex = pathRoute.Length - 1 };
        }

        private NativeList<PathNode> getEligibleNeighbours(PathNode centerNode, int2 gridSize) {

            NativeList<PathNode> eligibleNeighbours = new NativeList<PathNode>(Allocator.Temp);

            // iterate through all neighbours
            for (int i = 0; i < AdjacentOffsets.Length; i++) {
                int2 offset = AdjacentOffsets[i];
                int2 adjacentPosition = new int2(centerNode.x + offset.x, centerNode.y + offset.y);

                if (!IsPositionInsideGrid(adjacentPosition, gridSize)) {
                    continue;
                }

                int neighbourIndex = getNodeIndex(adjacentPosition.x, adjacentPosition.y);

                if (pathNodes[neighbourIndex].walkable) {
                    eligibleNeighbours.Add(pathNodes[neighbourIndex]);
                }
            }

            for (int i = 0; i < DiagonalOffsets.Length; i++) {
                int2 offset = DiagonalOffsets[i];
                int2 diagonalPosition = new int2(centerNode.x + offset.x, centerNode.y + offset.y);
                int2 cornerPosition1 = new int2(centerNode.x, centerNode.y + offset.y);
                int2 cornerPosition2 = new int2(centerNode.x + offset.x, centerNode.y);

                if (!IsPositionInsideGrid(diagonalPosition, gridSize)) {
                    continue;
                }

                int neighbourIndex = getNodeIndex(diagonalPosition.x, diagonalPosition.y);
                int cornerIndex1 = getNodeIndex(cornerPosition1.x, cornerPosition1.y);
                int cornerIndex2 = getNodeIndex(cornerPosition2.x, cornerPosition2.y);

                if (pathNodes[neighbourIndex].walkable &&
                    pathNodes[cornerIndex1].walkable &&
                    pathNodes[cornerIndex2].walkable) {
                    eligibleNeighbours.Add(pathNodes[neighbourIndex]);
                }
            }

            return eligibleNeighbours;
        }

        // will remove the node from nodes list
        private PathNode popNodeWithLowestFCost(ref NativeList<PathNode> nodes) {
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

        private void updateNodeInList(ref NativeList<PathNode> list, PathNode newNode) {
            for(int i = 0; i < list.Length; i++) {
                if (list[i].x == newNode.x && list[i].y == newNode.y) {
                    list[i] = newNode;
                    break;
                }
            }
        }

        private bool containsNode(NativeList<PathNode> list, PathNode node) {
            for (int i = 0; i < list.Length; i++) {
                if (list[i].x == node.x && list[i].y == node.y) {
                    return true;
                }
            }
            return false;
        }

        private PathNode getNodeInList(NativeList<PathNode> list, int2 nodePosition) {
            for (int i = 0; i < list.Length; i++) {
                if (list[i].toInt2().Equals(nodePosition)) {
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

        private int getNodeIndex(int x, int y) {
            return x + y * gridSize.x;
        }
        private int2 getPositionFromIndex(int index) {
            return new int2(index % gridSize.x, index / gridSize.x);
        }
        private int CalculateHeuristicCost(int2 positionA) {
            if (pathfindingType == PathfindingType.ToSpot) {
                return CalculateDistanceCost(positionA, new int2(positionEnd.x, positionEnd.y));
            } else {
                int shortestDistance = CalculateDistanceCost(positionA, targets[0]);
                for (int i = 1; i < targets.Length; i++) {
                    int distance = CalculateDistanceCost(positionA, targets[i]);
                    if (distance < shortestDistance)
                        shortestDistance = distance;
                }
                return shortestDistance;
            }
        }
        private int CalculateDistanceCost(int2 positionA, int2 positionB) {
            int xDistance = math.abs(positionA.x - positionB.x);
            int yDistance = math.abs(positionA.y - positionB.y);
            int remaining = math.abs(xDistance - yDistance);
            return DiagonalCost * math.min(xDistance, yDistance) + AdjacentCost * remaining;
        }
        private bool SuccessfullyReachedTarget(int2 position) {
            if (pathfindingType == PathfindingType.ToSpot) {
                return position.Equals(positionEnd);
            } else {
                for  (int i = 0; i < targets.Length; i++) {
                    if (position.Equals(targets[i]))
                        return true;
                }
                return false;
            }
        }
    }

    public static int2[] GetAllTargetsFromGrowth(List<MapGrowth> growths) {
        int2[] output = new int2[growths.Count * 4];

        for (int i = 0; i < growths.Count; i++) {
            output[i * 4] = new int2(growths[i].locationX, growths[i].locationY) + AdjacentOffsets[0];
            output[i * 4 + 1] = new int2(growths[i].locationX, growths[i].locationY) + AdjacentOffsets[1];
            output[i * 4 + 2] = new int2(growths[i].locationX, growths[i].locationY) + AdjacentOffsets[2];
            output[i * 4 + 3] = new int2(growths[i].locationX, growths[i].locationY) + AdjacentOffsets[3];
        }

        return output;
    }

    public struct PathNode {
        public ushort x;
        public ushort y;

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

        public int2 toInt2() {
            return new int2(x, y);
        }
    }
}
