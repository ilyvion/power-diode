namespace PowerDiode;

// Pure, free of any Verse/RimWorld types so it's unit-testable without spawning anything on a
// live Map, same rationale as GraphReachability.
internal static class PowerDiodeSharedGridDetection
{
    // Two live power nets are the same network whenever they're the exact same instance -
    // RimWorld's flood fill (PowerNetMaker) hands out a fresh PowerNet whenever a map's wiring
    // topology changes, so reference equality correctly flips the moment some other connection
    // joins (or stops joining) the diode's two sides.
    internal static bool IsSharedGrid<TNet>(TNet? sinkNet, TNet? sourceNet)
        where TNet : class => sinkNet != null && ReferenceEquals(sinkNet, sourceNet);
}
