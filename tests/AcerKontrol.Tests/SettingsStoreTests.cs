using AcerKontrol.Settings;

namespace AcerKontrol.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string tmp = Directory.CreateTempSubdirectory("acerkontrol-").FullName;
    private string path => Path.Combine(tmp, "settings.json");

    public void Dispose() => Directory.Delete(tmp, recursive: true);

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
        => Assert.True(new SettingsStore(Path.Combine(tmp, "yok.json")).Load().ChargeLimitEnabled);

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        File.WriteAllText(path, "{bozuk");
        Assert.Equal(new AppSettings(), new SettingsStore(path).Load());
    }

    [Fact]
    public void Load_EmptyFile_ReturnsDefaults()
    {
        File.WriteAllText(path, "");
        Assert.Equal(new AppSettings(), new SettingsStore(path).Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var s = new SettingsStore(Path.Combine(tmp, "alt", "settings.json"));
        s.Save(new AppSettings(false));
        Assert.False(s.Load().ChargeLimitEnabled);
    }
}
