using System.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

public class InputManager : ComponentSystem
{
    private float3 dragStartPosition;

    protected override void OnUpdate() {
        handleSelectionBox();
        handleMoveOrders();
    }

    private void handleMoveOrders() {
        if (Input.GetMouseButtonDown(1)) {
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Entities.ForEach((Entity entity, ref Translation translation, ref PathfindingRouteFollow follow, ref SelectableElement selectable) => {
                if (!selectable.isSelected)
                    return;

                EntityManager.AddComponentData(entity, new PathfindingParams {
                    startPosition = new int2((int)translation.Value.x, (int)translation.Value.y),
                    endPosition = new int2((int)targetPosition.x, (int)targetPosition.y)
                });
            });
        }
    }

    private void handleSelectionBox() {
        if (Input.GetMouseButtonDown(0)) {
            // Begin mouse down
            dragStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.gameObject.SetActive(true);
            WorldManager.SelectionArea.position = dragStartPosition;
        }
        if (Input.GetMouseButton(0)) {
            // Mouse held down
            float3 selectionAreaSize = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.localScale = selectionAreaSize - dragStartPosition;
        }
        if (Input.GetMouseButtonUp(0)) {
            // Mouse released
            WorldManager.SelectionArea.gameObject.SetActive(false);
            float3 endPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            float leftBoundary = math.min(endPosition.x, dragStartPosition.x);
            float rightBoundary = math.max(endPosition.x, dragStartPosition.x);
            float downBoundary = math.min(endPosition.y, dragStartPosition.y);
            float upBoundary = math.max(endPosition.y, dragStartPosition.y);

            Entities.ForEach((Entity entity, ref Translation translation, ref SelectableElement selectable) => {
                float3 entityPosition = translation.Value;

                bool withinSelection = entityPosition.x >= leftBoundary &&
                                       entityPosition.x <= rightBoundary &&
                                       entityPosition.y >= downBoundary &&
                                       entityPosition.y <= upBoundary;

                selectable.isSelected = withinSelection;
            });

            Entities.ForEach((Entity entity, ref TogglableSprite togglableSprite, ref Parent parent) => {
                if (EntityManager.GetComponentData<SelectableElement>(parent.Value).isSelected) {
                    EntityManager.RemoveComponent<Disabled>(entity);
                } else {
                    EntityManager.AddComponent<Disabled>(entity);
                }
            });
        }
    }
}