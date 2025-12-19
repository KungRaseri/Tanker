# Fireball Launch Feature

## Overview
The Tanker can now launch fireballs from their hands when held by the player!

## How to Use

1. **Spawn a Tanker** entity in the game
2. **Hold one of the Tanker's hands** by clicking and dragging it with your mouse
3. While holding the hand, **press the F key** to launch a fireball

## Features

### Fireball Properties
- **Visual**: Orange glowing sphere with fire particle trail
- **Physics**: Flies in the direction the hand is moving/facing
- **Launch Speed**: 10 units/second
- **Lifetime**: 3 seconds (or until impact)
- **Gravity**: Slight downward curve (0.2x gravity)

### Fireball Effects
- **Explosion on Impact**: Creates an explosion when hitting any object
- **Fire Damage**: Damages and ignites nearby entities on explosion
- **Explosion Radius**: 2 units
- **Damage**: 5 points to limbs in the explosion radius
- **Ignition Chance**: 70% for limbs, 50% for objects

### Launch Direction
The fireball launches in the direction determined by:
1. Hand velocity (if moving)
2. Hand rotation angle (if stationary)

## Implementation Details

### New Components Added

#### `FireballBehavior` Class
- Handles fireball lifetime
- Manages collision detection
- Creates explosion effects on impact
- Applies area damage and fire effects

#### Fireball Launch System (in `MoltenModeController`)
- `Update()` - Checks for F key press each frame
- `CheckAndLaunchFireball()` - Verifies hand is being held
- `IsLimbBeingHeld()` - Detects if hand has a FixedJoint2D (being grabbed)
- `LaunchFireball()` - Creates and launches the fireball
- `CreateFireball()` - Instantiates the fireball GameObject
- `CreateFireballSprite()` - Generates the fireball texture
- `FireballTrailEffect()` - Creates particle trail

### Technical Details

**Key Detection**: Unity's Input system detects `KeyCode.F`
**Held Detection**: Checks for `FixedJoint2D` components on the hand limb
**Hand Limbs**: Automatically finds left/right hands during initialization
**Particles**: Uses `ModAPI.CreateParticleEffect("Flash")` for visual effects

## Code Files Modified

- `MoltenModeController.cs` - Added fireball launch system and FireballBehavior class
- `Tanker.csproj` - Added Unity module references:
  - `UnityEngine.Physics2DModule` (for collision/physics)
  - `UnityEngine.ParticleSystemModule` (for particles)
  - `UnityEngine.InputLegacyModule` (for keyboard input)

## Notes

- The fireball feature works independently of Molten Mode (always available)
- Multiple fireballs can be launched rapidly by repeatedly pressing F
- Fireballs explode on contact with any collider (walls, objects, entities)
- Visual feedback includes flash particle effects and a notification message
- The fireball is a full physics object that interacts with the game world

## Future Enhancements

Possible improvements:
- Cooldown timer between fireball launches
- Require Molten Mode to be active to launch fireballs
- Different fireball types (ice, lightning, etc.)
- Charging system for more powerful fireballs
- Visual charging indicator
- Sound effects for launch and explosion
