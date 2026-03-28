# 💀 Biodeath Arena: The Horde Survival

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![Genre](https://img.shields.io/badge/Genre-Action_Horde_Survival-red.svg)](#)
[![Style](https://img.shields.io/badge/Style-Low_Poly-green.svg)](#)

**Biodeath Arena** is a brutal, adrenaline-fueled Action Horde Survival game. Thrust into an expansive, desolate wasteland, players must survive against relentless waves of grotesque biological mutations. Armed with devastating free-aim combat and backed by a customizable squad of allied bots, you must hold the line against swarms of beasts and towering reptilian monstrosities.

---

### 🛡️ Core Pillars
1. **Action-Survival Combat:** Forget tab-targeting. Combat is visceral and free-aimed. Your attacks cleave through hordes of swarming enemies. Area control and movement are your only lifeline.
2. **Brutal Bio-Mutations:** The arena is cursed. You will face hordes of berserk Monkeys, Sabrewulfs, Piranhas, and floating Killer Bees. 
3. **Colossal Boss Encounters:** At specific intervals, colossal T-Rexes (5x normal size) join the fray, destroying the very environment to get to you.

---

## ✨ Key Features (Current MVP)

- **🖥️ Main Menu Hub:** Start your session by setting your Player Name and choosing how many Allied Bots you want to bring into the slaughter.
- **🔫 Horde Weapon Drops:** Every match begins with **10 floor weapons per allied combatant**. Assault rifles, shotguns, and flamethrowers spawn across the arena for the whole squad.
- **🎮 Free-Action Combat:** Fireball, melee, and weapon combat now coexist. Weapons are auto-picked by walking over them, can be dropped with `Q`, and ranged weapons vanish when their ammo (20 rounds) is exhausted.
- **🔥 Enhanced Fireball:** Fireball direct hits deal **15-25** damage and explode in a **5 meter AoE** for **5-10** extra damage.
- **♨️ Flamethrower Upgrade:** The old water gun is now a **20 meter flamethrower** with infinite fuel and **5-25 DPS**.
- **🤖 Smart Allies:** Allied bots now pick up and prioritize equipped weapons to help you thin the horde.
- **🌊 3-Wave Attrition:** Survive escalating numbers of enemies per wave (20, 40, to 60 base monsters).
- **🦖 Environment-Shattering Bosses:** Three colossal bosses (T-Rexes) spawn per wave. They are so massive they obliterate arena obstacles in their path.
- **🌐 Solo First, LAN Future:** Currently a finely-tuned Singleplayer experience, built to support LAN Co-op.

---

## 🕹️ Controls

| Action | Control |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` |
| **Orbit Camera** | `Right Click` (Hold) |
| **Jump** | `Space` |
| **Swap Target** | `Tab` |
| **Fireball** | `1` or `Numpad 1` |
| **Summon Dog** | `2` or `Numpad 2` |
| **Melee Strike** | `3` or `Numpad 3` |
| **Weapon Attack** | `4` or `Numpad 4` |
| **Drop Weapon** | `Q` |
| **Pick Up Weapon** | `E` |
| **Reset Match** | `R` |

---

## 🛠️ Tech Stack & Setup

- **Engine:** Unity 2022.3 (Built-in Render Pipeline / URP compatible)
- **Combat Architecture:** Fully physics-based hit detection tailored for massive horde battles and performance.

### Quick Start
1. Open the project in Unity.
2. Load `Assets/Scenes/GetStarted_Scene.unity`.
3. Press **Play** and survive the horde.

### 🔌 MCP Unity Connection
To start the MCP Unity server, run the following command in your terminal:

```powershell
C:\Users\Manel\.local\bin\uv.exe tool run --from "mcpforunityserver==9.6.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools
```

---

## 🗺️ Roadmap (Vision by Creative Director)

We are currently executing the **Horde Survival Overhaul**.
- [x] Establish Dark/Brutal Tone and Vision.
- [x] Implement Welcome Screen (Player Name, Solo/LAN, Bot count).
- [x] Overhaul combat to free-aim action mechanics.
- [x] Implement the 3-Wave spawner and 5x scale Bosses.
- [x] Expand the Arena size and add destructible physics to trees/rocks.
- [ ] Implement LAN Multiplayer Support (Post-1.0).

### Current Combat Subtasks
- [x] Spawn 10 ground weapons per allied combatant at match start.
- [x] Auto-pickup for player and allied bots.
- [x] `Q` to drop equipped weapon.
- [x] Assault rifle and shotgun ammo system (20 ammo, weapon disappears at 0).
- [x] Flamethrower conversion with infinite fuel and 20m range.
- [x] Fireball direct damage and area damage upgrade.
- [x] Allied bots prioritize equipped weapons when available.

---

> [!WARNING]
> Keep moving. In Biodeath Arena, standing still means being surrounded. Use your allied bots to split the enemy aggression while you focus on the colossal T-Rexes.