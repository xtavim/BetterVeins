using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterVeins.Scripts
{
    public static class VeinDrops
    {
        private static readonly System.Func<DropTable, int, List<GameObject>> RollExactly =
            AccessTools.MethodDelegate<System.Func<DropTable, int, List<GameObject>>>(
                AccessTools.Method(typeof(DropTable), "GetDropList", new[] { typeof(int) }));

        // The game's own numbers, from DropOnDestroyed: half a metre of lift, then each successive
        // stack another three tenths higher inside a half metre circle, so a deposit's worth of loot
        // ends up as a short column rather than a heap of colliders shoving each other apart.
        private const float SpawnYOffset = 0.5f;
        private const float SpawnYStep = 0.3f;
        private const float SpawnRadius = 0.5f;

        private static readonly Dictionary<GameObject, int> Tally = new Dictionary<GameObject, int>();

        public static void Spawn(DropTable table, int areas, Vector3 anchor, bool cheated)
        {
            if (table == null || table.IsEmpty()) return;

            anchor = Grounded(anchor) + Vector3.up * SpawnYOffset;

            if (!Plugin.mergeDrops.Value)
            {
                Scatter(table, areas, anchor, cheated);
                return;
            }

            Tally.Clear();

            foreach (var prefab in Roll(table, areas))
            {
                if (prefab == null) continue;

                Tally.TryGetValue(prefab, out var had);
                Tally[prefab] = had + StackOf(prefab);
            }

            var step = 0;

            foreach (var pair in Tally)
            {
                if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"VeinDrops: {pair.Value} x {pair.Key.name}");

                Pile(pair.Key, pair.Value, anchor, ref step, cheated);
            }

            Tally.Clear();
        }

        private static Vector3 Grounded(Vector3 position)
        {
            if (ZoneSystem.instance == null) return position;

            var ground = ZoneSystem.instance.GetGroundHeight(position);

            if (position.y < ground) position.y = ground + 0.1f;

            return position;
        }

        // One roll of the drop table per chunk, which is what vanilla would have done. Where the table
        // allows it, the per-chunk amounts are added up first and drawn in a single pass: the private
        // overload picks that many items by weight and nothing carries between draws, so the result has
        // the same distribution as rolling one chunk at a time and costs one list instead of forty.
        // A table that drops one of each, or that can decline to drop at all, does carry state between
        // draws, so those keep the honest loop.
        private static IEnumerable<GameObject> Roll(DropTable table, int areas)
        {
            if (RollExactly == null || table.m_oneOfEach || table.m_dropChance < 1f || Game.m_resourceRate < 1f)
            {
                for (var i = 0; i < areas; i++)
                {
                    foreach (var prefab in table.GetDropList()) yield return prefab;
                }

                yield break;
            }

            var amount = 0;

            for (var i = 0; i < areas; i++) amount += Random.Range(table.m_dropMin, table.m_dropMax + 1);

            if (amount <= 0) yield break;

            foreach (var prefab in RollExactly(table, amount)) yield return prefab;
        }

        private static void Pile(GameObject prefab, int total, Vector3 anchor, ref int step, bool cheated)
        {
            if (total <= 0) return;

            var max = MaxStackOf(prefab);

            if (Plugin.overstackDrops.Value || max <= 0) max = total;

            while (total > 0)
            {
                var stack = Mathf.Min(total, max);
                total -= stack;

                var spawned = Object.Instantiate(prefab, Placed(anchor, step++), Rotation());

                var item = spawned.GetComponent<ItemDrop>();
                if (item != null) item.m_itemData.m_stack = stack;

                ItemDrop.OnCreateNew(spawned, cheated);
            }
        }

        private static void Scatter(DropTable table, int areas, Vector3 anchor, bool cheated)
        {
            var step = 0;

            for (var i = 0; i < areas; i++)
            {
                foreach (var prefab in table.GetDropList())
                {
                    if (prefab == null) continue;

                    ItemDrop.OnCreateNew(
                        Object.Instantiate(prefab, Placed(anchor, step++), Rotation()), cheated);
                }
            }
        }

        private static Vector3 Placed(Vector3 anchor, int step)
        {
            var circle = Random.insideUnitCircle * SpawnRadius;

            return anchor + new Vector3(circle.x, SpawnYStep * step, circle.y);
        }

        private static Quaternion Rotation()
        {
            return Quaternion.Euler(0f, Random.Range(0, 360), 0f);
        }

        private static int StackOf(GameObject prefab)
        {
            var item = prefab.GetComponent<ItemDrop>();
            return item != null ? Mathf.Max(1, item.m_itemData.m_stack) : 1;
        }

        private static int MaxStackOf(GameObject prefab)
        {
            var item = prefab.GetComponent<ItemDrop>();
            return item != null ? item.m_itemData.m_shared.m_maxStackSize : 0;
        }
    }
}
