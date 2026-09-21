# Double-Tap Hook For Unity Editor on macOS and Windows

## What it does

Turns a **double tap on a modifier key** into an editor shortcut: tap <kbd>Shift</kbd>,
<kbd>Ctrl</kbd>, <kbd>Alt</kbd> or <kbd>Cmd</kbd> twice in a row and a registered action runs — the
same gesture JetBrains IDEs use for Search Everywhere on double <kbd>Shift</kbd>.

Why it exists: it frees up shortcuts for tools you open constantly (finder, wizard, quick search)
without burning yet another <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + letter combo, and it keeps working
in places Unity's own shortcut system gives up — while a text field has focus, while a modal is up,
or while scripts are compiling.

A gesture only counts when both taps are quick:

| Rule | Default | Property |
|---|---|---|
| Each tap must be released within | 0.3s | `DoubleTapShortcut.MaxHoldSeconds` |
| Gap between the two taps at most | 0.3s | `DoubleTapShortcut.MaxGapSeconds` |

Anything else in between cancels the pending gesture — another key, a different modifier, or a mouse
click on macOS. Holding the modifier down (a normal <kbd>Shift</kbd> + click, a real shortcut) never
triggers it.

## How it works

Native editor plugin behind `WorldEditor.DoubleTapShortcut`. It reports modifier press/release with
precise timestamps, which Unity's IMGUI pipeline cannot do: IMGUI emits no key-up for modifiers and
drops events while a text field, a modal or a compile owns the editor.

The hook is installed **in-process** on the thread that pumps the editor event loop, so it only sees
input while the Unity editor is focused. No Accessibility permission on macOS, no global hook on
Windows. Editor only, macOS and Windows; on other platforms it silently does nothing.


## Installation Guide
- Download the `UnityEditorDoubleTap.unitypackage` from GitHub [Releases]( https://github.com/aprius/UnityEditor-DoubleTap/releases) and double click to install it to your Unity project.

## Build

macOS, produces a universal arm64 + x86_64 bundle:

```
./macos/build.sh
```

Windows, from a *x64 Native Tools Command Prompt for VS*:

```
windows\build.bat
```

The Windows DLL is not built here. After the first import, set its importer to **Editor** only,
`OS: Windows`, `CPU: x86_64` — the macOS bundle already ships with that meta.

## Platform differences

macOS also cancels a pending double-tap on mouse clicks; the Windows `WH_KEYBOARD` hook is keyboard
only, so a click between the two taps does not cancel there.

## Usage

Register from a static constructor so the binding exists as soon as the editor loads:

```csharp
[InitializeOnLoad]
internal static class SearchEverywhere
{
    static SearchEverywhere() => DoubleTapShortcut.Register(ModifierKey.Shift, Open);

    private static void Open() => EditorWindow.GetWindow<YourWindowWannaShow>();
}
```

`Register` adds to the action list for that key, so several tools can share one modifier; use
`Unregister` to drop a binding. Timing can be tuned globally:

```csharp
DoubleTapShortcut.MaxHoldSeconds = 0.25;
DoubleTapShortcut.MaxGapSeconds = 0.4;
```

Handlers run from `EditorApplication.update`, not from the native callback, so touching editor
windows is safe.
