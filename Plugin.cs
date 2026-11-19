using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace HorseStoreRefresh;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency(ModManagerGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    internal const string ModManagerGuid = "hoofprintbay.modmanager";
    public const string Guid = "hoofprintbay.horsestorerefresh";
    public const string Name = "Horse Store Refresh";
    public const string Version = "1.0.0";

    internal static ManualLogSource Logger;
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> ReservedSort;
    internal static ConfigEntry<int> RefreshesPerDay;
    internal const int MinPerDay = 1, MaxPerDay = 10;

    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("General", "Enabled", true, "Shows the \"New horses\" button in the horse trader's sales stable.");

        RefreshesPerDay = Config.Bind("General", "RefreshesPerDay", 1,
            new ConfigDescription("How many times a day the trader brings in new horses.", new AcceptableValueRange<int>(MinPerDay, MaxPerDay)));
        ReservedSort = Config.Bind("General", "ReservedSort", true, "Shows a \"Reserved\" sort button in the horse trader's sales stable (reserved horses first).");

        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(global::HorseStoreRefresh.ReservedSort));
        harmony.PatchAll(typeof(RefreshButton));
        harmony.PatchAll(typeof(RefreshState));
        if (IL2CPPChainloader.Instance.Plugins.ContainsKey(ModManagerGuid))
            ModManagerLink.Register();
        Log.LogInfo($"{Name} {Version} loaded.");
    }
}
