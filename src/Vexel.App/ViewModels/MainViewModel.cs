using System.Collections.ObjectModel;
using System.Windows.Input;
using Vexel.App.Models;
using Vexel.Core.Logging;
using Vexel.Core.Settings;

namespace Vexel.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IAppLogger _logger;
    private readonly ISettingsStore _settingsStore;
    private string _gameStatus = "Checking installation…";
    private string _statusDetail = "Vexel has not modified Minecraft.";
    private bool _isRefreshing;

    public MainViewModel(IAppLogger logger, ISettingsStore settingsStore)
    {
        _logger = logger;
        _settingsStore = settingsStore;
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
            GameStatus = "Minecraft detection is not implemented yet";
            StatusDetail = "Phase 3 will identify the package, process, and executable fingerprint. No patches can be applied.";
            await _logger.WriteAsync(new LogEntry(
                DateTimeOffset.UtcNow,
                LogLevel.Information,
                "app.refresh",
                "The initial application shell requested a Minecraft status refresh."));
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
