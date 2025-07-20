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
                OriginalItem = ModAPI.FindSpawnable("Rod"),
                NameOverride = "placeholder item" + ModTag,
                DescriptionOverride = "It's 6 am and theres a fly in my room and I'm scared of it.",
                CategoryOverride = ModAPI.FindCategory("Entities"),
                ThumbnailOverride = ModAPI.LoadSprite("sprites/placeholder.png"), //For convienence, it's best to place all sound effects and sprites within the sfx and sprites folder that comes with this base.
                AfterSpawn = (Instance) =>
                {
                    ModAPI.Notify("This line will run once the item is spawned.");
                }
            });
        }
    }
}