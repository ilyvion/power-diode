namespace PowerDiode;

internal static class PowerDiodeLinking
{
    internal static void TryLinkFeedNode(CompPowerDiodeFeed feed)
    {
        // Only unpaired intakes are candidates, so dev-mode placement, which bypasses
        // PlaceWorker checks, can never double-pair an intake.
        var draw = FindDrawNodeToLinkTo(feed.parent.Position, feed.parent.Map);
        if (draw != null)
        {
            Link(draw, feed);
        }
    }

    internal static void TryLinkDrawNode(CompPowerDiodeDraw draw)
    {
        var feed = FindFeedNodeToLinkTo(draw.parent.Position, draw.parent.Map);
        if (feed != null)
        {
            Link(draw, feed);
        }
    }

    private static void Link(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        draw.Partner = feed;
        feed.Partner = draw;
    }

    // The building a diode of the given def would pair with if it spawned at cell. When
    // reinstalling, movingThing is the building being moved, which stays paired with its current
    // partner until it's uninstalled, so that partner counts as unpaired.
    internal static Thing? FindPartnerToLinkTo(
        ThingDef def,
        IntVec3 cell,
        Map map,
        Thing? movingThing = null
    ) =>
        def.HasComp<CompPowerDiodeDraw>() ? FindFeedNodeToLinkTo(cell, map, movingThing)?.parent
        : def.HasComp<CompPowerDiodeFeed>() ? FindDrawNodeToLinkTo(cell, map, movingThing)?.parent
        : null;

    internal static CompPowerDiodeDraw? FindDrawNodeToLinkTo(
        IntVec3 cell,
        Map map,
        Thing? movingThing = null
    ) =>
        FindAdjacent<CompPowerDiodeDraw>(
            cell,
            map,
            d => d.Partner == null || d.Partner.parent == movingThing
        );

    internal static CompPowerDiodeFeed? FindFeedNodeToLinkTo(
        IntVec3 cell,
        Map map,
        Thing? movingThing = null
    ) =>
        FindAdjacent<CompPowerDiodeFeed>(
            cell,
            map,
            f => f.Partner == null || f.Partner.parent == movingThing
        );

    internal static List<CompPowerDiodeFeed> UnpairedAdjacentFeedNodes(CompPowerDiodeDraw draw) =>
        [
            .. AllAdjacent<CompPowerDiodeFeed>(
                draw.parent.Position,
                draw.parent.Map,
                f => f.Partner == null
            ),
        ];

    internal static List<CompPowerDiodeDraw> UnpairedAdjacentDrawNodes(CompPowerDiodeFeed feed) =>
        [
            .. AllAdjacent<CompPowerDiodeDraw>(
                feed.parent.Position,
                feed.parent.Map,
                d => d.Partner == null
            ),
        ];

    // Links two already-spawned diode buildings. Unpaired neighbours share one power net, so the
    // nets around the outlet are rebuilt for the new link to split them apart.
    internal static void LinkSpawned(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        if (draw.Partner != null || feed.Partner != null)
        {
            return;
        }
        Link(draw, feed);
        feed.parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(
            feed.PowerTrader
        );
    }

    private static T? FindAdjacent<T>(IntVec3 center, Map map, Predicate<T> validator)
        where T : ThingComp => AllAdjacent(center, map, validator).FirstOrDefault();

    private static IEnumerable<T> AllAdjacent<T>(IntVec3 center, Map map, Predicate<T> validator)
        where T : ThingComp
    {
        foreach (var dir in GenAdj.CardinalDirections)
        {
            var cell = center + dir;
            if (!cell.InBounds(map))
            {
                continue;
            }
            foreach (var candidate in cell.GetThingList(map))
            {
                if (candidate is ThingWithComps thingWithComps)
                {
                    var comp = thingWithComps.GetComp<T>();
                    if (comp != null && validator(comp))
                    {
                        yield return comp;
                    }
                }
            }
        }
    }
}
