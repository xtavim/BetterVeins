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

        private static readonly Dictionary<GameObject, int> Tally = new Dictionary<GameObject, int>();

        public static void Spawn(DropTable table, int areas, Vector3 centre, bool cheated)
        {
            if (table == null || table.IsEmpty()) return;

            if (!Plugin.mergeDrops.Value)
            {
                Scatter(table, areas, centre, cheated);
                return;
            }

            Tally.Clear();

            foreach (var prefab in Roll(table, areas))
            {
                if (prefab == null) continue;

                Tally.TryGetValue(prefab, out var had);
                Tally[prefab] = had + StackOf(prefab);
            }

            foreach (var pair in Tally)
            {
                if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"VeinDrops: {pair.Value} x {pair.Key.name}");

                Pile(pair.Key, pair.Value, centre, cheated);
            }

            Tally.Clear();
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

        private static void Pile(GameObject prefab, int total, Vector3 centre, bool cheated)
        {
            if (total <= 0) return;

            var max = MaxStackOf(prefab);

            if (Plugin.overstackDrops.Value || max <= 0) max = total;

            while (total > 0)
            {
                var stack = Mathf.Min(total, max);
                total -= stack;

                var spawned = Object.Instantiate(prefab, centre, Quaternion.identity);

                var item = spawned.GetComponent<ItemDrop>();
                if (item != null) item.m_itemData.m_stack = stack;

                ItemDrop.OnCreateNew(spawned, cheated);
            }
        }

        private static void Scatter(DropTable table, int areas, Vector3 centre, bool cheated)
        {
            for (var i = 0; i < areas; i++)
            {
                foreach (var prefab in table.GetDropList())
                {
                    if (prefab == null) continue;

                    var position = centre + Random.insideUnitSphere * 0.3f;

                    ItemDrop.OnCreateNew(Object.Instantiate(prefab, position, Quaternion.identity), cheated);
                }
            }
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
