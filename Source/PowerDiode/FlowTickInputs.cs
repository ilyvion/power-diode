namespace PowerDiode;

// Everything PowerDiodeFlow.TickFlowWatts needs to work out a diode's flow for one tick. The
// source side is the draw node's power net, the sink side the feed node's. The balances exclude
// the diode's own trader, and the switched-off draws are those of the consumers vanilla
// PowerNet.PowerNetTick would switch on if the network had the surplus for them.
internal record struct FlowTickInputs(
    PowerDiodeOperatingMode Mode,
    float TargetWatts,
    float ReserveWattDays,
    bool ReserveIsPercentage,
    float ReservePercent,
    float OverflowThresholdPercent,
    float TopUpThresholdPercent,
    float SourceRawBalanceExclSelf,
    IReadOnlyCollection<float> SourceSwitchedOffDrawWatts,
    float SinkRawBalanceExclSelf,
    IReadOnlyCollection<float> SinkSwitchedOffDrawWatts,
    float SinkAcceptWattDays,
    float SourceStoredWattDays,
    float SourceCapacityWattDays,
    float SinkStoredWattDays,
    float SinkCapacityWattDays
);
