using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchLights : MonoBehaviour
{
    public Texture2D[] darkLightmapDir, darkLightmapColor;
    public Texture2D[] brightLightmapDir, brightLightmapColor;

    public GameObject[] volumetricLights;

    public InputActionReference light1Action;
    public InputActionReference light2Action;

    [Header("Migotanie podczas pościgu")]
    [SerializeField] private Vector2 flickerOffDuration = new Vector2(0.1f, 0.6f);
    [SerializeField] private Vector2 flickerOnDuration = new Vector2(0.05f, 0.45f);

    private LightmapData[] darkLightmap;
    private LightmapData[] brightLightmap;

    private Coroutine flickerRoutine;
    private bool blackedOut;

    private void OnEnable()
    {
        EnemyAI.OnChaseStarted += StartFlicker;
        EnemyAI.OnChaseEnded += StopFlicker;
    }

    private void OnDisable()
    {
        EnemyAI.OnChaseStarted -= StartFlicker;
        EnemyAI.OnChaseEnded -= StopFlicker;

        flickerRoutine = null;
    }

    private void Start()
    {
        darkLightmap = BuildLightmaps(darkLightmapColor, darkLightmapDir);
        brightLightmap = BuildLightmaps(brightLightmapColor, brightLightmapDir);

        SetVolumetricLights(true);

#if UNITY_EDITOR
        light1Action.action.performed += OnLight1Switched;
        light2Action.action.performed += OnLight2Switched;

        light1Action.action.Enable();
        light2Action.action.Enable();
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        light1Action.action.performed -= OnLight1Switched;
        light2Action.action.performed -= OnLight2Switched;
#endif
    }

    // Tekstury dir są opcjonalne: wypalenie bez Directional Mode (np. Mixed) w ogóle ich nie generuje.
    private static LightmapData[] BuildLightmaps(Texture2D[] colors, Texture2D[] dirs)
    {
        LightmapData[] result = new LightmapData[colors.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            result[i] = new LightmapData
            {
                lightmapColor = colors[i],
                lightmapDir = dirs != null && i < dirs.Length ? dirs[i] : null
            };
        }

        return result;
    }

    private void OnLight1Switched(InputAction.CallbackContext ctx)
    {
        SetLights(false);
    }

    private void OnLight2Switched(InputAction.CallbackContext ctx)
    {
        SetLights(true);
    }

    public void SwitchToDarkLightmap()
    {
        blackedOut = true;
        StopFlicker();

        SetLights(false);
    }

    private void StartFlicker()
    {
        if (blackedOut || flickerRoutine != null || darkLightmap == null || brightLightmap == null)
            return;

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private void StopFlicker()
    {
        if (flickerRoutine == null)
            return;

        StopCoroutine(flickerRoutine);
        flickerRoutine = null;

        if (!blackedOut)
            SetLights(true);
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            SetLights(false);
            yield return new WaitForSeconds(Random.Range(flickerOffDuration.x, flickerOffDuration.y));

            SetLights(true);
            yield return new WaitForSeconds(Random.Range(flickerOnDuration.x, flickerOnDuration.y));
        }
    }

    private void SetLights(bool on)
    {
        LightmapData[] lightmaps = on ? brightLightmap : darkLightmap;

        // Pusty zestaw wyczyściłby wszystkie lightmapy sceny, więc wtedy przełączamy tylko lampy.
        if (lightmaps != null && lightmaps.Length > 0)
            LightmapSettings.lightmaps = lightmaps;

        SetVolumetricLights(on);
    }

    private void SetVolumetricLights(bool active)
    {
        foreach (GameObject lamp in volumetricLights)
        {
            if (lamp != null)
                lamp.SetActive(active);
        }
    }
}