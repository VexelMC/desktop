using Vexel.Core.Settings;

namespace Vexel.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"vexel-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsyncReturnsDefaultsWhenFileIsMissing()
    {
        var store = new JsonSettingsStore(Path.Combine(_directory, "settings.json"));

        var settings = await store.LoadAsync();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task SaveAsyncRoundTripsSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var expected = new AppSettings { AutoSprintPreferred = true, GuiScale = 1.25 };
        var store = new JsonSettingsStore(path);

        await store.SaveAsync(expected);

        Assert.Equal(expected, await store.LoadAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
