namespace PowerDiode.Patch;

// Graphic_LinkedTransmitter/Graphic_LinkedTransmitterOverlay decide whether a wire tile visually
// links to a neighboring cell purely by whether *some* PowerNet is registered there
// (PowerNetGrid.TransmittedPowerNetAt(c) != null) - not whether it's the *same* net as the tile
// doing the linking. In vanilla this distinction never matters, since any two cardinally-adjacent
// power-transmitting things are always the same net. A diode link breaks that invariant: once
// PowerNetMaker_ContiguousPowerBuildings has split a linked pair into two separate PowerNets, the
// wire still rendered as one continuous, uninterrupted line straight across the pair without this
// patch.
[HarmonyPatch(typeof(Graphic_LinkedTransmitter), nameof(Graphic_LinkedTransmitter.ShouldLinkWith))]
internal static class Graphic_LinkedTransmitter_ShouldLinkWith
{
    private static void Postfix(IntVec3 c, Thing parent, ref bool __result) =>
        RestrictToSameNet(c, parent, ref __result);

    internal static void RestrictToSameNet(IntVec3 c, Thing parent, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        var grid = parent.Map.powerNetGrid;
        if (grid.TransmittedPowerNetAt(parent.Position) != grid.TransmittedPowerNetAt(c))
        {
            __result = false;
        }
    }
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
