using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionBox : MonoBehaviour
{
    public static SelectionBox instance;
    private RectTransform rectTransform;
    private Vector2 startPos; // start position in world space

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        instance = this;
    }

    public void StartSelection() {
        startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gameObject.SetActive(true);
    }

    public void UpdateSelection() {
        Vector2 current = Input.mousePosition;
        Vector2 screenStartPoint = Camera.main.WorldToScreenPoint(startPos);

        float width = current.x - screenStartPoint.x;
        float height = current.y - screenStartPoint.y;

        rectTransform.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        rectTransform.pivot = new Vector2(width > 0 ? 0 : 1, height > 0 ? 0 : 1);
        rectTransform.anchoredPosition = screenStartPoint;
    }

    public (Vector2, Vector2) EndAndGetSelection() {
        gameObject.SetActive(false);

        return (
            startPos,
            Camera.main.ScreenToWorldPoint(Input.mousePosition)
        );
    }
}
