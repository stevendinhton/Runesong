using Unity.Entities;

[GenerateAuthoringComponent]
public struct PhysiologicalNeeds : IComponentData
{
    // How much of each is required to be satisfied
    // -1 means it is not required at all
    int rest;       
    int food;       
    int warmth;     
}