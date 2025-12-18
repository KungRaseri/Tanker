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
        public Texture2D originalSkin;
        public Texture2D originalFlesh;
        public Texture2D originalBone;
        public Texture2D moltenTexture;
        public Texture2D ultraSenseTexture;

        // Mode controllers
        private MoltenModeController moltenModeController;
        private UltraSenseModeController ultraSenseModeController;

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

            // Initialize molten mode controller
            InitializeMoltenModeController();

            // Initialize ultra sense mode controller
            InitializeUltraSenseModeController();
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

        private void InitializeMoltenModeController()
        {
            // Add the MoltenModeController component to this GameObject
            moltenModeController = gameObject.AddComponent<MoltenModeController>();
            moltenModeController.Initialize(person, this, moltenTexture, originalSkin, originalFlesh, originalBone);
        }

        private void InitializeUltraSenseModeController()
        {
            // Add the UltraSenseModeController component to this GameObject
            ultraSenseModeController = gameObject.AddComponent<UltraSenseModeController>();
            ultraSenseModeController.Initialize(person, this, ultraSenseTexture, originalSkin, originalFlesh, originalBone);
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
            // Clear molten mode using the controller
            if (moltenModeController != null && moltenModeController.IsActive)
            {
                moltenModeController.DisableMoltenMode();
            }

            // Clear ultra sense mode using the controller
            if (ultraSenseModeController != null && ultraSenseModeController.IsActive)
            {
                ultraSenseModeController.DisableUltraSenseMode();
            }
        }

        private void ToggleMoltenMode()
        {
            if (moltenModeController != null)
            {
                moltenModeController.ToggleMoltenMode();
            }
        }

        private void ToggleUltraSenseMode()
        {
            if (ultraSenseModeController != null)
            {
                ultraSenseModeController.ToggleUltraSenseMode();
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

            bool moltenModeActive = moltenModeController != null && moltenModeController.IsActive;
            bool ultraSenseModeActive = ultraSenseModeController != null && ultraSenseModeController.IsActive;

            string status = $"Tanker Status Report:\n" +
                          $"Health: {(avgHealth * 100):F0}%\n" +
                          $"Molten Mode: {(moltenModeActive ? "ACTIVE" : "INACTIVE")}\n" +
                          $"Ultra Sense Mode: {(ultraSenseModeActive ? "ACTIVE" : "INACTIVE")}\n";

            ModAPI.Notify(status);
        }

        void OnDestroy()
        {
            // Clean up mode controllers when the component is destroyed
            if (moltenModeController != null)
            {
                moltenModeController.DisableMoltenMode();
            }

            if (ultraSenseModeController != null)
            {
                ultraSenseModeController.DisableUltraSenseMode();
            }
        }
    }
}