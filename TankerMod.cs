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
        private List<GameObject> vaporParticles = new List<GameObject>();
        private bool vaporActive = false;
        private Coroutine vaporCoroutine;

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
            
            // Start continuous vapor generation
            if (vaporCoroutine != null)
            {
                StopCoroutine(vaporCoroutine);
            }
            vaporCoroutine = StartCoroutine(ContinuousVaporEffect());

            ModAPI.Notify($"Started persistent vapor effects on {person.Limbs.Length} limbs");
        }

        private IEnumerator ContinuousVaporEffect()
        {
            while (vaporActive && isMoltenMode)
            {
                // Create vapor effect on each limb
                foreach (var limb in person.Limbs)
                {
                    if (limb != null && limb.gameObject != null)
                    {
                        // Create vapor at limb position with slight random offset
                        Vector3 vaporPos = limb.transform.position + new Vector3(
                            UnityEngine.Random.Range(-0.1f, 0.1f),
                            UnityEngine.Random.Range(-0.05f, 0.1f),
                            0f
                        );
                        
                        var vapor = ModAPI.CreateParticleEffect("Vapor", vaporPos);
                        if (vapor != null)
                        {
                            // Store reference for cleanup (though they'll auto-destroy)
                            vaporParticles.Add(vapor);
                            
                            // Remove from list after a delay to prevent memory issues
                            StartCoroutine(RemoveVaporAfterDelay(vapor, 3f));
                        }
                    }
                }
                
                // Wait before creating next batch of vapor
                yield return new WaitForSeconds(0.3f); // Create new vapor every 0.3 seconds
            }
        }

        private IEnumerator RemoveVaporAfterDelay(GameObject vapor, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (vaporParticles.Contains(vapor))
            {
                vaporParticles.Remove(vapor);
            }
        }

        private void RemoveVaporParticles()
        {
            vaporActive = false;
            
            // Stop the continuous vapor coroutine
            if (vaporCoroutine != null)
            {
                StopCoroutine(vaporCoroutine);
                vaporCoroutine = null;
            }
            
            // Clean up any existing vapor particles
            foreach (var particles in vaporParticles)
            {
                if (particles != null)
                {
                    UnityEngine.Object.Destroy(particles);
                }
            }
            vaporParticles.Clear();
        }

        void OnDestroy()
        {
            // Clean up particles when the component is destroyed
            RemoveVaporParticles();
        }
    }
}