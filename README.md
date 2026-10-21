# Castle Market Towns

A Mount & Blade II: Bannerlord mod that turns castles into small market towns while they stay castles.

**Download:** [Castle Market Towns on Nexus Mods][nexus]

## Why this mod exists

Throughout history, castles were never only military outposts. They were the seats of lords, built to protect the surrounding fiefs, villages and lands, and markets, craftsmen and trade grew up around their walls.

In Mount & Blade II: Bannerlord, castles have been largely neglected. Almost every economic and social feature, from markets and taverns to workshops, merchants and caravans, belongs to the towns (cities), while a castle is little more than a garrison with walls.

Castle Market Towns overhauls castles to give them the life they deserve. They gain trade, people and income, scaled to sit between a village and a town, and they remain proper castles.

## Town vs. castle vs. Castle Market Towns castle

| Feature | Town (vanilla) | Castle (vanilla) | Castle with this mod |
|---|---|---|---|
| Settlement menu | Town menu | Castle menu | Castle menu, with extra options |
| Market and trade | Yes | No | Yes, smaller. Starts with 25% of the nearest town's goods |
| Own production | From its villages | None | 20% of its villages' output, on top of what they send to their town |
| Tavern | Yes | No | Yes, using the town tavern scene |
| Merchants and artisans | Yes | None | 2 merchants and 2 artisans |
| Workshops | 4 slots | None | 1 slot |
| Caravans | Formed by town merchants | None | Formed by castle merchants. Caravans also trade at castles |
| Volunteers from notables | Yes | None | Yes, at 65% of a town's speed, 50% chance of noble troops |
| Noble recruits from villages | No | Yes | Yes, unchanged |
| Prosperity | About 1,700 to 5,600 | About 420 to 1,100 | Raised to at least 1,000, grows with trade |
| Prosperity tax | Full | Low | 80% of a town at the same prosperity |
| Marketplace project | Yes | No | Yes, at 60% of the town cost |
| Rebellion | Yes | Never | Yes, can be switched off |
| Town healing perks | Yes | No | Yes |
| Siege food from the market | Yes | No | Yes |
| Merchant, artisan and village quests | Yes | None | A selection that fits a castle |
| Arena and tournaments | Yes | No | No |
| Walking the streets in third person | Yes | No | No |
| Brothel (Captivity Events) | Yes | No | No |

All numbers are the defaults and can be changed in the mod's options.

## How features were chosen

Every town feature was weighed against a few rules. A feature was included only if it passed all of them.

1. **A castle stays a castle.** The game never treats a castle as a town. Castles keep the castle menu, walls, garrison, sieges and noble recruits from their villages. Walls, garrisons and siege rules are unchanged, so castles do not become harder for AI lords to take and campaigns do not drag on.
2. **Between a village and a town.** Every value sits between the two. A castle has 4 notables, between a village's 3 and a town's 5, and its tax, production, recruits and prosperity are scaled the same way. A new castle starts modest and grows through trade. Only a castle with a long run of good trade, next to a town in trouble, can come close to a town.
3. **Towns remain the economic centres.** A castle's villages still deliver all their goods to their town. The castle gains its own share of production on top, and sells any surplus to its town, as villagers do. Towns never lose stock, prosperity or stability because of this mod.
4. **Only what a castle can actually host.** Castles have no arena and no town streets in the game. Arenas, tournaments, walking around in third person, and the quests that depend on them were left out on purpose, rather than added in a broken state. A castle tavern uses the town tavern scene, so it is included.
5. **A lord's seat, not a pleasure district.** Brothels from other mods are kept out of castles.
6. **Optional and reversible.** Every feature has its own switch. Turning a feature off returns the castles to vanilla and removes only what the mod added.

## Mod options ([MCM][mcm])

All values and features can be changed in the game through [Mod Configuration Menu (MCM)][mcm], under **Options > Mod Options > Castle Market Towns**, without restarting. You can switch features on or off, choose which castles are covered (your clan, your kingdom, or everyone), and adjust:

- the number of merchants, artisans and workshops;
- the prosperity floor, starting market stock, production and consumption;
- the tax and trade prosperity;
- recruit speed and noble recruit chance;
- caravan preferences;
- the Marketplace cost;
- each castle quest.

## Requirements and versions

| Dependency | Tested version | Expected to work with |
|---|---|---|
| Mount & Blade II: Bannerlord | 1.4.8 | 1.4.x |
| War Sails (NavalDLC), optional | 1.2.8 | 1.2.x |
| [Bannerlord Software Extender (BLSE)][blse] | 1.7.2 | 1.x |
| [Harmony][harmony] | 2.4.2 | 2.x |
| [ButterLib][butterlib] | 2.12.0 | 2.x |
| [UIExtenderEx][uiextenderex] | 2.13.3 | 2.x |
| [Mod Configuration Menu v5 (MCM)][mcm] | 5.12.3 | 5.x |

This mod needs [Harmony][harmony] and [MCM][mcm] directly. The others are needed by those two:

- [MCM][mcm] depends on [Harmony][harmony], [ButterLib][butterlib] and [UIExtenderEx][uiextenderex].
- [Harmony][harmony] depends on [BLSE][blse]. Start the game through the BLSE launcher (`Bannerlord.BLSE.Launcher.exe` or `Bannerlord.BLSE.LauncherEx.exe`), as described on its page.

The mod only uses the stable public parts of [Harmony][harmony] and [MCM][mcm], so newer releases with the same major version (Harmony 2.x, MCM 5.x) should work without changes. A new major version, such as MCM 6, may need an update of this mod.

Bannerlord updates can change the game code this mod adjusts. If that happens, the affected feature stays inactive and a message naming it is written to the game log, instead of the game crashing.

With War Sails, castles only link to towns they can actually reach, including the castle on the northern island.

## Compatibility

- **Compatible** with Hot Butter and Captivity Events.
- **Not compatible** with other mods that turn castles into towns or overhaul castle menus, markets, notables or buildings. Both mods would change the same castle objects. To switch to another castle overhaul, remove this mod first (see below).

## Installation

1. Install [BLSE][blse], [Harmony][harmony], [ButterLib][butterlib], [UIExtenderEx][uiextenderex] and [MCM][mcm].
2. Download Castle Market Towns from [Nexus Mods][nexus]. Install it with Vortex (*Mod Manager Download*), or extract the `CastleMarketTowns` folder into `Mount & Blade II Bannerlord\Modules`.
3. Start the game through the [BLSE][blse] launcher, enable Castle Market Towns and place it after [Harmony][harmony] and [MCM][mcm].

The mod can be added to an existing campaign.

## Removing the mod from a campaign

The mod adds no save types of its own, so a save still loads without it. To return castles to vanilla before removing it:

1. Load the campaign. In [MCM][mcm], turn off **Enable castle markets**.
2. Let three or more in-game days pass. Each castle cleans up on its daily update:
   - the tavern, mod workshops and castle Marketplace are removed;
   - the goods and prosperity the mod added are taken back;
   - merchants and artisans created by the mod move to the nearest town;
   - daily defaults return to vanilla.
3. Save to a new slot and exit.
4. Disable or uninstall the mod.

## Building from source

The repository contains only source code. The TaleWorlds, [Harmony][harmony] and [MCM][mcm] DLLs are not included; the build references them from your game installation.

Requirements:

- Windows with .NET Framework 4.7.2 or later.
- Visual Studio 2019 or later, or the Visual Studio Build Tools. The .NET Framework's own MSBuild also works.
- Bannerlord with [Harmony][harmony] and [MCM][mcm] installed.

The code is limited to C# 5 so it builds with the .NET Framework compiler too.

### Point the build at your game

Use one of the following:

- Copy `CastleMarketTowns.user.props.example` to `CastleMarketTowns.user.props` and set `BannerlordDir`. Git ignores this file.
- Set the `BANNERLORD_DIR` environment variable.
- Pass `/p:BannerlordDir="..."` to MSBuild.

If none is set, the default Steam folder `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord` is used.

### Build

```bat
build.cmd
```

The DLL is written to `bin\Win64_Shipping_Client\CastleMarketTowns.dll`, so the repository folder has the same layout as the installed module.

To also copy `SubModule.xml` and the DLL into the game's `Modules\CastleMarketTowns` folder after each build, set `DeployToGame` to `true` in your `CastleMarketTowns.user.props`, or run:

```bat
build.cmd /p:DeployToGame=true
```

Close the game first, otherwise the DLL is locked.

## Repository layout

```
CastleMarketTowns.csproj               Project file
CastleMarketTowns.user.props.example   Template for local build settings
build.cmd                              Finds MSBuild and builds the project
SubModule.xml                          Module definition loaded by the game
src/                                   C# source
```

## License and credit

Copyright (C) 2026 AtanuDebbarma.

Castle Market Towns is free software under the [GNU General Public License v3.0](LICENSE), with an additional attribution term described in [NOTICE](NOTICE):

- **Using or modifying this code:** if you publish a mod or other work that uses or is based on this code, you must credit **Castle Market Towns by AtanuDebbarma** with a link to [this repository][repo], in your source code and in your mod's documentation. Your work must also be released under the GPL v3.
- **Using it as inspiration:** if this project inspired your work without you copying its code, a credit and link are kindly requested.

Mount & Blade II: Bannerlord is a trademark of TaleWorlds Entertainment. This project is not affiliated with or endorsed by TaleWorlds.

[mcm]: https://www.nexusmods.com/mountandblade2bannerlord/mods/612
[butterlib]: https://www.nexusmods.com/mountandblade2bannerlord/mods/2018?tab=description
[harmony]: https://www.nexusmods.com/mountandblade2bannerlord/mods/2006?tab=description
[uiextenderex]: https://www.nexusmods.com/mountandblade2bannerlord/mods/2102?tab=description
[blse]: https://www.nexusmods.com/mountandblade2bannerlord/mods/1?tab=description
[repo]: https://github.com/AtanuDebbarma/CastleMarketTowns
