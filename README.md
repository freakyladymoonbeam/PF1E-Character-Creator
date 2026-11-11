# PF1E Character Creator
Now you can make Pathfinder 1E characters on your desktop! Well, soon. Right now this is the "rules engine" underneath the future desktop app. It knows how to represent a character and work out their numbers, but there are no screens yet.

## What's in here
- **`src/Rules.Core`** – the rules library (C#, .NET 11). A `Character` has a name, race, classes and levels, and ability scores. The code is split by topic: abilities, classes, combat, feats, skills, spells, and modifiers (bonuses and penalties from various sources that get added up to a final number).
- **`data/core`** – the game content, stored as plain JSON files rather than hard-coded: races (currently Elf), classes (Fighter, Wizard), feats, and skills. Adding or fixing content generally means editing these files, not the code. Feats list their prerequisites (such as a minimum caster level) in the data.
- **`tests/Rules.Tests`** – automated tests covering characters, hit points, classes, feats, races, skills, and modifiers.

## How it fits together
1. `DataPackLoader` reads the JSON "data packs" into race, class, feat, and skill definitions.
2. A `Character` is built from those definitions.
3. The modifier system combines all bonuses and penalties to produce the character's final stats.

## Status
Early work in progress. Only a small slice of the rules and content (one race, two classes, a starter set of feats) is included so far.

## Building and testing
Requires the .NET SDK (the projects target `net11.0`).

```
dotnet build PathfinderCreator.slnx
dotnet test PathfinderCreator.slnx
```