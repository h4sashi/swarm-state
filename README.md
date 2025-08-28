# Top-Down Action Prototype

A Unity-based top-down action game built with primitives only, featuring smooth player movement, AI enemies, power-up systems, and object pooling. Created as a technical demonstration of game architecture patterns and Unity development practices.

## 🎮 Game Engine
- **Unity 6000.1.1.0f1**

## 🎮 Game Features

### Core Gameplay
- **Smooth 4-directional movement** with WASD/Arrow keys
- **Dash ability** with cooldown system for combat and mobility
- **Enemy elimination** through dash attacks
- **Survival-based scoring** system
- **Progressive difficulty scaling**

### Enemy AI System
- **Chaser Enemies**: Aggressively pursue the player
- **Shooter Enemies**: Maintain distance and fire projectiles
- **State Machine Architecture** for complex AI behaviors
- **Object pooling** for performance optimization

### Power-Up System
- **Speed Boost**: Increases movement speed and acceleration
- **Double Dash**: Reduces dash cooldown and provides extra charges
- **Intelligent stacking**: Power-ups extend duration or stack effects
- **Visual feedback** and UI integration

## 🏗️ Architecture & Code Structure

### Core Systems

#### **Player System**
```
Player/
├── PlayerController.cs      # Main player coordinator
├── PlayerDash.cs           # Dash ability implementation
├── PlayerMovement.cs       # Movement and physics
├── PlayerHealth.cs         # Health management
└── PlayerHealthUI.cs       # Health display UI
```

#### **Enemy System**
```
Enemies/
├── Core/
│   ├── EnemyController.cs      # Base enemy behavior
│   ├── EnemyHealth.cs          # Enemy health system
│   ├── EnemyMovement.cs        # Movement mechanics
│   └── EnemyStateMachine.cs    # AI state management
├── Data/
│   ├── EnemyState.cs           # State definitions
│   └── EnemyType.cs            # Enemy type enums
├── States/
│   ├── AttackState.cs          # Attack behavior
│   ├── ChaseState.cs           # Pursuit logic
│   ├── DeathState.cs           # Death handling
│   ├── EnemyStateBase.cs       # Base state class
│   └── IdleState.cs            # Idle behavior
└── Types/
    ├── ChaserEnemy.cs          # Chaser AI implementation
    └── ShooterEnemy.cs         # Shooter AI implementation
```

#### **Power-Up System**
```
PowerUps/
├── BasePowerUp.cs              # Abstract base class
├── DoubleDashPowerUp.cs        # Dash enhancement
├── SpeedBoostPowerUp.cs        # Speed modification
├── PowerUpPickup.cs            # Collectible implementation
└── PowerUpFeedback.cs          # Visual/audio feedback
```

#### **Systems & Management**
```
Systems/
├── EnemyObjectPool.cs          # Object pooling for enemies
├── EnemySpawner.cs             # Enemy spawn management
├── PowerUpSpawner.cs           # Power-up spawn system
├── ScoreDisplayUI.cs           # UI score display
├── ScoreSystem.cs              # Score calculation
├── SoundManager.cs             # Audio management
└── WaveCountdownUI.cs          # Wave progression UI
```

### **Configuration System**
All gameplay values are exposed through ScriptableObjects for easy balancing:

```
Config/
├── EnemyConfigs/               # Enemy behavior settings
├── PowerUpConfigs/             # Power-up properties
├── EnemyConfig.cs              # Enemy configuration
├── PlayerConfig.cs             # Player settings
├── PowerUpConfig.cs            # Power-up definitions
└── ProjectileConfig.cs         # Projectile properties
```

### **Interface Design**
```
Interfaces/
├── IAbility.cs                 # Ability system interface
├── IConfigurable.cs            # Configuration interface
├── IDamageable.cs              # Damage system interface
├── IMovementController.cs      # Movement interface
├── IPowerUp.cs                 # Power-up interface
└── IStatModifier.cs            # Stat modification interface
```

## 🎯 Design Patterns Implemented

### **State Machine Pattern**
- Clean AI behavior management
- Extensible state system for enemy types
- Clear separation of concerns

### **Object Pooling**
- Performance-optimized projectile and enemy management
- Eliminates garbage collection spikes
- Scalable for high-frequency spawning

### **Event-Driven Architecture**
```csharp
// GameEvents.cs - Centralized event system
public static class GameEvents
{
    public static System.Action<Vector3> OnPlayerDash;
    public static System.Action<PowerUpType, bool> OnPowerUpStateChanged;
    public static System.Action<int> OnScoreChanged;
    // ... additional events
}
```

### **ScriptableObject Configuration**
- Designer-friendly parameter tweaking
- Runtime-safe value modifications
- Modular configuration system

### **Interface Segregation**
- Small, focused interfaces for specific behaviors
- Promotes loose coupling and testability
- Easy to extend and modify

## 🔧 Technical Implementation

### **Input System**
- Unity's New Input System integration
- Responsive movement with acceleration/deceleration
- Cooldown-based ability system

### **Physics & Movement**
- Smooth Rigidbody2D-based movement
- Collision detection for combat
- Dash mechanics with curve-based speed control

### **Performance Optimizations**
- Object pooling for frequently spawned objects
- Efficient coroutine management
- UI update throttling (10fps for smooth countdown)

### **Audio Integration**
- SoundManager for centralized audio control
- Event-driven sound triggering
- Configurable audio feedback

## 🚀 Getting Started

### Prerequisites
- Unity 2022.3 LTS or newer
- New Input System package

### Setup
1. Clone the repository
2. Open the project in Unity
3. Load the main scene from `Scenes/`
4. Press Play to start

### Controls
- **WASD/Arrow Keys**: Movement
- **Space/Left Click**: Dash ability
- **Collect power-ups**: Walk over glowing objects

## 🎮 Gameplay Flow

1. **Movement**: Navigate using WASD with smooth acceleration
2. **Combat**: Use dash to defeat enemies (watch the cooldown)
3. **Survival**: Avoid enemy projectiles and contact damage
4. **Power-ups**: Collect enhancements for temporary advantages
5. **Progression**: Survive longer waves for higher scores

## 📈 Extensibility

The architecture supports easy expansion:
- **New Enemy Types**: Inherit from `EnemyController` and add states
- **Additional Power-ups**: Extend `BasePowerUp` with new effects
- **Extra Abilities**: Implement `IAbility` interface
- **New UI Elements**: Hook into the event system
- **Different Weapons**: Use the projectile pool system

## 🔍 Code Quality Features

- **Comprehensive documentation** with XML comments
- **Consistent naming conventions** throughout
- **Error handling** and null-checking
- **Modular design** for easy testing
- **Clean separation** of concerns
- **Performance considerations** in all systems

## 📚 References & Inspirations

- **Reference Game**: [Gundotz](https://theferfactor.itch.io/gundotz)
- **State Machine Pattern**: Game Programming Patterns
- **Object Pooling**: Unity Performance Best Practices
- **ScriptableObject Architecture**: Unity Learn Tutorials


