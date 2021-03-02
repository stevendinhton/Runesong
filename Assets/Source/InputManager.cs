using System.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

public class InputManager : ComponentSystem
{
    private float3 dragStartPosition;

    protected override void OnUpdate() {
        if (Input.GetMouseButtonDown(0)) {
            dragStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.gameObject.SetActive(true);
            WorldManager.SelectionArea.position = dragStartPosition;
        }
        if (Input.GetMouseButton(0)) {
            float3 selectionAreaSize = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            WorldManager.SelectionArea.localScale = selectionAreaSize - dragStartPosition;
         }
        if (Input.GetMouseButtonUp(0)) {
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
        }
    }
}