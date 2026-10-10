using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DropdownAutoScroll : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void LateUpdate()
    {
        if (!dropdown.IsExpanded)
            return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;
        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        if (selectedRect == null)
            return;
        ScrollRect scrollRect = selected.GetComponentInParent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null)
            return;
        Toggle[] items = scrollRect.content.GetComponentsInChildren<Toggle>();

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].gameObject == selected)
            {
                if (i == 0)
                {
                    scrollRect.verticalNormalizedPosition = 1f;
                    return;
                }

                break;
            }
        }
        RectTransform viewport = scrollRect.viewport;
        Canvas.ForceUpdateCanvases();

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            viewport,
            selectedRect
        );

        float scrollSpeedUp = -0.25f;
        float scrollSpeedDown = 0.75f;

        float viewportTop = viewport.rect.height * scrollSpeedUp;
        float viewportBottom = -viewport.rect.height * scrollSpeedDown;

        if (bounds.min.y > viewportTop)
        {
            float difference = bounds.min.y - viewportTop;

            Scroll(scrollRect, difference);
        }
        else if (bounds.max.y < viewportBottom)
        {
            float difference = viewportBottom - bounds.max.y;

            Scroll(scrollRect, -difference);
        }

        if (IsFirstItem(selectedRect, scrollRect))
            scrollRect.verticalNormalizedPosition = 1f;

        if (IsLastItem(selectedRect, scrollRect))
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void Scroll(ScrollRect scrollRect, float difference)
    {
        RectTransform content = scrollRect.content;

        if (content == null)
            return;

        float scrollableHeight =
            content.rect.height - scrollRect.viewport.rect.height;

        if (scrollableHeight <= 0f)
            return;

        float position = scrollRect.verticalNormalizedPosition;

        position += difference / scrollableHeight;

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(position);
    }

    private bool IsFirstItem(RectTransform item, ScrollRect scrollRect)
    {
        return item.GetSiblingIndex() == 0;
    }

    private bool IsLastItem(RectTransform item, ScrollRect scrollRect)
    {
        return item.GetSiblingIndex() == scrollRect.content.childCount - 1;
    }
}