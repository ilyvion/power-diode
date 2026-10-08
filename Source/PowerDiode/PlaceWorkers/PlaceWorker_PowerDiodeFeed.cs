namespace PowerDiode;

internal class PlaceWorker_PowerDiodeFeed : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(
        BuildableDef checkingDef,
        IntVec3 loc,
        Rot4 rot,
        Map map,
        Thing? thingToIgnore = null,
        Thing? thing = null
    )
    {
        foreach (var dir in GenAdj.CardinalDirections)
        {
            var cell = loc + dir;
            if (!cell.InBounds(map))
            {
                continue;
            }
            foreach (var candidate in cell.GetThingList(map))
            {
                if (candidate == thingToIgnore || candidate is not ThingWithComps thingWithComps)
                {
                    continue;
                }
                var draw = thingWithComps.GetComp<CompPowerDiodeDraw>();
                // When reinstalling, thingToIgnore is the outlet being moved, which stays paired
                // with its current intake until it's uninstalled.
                if (draw != null && (draw.Partner == null || draw.Partner.parent == thingToIgnore))
                {
                    return true;
                }
            }
        }
        return "PowerDiode.MustPlaceNextToUnpairedDrawNode".Translate();
    }
}
