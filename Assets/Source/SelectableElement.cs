using Unity.Entities;

[GenerateAuthoringComponent]
public struct SelectableElement : IComponentData {
    public bool isSelected;
}
