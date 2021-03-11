using System.Collections;
using UnityEngine;
using Unity.Entities;

public class NeedSystem : ComponentSystem {
    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref PhysiologicalNeeds needs) => {
            NeedFood needFood = EntityManager.GetComponentData<NeedFood>(entity);
            NeedRest needRest = EntityManager.GetComponentData<NeedRest>(entity);

            needFood.foodLevel--;
            needRest.restLevel--;

            EntityManager.SetComponentData(entity, needFood);
            EntityManager.SetComponentData(entity, needRest);
        });
    }
}