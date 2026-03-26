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
- **🎮 Free-Action Combat:** Strike freely in front of you, hitting multiple targets at once. No friendly fire—your bots are safe from your wrath.
- **🌊 3-Wave Attrition:** Survive escalating numbers of enemies per wave (20, 40, to 60 base monsters).
- **🦖 Environment-Shattering Bosses:** Three colossal bosses spawn per wave (at 30s, 60s, and 90s). They are so massive they obliterate arena obstacles in their path.
- **🌐 Solo First, LAN Future:** Currently a finely-tuned Singleplayer experience, built with an architecture to support LAN Co-op in the future.

---

## 🕹️ Controls

| Action | Control |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` |
| **Orbit Camera** | `Right Click` (Hold) |
| **Jump** | `Space` |
| **Action Combat (Strike)** | `1` / `3` |
| **Dash / Evade** | `Left Shift` |
| **Reset Match** | `R` |

---

## 🛠️ Tech Stack & Setup

- **Engine:** Unity 2022.3 (Built-in Render Pipeline / URP compatible)
- **Combat Architecture:** Fully physics-based hit detection tailored for massive horde battles and performance.

### Quick Start
1. Open the project in Unity.
2. Load `Assets/Scenes/WelcomeScreen.unity` (Coming Soon) or `GetStarted_Scene.unity`.
3. Press **Play** and survive the horde.

---

## 🗺️ Roadmap (Vision by Creative Director)

We are currently executing the **Horde Survival Overhaul**.
- [x] Establish Dark/Brutal Tone and Vision.
- [ ] Implement Welcome Screen (Player Name, Solo/LAN, Bot count).
- [ ] Overhaul combat to free-aim action mechanics.
- [ ] Implement the 3-Wave spawner and 5x scale Bosses.
- [ ] Expand the Arena size and add destructible physics to trees/rocks.
- [ ] Implement LAN Multiplayer Support (Post-1.0).

---

> [!WARNING]
> Keep moving. In Biodeath Arena, standing still means being surrounded. Use your allied bots to split the enemy aggression while you focus on the colossal T-Rexes.

Error al encender MCP en Unity

C:\Users\Manel\.local\bin\uv.exe tool run --from "mcpforunityserver==9.6.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools