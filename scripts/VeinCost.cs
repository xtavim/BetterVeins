namespace BetterVeins.Scripts
{
    public static class VeinCost
    {
        public static void Charge(HitData hit, int chunks)
        {
            if (chunks <= 0) return;

            if (hit?.GetAttacker() is not Player player || !player) return;

            Stamina(player, chunks);
            Durability(player, chunks);
        }

        private static void Stamina(Player player, int chunks)
        {
            var rate = Plugin.staminaPerArea.Value;
            if (rate <= 0f) return;

            var weapon = player.GetCurrentWeapon();
            if (weapon == null) return;

            var attack = weapon.m_shared.m_attack;
            if (attack == null || attack.m_attackStamina <= 0f) return;

            player.UseStamina(attack.m_attackStamina * rate * chunks);
        }

        private static void Durability(Player player, int chunks)
        {
            var rate = Plugin.durabilityPerArea.Value;
            if (rate <= 0f) return;

            var weapon = player.GetCurrentWeapon();
            if (weapon == null || !weapon.m_shared.m_useDurability) return;

            weapon.m_durability -= weapon.m_shared.m_useDurabilityDrain * rate * chunks;

            if (weapon.m_durability < 0f) weapon.m_durability = 0f;
        }
    }
}
