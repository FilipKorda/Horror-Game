using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private bool canHighlight = true;
    [SerializeField] private Renderer[] renderers;

    private bool isHighlighted;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if ((highlighted && !canHighlight) || isHighlighted == highlighted)
            return;

        isHighlighted = highlighted;

        foreach (Renderer rend in renderers)
        {
            if (highlighted)
                InteractOutlineFeature.Add(rend);
            else
                InteractOutlineFeature.Remove(rend);
        }
    }

    public void SetCanHighlight(bool value)
    {
        canHighlight = value;

        if (!canHighlight)
            SetHighlighted(false);
    }
}