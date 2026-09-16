using HarmonyLib;
using UnityEngine;

namespace BetterVeins.Scripts
{
    // The older deposit type, which keeps a float per chunk on its ZDO rather than one packed array
    // and hides chunks one message at a time. The sweep is the same idea: zero every chunk, spawn the
    // drops together, and tell the other players once instead of once a chunk.
    public static class VeinRock
    {
        public const string RpcName = "BV_BreakRock";

        private static readonly AccessTools.FieldRef<MineRock, Collider[]> Areas =
            AccessTools.FieldRefAccess<MineRock, Collider[]>("m_hitAreas");

        private static readonly AccessTools.FieldRef<MineRock, ZNetView> NView =
            AccessTools.FieldRefAccess<MineRock, ZNetView>("m_nview");

        private static readonly System.Action<MineRock> UpdateVisability =
            AccessTools.MethodDelegate<System.Action<MineRock>>(
                AccessTools.Method(typeof(MineRock), "UpdateVisability"));

        private static readonly System.Func<MineRock, Collider, int> GetAreaIndex =
            AccessTools.MethodDelegate<System.Func<MineRock, Collider, int>>(
                AccessTools.Method(typeof(MineRock), "GetAreaIndex"));

        public static bool Sweeping;

        public static ZNetView View(MineRock rock) => NView(rock);

        public static int AreaIndexOf(MineRock rock, Collider collider)
        {
            return collider == null ? -1 : GetAreaIndex(rock, collider);
        }

        public static bool AreaDead(MineRock rock, int index)
        {
            var nview = NView(rock);
            if (nview == null || !nview.IsValid() || index < 0) return false;

            return nview.GetZDO().GetFloat(Key(index), rock.GetHealth()) <= 0f;
        }

        public static void Sweep(MineRock rock, HitData hit, int hitAreaIndex, int alreadyPaid)
        {
            var areas = Areas(rock);
            var nview = NView(rock);

            if (areas == null || nview == null || !nview.IsValid() || !nview.IsOwner()) return;

            var zdo = nview.GetZDO();
            var full = rock.GetHealth();
            var centre = Centre(rock, hit, hitAreaIndex);

            Sweeping = true;
            try
            {
                var broken = 0;

                for (var i = 0; i < areas.Length; i++)
                {
                    if (areas[i] == null) continue;
                    if (zdo.GetFloat(Key(i), full) <= 0f) continue;

                    zdo.Set(Key(i), 0f);
                    broken++;
                }

                if (broken == 0) return;

                VeinDrops.Spawn(rock.m_dropItems, broken, centre, Vein.Cheated(hit));
                VeinCost.Charge(hit, broken - alreadyPaid);

                rock.m_destroyedEffect.Create(centre, Quaternion.identity);

                if (rock.m_removeWhenDestroyed && AllDestroyed(rock))
                {
                    nview.Destroy();
                }
                else
                {
                    nview.InvokeRPC(ZNetView.Everybody, RpcName);
                    UpdateVisability(rock);
                }

                if (Plugin.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"VeinRock: broke {broken} chunks off {rock.name} at {centre}");
                }
            }
            finally
            {
                Sweeping = false;
            }
        }

        public static bool Present(MineRock rock, HitData hit, Vector3 point)
        {
            hit.ApplyResistance(rock.m_damageModifiers, out var modifier);

            var damage = hit.GetTotalDamage();

            if (damage <= 0f) return false;

            if (DamageText.instance != null) DamageText.instance.ShowText(modifier, point, damage);

            rock.m_hitEffect.Create(point, Quaternion.identity);

            if (hit.m_hitType != HitData.HitType.CinderFire)
            {
                var closest = Player.GetClosestPlayer(point, 10f);
                if (closest != null) closest.AddNoise(100f);
            }

            if (hit.GetAttacker() == Player.m_localPlayer && Game.instance != null)
            {
                var cheated = Vein.Cheated(hit);
                Game.instance.IncrementPlayerStat(PlayerStatType.MineHits, 1f, cheated);
                Game.instance.IncrementPlayerStat(PlayerStatType.Mines, 1f, cheated);
            }

            return true;
        }

        public static void Register(MineRock rock)
        {
            var nview = NView(rock);
            if (nview == null || !nview.IsValid()) return;

            nview.Register(RpcName, (System.Action<long>)(sender => UpdateVisability(rock)));
        }

        public static Vector3 Centre(MineRock rock, HitData hit, int hitAreaIndex)
        {
            var areas = Areas(rock);

            if (areas != null && hitAreaIndex >= 0 && hitAreaIndex < areas.Length && areas[hitAreaIndex] != null)
            {
                return areas[hitAreaIndex].bounds.center;
            }

            return hit != null ? hit.m_point : rock.transform.position;
        }

        private static bool AllDestroyed(MineRock rock)
        {
            var areas = Areas(rock);
            var zdo = NView(rock).GetZDO();
            var full = rock.GetHealth();

            for (var i = 0; i < areas.Length; i++)
            {
                if (zdo.GetFloat(Key(i), full) > 0f) return false;
            }

            return true;
        }

        private static string Key(int index) => "Health" + index;
    }
}
