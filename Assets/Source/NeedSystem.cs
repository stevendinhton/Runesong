using System.Collections;
using UnityEngine;
using Unity.Entities;

public class NeedSystem : ComponentSystem {

    private ulong lastUpdateTick = 0;

    protected override void OnUpdate() {
        int ticksPassed = (int)(TimeManager.DateInTicks - lastUpdateTick);

        if (ticksPassed == 0) return;

        lastUpdateTick = TimeManager.DateInTicks;

        Entities.ForEach((Entity entity, ref PhysiologicalNeeds needs) => {
            NeedFood needFood = EntityManager.GetComponentData<NeedFood>(entity);
            NeedRest needRest = EntityManager.GetComponentData<NeedRest>(entity);

            needFood.foodLevel -= ticksPassed;
            needRest.restLevel -= ticksPassed;

            EntityManager.SetComponentData(entity, needFood);
            EntityManager.SetComponentData(entity, needRest);
        });
    }
}