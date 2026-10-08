namespace PowerDiode;

[StaticConstructorOnStartup]
#pragma warning disable CA1724
internal static class Resources
#pragma warning restore CA1724
{
    internal static readonly Texture2D
        // operating modes
        ModeOneWayValveIcon = ContentFinder<Texture2D>.Get("UI/Commands/PWDIconModeOneWayValve"),
        ModeOverflowIcon = ContentFinder<Texture2D>.Get("UI/Commands/PWDIconModeOverflow"),
        ModeTopUpIcon = ContentFinder<Texture2D>.Get("UI/Commands/PWDIconModeTopUp"),
        // gizmos
        LinkNeighbourIcon = ContentFinder<Texture2D>.Get("UI/Commands/PWDIconLinkNeighbour");
}
