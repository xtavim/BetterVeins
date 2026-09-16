# BetterVeins

**Break a whole deposit in one swing.**

Copper, tin, silver, obsidian, scrap piles, plain rock — whatever you are swinging at, no more chipping away at the same thing forty times over. Break off one chunk and the rest comes down with it, and everything it drops lands in a neat stack at your feet instead of scattered halfway down the hill.

**And it won't wreck your framerate.** Other vein mining mods stutter because they smash the rock one piece at a time, forty times in a single frame. BetterVeins takes the whole thing down in one go, so a huge deposit costs your game no more than a single swing. It is genuinely lighter than mining the deposit by hand.

## What it feels like

Mine normally. When the first chunk breaks off, the rest goes with it.

Your pickaxe still has to be good enough for the ore — bronze on silver still says "too hard". And a better pickaxe still gets that first chunk off in fewer swings, so upgrading still matters.

Want it even faster? There is a setting to bring the whole thing down on your very first swing.

**Hold Left Alt to mine a single chunk**, for when you are trimming a rock you are building around or you just want a bit of stone.

## Why it is so much faster

Valheim charges a fixed price for every chunk you break. The deposit's health is decoded out of the save and re-encoded back into it, a message goes out to every player on the server, and each of those messages makes every client rebuild the deposit's mesh from scratch — three mesh combines, every time.

Other vein mining mods break chunks one after another, so they pay that price for every chunk in the deposit. Forty chunks means forty save writes, forty broadcasts and forty mesh rebuilds on every connected client, all in one frame. Spreading the work over several frames, which is the usual attempt at a fix, does not remove any of it — it just smears the stutter out.

BetterVeins never breaks chunks at all. It reads the health once, zeroes the whole deposit in memory, writes once, and sends one message. **A forty chunk deposit costs exactly what one chunk costs.** That is less work than the game does when you mine the deposit yourself, so vein mining here is not a performance cost you are accepting for convenience — it is a performance gain.

The loot is added up before anything is spawned, for the same reason. Every item on the ground is a networked object with a ZDO of its own, and forty of them appearing at once is a stutter in its own right. Adding the drops up first turns dozens of them into a handful of full stacks, dropped in one spot.

## What it does not change

The drop table is still rolled once per chunk, exactly as the game rolls it. The odds and the amounts are unchanged — the results are just added together before anything lands.

## Installation

Install with your mod manager, or drop `BetterVeins.dll` into `BepInEx/plugins`.

## Configuration

Everything below can be changed in-game with Configuration Manager, or in `BepInEx/config/xtav1m.BetterVeins.cfg`.

### General

| Setting | Default | What it does |
| --- | --- | --- |
| Enable | On | |
| Break On First Hit | **Off** | The whole deposit on the first swing, without waiting for a chunk |
| Mine One Chunk | Left Alt | Hold to take a single chunk, the way the game does |

### Nodes

| Setting | Default | What it does |
| --- | --- | --- |
| Ore Deposits | On | Copper, tin, silver, obsidian, and anything else that gives metal |
| Scrap Piles | On | Muddy scrap piles in the swamp crypts |
| Rock And Stone | On | Plain rock. Turn off if you shape rock by hand |

### Cost

| Setting | Default | What it does |
| --- | --- | --- |
| Durability Per Chunk | 1.0 | Pickaxe wear per chunk, as a multiple of a normal swing. 0 is free |
| Stamina Per Chunk | 1.0 | Stamina per extra chunk, as a multiple of a normal swing. 0 is free |

Neither ever stops a deposit halfway. A pickaxe that runs out finishes the job and breaks on your next swing.

### Drops

| Setting | Default | What it does |
| --- | --- | --- |
| Merge Drops | On | Add the drops up into full stacks in one spot, instead of one per chunk |
| Overstack Drops | **Off** | Everything in a single stack even past the normal limit |

**On Overstack Drops:** an oversized stack does not fit an inventory slot cleanly, and one left on the ground stays oversized if you remove the mod. Left off, you get as few normal stacks as the item's stack limit allows, all sitting on the same spot — 200 stone is four stacks of fifty, not one of two hundred. Turn it on if you would rather have the single stack and can live with the caveats.

## Compatibility

- Built against **Valheim 1.0** (build 25185596).
- Requires **BepInExPack Valheim 5.4.2350** or newer.
- **Only the person swinging needs it.** Deposits come down through the game's own machinery, so other players see it happen normally whether or not they have the mod.
- Install it on the server too if you want everyone playing by the same rules. Settings are synchronized with ServerSync and the server's values take precedence while **Lock Configuration** is enabled. Without it on the server, each player simply uses their own settings.
- Removing the mod changes nothing permanently. Deposits and loot go back to vanilla behaviour.

## Changelog

See [CHANGELOG.md](changelog) for version history.

## Credits

- **ServerSync** - server-authoritative configuration syncing, merged into the plugin DLL.

## License

This mod is provided as-is for the Valheim community.
