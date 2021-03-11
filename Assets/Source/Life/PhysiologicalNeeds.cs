using Unity.Entities;

[GenerateAuthoringComponent]
public struct PhysiologicalNeeds : IComponentData
{
    // How much of each is the max / required to be satisfied 
    // -1 means it is not required at all
    int maxRest;       
    int maxFood;       
    int maxHealth;

    // Cannot handle temperatures below minWarmth or above maxWarmth
    int minTemperature;
    int maxTemperature;
}