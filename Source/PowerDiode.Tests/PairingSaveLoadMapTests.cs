using System.Text;
using System.Text.RegularExpressions;
using DevTools.Testing;

namespace PowerDiode.Tests;

// Saves diode buildings, despawns them and spawns the loaded copies the way a loaded map does,
// in the order they were saved.
[TestFixture(TestType.Playing)]
internal sealed class PairingSaveLoadMapTests
{
    private sealed class ThingsHarness : IExposable
    {
        public List<Thing> Things = [];

        public void ExposeData() => Scribe_Collections.Look(ref Things, "things", LookMode.Deep);
    }

    private readonly List<Thing> spawned = [];

    private static Map Map => Find.CurrentMap;

    private static IntVec3 Origin => Map.Center;

    [TearDown]
    public void DestroySpawned()
    {
        foreach (var thing in spawned)
        {
            if (!thing.Destroyed)
            {
                thing.Destroy();
            }
        }
        spawned.Clear();
    }

    private Building Spawn(string defName, IntVec3 cell)
    {
        var thing = (Building)
            GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName)),
                cell,
                Map
            );
        spawned.Add(thing);
        return thing;
    }

    private List<Thing> SaveAndReload(List<Thing> things, Func<string, string>? editSave = null)
    {
        using var memory = ScribeRoundTrip.Save(new ThingsHarness { Things = things });
        var cells = things.Select(thing => thing.Position).ToList();
        foreach (var thing in things)
        {
            thing.Destroy();
        }

        using var reader = new StreamReader(memory);
        var xml = reader.ReadToEnd();
        if (editSave != null)
        {
            xml = editSave(xml);
        }
        using var edited = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var loaded = new ThingsHarness();
        ScribeRoundTrip.Load(edited, loaded);

        for (var i = 0; i < loaded.Things.Count; i++)
        {
            spawned.Add(
                GenSpawn.Spawn(
                    loaded.Things[i],
                    cells[i],
                    Map,
                    Rot4.North,
                    respawningAfterLoad: true
                )
            );
        }
        return loaded.Things;
    }

    private static CompPowerDiodeDraw Draw(Thing thing) => thing.TryGetComp<CompPowerDiodeDraw>();

    private static CompPowerDiodeFeed Feed(Thing thing) => thing.TryGetComp<CompPowerDiodeFeed>();

    private static void ExpectPaired(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
    }

    // An intake and an outlet left side by side, both unpaired: the intake paired with another
    // outlet when it was built, and that outlet has since been uninstalled.
    private (Building Draw, Building Feed) SpawnStrandedNeighbours()
    {
        var draw = Spawn("PowerDiode_DrawNode", Origin);
        var formerFeed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var feed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.West);
        _ = formerFeed.MakeMinified();
        Expect.IsNull(Draw(draw).Partner);
        Expect.IsNull(Feed(feed).Partner);
        return (draw, feed);
    }

    [Test]
    public void PairedDiodesStayPaired()
    {
        var draw = Spawn("PowerDiode_DrawNode", Origin);
        var feed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);

        var loaded = SaveAndReload([draw, feed]);

        ExpectPaired(Draw(loaded[0]), Feed(loaded[1]));
    }

    [Test]
    public void UnpairedNeighboursStayUnpaired([Parameters(false, true)] bool intakeSavedFirst)
    {
        var (draw, feed) = SpawnStrandedNeighbours();

        var loaded = SaveAndReload(intakeSavedFirst ? [draw, feed] : [feed, draw]);

        var (loadedDraw, loadedFeed) = intakeSavedFirst
            ? (loaded[0], loaded[1])
            : (loaded[1], loaded[0]);
        Expect.IsNull(Draw(loadedDraw).Partner);
        Expect.IsNull(Feed(loadedFeed).Partner);
    }

    [Test]
    public void ManualLinkToTheLaterSpawnedOutletIsKept()
    {
        var (draw, westFeed) = SpawnStrandedNeighbours();
        var eastFeed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        Feed(eastFeed).Unlink();
        PowerDiodeLinking.LinkSpawned(Draw(draw), Feed(eastFeed));

        var loaded = SaveAndReload([westFeed, draw, eastFeed]);

        ExpectPaired(Draw(loaded[1]), Feed(loaded[2]));
        Expect.IsNull(Feed(loaded[0]).Partner);
    }

    [Test]
    public void SaveWithoutPairingDataPairsNeighboursOnLoad()
    {
        var draw = Spawn("PowerDiode_DrawNode", Origin);
        var feed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);

        var loaded = SaveAndReload(
            [draw, feed],
            xml =>
            {
                Expect.IsTrue(xml.Contains("<pairingSaved>", StringComparison.Ordinal));
                Expect.IsTrue(xml.Contains("<partner>", StringComparison.Ordinal));
                return Regex.Replace(
                    xml,
                    @"<pairingSaved>[^<]*</pairingSaved>|<partner>[^<]*</partner>",
                    ""
                );
            }
        );

        ExpectPaired(Draw(loaded[0]), Feed(loaded[1]));
    }
}
