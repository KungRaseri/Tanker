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
                        SetupTankerMods(person);

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

        private static void SetupTankerMods(PersonBehaviour person)
        {

        }
    }
}