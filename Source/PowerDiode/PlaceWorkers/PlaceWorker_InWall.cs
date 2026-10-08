namespace PowerDiode;

internal class PlaceWorker_InWall : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(
        BuildableDef checkingDef,
        IntVec3 loc,
        Rot4 rot,
        Map map,
        Thing? thingToIgnore = null,
        Thing? thing = null
    ) =>
        loc.GetEdifice(map) is { def.IsWall: true }
            ? true
            : "PowerDiode.MustPlaceInWall".Translate();

    // Vanilla only lets a non-edifice be built over its exact ThingDefOf.Wall (and never over a
    // power conduit, since both transmit power); this extends that to every wall def and lets the
    // diode replace the conduit running under the wall, like the floor-standing diode does.
    public override bool ForceAllowPlaceOver(BuildableDef other) =>
        other is ThingDef { IsWall: true } or ThingDef { building.isPowerConduit: true };
}
