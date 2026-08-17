namespace Elementum.Domain.Enums;

/// <summary>
/// Returns the GoldAPI symbol for each metal (e.g. XAU for Gold).
/// </summary>
public static class MetalTypesExtensions
{
    /// <summary>Returns the API symbol for the metal (e.g. XAU for Gold, XAG for Silver, XPT for Platinum).</summary>
    public static string ToApiSymbol(this MetalTypes metal) => metal switch
    {
        MetalTypes.Gold => "XAU",
        MetalTypes.Silver => "XAG",
        MetalTypes.Platinum => "XPT",
        MetalTypes.Palladium => "XPD",
        _ => metal.ToString()
    };
}
