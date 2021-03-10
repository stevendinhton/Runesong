using System.Collections;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct WarmthNeed : IComponentData
{
    int warmthLevel;
}