namespace PricingCore
{
    /// <summary>Hatalar kaydetmeyi engeller; uyarılar onay penceresinde gösterilir.</summary>
    public sealed record PriceCheck(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
    {
        public bool IsValid => Errors.Count == 0;
    }

    public static class PriceValidator
    {
        /// <summary>Bu orandan fazla değişen fiyat için yazım hatası uyarısı verilir (0,5 = %50).</summary>
        public const decimal LargeChangeRatio = 0.5m;

        public static PriceCheck Check(PriceValues current, decimal? newPrice, decimal? newCompareAt)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (newPrice == null)
                errors.Add("Yeni fiyat boş.");
            else if (newPrice <= 0)
                errors.Add("Fiyat 0'dan büyük olmalı.");
            else if (decimal.Round(newPrice.Value, 2) != newPrice.Value)
                errors.Add("Fiyat en fazla 2 ondalık olabilir.");

            if (newCompareAt != null)
            {
                if (newCompareAt <= 0)
                    errors.Add("Karşılaştırma fiyatı 0'dan büyük olmalı.");
                else if (decimal.Round(newCompareAt.Value, 2) != newCompareAt.Value)
                    errors.Add("Karşılaştırma fiyatı en fazla 2 ondalık olabilir.");
            }

            if (errors.Count == 0 && newPrice is { } price)
            {
                if (newCompareAt is { } compare && compare <= price)
                    warnings.Add("Karşılaştırma fiyatı fiyattan büyük değil; Shopify üstü çizili fiyat göstermez.");

                if (current.Price > 0 && Math.Abs(price - current.Price) / current.Price > LargeChangeRatio)
                    warnings.Add($"Fiyat %{(price - current.Price) / current.Price * 100:0} değişiyor.");
            }

            return new PriceCheck(errors, warnings);
        }
    }
}
