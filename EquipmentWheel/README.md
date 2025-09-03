## Overview

This mod adds an user interface for equipping/using items in an easier and faster way. The menu will be visible by holding the 'G' key (can be changed). On gamepads it's the 'Use' button (X or A on Xbox Controllers) by default. (Can be changed to the 'Sit' Button or Left/Right button on the D-Pad.)

**New in version 1.5.2: Added config entry for using Sit-Button**

The new config allows you to use the 'Sit' button again as a hotkey. Just enable it in the config (UseSitButton = true). 

**New in version 1.5.1: Changed Gamepad bindings**

Hotkeys for Gamepad has been changed. The default is now the 'Use' button (X on Xbox Controllers). 

**New in version 1.5.0: Changed Gamepad bindings and Regex filtering**

Hotkeys for Gamepad has been changed. The default is now the LEFT (or RIGHT) button on the D-Pad. 
Filtering items now supports Regular Expressions.

**New in version 1.3.0: Better binding for hotkeys**

Hotkeys can now be key combos like 'SHIFT + 1'. Also the hotkey for gamepads can be changed in the config.

**New in version 1.2.0: Item Type Matching Mode**

When enabled, it scans the whole inventory for items of the specified types in the config and use them in the equip wheel.

**New in version 1.1.0: Gamepad Support**

Rebinds the gampad X-button to open the equip wheel. The left joystick controls the selected item.


To trigger equipping, release the Hotkey after selecting an item. (When TriggerOnRelease = true)

To trigger equipping, press the left mouse button after selecting an item. (When TriggerOnClick = true)

If you equip an one-handed weapon, the shield (if available) will be equipped automatically. 

The mod allows also to choose a different inventory row to use for the equip wheel.
The top-left hotkey bar can be optionally disabled.

Info: This mod should only be installed on the client side.


## Requirements (Manual Installation)

BepInEx Modloader must be installed. Can be found here:

[https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

To install the mod just move the downloaded EquipWheel.dll to the `<valheim-folder>\BepInEx\plugins\` folder.


## Config

A config file will be created after running the game once while the mod is installed:

`<valheim-folder>\BepInEx\config\virtuacode.valheim.equipwheel.cfg`

## Source Code

[https://github.com/virtuaCode/valheim-mods](https://github.com/virtuaCode/valheim-mods)

## Change Log

- Version 1.5.3
    - Fix issue with selecting items with controllers
- Version 1.5.2
    - Add config entry for using 'Sit'-Button as a hotkey
- Version 1.5.1
    - Changed the default hotkey on gamepads to the 'Use' button (X on Xbox controllers)
- Version 1.5.0
    - Changed gamepad hotkey to the LEFT button on the D-Pad
    - Regex support for filtering
- Version 1.4.0
    - Fix issue with TMPro
- Version 1.3.9
    - Fix issue with renamed variable (m_equiped)
- Version 1.3.8
    - Fix missing Patcher method
- Version 1.3.7
    - Fix issue with Mistlands Patch
- Version 1.3.6
    - Fix Bug with selecting Epic Loot items on EquipWheel Two, Three and Four
- Version 1.3.5
    - Fix issue with Hearth & Home Patch
- Version 1.3.4
    - Add compatibility with EpicLoot
    - Change default UI scale (0.75 to 0.5)
- Version 1.3.3
    - Add label for selected item
- Version 1.3.2
    - Fix problem with overlapping key bindings (e.g. F and F+Shift)
    - Fix ordering of shields when auto-equipping
    - Fix config problem for shield auto-equipping
    - Remove some config options for additional equip wheels (Options now inherited from main equip wheel)
- Version 1.3.1
    - Add protected bindings
- Version 1.3.0
    - Add equip weapons while running
    - Add gamepad hotkey and automatic unbinding
    - Add mod enabled/disabled option
    - Change hotkey option from KeyCode to KeyboardShortcut (enables key combos)
    - Enable shield autoequip for hotkeybar
- Version 1.2.3
    - Add ignoring item names (blacklist)
    - Add toggle option
    - Fix item names filtering
- Version 1.2.2
    - Allow overriding hotbar keys
    - Add item names matching
- Version 1.2.1
    - Add support for building variants
- Version 1.2.0
    - Add item type matching
    - Change Hotkey from string to enum
    - Fix distance from center scaling
- Version 1.1.0
    - Add gamepad support
    - Fix auto equip shield when one-hand weapon gets unequipped
- Version 1.0.4
    - Fix inverted logic for hiding hotkeybar
- Version 1.0.3
    - Add option to hide hotkey bar
    - Add option to choose different inventory row
    - Fix unequipping shild when it shouldn't
- Version 1.0.2
    - Change shader compilation and bundling (fixes pink textures)
    - Fix auto equip shield when switched from bow
- Version 1.0.1
    - Move asset unload right after GUI instantiation
- Version 1.0.0
    - Initial Release

