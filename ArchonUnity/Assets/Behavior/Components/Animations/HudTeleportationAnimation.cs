using Assets.Behavior.Adapters;
using Assets.Behavior.TransferTypes;
using UnityEngine;

namespace Assets.Behavior.Components.Animations
{
    /// <summary>
    /// Controls the HUD animation for teleportation progress.
    /// </summary>
    [RequireComponent(typeof(SoundAdapter))]
    public class HudTeleportationAnimation : MonoBehaviour
    {
        public Renderer progressBarRenderer;
        public Renderer iconRenderer;
        public Renderer toneDownRenderer;
        public Texture2D emergencyIcon;
        public Texture2D normal1Icon;

        [Range(0, 1)] public float progress;
        internal TeleportationType type;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            iconRenderer.enabled =
            toneDownRenderer.enabled =
            progressBarRenderer.enabled = type != TeleportationType.None && progress > 0;
            if (type != TeleportationType.None && progress > 0)
            {
                progressBarRenderer.material.SetVector("_Progress", M.V3(progress, 1, 1));
                progressBarRenderer.material.SetFloat("_FadeIn", M.Saturate(progress * 5));
                progressBarRenderer.material.SetFloat("_Flash", M.Smoothstep(0.6f, 1, progress) * 0.9f);
                progressBarRenderer.material.color = Color.Lerp(Color.white, Color.red, progress);

                iconRenderer.material.color = new Color(1, 1, 1, M.Saturate(progress * 5));
                iconRenderer.material.mainTexture = type == TeleportationType.Emergency ? emergencyIcon : normal1Icon;

                toneDownRenderer.material.SetFloat($"_Opacity", M.Smoothstep(0.4f, 0.6f, progress));
            }
        }
    }

}