using UnityEngine;

namespace FastFPSMovement.PostFX
{
    /// <summary>
    /// Full-screen pixelation + posterize + dither post-process for the BUILT-IN Render
    /// Pipeline only. Add this component directly to your camera (e.g. FPSCamera).
    /// If your project uses URP, use PixelationRendererFeature instead - URP cameras
    /// don't reliably call OnRenderImage.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelationEffectBuiltIn : MonoBehaviour
    {
        [Tooltip("Leave empty to auto-find Hidden/FastFPSMovement/PixelationBuiltIn.")]
        [SerializeField] private Shader pixelationShader;

        [Tooltip("Size (in screen pixels) of each pixelated block. Higher = chunkier.")]
        [Range(1f, 64f)]
        [SerializeField] private float pixelSize = 6f;

        [Tooltip("Number of color steps per channel. Lower = more retro/limited palette.")]
        [Range(2f, 32f)]
        [SerializeField] private float colorLevels = 6f;

        [Tooltip("Strength of the ordered dither pattern used to break up posterization banding.")]
        [Range(0f, 1f)]
        [SerializeField] private float ditherStrength = 0.35f;

        private Material _material;

        private void OnEnable()
        {
            if (pixelationShader == null)
            {
                pixelationShader = Shader.Find("Hidden/FastFPSMovement/PixelationBuiltIn");
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (pixelationShader == null || !pixelationShader.isSupported)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (_material == null || _material.shader != pixelationShader)
            {
                _material = new Material(pixelationShader) { hideFlags = HideFlags.HideAndDontSave };
            }

            _material.SetFloat("_PixelSize", pixelSize);
            _material.SetFloat("_ColorLevels", colorLevels);
            _material.SetFloat("_DitherStrength", ditherStrength);

            Graphics.Blit(source, destination, _material);
        }

        private void OnDisable()
        {
            if (_material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_material);
            }
            else
            {
                DestroyImmediate(_material);
            }
        }
    }
}
