using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

public class Pathfinder : MonoBehaviour {

    void Start() {

        float startTime = Time.realtimeSinceStartup;

        int findPathJobCount = 5;
        NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(findPathJobCount, Allocator.TempJob);

        for (int i = 0; i < findPathJobCount; i++) {
            PathFinderJob pathJob = new PathFinderJob {
                positionStart = new int2(0, 0),
                positionEnd = new int2(1023, 1023),
                gridSize = new int2(1024, 1024)
            };
            jobHandleArray[i] = pathJob.Schedule();
            Debug.Log("Creating Job Time: " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        }

        JobHandle.CompleteAll(jobHandleArray);
        Debug.Log("Total Time: " + ((Time.realtimeSinceStartup - startTime) * 1000f));
        jobHandleArray.Dispose();
    }

    [BurstCompile]
    private struct PathFinderJob : IJob {

        public int2 positionStart;
        public int2 positionEnd;
        public int2 gridSize;

        public void Execute() {
            NativeArray<PathNode> pathNodes = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);

            // set up all path nodes
            for (int x = 0; x < gridSize.x; x++) {
                for (int y = 0; y < gridSize.y; y++) {
                    PathNode pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = x + y * gridSize.x;

                    pathNode.walkable = true;
                    pathNode.cameFromNodeIndex = -1;

                    pathNodes[pathNode.index] = pathNode;
                }
            }

            NativeList<PathNode> openList = new NativeList<PathNode>(Allocator.Temp);
            NativeList<PathNode> closedList = new NativeList<PathNode>(Allocator.Temp);

            PathNode startNode = pathNodes[getNodeIndex(positionStart.x, positionStart.y, gridSize)];
            int endNodeIndex = getNodeIndex(positionEnd.x, positionEnd.y, gridSize);

            startNode.gCost = 0;
            startNode.UpdateFCost();
            openList.Add(startNode);

            while (openList.Length > 0) {
                PathNode currentNode = getNodeWithLowestFCost(ref openList);
                closedList.Add(currentNode);

                if (currentNode.index == endNodeIndex) {
                    // todo: return the path
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
                        pathNodes[neighbourNode.index] = neighbourNode;

                        if (!containsNode(openList, neighbourNode)) {
                            openList.Add(neighbourNode);
                        }
                    }
                }
            }

            NativeList<int2> path = getPath(pathNodes, pathNodes[endNodeIndex]);

            path.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        private NativeList<int2> getPath(NativeArray<PathNode> pathNodes, PathNode endNode) {
            PathNode currentNode = endNode;
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);

            if (currentNode.cameFromNodeIndex == -1) {
                return path; // return empty path if no path was found
            }

            path.Add(new int2(currentNode.x, currentNode.y));

            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodes[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.x, cameFromNode.y));
                currentNode = cameFromNode;
            }
            return path;
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

        private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) {
            return
                gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }

        private int getNodeIndex(int x, int y, int2 gridSize) {
            return x + y * gridSize.x;
        }

        private int CalculateDistanceCost(int2 aPosition, int2 bPosition) {
            int xDistance = math.abs(aPosition.x - bPosition.x);
            int yDistance = math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return 14 * math.min(xDistance, yDistance) + 10 * remaining;
        }
    }

    private struct PathNode {
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
