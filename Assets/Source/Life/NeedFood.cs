using Unity.Entities;

[GenerateAuthoringComponent]
public struct NeedFood : IComponentData {
    public int foodLevel;
}