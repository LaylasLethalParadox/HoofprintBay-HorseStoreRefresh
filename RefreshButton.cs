using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HorseStoreRefresh;

// "New horses" button in the trader's sales stable, a copy of the "Pattern" sort button placed as a third
// column next to the sort buttons. It lets the game draw a new set of horses for sale, once per day.
[HarmonyPatch]
internal static class RefreshButton
{
    private const string ButtonName = "HorseStoreRefresh Button";
    // The copied Tooltip shows its text as it is (its LanguageUpdater, which translated it, is removed).
    private const string TooltipText = "Ask the trader to bring in other horses. {0} more {1} today.";
    private const string TooltipUsedText = "The trader has brought in all the new horses for today. Come back tomorrow.";

    private static Breeder breeder;
    private static Button button;
    private static Image icon;

    [HarmonyPatch(typeof(Breeder), nameof(Breeder.OpenSalesStables))]
    [HarmonyPatch(typeof(Breeder), nameof(Breeder.OpenBuyHorse))]
    [HarmonyPostfix]
    private static void AfterOpenSalesStables(Breeder __instance)
    {
        try
        {
            breeder = __instance;
            if (button == null && Plugin.Enabled.Value)
                Create(__instance);
            if (button == null)
                return;
            button.gameObject.SetActive(Plugin.Enabled.Value);
            Place();
            UpdateState();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Adding the New horses button failed: {e}");
        }
    }

    // The game's own (debug) function draws the new horses; a game update could rename or drop it.
    private static readonly bool CanRegenerate =
        AccessTools.Method(typeof(Breeder), nameof(Breeder.DEBUG_RegenerateSalesHorses)) != null;

    private static void Create(Breeder b)
    {
        if (!CanRegenerate)
        {
            Plugin.Logger.LogWarning("The game no longer has Breeder.DEBUG_RegenerateSalesHorses; no New horses button. This mod needs an update.");
            return;
        }
        var sorting = b.salesStable.transform.Find("CanvasSalesStable/UiSorting/Sorting");
        var pattern = sorting?.Find("ButtonSortPattern");
        if (pattern == null)
        {
            Plugin.Logger.LogWarning("Sales stable sort buttons not found; no New horses button.");
            return;
        }
        // Sibling of the grid, not a child, so the grid's layout leaves it alone.
        var go = Object.Instantiate(pattern.gameObject, sorting.parent, false);
        go.name = ButtonName;
        foreach (var lu in go.GetComponentsInChildren<LanguageUpdater>(true))
            Object.Destroy(lu);
        button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(new Action(Click));

        var text = go.transform.Find("TextTask")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = "New horses";

        // The Back button's curved arrow reads as "refresh"; it is loaded on this screen.
        var iconT = go.transform.Find("IconTask");
        icon = iconT?.GetComponent<Image>();
        var back = b.salesStable.transform.Find("CanvasSalesStable/ButtonBack/BackIcon")?.GetComponent<Image>();
        if (icon != null && back != null)
        {
            icon.sprite = back.sprite;
            icon.preserveAspect = true;
        }
    }

    // Right of the Reserved sort button on the top row; without it, third column between the two rows.
    private static void Place()
    {
        var sorting = button.transform.parent.Find("Sorting");
        var age = sorting.Find("ButtonSortAge");
        var pattern = sorting.Find("ButtonSortPattern");
        var potential = sorting.Find("ButtonSortPotential");
        var p = pattern.position;
        var step = p.x - age.position.x;
        button.transform.position = ReservedSort.Button != null
            ? new Vector3(p.x + 2 * step, potential.position.y, p.z)
            : new Vector3(p.x + step, (p.y + potential.position.y) / 2f, p.z);
    }

    private static void UpdateState()
    {
        var left = RefreshState.LeftToday;
        var used = left <= 0;
        button.interactable = !used;
        var tip = button.GetComponent<Tooltip>();
        if (tip != null)
            tip.tooltipText = used ? TooltipUsedText : string.Format(TooltipText, left, left == 1 ? "time" : "times");
        if (icon != null)
            icon.color = used ? new Color(1, 1, 1, 0.5f) : Color.white;
    }

    // Public so the test bridge can press it.
    public static void Click()
    {
        try
        {
            if (breeder == null || RefreshState.LeftToday <= 0)
                return;
            var data = DataManager.i.data;
            var before = Names(data.traderHorses);
            var reserved = Reserved(data);
            var parents = Parents(data);
            Regenerate(breeder);
            KeepReserved(data, reserved);
            KeepParents(data, parents);
            RefreshState.MarkUsed();
            Plugin.Logger.LogInfo($"Day {data.round}: horses for sale were [{before}], now [{Names(data.traderHorses)}]; studs [{Names(data.traderStuds)}].");
            breeder.OpenBuyHorse();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"New horses failed: {e}");
        }
    }

    // Own method, so a missing game method fails here, inside Click's try, not when Click is compiled.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Regenerate(Breeder b) => b.DEBUG_RegenerateSalesHorses();

    // A reserved horse has resevedUntil (the game's spelling) at today or later.
    internal static bool IsReserved(HorseData h, int round) => h != null && h.resevedUntil > 0 && h.resevedUntil >= round;

    private static List<HorseData> Reserved(SaveGameData data)
    {
        var kept = new List<HorseData>();
        var list = data.traderHorses;
        if (list == null)
            return kept;
        for (var i = 0; i < list.Count; i++)
            if (IsReserved(list[i], data.round))
                kept.Add(list[i]);
        return kept;
    }

    // The regenerated list holds only new horses; the reserved ones go back at the front (where the
    // trader showed them) in place of the last new ones, so the stable keeps its size.
    private static void KeepReserved(SaveGameData data, List<HorseData> reserved)
    {
        if (reserved.Count == 0)
            return;
        var list = data.traderHorses;
        if (list == null)
        {
            list = new Il2CppSystem.Collections.Generic.List<HorseData>();
            data.traderHorses = list;
        }
        var size = Math.Max(list.Count, reserved.Count);
        for (var i = reserved.Count - 1; i >= 0; i--)
            list.Insert(0, reserved[i]);
        while (list.Count > size)
            list.RemoveAt(list.Count - 1);
        Plugin.Logger.LogInfo($"Kept reserved: [{string.Join(", ", reserved.Select(h => h.name))}].");
    }

    // Horses for sale (also ones the player sold, which go to the trader) and studs before the refresh.
    // The regeneration moves studs with foals to pastHorses itself but simply drops horses for sale, so a
    // foal's pedigree showed its sire or dam (and everything above) as Unknown.
    private static List<HorseData> Parents(SaveGameData data)
    {
        var found = new List<HorseData>();
        foreach (var list in new[] { data.traderHorses, data.traderStuds })
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        found.Add(list[i]);
        return found;
    }

    // The game's own rotation keeps every horse the player once owned (whereabouts SOLD, EXPORTED, ...) in
    // pastHorses, and so a horse with offspring. Do the same, and also keep a dropped horse that a horse still
    // in the save names as mother/father. Repeated, so a never-owned dam of a kept daughter stays as well.
    private static void KeepParents(SaveGameData data, List<HorseData> before)
    {
        var known = new HashSet<string>();
        var parentRefs = new HashSet<string>();
        void Add(HorseData h)
        {
            known.Add(h.uuid);
            if (!string.IsNullOrEmpty(h.uuidMother)) parentRefs.Add(h.uuidMother);
            if (!string.IsNullOrEmpty(h.uuidFather)) parentRefs.Add(h.uuidFather);
        }
        foreach (var list in new[] { data.horses, data.foalsAndPregnancies, data.boardedHorses, data.pastHorses, data.traderHorses, data.traderStuds })
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        Add(list[i]);
        var dropped = before.Where(h => !known.Contains(h.uuid)).ToList();
        var moved = new List<string>();
        bool changed;
        do
        {
            changed = false;
            foreach (var h in dropped)
            {
                if (known.Contains(h.uuid))
                    continue;
                if (h.whereabouts == WhereaboutsType.NEVER_OWNED && !parentRefs.Contains(h.uuid) && !HasKnownOffspring(h, known))
                    continue;
                if (data.pastHorses == null)
                    data.pastHorses = new Il2CppSystem.Collections.Generic.List<HorseData>();
                data.pastHorses.Add(h);
                Add(h);
                moved.Add(h.name);
                changed = true;
            }
        } while (changed);
        if (moved.Count > 0)
            Plugin.Logger.LogInfo($"Kept as past horses (once owned or parents): [{string.Join(", ", moved)}].");
    }

    private static bool HasKnownOffspring(HorseData h, HashSet<string> known)
    {
        if (h.uuidOffsprings == null)
            return false;
        for (var i = 0; i < h.uuidOffsprings.Count; i++)
            if (known.Contains(h.uuidOffsprings[i]))
                return true;
        return false;
    }

    private static string Names(Il2CppSystem.Collections.Generic.List<HorseData> list)
    {
        if (list == null)
            return "null";
        var names = new string[list.Count];
        for (var i = 0; i < list.Count; i++)
            names[i] = list[i]?.name;
        return string.Join(", ", names);
    }
}
