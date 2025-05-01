using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Pages.Scoreboard
{
    public partial class ScoreboardOverlay : web_backendComponentBase
    {
        private Timer _gameClockTimer;
        private Timer _shotClockTimer;
        private double _gameClock = 12 * 60; // Default 12 minutes
        private double _shotClock = 24; // Default 24 seconds
        private bool _gameClockRunning = false;
        private bool _shotClockRunning = false;
        private bool _shotClockWarning = false;
        private string _gameClockInput = "12:00";

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
        public string? ScoreboardId { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

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
            _gameClockTimer = new Timer(1000);
            _gameClockTimer.Elapsed += OnGameClockTick;

            _shotClockTimer = new Timer(1000);
            _shotClockTimer.Elapsed += OnShotClockTick;

            // Check if we have a scoreboard ID to load
            if (!string.IsNullOrEmpty(ScoreboardId))
            {
                await LoadScoreboardById(ScoreboardId);
            }
            else
            {
                // Otherwise load from default settings if available
                await LoadSettings();
            }
            
            InitializeSportDefaults();
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
                    }
                }
                catch (Exception ex)
                {
                    await JSRuntime.InvokeVoidAsync("console.error", "Error loading saved scoreboard: " + ex.Message);
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

        public void UpdateScore(string team, int points)
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
                await JSRuntime.InvokeVoidAsync("console.error", "Error loading settings: " + ex.Message);
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
            var uri = NavigationManager.ToAbsoluteUri($"/scoreboard/overlay?admin=false&sport={SportType}&shape={ScoreboardShape}");
            return uri.ToString();
        }

        public async Task CopyOverlayUrl()
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", GetOverlayUrl());
            await JSRuntime.InvokeVoidAsync("alert", "URL copied to clipboard!");
        }

        public void Dispose()
        {
            _gameClockTimer?.Dispose();
            _shotClockTimer?.Dispose();
        }
    }
}