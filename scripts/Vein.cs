using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterVeins.Scripts
{
    public static class Vein
    {
        private static readonly AccessTools.FieldRef<MineRock5, List<MineRock5.HitArea>> Areas =
            AccessTools.FieldRefAccess<MineRock5, List<MineRock5.HitArea>>("m_hitAreas");

        private static readonly AccessTools.FieldRef<MineRock5, ZNetView> NView =
            AccessTools.FieldRefAccess<MineRock5, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<MineRock5.HitArea, float> Health =
            AccessTools.FieldRefAccess<MineRock5.HitArea, float>("m_health");

        private static readonly AccessTools.FieldRef<MineRock5.HitArea, Collider> AreaCollider =
            AccessTools.FieldRefAccess<MineRock5.HitArea, Collider>("m_collider");

        private static readonly System.Action<MineRock5> SaveHealth =
            AccessTools.MethodDelegate<System.Action<MineRock5>>(
                AccessTools.Method(typeof(MineRock5), "SaveHealth"));

        private static readonly System.Func<MineRock5, Collider, int> GetAreaIndex =
            AccessTools.MethodDelegate<System.Func<MineRock5, Collider, int>>(
                AccessTools.Method(typeof(MineRock5), "GetAreaIndex"));

        public static bool Sweeping;

        public static ZNetView View(MineRock5 rock) => NView(rock);

        public static bool Suppressed()
        {
            var key = Plugin.suppressKey.Value;
            return key.MainKey != KeyCode.None && Input.GetKey(key.MainKey);
        }

        // Inside someone else's ward the sweep stands down and the swing is left to the game, so a
        // deposit in a stranger's base is still mined one chunk at a time. Your own ward grants access
        // and changes nothing.
        public static bool Warded(Vector3 position)
        {
            return !PrivateArea.CheckAccess(position, 0f, false, true);
        }

        public static int AreaIndexOf(MineRock5 rock, Collider collider)
        {
            return collider == null ? -1 : GetAreaIndex(rock, collider);
        }

        public static bool AreaDead(MineRock5 rock, int index)
        {
            var areas = Areas(rock);
            if (areas == null || index < 0 || index >= areas.Count) return false;

            return Health(areas[index]) <= 0f;
        }

        public static void Sweep(MineRock5 rock, HitData hit, int hitAreaIndex, int alreadyPaid)
        {
            var areas = Areas(rock);
            var nview = NView(rock);

            if (areas == null || nview == null || !nview.IsValid() || !nview.IsOwner()) return;

            var centre = Centre(rock, hit, hitAreaIndex);

            Sweeping = true;
            try
            {
                var broken = 0;

                for (var i = 0; i < areas.Count; i++)
                {
                    if (Health(areas[i]) <= 0f) continue;

                    Health(areas[i]) = 0f;
                    broken++;
                }

                if (broken == 0) return;

                SaveHealth(rock);

                VeinDrops.Spawn(rock.m_dropItems, broken, centre, Cheated(hit));
                VeinCost.Charge(hit, broken - alreadyPaid);

                rock.m_destroyedEffect.Create(centre, Quaternion.identity);

                if (rock.m_triggerPrivateArea && hit?.GetAttacker() != null)
                {
                    PrivateArea.OnObjectDamaged(rock.transform.position, hit.GetAttacker(), true);
                }

                // Every chunk that was standing is now at zero, so there is never anything left to
                // show. The deposit goes, and every other player sees it go through the same path
                // the game uses for a deposit mined by hand.
                nview.Destroy();

                if (Plugin.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"Vein: broke {broken} chunks off {rock.name} at {centre}");
                }
            }
            finally
            {
                Sweeping = false;
            }
        }

        // Everything the game would have shown for the swing, for the mode that takes the deposit
        // before the game gets a look at it. The damage itself is never invented: the swing's own
        // figure is run through the deposit's resistances and shown as it is.
        public static bool Present(MineRock5 rock, HitData hit, Vector3 point)
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
                var cheated = Cheated(hit);
                Game.instance.IncrementPlayerStat(PlayerStatType.MineHits, 1f, cheated);
                Game.instance.IncrementPlayerStat(PlayerStatType.Mines, 1f, cheated);
            }

            return true;
        }

        public static Vector3 Centre(MineRock5 rock, HitData hit, int hitAreaIndex)
        {
            var areas = Areas(rock);

            if (areas != null && hitAreaIndex >= 0 && hitAreaIndex < areas.Count)
            {
                var collider = AreaCollider(areas[hitAreaIndex]);
                if (collider != null) return collider.bounds.center;
            }

            return hit != null ? hit.m_point : rock.transform.position;
        }

        public static bool Cheated(HitData hit)
        {
            return hit?.GetAttacker() is Player player && player
                   && player.GetInventory().CheatedDamagingItemEquipped();
        }
    }
}
