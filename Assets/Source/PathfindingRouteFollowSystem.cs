using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class PathfindingRouteFollowSystem : ComponentSystem {

    private float centerOfTileOffset = 0.5f;

    protected override void OnUpdate() {
        Entities.ForEach((DynamicBuffer<PathfindingRoute> pathingRoute, ref Translation translation, ref PathfindingRouteFollow pathfindingRouteFollow) => {
            if (pathfindingRouteFollow.routeIndex != -1) {
                int2 pathPosition = pathingRoute[pathfindingRouteFollow.routeIndex].position;

                float3 targetPosition = new float3(pathPosition.x + centerOfTileOffset, pathPosition.y + centerOfTileOffset, 0);
                float3 moveDirection = math.normalizesafe(targetPosition - translation.Value);
                float moveSpeed = 3f;

                translation.Value += moveDirection * moveSpeed * Time.DeltaTime;

                if (math.distance(translation.Value, targetPosition) < .1f) {
                    pathfindingRouteFollow.routeIndex--;
                }
            }
        });
    }
}
