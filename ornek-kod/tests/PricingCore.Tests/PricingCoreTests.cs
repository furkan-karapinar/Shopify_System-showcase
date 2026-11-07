using PricingCore;

namespace PricingCore.Tests;

public sealed class PriceValidatorTests
{
    private static readonly PriceValues Current = new(100m, 150m);

    [Theory]
    [InlineData(null, "Yeni fiyat boş.")]
    [InlineData(0.0, "Fiyat 0'dan büyük olmalı.")]
    [InlineData(-5.0, "Fiyat 0'dan büyük olmalı.")]
    [InlineData(99.999, "Fiyat en fazla 2 ondalık olabilir.")]
    public void Check_RejectsInvalidPrice(double? price, string error)
    {
        var r = PriceValidator.Check(Current, (decimal?)price, null);
        Assert.False(r.IsValid);
        Assert.Contains(error, r.Errors);
    }

    [Fact]
    public void Check_WarnsWhenCompareAtNotAbovePrice()
    {
        var r = PriceValidator.Check(Current, 120m, 120m);
        Assert.True(r.IsValid);
        Assert.Single(r.Warnings);
    }

    [Theory]
    [InlineData(151.0, true)]
    [InlineData(49.0, true)]
    [InlineData(150.0, false)]
    [InlineData(105.0, false)]
    public void Check_WarnsOnLargeChange(double price, bool warn)
    {
        var r = PriceValidator.Check(Current, (decimal)price, null);
        Assert.True(r.IsValid);
        Assert.Equal(warn, r.Warnings.Any(w => w.StartsWith("Fiyat %")));
    }
}

public sealed class AtomicFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "atomicfile-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Write_ThenRead_RoundTrips_AndLeavesNoTempFiles()
    {
        var path = Path.Combine(_dir, "sub", "ayar.json");
        AtomicFile.WriteAllText(path, "{\"a\":1}");
        AtomicFile.WriteAllText(path, "{\"a\":2,\"ş\":\"ğ\"}");

        Assert.Equal("{\"a\":2,\"ş\":\"ğ\"}", AtomicFile.ReadAllText(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Write_DoesNotEmitBom()
    {
        var path = Path.Combine(_dir, "a.txt");
        AtomicFile.WriteAllText(path, "x");
        Assert.Equal(new byte[] { (byte)'x' }, File.ReadAllBytes(path));
    }

    [Fact]
    public void Read_MissingFile_ThrowsImmediately()
    {
        Directory.CreateDirectory(_dir);
        Assert.Throws<FileNotFoundException>(() => AtomicFile.ReadAllText(Path.Combine(_dir, "yok.txt")));
    }

    [Fact]
    public void ConcurrentReader_NeverSeesPartialContent()
    {
        var path = Path.Combine(_dir, "c.txt");
        var a = new string('a', 20000);
        var b = new string('b', 20000);
        AtomicFile.WriteAllText(path, a);

        using var cts = new CancellationTokenSource();
        var bad = 0;
        var reader = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var s = AtomicFile.ReadAllText(path);
                    if (s != a && s != b) Interlocked.Increment(ref bad);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        });

        for (var i = 0; i < 100; i++) AtomicFile.WriteAllText(path, i % 2 == 0 ? b : a);
        cts.Cancel();
        reader.Wait();
        Assert.Equal(0, bad);
    }
}
