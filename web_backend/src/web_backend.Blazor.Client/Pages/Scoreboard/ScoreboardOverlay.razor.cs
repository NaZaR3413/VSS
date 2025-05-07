using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.JSInterop;
using web_backend.Livestreams;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Authorization;

namespace web_backend.Blazor.Client.Pages.Scoreboard
{
    [AllowAnonymous]
    public partial class ScoreboardOverlay : web_backendComponentBase, IAsyncDisposable
    {
        private Timer _gameClockTimer;
        private Timer _shotClockTimer;
        private Timer _autoRefreshTimer;
        private double _gameClock = 12 * 60; // Default 12 minutes
        private double _shotClock = 24; // Default 24 seconds
        private bool _gameClockRunning = false;
        private bool _shotClockRunning = false;
        private bool _shotClockWarning = false;
        private string _gameClockInput = "12:00";
        private HubConnection hubConnection;
        private bool _syncWithLivestream = false;
        private Guid? _livestreamId;
        private LivestreamDto _linkedLivestream;
        private DateTime _lastScoreSync = DateTime.MinValue;
        private bool _isLoaded = false;
        private bool loading = true;


        [Parameter]
        [SupplyParameterFromQuery(Name = "admin")]
        public bool AdminMode { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "sport")]
        public string SportType { get; set; } = "Basketball";

        [Parameter]
        [SupplyParameterFromQuery(Name = "shape")]
        public string ScoreboardShape { get; set; } = "Rounded";

        [Parameter]
        [SupplyParameterFromQuery(Name = "id")]
        public string ScoreboardId { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "livestreamId")]
        public string LivestreamIdParam { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private ILivestreamAppService LivestreamService { get; set; }

        #region Scoreboard Properties
        // Event Info
        public string EventTitle { get; set; } = "Game Title";

        // Team Properties
        public string HomeTeamName { get; set; } = "Home";
        public string AwayTeamName { get; set; } = "Away";
        public int HomeTeamScore { get; set; } = 0;
        public int AwayTeamScore { get; set; } = 0;
        public string HomeTeamLogo { get; set; } = "";
        public string AwayTeamLogo { get; set; } = "";
        public int HomeTeamFouls { get; set; } = 0;
        public int AwayTeamFouls { get; set; } = 0;
        public string HomeTeamStats { get; set; } = "";
        public string AwayTeamStats { get; set; } = "";
        public string PossessionTeam { get; set; } = "home";

        // Timeout Properties
        public int HomeTimeouts { get; set; } = 3;
        public int AwayTimeouts { get; set; } = 3;
        public int MaxTimeouts { get; set; } = 3;
        public bool ShowTimeouts { get; set; } = true;

        // Period/Quarter Properties
        public int CurrentPeriod { get; set; } = 1;
        public int MaxPeriods { get; set; } = 4;

        // Baseball Specific
        public int CurrentInning { get; set; } = 1;
        public bool TopInning { get; set; } = true;
        public int Balls { get; set; } = 0;
        public int Strikes { get; set; } = 0;
        public int Outs { get; set; } = 0;

        // Style Properties
        public string BackgroundColor { get; set; } = "#1a1a1a";
        public string TextColor { get; set; } = "#ffffff";
        public string BorderColor { get; set; } = "#333333";
        public string HeaderColor { get; set; } = "#333333";
        public string HomeTeamBackgroundColor { get; set; } = "#3366cc";
        public string HomeTeamTextColor { get; set; } = "#ffffff";
        public string AwayTeamBackgroundColor { get; set; } = "#cc3333";
        public string AwayTeamTextColor { get; set; } = "#ffffff";

        // Additional Settings
        public bool ShowAdditionalStats { get; set; } = true;
        #endregion

        #region Computed Properties
        public string FormattedGameClock
        {
            get
            {
                TimeSpan time = TimeSpan.FromSeconds(_gameClock);
                return time.Minutes.ToString("D2") + ":" + time.Seconds.ToString("D2");
            }
        }

        public string ShotClock
        {
            get
            {
                if (SportType == "Basketball")
                {
                    return _shotClock.ToString("F0");
                }
                return "";
            }
        }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            loading = true; 

            _gameClockTimer = new Timer(1000);
            _gameClockTimer.Elapsed += OnGameClockTick;

            _shotClockTimer = new Timer(1000);
            _shotClockTimer.Elapsed += OnShotClockTick;

            // Setup auto refresh timer for OBS mode (non-admin mode)
            if (!AdminMode)
            {
                _autoRefreshTimer = new Timer(5000); // Check for updates every 5 seconds
                _autoRefreshTimer.Elapsed += OnAutoRefreshTick;
                _autoRefreshTimer.Start();
            }

            // Try to load data in this priority order:
            // 1. Direct URL parameter ID
            if (!string.IsNullOrEmpty(Id))
            {
                await LoadScoreboardById(Id);
            }
            // 2. Query parameter ID
            else if (!string.IsNullOrEmpty(ScoreboardId))
            {
                await LoadScoreboardById(ScoreboardId);
            }
            // 3. Livestream ID
            else if (!string.IsNullOrEmpty(LivestreamIdParam) && Guid.TryParse(LivestreamIdParam, out Guid livestreamGuid))
            {
                _livestreamId = livestreamGuid;
                _syncWithLivestream = true;
                await SyncWithLivestreamData();
            }
            // 4. Otherwise load default settings
            else
            {
                await LoadSettings();
            }

            InitializeSportDefaults();

            // Set up SignalR connection for real-time updates
            await SetupSignalRConnection();

            _isLoaded = true;
            loading = false;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (!AdminMode)
                    {
                        // Configure OBS specific settings when in non-admin mode
                        await JSRuntime.InvokeVoidAsync("configureForOBS");
                    }

                    await JSRuntime.InvokeVoidAsync("notifyScoreboardLoaded");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in OnAfterRenderAsync: {ex.Message}");
                }

                // Finally, make sure loading is false and force refresh
                if (loading)
                {
                    loading = false;
                    StateHasChanged();
                }
            }
        }
        private async Task SetupSignalRConnection()
        {
            try
            {
                // Only try to connect to SignalR if we're in a browser environment
                // and not using a static file host that doesn't support SignalR
                if (NavigationManager.BaseUri.Contains("localhost") ||
                    NavigationManager.BaseUri.Contains("20.3.254.14") ||
                    NavigationManager.BaseUri.Contains("vss-backend-api"))
                {
                    hubConnection = new HubConnectionBuilder()
                        .WithUrl(NavigationManager.ToAbsoluteUri("/signalr/scoreboard"))
                        .WithAutomaticReconnect()
                        .Build();

                    hubConnection.On<string, int, int>("UpdateScore", (gameId, homeScore, awayScore) =>
                    {
                        if (_syncWithLivestream && _livestreamId.HasValue && gameId == _livestreamId.ToString())
                        {
                            HomeTeamScore = homeScore;
                            AwayTeamScore = awayScore;
                            InvokeAsync(StateHasChanged);
                        }
                    });

                    hubConnection.On<string>("RefreshScoreboard", (gameId) =>
                    {
                        if (_syncWithLivestream && _livestreamId.HasValue && gameId == _livestreamId.ToString())
                        {
                            InvokeAsync(SyncWithLivestreamData);
                        }
                    });

                    try
                    {
                        await hubConnection.StartAsync();
                        Console.WriteLine("SignalR connection established successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error starting SignalR connection: {ex.Message}");
                        // Don't rethrow - scoreboard should still work without real-time updates
                    }
                }
                else
                {
                    Console.WriteLine("Skipping SignalR connection for static file hosting");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - scoreboard should still work without real-time updates
                Console.Error.WriteLine($"Error setting up SignalR connection: {ex.Message}");
            }
        }

        private async void OnAutoRefreshTick(object sender, ElapsedEventArgs e)
        {
            if (_syncWithLivestream && _livestreamId.HasValue)
            {
                // Only sync if it's been more than 5 seconds since the last sync
                if ((DateTime.Now - _lastScoreSync).TotalSeconds > 5)
                {
                    await InvokeAsync(SyncWithLivestreamData);
                }
            }
        }

        private async Task SyncWithLivestreamData()
        {
            if (!_livestreamId.HasValue) return;

            try
            {
                _linkedLivestream = await LivestreamService.GetAsync(_livestreamId.Value);

                if (_linkedLivestream != null)
                {
                    HomeTeamName = _linkedLivestream.HomeTeam;
                    AwayTeamName = _linkedLivestream.AwayTeam;
                    HomeTeamScore = _linkedLivestream.HomeScore;
                    AwayTeamScore = _linkedLivestream.AwayScore;
                    SportType = _linkedLivestream.EventType.ToString();
                    EventTitle = $"{_linkedLivestream.HomeTeam} vs {_linkedLivestream.AwayTeam}";

                    _lastScoreSync = DateTime.Now;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error syncing with livestream: " + ex.Message);
            }
        }

        private async Task LoadScoreboardById(string id)
        {
            var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "savedScoreboards");

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var savedScoreboards = JsonSerializer.Deserialize<List<SavedScoreboard>>(json);
                    var scoreboard = savedScoreboards?.FirstOrDefault(s => s.Id == id);

                    if (scoreboard != null)
                    {
                        // Apply the saved scoreboard settings
                        SportType = scoreboard.SportType;
                        ScoreboardShape = scoreboard.ScoreboardShape;
                        EventTitle = scoreboard.EventTitle;
                        HomeTeamName = scoreboard.HomeTeamName;
                        AwayTeamName = scoreboard.AwayTeamName;
                        HomeTeamLogo = scoreboard.HomeTeamLogo;
                        AwayTeamLogo = scoreboard.AwayTeamLogo;
                        BackgroundColor = scoreboard.BackgroundColor;
                        TextColor = scoreboard.TextColor;
                        BorderColor = scoreboard.BorderColor;
                        HeaderColor = scoreboard.HeaderColor;
                        HomeTeamBackgroundColor = scoreboard.HomeTeamBackgroundColor;
                        HomeTeamTextColor = scoreboard.HomeTeamTextColor;
                        AwayTeamBackgroundColor = scoreboard.AwayTeamBackgroundColor;
                        AwayTeamTextColor = scoreboard.AwayTeamTextColor;
                        ShowTimeouts = scoreboard.ShowTimeouts;
                        ShowAdditionalStats = scoreboard.ShowAdditionalStats;

                        // Check if there's a linked livestream
                        if (!string.IsNullOrEmpty(scoreboard.LivestreamId) && Guid.TryParse(scoreboard.LivestreamId, out Guid livestreamGuid))
                        {
                            _livestreamId = livestreamGuid;
                            _syncWithLivestream = true;
                            await SyncWithLivestreamData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error loading saved scoreboard: " + ex.Message);
                }
            }
        }

        private class SavedScoreboard
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string SportType { get; set; } = "Basketball";
            public string ScoreboardShape { get; set; } = "Rounded";
            public string EventTitle { get; set; } = "Game Title";
            public string HomeTeamName { get; set; } = "Home";
            public string AwayTeamName { get; set; } = "Away";
            public string HomeTeamLogo { get; set; } = "";
            public string AwayTeamLogo { get; set; } = "";
            public string BackgroundColor { get; set; } = "#1a1a1a";
            public string TextColor { get; set; } = "#ffffff";
            public string BorderColor { get; set; } = "#333333";
            public string HeaderColor { get; set; } = "#333333";
            public string HomeTeamBackgroundColor { get; set; } = "#3366cc";
            public string HomeTeamTextColor { get; set; } = "#ffffff";
            public string AwayTeamBackgroundColor { get; set; } = "#cc3333";
            public string AwayTeamTextColor { get; set; } = "#ffffff";
            public bool ShowTimeouts { get; set; } = true;
            public bool ShowAdditionalStats { get; set; } = true;
            public string LivestreamId { get; set; } = null;
        }

        private void InitializeSportDefaults()
        {
            switch (SportType)
            {
                case "Basketball":
                    _gameClock = 12 * 60; // 12 minutes
                    _shotClock = 24; // 24 seconds
                    MaxPeriods = 4;
                    MaxTimeouts = 4;
                    ShowTimeouts = true;
                    break;
                case "Football":
                    _gameClock = 15 * 60; // 15 minutes
                    _shotClock = 40; // Play clock
                    MaxPeriods = 4;
                    MaxTimeouts = 3;
                    ShowTimeouts = true;
                    break;
                case "Soccer":
                    _gameClock = 45 * 60; // 45 minutes
                    MaxPeriods = 2;
                    MaxTimeouts = 0;
                    ShowTimeouts = false;
                    break;
                case "Hockey":
                    _gameClock = 20 * 60; // 20 minutes
                    MaxPeriods = 3;
                    MaxTimeouts = 1;
                    ShowTimeouts = true;
                    break;
                case "Baseball":
                    _gameClock = 0; // No clock
                    MaxPeriods = 9; // Innings
                    CurrentInning = 1;
                    TopInning = true;
                    MaxTimeouts = 0;
                    ShowTimeouts = false;
                    break;
            }

            // Reset the input field for the clock
            TimeSpan time = TimeSpan.FromSeconds(_gameClock);
            _gameClockInput = $"{time.Minutes}:{time.Seconds:D2}";

            StateHasChanged();
        }

        private void OnGameClockTick(object sender, ElapsedEventArgs e)
        {
            if (!_gameClockRunning) return;

            _gameClock -= 1;

            if (_gameClock <= 0)
            {
                _gameClock = 0;
                _gameClockRunning = false;
                _gameClockTimer.Stop();

                // Automatically advance to next period if clock reaches 0
                if (CurrentPeriod < MaxPeriods)
                {
                    CurrentPeriod++;
                    ResetGameClock();
                }
            }

            InvokeAsync(StateHasChanged);
        }

        private void OnShotClockTick(object sender, ElapsedEventArgs e)
        {
            if (!_shotClockRunning) return;

            _shotClock -= 1;

            // Enable warning when shot clock is low
            _shotClockWarning = _shotClock <= 5;

            if (_shotClock <= 0)
            {
                _shotClock = 0;
                _shotClockRunning = false;
                _shotClockTimer.Stop();
            }

            InvokeAsync(StateHasChanged);
        }

        public void ToggleGameClock()
        {
            _gameClockRunning = !_gameClockRunning;

            if (_gameClockRunning)
            {
                _gameClockTimer.Start();
                if (SportType == "Basketball" || SportType == "Football")
                {
                    _shotClockRunning = true;
                    _shotClockTimer.Start();
                }
            }
            else
            {
                _gameClockTimer.Stop();
                _shotClockTimer.Stop();
                _shotClockRunning = false;
            }
        }

        public void ResetGameClock()
        {
            switch (SportType)
            {
                case "Basketball":
                    _gameClock = 12 * 60;
                    _shotClock = 24;
                    break;
                case "Football":
                    _gameClock = 15 * 60;
                    _shotClock = 40;
                    break;
                case "Soccer":
                    _gameClock = 45 * 60;
                    break;
                case "Hockey":
                    _gameClock = 20 * 60;
                    break;
            }

            _gameClockRunning = false;
            _shotClockRunning = false;
            _gameClockTimer.Stop();
            _shotClockTimer.Stop();
        }

        public void AdjustGameClock(int seconds)
        {
            _gameClock += seconds;

            // Don't allow negative time
            if (_gameClock < 0)
                _gameClock = 0;
        }

        public async Task UpdateScore(string team, int points)
        {
            if (team == "home")
            {
                HomeTeamScore += points;
                if (HomeTeamScore < 0) HomeTeamScore = 0;
            }
            else
            {
                AwayTeamScore += points;
                if (AwayTeamScore < 0) AwayTeamScore = 0;
            }

            // If connected to a livestream, update the database
            if (_syncWithLivestream && _livestreamId.HasValue)
            {
                await UpdateLivestreamScore();
            }
        }

        private async Task UpdateLivestreamScore()
        {
            try
            {
                var updateDto = new UpdateLivestreamDto
                {
                    HomeScore = HomeTeamScore,
                    AwayScore = AwayTeamScore
                };

                await LivestreamService.UpdateAsync(_livestreamId.Value, updateDto);
                _lastScoreSync = DateTime.Now;

                // Notify other instances of the scoreboard that the score has changed
                if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.SendAsync("UpdateScore", _livestreamId.ToString(), HomeTeamScore, AwayTeamScore);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating livestream score: " + ex.Message);
            }
        }

        public void UpdateTimeouts(string team, int change)
        {
            if (team == "home")
            {
                HomeTimeouts += change;
                if (HomeTimeouts < 0) HomeTimeouts = 0;
                if (HomeTimeouts > MaxTimeouts) HomeTimeouts = MaxTimeouts;
            }
            else
            {
                AwayTimeouts += change;
                if (AwayTimeouts < 0) AwayTimeouts = 0;
                if (AwayTimeouts > MaxTimeouts) AwayTimeouts = MaxTimeouts;
            }
        }

        public void IncrementPeriod()
        {
            if (CurrentPeriod < MaxPeriods)
            {
                CurrentPeriod++;

                if (SportType == "Baseball")
                {
                    if (TopInning)
                    {
                        TopInning = false;
                    }
                    else
                    {
                        TopInning = true;
                        CurrentInning++;
                    }
                }
            }
        }

        public void DecrementPeriod()
        {
            if (CurrentPeriod > 1)
            {
                CurrentPeriod--;

                if (SportType == "Baseball")
                {
                    if (!TopInning)
                    {
                        TopInning = true;
                    }
                    else if (CurrentInning > 1)
                    {
                        TopInning = false;
                        CurrentInning--;
                    }
                }
            }
        }

        public void TogglePossession()
        {
            PossessionTeam = PossessionTeam == "home" ? "away" : "home";
        }

        public void SetGameClockFromInput()
        {
            if (string.IsNullOrWhiteSpace(_gameClockInput)) return;

            var match = Regex.Match(_gameClockInput, @"^(\d+):(\d{1,2})$");
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[1].Value);
                int seconds = int.Parse(match.Groups[2].Value);
                _gameClock = (minutes * 60) + seconds;
            }
        }

        public string GetPeriodLabel()
        {
            return SportType switch
            {
                "Basketball" => "Quarter",
                "Football" => "Quarter",
                "Soccer" => "Half",
                "Hockey" => "Period",
                "Baseball" => "Inning",
                _ => "Period"
            };
        }

        public async Task OnSportTypeChanged(ChangeEventArgs e)
        {
            SportType = e.Value.ToString();
            InitializeSportDefaults();
            await Task.CompletedTask;
        }

        public async Task OnShapeChanged(ChangeEventArgs e)
        {
            ScoreboardShape = e.Value.ToString();
            await Task.CompletedTask;
        }

        public async Task SaveSettings()
        {
            var settings = new Dictionary<string, object>
            {
                { "SportType", SportType },
                { "ScoreboardShape", ScoreboardShape },
                { "EventTitle", EventTitle },
                { "HomeTeamName", HomeTeamName },
                { "AwayTeamName", AwayTeamName },
                { "HomeTeamScore", HomeTeamScore },
                { "AwayTeamScore", AwayTeamScore },
                { "HomeTeamLogo", HomeTeamLogo },
                { "AwayTeamLogo", AwayTeamLogo },
                { "CurrentPeriod", CurrentPeriod },
                { "GameClock", _gameClock },
                { "BackgroundColor", BackgroundColor },
                { "TextColor", TextColor },
                { "BorderColor", BorderColor },
                { "HeaderColor", HeaderColor },
                { "HomeTeamBackgroundColor", HomeTeamBackgroundColor },
                { "HomeTeamTextColor", HomeTeamTextColor },
                { "AwayTeamBackgroundColor", AwayTeamBackgroundColor },
                { "AwayTeamTextColor", AwayTeamTextColor },
                { "ShowTimeouts", ShowTimeouts },
                { "ShowAdditionalStats", ShowAdditionalStats }
            };

            var json = JsonSerializer.Serialize(settings);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "scoreboardSettings", json);

            await JSRuntime.InvokeVoidAsync("alert", "Settings saved successfully!");
        }

        public async Task LoadSettings()
        {
            var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "scoreboardSettings");

            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                SportType = GetValue(settings, "SportType", SportType);
                ScoreboardShape = GetValue(settings, "ScoreboardShape", ScoreboardShape);
                EventTitle = GetValue(settings, "EventTitle", EventTitle);
                HomeTeamName = GetValue(settings, "HomeTeamName", HomeTeamName);
                AwayTeamName = GetValue(settings, "AwayTeamName", AwayTeamName);
                HomeTeamScore = GetValueInt(settings, "HomeTeamScore", HomeTeamScore);
                AwayTeamScore = GetValueInt(settings, "AwayTeamScore", AwayTeamScore);
                HomeTeamLogo = GetValue(settings, "HomeTeamLogo", HomeTeamLogo);
                AwayTeamLogo = GetValue(settings, "AwayTeamLogo", AwayTeamLogo);
                CurrentPeriod = GetValueInt(settings, "CurrentPeriod", CurrentPeriod);
                _gameClock = GetValueDouble(settings, "GameClock", _gameClock);
                BackgroundColor = GetValue(settings, "BackgroundColor", BackgroundColor);
                TextColor = GetValue(settings, "TextColor", TextColor);
                BorderColor = GetValue(settings, "BorderColor", BorderColor);
                HeaderColor = GetValue(settings, "HeaderColor", HeaderColor);
                HomeTeamBackgroundColor = GetValue(settings, "HomeTeamBackgroundColor", HomeTeamBackgroundColor);
                HomeTeamTextColor = GetValue(settings, "HomeTeamTextColor", HomeTeamTextColor);
                AwayTeamBackgroundColor = GetValue(settings, "AwayTeamBackgroundColor", AwayTeamBackgroundColor);
                AwayTeamTextColor = GetValue(settings, "AwayTeamTextColor", AwayTeamTextColor);
                ShowTimeouts = GetValueBool(settings, "ShowTimeouts", ShowTimeouts);
                ShowAdditionalStats = GetValueBool(settings, "ShowAdditionalStats", ShowAdditionalStats);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading settings: " + ex.Message);
            }
        }

        private string GetValue(Dictionary<string, JsonElement> dict, string key, string defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        private int GetValueInt(Dictionary<string, JsonElement> dict, string key, int defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value.TryGetInt32(out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private double GetValueDouble(Dictionary<string, JsonElement> dict, string key, double defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value.TryGetDouble(out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private bool GetValueBool(Dictionary<string, JsonElement> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                try
                {
                    if (value.ValueKind == JsonValueKind.True)
                        return true;
                    if (value.ValueKind == JsonValueKind.False)
                        return false;
                }
                catch
                {
                    // In case of error, return the default value
                }
                return defaultValue;
            }
            return defaultValue;
        }

        public string GetOverlayUrl()
        {
            if (_syncWithLivestream && _livestreamId.HasValue)
            {
                var uri = NavigationManager.ToAbsoluteUri($"/scoreboard/overlay?admin=false&livestreamId={_livestreamId}");
                return uri.ToString();
            }

            if (!string.IsNullOrEmpty(ScoreboardId))
            {
                var uri = NavigationManager.ToAbsoluteUri($"/scoreboard/overlay?admin=false&id={ScoreboardId}");
                return uri.ToString();
            }

            var baseUri = NavigationManager.ToAbsoluteUri("/scoreboard/overlay");
            return $"{baseUri}?admin=false&sport={SportType}&shape={ScoreboardShape}";
        }

        public async Task CopyOverlayUrl()
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", GetOverlayUrl());
            await JSRuntime.InvokeVoidAsync("alert", "URL copied to clipboard!");
        }

        public async ValueTask DisposeAsync()
        {
            _gameClockTimer?.Dispose();
            _shotClockTimer?.Dispose();
            _autoRefreshTimer?.Dispose();

            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
            }
        }

        public void Dispose()
        {
            _gameClockTimer?.Dispose();
            _shotClockTimer?.Dispose();
            _autoRefreshTimer?.Dispose();
        }
    }
}
