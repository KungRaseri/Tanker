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
                // Method 1: Use SetBodyTextures with molten texture
                person.SetBodyTextures(moltenTexture, moltenTexture, moltenTexture, 1f);

                ModAPI.Notify("Molten mode activated! Textures applied.");
            }
        }

        private void DisableMoltenMode()
        {
            isMoltenMode = false;

            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                // Restore original textures
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

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
    }
}