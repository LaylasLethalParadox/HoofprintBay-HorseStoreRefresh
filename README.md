# Horse Store Refresh for Horses of Hoofprint Bay

Horse Store Refresh for [Horses of Hoofprint Bay](https://store.steampowered.com/app/3727090/) adds a "New horses" button to the trader's sales stable (Town, Breeder, Buy Horse), next to the sort buttons. Click it and the trader brings in a new set of horses for sale.

- The button works once a day by default. You can allow up to 10 times a day. Once it is used up it greys out until the next day.
- Horses you have reserved stay in the stable and come first in the list.
- The uses are saved with your game. If you quit without saving, the old horses and the unused button come back together.
- A "Reserved" sort button puts the reserved horses first, the one whose hold ends soonest at the top. Click it again to turn the order round.

The new horses come from the game's own code for the trader, so they are the same kind of horses the trader would bring anyway. That code also swaps some of the studs for breeding. A stallion that sired one of your foals stays in the foal's pedigree.

## Install

You need BepInEx 6 bleeding edge build 793 for IL2CPP (Windows x64). BepInEx 5 and the older 6.0.0-pre builds can't load this game. [Mod Manager](https://github.com/LaylasLethalParadox/HoofprintBay-ModManager#install) explains how to install it; Horse Store Refresh itself doesn't need Mod Manager.

1. Extract the Horse Store Refresh zip into the game folder, so that you have `BepInEx\plugins\HorseStoreRefresh\HorseStoreRefresh.dll`.
2. Start the game, go to Town, then the Breeder, then Buy Horse.

## Settings

With Mod Manager installed, the settings are in the Mods menu under Horse Store Refresh. Without it, edit `BepInEx\config\hoofprintbay.horsestorerefresh.cfg` while the game is closed:

- `Enabled`: shows the New horses button (default `true`).
- `RefreshesPerDay`: how many times a day the button works, 1 to 10 (default `1`).
- `ReservedSort`: shows the Reserved sort button (default `true`).

To uninstall, delete `BepInEx\plugins\HorseStoreRefresh`. The file `HorseStoreRefresh.txt` in each save folder only holds the day of the last use and can be left there.

## Building

You need the .NET 6 SDK and the game with BepInEx installed and started at least once, because the project compiles against files BepInEx generates from the game. This repository contains no game files. Mod Manager must be installed too, since the project references its DLL for the optional settings page.

```
dotnet build -c Release -p:GameDir="C:\Path\To\HorsesOfHoofprintBay\\"
```

The build copies the DLL into the game's `BepInEx\plugins\HorseStoreRefresh` folder, so close the game first. Add `-p:NoInstall=true` to only compile. `package.ps1` makes the release zip without installing it.

## License

GPL-3.0, see [LICENSE](LICENSE).

This is an unofficial fan project and isn't affiliated with or endorsed by the developers of Horses of Hoofprint Bay.
