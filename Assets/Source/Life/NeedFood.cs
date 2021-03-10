using System.Collections;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct NeedFood : IComponentData {
    int foodLevel;
}