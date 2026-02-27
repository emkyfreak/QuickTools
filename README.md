# QuickTools for Koikatsu Studio

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/emkyfreak/QuickTools/releases)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.x-green.svg)](https://github.com/BepInEx/BepInEx)
[![Game](https://img.shields.io/badge/game-Koikatsu-pink.svg)]()
[![VR](https://img.shields.io/badge/VR-supported-purple.svg)]()

QuickTools is a BepInEx plugin for Koikatsu Studio (and VR) that adds a quick UI panel for common character, camera, and item controls. It's built to save you time from digging through the native menus.

## Screenshots

<table>
  <tr>
    <td align="center"><b>Main Menu</b></td>
    <td align="center"><b>Male Tools</b></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><img width="400" alt="QuickTools" src="https://github.com/user-attachments/assets/0cf0aade-bcb8-4d79-b5d4-0b6e1ae93e34" /></td>
    <td><img width="400"alt="QuickTools - Man" src="https://github.com/user-attachments/assets/78513a9d-fca7-4308-bd85-a7fb9bcd0a23" /></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td align="center"><b>Female Tools</b></td>
    <td></td>
    <td></td>
    <td align="center"><b>Item Tools</b></td>
  </tr>
  <tr>
    <td><img width="400" alt="QuickTools - Female 1" src="https://github.com/user-attachments/assets/81512eba-5440-4e12-a693-248e7d4525a3" /></td>
    <td><img width="400" alt="QuickTools - Female 2" src="https://github.com/user-attachments/assets/4dc9c033-c966-45ad-94ef-51ef713775c5" /></td>
    <td><img width="400" alt="QuickTools - Female 3" src="https://github.com/user-attachments/assets/5c69b383-4f51-4957-9fa7-d3d18abe27b6" /></td>
    <td><img width="400" alt="QuickTools - Items" src="https://github.com/user-attachments/assets/a9438386-395b-4a13-8503-da9c8f3b4c2b" /></td>
  </tr>
</table>


---

## Features

### Male Tools (Camera & POV)
- **POV Mode:** Snap the camera to any male character's first-person perspective.
- **Stop POV:** Automatically reset the camera back to its normal state.
- **Lock View:** Locks the camera to track the character's head movement.
- **Hide Head:** Toggles head and accessory visibility to prevent them from clipping into the camera while in POV.
- **Camera Tilt:** Adjust camera tilt angles, featuring improved tilting behavior while in POV mode.
- **Character Cycler:** Quickly swap between multiple male characters in your scene.

### Female Tools (Character Controls)
- **Character Cycler:** Easily jump between female characters.
- **Sight Target:** Force characters to look at the camera or revert to their default sight target.
- **Outfit Cycler:** Switch between standard outfits (0-6). 
  - *New:* You can now cycle through extended outfits (0-120). This can be enabled in the F1 Plugin Settings.
- **Coordinate Clothing Cycler:** Integrated a dedicated cycler for coordinate clothing directly into the tools. (You need to cycle to it's folder first)
- **Clothing States:** Instantly toggle between Clothed, Half-clothed, and Nude.
- **Facial Expressions:** Cycle through eyebrow (0-7), eye (0-39), and mouth (0-34) patterns.
- **Animations:** Adjust animation Group, Category, and ID values, and play them straight from the panel.

### Item Tools
- **Quick Spawn:** Spawn items by name (configurable via F1).
- **Camera Kit:** Spawn a 16:9 FOV120 Camera and Monitor with a single click.
- **Dynamic Bones:** Disable the creation of NEW dynamic bones on all dynamic bones in open scene (This should prevent the breaking of dynmic bones on female char replacing).

---

## Installation

### Requirements
- Koikatsu
- BepInEx 5.4.x or newer
- Studio / CharaStudio

### Setup
1. Download the latest release from the [Releases](https://github.com/emkyfreak/QuickTools/releases) tab.
2. Drop `QuickToolsPlugin.dll` into your `BepInEx/plugins/` folder.
3. Launch Koikatsu Studio.
4. Click the **[QuickTools]** button in the bottom right corner to open the menu.

---

## Usage Notes

- The UI panel is fully draggable.
- In the Female Tools menu, you can use your mouse wheel to scroll through all available options.
- **VR Support:** The plugin works natively in VR. The UI scales properly, backgrounds/separators render correctly, and POV positioning automatically aligns to eye-level.

## Known Issues

- **Outfit Changes:** Swapping outfits might cause a 1-2 second freeze. This is completely normal and is just the game loading the assets.
- **Spawn by Name:** The search is case-insensitive but will only spawn the first matched item it finds.

---

## Compiling from Source

If you want to build the plugin yourself:
1. Clone this repository.
2. Grab the following assemblies from your game folder and add them as references: 
   `KKAPI.dll`, `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `UnityEngine.dll`, `UnityEngine.UI.dll`, `BepInEx.dll`, and `0Harmony.dll`.
3. Build using .NET Framework 3.5.

## License
MIT License - see the [LICENSE](LICENSE) file.
