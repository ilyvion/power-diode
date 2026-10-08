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

    // How far above its reserve floor (or overflow threshold) the source network's batteries must
    // be before a switched-off sink consumer counts as demand, matching vanilla PowerNet's own
    // MinStoredEnergyToTurnOn. This keeps a consumer that browned out at the floor from switching
    // back on as soon as the battery recharges past the ReactionMarginTicks ramp, only to brown
    // out again moments later.
    public const float RestartMarginWattDays = 5f;

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
        var sourceSupply = SourceSupplyWatts(
            capWatts,
            sourceNetBalanceExclSelf,
            sourceBatteryReserveWatts
        );
        return Math.Min(sinkDeficit, sourceSupply);
    }

    // The most the diode can feed, regardless of how much the sink network wants.
    public static float SourceSupplyWatts(
        float capWatts,
        float sourceNetBalanceExclSelf,
        float sourceBatteryReserveWatts
    )
    {
        var sourceSurplus =
            Math.Max(0f, sourceNetBalanceExclSelf) + Math.Max(0f, sourceBatteryReserveWatts);
        return Math.Clamp(sourceSurplus, 0f, capWatts);
    }

    // Vanilla PowerNet only switches a consumer on once its network already has the surplus to
    // cover it, and a switched-off consumer doesn't count towards a net balance, so without this
    // the diode would neither feed a switched-off consumer on its sink network nor leave room for
    // one on its source network. Returns netBalanceExclSelf less the draw of each switched-off
    // consumer, smallest first, for as long as restartSupplyWatts still covers the resulting
    // deficit; a consumer the supply can't fully cover is left out rather than given power it can
    // never switch on with.
    public static float BalanceWithStartableConsumers(
        float netBalanceExclSelf,
        IEnumerable<float> switchedOffDrawWatts,
        float restartSupplyWatts
    )
    {
        var balance = netBalanceExclSelf;
        foreach (var drawWatts in switchedOffDrawWatts.OrderBy(watts => watts))
        {
            if (drawWatts - balance > restartSupplyWatts)
            {
                break;
            }
            balance -= drawWatts;
        }
        return balance;
    }

    // The reserve floor BatterySustainableWatts ramps the source battery's contribution down to
    // in one-way-valve mode: either an absolute watt-day amount, or (when reserveIsPercentage is
    // set) a percentage of the source network's total battery capacity. Overflow and top-up don't
    // use a source-side floor at all - they gate the entire flow instead, via
    // OverflowGateFraction/TopUpGateFraction.
    public static float SourceReserveFloorWattDays(
        PowerDiodeOperatingMode mode,
        float reserveWattDays,
        bool reserveIsPercentage,
        float reservePercent,
        float sourceBatteryCapacityWattDays
    ) =>
        mode == PowerDiodeOperatingMode.OneWayValve
            ? reserveIsPercentage
                ? reservePercent / 100f * sourceBatteryCapacityWattDays
                : reserveWattDays
            : 0f;

    // Vanilla PowerNet doesn't switch anything on while its batteries hold at least 0.1 Wd but
    // less than PowerNet.MinStoredEnergyToTurnOn, so while a switched-off consumer on the source
    // network is waiting to be switched on, the source batteries are kept at or above that.
    public static float SourceReserveFloorWithWaitingConsumersWattDays(
        float reserveFloorWattDays,
        bool sourceHasWaitingConsumers
    ) =>
        sourceHasWaitingConsumers
            ? Math.Max(reserveFloorWattDays, PowerNet.MinStoredEnergyToTurnOn)
            : reserveFloorWattDays;

    // Overflow mode's whole-flow gate: 0 while the source network's batteries are at or below the
    // threshold percentage of their capacity, ramping up to 1 over the final
    // ReactionMarginTicks ticks' worth of watt-days above it (scaled against capWatts, since
    // BatterySustainableWatts otherwise returns unbounded watts rather than a 0-1 fraction). This
    // gates the diode's entire output - including any flow that would otherwise be driven by the
    // sink network's own consumer deficit - not just the portion drawn from the source battery
    // itself, since CompTick recomputes it fresh every tick as the source battery's stored energy
    // changes. marginWattDays raises the threshold by that many watt-days.
    public static float OverflowGateFraction(
        float capWatts,
        float overflowThresholdPercent,
        float sourceBatteryStoredWattDays,
        float sourceBatteryCapacityWattDays,
        float marginWattDays = 0f
    )
    {
        if (capWatts <= 0f)
        {
            return 0f;
        }
        var floorWattDays =
            (overflowThresholdPercent / 100f * sourceBatteryCapacityWattDays) + marginWattDays;
        var sustainableWatts = BatterySustainableWatts(sourceBatteryStoredWattDays, floorWattDays);
        return Mathf.Clamp01(sustainableWatts / capWatts);
    }

    // Top-up mode's whole-flow gate: the mirror image of OverflowGateFraction, gating on the sink
    // network's batteries instead - 1 while they're below the threshold percentage of their
    // capacity, ramping down to 0 as they approach it.
    public static float TopUpGateFraction(
        float capWatts,
        float topUpThresholdPercent,
        float sinkBatteryStoredWattDays,
        float sinkBatteryCapacityWattDays
    )
    {
        if (capWatts <= 0f)
        {
            return 0f;
        }
        var ceilingWattDays = topUpThresholdPercent / 100f * sinkBatteryCapacityWattDays;
        var headroomWattDays = Math.Max(0f, ceilingWattDays - sinkBatteryStoredWattDays);
        var sustainableWatts = BatterySustainableWatts(headroomWattDays);
        return Mathf.Clamp01(sustainableWatts / capWatts);
    }
}
