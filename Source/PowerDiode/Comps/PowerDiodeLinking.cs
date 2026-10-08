namespace PowerDiode;

internal static class PowerDiodeLinking
{
    internal static void TryLinkFeedNode(CompPowerDiodeFeed feed)
    {
        // Only unpaired intakes are candidates, so dev-mode placement, which bypasses
        // PlaceWorker checks, can never double-pair an intake.
        var draw = FindAdjacent<CompPowerDiodeDraw>(feed.parent, d => d.Partner == null);
        if (draw != null)
        {
            Link(draw, feed);
        }
    }

    internal static void TryLinkDrawNode(CompPowerDiodeDraw draw)
    {
        var feed = FindAdjacent<CompPowerDiodeFeed>(draw.parent, f => f.Partner == null);
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

    internal static T? FindAdjacent<T>(Thing thing, Predicate<T> validator)
        where T : ThingComp
    {
        var map = thing.Map;
        foreach (var dir in GenAdj.CardinalDirections)
        {
            var cell = thing.Position + dir;
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
                        return comp;
                    }
                }
            }
        }
        return null;
    }
}
