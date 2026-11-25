using System;
using HarmonyLib;

namespace HorseStoreRefresh;

// The game's pedigree finds grandparents only among horses the player once owned or possible sires, so a
// never-owned mare (a trader's horse that New horses kept as a past horse) shows as Unknown although she is
// in the save. After the game has drawn the pedigree, fill those slots from the full horse lists.
[HarmonyPatch(typeof(UiPedigree), nameof(UiPedigree.ShowPedigree))]
internal static class Pedigree
{
    // Slot layout of the game's 14 ancestor fields: a node's dam follows it, its sire comes after the dam's
    // subtree (0 dam, 1 dam's dam, 2/3 her parents, 4 dam's sire, 5/6 his parents, 7 sire, 8..13 likewise).
    private static readonly int[] DamSlot = { 1, 2, -1, -1, 5, -1, -1, 8, 9, -1, -1, 12, -1, -1 };
    private static readonly int[] SireSlot = { 4, 3, -1, -1, 6, -1, -1, 11, 10, -1, -1, 13, -1, -1 };

    private static void Postfix(UiPedigree __instance, HorseData horseToDisplay)
    {
        try
        {
            var horse = horseToDisplay;
            if (!Plugin.Enabled.Value || horse == null || DataManager.i?.data == null)
                return;
            var names = __instance.textAncestorNames;
            var portraits = __instance.ancestorPortraits;
            if (names == null || names.Length < 14)
                return;
            var data = DataManager.i.data;
            var filled = 0;
            var slots = new HorseData[14];
            slots[0] = Find(data, horse.uuidMother);
            slots[7] = Find(data, horse.uuidFather);
            for (var i = 0; i < 14; i++)
            {
                var h = slots[i];
                if (h == null)
                    continue;
                if (DamSlot[i] >= 0)
                    slots[DamSlot[i]] = Find(data, h.uuidMother);
                if (SireSlot[i] >= 0)
                    slots[SireSlot[i]] = Find(data, h.uuidFather);
                var text = names[i];
                if (text == null || text.text == h.name)
                    continue;
                text.text = h.name;
                var portrait = portraits != null && i < portraits.Length ? portraits[i] : null;
                if (portrait != null)
                {
                    var sprite = h.GetPortrait();
                    if (sprite != null)
                        portrait.sprite = sprite;
                }
                filled++;
            }
            if (filled > 0)
                Plugin.Logger.LogInfo($"Pedigree of {horse.name}: filled {filled} ancestor(s) the game showed as Unknown.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Filling the pedigree failed: {e}");
        }
    }

    private static HorseData Find(SaveGameData data, string uuid) =>
        string.IsNullOrEmpty(uuid) ? null : data.FindHorseInAllAllLists(uuid);
}
