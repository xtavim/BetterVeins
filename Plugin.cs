using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace BetterVeins
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> veinMining;
        public static ConfigEntry<bool> breakOnFirstHit;
        public static ConfigEntry<KeyboardShortcut> suppressKey;

        public static ConfigEntry<bool> oreDeposits;
        public static ConfigEntry<bool> scrapPiles;
        public static ConfigEntry<bool> rockFormations;

        public static ConfigEntry<float> durabilityPerArea;
        public static ConfigEntry<float> staminaPerArea;

        public static ConfigEntry<bool> mergeDrops;
        public static ConfigEntry<bool> overstackDrops;

        public static ConfigEntry<bool> debugMode;

        public new static readonly ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

        private static readonly ConfigSync configSync = new(PluginInfo.PLUGIN_GUID)
        {
            DisplayName = PluginInfo.PLUGIN_NAME,
            CurrentVersion = PluginInfo.PLUGIN_VERSION,
            MinimumRequiredVersion = PluginInfo.PLUGIN_VERSION
        };

        private void Awake()
        {
            InitializeConfig();
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_VERSION} loaded");
        }

        private ConfigEntry<T> ConfigSync<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            var configDescription = new ConfigDescription(
                description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            configSync.AddConfigEntry(configEntry).SynchronizedConfig = synchronizedSetting;
            return configEntry;
        }

        private void InitializeConfig()
        {
            Config.SaveOnConfigSet = false;

            var serverConfigLocked = ConfigSync("1 - ServerSync", "Lock Configuration", true,
                new ConfigDescription(
                    "If enabled, the configuration is locked and can be changed by server admins only."));
            configSync.AddLockingConfigEntry(serverConfigLocked);

            veinMining = ConfigSync("General", "Enable", true,
                new ConfigDescription(
                    "Break a whole deposit at once instead of one chunk at a time."));

            breakOnFirstHit = ConfigSync("General", "Break On First Hit", false,
                new ConfigDescription(
                    "Bring the whole deposit down on the first swing that connects, instead of waiting for one chunk to come off. Your pickaxe still has to be good enough for the ore, but every pickaxe that is good enough works the same, so the upgrade stops meaning anything. Off by default for that reason."));

            suppressKey = Config.Bind("General", "Mine One Chunk", new KeyboardShortcut(KeyCode.LeftAlt),
                new ConfigDescription(
                    "Hold this to mine a single chunk, the way the game does. For trimming a rock you are building around, or taking two stone instead of two hundred."));

            oreDeposits = ConfigSync("Nodes", "Ore Deposits", true,
                new ConfigDescription(
                    "Copper, tin, silver, obsidian and anything else that gives metal."));

            scrapPiles = ConfigSync("Nodes", "Scrap Piles", true,
                new ConfigDescription(
                    "Muddy scrap piles in the swamp crypts."));

            rockFormations = ConfigSync("Nodes", "Rock And Stone", true,
                new ConfigDescription(
                    "Plain rock, which gives stone. Turn this off if you shape rock by hand, to build into a cliff or trim a boulder, since taking the whole thing is rarely what you want there."));

            durabilityPerArea = ConfigSync("Cost", "Durability Per Chunk", 1f,
                new ConfigDescription(
                    "How much the pickaxe wears for each chunk that comes off, as a multiple of a normal swing. 1 charges the full cost of every chunk, 0.25 charges a quarter, 0 makes the whole deposit free. A pickaxe that runs out partway still finishes the deposit and breaks afterwards.",
                    new AcceptableValueRange<float>(0f, 2f)));

            staminaPerArea = ConfigSync("Cost", "Stamina Per Chunk", 1f,
                new ConfigDescription(
                    "How much stamina each extra chunk costs, as a multiple of a normal swing. 0 makes the whole deposit free. Running out never stops a deposit halfway: you are simply emptied.",
                    new AcceptableValueRange<float>(0f, 2f)));

            mergeDrops = ConfigSync("Drops", "Merge Drops", true,
                new ConfigDescription(
                    "Drop one pile where you swung instead of scattering a stack for every chunk. What the deposit gives is unchanged: every chunk is still rolled for, the results are just added up first."));

            overstackDrops = ConfigSync("Drops", "Overstack Drops", false,
                new ConfigDescription(
                    "Put everything into a single stack even when that is past what the item normally stacks to, so 200 stone is one pile rather than four. Off by default: an oversized stack does not fit an inventory slot cleanly, and one left on the ground stays oversized if you remove this mod."));

            debugMode = ConfigSync("Debug", "Debug Mode", false,
                new ConfigDescription(
                    "Log what is being broken and what it dropped."), false);

            Config.SaveOnConfigSet = true;
            Config.Save();
        }
    }
}
