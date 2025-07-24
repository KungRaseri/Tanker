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

        // Heat aura system
        private bool heatAuraActive = false;
        private Coroutine heatAuraCoroutine;
        private float heatAuraRadius = 2.0f;
        private float heatDamageAmount = 0.5f;
        private float heatDamageInterval = 0.2f;
        private float heatAuraIgniteChance = 0.5f;

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

                // Make Tanker immune to heat/fire effects
                var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
                if (physicalBehaviour != null)
                {
                    // Set high heat resistance
                    physicalBehaviour.Temperature = 20f; // Set to ambient temperature
                    
                    // We'll also prevent heating in the ApplyHeatDamage method through our immunity checks
                }
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
            // Stop heat aura and vapor effects first
            StopHeatAura();
            RemoveVaporParticles();

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

                // Activate heat damage aura
                StartHeatAura();

                ModAPI.Notify("Molten mode activated! Heat aura engaged. Tanker is immune to heat damage.");
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

                // Stop heat aura
                StopHeatAura();

                ModAPI.Notify("Molten mode deactivated! Heat aura disabled.");
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
            // Main module - basic particle properties for steam
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); // Steam lifespan
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f); // Slower, drifting steam
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); // Start very small
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.9f, 1f), new Color(0.7f, 0.7f, 0.7f, 1f)); // Light gray steam
            main.maxParticles = 100; // Fewer particles for wispy effect
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f); // Random rotation
            main.gravityModifier = -0.075f; // Very light upward drift

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

                    // Create a wispy steam texture for the particles
                    Texture2D steamTexture = CreateSteamTexture(128);
                    steamMaterial.mainTexture = steamTexture;
                }
            }

            // Emission - lighter steam emission
            var emission = particles.emission;
            emission.rateOverTime = 10f; // Lighter steam emission

            // Shape - emit from around the limb surface
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.03f; // Smaller emission area for more focused steam
            shape.radiusThickness = 0.75f; // Emit from edge only

            // Velocity over lifetime - steam rises and drifts
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.3f, 0.7f); // Gentle rise
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f); // Light horizontal drift

            // Size over lifetime - steam grows and stretches
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.4f); // Start small
            sizeCurve.AddKey(0.5f, 1.2f); // Grow quickly
            sizeCurve.AddKey(1f, 3.0f); // Spread out and dissipate
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

            // Rotation over lifetime - gentle steam swirl
            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-15f, 15f); // Gentler swirl

            // Noise module - add subtle turbulence for realistic steam movement
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.2f; // Lighter turbulence
            noise.frequency = 0.3f; // Lower frequency for smoother movement
            noise.scrollSpeed = 0.3f; // Slower scroll for gentle drift
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

        private Texture2D CreateSteamTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 offset = pos - center;

                    // Create an elongated, wispy shape
                    float normalizedX = offset.x / (size / 3f);
                    float normalizedY = offset.y / (size / 1.5f);

                    // Create a soft, elongated shape that's wider at the bottom
                    float verticalFactor = Mathf.Clamp01(1f - Mathf.Abs(normalizedY));
                    float horizontalFactor = Mathf.Clamp01(1f - Mathf.Abs(normalizedX));

                    // Make it more elongated and wispy
                    float steamShape = verticalFactor * horizontalFactor;

                    // Add some noise for wispy effect
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                    steamShape = Mathf.Clamp01(steamShape + noise - 0.2f);

                    // Create soft falloff
                    float distance = Vector2.Distance(pos, center) / (size / 2f);
                    float falloff = Mathf.Clamp01(1f - distance);
                    falloff = Mathf.Pow(falloff, 0.8f); // Soft edge

                    float alpha = steamShape * falloff;

                    // Make bottom heavier and top lighter for realistic steam
                    if (normalizedY < 0) // Bottom half
                    {
                        alpha *= 1.2f;
                    }
                    else // Top half
                    {
                        alpha *= 0.6f;
                    }

                    alpha = Mathf.Clamp01(alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private void StartHeatAura()
        {
            if (heatAuraCoroutine != null)
            {
                StopCoroutine(heatAuraCoroutine);
            }

            heatAuraActive = true;
            heatAuraCoroutine = StartCoroutine(HeatAuraEffect());
        }

        private void StopHeatAura()
        {
            heatAuraActive = false;

            if (heatAuraCoroutine != null)
            {
                StopCoroutine(heatAuraCoroutine);
                heatAuraCoroutine = null;
            }
        }

        private IEnumerator HeatAuraEffect()
        {
            while (heatAuraActive && isMoltenMode)
            {
                ApplyHeatDamage();
                
                // Keep the Tanker's own temperature regulated to prevent self-damage
                MaintainTankerTemperature();
                
                yield return new WaitForSeconds(heatDamageInterval);
            }
        }

        private void MaintainTankerTemperature()
        {
            if (person == null || person.Limbs == null) return;

            // Keep Tanker's limbs at a safe temperature to prevent self-damage
            foreach (var limb in person.Limbs)
            {
                var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
                if (physicalBehaviour != null)
                {
                    // Keep temperature at a reasonable level that won't cause auto-ignition
                    if (physicalBehaviour.Temperature > 200f)
                    {
                        physicalBehaviour.Temperature = 200f; // Cap at 200°C
                    }
                    
                    // Extinguish any fire that might have been applied to the Tanker
                    if (physicalBehaviour.OnFire)
                    {
                        physicalBehaviour.Extinguish();
                    }
                }
            }
        }

        private void ApplyHeatDamage()
        {
            if (person == null || person.gameObject == null) return;

            Vector3 tankerPosition = person.transform.position;

            // Find all objects within heat aura radius
            Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(tankerPosition, heatAuraRadius);

            foreach (var collider in objectsInRange)
            {
                // Skip the tanker itself and all its limbs
                if (collider.gameObject == person.gameObject) continue;
                if (IsTankerLimb(collider.gameObject)) continue;

                // Check if it's a person or limb that can take damage
                var targetPerson = collider.GetComponent<PersonBehaviour>();
                var targetLimb = collider.GetComponent<LimbBehaviour>();
                var targetPhysical = collider.GetComponent<PhysicalBehaviour>();

                if (targetPerson != null)
                {
                    // Skip if this is the same person as the tanker
                    if (targetPerson == person) continue;

                    // Deal damage to all limbs of the person
                    foreach (var limb in targetPerson.Limbs)
                    {
                        ApplyHeatDamageToLimb(limb, tankerPosition);
                    }
                }
                else if (targetLimb != null)
                {
                    // Skip if this limb belongs to the tanker
                    if (targetLimb.Person == person) continue;

                    // Deal damage directly to the limb
                    ApplyHeatDamageToLimb(targetLimb, tankerPosition);
                }
                else if (targetPhysical != null)
                {
                    // Skip if this physical object is part of the tanker
                    if (IsPartOfTanker(targetPhysical.gameObject)) continue;

                    // Try to ignite other physical objects (like wood, paper, etc.)
                    ApplyHeatDamageToPhysicalObject(targetPhysical, tankerPosition);
                }
            }
        }

        private void ApplyHeatDamageToLimb(LimbBehaviour limb, Vector3 heatSource)
        {
            if (limb == null) return;

            float distance = Vector3.Distance(heatSource, limb.transform.position);
            if (distance > heatAuraRadius) return;

            // Calculate damage based on distance (closer = more damage)
            float damageMultiplier = 1f - (distance / heatAuraRadius);
            float actualDamage = heatDamageAmount * damageMultiplier;

            // Apply the damage
            limb.Damage(actualDamage);
            IgniteLimb(limb);
        }

        private void ApplyHeatDamageToPhysicalObject(PhysicalBehaviour physicalObject, Vector3 heatSource)
        {
            if (physicalObject == null) return;

            float distance = Vector3.Distance(heatSource, physicalObject.transform.position);
            if (distance > heatAuraRadius) return;

            // Calculate ignition chance based on distance
            float damageMultiplier = 1f - (distance / heatAuraRadius);
            float ignitionChance = heatAuraIgniteChance * damageMultiplier * 0.3f; // Lower chance for non-living objects

            // Increase temperature of the object (only if it simulates temperature)
            if (physicalObject.SimulateTemperature)
            {
                physicalObject.Temperature += 30f * damageMultiplier;
            }

            // Check for ignition chance (only if not already on fire)
            if (!physicalObject.OnFire && UnityEngine.Random.value < ignitionChance)
            {
                IgnitePhysicalObject(physicalObject);
            }
        }

        private bool IsTankerLimb(GameObject obj)
        {
            if (person == null || person.Limbs == null) return false;

            // Check if the object is one of the tanker's limbs
            foreach (var limb in person.Limbs)
            {
                if (limb != null && limb.gameObject == obj)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPartOfTanker(GameObject obj)
        {
            if (person == null) return false;

            // Check if the object is the tanker's main GameObject
            if (obj == person.gameObject) return true;

            // Check if the object is a child of the tanker
            Transform current = obj.transform;
            while (current != null)
            {
                if (current.gameObject == person.gameObject)
                {
                    return true;
                }
                current = current.parent;
            }

            // Check if the object is one of the tanker's limbs
            return IsTankerLimb(obj);
        }

        private void IgniteLimb(LimbBehaviour limb)
        {
            if (limb == null) return;

            // Use the correct API to ignite the limb via PhysicalBehaviour
            var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
            if (physicalBehaviour != null)
            {
                try
                {
                    // Use the correct PhysicalBehaviour.Ignite() method
                    physicalBehaviour.Ignite(false); // Pass false to respect flammability

                    // Create fire particle effect at limb position
                    // ModAPI.CreateParticleEffect("Flash", limb.transform.position);
                }
                catch
                {
                    // Fallback: create fire visual effects if Ignite() doesn't work
                    CreateFireEffect(limb);
                }
            }
            else
            {
                // Fallback if no PhysicalBehaviour found
                CreateFireEffect(limb);
            }
        }

        private void IgnitePhysicalObject(PhysicalBehaviour physicalObject)
        {
            if (physicalObject == null) return;

            // Check if object is already on fire to avoid redundant ignition
            if (physicalObject.OnFire) return;

            try
            {
                // Use the correct PhysicalBehaviour.Ignite() method
                physicalObject.Ignite(false); // Pass false to respect flammability

                // Create fire particle effect at object position
                // ModAPI.CreateParticleEffect("Flash", physicalObject.transform.position);
            }
            catch
            {
                // Fallback: just create visual effects if ignition fails
                // ModAPI.CreateParticleEffect("Flash", physicalObject.transform.position);
            }
        }

        private void CreateFireEffect(LimbBehaviour limb)
        {
            // Create fire particle effects
            // ModAPI.CreateParticleEffect("Flash", limb.transform.position);

            // Add continuous fire visual until limb is destroyed or extinguished
            StartCoroutine(ContinuousFireEffect(limb));
        }

        private IEnumerator ContinuousFireEffect(LimbBehaviour limb)
        {
            float fireDuration = 5.0f; // Fire lasts 5 seconds
            float fireInterval = 0.3f;
            float elapsed = 0f;

            while (elapsed < fireDuration && limb != null && limb.gameObject != null)
            {
                // Create fire particles
                if (UnityEngine.Random.value < 0.7f)
                {
                    Vector3 firePos = limb.transform.position + new Vector3(
                        UnityEngine.Random.Range(-0.1f, 0.1f),
                        UnityEngine.Random.Range(-0.1f, 0.1f),
                        0f
                    );
                    ModAPI.CreateParticleEffect("Flash", firePos);
                }

                // Deal fire damage over time
                if (limb != null)
                {
                    limb.Damage(0.2f);
                }

                elapsed += fireInterval;
                yield return new WaitForSeconds(fireInterval);
            }
        }

        void OnDestroy()
        {
            // Clean up particles and heat aura when the component is destroyed
            RemoveVaporParticles();
            StopHeatAura();
        }
    }
}