# Translating Castle Market Towns

Castle Market Towns ships in English only. Every piece of text the mod adds can be translated, and translations are welcome as separate mods that require Castle Market Towns. You do not need to change or copy any of its files.

## What can be translated

All of the mod's own text is in `ModuleData/Languages/EN/cmt_strings_en.xml`: the Mod Configuration Menu (MCM) page with every group, setting name and hint, the castle menus including the castle trainers, the workshop limit messages and the clan screen's workshop count, the notification shown when castle workshops are sold, and three error messages. Use that file as your template.

Some castle menu lines reuse the base game's own text, such as **Trade**, **Recruit troops**, **Enter smithy** and **Back**. The game already translates those, so they are not in the file.

The Hot Butter options in the castle tavern use Hot Butter's own IDs, which start with `pompa`. They belong to Hot Butter and are not in the file. You may translate them in your module as well; that also translates the same options in towns.

## Making a translation module

A translation module contains only text files. Example for Russian:

```
CastleMarketTowns_RU/
├─ SubModule.xml
└─ ModuleData/
   └─ Languages/
      └─ RU/
         ├─ language_data.xml
         └─ cmt_strings_ru.xml
```

`SubModule.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Module>
  <Name value="Castle Market Towns - Russian"/>
  <Id value="CastleMarketTowns_RU"/>
  <Version value="v1.0.0"/>
  <SingleplayerModule value="true"/>
  <MultiplayerModule value="false"/>
  <DependedModules>
    <DependedModule Id="CastleMarketTowns" DependentVersion="v1.3.2"/>
  </DependedModules>
  <DependedModuleMetadatas>
    <DependedModuleMetadata id="CastleMarketTowns" order="LoadBeforeThis" version="v1.3.2"/>
  </DependedModuleMetadatas>
  <SubModules/>
</Module>
```

Give your module its own `Id`. Set `DependentVersion` and `version` to the Castle Market Towns version your file was made from.

`language_data.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Русский">
  <LanguageFile xml_path="RU/cmt_strings_ru.xml" />
</LanguageData>
```

`cmt_strings_ru.xml` is a copy of `cmt_strings_en.xml` with the language tag changed to the same name and every `text` value translated:

```xml
<tags>
  <tag language="Русский" />
</tags>
```

### Language names

The language name must match the game's exactly, including accents and capital letters:

| Language | Name to use |
|---|---|
| Brazilian Portuguese | `Português (BR)` |
| Chinese (Simplified) | `简体中文` |
| Chinese (Traditional) | `繁體中文` |
| French | `Français` |
| German | `Deutsch` |
| Italian | `Italiano` |
| Japanese | `日本語` |
| Korean | `한국어` |
| Latin American Spanish | `Español (LA)` |
| Polish | `Polski` |
| Russian | `Русский` |
| Turkish | `Türkçe` |

For a language the game does not list here, copy the name from the `language_data.xml` of a mod that adds that language to the game.

## Rules for the text

- **Keep every `id` exactly as it is.** Only change `text`.
- **Keep every `{VARIABLE}`,** such as `{SETTLEMENT}`, `{COUNT}`, `{GOLD}` and `{GOLD_ICON}`. You may move them to fit your language's word order.
- **Conditional text** looks like `{?(COUNT > 1)}workshops there were{?}workshop there was{\?}`: the part before `{?}` is used when the condition is true, the part after it otherwise. Translate both parts and keep the markers. If your language needs other plural forms, you may change the condition; the game's own translation files for your language show the forms it uses.
- **MCM group names** are translated one by one. **Castle companions** is shown inside **General**, so translate each on its own.
- **Save the file as UTF-8.**
- **Escape XML characters** inside `text`: `&` as `&amp;`, `<` as `&lt;`, `>` as `&gt;` and `"` as `&quot;`.
- **Missing lines are fine.** Any ID left out of your file shows in English, so a partial translation still works.

## Testing

1. Enable your module after Castle Market Towns in the launcher and choose your language in the game's options.
2. Check the **Castle Market Towns** page in MCM under **Options > Mod Options**, and the castle menus: **Go to the castle**, **Visit the castle trainers**, **Visit the castle tavern** and **Back to the castle**.
3. A line that still shows in English means its ID is missing or misspelled in your file. If the game log reports an XML error, check the escaping rules above.

The translation only changes text. It adds nothing to saves, so it can be added or removed at any time.

## Keeping up with updates

IDs are kept from one version to the next, so a translation keeps working after Castle Market Towns updates. New text gets new IDs, which show in English until your translation adds them. The [changelog](CHANGELOG.md) lists added, changed and removed IDs for each version.

## Permission and credit

You may publish a translation of Castle Market Towns as a separate mod, on Nexus Mods or elsewhere, under terms of your choice, as long as it contains only translated text and no code or other files from Castle Market Towns. Please list Castle Market Towns as a requirement and credit it in your mod's description:

    Translation of Castle Market Towns by AtanuDebbarma
    https://www.nexusmods.com/mountandblade2bannerlord/mods/14182
