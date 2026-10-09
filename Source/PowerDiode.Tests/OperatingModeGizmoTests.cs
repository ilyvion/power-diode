using DevTools.Testing;

namespace PowerDiode.Tests;

// Playing, since opening the mode menu needs a window stack and a camera to play its sound on.
[TestFixture(TestType.Playing)]
internal sealed class OperatingModeGizmoTests
{
    private float originalMinWattage;
    private float originalMaxWattage;
    private float originalMinReserve;
    private float originalMaxReserve;
    private bool originalReserveIsPercentage;

    // Every slider is shown unless its settings range is empty.
    [SetUp]
    public void ApplySettings()
    {
        var settings = PowerDiodeMod.Settings;
        originalMinWattage = settings.MinWattage;
        originalMaxWattage = settings.MaxWattage;
        originalMinReserve = settings.MinReserveWattDays;
        originalMaxReserve = settings.MaxReserveWattDays;
        originalReserveIsPercentage = settings.ReserveIsPercentage;
        settings.MinWattage = 0f;
        settings.MaxWattage = 5000f;
        settings.MinReserveWattDays = 10f;
        settings.MaxReserveWattDays = 500f;
        settings.ReserveIsPercentage = false;
    }

    [TearDown]
    public void RestoreSettings()
    {
        var settings = PowerDiodeMod.Settings;
        settings.MinWattage = originalMinWattage;
        settings.MaxWattage = originalMaxWattage;
        settings.MinReserveWattDays = originalMinReserve;
        settings.MaxReserveWattDays = originalMaxReserve;
        settings.ReserveIsPercentage = originalReserveIsPercentage;
        _ = Find.WindowStack.TryRemove(typeof(FloatMenu), doCloseSound: false);
    }

    // Built without PostMake/ID, as in CompPowerDiodeFeedSettingsTests, and never spawned. The
    // feed gets a partner so CompGetGizmosExtra skips the link gizmo, which needs a map position.
    private static CompPowerDiodeFeed MakePairedFeedComp()
    {
        var feed = MakeBuilding("PowerDiode_FeedNode").GetComp<CompPowerDiodeFeed>();
        feed.Partner = MakeBuilding("PowerDiode_DrawNode").GetComp<CompPowerDiodeDraw>();
        return feed;
    }

    private static Building MakeBuilding(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building;
    }

    private static Command_SetDiodeOperatingMode? ModeGizmo(CompPowerDiodeFeed feed) =>
        feed.CompGetGizmosExtra()
            .OfType<Command_SetDiodeOperatingMode>()
            .FirstOrDefault(gizmo =>
                gizmo.defaultLabel
                == "PowerDiode.OperatingModeGizmoLabel".Translate(feed.OperatingMode.Label())
            );

    private static FloatMenu? OpenModeMenu(Command_SetDiodeOperatingMode modeGizmo)
    {
        modeGizmo.ProcessInput(new Event());
        return Find.WindowStack.WindowOfType<FloatMenu>();
    }

    private static readonly PowerDiodeOperatingMode[] AllModes =
    [
        .. Enum.GetValues(typeof(PowerDiodeOperatingMode)).Cast<PowerDiodeOperatingMode>(),
    ];

    [Test]
    public static void OnlyTheModesThresholdSliderIsShownAlongsideTheWattageSlider(
        [Parameters(
            PowerDiodeOperatingMode.OneWayValve,
            PowerDiodeOperatingMode.Overflow,
            PowerDiodeOperatingMode.TopUp
        )]
            PowerDiodeOperatingMode mode
    )
    {
        var feed = MakePairedFeedComp();
        feed.OperatingMode = mode;

        var gizmos = feed.CompGetGizmosExtra().ToList();

        Expect.AreEqual(
            mode == PowerDiodeOperatingMode.OneWayValve ? 1 : 0,
            gizmos.OfType<Gizmo_SetDiodeReserve>().Count(),
            "reserve slider"
        );
        Expect.AreEqual(
            mode == PowerDiodeOperatingMode.Overflow ? 1 : 0,
            gizmos.OfType<Gizmo_SetDiodeOverflowThreshold>().Count(),
            "overflow slider"
        );
        Expect.AreEqual(
            mode == PowerDiodeOperatingMode.TopUp ? 1 : 0,
            gizmos.OfType<Gizmo_SetDiodeTopUpThreshold>().Count(),
            "top-up slider"
        );
        Expect.AreEqual(1, gizmos.OfType<Gizmo_SetDiodeWattage>().Count(), "wattage slider");
        Expect.IsNotNull(ModeGizmo(feed), "mode gizmo");
    }

    [Test]
    public static void ModeMenuOffersEveryModeAndSetsTheChosenOne(
        [Parameters(
            PowerDiodeOperatingMode.OneWayValve,
            PowerDiodeOperatingMode.Overflow,
            PowerDiodeOperatingMode.TopUp
        )]
            PowerDiodeOperatingMode chosenMode
    )
    {
        var feed = MakePairedFeedComp();
        feed.OperatingMode = AllModes.First(mode => mode != chosenMode);

        var modeGizmo = ModeGizmo(feed);
        Expect.IsNotNull(modeGizmo, "mode gizmo");
        if (modeGizmo == null)
        {
            return;
        }

        var menu = OpenModeMenu(modeGizmo);
        Expect.IsNotNull(menu, "mode menu");
        if (menu == null)
        {
            return;
        }
        Expect.AreEqual(AllModes.Length, menu.options.Count);
        foreach (var mode in AllModes)
        {
            Expect.AreEqual(
                1,
                menu.options.Count(option => option.Label == mode.Label()),
                mode.ToString()
            );
        }

        menu.options.First(option => option.Label == chosenMode.Label()).action();

        Expect.AreEqual(chosenMode, feed.OperatingMode);
    }

    // Outlets in the same mode share one grouped gizmo; the gizmo grid hands the clicked gizmo
    // the rest of its group through InheritInteractionsFrom.
    [Test]
    public static void GroupedModeMenuSetsTheChosenModeOnEverySelectedOutlet()
    {
        var feeds = new[] { MakePairedFeedComp(), MakePairedFeedComp(), MakePairedFeedComp() };
        var gizmos = feeds.Select(ModeGizmo).OfType<Command_SetDiodeOperatingMode>().ToList();
        Expect.AreEqual(feeds.Length, gizmos.Count, "mode gizmos");
        if (gizmos.Count != feeds.Length)
        {
            return;
        }
        var clicked = gizmos[0];
        foreach (var other in gizmos.Skip(1))
        {
            Expect.IsTrue(clicked.GroupsWith(other), "gizmos group");
            Expect.IsFalse(clicked.InheritInteractionsFrom(other), "others don't open menus");
        }

        var menu = OpenModeMenu(clicked);
        Expect.IsNotNull(menu, "mode menu");
        if (menu == null)
        {
            return;
        }
        menu.options.First(option => option.Label == PowerDiodeOperatingMode.TopUp.Label())
            .action();

        foreach (var feed in feeds)
        {
            Expect.AreEqual(PowerDiodeOperatingMode.TopUp, feed.OperatingMode);
        }
    }
}
