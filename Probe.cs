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

    private static string Where(SaveGameData d, string uuid)
    {
        string In(Il2CppSystem.Collections.Generic.List<HorseData> l, string n)
        {
            if (l != null) for (var i = 0; i < l.Count; i++) if (l[i].uuid == uuid) return n;
            return null;
        }
        return In(d.horses, "horses") ?? In(d.traderHorses, "traderHorses") ?? In(d.traderStuds, "traderStuds")
            ?? In(d.pastHorses, "pastHorses") ?? In(d.boardedHorses, "boarded") ?? In(d.foalsAndPregnancies, "foals") ?? "NOWHERE";
    }

    // Possible sires and every pregnancy's father: where it is and whether the game's lookups find it.
    public static string Sires()
    {
        var d = DataManager.i.data;
        var s = "sires:";
        var all = d.GetListOfAllPossibleSires();
        for (var i = 0; i < all.Count; i++)
            s += $" {all[i].name}[{Where(d, all[i].uuid)}]";
        s += " | studs:";
        for (var i = 0; i < d.traderStuds.Count; i++)
            s += $" {d.traderStuds[i].name}={d.traderStuds[i].uuid.Substring(0, 8)}";
        s += " | foals:";
        var f = d.foalsAndPregnancies;
        for (var i = 0; i < f.Count; i++)
        {
            var u = f[i].uuidFather;
            var a = d.FindHorseInAllAllLists(u);
            var p = d.FindHorseInAllPossibleSires(u);
            s += $" {f[i].name}(age {f[i].age}) father={u?.Substring(0, System.Math.Min(8, u?.Length ?? 0))} in {Where(d, u)} all={a?.name} sires={p?.name};";
        }
        return s;
    }

    private static HorseData Find(string name)
    {
        var d = DataManager.i.data;
        foreach (var l in new[] { d.horses, d.traderHorses, d.traderStuds, d.pastHorses, d.foalsAndPregnancies })
            for (var i = 0; i < l.Count; i++) if (l[i].name == name) return l[i];
        return null;
    }

    // Selects a mare and a stallion in the open breeding station, then presses Breed.
    public static string Cover(string mare, string stallion)
    {
        var b = UnityEngine.Resources.FindObjectsOfTypeAll<Breeder>()[0];
        var m = Find(mare);
        var st = Find(stallion);
        if (m == null || st == null) return $"not found: mare={m?.name} stallion={st?.name}";
        b.SelectMareToCover(m);
        b.SelectStallion(st);
        b.Breed();
        return "breed pressed";
    }

    // In which lists a horse is, and how often (a duplicate would show as 2).
    public static string Count(string name)
    {
        var d = DataManager.i.data;
        var s = name + ":";
        void C(Il2CppSystem.Collections.Generic.List<HorseData> l, string n)
        {
            var k = 0;
            if (l != null) for (var i = 0; i < l.Count; i++) if (l[i].name == name) k++;
            if (k > 0) s += $" {n}={k}";
        }
        C(d.horses, "horses"); C(d.traderHorses, "traderHorses"); C(d.traderStuds, "traderStuds");
        C(d.pastHorses, "pastHorses"); C(d.boardedHorses, "boarded"); C(d.foalsAndPregnancies, "foals");
        return s;
    }

    // The game's own daily trader update, n times (control: what the game does with a sire it drops).
    public static string Rotate(string n)
    {
        var d = DataManager.i.data;
        for (var i = 0; i < int.Parse(n); i++) d.UpdateHorsesAvailableAtTrader();
        return $"rotated; pastHorses={d.pastHorses.Count} traderHorses={d.traderHorses.Count} studs={d.traderStuds.Count}";
    }

    public static string SetAge(string name, string age)
    {
        var h = Find(name);
        if (h == null) return "not found";
        h.age = int.Parse(age);
        return $"{h.name} age {h.age}";
    }

    // A horse's offspring list and where it is.
    public static string Info(string name)
    {
        var h = Find(name);
        var d = DataManager.i.data;
        if (h == null) return "not found";
        var s = $"{h.name} {h.uuid.Substring(0, 8)} in {Where(d, h.uuid)} whereabouts={h.whereabouts} offspring:";
        for (var i = 0; i < h.uuidOffsprings.Count; i++) s += " " + h.uuidOffsprings[i].Substring(0, 8);
        return s;
    }

    public static string Unreserve(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        h.resevedUntil = 0;
        return "ok";
    }

    public static string Save()
    {
        DataManager.i.Save(false);
        return "saved";
    }

    // Opens the game's pedigree for a horse or unborn foal (horse screen only).
    public static string Pedigree(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        var all = UnityEngine.Resources.FindObjectsOfTypeAll<UiPedigree>();
        if (all.Length == 0) return "no UiPedigree loaded";
        var ui = all[0];
        var t = ui.transform;
        while (t != null) { t.gameObject.SetActive(true); t = t.parent; }
        ui.ShowPedigree(h);
        var s = $"{ui.name}: own={ui.textOwnName?.text}";
        for (var i = 0; i < ui.textAncestorNames.Length; i++) s += $" | {i}:{ui.textAncestorNames[i]?.text}";
        return s;
    }

    // Takes a horse out of pastHorses (shows what a dropped sire looks like). Memory only.
    public static string DropPast(string name)
    {
        var l = DataManager.i.data.pastHorses;
        for (var i = 0; i < l.Count; i++) if (l[i].name == name) { l.RemoveAt(i); return "dropped"; }
        return "not found";
    }

    public static string Confirm(string n)
    {
        UnityEngine.Resources.FindObjectsOfTypeAll<Breeder>()[0].BreedConfirmed(int.Parse(n));
        return "ok";
    }

    // Lets the button be pressed again on the same day.
    public static string ResetUsed()
    {
        RefreshState.ResetForTest();
        return "ok";
    }
}
#endif
