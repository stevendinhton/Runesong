using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class UnitMoveOrderSystem : ComponentSystem {

    protected override void OnUpdate() {
    }

    private void moveSelectedUnits(int targetPositionX, int targetPositionY) {
        Entities.ForEach((Entity entity, ref Translation translation, ref PathfindingRouteFollow follow, ref SelectableElement selectable) => {
            if (!selectable.isSelected)
                return;

            int startX = (int)(translation.Value.x);
            int startY = (int)(translation.Value.y);

            // Add Pathfinding Params
            EntityManager.AddComponentData(entity, new PathfindingParams {
                startPosition = new int2(startX, startY),
                endPosition = new int2(targetPositionX, targetPositionY)
            });
        });
    }

    private void moveRandomly() {
        int gridSize = WorldManager.MapWorld.regionSize;
        System.Random rnd = new System.Random();

        Entities.ForEach((Entity entity, ref Translation translation, ref PathfindingRouteFollow routeFollow) => {

            // Add Pathfinding Params
            if (routeFollow.routeIndex == -1) {
                int startX = (int)(translation.Value.x);
                int startY = (int)(translation.Value.y);

                EntityManager.AddComponentData(entity, new PathfindingParams {
                    startPosition = new int2(startX, startY),
                    endPosition = new int2(rnd.Next(0, gridSize - 1), rnd.Next(0, gridSize - 1))
                });
            }
        });
    }
}
