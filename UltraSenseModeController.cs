using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanker
{
    public class UltraSenseModeController : MonoBehaviour
    {
        // Reference to the parent tanker
        private PersonBehaviour person;
        private TankerMod tankerMod;
        
        // Ultra sense mode state
        public bool IsActive { get; private set; } = false;
        
        // Textures
        private Texture2D ultraSenseTexture;
        private Texture2D originalSkin;
        private Texture2D originalFlesh;
        private Texture2D originalBone;

        public void Initialize(PersonBehaviour person, TankerMod tankerMod, 
                              Texture2D ultraSenseTexture, Texture2D originalSkin, 
                              Texture2D originalFlesh, Texture2D originalBone)
        {
            this.person = person;
            this.tankerMod = tankerMod;
            this.ultraSenseTexture = ultraSenseTexture;
            this.originalSkin = originalSkin;
            this.originalFlesh = originalFlesh;
            this.originalBone = originalBone;
        }

        public void ToggleUltraSenseMode()
        {
            if (IsActive)
            {
                DisableUltraSenseMode();
            }
            else
            {
                EnableUltraSenseMode();
            }
        }

        public void EnableUltraSenseMode()
        {
            IsActive = true;

            if (ultraSenseTexture != null)
            {
                // Apply ultra sense texture to all body parts
                person.SetBodyTextures(ultraSenseTexture, ultraSenseTexture, ultraSenseTexture, 1f);

                ModAPI.Notify("Ultra sense mode activated! Enhanced perception active.");
            }
        }

        public void DisableUltraSenseMode()
        {
            IsActive = false;

            if (originalSkin != null && originalFlesh != null && originalBone != null)
            {
                // Restore original textures
                person.SetBodyTextures(originalSkin, originalFlesh, originalBone, 1f);

                ModAPI.Notify("Ultra sense mode deactivated! Original textures restored.");
            }
        }
    }
}
