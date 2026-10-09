namespace PowerDiode;

// Outlets in the same mode group into one gizmo, whose menu sets the mode on every outlet in the
// group.
internal sealed class Command_SetDiodeOperatingMode : Command
{
    private readonly List<CompPowerDiodeFeed> feeds;

    internal Command_SetDiodeOperatingMode(CompPowerDiodeFeed feed)
    {
        feeds = [feed];
        defaultLabel = "PowerDiode.OperatingModeGizmoLabel".Translate(feed.OperatingMode.Label());
        defaultDesc = feed.OperatingMode.Description();
        icon = feed.OperatingMode.Icon();
    }

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        Find.WindowStack.Add(
            new FloatMenu([
                .. Enum.GetValues(typeof(PowerDiodeOperatingMode))
                    .Cast<PowerDiodeOperatingMode>()
                    .Select(mode => new FloatMenuOption(mode.Label(), () => SetMode(mode))),
            ])
        );
    }

    private void SetMode(PowerDiodeOperatingMode mode)
    {
        foreach (var feed in feeds)
        {
            feed.OperatingMode = mode;
        }
    }

    public override bool InheritInteractionsFrom(Gizmo other)
    {
        if (other is Command_SetDiodeOperatingMode command)
        {
            feeds.AddRange(command.feeds.Where(feed => !feeds.Contains(feed)));
        }
        return false;
    }
}
