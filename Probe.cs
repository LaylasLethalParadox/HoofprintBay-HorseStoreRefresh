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

    // Opens the game's own sell dialog for a horse (Breeder screen must be loaded).
    public static string Sell(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        UnityEngine.Resources.FindObjectsOfTypeAll<Breeder>()[0].SellHorseDialog(h);
        return "dialog";
    }

    // Every horse in the save with list, mother, father, offspring count, whereabouts and removedInRound.
    public static string All()
    {
        var d = DataManager.i.data;
        var s = $"round={d.round}";
        void L(Il2CppSystem.Collections.Generic.List<HorseData> l, string n)
        {
            s += $" || {n}({l?.Count}):";
            if (l != null) for (var i = 0; i < l.Count; i++)
                s += $" {l[i].name}#{l[i].uuid.Substring(0, 6)} m={Short(l[i].uuidMother)} f={Short(l[i].uuidFather)} off={l[i].uuidOffsprings?.Count} w={l[i].whereabouts} rm={l[i].removedInRound};";
        }
        L(d.horses, "horses"); L(d.foalsAndPregnancies, "foals"); L(d.boardedHorses, "boarded");
        L(d.pastHorses, "past"); L(d.traderHorses, "trader"); L(d.traderStuds, "studs");
        return s;
    }

    private static string Short(string u) => string.IsNullOrEmpty(u) ? "-" : u.Substring(0, System.Math.Min(6, u.Length));

    // Test-only: makes `child` an offspring of `mother` (memory only), to build a sold dam/daughter chain.
    public static string Link(string mother, string child)
    {
        var m = Find(mother); var c = Find(child);
        if (m == null || c == null) return "not found";
        c.uuidMother = m.uuid;
        m.uuidOffsprings.Add(c.uuid);
        return "linked";
    }

    // Test-only: moves a past horse into the trader's stock (as a sold horse is), memory only.
    public static string ToTrader(string name)
    {
        var d = DataManager.i.data;
        var h = Find(name);
        if (h == null) return "not found";
        for (var i = d.pastHorses.Count - 1; i >= 0; i--) if (d.pastHorses[i].uuid == h.uuid) d.pastHorses.RemoveAt(i);
        d.traderHorses.Add(h);
        return "moved";
    }

    // Opens a horse's own screen (Stable scene), where the pedigree UI is loaded.
    public static string OpenHorse(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        DataManager.i.currentlySelectedHorse = h;
        SceneSwitcher.i.SwitchSceneWithoutSaving("Stable");
        return "switching";
    }

    // Opens the game's own buy dialog for a horse for sale (sales stable must be open).
    public static string Buy(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        UnityEngine.Resources.FindObjectsOfTypeAll<Breeder>()[0].BuyHorseDialog(h);
        return "dialog";
    }

    // Which of the game's lookups find a horse (the pedigree may use a narrower one for grandparents).
    public static string Lookup(string name)
    {
        var h = Find(name);
        if (h == null) return "not found";
        var d = DataManager.i.data;
        return $"{h.name}: all={d.FindHorseInAllAllLists(h.uuid)?.name} everOwned={d.FindHorseInAllEverOwned(h.uuid)?.name} sires={d.FindHorseInAllPossibleSires(h.uuid)?.name} fullPedigree={h.HasFullPedigree()}";
    }
}
#endif
