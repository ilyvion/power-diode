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
    ) =>
        PowerDiodeLinking.FindDrawNodeToLinkTo(loc, map, thingToIgnore) != null
            ? true
            : "PowerDiode.MustPlaceNextToUnpairedDrawNode".Translate();
}
