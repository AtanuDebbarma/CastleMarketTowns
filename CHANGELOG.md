# Changelog

## 1.1.0

- Castles gain a smithy. **Enter smithy** in the castle menu opens the same smithy screen as in towns, for smithing, smelting and refining with your own materials. Smithing orders stay in towns, so the castle smithy shows none. It has its own MCM switch, **Castle smithy**, on by default.
- The castle menu is laid out like a town menu. **Go to the castle** opens the dungeon, Donate troops to garrison, Manage garrison, Open stash and the lord's hall, as **Go to the keep** does in towns, and Manage castle as well. Each keeps the base game's rules for who sees it. The castle tavern, Recruit troops, Trade and Enter smithy follow in town order. Castles the mod does not cover keep the base game's menu.
- Castle menu changes are registered after the base game finishes creating its options, so **Go to the castle** and its submenu initialize reliably on both new and loaded campaigns.
- Castles open their gates like towns. Visitors the castle would let in walk straight into the castle menu for the tavern, trade, recruits and smithy, without asking the guards. **Go to the castle** now guards the keep as **Go to the keep** does in towns: visitors from outside your kingdom with clan tier below 3 pay the castle's usual bribe, and for them it is greyed out when nobody is inside. The lords in the hall can be spoken to only from inside the castle, not from the castle menu or while waiting. War, any crime rating and a bad relation with the castle's lord still keep you outside, and asking the guards for entry no longer gets around a bad relation. **Request a meeting with someone** stays at the gate. Castles the mod does not cover keep the base game's gate.
- Companions can be hired in castle taverns. On the first eligible day, the active companion system (the base game's, or a replacement such as Women in Calradia's) creates a small, globally capped seed from its unused templates in different castles; a save marker prevents reseeding. If no compatible companion system is active, the seed is skipped once and castle taverns fill only as companions move from towns. Afterward, unhired companions move between towns that are not under siege and nearby castle taverns over time. The seed and new arrivals stop once castles hold a third of the free pool. Moves only go to one of the 3 closest castles or towns, and the **Castle companions** MCM group sets the daily arrival chance (default 100%), daily departure chance (default 10%) and minimum stay between moves (default 7 days, 1 to 30). War Sails' active storyline location model is supported without making the DLC a dependency. The MCM switch, **Companions in castle taverns**, is on by default; turning it off sends tracked unhired castle companions to the nearest reachable town of the castle's culture, or the nearest reachable town if none exists, once they are free to move (not giving a quest, imprisoned, in a party or in the castle you are in), without deleting seeded heroes.
- War Sails caravans can reach castle markets without treating castles as ports. The base game's navigation chooses a land route or a mixed sea-and-land route, including for the northern island castle, while ship services remain exclusive to real ports. Caravans also perform the base game's overweight sell-down when leaving a castle.
- MCM now states the correct requirement for Caravan Ambush: its merchant or artisan must own at least one caravan. Cleanup descriptions now explain the same-culture town preference and that **Keep brothels out** operates independently of the master switch.
- AI lords recruit from castle notables only when **Recruit troops in castles** is enabled, the castle is covered by the mod and that lord's clan is allowed by the two clan-coverage switches. Town and village recruitment remains unchanged.
- Patch diagnostics now name features whose expected game methods are missing even when Harmony skips the patch before applying it.
- Castle merchants get a title after their name, such as "the Broker", as town merchants do. Merchants already in your castles get theirs when you load the save.
- A merchant or artisan who moves from a castle to a town no longer keeps their castle workshop when no notable stays behind to take it over. The workshop is removed instead.
- BLSE is now listed as optional instead of required. This mod does not use it, and none of the mods it needs require it on Steam, GOG or Epic. It is still needed for the Xbox / PC Game Pass version of the game.
- Tested with UIExtenderEx 3.0.3 and MCM 5.13.1.

## 1.0.0

First public version, tested with Bannerlord 1.4.8, War Sails 1.2.8, Harmony 2.4.2 and MCM 5.12.3.

- Castles gain a market, tavern, merchants, artisans, workshops, a Marketplace project, town-style tax and prosperity, caravan trade, recruits and selected quests, each with its own MCM switch.
- Saves that hit the base game's 32 KB save entry limit load again, and the mod splits its own save data so new saves stay under the limit.
- The mod records which taverns, markets, Marketplaces and daily-default buildings it created, and only removes those when it returns a castle to vanilla.
- Castles only link to towns they can reach, including island castles with War Sails.
- A log message names any patch that found nothing to change after a game update or another mod changed the game code.
