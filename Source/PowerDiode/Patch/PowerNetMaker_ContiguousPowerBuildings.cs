namespace PowerDiode.Patch;

// Both diode buildings need transmitsPower=true so they wire directly into their own network
// like a normal generator/battery (no indirect lamp-style connection). But PowerNetMaker's flood
// fill treats any two cardinally-adjacent transmitters as part of the same PowerNet, and a linked
// pair is placed exactly that way - so left unpatched, the flood fill merges the two "separate"
// networks the diode is supposed to keep apart.
//
// This postfix lets the vanilla flood fill (and any other mod's own prefixes/transpilers on it)
// run completely untouched, then - only when the merged result actually spans a linked draw/feed
// pair - trims it back down to the subset reachable from root without crossing that pair's edge.
// Every network that doesn't involve a diode link is returned exactly as vanilla produced it.
[HarmonyPatch(typeof(PowerNetMaker), "ContiguousPowerBuildings")]
internal static class PowerNetMaker_ContiguousPowerBuildings
{
    private static void Postfix(Building root, ref IEnumerable<CompPower> __result)
    {
        var comps = __result as IReadOnlyCollection<CompPower> ?? [.. __result];
        var buildings = new HashSet<Building>(comps.Select(comp => (Building)comp.parent));
        if (!SpansADiodeLinkEdge(buildings))
        {
            __result = comps;
            return;
        }
        __result = GraphReachability
            .ReachableWithoutCrossingBlockedEdges(
                root,
                buildings,
                AdjacentTransmitters,
                IsDiodeLinkEdge
            )
            .Select(building => building.PowerComp);
    }

    private static bool SpansADiodeLinkEdge(HashSet<Building> buildings)
    {
        foreach (var building in buildings)
        {
            var drawPartner = building.GetComp<CompPowerDiodeDraw>()?.Partner?.parent;
            var feedPartner = building.GetComp<CompPowerDiodeFeed>()?.Partner?.parent;
            if (
                (drawPartner != null && buildings.Contains(drawPartner))
                || (feedPartner != null && buildings.Contains(feedPartner))
            )
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<Building> AdjacentTransmitters(Building building)
    {
        foreach (var cell in GenAdj.CellsAdjacentCardinal(building))
        {
            if (!cell.InBounds(building.Map))
            {
                continue;
            }
            var thingList = cell.GetThingList(building.Map);
            for (var i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Building { TransmitsPowerNow: not false } candidate)
                {
                    yield return candidate;
                }
            }
        }
    }

    internal static bool IsDiodeLinkEdge(Building a, Building b) =>
        a.GetComp<CompPowerDiodeDraw>()?.Partner?.parent == b
        || a.GetComp<CompPowerDiodeFeed>()?.Partner?.parent == b;
}
