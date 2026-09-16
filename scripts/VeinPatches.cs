using HarmonyLib;

namespace BetterVeins.Scripts
{
    [HarmonyPatch]
    public static class VeinPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(MineRock5), "Awake")]
        private static void MineRock5_Awake_Postfix(MineRock5 __instance)
        {
            Vein.Register(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MineRock), "Start")]
        private static void MineRock_Start_Postfix(MineRock __instance)
        {
            VeinRock.Register(__instance);
        }

        // A deposit is damaged by messaging its owner, which on a server is rarely the player
        // swinging. The sweep reads settings and a held key that only exist on the player's own
        // machine, so the deposit is taken over before the swing lands.
        [HarmonyPrefix, HarmonyPatch(typeof(MineRock5), "Damage")]
        private static bool MineRock5_Damage_Prefix(MineRock5 __instance, HitData hit)
        {
            if (Vein.Sweeping) return true;
            if (!Allowed(hit)) return true;

            var nview = Vein.View(__instance);
            if (nview == null || !nview.IsValid()) return true;

            if (!NodeKinds.Wanted(nview, __instance.m_dropItems)) return true;
            if (Vein.Warded(__instance.transform.position)) return true;

            if (!nview.IsOwner()) nview.ClaimOwnership();

            if (!Plugin.breakOnFirstHit.Value) return true;

            if (!hit.CheckToolTier(__instance.m_minToolTier))
            {
                if (DamageText.instance != null)
                {
                    DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
                }

                return false;
            }

            var index = Vein.AreaIndexOf(__instance, hit.m_hitCollider);
            var point = Vein.Centre(__instance, hit, index);

            if (!Vein.Present(__instance, hit, point)) return false;

            Vein.Sweep(__instance, hit, index, alreadyPaid: 1);

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MineRock5), "DamageArea")]
        private static void MineRock5_DamageArea_Postfix(MineRock5 __instance, int hitAreaIndex, HitData hit,
            bool __result)
        {
            if (!__result) return;
            if (Vein.Sweeping) return;
            if (!Allowed(hit)) return;
            if (!Vein.AreaDead(__instance, hitAreaIndex)) return;

            var nview = Vein.View(__instance);
            if (nview == null || !nview.IsValid()) return;

            if (!NodeKinds.Wanted(nview, __instance.m_dropItems)) return;
            if (Vein.Warded(__instance.transform.position)) return;

            Vein.Sweep(__instance, hit, hitAreaIndex, alreadyPaid: 0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MineRock5), "CheckSupport")]
        private static bool MineRock5_CheckSupport_Prefix()
        {
            return !Vein.Sweeping;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MineRock), "Damage")]
        private static bool MineRock_Damage_Prefix(MineRock __instance, HitData hit)
        {
            if (VeinRock.Sweeping) return true;
            if (!Allowed(hit)) return true;

            var nview = VeinRock.View(__instance);
            if (nview == null || !nview.IsValid()) return true;

            if (!NodeKinds.Wanted(nview, __instance.m_dropItems)) return true;
            if (Vein.Warded(__instance.transform.position)) return true;

            if (!nview.IsOwner()) nview.ClaimOwnership();

            if (!Plugin.breakOnFirstHit.Value) return true;

            if (!hit.CheckToolTier(__instance.m_minToolTier))
            {
                if (DamageText.instance != null)
                {
                    DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
                }

                return false;
            }

            var index = VeinRock.AreaIndexOf(__instance, hit.m_hitCollider);
            var point = VeinRock.Centre(__instance, hit, index);

            if (!VeinRock.Present(__instance, hit, point)) return false;

            VeinRock.Sweep(__instance, hit, index, alreadyPaid: 1);

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MineRock), "RPC_Hit")]
        private static void MineRock_RPC_Hit_Postfix(MineRock __instance, HitData hit, int hitAreaIndex)
        {
            if (VeinRock.Sweeping) return;
            if (!Allowed(hit)) return;
            if (!VeinRock.AreaDead(__instance, hitAreaIndex)) return;

            var nview = VeinRock.View(__instance);
            if (nview == null || !nview.IsValid()) return;

            if (!NodeKinds.Wanted(nview, __instance.m_dropItems)) return;
            if (Vein.Warded(__instance.transform.position)) return;

            VeinRock.Sweep(__instance, hit, hitAreaIndex, alreadyPaid: 0);
        }

        private static bool Allowed(HitData hit)
        {
            if (!Plugin.veinMining.Value) return false;

            // Structural damage is the game settling an undermined deposit, not a player mining it.
            if (hit == null || hit.m_hitType == HitData.HitType.Structural) return false;

            if (hit.GetAttacker() != Player.m_localPlayer) return false;

            return !Vein.Suppressed();
        }
    }
}
