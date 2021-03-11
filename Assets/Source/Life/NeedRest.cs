using Unity.Entities;

[GenerateAuthoringComponent]
public struct NeedRest : IComponentData {
    public int restLevel;
}