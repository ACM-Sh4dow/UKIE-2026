using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class Pixelate : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 16)]
        public int downsampleFactor = 4;

        public Color[] colors;
        
        [System.NonSerialized]
        public TextureHandle LowColorTexture;
    }

    public Settings settings = new Settings();
    public ComputeShader quantiseShader;
    
    private PixelatePass pass;

    public override void Create()
    {
        pass = new PixelatePass(settings, quantiseShader)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        renderer.EnqueuePass(pass);
    }
    
    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }

    class PixelatePass : ScriptableRenderPass
    {
        private class DSColor
        {
            public TextureHandle SourceTexture;
            public TextureHandle DestinationTexture;
        }
        private class QuantiseData
        {
            public TextureHandle Source;
            public TextureHandle Output;
            public ComputeShader Shader;
            public ComputeBuffer Palette;
            public int PaletteSize;
            public int Width;
            public int Height;
        }
        
        private static readonly int ColorID = Shader.PropertyToID("PixelateColorTexture");

        private readonly Settings settings;
        
        private TextureHandle transientTextureHandle;
        private ComputeShader quantiseShader;
        private ComputeBuffer paletteBuffer;
        private TextureHandle quantisedTextureHandle;

        public PixelatePass(Settings settings, ComputeShader quantiseShader)
        {
            this.settings = settings;
            this.quantiseShader = quantiseShader;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            #region Preamble
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            
            var descriptor = cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.width  = Mathf.Max(1, descriptor.width  / settings.downsampleFactor);
            descriptor.height = Mathf.Max(1, descriptor.height / settings.downsampleFactor);
            
            transientTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "PixelateLowColor", false, FilterMode.Point);
            #endregion
            
            #region Downsample
            using (var dsBuilder = renderGraph.AddRasterRenderPass<DSColor>("Pixelate - Colour Downsample (with Transparents)", out var ds))
            {
                ds.SourceTexture = resourceData.activeColorTexture;
                ds.DestinationTexture = transientTextureHandle;

                dsBuilder.UseTexture(ds.SourceTexture, AccessFlags.Read);
                dsBuilder.SetRenderAttachment(ds.DestinationTexture, 0, AccessFlags.Write);

                dsBuilder.SetRenderFunc((DSColor data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.SourceTexture, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                });
                
                dsBuilder.SetGlobalTextureAfterPass(transientTextureHandle, ColorID);
                settings.LowColorTexture = transientTextureHandle;
            }
            #endregion
            #region Quantise
            if (settings.colors != null && settings.colors.Length > 0 && quantiseShader != null)
            {
                paletteBuffer?.Release();
                paletteBuffer = new ComputeBuffer(settings.colors.Length, sizeof(float) * 4);
                Vector4[] labPalette = new Vector4[settings.colors.Length];
                for (int i = 0; i < settings.colors.Length; i++)
                {
                    Color c = settings.colors[i].linear;
                    labPalette[i] = new Vector4(c.r, c.g, c.b, 1f);
                }
                paletteBuffer.SetData(labPalette);

                var quantiseDescriptor = cameraData.cameraTargetDescriptor;
                quantiseDescriptor.depthBufferBits = 0;
                quantiseDescriptor.width  = Mathf.Max(1, quantiseDescriptor.width  / settings.downsampleFactor);
                quantiseDescriptor.height = Mathf.Max(1, quantiseDescriptor.height / settings.downsampleFactor);
                quantiseDescriptor.enableRandomWrite = true;

                quantisedTextureHandle = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, quantiseDescriptor, "PixelateQuantised", false, FilterMode.Point);

                using (var qBuilder = renderGraph.AddComputePass<QuantiseData>("Pixelate - Quantise", out var q))
                {
                    q.Source      = transientTextureHandle;
                    q.Output      = quantisedTextureHandle;
                    q.Shader      = quantiseShader;
                    q.Palette     = paletteBuffer;
                    q.PaletteSize = settings.colors.Length;
                    q.Width       = quantiseDescriptor.width;
                    q.Height      = quantiseDescriptor.height;

                    qBuilder.UseTexture(q.Source, AccessFlags.Read);
                    qBuilder.UseTexture(q.Output, AccessFlags.Write);

                    qBuilder.SetRenderFunc((QuantiseData data, ComputeGraphContext context) =>
                    {
                        int kernel = data.Shader.FindKernel("CSMain");
                        data.Shader.SetTexture(kernel, "Input",      data.Source);
                        data.Shader.SetTexture(kernel, "Output",     data.Output);
                        data.Shader.SetBuffer (kernel, "Palette",    data.Palette);
                        data.Shader.SetInt   ("PaletteSize",         data.PaletteSize);

                        int tx = Mathf.CeilToInt(data.Width  / 8f);
                        int ty = Mathf.CeilToInt(data.Height / 8f);
                        context.cmd.DispatchCompute(data.Shader, kernel, tx, ty, 1);
                    });
                }
                
                transientTextureHandle = quantisedTextureHandle;
            }
            #endregion
            #region Upsample
            using (var usBuilder = renderGraph.AddRasterRenderPass<DSColor>("Pixelate - Colour Upsample", out var us))
            {
                us.SourceTexture = transientTextureHandle;
                us.DestinationTexture = resourceData.activeColorTexture;

                usBuilder.UseTexture(us.SourceTexture, AccessFlags.Read);
                usBuilder.SetRenderAttachment(us.DestinationTexture, 0, AccessFlags.Write);

                usBuilder.SetRenderFunc((DSColor data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.SourceTexture, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                });
            }
            #endregion
        }
        
        public void Dispose()
        {
            paletteBuffer?.Release();
            paletteBuffer = null;
        }
    }
}