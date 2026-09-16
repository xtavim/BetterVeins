# BetterVeins — design

Whole-deposit mining for Valheim 1.0.7. Native only, no Jotunn, same as BetterMap:
everything it needs is reachable on `MineRock5`, `DropTable` and `ZNetView`, and the
one thing that is private is reached the way BetterMap reaches private things.

Every feature is a BepInEx config entry, synchronised with ServerSync.

## Guiding principle

**The player's swing is never faked.** There is no invented damage figure anywhere in
this mod, no 1,000,000. The deposit comes down because its chunk health is set to
zero, not because it was hit with a number nobody could survive.

By default the swing itself goes through vanilla untouched — the tool tier check, the
damage number, the effects, the statistics — and we only look at what it did
afterwards. The one exception is the mode that takes the deposit on the first swing,
which has to step in before the game has processed the chunk. There the tier check,
the resistances, the damage number and the effects are run in the same order vanilla
runs them, using the swing's own damage. A pickaxe too weak for the ore still bounces
off either way.

The second principle is BetterMap's: derive from game data, never from hardcoded
tables. What kind of deposit something is comes from what it drops.

## The trigger

Mine normally. When the **first chunk breaks off**, the rest of the deposit goes
with it.

That is the default, and the alternative — the whole deposit on the first swing —
is a setting that is off. The reason is progression. Tool tier still gates
everything either way, so bronze never touches silver, but if one hit is enough
then every pickaxe good enough for the ore behaves identically and the upgrade
stops meaning anything. Waiting for a chunk keeps a weak pickaxe slower than a
good one and gives the collapse a reason: the deposit was undermined.

The repetition worth removing is the hundred and fifty swings after the first few,
not the first few.

## Why it is fast

Vanilla pays a fixed price per chunk because a player only ever breaks one at a
time. Per chunk, `MineRock5.DamageArea` does:

- `LoadHealth`, which reads the health array out of the ZDO as base64 and decodes it
- `SaveHealth`, which encodes it again and bumps the ZDO revision, so it syncs
- an RPC to **every** player announcing the chunk's death
- and on the receiving end that RPC calls `UpdateMesh`, which combines the surviving
  chunks into a single mesh — three `CombineMeshes` calls and two array allocations
- a drop table roll, then one `Instantiate` and one new ZDO per item that fell out

A forty chunk copper deposit done the obvious way is forty base64 decodes, forty
encodes, forty network broadcasts, and **forty full mesh rebuilds on every connected
client**. That last one is the stutter. Spreading the work over several frames, which
is what the mods people complain about do, does not remove any of it.

So the sweep does not break chunks. It reads the array once, zeroes every chunk that
is still standing, writes the array once, and destroys the deposit. The mesh is never
rebuilt at all. **A deposit of forty chunks costs less than one chunk costs.**

The sweep zeroes every chunk that was standing, so there is never anything left to
show and never a reason to send anything of our own. The deposit is destroyed through
the same path the game uses for one mined by hand, which is why a player without the
mod sees it come down normally and why nothing here needs to be on the server.

A cap on chunks per deposit used to guard against a deposit larger than anything the
game ships. It was never reached, and it was the only thing that could leave a
deposit half standing — which meant carrying a custom message, a health array packed
into it, and a handler on every deposit in the world, all for a case that never
happened. Removing it deleted all of that.

## Ownership

A deposit is damaged by sending its owner a message, and the owner is whoever
happens to have loaded that part of the world. On a server that is usually not the
person swinging. Since the sweep reads settings and a held key that only exist on
the player's own machine, the player takes the deposit over before the swing lands.
Vanilla does the same whenever a client needs authority over something.

## Drops

Full stacks in one spot, not a carpet.

What a deposit gives is untouched. The drop table is still rolled once per chunk,
exactly as vanilla rolls it, so the odds and the amounts are the game's. The results
are added up before anything is spawned, then put down as full stacks: forty chunks
that would have dropped forty objects drop as many as the stack limit needs, which
for eighty copper ore at a limit of thirty is three.

That is worth more than tidiness. Every dropped object is a networked object with a
ZDO of its own, and forty appearing in one frame is its own stutter.

### Where they land

Not at the chunk the player hit. That was the first idea, since it puts the loot under
the pickaxe, and it is wrong: a flametal deposit standing in lava has chunks below the
surface, and swinging at one of those put the entire deposit's ore under the lava,
where it sinks out of reach. Vanilla gets away with spawning at the hit chunk because
it spawns per chunk, so only that chunk's share is lost. Gathering everything into one
place turns a fraction into all of it.

So the anchor is the **highest chunk that was still standing**. A deposit has its top
out of whatever it is standing in, or there would have been nothing to hit, which makes
that the one point on it known to be reachable.

From there the placement is `DropOnDestroyed`'s, the game's own pattern for putting a
pile of items on the ground:

- clamp to `ZoneSystem.GetGroundHeight` so nothing starts underground
- lift half a metre
- step each successive stack another three tenths higher, inside a half metre circle,
  with a random facing

The step matters as much as the height. Spawning several stacks at one point leaves
their colliders inside each other, and physics resolves that by flinging them apart —
which near a lava edge is its own way to lose the ore. A short column settles instead.

Lava is not something the game models as a liquid: `LiquidType` knows water and tar and
nothing else, so items do not float on it, and `ItemDrop.TerrainCheck` will helpfully
push a sunk item down to the rock floor underneath it. There is no asking the game how
high the lava is. Anchoring above it is the whole defence.

### Overstacking

Asking for "200 stone in one stack" means a stack four times what stone is allowed
to reach. That is possible and it is a setting, but it is **off**, because:

- an oversized stack does not fit an inventory slot cleanly, and every path that
  puts an item away reasons in terms of the maximum stack size
- one left on the ground stays oversized after the mod is removed, which breaks
  BetterMap's rule that uninstalling is survivable
- a player without the mod picking it up hits the same problem

Off by default is the honest version of the same idea: as few legal stacks as
possible, all at one point. Four stacks of fifty stone sitting on each other are
still four objects, but they are four instead of forty and they behave like
ordinary loot.

## Cost

Keep the cost, drop the repetition.

- **Durability** wears for every chunk that comes off, not just the one you hit, as a
  multiplier on a normal swing's wear. The tool did the work, so it takes the work.
  Full cost by default, since a forty chunk deposit taking forty swings off a pickaxe
  is exactly what mining it by hand would have done.
- **Stamina** goes the same way, as a multiplier, so a large deposit still empties
  the bar.

Neither ever stops a sweep. Both are charged after the fact and are allowed to take
you to nothing, because a deposit that breaks halfway leaves the player with no idea
what went wrong, while a pickaxe that gives out right after it comes down is legible.
A worn out pickaxe then breaks on the next swing, the way it always does.

The chunk the player actually hit is not charged twice — the swing already paid for it.

## What counts as a deposit

Three kinds, worked out from the drop table rather than from a list of prefabs:

| Drops | Kind |
| --- | --- |
| iron scrap | scrap pile |
| nothing but stone | rock |
| anything else | ore |

Two item names are named in code, stone and iron scrap, and that is the whole
exception — the same unavoidable one BetterMap makes for vehicle prefabs. Nothing on
a dropped item says "this is the common building material", so the two that anchor
the classification are named and everything else falls out of them. Ore added by an
update or by another mod is handled with no maintenance here.

Each kind is a setting. Rock is the one people are most likely to turn off, because
shaping a boulder or digging into a cliff by hand is a real thing players do and
taking the whole thing is rarely what is wanted there.

## Suppressing it

Vein mining is on by default with no keybind, because the point of the mod is not
having to think about it, and holding a key through a two hour mining session is
worse than the problem.

The key is therefore a **suppress**, not an activate: hold it to take a single
chunk. For trimming a rock you are building around, or taking two stone instead of
two hundred.

## Reaching private members

`MineRock5.HitArea` is a private nested class and the health array, the mesh rebuild
and the health load and save are all private. They are reached with
`AccessTools.FieldRef` and `AccessTools.MethodDelegate`, resolved once into static
readonly delegates, which is BetterMap's house style and costs nothing per call.
The project still references the publicized assemblies, but only so the private
nested type can be named at compile time.

## Deferred

- **`Destructible`.** Single-health objects like small rocks and stumps. There are no
  chunks to sweep, so there is nothing for this to do that the game does not already.
- **Cross-deposit propagation**, two copper nodes standing against each other
  breaking together. Needs a bounded spatial query and a hard node cap, and is the
  one thing here that could genuinely be slow. Not worth it until someone asks.
- **Drops flying to the player**, which is polish and nothing more.

## Open until played

- Whether one destroyed effect at the pile reads as enough for a large deposit, or
  whether a handful at the extremes is worth the cost.
- Whether stamina at full rate empties the bar so fast that it feels punishing
  rather than fair, and what the right default multiplier is.

## Wards

Inside someone else's ward the sweep stands down and the swing is handed back to the
game, so a deposit in a stranger's base is still mined a chunk at a time. Your own
ward grants access and changes nothing. The game's own notification to the ward,
which is how it flashes and alerts, is sent for the sweep as well.

## Two deposit types

`MineRock5` keeps its chunk health as one packed array on the ZDO and rebuilds a
combined mesh; `MineRock`, the older type, keeps a float per chunk and hides chunk
objects one message at a time. They need separate code but the shape is the same:
read once, zero every chunk, drop once, tell everyone once.

`MineRock` has no support check and no ward hook of its own, so its sweep is the
simpler of the two.
