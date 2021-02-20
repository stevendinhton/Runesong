using Unity.Entities;

[GenerateAuthoringComponent]
public struct PathfindingRouteFollow : IComponentData {
    public int routeIndex;
}
