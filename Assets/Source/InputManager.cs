using System.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

public class InputManager : ComponentSystem
{
    private float3 selectionDragStartPos;
    private Vector3 cameraDragStartPos;
    private float3 cameraObjectStartPos;

    private float3 dragSpeed = new Vector3(0.01f, 0.01f, 0.01f);
    private float zoomSpeed = 0.25f;

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
            cameraDragStartPos = Input.mousePosition;
            cameraObjectStartPos = ControllableCamera.instance.transform.position;
        }
        if (Input.GetMouseButton(2)) {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 difference = cameraDragStartPos - mousePosition;
            ControllableCamera.instance.transform.position = cameraObjectStartPos + difference * dragSpeed;
        }

        if (Input.mouseScrollDelta.y < 0) {
            ControllableCamera.instance.GetComponent<Camera>().orthographicSize += zoomSpeed;
        }
        if (Input.mouseScrollDelta.y > 0) {
            ControllableCamera.instance.GetComponent<Camera>().orthographicSize -= zoomSpeed;
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
            selectionDragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.gameObject.SetActive(true);
            WorldManager.SelectionArea.position = selectionDragStartPos;
            Debug.Log(selectionDragStartPos);
        }
        if (Input.GetMouseButton(0)) {
            // Mouse held down
            float3 selectionAreaSize = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.localScale = selectionAreaSize - selectionDragStartPos;
        }
        if (Input.GetMouseButtonUp(0)) {
            // Mouse released
            WorldManager.SelectionArea.gameObject.SetActive(false);
            float3 endPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            float leftBoundary = math.min(endPosition.x, selectionDragStartPos.x);
            float rightBoundary = math.max(endPosition.x, selectionDragStartPos.x);
            float downBoundary = math.min(endPosition.y, selectionDragStartPos.y);
            float upBoundary = math.max(endPosition.y, selectionDragStartPos.y);

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