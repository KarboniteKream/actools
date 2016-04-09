﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AcManager.Controls;
using AcManager.Controls.Helpers;
using AcManager.Controls.ViewModels;
using AcManager.Pages.Dialogs;
using AcManager.Tools.Data;
using AcManager.Tools.Helpers;
using AcManager.Tools.Lists;
using AcManager.Tools.Managers;
using AcManager.Tools.Objects;
using AcManager.Tools.SemiGui;
using AcTools.Processes;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Navigation;

namespace AcManager.Pages.Drive {
    public partial class QuickDrive {
        public const string UserPresetableKeyValue = "Quick Drive";

        private readonly QuickDriveViewModel _model;

        public QuickDrive() {
            InitializeComponent();
            DataContext = _model = new QuickDriveViewModel();
        }

        private DispatcherTimer _realConditionsTimer;

        private void QuickDrive_Loaded(object sender, RoutedEventArgs e) {
            _realConditionsTimer = new DispatcherTimer();
            _realConditionsTimer.Tick += (o, args) => {
                if (_model.RealConditions) {
                    _model.TryToSetRealConditions();
                }
            };
            _realConditionsTimer.Interval = new TimeSpan(0, 0, 60);
            _realConditionsTimer.Start();
        }

        private void QuickDrive_Unloaded(object sender, RoutedEventArgs e) {
            _realConditionsTimer.Stop();
        }

        private void ModeTab_OnFrameNavigated(object sender, NavigationEventArgs e) {
            _model.SelectedModeViewModel = (ModeTab.Frame.Content as IQuickDriveModeControl)?.Model;
        }

        private void Label_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount != 1) return;

            var label = (Label)sender;
            Keyboard.Focus(label.Target);
        }

        private void AssistsMore_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            new AssistsDialog(_model.AssistsViewModel).ShowDialog();
        }

        public class QuickDriveViewModel : NotifyPropertyChanged, IUserPresetable {
            #region Notifieable Stuff
            private Uri _selectedMode;
            private CarObject _selectedCar;
            private TrackBaseObject _selectedTrack;
            private WeatherObject _selectedWeather;
            private bool _realConditions,
                _isTimeClamped, _isTemperatureClamped, _isWeatherNotSupported,
                _realConditionsTimezones, _realConditionsLighting;
            private double _temperature;
            private int _time;

            public Uri SelectedMode {
                get { return _selectedMode; }
                set {
                    if (Equals(value, _selectedMode)) return;
                    _selectedMode = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public CarObject SelectedCar {
                get { return _selectedCar; }
                set {
                    if (Equals(value, _selectedCar)) return;
                    _selectedCar = value;
                    // _selectedCar?.LoadSkins();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GoCommand));
                    OnSelectedUpdated();
                    SaveLater();
                }
            }

            public BindingList<Game.TrackPropertiesPreset> TrackPropertiesPresets => Game.DefaultTrackPropertiesPresets;

            public Game.TrackPropertiesPreset SelectedTrackPropertiesPreset {
                get { return _selectedTrackPropertiesPreset; }
                set {
                    if (Equals(value, _selectedTrackPropertiesPreset)) return;
                    _selectedTrackPropertiesPreset = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public TrackBaseObject SelectedTrack {
                get { return _selectedTrack; }
                set {
                    if (Equals(value, _selectedTrack)) return;
                    _selectedTrack = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GoCommand));
                    OnSelectedUpdated();

                    _selectedTrackGeoTags = null;
                    _selectedTrackTimeZone = null;
                    RealWeather = null;
                    SaveLater();

                    if (RealConditions) {
                        TryToSetRealConditions();
                    }

                    FancyBackgroundManager.Instance.ChangeBackground(value?.PreviewImage);
                }
            }

            public WeatherObject SelectedWeather {
                get { return _selectedWeather; }
                set {
                    if (Equals(value, _selectedWeather)) return;
                    _selectedWeather = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RoadTemperature));

                    if (!RealConditions) {
                        SaveLater();
                    }
                }
            }

            public bool RealConditions {
                get { return _realConditions; }
                set {
                    if (value == _realConditions) return;
                    _realConditions = value;

                    if (value) {
                        TryToSetRealConditions();
                    } else {
                        IsTimeClamped = IsTemperatureClamped =
                            IsWeatherNotSupported = false;
                        RealWeather = null;
                    }

                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public bool IsTimeClamped {
                get { return _isTimeClamped; }
                set {
                    if (value == _isTimeClamped) return;
                    _isTimeClamped = value;
                    OnPropertyChanged();
                }
            }

            public bool IsTemperatureClamped {
                get { return _isTemperatureClamped; }
                set {
                    if (value == _isTemperatureClamped) return;
                    _isTemperatureClamped = value;
                    OnPropertyChanged();
                }
            }

            public bool IsWeatherNotSupported {
                get { return _isWeatherNotSupported; }
                set {
                    if (value == _isWeatherNotSupported) return;
                    _isWeatherNotSupported = value;
                    OnPropertyChanged();
                }
            }

            public bool RealConditionsTimezones {
                get { return _realConditionsTimezones; }
                set {
                    if (value == _realConditionsTimezones) return;
                    _realConditionsTimezones = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public bool RealConditionsLighting {
                get { return _realConditionsLighting; }
                set {
                    if (value == _realConditionsLighting) return;
                    _realConditionsLighting = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            // default limit: 10/36
            public double TemperatureMinimum => 0.0;
            public double TemperatureMaximum => 36.0;
            public double Temperature {
                get { return _temperature; }
                set {
                    value = MathUtils.Round(value, 0.5);
                    if (Equals(value, _temperature)) return;
                    _temperature = MathUtils.Clamp(value, TemperatureMinimum, TemperatureMaximum);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RoadTemperature));

                    if (!RealConditions) {
                        SaveLater();
                    }
                }
            }

            public double RoadTemperature => Game.ConditionProperties.GetRoadTemperature(Time, Temperature,
                    SelectedWeather?.TemperatureCoefficient ?? 0.0);

            public int TimeMinimum => 8 * 60 * 60;
            public int TimeMaximum => 18 * 60 * 60;
            public int Time {
                get { return _time; }
                set {
                    if (value == _time) return;
                    _time = MathUtils.Clamp(value, TimeMinimum, TimeMaximum);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayTime));
                    OnPropertyChanged(nameof(RoadTemperature));

                    if (!RealConditions) {
                        SaveLater();
                    }
                }
            }

            public string DisplayTime {
                get { return $"{_time / 60 / 60:D2}:{_time / 60 % 60:D2}"; }
                set {
                    int time;
                    if (!FlexibleParser.TryParseTime(value, out time)) return;
                    Time = time;
                }
            }

            public int TimeMultiplerMinimum => 1;
            public int TimeMultiplerMaximum => 360;
            public int TimeMultipler {
                get { return _timeMultipler; }
                set {
                    if (value == _timeMultipler) return;
                    _timeMultipler = MathUtils.Clamp(value, TimeMultiplerMinimum, TimeMultiplerMaximum);
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public WeatherDescription RealWeather {
                get { return _realWeather; }
                set {
                    if (Equals(value, _realWeather)) return;
                    _realWeather = value;
                    OnPropertyChanged();
                }
            }

            public AcLoadedOnlyCollection<WeatherObject> WeatherList => WeatherManager.Instance.LoadedOnlyCollection;
            #endregion

            private GeoTagsEntry _selectedTrackGeoTags;
            private static readonly GeoTagsEntry InvalidGeoTagsEntry = new GeoTagsEntry("", "");
            private TimeZoneInfo _selectedTrackTimeZone;
            private static readonly TimeZoneInfo InvalidTimeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("_", TimeSpan.Zero,
                                                                                                         "", "");

            private class SaveableData {
                public Uri Mode;
                public string ModeData, CarId, TrackId, WeatherId, TrackPropertiesPreset;
                public bool RealConditions, RealConditionsTimezones, RealConditionsLighting;
                public double Temperature;
                public int Time, TimeMultipler;
            }

            private readonly ISaveHelper _saveable;

            private void SaveLater() {
                _saveable.SaveLater();
                Changed?.Invoke(this, EventArgs.Empty);
            }

            internal QuickDriveViewModel() {
                (_saveable = new SaveHelper<SaveableData>("__QuickDrive_Main", () => new SaveableData {
                    RealConditions = RealConditions,
                    RealConditionsTimezones = RealConditionsTimezones,
                    RealConditionsLighting = RealConditionsLighting,

                    Mode = SelectedMode,
                    ModeData = SelectedModeViewModel?.ToSerializedString(),

                    CarId = SelectedCar?.Id,
                    TrackId = SelectedTrack?.IdWithLayout,
                    WeatherId = SelectedWeather?.Id,
                    TrackPropertiesPreset = SelectedTrackPropertiesPreset.Name,

                    Temperature = Temperature,
                    Time = Time,
                    TimeMultipler = TimeMultipler,
                }, o => {
                    RealConditions = o.RealConditions;
                    RealConditionsTimezones = o.RealConditionsTimezones;
                    RealConditionsLighting = o.RealConditionsLighting;

                    if (o.Mode != null) SelectedMode = o.Mode ?? SelectedMode;
                    if (o.ModeData != null) SelectedModeViewModel?.FromSerializedString(o.ModeData);

                    if (o.CarId != null) SelectedCar = CarsManager.Instance.GetById(o.CarId) ?? SelectedCar;
                    if (o.TrackId != null) SelectedTrack = TracksManager.Instance.GetLayoutById(o.TrackId) ?? SelectedTrack;
                    if (o.WeatherId != null) SelectedWeather = WeatherManager.Instance.GetById(o.WeatherId) ?? SelectedWeather;
                    if (o.TrackPropertiesPreset != null) SelectedTrackPropertiesPreset =
                            Game.DefaultTrackPropertiesPresets.FirstOrDefault(x => x.Name == o.TrackPropertiesPreset) ?? SelectedTrackPropertiesPreset;

                    Temperature = o.Temperature;
                    Time = o.Time;
                    TimeMultipler = o.TimeMultipler;
                }, () => {
                    RealConditionsTimezones = false;
                    RealConditionsLighting = false;
                    RealConditions = false;

                    SelectedMode = new Uri("/Pages/Drive/QuickDrive_Race.xaml", UriKind.Relative);
                    SelectedCar = CarsManager.Instance.GetDefault();
                    SelectedTrack = TracksManager.Instance.GetDefault();
                    SelectedWeather = WeatherManager.Instance.GetDefault();
                    SelectedTrackPropertiesPreset = Game.GetDefaultTrackPropertiesPreset();

                    Temperature = 12.0;
                    Time = 12 * 60 * 60;
                    TimeMultipler = 1;
                })).Init();
            }

            #region Presets
            bool IUserPresetable.CanBeSaved => true;

            string IUserPresetable.UserPresetableKey => UserPresetableKeyValue;

            string IUserPresetable.ExportToUserPresetData() {
                return _saveable.ToSerializedString();
            }

            public event EventHandler Changed;

            void IUserPresetable.ImportFromUserPresetData(string data) {
                _saveable.FromSerializedString(data);
            }
            #endregion

            public AssistsViewModel AssistsViewModel => AssistsViewModel.Instance;

            private bool _realConditionsInProcess;

            public async void TryToSetRealConditions() {
                if (_realConditionsInProcess || !RealConditions) return;
                _realConditionsInProcess = true;

                if (_selectedTrackGeoTags == null) {
                    var geoTags = SelectedTrack.GeoTags;
                    if (geoTags == null || geoTags.IsEmptyOrInvalid) {
                        geoTags = await Task.Run(() => TracksLocator.TryToLocate(SelectedTrack));
                        if (!RealConditions) {
                            _realConditionsInProcess = false;
                            return;
                        }

                        if (geoTags == null) {
                            // TODO: Informing
                            geoTags = InvalidGeoTagsEntry;
                        }
                    }

                    _selectedTrackGeoTags = geoTags;
                }

                TryToSetRealTime();
                TryToSetRealWeather();

                _realConditionsInProcess = false;
            }

            #region Real Time
            private const int SecondsPerDay = 24 * 60 * 60;
            private bool _realTimeInProcess;
            private Game.TrackPropertiesPreset _selectedTrackPropertiesPreset;

            private async void TryToSetRealTime() {
                if (_realTimeInProcess || !RealConditions) return;
                _realTimeInProcess = true;

                var now = DateTime.Now;
                var time = now.Hour * 60 * 60 + now.Minute * 60 + now.Second;

                if (_selectedTrackGeoTags == null || _selectedTrackGeoTags == InvalidGeoTagsEntry) {
                    TryToSetTime(time);
                    return;
                }

                if (_selectedTrackTimeZone == null) {
                    var timeZone = await Task.Run(() => TimeZoneDeterminer.TryToDetermine(_selectedTrackGeoTags));
                    if (!RealConditions) {
                        _realTimeInProcess = false;
                        return;
                    }

                    if (timeZone == null) {
                        // TODO: Informing
                        timeZone = InvalidTimeZoneInfo;
                    }

                    _selectedTrackTimeZone = timeZone;
                }

                if (_selectedTrackTimeZone == null || ReferenceEquals(_selectedTrackTimeZone, InvalidTimeZoneInfo)) {
                    TryToSetTime(time);
                    return;
                }

                time += (int)(_selectedTrackTimeZone.BaseUtcOffset.TotalSeconds - TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds);
                time = (time + SecondsPerDay) % SecondsPerDay;

                TryToSetTime(time);
            }

            private void TryToSetTime(int value) {
                var clamped = MathUtils.Clamp(value, TimeMinimum, TimeMaximum);
                IsTimeClamped = clamped != value;
                Time = clamped;
                _realTimeInProcess = false;
            }
            #endregion

            #region Real Weather
            private bool _realWeatherInProcess;
            private WeatherDescription _realWeather;
            private int _timeMultipler;

            private async void TryToSetRealWeather() {
                if (_realWeatherInProcess || !RealConditions) return;

                if (_selectedTrackGeoTags == null || _selectedTrackGeoTags == InvalidGeoTagsEntry) {
                    return;
                }

                _realWeatherInProcess = true;

                var weather = await Task.Run(() => WeatherProvider.TryToGetWeather(_selectedTrackGeoTags));
                if (!RealConditions) {
                    _realWeatherInProcess = true;
                    return;
                }

                if (weather != null) {
                    RealWeather = weather;
                    TryToSetTemperature(weather.Temperature);
                    await TryToSetWeatherType(weather.Type);
                }

                _realWeatherInProcess = false;
            }

            private void TryToSetTemperature(double value) {
                var clamped = MathUtils.Clamp(value, TemperatureMinimum, TemperatureMaximum);
                IsTemperatureClamped = value < TemperatureMinimum || value > TemperatureMaximum;
                Temperature = clamped;
            }

            private bool _waitingForWeatherList;

            private async Task TryToSetWeatherType(WeatherDescription.WeatherType type) {
                if (_waitingForWeatherList) return;

                _waitingForWeatherList = true;
                await WeatherManager.Instance.EnsureLoadedAsync();
                var list = WeatherManager.Instance.LoadedOnly.ToList();
                _waitingForWeatherList = false;

                try {
                    var closest = WeatherDescription.FindClosestWeather(list.Select(x => x.WeatherType).Where(x => x.HasValue).Select(x => x.Value), type);
                    if (closest == null) {
                        IsWeatherNotSupported = true;
                    } else {
                        SelectedWeather = list.Where(x => x.WeatherType == closest).RandomElement();
                    }
                } catch (Exception e) {
                    IsWeatherNotSupported = true;
                    Logging.Warning("[QUICKDRIVE] FindClosestWeather exception: " + e);
                }
            }
            #endregion

            private ICommand _changeCarCommand;

            public ICommand ChangeCarCommand => _changeCarCommand ?? (_changeCarCommand = new RelayCommand(o => {
                var dialog = new SelectCarDialog(SelectedCar);
                dialog.ShowDialog();
                if (!dialog.IsResultOk || dialog.SelectedCar == null) return;

                SelectedCar = dialog.SelectedCar;
                SelectedCar.SelectedSkin = dialog.SelectedSkin;
            }));

            private ICommand _changeTrackCommand;

            public ICommand ChangeTrackCommand => _changeTrackCommand ?? (_changeTrackCommand = new RelayCommand(o => {
                var dialog = new SelectTrackDialog(SelectedTrack);
                dialog.ShowDialog();
                if (!dialog.IsResultOk || dialog.Model.SelectedTrackConfiguration == null) return;

                SelectedTrack = dialog.Model.SelectedTrackConfiguration;
            }));

            private QuickDriveModeViewModel _selectedModeViewModel;

            private AsyncCommand _goCommand;

            public AsyncCommand GoCommand => _goCommand ?? (_goCommand =
                    new AsyncCommand(o => Go(), o => SelectedCar != null && SelectedTrack != null && SelectedModeViewModel != null));

            private async Task Go() {
                GoCommand.OnCanExecuteChanged();

                var selectedMode = SelectedModeViewModel;
                if (selectedMode == null) return;

                try {
                    await selectedMode.Drive(SelectedCar, SelectedTrack, AssistsViewModel.GameProperties, new Game.ConditionProperties {
                        AmbientTemperature = Temperature,
                        RoadTemperature = RoadTemperature,

                        SunAngle = Game.ConditionProperties.GetSunAngle(Time),
                        TimeMultipler = TimeMultipler,
                        CloudSpeed = 0.2,

                        WeatherName = SelectedWeather?.Id
                    }, SelectedTrackPropertiesPreset.Properties);
                } finally {
                    GoCommand.OnCanExecuteChanged();
                }
            }

            private void OnSelectedUpdated() {
                SelectedModeViewModel?.OnSelectedUpdated(SelectedCar, SelectedTrack);
            }

            public QuickDriveModeViewModel SelectedModeViewModel {
                get { return _selectedModeViewModel; }
                set {
                    if (Equals(value, _selectedModeViewModel)) return;
                    if (_selectedModeViewModel != null) {
                        _selectedModeViewModel.Changed -= SelectedModeViewModel_Changed;
                    }

                    _selectedModeViewModel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GoCommand));

                    if (_selectedModeViewModel != null) {
                        _selectedModeViewModel.Changed += SelectedModeViewModel_Changed;
                    }
                    OnSelectedUpdated();
                }
            }

            private void SelectedModeViewModel_Changed(object sender, EventArgs e) {
                Changed?.Invoke(this, new EventArgs());
            }
        }
    }

    public interface IQuickDriveModeControl {
        QuickDriveModeViewModel Model { get; }
    }

    public abstract class QuickDriveModeViewModel : NotifyPropertyChanged {
        protected ISaveHelper Saveable { set; private get; }

        public event EventHandler Changed;

        protected void SaveLater() {
            Saveable.SaveLater();
            Changed?.Invoke(this, new EventArgs());
        }

        public abstract Task Drive(CarObject selectedCar, TrackBaseObject selectedTrack,
            Game.AssistsProperties assistsProperties,
            Game.ConditionProperties conditionProperties, Game.TrackProperties trackProperties);

        protected async Task StartAsync(Game.StartProperties properties) {
            await GameWrapper.StartAsync(properties);
        }

        public virtual void OnSelectedUpdated(CarObject selectedCar, TrackBaseObject selectedTrack) {
        }

        public string ToSerializedString() {
            return Saveable.ToSerializedString();
        }

        public void FromSerializedString(string data) {
            Saveable.FromSerializedString(data);
        }
    }
}