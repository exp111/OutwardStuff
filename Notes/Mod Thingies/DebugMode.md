# TODO:
- [ ] Optionally disable hotkeys and move them into a custom menu
- [ ] Optionally skip save selection menu (CharacterSelectionPanel.OnCharacterClicked)

## Debug Hotkeys
https://outward.fandom.com/wiki/Debug_Mode
### F Key Menus
f1 (item spawn): DeveloperToolManager.Update
f2 (charactercheats): "
f3 (skill): "
f4 (questevent): "
### Free Cam
shift+, (freecam): VideoCamera.Update
shift+
### Other
Shift+O (open gui?): CameraQuality.Update
Alt (Toggle cursor lock): Global.Update
Numpad 1/Mouse 5 (slow mo): Global.Update
Shift+H (hide ui): Global.Update
Drag Corpse: LocalCharacterControl.UpdateInteraction
Numpad Divide (Toggle Debug Info): MenuManager.Update
Ctrl+Shift+K (gets players with mouse access): MenuManager.Update => does nothing 
Ctrl+Alt+S (Force save environment): NetworkLevelLoader.Update
Ctrl+Alt+L (Reload current scene): NetworkLevelLoader.Update
Ctrl+Alt+X (Toggle Photon Room): NetworkLevelLoader.Update
Alt+Pg Up (Increase Graphics Preset): OptionManager.Update
Alt+Pg Down (Decrease Graphics Preset): OptionManager.Update
F12 (Screenshot): ScreenshotTaker
7 (Set damage dealt to needed for vampire): Weapon.UpdateProcessing 