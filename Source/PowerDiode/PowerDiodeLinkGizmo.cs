namespace PowerDiode;

// Offered on an unpaired diode building next to unpaired partners it doesn't pair with on its own,
// since pairing only happens automatically when a diode building spawns.
internal static class PowerDiodeLinkGizmo
{
    private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get(
        "UI/Commands/PWDIconLinkNeighbour"
    );

    internal static Command_Action? Create<T>(
        Thing self,
        List<T> candidates,
        string label,
        string description,
        Action<T> link
    )
        where T : ThingComp =>
        candidates.Count == 0
            ? null
            : new Command_Action
            {
                defaultLabel = label,
                defaultDesc = description,
                icon = Icon,
                onHover = () =>
                {
                    foreach (var candidate in candidates)
                    {
                        Highlight(candidate.parent);
                    }
                },
                action = () =>
                {
                    if (candidates.Count == 1)
                    {
                        link(candidates[0]);
                        return;
                    }
                    Find.WindowStack.Add(
                        new FloatMenu([
                            .. candidates.Select(candidate => new FloatMenuOption(
                                "PowerDiode.LinkTargetOption".Translate(
                                    candidate.parent.LabelCap,
                                    Rot4.FromIntVec3(candidate.parent.Position - self.Position)
                                        .ToStringHuman()
                                ),
                                () => link(candidate),
                                mouseoverGuiAction: _ => Highlight(candidate.parent)
                            )),
                        ])
                    );
                },
            };

    private static void Highlight(Thing thing) =>
        TargetHighlighter.Highlight(thing, arrow: true, colonistBar: false, circleOverlay: true);
}
