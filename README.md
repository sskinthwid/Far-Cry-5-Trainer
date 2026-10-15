# Hope Control — Advanced Far Cry 5 Native Trainer & Runtime Editor

[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-blue.svg)](#)
[![Target](https://img.shields.io/badge/Target-Far%20Cry%205%20(FC__m64.dll)-red.svg)](#)
[![Framework](https://img.shields.io/badge/.NET%20Framework-4.0%20%2F%20WPF-purple.svg)](#)
[![Architecture](https://img.shields.io/badge/Engine-Pure%20x64%20Assembly%20Caves-success.svg)](#)
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero%20External%20Runtimes-brightgreen.svg)](#)
[![Validation](https://img.shields.io/badge/Self--Test-100%25%20PASS-brightgreen.svg)](#)

A high-performance, pure C# / WPF trainer, memory manipulator, and configuration suite for **Far Cry 5**. Built directly on top of native Win32 APIs and custom x64 machine-code emitter caves. Operates completely standalone with **zero Cheat Engine runtime dependencies**, **zero external DLL injectors**, and **no keyboard/mouse input simulation**.

---

## ⚡ Quick Start

1. Launch **Far Cry 5** and load your single-player campaign.
2. Run **`HopeControl.exe`**.
3. Click **`Connect to game`** (or let it auto-detect PID).
4. Toggle your desired modifications or adjust sliders in real-time.
5. Hit **`Restore all`** (<kbd>F12</kbd>) at any time to instantly restore 100% of the game's original memory bytes.

---

## 🎮 Key Capabilities & Feature Matrix

### 🏃 1. Player & Survival
| Feature | ID | Description | Default |
| :--- | :--- | :--- | :--- |
| **God Mode** | `health` | Prevents combat, falling, and wingsuit damage via native damage hooks. | Toggle (Off) |
| **Infinite Body Armor** | `armor` | Preserves player armor health without granting armor to enemies. | Toggle (Off) |
| **Infinite Stamina** | `stamina` | Completely eliminates sprinting and physical exhaustion drain. | Toggle (Off) |
| **Infinite Oxygen** | `oxygen` | Eliminates underwater and suffocation oxygen consumption. | Toggle (Off) |
| **No Fall Damage** | `no-fall-damage` | Nullifies lethal impact velocity damage from heights and wingsuit crashes. | Toggle (Off) |
| **No Ragdoll** | `no-ragdoll` | Keeps player in full control, preventing stumble or ragdoll states from falls. | Toggle (Off) |
| **Stealth / Undetected** | `stealth` | Prevents enemy visual and auditory detection meters from filling. | Toggle (Off) |
| **Energy Overheat Protection** | `energy-overheat` | Eliminates weapon and equipment overheating meters. | Toggle (Off) |
| **Infinite Gravity Belt** | `gravity-belt` | Infinite fuel for the *Lost on Mars* DLC gravity belt / jetpack. | Toggle (Off) |
| **Infinite Syringe Buffs** | `buff-duration` | Freezes active medical, combat, and speed syringe durations. | Toggle (Off) |

---

### 🎯 2. Weapons & Combat
| Feature | ID | Description | Default |
| :--- | :--- | :--- | :--- |
| **No Weapon Recoil** | `recoil` | **Camera-safe:** Completely zeroes weapon kickback while keeping mouse camera look & aim 100% smooth and unlocked. | Toggle (Off) |
| **Zero Bullet Spread** | `spread` | Eliminates cone of fire for pin-point accuracy on all firearms. | Toggle (Off) |
| **No Scoped Sway** | `sway` | Eliminates scope drift and breathing movement for sniper rifles. | Toggle (Off) |
| **No Reload** | `no-reload` | Preserves loaded magazine counts; shoot continuously without reloading. | Toggle (Off) |
| **Infinite Reserve Ammo** | `ammo` | Prevents reserve ammunition depletion across all firearm calibers. | Toggle (Off) |
| **Rapid Fire** | `rapid-fire` | Dynamic multiplier scaling weapon cycle and firing rates. | `1.0×` (Numeric) |
| **Instant Bow Draw** | `bow-draw` | Instantly draws arrows to full tension with zero pull-back latency. | Toggle (Off) |
| **Turret Overheat Prevention** | `turret-heat` | Fire mounted vehicle and ground heavy turrets continuously. | Toggle (Off) |
| **Projectile Speed Multiplier** | `projectile-speed` | Accelerates bullet, rocket, and arrow flight velocities. | `1.0×` (Numeric) |
| **Projectile Damage Multiplier** | `projectile-damage` | Scales bullet and explosive impact damage. | `1.0×` (Numeric) |
| **One-Hit Kill** | `one-hit-kill` | Instantly eliminates hostile combatants and predators on first hit. | Toggle (Off) |
| **Hostile Damage / Armor Scaling** | `npc-damage`, `npc-armor` | Configure incoming and outgoing damage factors for enemies and wildlife. | `1.0×` (Numeric) |

---

### 📍 3. Teleportation & Waypoint Navigation
| Feature | Control | Hotkey | Description |
| :--- | :--- | :--- | :--- |
| **Teleport to Waypoint** | Toolbar Button | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>W</kbd> | Instantly moves the player to the active map marker. |
| **Auto Teleport to Waypoint** | Runtime Toggle | `auto-teleport-waypoint` | Continuously locks player position to the map waypoint. |
| **Save Position Bookmark** | Toolbar Button | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>S</kbd> | Saves current 3D coordinates ($X, Y, Z$) to memory. |
| **Load Position Bookmark** | Toolbar Button | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>L</kbd> | Teleports the player back to the saved coordinate bookmark. |
| **Live HUD Coordinates** | Status Display | *Real-Time* | Continuous HUD readout: `Player: X, Y, Z │ Saved: X, Y, Z │ Waypoint: X, Y, Z`. |

---

### ⏱️ 4. Missions, Stunts & World Physics
| Feature | ID | Description | Default |
| :--- | :--- | :--- | :--- |
| **Freeze Mission Timer** | `mission-timer` | Freezes countdown clocks during Clutch Nixon stunt races, wingsuit courses, and timed story trials. | Toggle (Off) |
| **Instant Interactions** | `interaction-delay` | Eliminates hold-key delays for reviving allies, looting, and unlocking doors. | Toggle (Off) |
| **Game Speed / Time Multiplier** | `time-scale` | Accelerate or decelerate global world physics and clock delta. | `1.0×` (Numeric) |
| **Movement Speed Multiplier** | `move-speed` | Scales sprinting, walking, and crouching ground speeds. | `1.0×` (Numeric) |
| **Jump Velocity Multiplier** | `jump-speed` | Increases vertical leap heights for platforming across terrain. | `1.0×` (Numeric) |
| **Swim Speed Multiplier** | `swim-speed` | Increases swimming and underwater propulsion speeds. | `1.0×` (Numeric) |
| **Infinite Photo Mode Range** | `photo-range` | Removes camera distance radius constraints in Photo Mode. | Toggle (Off) |

---

### 🚗 5. Vehicles & Aircraft
| Feature | ID | Description | Default |
| :--- | :--- | :--- | :--- |
| **Vehicle God Mode** | `vehicle-god` | Dynamically tracks player's occupied vehicle pointer and protects it from damage and crashes. | Toggle (Off) |
| **Infinite Aircraft Ordnance** | `aircraft-bombs` | Unlimited bombs, rockets, flares, and countermeasures for planes and helicopters. | Toggle (Off) |

---

### 🎒 6. Inventory, Resources & Currency Editor
| Feature | Type | Description |
| :--- | :--- | :--- |
| **Interactive Inventory Editor** | GUI Editor | Read, edit, and queue precise quantities (0 – 1,000,000,000) for US Cash, Mars Energy, Medkits, Crafting Components, and Ammunition. |
| **`⚡ 10× Resources`** | Quick Boost | One-click button to multiply all currently held resources by 10× up to max capacity. |
| **Pickup Multiplier** | Multiplier | Multiplies loot, ammo, and materials acquired from containers and fallen enemies. |
| **Capacity Multiplier** | Multiplier | Expands maximum carrying limits across ammunition bags and backpacks. |
| **Infinite Resource Toggles** | Runtime Switches | Independent toggles that freeze consumption for Cash, Mars Energy, Medkits, Crafting Parts, Arrows, Flamethrower Fuel, AP Ammo, Bait, Grenades, Molotovs, C4, Proximity Mines, Pipe Bombs, Dynamite, Throwing Knives, and Flares. |

---

### 🎣 7. Fishing & Progression
| Feature | ID | Description | Default |
| :--- | :--- | :--- | :--- |
| **Unbreakable Fishing Line** | `fishing-line` | Prevents line tension breakage when catching hard-fighting fish. | Toggle (Off) |
| **Fast Reel Speed** | `fishing-reel` | Rapidly reels in hooked fish to complete catches in seconds. | Toggle (Off) |
| **Fish Weight Multiplier** | `fish-weight` | Multiplies recorded catch weight for records and selling prices. | `1.0×` (Numeric) |
| **XP Multiplier** | `xp` | Multiplies experience points earned from missions and activities. | `1.0×` (Numeric) |
| **Perk Points Multiplier** | `perk-reward` | Multiplies perk magazine and challenge completion point rewards. | `1.0×` (Numeric) |
| **Free Perk Unlocks** | `perk-spend` | Allows unlocking perk skills without consuming perk points. | Toggle (Off) |
| **Store Unlocker** | `store` | Bypasses progression locks for weapons, vehicles, and apparel in shops. | Toggle (Off) |

---

### ⚙️ 8. Integrated Profile & Game Settings Editor
Access **116 game profile settings** directly from the UI without launching the game:
- **Display & Graphics**: Resolutions, Refresh Rates, FPS Cap, Field of View (FOV), Resolution Scale, Motion Blur, V-Sync, HDR, and detail quality presets.
- **Audio & Controls**: Mouse smoothing/acceleration, sensitivity curves, controller deadzones, volume channels, and accessibility options.
- **Safety**: Automatically makes timestamped `.bak` backups before writing; safely merges XML fields without corrupting unrelated profile nodes.

---

## ⌨️ Default Hotkeys

| Hotkey | Action |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>W</kbd> | **Teleport to Waypoint** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>S</kbd> | **Save Current Position Bookmark** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>L</kbd> | **Load / Teleport to Saved Position** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>1</kbd> | Toggle **God Mode** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>2</kbd> | Toggle **Movement Speed Boost** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>3</kbd> | Toggle **Infinite Reserve Ammo** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>4</kbd> | Toggle **No Reload** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>5</kbd> | Toggle **No Recoil** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>6</kbd> | Toggle **Zero Spread** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>7</kbd> | Toggle **Infinite Stamina** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>8</kbd> | Toggle **Infinite Oxygen** |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>9</kbd> | Toggle **Infinite Aircraft Ordnance** |
| <kbd>F12</kbd> | **Emergency Restore All** (Restores all original game code) |

---

## 🏗️ Architecture & Safety Engineering

```
┌─────────────────────────────────────────────────────────────┐
│                       HopeControl.exe                       │
│      WPF GUI  •  Input Validation  •  Diagnostic Engine     │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
   Atomic Thread Management            x64 Assembly Emitters
  ┌────────────┴────────────┐         ┌────────┴─────────────┐
  │ NtSuspendProcess        │         │ Rel32 Trampolines    │
  │ Thread RIP Check        │         │ Entity Wielder Guard │
  │ WriteProcessMemory      │         │ Original Code Replay │
  │ FlushInstructionCache   │         │ Param Injections     │
  │ NtResumeProcess         │         │ Independent Bitmasks │
  └─────────────────────────┘         └──────────────────────┘
               │                               │
               ▼                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    FarCry5 (FC_m64.dll)                     │
│                Protected Single-Player Memory               │
└─────────────────────────────────────────────────────────────┘
```

1. **Non-Intrusive Weapon Cave Emitters**:
   Unlike generic memory trainers that patch global instructions (affecting NPCs or breaking player camera look-angles), Hope Control resolves the weapon wielder (`[r13+0x70]`) and owner (`[r13+0x80]`) against the local player pointer (`[[HealthHook]+8]`). Recoil suppression operates solely on the weapon rate vector, leaving camera rotation pitch and yaw completely free.
2. **Atomic Memory Patching**:
   Instruction injection utilizes Windows NT thread context inspection (`NtSuspendProcess`, `GetThreadContext`). If any running thread's instruction pointer (`RIP`) falls within a target byte patch region, the trainer yields before patching to eliminate crash hazards.
3. **Zero Runtime Dependencies**:
   Compiled strictly against the standard `.NET Framework 4.0` standard library. Requires no Cheat Engine, no external DLL injection libraries, and no third-party background services.
4. **Clean Restoration Guarantee**:
   Every modified byte is recorded before alteration. Detaching, exiting, or pressing <kbd>F12</kbd> restores identical original disk bytes to memory.

---

## 🛠️ Build & Verification

### Build from Source
The project builds using Microsoft's native C# compiler (`csc.exe` / MSBuild):
```powershell
.\build.ps1 -OutputDirectory release
```

### Automated Self-Tests & Diagnostics
Run the integrated verification suite without or with a running game instance:

```powershell
# Run the 10-suite offline native guard & assembly verification
.\release\HopeControl.exe --self-test

# Run live roundtrip diagnostics against running FarCry5.exe
.\release\HopeControl.exe --diagnose --roundtrip

# Verify live weapon ownership and hook compatibility
.\release\HopeControl.exe --weapon-check

# Verify live ballistics and projectile hooks
.\release\HopeControl.exe --ballistics-check

# Verify inventory descriptor queries without modifying balances
.\release\HopeControl.exe --inventory-check
```

---

## 🔒 Notes & Compatibility

- **Target Game Version**: Far Cry 5 Title Update 1.011 (`FC_m64.dll` SHA256: `00833fae4d5d70213158a146ca98439b28a8e962b934885ecccd906204880af2`). Compatible with Steam, Ubisoft Connect, and DRM-free editions.
- **Single-Player Only**: Designed strictly for the single-player story campaign and arcade solo/co-op mode. Do not use in competitive multiplayer modes.
- **Silver Bars**: Hope Control does **not** modify Silver Bar balances. Silver Bars are verified server-side through Ubisoft platform accounts; memory edits to premium balances are rejected by design.

---

## 📜 License & Disclaimers

This software is for educational, single-player modification, and reverse engineering research purposes. *Far Cry* and *Ubisoft* are registered trademarks of Ubisoft Entertainment. This project is not affiliated with, endorsed by, or connected to Ubisoft.
