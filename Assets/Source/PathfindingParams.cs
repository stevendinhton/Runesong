using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Pathfinding {

    public enum PathfindingType { ToSpot, ToAdjacent };

    public struct PathfindingParams : IComponentData {
        public int2 startPosition;
        public int2 endPosition;
        public PathfindingType pathfindingType;
        public int growthCode;
    }
}