namespace PowerDiode;

// Passive by design: the paired CompPowerDiodeFeed computes flow and drives both sides'
// CompPowerTrader.PowerOutput each tick, so this comp only needs to hold the link and expose
// its own CompPowerTrader for the feed node to grab hold of - one source of truth for the flow
// calculation, fewer moving parts.
[HotSwappable]
internal class CompPowerDiodeDraw : ThingComp
{
    private bool pairingSaved;

    internal CompPowerDiodeFeed? Partner { get; set; }

    internal CompPowerTrader PowerTrader
    {
        get
        {
            field ??= parent.GetComp<CompPowerTrader>();
            return field;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Partner == null && !(respawningAfterLoad && pairingSaved))
        {
            PowerDiodeLinking.TryLinkDrawNode(this);
        }
    }

    // The pairing itself is saved by the outlet.
    public override void PostExposeData()
    {
        base.PostExposeData();
        PowerDiodeLinking.ExposePairingSaved(ref pairingSaved);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        Partner?.Unlink();
        base.PostDeSpawn(map, mode);
    }

    public override string CompInspectStringExtra() =>
        Partner == null ? "PowerDiode.NotLinked".Translate()
        : Partner.IsSharedGridDegenerate
            ? "PowerDiode.SharedGrid".Translate(Partner.parent.LabelCap)
        : PowerTrader.PowerOutput >= 0f ? "PowerDiode.LinkedIdle".Translate(Partner.parent.LabelCap)
        : "PowerDiode.Feeding".Translate(
            Partner.parent.LabelCap,
            (-PowerTrader.PowerOutput).ToString("F0", CultureInfo.InvariantCulture)
        );

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (Partner == null)
        {
            var linkGizmo = PowerDiodeLinkGizmo.Create(
                parent,
                PowerDiodeLinking.UnpairedAdjacentFeedNodes(this),
                "PowerDiode.LinkToOutlet".Translate(),
                "PowerDiode.LinkToOutletDesc".Translate(),
                feed => PowerDiodeLinking.LinkSpawned(this, feed)
            );
            if (linkGizmo != null)
            {
                yield return linkGizmo;
            }
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        var partner = Partner;
        if (partner == null)
        {
            return;
        }
        PowerDiodeConnectorOverlay.DrawConnectorNub(
            parent,
            partner.parent.Position - parent.Position
        );
        if (partner.IsSharedGridDegenerate)
        {
            PowerDiodeOverlay.DrawSharedGridOverlay(parent);
        }
    }
}
