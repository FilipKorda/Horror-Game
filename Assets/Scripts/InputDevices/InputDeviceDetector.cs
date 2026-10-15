using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public enum InputDeviceType
{
    KeyboardMouse,
    Xbox,
    PlayStation,
    Generic 
}

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }

    private const float GamepadActivationThreshold = 0.3f;

    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;

    public event Action<InputDeviceType> OnDeviceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        if (device is Gamepad && !HasMeaningfulInput(eventPtr, device))
            return;

        InputDeviceType detected = ClassifyDevice(device);
        if (detected == CurrentDevice)
            return;

        CurrentDevice = detected;
        OnDeviceChanged?.Invoke(CurrentDevice);
    }

    private static bool HasMeaningfulInput(InputEventPtr eventPtr, InputDevice device)
    {
        foreach (InputControl control in eventPtr.EnumerateChangedControls(device, GamepadActivationThreshold))
            return control != null;

        return false;
    }

    private InputDeviceType ClassifyDevice(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
            return InputDeviceType.KeyboardMouse;

        if (device is Gamepad gamepad)
        {
            string layout = gamepad.layout ?? string.Empty;
            string product = gamepad.description.product ?? string.Empty;

            if (layout.Contains("DualShock") || layout.Contains("DualSense") ||
                product.Contains("Wireless Controller") || product.Contains("DualSense"))
                return InputDeviceType.PlayStation;

            if (layout.Contains("XInput") || layout.Contains("Xbox") || product.Contains("Xbox"))
                return InputDeviceType.Xbox;

            return InputDeviceType.Generic;
        }

        return CurrentDevice; 
    }
}