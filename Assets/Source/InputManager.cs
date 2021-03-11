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
    private Vector3 cameraStartPosition;
    private Vector3 lastMousePosition;

    protected override void OnUpdate() {
        handleSelectionBox();
        handleMoveOrders();
        handleCameraControl();

        if (Input.GetKeyDown(KeyCode.Space)) {
            Entities.ForEach((Entity entity, ref Translation translation, ref SelectableElement selectable) => {
                if (selectable.isSelected) {
                    Entity newEntity = EntityManager.Instantiate(entity);
                    Translation newTrans = EntityManager.GetComponentData<Translation>(newEntity);
                    newTrans.Value.x += 1;
                    EntityManager.AddComponentData(newEntity, newTrans);
                }
            });
        }
    }

    public void handleCameraControl() {
        if (Input.GetMouseButtonDown(2)) {
            cameraStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2)) {

            Vector3 mousePosition = Input.mousePosition;
            Vector3 difference = cameraStartPosition - mousePosition;

            //if ((ControllableCamera.instance.transform.position - difference).magnitude > 1) {
                ControllableCamera.instance.transform.position = difference;
            //}
        }
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

            Entities.ForEach((Entity entity, ref Translation translation, ref VisibleIfParentSelected visibleIfParentSelected, ref Parent parent) => {
                if (EntityManager.GetComponentData<SelectableElement>(parent.Value).isSelected) {
                    translation.Value.y = 0;
                } else {
                    translation.Value.y = 1000000;
                }
            });
        }
    }
}