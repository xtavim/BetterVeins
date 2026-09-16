using System.Collections.Generic;

namespace BetterVeins.Scripts
{
    public enum NodeKind
    {
        Ore,
        Scrap,
        Rock
    }

    public static class NodeKinds
    {
        private static readonly int Stone = "Stone".GetStableHashCode();
        private static readonly int IronScrap = "IronScrap".GetStableHashCode();

        private static readonly Dictionary<int, NodeKind> Known = new Dictionary<int, NodeKind>();

        public static bool Wanted(ZNetView nview, DropTable table)
        {
            switch (Of(nview, table))
            {
                case NodeKind.Ore: return Plugin.oreDeposits.Value;
                case NodeKind.Scrap: return Plugin.scrapPiles.Value;
                case NodeKind.Rock: return Plugin.rockFormations.Value;
                default: return false;
            }
        }

        public static NodeKind Of(ZNetView nview, DropTable table)
        {
            if (nview == null || !nview.IsValid()) return NodeKind.Ore;

            var prefab = nview.GetZDO().GetPrefab();

            if (Known.TryGetValue(prefab, out var kind)) return kind;

            kind = Classify(table);
            Known[prefab] = kind;

            if (Plugin.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"NodeKinds: {nview.name} is {kind}");
            }

            return kind;
        }

        private static NodeKind Classify(DropTable table)
        {
            if (table == null || table.m_drops == null) return NodeKind.Ore;

            var stoneOnly = true;
            var sawAnything = false;

            foreach (var drop in table.m_drops)
            {
                if (drop.m_item == null) continue;

                sawAnything = true;

                var hash = drop.m_item.name.GetStableHashCode();

                if (hash == IronScrap) return NodeKind.Scrap;
                if (hash != Stone) stoneOnly = false;
            }

            if (!sawAnything) return NodeKind.Ore;

            return stoneOnly ? NodeKind.Rock : NodeKind.Ore;
        }
    }
}
