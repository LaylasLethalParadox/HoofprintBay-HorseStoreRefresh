using System;
using System.IO;
using HarmonyLib;

namespace HorseStoreRefresh;

// The day (SaveGameData.round) of the last refresh and how many were done that day, per save. Kept in
// memory and written to the save's folder only when the game saves, so quitting without saving undoes the
// refreshes and their uses together. File: "<day> <count>"; an older file holds only the day (= 1 use).
[HarmonyPatch]
internal static class RefreshState
{
    private const string FileName = "HorseStoreRefresh.txt"; // not save*.json: the game takes those for backups
    private const string NoSave = "NoSave";

    private static string loadedFor;   // save whose file was read
    private static int lastRound = -1; // -1 = never refreshed
    private static int count;          // refreshes done on lastRound
    private static bool dirty;

    private static string Save => DataManager.currentSaveDirectory;

    private static string PathFor(string save) => Path.Combine(DataManager.saveGamePath ?? "", save, FileName);

    private static void Ensure()
    {
        var save = Save;
        if (save == loadedFor)
            return;
        loadedFor = save;
        lastRound = -1;
        count = 0;
        dirty = false;
        if (string.IsNullOrEmpty(save) || save == NoSave)
            return;
        try
        {
            var path = PathFor(save);
            if (!File.Exists(path))
                return;
            var parts = File.ReadAllText(path).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[0], out var r))
            {
                lastRound = r;
                count = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 1;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Reading {FileName} failed: {e.Message}");
        }
    }

    public static int Today => DataManager.i?.data?.round ?? -1;

    public static int UsedToday
    {
        get
        {
            Ensure();
            return lastRound >= 0 && lastRound == Today ? count : 0;
        }
    }

    public static int LeftToday => Math.Max(0, Plugin.RefreshesPerDay.Value - UsedToday);

    public static void MarkUsed()
    {
        var used = UsedToday;
        lastRound = Today;
        count = used + 1;
        dirty = true;
    }

#if DEBUG_PROBE
    public static void ResetForTest()
    {
        Ensure();
        lastRound = -1;
        count = 0;
    }
#endif

    [HarmonyPatch(typeof(DataManager), nameof(DataManager.Save))]
    [HarmonyPostfix]
    private static void AfterSave()
    {
        try
        {
            if (!dirty || loadedFor != Save || string.IsNullOrEmpty(Save) || Save == NoSave)
                return;
            var folder = Path.Combine(DataManager.saveGamePath ?? "", Save);
            if (!Directory.Exists(folder))
                return;
            File.WriteAllText(PathFor(Save), $"{lastRound} {count}");
            dirty = false;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Writing {FileName} failed: {e}");
        }
    }

    [HarmonyPatch(typeof(DataManager), nameof(DataManager.LoadFromSaveFromFile))]
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.LoadSaveFromJsonOrBinary))]
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.LoadSaveFromProtectedFile))]
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.DeleteSaveFile))]
    [HarmonyPrefix]
    private static void BeforeLoadOrDelete() => loadedFor = null;
}
