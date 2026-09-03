namespace PowerDiode;

// A small mounted cable-clamp graphic drawn on top of a linked draw/feed node, rotated to face
// whichever cardinal side its paired partner actually sits on - so a linked pair reads as visibly
// connected together regardless of which side they were joined from.
[StaticConstructorOnStartup]
internal static class PowerDiodeConnectorOverlay
{
    private static readonly Material ConnectorMat = MaterialPool.MatFrom(
        "Buildings/PowerDiodeConnectorNub",
        ShaderDatabase.Cutout
    );

    internal static void DrawConnectorNub(Thing parent, IntVec3 partnerOffset)
    {
        var rotation = Rot4.FromIntVec3(partnerOffset);
        var drawPos = parent.DrawPos;
        drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();

        var size = parent.Graphic.drawSize;
        var mesh = MeshPool.GridPlane(size);

        Graphics.DrawMesh(
            mesh,
            Matrix4x4.TRS(drawPos, rotation.AsQuat, Vector3.one),
            ConnectorMat,
            0
        );
    }
}
