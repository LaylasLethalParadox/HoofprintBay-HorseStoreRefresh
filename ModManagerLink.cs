using System.Runtime.CompilerServices;
using ModManager;

namespace HorseStoreRefresh;

// Our own page in the Mod Manager's window: the same switches as the config entries, but without
// the "Reset to defaults" button Mod Manager adds under config entries.
// Kept in its own class so ModManager.dll is only loaded when Mod Manager is installed.
internal static class ModManagerLink
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Register() => ModSettings.Register(Plugin.Guid, Build, showConfigEntries: false);

    private static void Build(ISettingsPage page)
    {
        page.Toggle("New horses button", () => Plugin.Enabled.Value, on => Plugin.Enabled.Value = on);
        page.Hint(Plugin.Enabled.Description.Description);
        page.Slider(n => $"New horses per day: {n}", Plugin.MinPerDay, Plugin.MaxPerDay,
            () => Plugin.RefreshesPerDay.Value, n => Plugin.RefreshesPerDay.Value = n,
            defaultValue: 1, visible: () => Plugin.Enabled.Value);
        page.Hint(Plugin.RefreshesPerDay.Description.Description, visible: () => Plugin.Enabled.Value);
        page.Toggle("Reserved sort button", () => Plugin.ReservedSort.Value, on => Plugin.ReservedSort.Value = on);
        page.Hint(Plugin.ReservedSort.Description.Description);
    }
}
