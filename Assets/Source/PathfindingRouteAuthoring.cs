using UnityEngine;
using Unity.Entities;

public class PathfindingRouteAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
    
    public void Convert(Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem) {
        manager.AddBuffer<PathfindingRoute>(entity);
    }
}