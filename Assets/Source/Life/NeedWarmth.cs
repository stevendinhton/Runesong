using Unity.Entities;

[GenerateAuthoringComponent]
public struct NeedWarmth : IComponentData {
    public int warmthLevel;
}