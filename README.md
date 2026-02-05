# 🎮 QuickTools for Koikatsu Studio

A powerful BepInEx plugin for Koikatsu Studio that provides quick access to essential character and camera controls through an intuitive UI panel.

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.x-green.svg)
![Game](https://img.shields.io/badge/game-Koikatsu-pink.svg)
![VR](https://img.shields.io/badge/VR-supported-purple.svg)

---

## ✨ Features

### 🎭 Male Tools

- **POV Mode**: Instantly switch to first-person perspective from male character's eyes
- **Lock View**: Keep camera locked to character's head movement
- **Hide Head**: Toggle head and accessory visibility for immersive POV
- **Camera Tilt**: Quick tilt adjustments for different viewing angles
- **Character Cycling**: Quickly switch between multiple male characters in scene

### 👧 Female Tools

- **Character Cycling**: Navigate through all female characters
- **Sight Target**: Control where character looks (Default / Look at Camera)
- **Outfit Cycling**: Switch through all 7 outfit slots (0-6)
- **Clothing States**: Toggle between Clothed / Partial / Nude
- **Facial Expressions**: Cycle through eyebrow, eye, and mouth patterns
- **Animation Controls**: Adjust animation Group, Category, and ID values
- **Play Animations**: Apply selected animation to character

### 🎨 Item Tools

- **Quick Spawn**: Spawn items by name or custom coordinates (Edit in F1)
- **Camera Kit**: One-click spawn of 16:9 FOV120 Camera and Monitor
- **Dynamic Bones**: Disable new dynamic bones on colliders

---

## 📦 Installation

### Requirements

- **Koikatsu** (base game or Sunshine)
- **BepInEx 5.4.x** or newer
- **Studio** (CharaStudio)

### Steps

1. Download the latest release from [Releases](https://github.com/emkyfreak/QuickTools/releases)
2. Extract `QuickToolsPlugin.dll` to `BepInEx/plugins/` folder
3. Launch Koikatsu Studio
4. Access QuickTools via the **[QuickTools]** button

---

## 🎯 Usage

### Opening the Panel

1. Open Koikatsu Studio
2. Look for the **[QuickTools]** button in the bottom left area
3. Click to open the main menu

### Basic Controls

- **Main Menu**: Choose between Male Tools, Female Tools, or Item Tools
- **Back Button**: Return to main menu from any submenu
- **Draggable Panel**: Click and drag anywhere on the panel to reposition
- **Scrolling**: Use mouse wheel in Female Tools menu to access all options

### Desktop Mode

- **POV**: Select male character → Enable "Hide Head" (optional) → Click "Set POV"
- **Lock View**: Enable after setting POV to track head movement
- **Tilt**: Use Tilt Up/Down/Reset buttons to adjust camera angle

### VR Mode

- **Full Support**: All features work in VR
- **Optimized UI**: Background and separators are fully visible in VR
- **POV Positioning**: Accurate eye-level positioning with your code integration

---

### Main Menu

```
┌─────────────────────────┐
│     QuickTools          │ [X]
├─────────────────────────┤
│                         │
│   [  Male Tools  ]      │
│                         │
│   [ Female Tools ]      │
│                         │
│   [  Item Tools  ]      │
│                         │
└─────────────────────────┘
```

### Male Tools

- Character cycler with display (e.g., "Ryu (1/2)")
- Set POV button
- Lock View checkbox
- Hide Head checkbox
- Camera tilt controls

### Female Tools (Scrollable)

- Character cycler with display
- Sight target cycler (Default / Look at Camera)
- Outfit cycler (0-6)
- Clothing state cycler
- Facial expression cyclers (Eyebrows, Eyes, Mouth)
- Animation value controls
- Play Animation button

---

## 🛠️ Technical Details

### Facial Expression Ranges

- **Eyebrows**: 0-7 (8 patterns)
- **Eyes**: 0-39 (40 patterns)
- **Mouth**: 0-34 (35 patterns)

---

## 🤝 Building your own Release

### Development Setup

1. Clone the repository
2. Reference required assemblies:
   - `KKAPI.dll`
   - `Assembly-CSharp.dll`
   - `Assembly-CSharp-firstpass.dll`
   - `UnityEngine.dll`
   - `UnityEngine.UI.dll`
   - `BepInEx.dll`
   - `0Harmony.dll`
3. Build with .NET Framework 3.5 compatibility

---

## 📝 Known Limitations

- **Outfit Changes**: May cause 1-2 second freeze (necessary for proper loading)
- **Dynamic Bones**: Field detection is generic and may not work on all custom colliders
- **Spawn by Name**: Only spawns first match (case-insensitive search)

---

## 🐛 Bug Reports

Found a bug? Please open an issue with:

- **Description**: What happened vs what you expected
- **Steps to Reproduce**: How to trigger the bug
- **Environment**: Desktop/VR, Koikatsu version, BepInEx version
- **Logs**

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---
