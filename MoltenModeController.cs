using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanker
{
    public class MoltenModeController : MonoBehaviour
    {
        // Reference to the parent tanker
        private PersonBehaviour person;
        private TankerMod tankerMod;

        // Molten mode state
        public bool IsActive { get; private set; } = false;

        // Textures
        private Texture2D moltenTexture;
        private Texture2D originalSkin;
        private Texture2D originalFlesh;
        private Texture2D originalBone;

        // Particle systems for vapor effects
        private List<ParticleSystem> vaporParticleSystems = new List<ParticleSystem>();
        private bool vaporActive = false;

        // Heat aura system
        private bool heatAuraActive = false;
        private Coroutine heatAuraCoroutine;
        private float heatAuraRadius = 3.0f;
        private float heatDamageAmount = 0.5f;
        private float heatDamageInterval = 0.2f;
        private float heatAuraIgniteChance = 0.5f;
        private int heatAuraCycles = 0;

        // Fireball system
        private LimbBehaviour leftHandLimb;
        private LimbBehaviour rightHandLimb;

        public void Initialize(PersonBehaviour person, TankerMod tankerMod,
                              Texture2D moltenTexture, Texture2D originalSkin,
                              Texture2D originalFlesh, Texture2D originalBone)
        {
            this.person = person;
            this.tankerMod = tankerMod;
            this.moltenTexture = moltenTexture;
            this.originalSkin = originalSkin;
            this.originalFlesh = originalFlesh;
            this.originalBone = originalBone;

            // Find hand limbs for fireball launching
            FindHandLimbs();
        }

        private void FindHandLimbs()
        {
            if (person == null || person.Limbs == null) return;

            // Search through all limbs to find the hands
            for (int i = 0; i < person.Limbs.Length; i++)
            {
                var limb = person.Limbs[i];
                if (limb == null) continue;

                string limbName = limb.name.Trim().ToUpper();

                // Check if this limb name contains ARM (to filter for arm limbs)
                if (limbName.Contains("ARM"))
                {
                    // LOWERARMFRONT = left hand
                    if (limbName.Contains("LOWERARM") && limbName.Contains("FRONT") && leftHandLimb == null)
                    {
                        leftHandLimb = limb;
                        ModAPI.Notify($"✓ Found LEFT hand at index {i}: {limb.name}");
                    }
                    // LOWERARM (without FRONT, without UPPER) = right hand
                    else if (limbName == "LOWERARM" && rightHandLimb == null)
                    {
                        rightHandLimb = limb;
                        ModAPI.Notify($"✓ Found RIGHT hand at index {i}: {limb.name}");
                    }
                }
            }

            if (leftHandLimb != null && rightHandLimb != null)
            {
                ModAPI.Notify("✓ Both hands detected successfully!");
            }
            else if (leftHandLimb == null && rightHandLimb == null)
            {
                ModAPI.Notify("✗ WARNING: No hands found!");
            }
            else
            {
                ModAPI.Notify($"⚠ Only one hand found! Left: {leftHandLimb != null}, Right: {rightHandLimb != null}");
            }
        }
        void Update()
        {
            // Check for fireball launch input (F key)
            if (Input.GetKeyDown(KeyCode.F))
            {
                LaunchFireballFromHeldHand();
            }
        }

        private void LaunchFireballFromHeldHand()
        {
            // Get mouse position
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Check which hand is closest to mouse (within reasonable distance)
            LimbBehaviour closestHand = null;
            float closestDistance = float.MaxValue;
            float maxDistance = 2f; // Maximum distance from mouse to hand

            if (leftHandLimb != null)
            {
                float dist = Vector3.Distance(leftHandLimb.transform.position, mousePos);
                if (dist < closestDistance && dist < maxDistance)
                {
                    closestDistance = dist;
                    closestHand = leftHandLimb;
                }
            }

            if (rightHandLimb != null)
            {
                float dist = Vector3.Distance(rightHandLimb.transform.position, mousePos);
                if (dist < closestDistance && dist < maxDistance)
                {
                    closestDistance = dist;
                    closestHand = rightHandLimb;
                }
            }

            if (closestHand != null)
            {
                LaunchFireball(closestHand);
            }
            else
            {
                ModAPI.Notify("Move mouse near a hand and press F to launch fireball!");
            }
        }

        private void LaunchFireball(LimbBehaviour handLimb)
        {
            if (handLimb == null) return;

            // Get launch position from hand
            Vector3 launchPosition = handLimb.transform.position;

            // Calculate launch direction based on hand orientation/velocity
            Vector2 launchDirection = CalculateLaunchDirection(handLimb);

            // Create the fireball projectile
            CreateFireball(launchPosition, launchDirection);

            // Visual feedback
            ModAPI.CreateParticleEffect("Flash", launchPosition);
            ModAPI.Notify("Fireball launched!");
        }

        private Vector2 CalculateLaunchDirection(LimbBehaviour handLimb)
        {
            // Get mouse position in world space
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Calculate direction from hand to mouse cursor
            Vector2 direction = (mousePos - handLimb.transform.position).normalized;

            return direction;
        }

        private void CreateFireball(Vector3 position, Vector2 direction)
        {
            // Create a physical fireball object
            GameObject fireballObj = new GameObject("Fireball");
            fireballObj.transform.position = position;

            // Add visual components
            SpriteRenderer spriteRenderer = fireballObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateFireballSprite();
            spriteRenderer.color = new Color(1f, 0.3f, 0f, 1f); // Bright orange/red fire color
            fireballObj.transform.localScale = Vector3.one * 0.3f;

            // Add glow effect with another sprite renderer
            GameObject glowObj = new GameObject("FireballGlow");
            glowObj.transform.SetParent(fireballObj.transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 1.5f;

            SpriteRenderer glowRenderer = glowObj.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = CreateFireballSprite();
            glowRenderer.color = new Color(1f, 0.6f, 0.1f, 0.5f); // Outer orange glow
            glowRenderer.sortingOrder = -1; // Render behind main fireball

            // Add physics
            Rigidbody2D rb = fireballObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.2f; // Slight gravity
            rb.velocity = direction * 10f; // Launch speed
            rb.mass = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Add collider
            CircleCollider2D collider = fireballObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.15f; // Adjusted to match visual size better
            collider.isTrigger = false; // Must be false for OnCollisionEnter2D to work

            // Add physical behavior for temperature/fire
            PhysicalBehaviour physicalBehaviour = fireballObj.AddComponent<PhysicalBehaviour>();
            physicalBehaviour.InitialMass = 0.5f;
            physicalBehaviour.Temperature = 1000f; // Very hot
            physicalBehaviour.Charge = 0f;

            // Add fireball behavior component
            FireballBehavior fireballBehavior = fireballObj.AddComponent<FireballBehavior>();
            fireballBehavior.Initialize(3f); // 3 second lifetime

            // Create fire particle trail
            CreateFireballParticles(fireballObj);
        }

        private Sprite CreateFireballSprite()
        {
            // Create a circular texture for the fireball with fire colors
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    if (distance <= radius)
                    {
                        float normalizedDist = distance / radius;
                        float alpha = 1f - normalizedDist;
                        alpha = Mathf.Pow(alpha, 0.6f); // Smooth falloff

                        // Create fire gradient: white center -> yellow -> orange -> red
                        Color fireColor;
                        if (normalizedDist < 0.3f)
                        {
                            // Center: bright yellow-white
                            fireColor = Color.Lerp(new Color(1f, 1f, 0.9f), new Color(1f, 0.9f, 0.3f), normalizedDist / 0.3f);
                        }
                        else if (normalizedDist < 0.6f)
                        {
                            // Middle: orange
                            float t = (normalizedDist - 0.3f) / 0.3f;
                            fireColor = Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.4f, 0f), t);
                        }
                        else
                        {
                            // Outer: red-orange
                            float t = (normalizedDist - 0.6f) / 0.4f;
                            fireColor = Color.Lerp(new Color(1f, 0.4f, 0f), new Color(0.8f, 0.1f, 0f), t);
                        }

                        pixels[y * size + x] = new Color(fireColor.r, fireColor.g, fireColor.b, alpha);
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
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void CreateFireballParticles(GameObject fireballObj)
        {
            GameObject particleGO = new GameObject("FireballParticles");
            particleGO.transform.SetParent(fireballObj.transform);
            particleGO.transform.localPosition = Vector3.zero;

            // Simple particle effect using ModAPI
            StartCoroutine(FireballTrailEffect(fireballObj));
        }

        private IEnumerator FireballTrailEffect(GameObject fireballObj)
        {
            float particleInterval = 0.05f;

            while (fireballObj != null)
            {
                if (UnityEngine.Random.value < 0.8f)
                {
                    ModAPI.CreateParticleEffect("Flash", fireballObj.transform.position);
                }

                yield return new WaitForSeconds(particleInterval);
            }
        }

        public void ToggleMoltenMode()
        {
            if (IsActive)
            {
                DisableMoltenMode();
            }
            else
            {
                EnableMoltenMode();
            }
        }

        public void EnableMoltenMode()
        {
            IsActive = true;

            if (moltenTexture != null)
            {
                // Create explosion when entering molten mode
                ExplosionCreator.Explode(new ExplosionCreator.ExplosionParameters
                {
                    Position = person.transform.position,
                    CreateParticlesAndSound = true,
                    DismemberChance = 0.50f,
                    FragmentForce = 4,
                    FragmentationRayCount = 16,
                    Range = 3
                });

                person.SetBodyTextures(moltenTexture, moltenTexture, moltenTexture, 1f);

                // Create vapor particles on each limb
                CreateVaporParticles();

                // Activate heat damage aura
                StartHeatAura();

                ModAPI.Notify("Molten mode activated! Heat aura engaged.");
            }
        }

        public void DisableMoltenMode()
        {
            IsActive = false;

            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                // Restore original textures
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

                // Remove vapor particles
                RemoveVaporParticles();

                // Stop heat aura
                StopHeatAura();

                ModAPI.Notify("Molten mode deactivated!");
            }
        }

        private void CreateVaporParticles()
        {
            RemoveVaporParticles();
            vaporActive = true;

            foreach (var limb in person.Limbs)
            {
                GameObject particleGO = new GameObject("SteamVapor");
                particleGO.transform.SetParent(limb.transform);
                particleGO.transform.localPosition = Vector3.zero;

                ParticleSystem particles = particleGO.AddComponent<ParticleSystem>();
                ConfigureSteamParticles(particles);

                vaporParticleSystems.Add(particles);
            }
        }

        private void ConfigureSteamParticles(ParticleSystem particles)
        {
            // Main module - basic particle properties for steam
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.9f, 1f), new Color(0.7f, 0.7f, 0.7f, 1f));
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.gravityModifier = -0.075f;

            // Configure renderer for proper material
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Material steamMaterial = null;

                var spritesShader = Shader.Find("Sprites/Default");
                if (spritesShader != null)
                {
                    steamMaterial = new Material(spritesShader);
                }
                else
                {
                    var unlitShader = Shader.Find("Unlit/Transparent");
                    if (unlitShader != null)
                    {
                        steamMaterial = new Material(unlitShader);
                    }
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

                    Texture2D steamTexture = CreateSteamTexture(128);
                    steamMaterial.mainTexture = steamTexture;
                }
            }

            // Emission
            var emission = particles.emission;
            emission.rateOverTime = 10f;

            // Shape
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.03f;
            shape.radiusThickness = 0.75f;

            // Velocity over lifetime
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            // Size over lifetime
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.4f);
            sizeCurve.AddKey(0.5f, 1.2f);
            sizeCurve.AddKey(1f, 3.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.6f),
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.6f, 0.3f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;

            // Rotation over lifetime
            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-15f, 15f);

            // Noise module
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.3f;
            noise.damping = true;

            // Texture sheet animation
            var textureSheetAnimation = particles.textureSheetAnimation;
            textureSheetAnimation.enabled = false;
        }

        private void RemoveVaporParticles()
        {
            vaporActive = false;

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

                    float normalizedX = offset.x / (size / 3f);
                    float normalizedY = offset.y / (size / 1.5f);

                    float verticalFactor = Mathf.Clamp01(1f - Mathf.Abs(normalizedY));
                    float horizontalFactor = Mathf.Clamp01(1f - Mathf.Abs(normalizedX));

                    float steamShape = verticalFactor * horizontalFactor;

                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                    steamShape = Mathf.Clamp01(steamShape + noise - 0.2f);

                    float distance = Vector2.Distance(pos, center) / (size / 2f);
                    float falloff = Mathf.Clamp01(1f - distance);
                    falloff = Mathf.Pow(falloff, 0.8f);

                    float alpha = steamShape * falloff;

                    if (normalizedY < 0)
                    {
                        alpha *= 1.2f;
                    }
                    else
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
            heatAuraCycles = 0;

            while (heatAuraActive && IsActive)
            {
                heatAuraCycles++;
                ApplyHeatDamage();
                MaintainTankerTemperature();

                yield return new WaitForSeconds(heatDamageInterval);
            }
        }

        private void MaintainTankerTemperature()
        {
            if (person == null || person.Limbs == null) return;

            foreach (var limb in person.Limbs)
            {
                var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
                if (physicalBehaviour != null)
                {
                    if (physicalBehaviour.Temperature > 200f)
                    {
                        physicalBehaviour.Temperature = 200f;
                    }

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

            Vector3 tankerPosition = GetTankerCenterPosition();
            int targetsAffected = 0;

            Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(tankerPosition, heatAuraRadius);
            PersonBehaviour[] allPersons = FindObjectsOfType<PersonBehaviour>();

            foreach (var targetPerson in allPersons)
            {
                if (targetPerson == null || targetPerson == person) continue;

                float distanceToTarget = Vector3.Distance(tankerPosition, targetPerson.transform.position);
                if (distanceToTarget <= heatAuraRadius)
                {
                    foreach (var limb in targetPerson.Limbs)
                    {
                        if (limb != null)
                        {
                            ApplyHeatDamageToLimb(limb, tankerPosition);
                            targetsAffected++;
                        }
                    }
                }
            }

            foreach (var collider in objectsInRange)
            {
                if (collider.gameObject == person.gameObject) continue;
                if (IsTankerLimb(collider.gameObject)) continue;

                var targetLimb = collider.GetComponent<LimbBehaviour>();
                var targetPhysical = collider.GetComponent<PhysicalBehaviour>();

                if (targetLimb != null)
                {
                    if (targetLimb.Person == person) continue;
                    ApplyHeatDamageToLimb(targetLimb, tankerPosition);
                    targetsAffected++;
                }
                else if (targetPhysical != null)
                {
                    if (IsPartOfTanker(targetPhysical.gameObject)) continue;
                    ApplyHeatDamageToPhysicalObject(targetPhysical, tankerPosition);
                    targetsAffected++;
                }
            }

            if (targetsAffected > 0 && heatAuraCycles % 100 == 0)
            {
                ModAPI.Notify($"Heat aura affecting {targetsAffected} targets");
            }
        }

        private Vector3 GetTankerCenterPosition()
        {
            if (person == null || person.Limbs == null || person.Limbs.Length == 0)
            {
                return person != null ? person.transform.position : Vector3.zero;
            }

            Vector3 centerPosition = Vector3.zero;
            int validLimbCount = 0;

            foreach (var limb in person.Limbs)
            {
                if (limb != null && limb.gameObject != null)
                {
                    centerPosition += limb.transform.position;
                    validLimbCount++;
                }
            }

            if (validLimbCount > 0)
            {
                centerPosition /= validLimbCount;
                return centerPosition;
            }
            else
            {
                return person.transform.position;
            }
        }

        private void ApplyHeatDamageToLimb(LimbBehaviour limb, Vector3 heatSource)
        {
            if (limb == null) return;

            float distance = Vector3.Distance(heatSource, limb.transform.position);
            if (distance > heatAuraRadius) return;

            float damageMultiplier = 1f - (distance / heatAuraRadius);
            float actualDamage = heatDamageAmount * damageMultiplier;

            limb.Damage(actualDamage);

            float ignitionChance = heatAuraIgniteChance * damageMultiplier;
            if (UnityEngine.Random.value < ignitionChance)
            {
                IgniteLimb(limb);
            }
        }

        private void ApplyHeatDamageToPhysicalObject(PhysicalBehaviour physicalObject, Vector3 heatSource)
        {
            if (physicalObject == null) return;

            float distance = Vector3.Distance(heatSource, physicalObject.transform.position);
            if (distance > heatAuraRadius) return;

            float damageMultiplier = 1f - (distance / heatAuraRadius);
            float ignitionChance = heatAuraIgniteChance * damageMultiplier * 0.3f;

            if (physicalObject.SimulateTemperature)
            {
                physicalObject.Temperature += 30f * damageMultiplier;
            }

            if (!physicalObject.OnFire && UnityEngine.Random.value < ignitionChance)
            {
                IgnitePhysicalObject(physicalObject);
            }
        }

        private bool IsTankerLimb(GameObject obj)
        {
            if (person == null || person.Limbs == null) return false;

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

            if (obj == person.gameObject) return true;

            Transform current = obj.transform;
            while (current != null)
            {
                if (current.gameObject == person.gameObject)
                {
                    return true;
                }
                current = current.parent;
            }

            return IsTankerLimb(obj);
        }

        private void IgniteLimb(LimbBehaviour limb)
        {
            if (limb == null) return;

            var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
            if (physicalBehaviour != null)
            {
                try
                {
                    physicalBehaviour.Ignite(false);
                    ModAPI.CreateParticleEffect("Flash", limb.transform.position);
                }
                catch
                {
                    CreateFireEffect(limb);
                }
            }
            else
            {
                CreateFireEffect(limb);
            }
        }

        private void IgnitePhysicalObject(PhysicalBehaviour physicalObject)
        {
            if (physicalObject == null) return;

            if (physicalObject.OnFire) return;

            try
            {
                physicalObject.Ignite(false);
                ModAPI.CreateParticleEffect("Flash", physicalObject.transform.position);
            }
            catch
            {
                ModAPI.CreateParticleEffect("Flash", physicalObject.transform.position);
            }
        }

        private void CreateFireEffect(LimbBehaviour limb)
        {
            ModAPI.CreateParticleEffect("Flash", limb.transform.position);
            StartCoroutine(ContinuousFireEffect(limb));
        }

        private IEnumerator ContinuousFireEffect(LimbBehaviour limb)
        {
            float fireDuration = 5.0f;
            float fireInterval = 0.3f;
            float elapsed = 0f;

            while (elapsed < fireDuration && limb != null && limb.gameObject != null)
            {
                if (UnityEngine.Random.value < 0.7f)
                {
                    Vector3 firePos = limb.transform.position + new Vector3(
                        UnityEngine.Random.Range(-0.1f, 0.1f),
                        UnityEngine.Random.Range(-0.1f, 0.1f),
                        0f
                    );
                    ModAPI.CreateParticleEffect("Flash", firePos);
                }

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
            RemoveVaporParticles();
            StopHeatAura();
        }
    }

    // Fireball behavior component to handle lifetime and impact
    public class FireballBehavior : MonoBehaviour
    {
        private float lifetime = 3f;
        private float elapsedTime = 0f;
        private bool hasExploded = false;
        private float explosionRadius = 2f;
        private float explosionDamage = 5f;

        public void Initialize(float lifetime)
        {
            this.lifetime = lifetime;
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;

            // Destroy after lifetime expires
            if (elapsedTime >= lifetime && !hasExploded)
            {
                Explode();
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            // Explode on impact
            if (!hasExploded)
            {
                ModAPI.Notify("Fireball collision detected!");
                Explode();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Backup trigger-based collision
            if (!hasExploded)
            {
                ModAPI.Notify("Fireball trigger detected!");
                Explode();
            }
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            Vector3 explosionPosition = transform.position;

            // Create explosion effect
            ExplosionCreator.Explode(new ExplosionCreator.ExplosionParameters
            {
                Position = explosionPosition,
                CreateParticlesAndSound = true,
                DismemberChance = 0.3f,
                FragmentForce = 3f,
                FragmentationRayCount = 12,
                Range = explosionRadius
            });

            // Apply fire damage to nearby objects
            Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius);
            foreach (var collider in objectsInRange)
            {
                var limb = collider.GetComponent<LimbBehaviour>();
                if (limb != null)
                {
                    limb.Damage(explosionDamage);

                    var physicalBehaviour = limb.GetComponent<PhysicalBehaviour>();
                    if (physicalBehaviour != null && UnityEngine.Random.value < 0.7f)
                    {
                        try
                        {
                            physicalBehaviour.Ignite(false);
                        }
                        catch { }
                    }
                }
                else
                {
                    var physicalBehaviour = collider.GetComponent<PhysicalBehaviour>();
                    if (physicalBehaviour != null)
                    {
                        physicalBehaviour.Temperature += 500f;
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            try
                            {
                                physicalBehaviour.Ignite(false);
                            }
                            catch { }
                        }
                    }
                }
            }

            // Destroy the fireball object
            Destroy(gameObject);
        }
    }
}
