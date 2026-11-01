using UnityEngine;
using UnityEngine.Rendering;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private Light sunLight;
        private Light fillLight;
        private Material runtimeSkybox;
        private Texture2D alderwakeOverview;
        private Texture2D morrowfenOverview;
        private Texture2D cinderwoldOverview;
        private Texture2D vignetteTexture;
        private string presentedRegion;
        private float regionRevealStartedAt;
        private float regionRevealEndsAt;
        private Color targetAmbient;
        private Color targetAmbientSky;
        private Color targetAmbientEquator;
        private Color targetAmbientGround;
        private Color targetFog;
        private float targetFogDensity;
        private Color targetSun;
        private Color targetSky;
        private Color targetGround;

        private void InitializePresentation()
        {
            alderwakeOverview = Resources.Load<Texture2D>("Art/Original/tetherfall_alderwake_overview");
            morrowfenOverview = Resources.Load<Texture2D>("Art/Original/tetherfall_morrowfen_overview");
            cinderwoldOverview = Resources.Load<Texture2D>("Art/Original/tetherfall_cinderwold_overview");
            vignetteTexture = CreateVignetteTexture(192, 108);
            PresentRegion(CurrentRegion, true);
            WorldPresentationBuilder.Build(this);
            TetherfallRegionalArtOverhaul.Build(this);
            TetherfallRoamingWorldDirector.EnsureInitialized(this);
        }

        private void UpdatePresentation()
        {
            var easing = 1f - Mathf.Exp(-1.8f * Time.deltaTime);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, easing);
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, targetAmbientSky, easing);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, targetAmbientEquator, easing);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, targetAmbientGround, easing);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFog, easing);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, easing);

            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(sunLight.color, targetSun, easing);
            }

            if (fillLight != null)
            {
                var fillColor = Color.Lerp(targetSky, new Color(0.34f, 0.88f, 0.76f), 0.28f);
                fillLight.color = Color.Lerp(fillLight.color, fillColor, easing);
            }

            if (runtimeSkybox != null)
            {
                SetSkyboxColor("_SkyTint", Color.Lerp(GetSkyboxColor("_SkyTint"), targetSky, easing));
                SetSkyboxColor("_GroundColor", Color.Lerp(GetSkyboxColor("_GroundColor"), targetGround, easing));
            }
        }

        private void PresentRegion(string region, bool immediate)
        {
            var presentation = RegionPresentation.Get(region);
            targetAmbient = presentation.ambient;
            targetAmbientSky = Color.Lerp(presentation.ambient, presentation.sky, 0.28f);
            targetAmbientEquator = Color.Lerp(presentation.ambient, presentation.ground, 0.32f);
            targetAmbientGround = Color.Lerp(presentation.ground, Color.black, 0.3f);
            targetFog = presentation.fog;
            var fallbackFogDensity = region switch
            {
                "Morrowfen" => 0.0052f,
                "Cinderwold Verge" => 0.0042f,
                _ => 0.0032f
            };
            targetFogDensity = TetherfallRegionPacks.GetFogDensity(region, fallbackFogDensity);
            targetSun = presentation.sun;
            targetSky = presentation.sky;
            targetGround = presentation.ground;

            if (immediate)
            {
                RenderSettings.ambientLight = targetAmbient;
                RenderSettings.ambientSkyColor = targetAmbientSky;
                RenderSettings.ambientEquatorColor = targetAmbientEquator;
                RenderSettings.ambientGroundColor = targetAmbientGround;
                RenderSettings.fogColor = targetFog;
                RenderSettings.fogDensity = targetFogDensity;
                if (sunLight != null)
                {
                    sunLight.color = targetSun;
                }

                SetSkyboxColor("_SkyTint", targetSky);
                SetSkyboxColor("_GroundColor", targetGround);
            }

            if (presentedRegion == region)
            {
                return;
            }

            presentedRegion = region;
            regionRevealStartedAt = Time.unscaledTime;
            regionRevealEndsAt = regionRevealStartedAt + (immediate ? 2.7f : 2.25f);
        }

        private void DrawWorldFrame(float width, float height)
        {
            if (vignetteTexture == null || CodexOpen || activePanel != GamePanel.None)
            {
                return;
            }

            var priorColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.42f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), vignetteTexture);
            GUI.color = priorColor;
        }

        private void DrawRegionDiscovery(float width, float height)
        {
            if (Time.unscaledTime >= regionRevealEndsAt || CodexOpen || activePanel != GamePanel.None)
            {
                return;
            }

            var presentation = RegionPresentation.Get(presentedRegion);
            var image = GetRegionOverview(presentedRegion);
            if (image == null)
            {
                return;
            }

            var elapsed = Time.unscaledTime - regionRevealStartedAt;
            var remaining = regionRevealEndsAt - Time.unscaledTime;
            var alpha = Mathf.Clamp01(Mathf.Min(elapsed * 2.6f, remaining * 2.2f));
            var cardWidth = Mathf.Min(540f, width - 64f);
            var cardHeight = cardWidth * 0.236f;
            var card = new Rect(width * 0.5f - cardWidth * 0.5f, Mathf.Min(92f, height * 0.13f), cardWidth, cardHeight);
            var artRect = new Rect(card.x, card.y, card.width * 0.39f, card.height);
            var detailsRect = new Rect(artRect.xMax, card.y, card.width - artRect.width, card.height);

            var priorColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(card, inkTexture);
            GUI.DrawTexture(artRect, image, ScaleMode.ScaleAndCrop);
            GUI.DrawTexture(new Rect(artRect.xMax - 4f, artRect.y, 4f, artRect.height), copperTexture);
            GUI.DrawTexture(new Rect(detailsRect.x, detailsRect.y, detailsRect.width, 3f), tealTexture);
            GUI.Label(
                new Rect(detailsRect.x + 24f, detailsRect.y + 20f, detailsRect.width - 48f, 28f),
                presentedRegion.ToUpperInvariant(),
                headingStyle);
            GUI.Label(
                new Rect(detailsRect.x + 24f, detailsRect.y + 56f, detailsRect.width - 48f, 46f),
                presentation.discoveryText,
                bodyStyle);
            GUI.Label(
                new Rect(detailsRect.x + 24f, detailsRect.yMax - 27f, detailsRect.width - 48f, 16f),
                "THREADGLASS REGIONAL RECORD",
                mutedStyle);
            GUI.color = priorColor;
        }

        public Material CreateGlowMaterial(Color color, float intensity = 1.4f, float smoothness = 0.45f)
        {
            var material = CreateMaterial(color, 0.08f, smoothness);
            if (material != null && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }

            return material;
        }

        public Material CreateTransparentMaterial(Color color, float smoothness = 0.45f)
        {
            var material = CreateMaterial(color, 0f, smoothness);
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }

        private Texture2D GetRegionOverview(string region)
        {
            var regionPackOverview = TetherfallRegionPacks.GetOverviewTexture(region);
            if (regionPackOverview != null)
            {
                return regionPackOverview;
            }

            return region switch
            {
                "Morrowfen" => morrowfenOverview,
                "Cinderwold Verge" => cinderwoldOverview,
                _ => alderwakeOverview
            };
        }

        private void SetSkyboxColor(string property, Color color)
        {
            if (runtimeSkybox != null && runtimeSkybox.HasProperty(property))
            {
                runtimeSkybox.SetColor(property, color);
            }
        }

        private Color GetSkyboxColor(string property)
        {
            return runtimeSkybox != null && runtimeSkybox.HasProperty(property)
                ? runtimeSkybox.GetColor(property)
                : targetSky;
        }

        private static Texture2D CreateVignetteTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalizedX = (x / (float)(width - 1) - 0.5f) * 2f;
                    var normalizedY = (y / (float)(height - 1) - 0.5f) * 2f;
                    var edge = Mathf.Clamp01(Mathf.Max(Mathf.Abs(normalizedX), Mathf.Abs(normalizedY)));
                    var alpha = Mathf.SmoothStep(0f, 0.86f, Mathf.InverseLerp(0.38f, 1f, edge));
                    texture.SetPixel(x, y, new Color(0.005f, 0.02f, 0.025f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }

    public static class RegionPresentation
    {
        public readonly struct Definition
        {
            public readonly Color ambient;
            public readonly Color fog;
            public readonly Color sun;
            public readonly Color sky;
            public readonly Color ground;
            public readonly string discoveryText;

            public Definition(
                Color ambientColor,
                Color fogColor,
                Color sunColor,
                Color skyColor,
                Color groundColor,
                string text)
            {
                ambient = ambientColor;
                fog = fogColor;
                sun = sunColor;
                sky = skyColor;
                ground = groundColor;
                discoveryText = text;
            }
        }

        public static Definition Get(string region)
        {
            if (TetherfallRegionPacks.TryGetPresentation(region, out var regionPackPresentation))
            {
                return regionPackPresentation;
            }

            return region switch
            {
                "Morrowfen" => new Definition(
                    new Color(0.23f, 0.36f, 0.4f),
                    new Color(0.14f, 0.28f, 0.34f),
                    new Color(0.56f, 0.73f, 0.78f),
                    new Color(0.22f, 0.44f, 0.55f),
                    new Color(0.08f, 0.13f, 0.16f),
                    "Lantern reeds mark the shallow routes. The Skyvault waits beneath the drowned stone."),
                "Cinderwold Verge" => new Definition(
                    new Color(0.4f, 0.25f, 0.2f),
                    new Color(0.3f, 0.18f, 0.16f),
                    new Color(1f, 0.53f, 0.33f),
                    new Color(0.58f, 0.28f, 0.2f),
                    new Color(0.15f, 0.07f, 0.06f),
                    "Broken relays still point across the ash. Updrafts carry a prepared Wayfinder over the shelves."),
                _ => new Definition(
                    new Color(0.38f, 0.47f, 0.4f),
                    new Color(0.25f, 0.37f, 0.32f),
                    new Color(1f, 0.77f, 0.55f),
                    new Color(0.45f, 0.66f, 0.67f),
                    new Color(0.13f, 0.21f, 0.16f),
                    "Restored paths begin here. Follow Threadglass markers toward work that reconnects Veyra.")
            };
        }
    }
}
