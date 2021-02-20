using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PathfindingRoute : IBufferElementData {
    public int2 position;
}
