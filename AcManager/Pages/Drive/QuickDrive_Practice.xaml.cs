﻿using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AcManager.Tools.Helpers;
using AcManager.Tools.Objects;
using AcTools.Processes;

namespace AcManager.Pages.Drive {
    public partial class QuickDrive_Practice : IQuickDriveModeControl {
        public class QuickDrive_PracticeViewModel : QuickDriveModeViewModel {
            private bool _penalties;
            private Game.StartType _selectedStartType;

            public bool Penalties {
                get { return _penalties; }
                set {
                    if (value == _penalties) return;
                    _penalties = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            public BindingList<Game.StartType> StartTypes => Game.StartType.Values;

            public Game.StartType SelectedStartType {
                get { return _selectedStartType; }
                set {
                    if (Equals(value, _selectedStartType)) return;
                    _selectedStartType = value;
                    OnPropertyChanged();
                    SaveLater();
                }
            }

            private class SaveableData {
                public bool Penalties;
                public string StartType;
            }

            public QuickDrive_PracticeViewModel() {
                (Saveable = new SaveHelper<SaveableData>("__QuickDrive_Practice", () => new SaveableData {
                    Penalties = Penalties,
                    StartType = SelectedStartType.Value
                }, o => {
                    Penalties = o.Penalties;
                    SelectedStartType = Game.StartType.Values.FirstOrDefault(x => x.Value == o.StartType) ?? Game.StartType.Pit;
                }, () => {
                    Penalties = true;
                    SelectedStartType = Game.StartType.Pit;
                })).Init();
            }

            public override async Task Drive(CarObject selectedCar, TrackBaseObject selectedTrack, Game.AssistsProperties assistsProperties,
                Game.ConditionProperties conditionProperties, Game.TrackProperties trackProperties) {
                await StartAsync(new Game.StartProperties {
                    BasicProperties = new Game.BasicProperties {
                        CarId = selectedCar.Id,
                        CarSkinId = selectedCar.SelectedSkin?.Id,
                        TrackId = selectedTrack.Id,
                        TrackConfigurationId = selectedTrack.LayoutId
                    },
                    AssistsProperties = assistsProperties,
                    ConditionProperties = conditionProperties,
                    TrackProperties = trackProperties,
                    ModeProperties = new Game.PracticeProperties {
                        Penalties = Penalties,
                        StartType = SelectedStartType
                    }
                });
            }
        }

        public QuickDrive_Practice() {
            InitializeComponent();
            DataContext = new QuickDrive_PracticeViewModel();
        }

        public QuickDriveModeViewModel Model => (QuickDrive_PracticeViewModel)DataContext;
    }
}