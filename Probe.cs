#if DEBUG_PROBE
// Test-only (build with -p:DefineConstants=DEBUG_PROBE), called through the GameBridge `call`.
namespace HorseStoreRefresh;

public static class Probe
{
    public static string List()
    {
        var data = DataManager.i.data;
        var s = $"round={data.round}";
        var list = data.traderHorses;
        for (var i = 0; i < list.Count; i++)
            s += $" | {i}:{list[i].name} until={list[i].resevedUntil}";
        return s;
    }

    // Order the sales stable shows, plus the sort state.
    public static string Shown()
    {
        var sel = UnityEngine.Resources.FindObjectsOfTypeAll<Breeder>()[0].uiHorseSelectionSalesStable;
        var s = $"criteria={sel.selectedSortCriteria} desc={sel.sortDescending}";
        var list = sel.horseDatas;
        for (var i = 0; i < list.Count; i++)
            s += $" | {list[i].name}:{list[i].resevedUntil}";
        return s;
    }

    // Lets the button be pressed again on the same day.
    public static string ResetUsed()
    {
        RefreshState.ResetForTest();
        return "ok";
    }
}
#endif
