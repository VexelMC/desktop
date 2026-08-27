using System.Collections.ObjectModel;
using System.Windows.Input;
using Vexel.App.Models;
using Vexel.Core.Logging;
using Vexel.Core.Minecraft;
using Vexel.Core.Settings;

namespace Vexel.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IAppLogger _logger;
    private readonly IMinecraftDetector _minecraftDetector;
    private readonly ISettingsStore _settingsStore;
    private string _gameStatus = "Checking installation…";
    private string _statusDetail = "Vexel has not modified Minecraft.";
    private bool _isRefreshing;

    public MainViewModel(
        IAppLogger logger,
        ISettingsStore settingsStore,
        IMinecraftDetector minecraftDetector)
    {
        _logger = logger;
        _settingsStore = settingsStore;
        _minecraftDetector = minecraftDetector;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsRefreshing);
        Features = new ObservableCollection<FeatureCard>
        {
            new("Item Delay Fix", "Reduces the delay between applicable item uses.", "Not verified", "No compatible Minecraft build is verified yet.", false),
            new("No Camera Reset", "Prevents supported teleport rotation resets.", "Not verified", "No compatible Minecraft build is verified yet.", false),
            new("AutoSprint", "Maintains sprint only under normal game conditions.", "Research required", "No implementation has been approved yet.", false),
            new("No Hurt Cam", "Removes the supported damage-camera effect.", "Not verified", "No compatible Minecraft build is verified yet.", false),
            new("GUI Scale", "Offers safe, verified scale controls when available.", "Not verified", "No compatible Minecraft build is verified yet.", false),
        };
    }

    public ObservableCollection<FeatureCard> Features { get; }

    public ICommand RefreshCommand { get; }

    public string GameStatus
    {
        get => _gameStatus;
        private set
        {
            _gameStatus = value;
            OnPropertyChanged();
        }
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set
        {
            _statusDetail = value;
            OnPropertyChanged();
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            _isRefreshing = value;
            OnPropertyChanged();
            ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
        }
    }

    public async Task InitializeAsync()
    {
        _ = await _settingsStore.LoadAsync();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            var detection = await _minecraftDetector.DetectAsync();
            UpdateGameStatus(detection);
            await _logger.WriteAsync(new LogEntry(
                DateTimeOffset.UtcNow,
                LogLevel.Information,
                "app.refresh",
                "Minecraft status refresh completed.",
                new Dictionary<string, string>
                {
                    ["installed"] = detection.IsInstalled.ToString(),
                    ["running"] = detection.IsRunning.ToString(),
                    ["fingerprinted"] = (detection.Fingerprint is not null).ToString(),
                }));
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void UpdateGameStatus(MinecraftDetectionResult detection)
    {
        if (detection.IsRunning)
        {
            var version = detection.Installation?.Version ?? detection.Fingerprint?.ProductVersion ?? "unknown build";
            GameStatus = $"Minecraft {version} is running";

            if (detection.Fingerprint is null)
            {
                StatusDetail = detection.Diagnostic ?? "The running executable could not be fingerprinted. Patches remain unavailable.";
                return;
            }

            StatusDetail = $"{detection.Fingerprint.Architecture} · {FingerprintLabel(detection.Fingerprint)} {detection.Fingerprint.Sha256[..12]}… · no verified patch definitions for this build.";
            return;
        }

        if (!detection.IsInstalled)
        {
            GameStatus = "No Minecraft process detected";
            StatusDetail = detection.Diagnostic ?? "Start Minecraft, then refresh. A Microsoft Store package is not required.";
            return;
        }

        GameStatus = $"Minecraft {detection.Installation!.Version} is installed but closed";

        if (detection.Fingerprint is null)
        {
            StatusDetail = detection.Diagnostic ?? "The executable could not be fingerprinted. Patches remain unavailable.";
            return;
        }

        StatusDetail = $"{detection.Fingerprint.Architecture} · {FingerprintLabel(detection.Fingerprint)} {detection.Fingerprint.Sha256[..12]}… · no verified patch definitions for this build.";
    }

    private static string FingerprintLabel(MinecraftBuildFingerprint fingerprint) =>
        fingerprint.Source == FingerprintSource.ExecutableFile ? "File SHA-256" : "Loaded-image SHA-256";
}
