namespace PowerDiode.Patch;

// Graphic_LinkedTransmitter/Graphic_LinkedTransmitterOverlay decide whether a wire tile visually
// links to a neighboring cell purely by whether *some* PowerNet is registered there
// (PowerNetGrid.TransmittedPowerNetAt(c) != null) - not whether it's the *same* net as the tile
// doing the linking. A diode link splits a linked pair into two separate PowerNets (see
// PowerNetMaker_ContiguousPowerBuildings), so without this patch the wire still rendered as one
// continuous line straight across the pair. Only links between two diode buildings on different
// nets are cut.
[HarmonyPatch(typeof(Graphic_LinkedTransmitter), nameof(Graphic_LinkedTransmitter.ShouldLinkWith))]
internal static class Graphic_LinkedTransmitter_ShouldLinkWith
{
    private static void Postfix(IntVec3 c, Thing parent, ref bool __result) =>
        RestrictToSameNet(c, parent, ref __result);

    internal static void RestrictToSameNet(IntVec3 c, Thing parent, ref bool __result)
    {
        if (
            !__result
            || !IsDiodeBuilding(parent)
            || !c.GetThingList(parent.Map).Any(IsDiodeBuilding)
        )
        {
            return;
        }
        var grid = parent.Map.powerNetGrid;
        if (grid.TransmittedPowerNetAt(parent.Position) != grid.TransmittedPowerNetAt(c))
        {
            __result = false;
        }
    }

    private static bool IsDiodeBuilding(Thing thing) =>
        thing is ThingWithComps thingWithComps
        && (
            thingWithComps.GetComp<CompPowerDiodeDraw>() != null
            || thingWithComps.GetComp<CompPowerDiodeFeed>() != null
        );
}

[HarmonyPatch(
    typeof(Graphic_LinkedTransmitterOverlay),
    nameof(Graphic_LinkedTransmitterOverlay.ShouldLinkWith)
)]
internal static class Graphic_LinkedTransmitterOverlay_ShouldLinkWith
{
    private static void Postfix(IntVec3 c, Thing parent, ref bool __result) =>
        Graphic_LinkedTransmitter_ShouldLinkWith.RestrictToSameNet(c, parent, ref __result);
}
