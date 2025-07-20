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
                        // Apply custom skin texture to the person
                        SetEntityTextures(person);

                        // Add a helper component to handle delayed setup
                        var helper = person.gameObject.AddComponent<TankerSetupHelper>();
                        helper.person = person;

                        ModAPI.Notify("Tanker deployed! Enhanced armor and durability active.");
                    }
                }
            });
        }

        private static void SetEntityTextures(PersonBehaviour person)
        {

            var skin = ModAPI.LoadTexture("Sprites/Tanker-skin.png");
            var flesh = ModAPI.LoadTexture("Sprites/Tanker-flesh.png");
            var bone = ModAPI.LoadTexture("Sprites/Tanker-bone.png");

            person.SetBodyTextures(skin, flesh, bone, 1);
        }

        private static System.Collections.IEnumerator DelayedContextMenuSetup(PersonBehaviour person)
        {
            yield return new WaitForEndOfFrame();
            SetupTankerContextMenu(person);
        }

        public static void SetupTankerContextMenu(PersonBehaviour person)
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
                        new UnityAction(() => ToggleMoltenMode(person))),

                    new ContextMenuButton("tankerStatusReport", "Status Report", "Show current status of the tanker",
                        new UnityAction(() => ShowStatusReport(person))),
                };

                // Set the buttons list
                contextMenuOptions.Buttons = buttons;
            }
        }

        private static void ToggleMoltenMode(PersonBehaviour person)
        {
            bool moltenActive = person.gameObject.GetComponent<TankerMoltenComponent>() != null;

            if (moltenActive)
            {
                // Disable molten mode
                var moltenComponent = person.gameObject.GetComponent<TankerMoltenComponent>();
                if (moltenComponent != null)
                {
                    UnityEngine.Object.Destroy(moltenComponent);
                }
                ModAPI.Notify("Molten mode disabled");
            }
            else
            {
                // Enable molten mode
                var moltenComponent = person.gameObject.AddComponent<TankerMoltenComponent>();
                moltenComponent.person = person;
                ModAPI.Notify("Molten mode activated!");
            }
        }

        private static void ShowStatusReport(PersonBehaviour person)
        {
            bool moltenActive = person.gameObject.GetComponent<TankerMoltenComponent>() != null;

            float avgHealth = 0f;
            foreach (var limb in person.Limbs)
            {
                avgHealth += limb.Health / limb.InitialHealth;
            }
            avgHealth /= person.Limbs.Length;

            string status = $"Tanker Status Report:\n" +
                          $"Health: {(avgHealth * 100):F0}%\n" +
                          $"Molten Mode: {(moltenActive ? "ACTIVE" : "INACTIVE")}";

            ModAPI.Notify(status);
        }
    }

    // Component to handle molten mode
    public class TankerMoltenComponent : MonoBehaviour
    {
        public PersonBehaviour person;
        private Texture2D originalSkin;
        private Texture2D originalFlesh;
        private Texture2D originalBone;

        void Start()
        {
            ModAPI.Notify("TankerMoltenComponent Start() called");

            if (person != null)
            {
                ModAPI.Notify("Person is not null, starting texture change process");

                // Store original textures
                originalSkin = ModAPI.LoadTexture("Sprites/Tanker-skin.png");
                originalFlesh = ModAPI.LoadTexture("Sprites/Tanker-flesh.png");
                originalBone = ModAPI.LoadTexture("Sprites/Tanker-bone.png");

                // Apply molten textures immediately
                ApplyMoltenTextures();
            }
            else
            {
                ModAPI.Notify("Person is null! Cannot apply textures");
            }
        }

        private void ApplyMoltenTextures()
        {
            ModAPI.Notify("ApplyMoltenTextures method started");

            var moltenTexture = ModAPI.LoadTexture("Sprites/skin_KIAREKAKAMI_PURE_RAGE_PPG.png");

            if (moltenTexture != null)
            {
                ModAPI.Notify($"Molten texture loaded successfully! Size: {moltenTexture.width}x{moltenTexture.height}");

                // Method 1: Use SetBodyTextures
                person.SetBodyTextures(moltenTexture, moltenTexture, moltenTexture, 1f);
                ModAPI.Notify("SetBodyTextures called");

                // Method 2: Force sync on all skin material handlers
                foreach (var limb in person.Limbs)
                {
                    var skinHandler = limb.GetComponent<SkinMaterialHandler>();
                    if (skinHandler != null)
                    {
                        skinHandler.Sync();
                    }
                }
                ModAPI.Notify("SkinMaterialHandler Sync called on all limbs");

                // Method 3: Try material update
                if (person.DefaultMaterial != null)
                {
                    var moltenMaterial = new Material(person.DefaultMaterial);
                    moltenMaterial.mainTexture = moltenTexture;

                    foreach (var limb in person.Limbs)
                    {
                        var skinHandler = limb.GetComponent<SkinMaterialHandler>();
                        if (skinHandler != null && skinHandler.renderer != null)
                        {
                            skinHandler.renderer.material = moltenMaterial;
                        }
                    }
                    ModAPI.Notify("Material-based texture application completed");
                }

                ModAPI.Notify("Molten textures applied successfully!");
            }
            else
            {
                ModAPI.Notify("Failed to load molten texture! Check file path: Sprites/skin_KIAREKAKAMI_PURE_RAGE_PPG.png");
            }
        }

        void OnDestroy()
        {
            // Restore original textures when molten mode is disabled
            if (person != null && originalSkin != null)
            {
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

                // Also restore the original material
                foreach (var limb in person.Limbs)
                {
                    var skinHandler = limb.GetComponent<SkinMaterialHandler>();
                    if (skinHandler != null && skinHandler.renderer != null)
                    {
                        skinHandler.renderer.material = person.ChosenMaterial;
                        skinHandler.Sync();
                    }
                }

                ModAPI.Notify("Original textures restored!");
            }
        }
    }

    // Helper component to handle delayed context menu setup
    public class TankerSetupHelper : MonoBehaviour
    {
        public PersonBehaviour person;

        void Start()
        {
            StartCoroutine(DelayedSetup());
        }

        private System.Collections.IEnumerator DelayedSetup()
        {
            yield return new WaitForEndOfFrame();
            TankerMod.SetupTankerContextMenu(person);

            // Remove this helper component after setup is complete
            Destroy(this);
        }
    }
}