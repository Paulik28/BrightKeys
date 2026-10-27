# BrightKeys 🔆🌙

> **Dual-Monitor Brightness & Per-Monitor Night Light Control via Laptop FN Keys or Custom Global Shortcuts.**  
> *Control individual și simultan al luminozității și luminii de noapte pentru ambele monitoare din tastele FN.*

---

## 🌟 Key Features / Caracteristici Principale

- **Dual-Monitor Hardware Control via DDC/CI**: Adjust real physical monitor brightness without third-party dependencies (no Monitorian needed, no admin rights required).
- **Per-Monitor Night Light (Lumină de Noapte Individuală)**:
  - Toggle warmth on/off smoothly.
  - Adjust warmth independently for Monitor 1 or Monitor 2, or both at once depending on the selected target (`Fn+F12`).
- **60 FPS Smooth Glide (Continuous Hold)**: Hold down `Fn+F9` or `Fn+F10` for butter-smooth sliding with no stuttering and instant response (0ms UI lag).
- **Bilingual Interface (Română & English)**: Switch language anytime from the tray menu or the settings window.
- **Dedicated Settings Window (Meniu de Setări)**: Configure adjustment steps (1%–20%), Windows autostart, and custom global keyboard shortcuts.
- **Universal Keyboard Support**:
  - Works with laptop OEM multimedia keys (`LaunchMail`, `BrowserHome`, `LaunchMedia`) mapped to `Fn+F9`, `Fn+F10`, `Fn+F12`.
  - Works with any external keyboard via configurable Win32 global hotkeys (e.g. `Ctrl+Alt+F9/F10/F12`).

---

## ⌨️ Default Shortcuts / Taste Implicite

| Hotkey | Description (English) | Descriere (Română) |
|---|---|---|
| `Fn + F9` | Decrease brightness on targeted display | Scade luminozitatea monitorului selectat |
| `Fn + F10` | Increase brightness on targeted display | Crește luminozitatea monitorului selectat |
| `Fn + F12` | Cycle target: Both → Monitor 1 → Monitor 2 | Comută ținta: Ambele → Monitor 1 → Monitor 2 |
| `Shift + Fn + F12` | Toggle Night Light On/Off | Activează / Dezactivează Lumina de Noapte |
| `Shift + Fn + F9` | Decrease Night Light warmth | Scade căldura luminii de noapte |
| `Shift + Fn + F10` | Increase Night Light warmth | Crește căldura luminii de noapte |

> 💡 **Continuous Hold**: Hold `Fn+F9` or `Fn+F10` (with or without Shift) to glide continuously. Releasing Shift instantly switches back to brightness adjustment!

---

## 🛠️ Build & Installation

### Requirements
- Windows 10 / Windows 11
- .NET Framework 4.0+ (pre-installed on Windows 10/11)

### Build from Source
Run the included PowerShell build script:
```powershell
.\build.ps1
```
The compiled binaries will be output to the `dist/` folder:
- `BrightKeys.exe` (main tray application)
- `BrightKeysDown.exe`, `BrightKeysUp.exe`, `BrightKeysNext.exe`, `BrightKeysNightLight.exe` (lightweight helper launchers)

---

## 📄 License
MIT License.
