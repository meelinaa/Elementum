namespace Elementum.Shared.Enums;

/// <summary>
/// Returns the GoldAPI symbol for each metal (e.g. XAU for Gold).
/// </summary>
public static class MetalTypesExtensions
{
    public static string ToApiSymbol(this MetalTypes metal) => metal switch
    {
        MetalTypes.Gold => "XAU",
        MetalTypes.Silver => "XAG",
        MetalTypes.Platinum => "XPT",
        MetalTypes.Palladium => "XPD",
        _ => metal.ToString()
    };
}
