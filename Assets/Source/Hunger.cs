using System.Collections;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct Hunger : IComponentData {
    int foodLevel;
}