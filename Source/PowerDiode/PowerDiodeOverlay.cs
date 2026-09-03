namespace PowerDiode;

[StaticConstructorOnStartup]
internal static class PowerDiodeOverlay
{
    private static readonly Material SharedGridMat = MaterialPool.MatFrom(
        "UI/Overlays/PowerDiodeSharedGridOverlay",
        ShaderDatabase.MetaOverlay
    );

    private const float OverlaySizeFraction = 3f / 5f;

    internal static void DrawSharedGridOverlay(Thing parent)
    {
        var drawPos = parent.DrawPos;
        drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.21951221f;

        var iconSize = parent.Graphic.drawSize.y * OverlaySizeFraction;
        var mesh = MeshPool.GridPlane(new Vector2(iconSize, iconSize));

        var pulsePhase = (Time.realtimeSinceStartup + (397f * (parent.thingIDNumber % 571))) * 4f;
        var pulse = ((float)Math.Sin(pulsePhase) + 1f) * 0.5f;
        pulse = 0.3f + (pulse * 0.7f);

        var fadedMaterial = FadedMaterialPool.FadedVersionOf(SharedGridMat, pulse);
        Graphics.DrawMesh(
            mesh,
            Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one),
            fadedMaterial,
            0
        );
    }
}
