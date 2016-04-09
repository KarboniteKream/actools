﻿using FirstFloor.ModernUI.Windows.Navigation;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Presentation {
    public class AppearanceManager : NotifyPropertyChanged {
        public static readonly Uri DarkThemeSource = new Uri("/FirstFloor.ModernUI;component/Assets/ModernUI.Dark.xaml", UriKind.Relative);
        public static readonly Uri LightThemeSource = new Uri("/FirstFloor.ModernUI;component/Assets/ModernUI.Light.xaml", UriKind.Relative);

        public static readonly Uri FixedToolBarsSource = new Uri("/AcManager.Controls;component/Assets/SelectedObjectToolBarTray/Fixed.xaml", UriKind.Relative);
        public static readonly Uri PopupToolBarsSource = new Uri("/AcManager.Controls;component/Assets/SelectedObjectToolBarTray/Popup.xaml", UriKind.Relative);

        public const string KeyAccentColor = "AccentColor";
        public const string KeyAccent = "Accent";
        public const string KeyDefaultFontSize = "DefaultFontSize";
        public const string KeyFixedFontSize = "FixedFontSize";
        public const string KeySubMenuFontSize = "ModernSubMenuFontSize";
        
        private AppearanceManager() {
        }

        public void Initialize() {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/FirstFloor.ModernUI;component/Assets/ModernUI.xaml", UriKind.Relative)
            });
        }

        private ResourceDictionary GetThemeDictionary() {
            // determine the current theme by looking at the app resources and return the first dictionary having the resource key 'WindowBackground' defined.
            return (from dict in Application.Current.Resources.MergedDictionaries
                    where dict.Contains("WindowBackground")
                    select dict).FirstOrDefault();
        }

        private void ApplyAccentColor(Color accentColor) {
            // set accent color and brush resources
            Application.Current.Resources[KeyAccentColor] = accentColor;
            Application.Current.Resources[KeyAccent] = new SolidColorBrush(accentColor);
        }

        public static AppearanceManager Current { get; } = new AppearanceManager();

        private bool _themeSkipAccentColor;
        public Uri ThemeSource {
            get { return GetThemeDictionary()?.Source; }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));

                var oldThemeDict = GetThemeDictionary();
                var dictionaries = Application.Current.Resources.MergedDictionaries;
                var themeDict = new ResourceDictionary { Source = value };

                // if theme defines an accent color, use it
                var accentColor = themeDict[KeyAccentColor] as Color?;
                if (accentColor.HasValue) {
                    themeDict.Remove(KeyAccentColor);
                    if (!_themeSkipAccentColor) {
                        ApplyAccentColor(accentColor.Value);
                    }
                }

                _themeSkipAccentColor = false;
                dictionaries.Add(themeDict);
                
                if (oldThemeDict != null) {
                    dictionaries.Remove(oldThemeDict);
                }

                OnPropertyChanged(nameof(ThemeSource));
            }
        }

        public FontSize FontSize {
            get { return Equals(Application.Current.Resources[KeyDefaultFontSize] as double? ?? 0d, 12D) ? FontSize.Small : FontSize.Large; }
            set {
                if (FontSize == value) return;
                Application.Current.Resources[KeyDefaultFontSize] = value == FontSize.Small ? 12D : 13D;
                Application.Current.Resources[KeyFixedFontSize] = value == FontSize.Small ? 10.667D : 13.333D;
                OnPropertyChanged(nameof(FontSize));
            }
        }

        public FontSize SubMenuFontSize {
            get { return Equals(Application.Current.Resources[KeySubMenuFontSize] as double? ?? 0d, 11D) ? FontSize.Small : FontSize.Large; }
            set {
                if (SubMenuFontSize == value) return;
                Application.Current.Resources[KeySubMenuFontSize] = value == FontSize.Small ? 11D : 14D;
                OnPropertyChanged();
            }
        }

        public Color AccentColor {
            get { return Application.Current.Resources[KeyAccentColor] as Color? ?? Color.FromArgb(0xff, 0x1b, 0xa1, 0xe2); }
            set {
                ApplyAccentColor(value);

                var themeSource = ThemeSource;
                if (themeSource != null) {
                    _themeSkipAccentColor = true;
                    ThemeSource = themeSource;
                }

                OnPropertyChanged();
            }
        }

        private ResourceDictionary _toolBarModeDictionary;

        public bool? PopupToolBars {
            get { return _toolBarModeDictionary == null ? (bool?)null : _toolBarModeDictionary.Source == PopupToolBarsSource; }
            set {
                if (Equals(value, PopupToolBars)) return;
                OnPropertyChanged();

                if (_toolBarModeDictionary != null) {
                    Application.Current.Resources.MergedDictionaries.Remove(_toolBarModeDictionary);
                    _toolBarModeDictionary = null;
                }

                if (!value.HasValue) return;
                _toolBarModeDictionary = new ResourceDictionary { Source = value.Value ? PopupToolBarsSource : FixedToolBarsSource };
                Application.Current.Resources.MergedDictionaries.Add(_toolBarModeDictionary);
            }
        }
    }
}