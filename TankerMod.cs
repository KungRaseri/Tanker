using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tanker
{
    public class TankerMod : MonoBehaviour
    {
        public static string ModTag = " [Tag]"; //Whilst not required, modded items should contain 'tags' at the end of their names to prevent errors in which two mods have an item of the same name.

        // Instance variables for tracking state per Tanker
        public PersonBehaviour person;
        public bool isMoltenMode, isUltraSenseMode = false;
        public Texture2D originalSkin;
        public Texture2D originalFlesh;
        public Texture2D originalBone;
        public Texture2D moltenTexture;
        public Texture2D ultraSenseTexture;

        // Particle systems for vapor effects
        private List<ParticleSystem> vaporParticleSystems = new List<ParticleSystem>();
        private bool vaporActive = false;

        public static void Main()
        {
            ModAPI.Register(
            new Modification()
            {
                OriginalItem = ModAPI.FindSpawnable("Android"),
                NameOverride = "Tanker" + ModTag,
                DescriptionOverride = "A heavily armored tank operator with enhanced durability and combat capabilities.",
                CategoryOverride = ModAPI.FindCategory("Entities"),
                ThumbnailOverride = ModAPI.LoadSprite("sprites/Tanker-skin.png"),
                AfterSpawn = (Instance) =>
                {
                    var person = Instance.GetComponent<PersonBehaviour>();
                    if (person != null)
                    {
                        // Add the TankerMod component to this specific instance
                        var tankerComponent = person.gameObject.AddComponent<TankerMod>();
                        tankerComponent.person = person;
                        tankerComponent.InitializeTanker();

                        ModAPI.Notify("Tanker deployed! Enhanced armor and durability active.");
                    }
                }
            });
        }

        void Start()
        {
            // This will be called when the component is added to a spawned Tanker
            if (person != null)
            {
                StartCoroutine(DelayedSetup());
            }
        }

        private void InitializeTanker()
        {
            // Load and store textures for mode switching
            LoadTextures();

            // Apply custom skin texture to the person
            SetEntityTextures();

            // Set up tanker initial stats
            SetTankerStats();
        }

        private void LoadTextures()
        {
            originalSkin = ModAPI.LoadTexture("sprites/Tanker-skin.png");
            originalFlesh = ModAPI.LoadTexture("sprites/Tanker-flesh.png");
            originalBone = ModAPI.LoadTexture("sprites/Tanker-bone.png");
            moltenTexture = ModAPI.LoadTexture("sprites/Tanker-molten.png");
            ultraSenseTexture = ModAPI.LoadTexture("sprites/Tanker-ultrasense.png");
        }

        private System.Collections.IEnumerator DelayedSetup()
        {
            yield return new WaitForEndOfFrame();
            SetupTankerContextMenu();
        }

        private void SetEntityTextures()
        {
            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1);
            }
        }

        private void SetTankerStats()
        {
            // Set up any special abilities or attributes for the Tanker
            // This could include things like increased health, special attacks, etc.
            foreach (var limb in person.Limbs)
            {
                // Example: Increase health of each limb
                limb.InitialHealth *= 2; // Double the initial health for durability
                limb.Health = limb.InitialHealth; // Reset current health to match

                limb.ImpactDamageMultiplier = 0.5f; // Reduce impact damage taken by limbs
                limb.ShotDamageMultiplier = 0.5f; // Reduce shot damage taken by limbs
            }
        }

        public void SetupTankerContextMenu()
        {
            // Try adding context menu to each limb since that's where right-clicks are detected
            foreach (var limb in person.Limbs)
            {
                var contextMenuOptions = limb.GetComponent<ContextMenuOptionComponent>();
                if (contextMenuOptions == null)
                {
                    contextMenuOptions = limb.gameObject.AddComponent<ContextMenuOptionComponent>();
                }

                // Create the context menu buttons using proper constructor
                var buttons = new List<ContextMenuButton>
                {
                    new ContextMenuButton("tankerMoltenMode", "Toggle Molten Mode", "Toggle molten tanker mode",
                        new UnityAction(() => ToggleMoltenMode())),

                    new ContextMenuButton("tankerUltraSenseMode", "Toggle Ultra Sense Mode", "Toggle ultra sense mode",
                        new UnityAction(() => ToggleUltraSenseMode())),

                    new ContextMenuButton("tankerStatusReport", "Status Report", "Show current status of the tanker",
                        new UnityAction(() => ShowStatusReport())),
                };

                // Set the buttons list
                contextMenuOptions.Buttons = buttons;
            }
        }

        private void ClearModes()
        {
            // Clear any existing modes
            if (isMoltenMode)
            {
                DisableMoltenMode();
            }

            if (isUltraSenseMode)
            {
                DisableUltraSenseMode();
            }
        }

        private void ToggleMoltenMode()
        {
            if (isMoltenMode)
            {
                // Disable molten mode
                DisableMoltenMode();
            }
            else
            {
                // Enable molten mode
                EnableMoltenMode();
            }
        }

        private void ToggleUltraSenseMode()
        {
            if (isUltraSenseMode)
            {
                DisableUltraSenseMode();
            }
            else
            {
                // Enable ultra sense mode
                EnableUltraSenseMode();
            }
        }

        private void EnableMoltenMode()
        {
            ClearModes();

            isMoltenMode = true;

            if (moltenTexture != null)
            {
                person.SetBodyTextures(moltenTexture, moltenTexture, moltenTexture, 1f);

                // Create vapor particles on each limb
                CreateVaporParticles();

                ModAPI.Notify("Molten mode activated!");
            }
        }

        private void DisableMoltenMode()
        {
            isMoltenMode = false;

            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                // Restore original textures
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

                // Remove vapor particles
                RemoveVaporParticles();

                ModAPI.Notify("Molten mode deactivated! Original textures restored.");
            }
        }

        private void EnableUltraSenseMode()
        {
            ClearModes();

            isUltraSenseMode = true;

            if (ultraSenseTexture != null)
            {
                // Method 1: Use SetBodyTextures with ultra sense texture
                person.SetBodyTextures(ultraSenseTexture, ultraSenseTexture, ultraSenseTexture, 1f);

                ModAPI.Notify("Ultra sense mode activated! Textures applied.");
            }
        }

        private void DisableUltraSenseMode()
        {
            isUltraSenseMode = false;

            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                // Restore original textures
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

                ModAPI.Notify("Ultra sense mode deactivated! Original textures restored.");
            }
        }

        private void ShowStatusReport()
        {
            float avgHealth = 0f;
            foreach (var limb in person.Limbs)
            {
                avgHealth += limb.Health / limb.InitialHealth;
            }
            avgHealth /= person.Limbs.Length;

            string status = $"Tanker Status Report:\n" +
                          $"Health: {(avgHealth * 100):F0}%\n" +
                          $"Molten Mode: {(isMoltenMode ? "ACTIVE" : "INACTIVE")}\n" +
                          $"Ultra Sense Mode: {(isUltraSenseMode ? "ACTIVE" : "INACTIVE")}\n";

            ModAPI.Notify(status);
        }

        private void CreateVaporParticles()
        {
            // Remove any existing particles first
            RemoveVaporParticles();

            vaporActive = true;

            foreach (var limb in person.Limbs)
            {
                // Create a new GameObject for the particle system
                GameObject particleGO = new GameObject("SteamVapor");
                particleGO.transform.SetParent(limb.transform);
                particleGO.transform.localPosition = Vector3.zero;

                // Add and configure the particle system for steamy vapor
                ParticleSystem particles = particleGO.AddComponent<ParticleSystem>();
                ConfigureSteamParticles(particles);

                vaporParticleSystems.Add(particles);
            }

            ModAPI.Notify($"Created custom steam vapor on {vaporParticleSystems.Count} limbs");
        }

        private void ConfigureSteamParticles(ParticleSystem particles)
        {
            // Main module - basic particle properties
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 5.0f); // Longer lasting steam
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f); // Slower, more steam-like
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // Small start size
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.7f, 0.7f, 0.8f), new Color(0.8f, 0.8f, 0.8f, 0.6f)); // Warm steam colors
            main.maxParticles = 50; // More particles for dense steam
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f); // Random rotation
            main.gravityModifier = -0.1f; // Slight upward force

            // Configure renderer for proper material
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // Try different shader approaches for compatibility
                Material steamMaterial = null;
                
                // Try 1: Standard Sprites shader
                var spritesShader = Shader.Find("Sprites/Default");
                if (spritesShader != null)
                {
                    steamMaterial = new Material(spritesShader);
                }
                // Try 2: Unlit/Transparent shader as fallback
                else
                {
                    var unlitShader = Shader.Find("Unlit/Transparent");
                    if (unlitShader != null)
                    {
                        steamMaterial = new Material(unlitShader);
                    }
                    // Try 3: Legacy particles shader
                    else
                    {
                        var particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                        if (particleShader != null)
                        {
                            steamMaterial = new Material(particleShader);
                        }
                    }
                }
                
                if (steamMaterial != null)
                {
                    steamMaterial.color = Color.white;
                    renderer.material = steamMaterial;
                    
                    // Create a simple white circle texture for the particles
                    Texture2D circleTexture = CreateCircleTexture(64);
                    steamMaterial.mainTexture = circleTexture;
                }
            }

            // Emission - continuous steam generation
            var emission = particles.emission;
            emission.rateOverTime = 15f; // Dense steam emission

            // Shape - emit from around the limb surface
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f; // Larger emission area
            shape.radiusThickness = 0.8f; // Emit from edge, not center

            // Velocity over lifetime - steam rises and spreads
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); // Rise upward
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); // Slight horizontal drift
            
            // Size over lifetime - steam expands as it rises
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f); // Start small
            sizeCurve.AddKey(0.5f, 1.0f); // Expand in middle
            sizeCurve.AddKey(1f, 2.5f); // Large at end
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime - fade from warm to transparent
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.0f), // Warm start
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.6f), // Cool middle
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1.0f)  // Gray end
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0.0f), // Start visible
                    new GradientAlphaKey(0.6f, 0.3f), // Peak visibility
                    new GradientAlphaKey(0.0f, 1.0f)  // Fade out completely
                }
            );
            colorOverLifetime.color = gradient;

            // Rotation over lifetime - steam swirls
            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-30f, 30f); // Slow swirl

            // Noise module - add turbulence for realistic steam movement
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.5f;
            noise.damping = true;

            // Texture sheet animation - if we want animated sprites (optional)
            var textureSheetAnimation = particles.textureSheetAnimation;
            textureSheetAnimation.enabled = false; // Keep simple for now
        }

        private void RemoveVaporParticles()
        {
            vaporActive = false;
            
            // Clean up all particle systems
            foreach (var particleSystem in vaporParticleSystems)
            {
                if (particleSystem != null && particleSystem.gameObject != null)
                {
                    UnityEngine.Object.Destroy(particleSystem.gameObject);
                }
            }
            vaporParticleSystems.Clear();
        }

        private Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f; // Leave small border
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius)
                    {
                        // Create soft circular gradient
                        float alpha = 1f - (distance / radius);
                        alpha = Mathf.Pow(alpha, 0.5f); // Soft falloff
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        void OnDestroy()
        {
            // Clean up particles when the component is destroyed
            RemoveVaporParticles();
        }
    }
}