using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Układa w jednym rzędzie kilka osobnych tekstów TMP (np. "Naciśnij" + ikona klawisza + opis)
/// według ich faktycznej, wyrenderowanej szerokości. Dzięki temu odstępy są takie same w każdym
/// języku i dla każdej ikony, zamiast zależeć od ręcznie ustawionych pozycji.
/// Pierwszy element zostaje na miejscu, kolejne są dosuwane do poprzedniego. Pozycji w pionie nie rusza.
/// </summary>
public class InputPromptRowLayout : MonoBehaviour
{
    [Tooltip("Teksty w kolejności od lewej do prawej.")]
    [SerializeField] private TMP_Text[] parts;

    [Tooltip("Odstęp między elementami w jednostkach Canvasa (przy 1920x1080).")]
    [SerializeField] private float spacing = 20f;

    private bool dirty = true;
    private Canvas rootCanvas;

    private readonly Vector3[] scratchCorners = new Vector3[4];

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        dirty = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
    }

    private void OnValidate()
    {
        dirty = true;
    }

    private void HandleTextChanged(Object changed)
    {
        if (dirty || parts == null)
            return;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == changed)
            {
                dirty = true;
                return;
            }
        }
    }

    // LateUpdate, bo LocalizeStringEvent i LocalizedInputPrompt podmieniają teksty w różnej kolejności -
    // układamy dopiero, gdy w tej klatce wszystkie już się zmieniły
    private void LateUpdate()
    {
        if (!dirty)
            return;

        dirty = false;
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (parts == null || parts.Length < 2)
            return;

        if (rootCanvas == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
        }

        float worldSpacing = spacing * (rootCanvas != null ? rootCanvas.scaleFactor : 1f);

        float previousRight = 0f;
        bool havePrevious = false;

        foreach (TMP_Text part in parts)
        {
            if (part == null || !part.gameObject.activeInHierarchy)
                continue;

            part.ForceMeshUpdate();

            if (!TryGetInkRange(part, out float left, out float right))
                continue;

            if (havePrevious)
            {
                float shift = previousRight + worldSpacing - left;

                if (Mathf.Abs(shift) > 0.01f)
                {
                    RectTransform rect = part.rectTransform;
                    Vector3 localShift = rect.parent.InverseTransformVector(new Vector3(shift, 0f, 0f));
                    rect.anchoredPosition += new Vector2(localShift.x, 0f);

                    left += shift;
                    right += shift;
                }
            }

            previousRight = right;
            havePrevious = true;
        }
    }

    /// <summary>
    /// Lewa i prawa krawędź widocznych znaków w przestrzeni świata. Liczymy po znakach, bo dla ikon
    /// (sprite) TMP nie podaje sensownej szerokości - mają zerowy advance i rysują się w prawo od punktu zaczepienia.
    /// </summary>
    private bool TryGetInkRange(TMP_Text text, out float left, out float right)
    {
        left = float.MaxValue;
        right = float.MinValue;

        TMP_TextInfo textInfo = text.textInfo;
        Transform textTransform = text.transform;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            scratchCorners[0] = textTransform.TransformPoint(character.bottomLeft);
            scratchCorners[1] = textTransform.TransformPoint(character.topRight);

            left = Mathf.Min(left, Mathf.Min(scratchCorners[0].x, scratchCorners[1].x));
            right = Mathf.Max(right, Mathf.Max(scratchCorners[0].x, scratchCorners[1].x));
        }

        return left <= right;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Domyślnie bierzemy bezpośrednie dzieci z tekstem, w kolejności z hierarchii
        List<TMP_Text> found = new List<TMP_Text>();

        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out TMP_Text text))
                found.Add(text);
        }

        parts = found.ToArray();
    }
#endif
}
