/// <summary>The accepted Earth owner; reference algorithms are tested outside player routing.</summary>
internal static class EarthRoutePolicy
{
    internal static bool UsesNcsm1(string scene) => scene is "earth" or "sol";
}
