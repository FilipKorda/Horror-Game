using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zamienia InputAction + wykryty typ urządzenia na gotowy tag TMP do wstawienia w tekst.
/// Dla zwykłej akcji (jeden binding, np. Interact -> E) zwraca jedną ikonę. Dla akcji
/// kompozytowej (np. Move -> 2D Vector z częściami W/A/S/D) zbiera WSZYSTKIE pasujące
/// części kompozytu i skleija je w jeden tag (np. ikony W, A, S, D obok siebie) -
/// pojedynczy "return po pierwszym trafieniu" wystarcza tylko na akcje z jednym bindingiem.
/// </summary>
public class InputGlyphProvider : MonoBehaviour
{
    [SerializeField] private List<InputIconSet> iconSets;

    public string GetGlyphTag(InputAction action, InputDeviceType deviceType)
    {
        if (action == null)
            return "?";

        List<string> bindingPaths = FindBindingPaths(action, deviceType);

        if (bindingPaths.Count == 0)
            return $"[{action.name}]";

        InputIconSet iconSet = iconSets?.Find(set => set.deviceType == deviceType);

        StringBuilder result = new StringBuilder();

        foreach (string path in bindingPaths)
        {
            string tag = iconSet != null ? iconSet.GetSpriteTag(path) : null;
            result.Append(tag ?? $"[{path}]"); // jawny fallback per-klawisz, żeby brak jednej ikony nie ukrywał reszty
        }

        return result.ToString();
    }

    /// <summary>Zbiera ścieżki WSZYSTKICH bindingów akcji pasujących do danego urządzenia (nie tylko pierwszego) - w tym każdej części kompozytu (isPartOfComposite), z pominięciem samego nagłówka kompozytu (isComposite), który nie ma własnej ścieżki.</summary>
    private List<string> FindBindingPaths(InputAction action, InputDeviceType deviceType)
    {
        List<string> result = new List<string>();

        foreach (InputBinding binding in action.bindings)
        {
            if (binding.isComposite)
                continue; // to nagłówek kompozytu (np. "2D Vector") - nie ma własnej ścieżki, tylko grupuje części

            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path))
                continue;

            bool matches = deviceType switch
            {
                InputDeviceType.KeyboardMouse =>
                    path.StartsWith("<Keyboard>") ||
                    path.StartsWith("<Mouse>") ||
                    path.StartsWith("<Pointer>"), // Look/delta bywa bindowany do <Pointer>/delta, nie <Mouse>/delta

                InputDeviceType.Xbox =>
                    path.StartsWith("<Gamepad>") ||
                    path.StartsWith("<XInputController>"),

                InputDeviceType.PlayStation =>
                    path.StartsWith("<Gamepad>") ||
                    path.StartsWith("<DualShockGamepad>") ||
                    path.StartsWith("<DualSenseGamepadHID>"),

                _ => path.StartsWith("<Gamepad>")
            };

            if (matches)
                result.Add(path);
        }

        return result;
    }
}