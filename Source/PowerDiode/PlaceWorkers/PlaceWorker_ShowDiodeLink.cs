namespace PowerDiode;

internal class PlaceWorker_ShowDiodeLink : PlaceWorker
{
    public override void DrawGhost(
        ThingDef def,
        IntVec3 center,
        Rot4 rot,
        Color ghostCol,
        Thing? thing = null
    )
    {
        var partner = LinkTargetFor(def, center, Find.CurrentMap, thing);
        if (partner != null)
        {
            GenDraw.DrawLineBetween(
                GenThing.TrueCenter(center, rot, def.size, def.Altitude),
                partner.TrueCenter(),
                SimpleColor.Green
            );
        }
    }

    // def is a blueprint or frame def when drawn for a selected blueprint or frame, and the
    // building's own def when drawn for the selected built diode, which shows no link.
    internal static Thing? LinkTargetFor(ThingDef def, IntVec3 center, Map map, Thing? thing)
    {
        var buildingDef = def.entityDefToBuild as ThingDef;
        var isSelectedBuiltDiode =
            buildingDef == null
            && thing is { Spawned: true }
            && thing.def == def
            && thing.Position == center;
        return isSelectedBuiltDiode
            ? null
            : PowerDiodeLinking.FindPartnerToLinkTo(buildingDef ?? def, center, map, thing);
    }
}
