using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform flashlightPivot; 
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private RectTransform canvasRect; 
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private Image crosshairImage;

    [Header("Canvas")]
    [SerializeField] private Camera uiCamera; 

    [Header("Zasięg rzutowania")]
    [SerializeField] private float projectionDistance = 10f;

    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite pickupSprite;
    [SerializeField] private Sprite doorSprite;
    [SerializeField] private Sprite useSprite;
    [SerializeField] private Sprite hideSprite;
    [SerializeField] private Sprite exitSprite;

    [Header("Rozmiar")]
    [SerializeField] private Vector2 enlargedSize = new Vector2(50f, 50f); 

    private Vector2 baseSize;

    private void Awake()
    {
        if (crosshairRect != null)
            baseSize = crosshairRect.sizeDelta; 
    }

    private void LateUpdate()
    {
        UpdatePosition();
        UpdateSprite();
    }

    private void UpdatePosition()
    {
        if (playerCamera == null || flashlightPivot == null || crosshairRect == null || canvasRect == null)
            return;

        Vector3 worldPoint = flashlightPivot.position + flashlightPivot.forward * projectionDistance;
        Vector3 screenPoint = playerCamera.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
            return; 

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            crosshairRect.anchoredPosition = localPoint;
    }

    private void UpdateSprite()
    {
        if (crosshairImage == null || interactionSystem == null)
            return;

        Sprite target = defaultSprite;

        if (interactionSystem.IsHiding)
        {
            target = hideSprite;
        }
        else if (interactionSystem.CurrentInteractable != null)
        {
            target = interactionSystem.CurrentInteractable.Type switch
            {
                InteractionType.Pickup => pickupSprite,
                InteractionType.Door => doorSprite,
                InteractionType.Use => useSprite,
                InteractionType.Hide => hideSprite,
                InteractionType.Exit => exitSprite,
                _ => defaultSprite,
            };
        }

        if (target != null)
            crosshairImage.sprite = target;

        if (crosshairRect != null)
            crosshairRect.sizeDelta = target == defaultSprite ? baseSize : enlargedSize;
    }
}