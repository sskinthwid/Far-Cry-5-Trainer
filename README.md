# Hope Control for Far Cry 5

C# / WPF trainer with **53 native controls**, an integrated **player currency/inventory editor**, and **116 game settings**. External mod cards, unfinished entries, installer launchers and website buttons have been removed.

## Run

Run `HopeControl.exe`, load a single-player campaign and select **Connect to game**. All runtime switches start off. God mode and infinite armour, stamina and oxygen are toggles; movement speed, fire rate and other numeric controls have custom inputs.

**Restore all** cancels pending inventory edits and restores this application's patches. Closing also restores patches. Incompatible signatures and foreign patches are rejected. Use the app's normal close while attached.

## Currency and inventory

Select **Currency and inventory**, then **Start / refresh inventory**. The list fills as the game queries the player's resources. Open the weapon wheel, inventory or a store in the game, then refresh to expose more counters.

Enter a whole quantity from 0 to 1,000,000,000 and select **Set**. The native callback verifies current player, descriptor and previous quantity, applies the edit on the next matching query and acknowledges it. If the quantity changed before application, refresh and retry. Only one command is pending at once; **Cancel pending edit** clears it. Status appears above the list.

Editable families: US cash, Lost on Mars energy, player ammunition, medkits and crafting ingredients. NPC ammunition and story/intel records are excluded. Separate counters can share a resource name; each row displays its own quantity. Edits can persist in saves. No fake Silver Bar balance or purchase-validation bypass is offered; Ubisoft documents Silver Bars as account/platform-linked.

Additional toggles preserve cash, Mars energy, medkits and crafting ingredients on consumption. Initial balances and eligibility checks still apply.

## Game settings

The integrated editor covers HUD, mouse/keyboard, controller, audio, devices, FOV, FPS cap, resolutions, resolution scale, motion blur, graphics presets and detail quality. Search filters settings too.

Close Far Cry 5 before **Save settings for next launch** so it cannot overwrite the profile on exit. Drafts survive navigation and game exit. Saving merges only edited fields into the latest profile and creates a timestamped backup. Editing graphics detail selects the custom preset. **Restore settings backup** restores the latest backup. Unknown XML is preserved; device-specific options require corresponding hardware. These are next-launch profile settings, not immediate memory sliders.

## Important control limits

- Rapid fire and no reload require weapon owner and wielder to match the player. Rapid fire follows weapon changes but remains subject to animation locks, explicit rate overrides and semi-auto trigger behavior. No reload preserves an existing loaded magazine; it does not fill an empty one. Disable before fishing.
- Current vehicle protection checks the live occupied-vehicle reference each update. It restores health only for the matching car; scripted destruction or damage bypassing health is not covered.
- NPC/animal damage multipliers can affect allies and companions. Recoil/sway controls do not remove every camera or explosion effect.

## Presets and hotkeys

Save/load runtime values and switches through presets. Inventory quantities and settings-file edits are not applied by runtime presets. Ctrl+Alt+1 god mode; 2 speed; 3 reserve ammo; 4 no reload; 5 recoil; 6 spread; 7 stamina; 8 oxygen; 9 aircraft missiles/countermeasures. F12 restores all when available.

## Build and verification

```powershell
.\build.ps1 -OutputDirectory release
.\release\HopeControl.exe --self-test
.\release\HopeControl.exe --diagnose --roundtrip
.\release\HopeControl.exe --inventory-check
```

Build uses Windows' .NET Framework C# compiler and WPF assemblies. No Cheat Engine installation or downloaded SDK is required. No mouse or keyboard automation is used.

Self-tests execute actual emitted guards against controlled objects and test scanner/numeric/settings behavior. Live roundtrips verify activation/readback/restoration, not every gameplay effect. Inventory checks observe player counters and test a queued cash write at its existing balance without granting money. Settings save/restore tests use isolated profile copies; your actual settings were only read. See `logs/`, [RESEARCH.md](RESEARCH.md) and [THIRD_PARTY.md](THIRD_PARTY.md).
