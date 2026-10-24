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

public sealed class SettingsStoreTrySaveTests : IDisposable
{
    private readonly string tmp = Directory.CreateTempSubdirectory("acerkontrol-").FullName;
    public void Dispose() => Directory.Delete(tmp, recursive: true);

    [Fact]
    public void TrySave_Unwritable_ReturnsFalse()
    {
        var path = Path.Combine(tmp, "settings.json");
        Directory.CreateDirectory(path); // aynı adda bir klasör yazmayı engeller
        Assert.False(new SettingsStore(path).TrySave(new AppSettings(false)));
    }

    [Fact]
    public void TrySave_Writable_ReturnsTrue()
        => Assert.True(new SettingsStore(Path.Combine(tmp, "settings.json")).TrySave(new AppSettings(false)));
}
