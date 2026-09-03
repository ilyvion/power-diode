namespace PowerDiode;

internal static class PowerDiodeLinking
{
    internal static void TryLinkFeedNode(CompPowerDiodeFeed feed)
    {
        var draw = FindAdjacent<CompPowerDiodeDraw>(feed.parent);
        if (draw == null)
        {
            return;
        }
        if (draw.Partner != null)
        {
            // The PlaceWorker normally prevents this, but dev-mode placement can bypass
            // PlaceWorker checks, so this has to hold up as an independent guarantee too.
            Log.Error(
                $"{feed.parent} tried to link to {draw.parent}, but it's already linked to {draw.Partner.parent}."
            );
            return;
        }
        Link(draw, feed);
    }

    internal static void TryLinkDrawNode(CompPowerDiodeDraw draw)
    {
        var feed = FindAdjacent<CompPowerDiodeFeed>(draw.parent);
        if (feed == null)
        {
            return;
        }
        if (feed.Partner != null)
        {
            Log.Error(
                $"{draw.parent} tried to link to {feed.parent}, but it's already linked to {feed.Partner.parent}."
            );
            return;
        }
        Link(draw, feed);
    }

    private static void Link(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        draw.Partner = feed;
        feed.Partner = draw;
    }

    internal static T? FindAdjacent<T>(Thing thing)
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
                    if (comp != null)
                    {
                        return comp;
                    }
                }
            }
        }
        return null;
    }
}
