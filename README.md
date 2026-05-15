# Smart Face Target

This is a [Dalamud](https://github.com/goatcorp/Dalamud) plugin for Final Fantasy XIV that automatically orients your character towards your current target when you are stationary and quickly selects the nearest enemy while in combat.

## Features

- **Automatic Target Acquisition**: Automatically targets the closest attackable enemy in front of you within a specified cone angle, primarily useful in combat.
- **Smart Facing**: Automatically rotates your character to face your current target after you remain stationary for a brief period.
- **Dynamic Movement Mode**: Automatically switches between Standard and Legacy movement modes based on combat state or whether your weapon is drawn.
- **Highly Configurable**: Control rotation speeds, targeting angles, delays, and context-specific rules.

## Commands

- `/sft` - Opens the SmartFaceTarget settings window.

## Settings

- **Active**: Toggles the plugin functionality on or off.
- **Combat Only**: Limits the plugin's functionality so it only activates when your character is in combat.
- **Rotation Angle**: The maximum angle to the target within which automatic facing will trigger.
- **Rotation Speed**: Controls how smoothly and fast the character rotates to face the target.
- **Stationary Time**: The delay (in milliseconds) the character must be stationary before automatic rotation kicks in.
- **Targeting Angle**: The field of view cone angle for automatically selecting the nearest enemy.
- **Targeting Delay**: The frequency/delay (in milliseconds) between automatic targeting checks.
- **Movement Mode Settings**: Configures the preferred movement mode (Standard, Legacy, or Default) based on:
  - In Combat
  - Out of Combat
  - Weapon Drawn
  - Weapon Sheathed

## Installation

You can install this plugin by adding a custom repository in Dalamud:

1. Open the Dalamud Settings menu by typing `/xlsettings` in game.
2. Navigate to the **Experimental** tab.
3. Under **Custom Plugin Repositories**, enter this [URL](https://raw.githubusercontent.com/Neo-The-Wan/SmartFaceTarget/refs/heads/master/repo.json).
4. Click the **+** button to add it, then **Save and Close**.
5. Open the Plugin Installer (`/xlplugins`), search for **SmartFaceTarget**, and click Install.

## Author

Created by [NeoTheWan](https://github.com/Neo-The-Wan).