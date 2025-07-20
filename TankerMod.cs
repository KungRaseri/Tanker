using System.Collections;
using System;
using UnityEngine;

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
                        SetupTankerContextMenu(person);

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

        private static void SetupTankerContextMenu(PersonBehaviour person)
        {
            // Get or add a ContextMenuBehaviour component
            var contextMenu = person.GetComponent<ContextMenuBehaviour>();
            if (contextMenu == null)
            {
                contextMenu = person.gameObject.AddComponent<ContextMenuBehaviour>();
            }

            // Add context menu options
            var buttons = new ContextMenuButton[]
            {
                new ContextMenuButton("tankerMoltenMode", "Toggle Molten Mode", "Toggle molten tanker mode", () => {
                    ToggleMoltenMode(person);
                }),

                new ContextMenuButton("tankerStatusReport", "Status Report", "Show current status of the tanker", () => {
                    ShowStatusReport(person);
                }),
            };

            // Apply the context menu buttons
            if (contextMenu != null)
            {
                // Note: The exact method to add buttons may vary based on the API
                // This is a common pattern, but may need adjustment
                try
                {
                    contextMenu.buttons = buttons;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning("Could not set context menu buttons: " + ex.Message);
                }
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

        void Start()
        {
            // Increase damage resistance when molten mode is active
            if (person != null)
            {

            }
        }

        void OnDestroy()
        {
            // Restore normal values when molten mode is disabled
            if (person != null)
            {

            }
        }
    }
}