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
| Settlement menu | Town menu | Castle menu | Castle menu, with extra options. The dungeon, garrison, stash and lord's hall move under **Go to the castle**, as towns keep them under **Go to the keep** |
| Entering | Walk in unless at war or wanted. The keep may ask for a bribe | Ask the guards. Visitors from outside the kingdom with clan tier below 3 pay a bribe at the gate | Walk in unless the castle would turn you away (war, any crime, a bad relation with its lord). **Go to the castle** asks for the castle's bribe, as a town keep does |
| Market and trade | Yes | No | Yes, smaller. Starts with 25% of the nearest reachable same-culture town's goods, falling back to the nearest reachable town. AI lords sell loot there and travel there to buy food, as they do in towns |
| Own production | From its villages | None | 20% of its villages' output, on top of what they send to their town |
| Tavern | Yes | No | Yes, using the town tavern scene |
| Companions to hire | In the tavern | None | In the castle tavern. A small one-time seed uses the active companion system's creation rules; afterward free companions move between towns and nearby castles over time |
| Merchants and artisans | Yes | None | 2 merchants and 2 artisans |
| Workshops | 4 slots | None | 1 slot. Bought from castle notables and managed from the clan screen like a town workshop, with its own warehouse |
| Workshops you can own | Clan tier + 1, across all towns | None, castles have no workshops | Clan tier + 1, shared with towns: castle workshops count toward the same limit. An optional extra limit raises it for both |
| Caravans | Formed by town merchants | None | Formed by castle merchants. Caravans also trade at castles |
| Volunteers from notables | Yes | None | Yes, at 65% of a town's speed, 50% chance of noble troops |
| Noble recruits from villages | No | Yes | Yes, unchanged |
| Prosperity | About 1,700 to 5,600 | About 420 to 1,100 | Raised to at least 1,000, grows with trade |
| Prosperity tax | Full | Low | 80% of a town at the same prosperity |
| Marketplace project | Yes | No | Yes, at 60% of the town cost |
| Rebellion | Yes | Never | Yes, can be switched off |
| Town healing perks | Yes | No | Yes |
| Smithy | Yes, with smithing orders | No | Yes, without smithing orders |
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
4. **Only what a castle can actually host.** Castles have no arena and no town streets in the game. Arenas, tournaments, town streets to walk in third person, and the quests that depend on them were left out on purpose, rather than added in a broken state. The base game's **Take a walk around the castle** stays. A castle tavern uses the town tavern scene, so it is included.
5. **A lord's seat, not a pleasure district.** Brothels from other mods are kept out of castles.
6. **Optional and reversible.** Most features have their own switch. Town-style castle entry and the **Go to the castle** menu have none; they follow **Enable castle markets** and the two **Apply to** switches. Turning a feature off returns the castles to vanilla and removes only what the mod added. One-time seeded companions remain as ordinary game companions and move out of castles once they are free to move; they are not deleted. **Keep brothels out** is the other deliberate exception: it is independent of the master switch and removes any castle location named `brothel` until its own switch is turned off.

## Mod options ([MCM][mcm])

All values and features can be changed in the game through [Mod Configuration Menu (MCM)][mcm], under **Options > Mod Options > Castle Market Towns**, without restarting. You can switch features on or off, choose which castles are covered (your clan, your kingdom, or everyone), and adjust:

- the number of merchants, artisans and workshops;
- an extra limit on how many workshops you can own, in towns and castles alike;
- the prosperity floor, starting market stock, production and consumption;
- the tax and trade prosperity;
- recruit speed and noble recruit chance;
- caravan preferences;
- companions in castle taverns, with their arrival chance, departure chance and minimum stay;
- the Marketplace cost;
- each castle quest;
- Improved Garrisons support, if that mod is installed.

## Requirements and versions

| Dependency | Tested version | Expected to work with |
|---|---|---|
| Mount & Blade II: Bannerlord | 1.4.8 | 1.4.x |
| War Sails (NavalDLC), optional | 1.2.8 | 1.2.x |
| [Bannerlord Software Extender (BLSE)][blse], optional | 1.7.5 | 1.x |
| [Harmony][harmony] | 2.4.2 | 2.x |
| [ButterLib][butterlib] | 2.12.0 | 2.x |
| [UIExtenderEx][uiextenderex] | 3.0.3 | 3.x, as required by MCM 5.13 |
| [Mod Configuration Menu v5 (MCM)][mcm] | 5.13.1 | 5.x |

This mod needs [Harmony][harmony] and [MCM][mcm] directly. [MCM][mcm] in turn needs [ButterLib][butterlib] and [UIExtenderEx][uiextenderex].

[BLSE][blse] is optional. As [Harmony][harmony]'s page notes, it is not required on Steam, GOG or Epic, and none of the mods above require it there. This mod does not use it either. BLSE improves mod load-order sorting and crash reporting, and the Xbox / PC Game Pass version of the game needs it.

The mod only uses the stable public parts of [Harmony][harmony] and [MCM][mcm], so newer releases with the same major version (Harmony 2.x, MCM 5.x) should work without changes. A new major version, such as MCM 6, may need an update of this mod.

Bannerlord updates can change the game code this mod adjusts. If that happens, the affected feature stays inactive and a message naming it is written to the game log, instead of the game crashing.

With War Sails, castles only link to towns they can actually reach. Caravans use the base game's land or mixed sea-and-land navigation to visit castle markets, including the castle on the northern island. Castles remain portless, and ship services remain exclusive to real ports.

## Compatibility

- **Compatible** with Hot Butter and Captivity Events.
- **Improved Garrisons** (tested with 4.3.0.3) is supported without being required, and its files are never changed. Its garrisons recruit from castle merchants and artisans as they do from town notables, and its management screen shows the castle Marketplace, the castle's daily defaults, castle workshops and castle tax. With **Improved Garrisons support** on in [MCM][mcm] (the default), in castles this mod covers its button moves into **Go to the castle**, as it sits in **Go to the keep** in towns; its project tooltips show the castle Marketplace tariff and the Castellan's Office tax; and its recruiter, patrol and mobile garrison parties treat those castles like towns, recruiting from castle notables, selling loot and buying horses in the castle market. Its **Gold change** figure leaves out tariff income, for towns and castles alike. If an Improved Garrisons update changes the parts this mod adjusts, that support stays inactive and the game log names it.
- **[CompanionsCraft][companionscraft]** (tested with 1.1.0) is supported without being required, and its files are never changed. When you leave a castle, its companions buy hardwood, charcoal, iron ore and iron from the castle market at the castle's prices, as they do in towns. Before 1.3.0, leaving a castle whose market stocked any of these crashed the game. Refining stays in towns, as in CompanionsCraft itself. If a CompanionsCraft update changes the part this mod adjusts, that support stays inactive and the game log names it.
- **Not compatible** with other mods that turn castles into towns or overhaul castle menus, markets, notables or buildings. Both mods would change the same castle objects. To switch to another castle overhaul, remove this mod first (see below).
- **Companions:** on the first eligible daily tick, the mod asks the active companion system to create a small, globally capped seed from its unused companion templates and places each in a different eligible castle. This is the base game's companion system, or a replacement that keeps its companion-creation step, such as Women in Calradia's. If no such system is active, the seed is skipped once and castle taverns fill only as existing companions move from towns. A save marker prevents another seed after the option is switched off and on. Afterward, unhired companions move between towns and nearby castles over time. The seed and new arrivals stop once castles hold a third of the free pool; if hiring later shrinks the pool, castles can briefly hold more until companions leave. A companion only moves to one of the 3 castles closest to their town, and returns to one of the 3 towns closest to their castle, so they do not jump across the map. The **Castle companions** group in [MCM][mcm] sets the daily arrival chance (default 100%), the daily departure chance (default 10%) and the minimum stay between moves (default 7 days, 1 to 30); these settings are greyed out while the group's switch is off. The base game's own town-to-town companion swaps are unchanged. War Sails is supported without being required, including while its storyline location model is active. Mods that only add companion templates remain compatible. If another mod overrides companion behavior and causes problems in castles, turn off **Companions in castle taverns** in [MCM][mcm]: tracked unhired castle companions go to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists, once they are free to move (not giving a quest, imprisoned, in a party or in the castle you are in), and the rest of the mod is unaffected. Seeded companions remain normal companions in the world and are not deleted.

## Installation

1. Install [Harmony][harmony], [ButterLib][butterlib], [UIExtenderEx][uiextenderex] and [MCM][mcm]. [BLSE][blse] is optional on Steam, GOG and Epic.
2. Download Castle Market Towns from [Nexus Mods][nexus]. Install it with Vortex (*Mod Manager Download*), or extract the `CastleMarketTowns` folder into `Mount & Blade II Bannerlord\Modules`.
3. Start the game launcher, or the [BLSE][blse] launcher if you use it. Enable Castle Market Towns and place it after [Harmony][harmony] and [MCM][mcm].

The mod can be added to an existing campaign.

## Removing the mod from a campaign

The mod adds no save types of its own, so a save still loads without it. To return castles to vanilla before removing it:

1. Load the campaign. In [MCM][mcm], turn off **Enable castle markets**.
2. Let three or more in-game days pass. Each castle cleans up on its daily update:
   - workshops you own in the castle are sold to one of its notables at the base game's price, or to a notable of the nearest town if none is left. Their warehouse goods go to your party if you are in the castle, to the castle stash if your clan owns it, and are otherwise sold at the castle's price;
   - the tavern, mod workshops and castle Marketplace are removed;
   - the goods and prosperity the mod added are taken back;
   - merchants and artisans created by the mod move to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists;
   - tracked unhired companions, including the one-time seeded companions, move by the same rule and remain normal companions in the world. A companion who is giving a quest, imprisoned or in a party moves only once that ends, so check castle taverns are empty before uninstalling, and leave the castle you are in so its companion can move;
   - daily defaults return to vanilla.
   - **Keep brothels out** continues to block castle brothels independently; turn that switch off too if you want another mod's brothel restored before uninstalling.
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
[companionscraft]: https://www.nexusmods.com/mountandblade2bannerlord/mods/8649
[repo]: https://github.com/AtanuDebbarma/CastleMarketTowns
[nexus]: https://www.nexusmods.com/mountandblade2bannerlord/mods/14182
