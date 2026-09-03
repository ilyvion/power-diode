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
                if (draw != null && draw.Partner == null)
                {
                    return true;
                }
            }
        }
        return "PowerDiode.MustPlaceNextToUnpairedDrawNode".Translate();
    }
}
