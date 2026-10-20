using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class InteractOutlineFeature : ScriptableRendererFeature
{
    private static readonly List<Renderer> targets = new List<Renderer>();
    private static Color? colorOverride;

    private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");

    [SerializeField] private Shader shader;
    [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    [Tooltip("Kolor domyślny. Po starcie nadpisywany wyborem gracza z ustawień (SettingsManager).")]
    [SerializeField] private Color color = Color.white;
    [Tooltip("Mnożnik jasności. Wartości > 1 wymagają HDR w URP Asset - dopiero wtedy Bloom rozświetla kontur.")]
    [SerializeField, Min(0f)] private float intensity = 4f;
    [Tooltip("Grubość twardego konturu w pikselach (przy wysokości ekranu 1080).")]
    [SerializeField, Range(0f, 8f)] private float outlineWidth = 2f;
    [Tooltip("Zasięg miękkiej poświaty w pikselach (przy wysokości ekranu 1080).")]
    [SerializeField, Range(0f, 32f)] private float glowWidth = 12f;
    [SerializeField, Range(0f, 2f)] private float glowStrength = 0.5f;

    private Material material;
    private OutlinePass pass;

    public static void Add(Renderer renderer)
    {
        if (renderer == null || targets.Contains(renderer))
            return;

        if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            targets.Add(renderer);
    }

    public static void Remove(Renderer renderer)
    {
        targets.Remove(renderer);
    }

    public static void SetColor(Color value)
    {
        colorOverride = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        targets.Clear();
        colorOverride = null;
    }

    public override void Create()
    {
        CoreUtils.Destroy(material);
        material = null;
        pass = null;

        if (shader == null)
        {
            Debug.LogWarning($"[{nameof(InteractOutlineFeature)}] Brak przypisanego shadera (InteractOutline).");
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new OutlinePass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || targets.Count == 0)
            return;

        material.SetVector(ColorId, (colorOverride ?? color).linear * intensity);
        material.SetFloat(OutlineWidthId, outlineWidth);
        material.SetFloat(GlowWidthId, glowWidth);
        material.SetFloat(GlowStrengthId, glowStrength);

        pass.renderPassEvent = passEvent;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
        pass = null;
    }

    private class OutlinePass : ScriptableRenderPass
    {
        private const int MaskShaderPass = 0;
        private const int CompositeShaderPass = 1;

        private static readonly List<Material> scratchMaterials = new List<Material>();

        private readonly Material material;

        public OutlinePass(Material material)
        {
            this.material = material;
        }

        private class MaskPassData
        {
            public List<Renderer> renderers;
            public Material material;
        }

        private class CompositePassData
        {
            public TextureHandle mask;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType != CameraType.Game || targets.Count == 0)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            TextureDesc maskDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            maskDesc.name = "_InteractOutlineMask";
            maskDesc.colorFormat = GraphicsFormat.R8_UNorm;
            maskDesc.msaaSamples = MSAASamples.None;
            maskDesc.bindTextureMS = false;
            maskDesc.filterMode = FilterMode.Bilinear;
            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.clear;

            TextureHandle mask = renderGraph.CreateTexture(maskDesc);

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Interact Outline Mask", out var maskData))
            {
                maskData.renderers = targets;
                maskData.material = material;

                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) => DrawMask(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Interact Outline Composite", out var compositeData))
            {
                compositeData.mask = mask;
                compositeData.material = material;

                builder.UseTexture(mask);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) => DrawComposite(data, context));
            }
        }

        private static void DrawMask(MaskPassData data, RasterGraphContext context)
        {
            foreach (Renderer renderer in data.renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                renderer.GetSharedMaterials(scratchMaterials);

                for (int submesh = 0; submesh < scratchMaterials.Count; submesh++)
                    context.cmd.DrawRenderer(renderer, data.material, submesh, MaskShaderPass);
            }
        }

        private static void DrawComposite(CompositePassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.mask, new Vector4(1f, 1f, 0f, 0f), data.material, CompositeShaderPass);
        }
    }
}