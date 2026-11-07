namespace PricingCore
{
    /// <summary>Bir varyantın mevcut fiyatı ve üstü çizili (karşılaştırma) fiyatı.</summary>
    public sealed record PriceValues(decimal Price, decimal? CompareAtPrice);
}
