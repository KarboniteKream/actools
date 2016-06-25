﻿using System.Linq;
using System.Windows.Media;
using AcManager.Controls.Helpers;
using AcManager.Controls.Presentation;
using AcManager.Tools.Helpers;
using FirstFloor.ModernUI.Presentation;

namespace AcManager.Pages.Settings {
    public partial class SettingsAppearance {
        public SettingsAppearance() {
            InitializeComponent();
            DataContext = new AppearanceViewModel();
        }

        public class AppearanceViewModel : NotifyPropertyChanged {
            private static BitmapScalingMode? _originalScalingMode;

            public FancyBackgroundManager FancyBackgroundManager => FancyBackgroundManager.Instance;

            public AppAppearanceManager AppAppearanceManager => AppAppearanceManager.Instance;

            internal AppearanceViewModel() {
                if (!_originalScalingMode.HasValue) {
                    _originalScalingMode = BitmapScaling.Value;
                }

                BitmapScaling = BitmapScalings.FirstOrDefault(x => x.Value == AppAppearanceManager.BitmapScalingMode) ?? BitmapScalings.First();
                TextFormatting = AppAppearanceManager.IdealFormattingMode ? TextFormattings[1] : TextFormattings[0];
            }

            public class BitmapScalingEntry : Displayable {
                public BitmapScalingMode Value { get; set; }
            }

            private bool _bitmapScalingRestartRequired;

            public bool BitmapScalingRestartRequired {
                get { return _bitmapScalingRestartRequired; }
                set {
                    if (Equals(value, _bitmapScalingRestartRequired)) return;
                    _bitmapScalingRestartRequired = value;
                    OnPropertyChanged();
                }
            }

            private RelayCommand _restartCommand;

            public RelayCommand RestartCommand => _restartCommand ?? (_restartCommand = new RelayCommand(o => {
                WindowsHelper.RestartCurrentApplication();
            }));

            private BitmapScalingEntry _bitmapScaling;

            public BitmapScalingEntry BitmapScaling {
                get { return _bitmapScaling; }
                set {
                    if (Equals(value, _bitmapScaling)) return;
                    _bitmapScaling = value;
                    OnPropertyChanged();

                    if (value != null) {
                        AppAppearanceManager.BitmapScalingMode = value.Value;
                        BitmapScalingRestartRequired = value.Value != _originalScalingMode;
                    }
                }
            }

            public BitmapScalingEntry[] BitmapScalings { get; } = {
                new BitmapScalingEntry { DisplayName = "Low", Value = BitmapScalingMode.NearestNeighbor },
                new BitmapScalingEntry { DisplayName = "Normal", Value = BitmapScalingMode.LowQuality },
                new BitmapScalingEntry { DisplayName = "High", Value = BitmapScalingMode.HighQuality }
            };

            private Displayable _textFormatting;

            public Displayable TextFormatting {
                get { return _textFormatting; }
                set {
                    if (Equals(value, _textFormatting)) return;
                    _textFormatting = value;
                    OnPropertyChanged();
                    AppAppearanceManager.IdealFormattingMode = value == TextFormattings[1];
                }
            }

            public Displayable[] TextFormattings { get; } = {
                new Displayable { DisplayName = "Subpixel" },
                new Displayable { DisplayName = "Ideal" },
            };
        }
    }
}
