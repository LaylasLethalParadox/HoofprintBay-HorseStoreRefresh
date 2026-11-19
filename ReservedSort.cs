using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HorseStoreRefresh;

// "Reserved" sort button in the trader's sales stable, a copy of the "Pattern" sort button in the third
// column above "New horses". Reserved horses come first (the one whose hold ends soonest first);
// pressing it again turns the order round, like the game's own sort buttons.
[HarmonyPatch]
internal static class ReservedSort
{
    private const string ButtonName = "HorseStoreRefresh ReservedSort";
    // Not one of the game's criteria, so its own buttons start fresh after this one.
    private const int Criteria = 1001;
    private const string TooltipText = "Sort by reservation: reserved horses first. Click again for the other way round.";

    private static UiHorseSelection selection;
    private static Button button;

    internal static Transform Button => button != null && button.gameObject.activeSelf ? button.transform : null;

    // Runs before RefreshButton's postfix so that one can find this button when placing itself.
    [HarmonyPatch(typeof(Breeder), nameof(Breeder.OpenSalesStables))]
    [HarmonyPatch(typeof(Breeder), nameof(Breeder.OpenBuyHorse))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    private static void AfterOpenSalesStables(Breeder __instance)
    {
        try
        {
            selection = __instance.uiHorseSelectionSalesStable;
            if (button == null && Plugin.ReservedSort.Value)
                Create(__instance);
            if (button == null)
                return;
            button.gameObject.SetActive(Plugin.ReservedSort.Value);
            Place();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Adding the Reserved sort button failed: {e}");
        }
    }

    private static void Create(Breeder b)
    {
        var sorting = b.salesStable.transform.Find("CanvasSalesStable/UiSorting/Sorting");
        var pattern = sorting?.Find("ButtonSortPattern");
        if (pattern == null)
        {
            Plugin.Logger.LogWarning("Sales stable sort buttons not found; no Reserved sort button.");
            return;
        }
        var go = Object.Instantiate(pattern.gameObject, sorting.parent, false);
        go.name = ButtonName;
        foreach (var lu in go.GetComponentsInChildren<LanguageUpdater>(true))
            Object.Destroy(lu);
        button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(new Action(Click));
        var tip = go.GetComponent<Tooltip>();
        if (tip != null)
            tip.tooltipText = TooltipText;

        var text = go.transform.Find("TextTask")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = "Reserved";

        // The day calendar (top right of this screen) stands for "held for some days".
        var icon = go.transform.Find("IconTask")?.GetComponent<Image>();
        var calendar = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "ui-panels_calendar");
        if (icon != null && calendar != null)
        {
            icon.sprite = calendar;
            icon.preserveAspect = true;
        }
    }

    // Third column, top row.
    private static void Place()
    {
        var sorting = button.transform.parent.Find("Sorting");
        var age = sorting.Find("ButtonSortAge");
        var pattern = sorting.Find("ButtonSortPattern");
        var potential = sorting.Find("ButtonSortPotential");
        var step = pattern.position.x - age.position.x;
        var p = potential.position;
        button.transform.position = new Vector3(pattern.position.x + step, p.y, p.z);
    }

    // Public so the test bridge can press it.
    public static void Click()
    {
        try
        {
            if (selection == null || selection.horseDatas == null)
                return;
            var descending = selection.selectedSortCriteria != Criteria || !selection.sortDescending;
            selection.selectedSortCriteria = Criteria;
            selection.sortDescending = descending;

            var round = DataManager.i.data.round;
            var list = selection.horseDatas;
            var horses = new List<HorseData>();
            for (var i = 0; i < list.Count; i++)
                horses.Add(list[i]);
            // OrderBy is stable, so horses that tie keep the order they had.
            var sorted = horses.OrderBy(h => Key(h, round, descending)).ToList();
            list.Clear();
            foreach (var h in sorted)
                list.Add(h);
            selection.UpdateData();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Sorting by reservation failed: {e}");
        }
    }

    // Reserved first, soonest-ending hold first; turned round when pressed again.
    private static int Key(HorseData h, int round, bool reservedFirst)
    {
        var k = RefreshButton.IsReserved(h, round) ? h.resevedUntil : int.MaxValue;
        return reservedFirst ? k : (k == int.MaxValue ? int.MinValue : -k);
    }
}
