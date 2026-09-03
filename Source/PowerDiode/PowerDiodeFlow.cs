namespace PowerDiode;

internal static class PowerDiodeFlow
{
    // How many ticks of reaction time BatterySustainableWatts ramps a battery's contribution
    // over as it approaches its reserve floor (or full charge, for headroom). This comp's flow
    // decision and RimWorld's own PowerNetTick brownout check don't run in lockstep - the latter
    // can act on a PowerOutput this comp set a tick earlier - so an instantaneous cutoff the
    // moment a battery is theoretically exhausted still overshoots by about a tick's worth of
    // watts. Ramping over several ticks' worth of margin instead gives the flow calculation time
    // to catch up before the battery actually runs dry.
    public const int ReactionMarginTicks = 30;

    // Converts a battery's remaining charge capacity or stored energy (in watt-days) to a
    // sustainable wattage, treating reserveBufferWattDays of it as untouchable and ramping the
    // rest down to 0 over the final ReactionMarginTicks ticks' worth of watt-days rather than
    // cutting off abruptly - so a healthily charged battery still fully backs a diode's target
    // wattage, and only tapers off as it nears the reserve floor.
    public static float BatterySustainableWatts(
        float storedOrAcceptWattDays,
        float reserveBufferWattDays = 0f
    ) =>
        Math.Max(0f, storedOrAcceptWattDays - reserveBufferWattDays)
        / (ReactionMarginTicks * CompPower.WattsToWattDaysPerTick);

    // sinkBatteryHeadroomWatts/sourceBatteryReserveWatts: BatterySustainableWatts applied to the
    // feed/draw node's power net batteries' total headroom/stored energy. Net balance
    // (sinkNetBalanceExclSelf/sourceNetBalanceExclSelf) never reflects batteries at all -
    // PowerNet tracks them separately from the generators/consumers that feed into that balance -
    // so without these, a battery-only network looks exactly like an empty one.
    public static float ComputeFlowWatts(
        float capWatts,
        float sinkNetBalanceExclSelf,
        float sourceNetBalanceExclSelf,
        float sinkBatteryHeadroomWatts,
        float sourceBatteryReserveWatts
    )
    {
        var consumerDeficit = Math.Max(0f, -sinkNetBalanceExclSelf);
        var batteryDemand = Math.Max(0f, sinkBatteryHeadroomWatts);
        var sinkDeficit = consumerDeficit + batteryDemand;
        var sourceSurplus =
            Math.Max(0f, sourceNetBalanceExclSelf) + Math.Max(0f, sourceBatteryReserveWatts);
        var flow = Math.Min(capWatts, Math.Min(sinkDeficit, sourceSurplus));
        return Math.Clamp(flow, 0f, capWatts);
    }
}
