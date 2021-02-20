using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;


public class UnitMoveOrderSystem : ComponentSystem {
    protected override void OnUpdate() {
        if (Input.GetMouseButtonDown(0)) {
            Debug.Log("Mouse Button Down");

            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log(string.Format("Mouse click at [X: {0} Y: {0}]", pos.x, pos.y));

            Entities.ForEach((Entity entity, ref Translation translation) => {
                int startX = (int)(translation.Value.x);
                int startY = (int)(translation.Value.y);

                // Add Pathfinding Params
                EntityManager.AddComponentData(entity, new PathfindingParams {
                    startPosition = new int2(startX, startY),
                    endPosition = new int2((int)pos.x, (int)pos.y)
                });
            });
        }
        
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
