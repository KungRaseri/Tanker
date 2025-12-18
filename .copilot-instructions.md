# Tanker Mod - People Playground

This is a mod for the game People Playground that adds a custom "Tanker" entity with special abilities.

## Project Overview

The Tanker mod creates a heavily armored tank operator entity based on the Android with enhanced durability and two special modes:
- **Molten Mode**: Transforms the Tanker with heat aura, steam particles, and fire damage
- **Ultra Sense Mode**: Enhanced perception mode with visual texture changes

## Architecture

The mod uses a modular architecture with separate controllers for each mode:

```
TankerMod (Main Controller)
├── MoltenModeController - Heat aura, steam particles, explosion effects
├── UltraSenseModeController - Texture management and visual effects
└── Core Features - Stats, health, context menu, setup
```

## Key Files

- `TankerMod.cs` - Main mod class and entity registration
- `MoltenModeController.cs` - All molten mode functionality
- `UltraSenseModeController.cs` - All ultra sense mode functionality
- `mod.json` - Mod metadata and configuration
- `Tanker.csproj` - Project file with dependencies

## People Playground API Guidelines

### Core Components
- `PersonBehaviour` - Main person/entity component
- `LimbBehaviour` - Individual limb component with health/damage
- `PhysicalBehaviour` - Physics and temperature simulation
- `ModAPI` - Main mod API for registration and utilities

### Common Patterns
- Use `ModAPI.Register()` with `Modification` for entity registration
- Use `AfterSpawn` callback to add custom components
- Use `ContextMenuOptionComponent` and `ContextMenuButton` for right-click menus
- Use `person.SetBodyTextures()` for texture changes
- Use `limb.Damage()` for health damage
- Use `physicalBehaviour.Ignite()` for fire effects
- Use `ModAPI.CreateParticleEffect()` for visual effects
- Use `ExplosionCreator.Explode()` for explosions

### Texture Management
- Load textures with `ModAPI.LoadTexture("path")`
- Apply with `person.SetBodyTextures(skin, flesh, bone, alpha)`
- Store original textures for restoration

### Particle Systems
- Create custom particle systems with `GameObject.AddComponent<ParticleSystem>()`
- Configure main, emission, shape, velocity, size, color, rotation, and noise modules
- Parent to limbs for attachment: `particleGO.transform.SetParent(limb.transform)`

### Heat/Fire Effects
- Use `Physics2D.OverlapCircleAll()` for area detection
- Use `FindObjectsOfType<PersonBehaviour>()` for person detection
- Apply damage with distance-based scaling
- Use `physicalBehaviour.Ignite(false)` for ignition
- Maintain temperature with `physicalBehaviour.Temperature`

## Code Style Guidelines

### Naming Conventions
- Use PascalCase for classes, methods, and properties
- Use camelCase for fields and local variables
- Use descriptive names: `moltenModeController`, `heatAuraRadius`

### Organization
- Keep mode-specific logic in separate controller classes
- Use public properties for state exposure: `public bool IsActive { get; private set; }`
- Use initialization methods: `Initialize(person, tankerMod, textures...)`
- Implement proper cleanup in `OnDestroy()`

### Error Handling
- Check for null references before using components
- Use try-catch for API calls that might fail
- Provide fallback behavior when possible

### Performance
- Cache component references instead of repeated GetComponent calls
- Use coroutines for continuous effects (heat aura, particles)
- Clean up resources properly (particles, coroutines)

## Common Patterns in This Project

### Mode Controllers
```csharp
public class ModeController : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;
    
    public void Initialize(PersonBehaviour person, TankerMod tankerMod, textures...) { }
    public void ToggleMode() { }
    public void EnableMode() { }
    public void DisableMode() { }
    void OnDestroy() { /* cleanup */ }
}
```

### Context Menu Setup
```csharp
var buttons = new List<ContextMenuButton>
{
    new ContextMenuButton("id", "Display Name", "Description",
        new UnityAction(() => MethodCall()))
};
contextMenuOptions.Buttons = buttons;
```

### Particle System Creation
```csharp
GameObject particleGO = new GameObject("ParticleName");
particleGO.transform.SetParent(limb.transform);
ParticleSystem particles = particleGO.AddComponent<ParticleSystem>();
// Configure modules...
```

### Area Damage Detection
```csharp
Vector3 position = GetCenterPosition();
Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(position, radius);
PersonBehaviour[] allPersons = FindObjectsOfType<PersonBehaviour>();
```

## Debugging Tips

- Use `ModAPI.Notify()` for in-game debug messages
- Check Unity console for errors and warnings
- Verify texture loading with null checks
- Test mode transitions thoroughly
- Ensure proper cleanup to prevent memory leaks

## Sprite Requirements

The mod expects these texture files in the `sprites/` directory:
- `Tanker-skin.png` - Default skin texture
- `Tanker-flesh.png` - Default flesh texture  
- `Tanker-bone.png` - Default bone texture
- `Tanker-molten.png` - Molten mode texture
- `Tanker-ultrasense.png` - Ultra sense mode texture
