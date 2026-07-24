using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FastFPSMovement.PostFX
{
    /// <summary>
    /// URP Renderer Feature version of the pixelation/posterize/dither effect. Use this
    /// instead of PixelationEffectBuiltIn if your project uses the Universal Render
    /// Pipeline (URP cameras don't reliably call OnRenderImage, so a classic MonoBehaviour
    /// post-process component doesn't work under URP - this Renderer Feature is the
    /// correct URP equivalent).
    ///
    /// SETUP:
    /// 1. Select your URP Renderer Data asset (Assets -> the .asset referenced by your
    ///    Universal Render Pipeline Asset's "Renderer List").
    /// 2. Add Renderer Feature -> pick "Pixelation Renderer Feature" from the list.
    /// 3. Assign Shaders/PixelationURP.shader to the feature's Pixelation Shader field
    ///    (it will also auto-find it by name if left empty).
    /// 4. Tune Pixel Size / Color Levels / Dither Strength in the Renderer Feature inspector.
    /// </summary>
    public class PixelationRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Leave empty to auto-find Hidden/FastFPSMovement/PixelationURP.")]
            public Shader pixelationShader;

            [Tooltip("Size (in screen pixels) of each pixelated block. Higher = chunkier.")]
            [Range(1f, 64f)]
            public float pixelSize = 6f;

            [Tooltip("Number of color steps per channel. Lower = more retro/limited palette.")]
            [Range(2f, 32f)]
            public float colorLevels = 6f;

            [Tooltip("Strength of the ordered dither pattern used to break up posterization banding.")]
            [Range(0f, 1f)]
            public float ditherStrength = 0.35f;

            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public Settings settings = new Settings();

        private PixelationPass _pass;
        private Material _material;

        public override void Create()
        {
            if (settings.pixelationShader == null)
            {
                settings.pixelationShader = Shader.Find("Hidden/FastFPSMovement/PixelationURP");
            }

            if (settings.pixelationShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(settings.pixelationShader);
            }

            _pass = new PixelationPass(_material, settings)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }

        private class PixelationPass : ScriptableRenderPass
        {
            private readonly Material _material;
            private readonly Settings _settings;
            private RenderTargetIdentifier _source;
            private RenderTargetHandle _tempTexture;

            public PixelationPass(Material material, Settings settings)
            {
                _material = material;
                _settings = settings;
                _tempTexture.Init("_PixelationTempTex");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null)
                {
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get("Pixelation");

                _source = renderingData.cameraData.renderer.cameraColorTarget;

                _material.SetFloat("_PixelSize", _settings.pixelSize);
                _material.SetFloat("_ColorLevels", _settings.colorLevels);
                _material.SetFloat("_DitherStrength", _settings.ditherStrength);

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                cmd.GetTemporaryRT(_tempTexture.id, descriptor, FilterMode.Point);

                Blit(cmd, _source, _tempTexture.Identifier(), _material);
                Blit(cmd, _tempTexture.Identifier(), _source);

                cmd.ReleaseTemporaryRT(_tempTexture.id);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
