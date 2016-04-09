﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using AcManager.Annotations;
using AcManager.Controls;
using AcManager.Controls.Helpers;
using AcManager.Controls.Pages.Dialogs;
using AcManager.Pages.Dialogs;
using AcManager.Pages.Drive;
using AcManager.Pages.Lists;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Lists;
using AcManager.Tools.Managers;
using AcManager.Tools.Objects;
using AcTools.Kn5Render.Utils;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows;
using MenuItem = System.Windows.Controls.MenuItem;

namespace AcManager.Pages.Selected {
    public partial class SelectedCarPage : ILoadableContent, IParametrizedUriContent {
        public class SelectedCarPageViewModel : SelectedAcObjectViewModel<CarObject> {
            public SelectedCarPageViewModel([NotNull] CarObject acObject) : base(acObject) {}

            #region Open In Showroom
            private RelayCommand _openInShowroomCommand;

            public RelayCommand OpenInShowroomCommand => _openInShowroomCommand ?? (_openInShowroomCommand = new RelayCommand(o => {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ||
                        !CarOpenInShowroomDialog.Run(SelectedObject, SelectedObject.SelectedSkin?.Id)) {
                    OpenInShowroomOptionsCommand.Execute(null);
                }
            }, o => SelectedObject.Enabled && SelectedObject.SelectedSkin != null));

            private RelayCommand _openInShowroomOptionsCommand;

            public RelayCommand OpenInShowroomOptionsCommand => _openInShowroomOptionsCommand ?? (_openInShowroomOptionsCommand = new RelayCommand(o => {
                new CarOpenInShowroomDialog(SelectedObject, SelectedObject.SelectedSkin?.Id).ShowDialog();
            }, o => SelectedObject.Enabled && SelectedObject.SelectedSkin != null));

            private RelayCommand _openInCustomShowroomCommand;

            public RelayCommand OpenInCustomShowroomCommand => _openInCustomShowroomCommand ?? (_openInCustomShowroomCommand = new RelayCommand(o => {
                Kn5RenderWrapper.StartBrightRoomPreview(SelectedObject.Location, SelectedObject.SelectedSkin?.Id);
            }));
            #endregion

            #region Auto-Update Previews
            private ICommand _updatePreviewsCommand;

            public ICommand UpdatePreviewsCommand => _updatePreviewsCommand ?? (_updatePreviewsCommand = new RelayCommand(o => {
                new CarUpdatePreviewsDialog(SelectedObject, GetAutoUpdatePreviewsDialogMode()).ShowDialog();
            }, o => SelectedObject.Enabled));

            private ICommand _updatePreviewsManuallyCommand;

            public ICommand UpdatePreviewsManuallyCommand => _updatePreviewsManuallyCommand ?? (_updatePreviewsManuallyCommand = new RelayCommand(o => {
                new CarUpdatePreviewsDialog(SelectedObject, CarUpdatePreviewsDialog.DialogMode.StartManual).ShowDialog();
            }, o => SelectedObject.Enabled));

            private ICommand _updatePreviewsOptionsCommand;

            public ICommand UpdatePreviewsOptionsCommand => _updatePreviewsOptionsCommand ?? (_updatePreviewsOptionsCommand = new RelayCommand(o => {
                new CarUpdatePreviewsDialog(SelectedObject, CarUpdatePreviewsDialog.DialogMode.Options).ShowDialog();
            }, o => SelectedObject.Enabled));

            public static CarUpdatePreviewsDialog.DialogMode GetAutoUpdatePreviewsDialogMode() {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return CarUpdatePreviewsDialog.DialogMode.Options;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) return CarUpdatePreviewsDialog.DialogMode.StartManual;
                return CarUpdatePreviewsDialog.DialogMode.Start;
            }
            #endregion

            #region Presets
            public ObservableCollection<MenuItem> ShowroomPresets {
                get { return _showroomPresets; }
                private set {
                    if (Equals(value, _showroomPresets)) return;
                    _showroomPresets = value;
                    OnPropertyChanged();
                }
            }

            public ObservableCollection<MenuItem> UpdatePreviewsPresets {
                get { return _updatePreviewsPresets; }
                private set {
                    if (Equals(value, _updatePreviewsPresets)) return;
                    _updatePreviewsPresets = value;
                    OnPropertyChanged();
                }
            }

            public ObservableCollection<MenuItem> QuickDrivePresets {
                get { return _quickDrivePresets; }
                private set {
                    if (Equals(value, _quickDrivePresets)) return;
                    _quickDrivePresets = value;
                    OnPropertyChanged();
                }
            }

            private static readonly List<PresetsHandlerToRemove> PresetsHandlersToRemove = new List<PresetsHandlerToRemove>();
            private static ObservableCollection<MenuItem> _showroomPresets, _updatePreviewsPresets, _quickDrivePresets;

            private class PresetsHandlerToRemove {
                public string Key;
                public EventHandler Handler;
            }

            private ObservableCollection<MenuItem> CreatePresetsMenu(string presetsKey, Action<string> action) {
                var result = new BetterObservableCollection<MenuItem>();

                Action rebuildPresets = () => result.ReplaceEverythingBy(UserPresetsControl.GroupPresets(presetsKey, (sender, args) => {
                    action(((UserPresetsControl.TagHelper)((MenuItem)sender).Tag).Entry.Filename);
                }));
                rebuildPresets();

                var updateHandler = new EventHandler((sender, e) => rebuildPresets());
                PresetsManager.Instance.Watcher(presetsKey).Update += updateHandler;
                PresetsHandlersToRemove.Add(new PresetsHandlerToRemove { Key = presetsKey, Handler = updateHandler });

                return result;
            }

            public void InitializeShowroomPresets() {
                if (ShowroomPresets == null) {
                    ShowroomPresets = CreatePresetsMenu(CarOpenInShowroomDialog.UserPresetableKeyValue, p => {
                        CarOpenInShowroomDialog.RunPreset(p, SelectedObject, SelectedObject.SelectedSkin?.Id);
                    });
                }
            }

            public void InitializeQuickDrivePresets() {
                if (QuickDrivePresets == null) {
                    QuickDrivePresets = CreatePresetsMenu(QuickDrive.UserPresetableKeyValue, p => {
                        // TODO
                    });
                }
            }

            public void InitializeUpdatePreviewsPresets() {
                if (UpdatePreviewsPresets == null) {
                    UpdatePreviewsPresets = CreatePresetsMenu(CarUpdatePreviewsDialog.UserPresetableKeyValue, presetFilename => {
                        new CarUpdatePreviewsDialog(SelectedObject, GetAutoUpdatePreviewsDialogMode(), presetFilename).ShowDialog();
                    });
                }
            }

            public void UnloadPresetsWatchers() {
                foreach (var presetsHandlerToRemove in PresetsHandlersToRemove) {
                    PresetsManager.Instance.Watcher(presetsHandlerToRemove.Key).Update -= presetsHandlerToRemove.Handler;
                }

                PresetsHandlersToRemove.Clear();
            }

            #endregion

            private RelayCommand _manageSkinsCommand;

            public RelayCommand ManageSkinsCommand => _manageSkinsCommand ?? (_manageSkinsCommand = new RelayCommand(o => {
                new CarSkinsDialog(SelectedObject).ShowDialogWithoutBlocking();
            }));
        }

        private string _id;

        void IParametrizedUriContent.OnUri(Uri uri) {
            _id = uri.GetQueryParam("Id");
            if (_id == null) {
                throw new Exception("ID is missing");
            }
        }

        private CarObject _object;

        async Task ILoadableContent.LoadAsync(CancellationToken cancellationToken) {
            _object = await CarsManager.Instance.GetByIdAsync(_id);
            if (_object == null) return;
            await _object.SkinsManager.EnsureLoadedAsync();
        }

        void ILoadableContent.Load() {
            _object = CarsManager.Instance.GetById(_id);
            _object?.SkinsManager.EnsureLoaded();
        }

        private SelectedCarPageViewModel _model;

        void ILoadableContent.Initialize() {
            if (_object == null) throw new ArgumentException("Can't find object with provided ID");

            InitializeAcObjectPage(_model = new SelectedCarPageViewModel(_object));
            InputBindings.AddRange(new[] {
                new InputBinding(_model.UpdatePreviewsCommand, new KeyGesture(Key.P, ModifierKeys.Control)),
                new InputBinding(_model.UpdatePreviewsOptionsCommand, new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)),
                new InputBinding(_model.UpdatePreviewsManuallyCommand, new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Alt)),

                new InputBinding(_model.OpenInShowroomCommand, new KeyGesture(Key.H, ModifierKeys.Control)),
                new InputBinding(_model.OpenInShowroomOptionsCommand, new KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift)),
                new InputBinding(_model.OpenInCustomShowroomCommand, new KeyGesture(Key.U, ModifierKeys.Control)),

                new InputBinding(_model.ManageSkinsCommand, new KeyGesture(Key.K, ModifierKeys.Control))
            });
            InitializeComponent();
        }

        #region Skins
        private void SelectedSkinPreview_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                if (e.ClickCount == 2 && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                    e.Handled = true;
                    CarOpenInShowroomDialog.Run(_model.SelectedObject, _model.SelectedObject.SelectedSkin?.Id);
                } else if (e.ClickCount == 1 && ReferenceEquals(sender, SelectedSkinPreviewImage) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                    e.Handled = true;
                    new ImageViewer(
                        _model.SelectedObject.Skins.Select(x => x.PreviewImage),
                        _model.SelectedObject.Skins.IndexOf(_model.SelectedObject.SelectedSkin)
                    ).ShowDialog();
                }
            } else if (e.ChangedButton == MouseButton.Right) {
                e.Handled = true;
                OpenSkinContextMenu((((FrameworkElement)sender).DataContext as AcItemWrapper)?.Value as CarSkinObject);
            }
        }

        private void OpenSkinContextMenu(CarSkinObject skin) {
            if (skin == null) return;

            // TODO: More commands?
            var contextMenu = new ContextMenu {
                Items = {
                    new MenuItem {
                        Header = $"Skin: {skin.DisplayName?.Replace("_", "__") ?? "?"}",
                        StaysOpenOnClick = true
                    }
                }
            };

            var item = new MenuItem { Header = "Open In Showroom", InputGestureText = "Ctrl+H" };
            item.Click += (sender, args) => CarOpenInShowroomDialog.Run(_model.SelectedObject, skin.Id);
            contextMenu.Items.Add(item);

            item = new MenuItem { Header = "Open In Custom Showroom", InputGestureText = "Ctrl+U" };
            item.Click += (sender, args) => Kn5RenderWrapper.StartBrightRoomPreview(_model.SelectedObject.Location, skin.Id);
            contextMenu.Items.Add(item);

            item = new MenuItem { Header = "Drive", IsEnabled = false };
            contextMenu.Items.Add(item);
            
            contextMenu.Items.Add(new MenuItem {
                Header = "Folder",
                Command = skin.ViewInExplorerCommand
            });
            
            contextMenu.Items.Add(new Separator());

            item = new MenuItem { Header = "Update Preview" };
            item.Click += (sender, args) => new CarUpdatePreviewsDialog(_model.SelectedObject, new[] { skin.Id },
                    SelectedCarPageViewModel.GetAutoUpdatePreviewsDialogMode()).ShowDialog();
            contextMenu.Items.Add(item);

            item = new MenuItem { Header = "Update Livery" };
            var subItem = new MenuItem { Header = "From Preview" };
            subItem.Click += (sender, args) => ImageUtils.GenerateLivery(skin.PreviewImage, skin.LiveryImage);
            item.Items.Add(subItem);
            subItem = new MenuItem { Header = "Using Custom Showroom" };
            subItem.Click += (sender, args) => Kn5RenderWrapper.GenerateLivery(_model.SelectedObject.Location, skin.Id, skin.LiveryImage);
            item.Items.Add(subItem);
            contextMenu.Items.Add(item);

            contextMenu.Items.Add(new MenuItem {
                Header = "Delete Skin",
                Command = skin.DeleteCommand
            });

            contextMenu.IsOpen = true;
        }
        #endregion

        #region Presets (Dynamic Loading)
        private void ToolbarButtonShowroom_OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
            _model.InitializeShowroomPresets();
        }

        private void ToolbarButtonQuickDrive_OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
            _model.InitializeQuickDrivePresets();
        }

        private void ToolbarButtonUpdatePreviews_OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
            _model.InitializeUpdatePreviewsPresets();
        }
        #endregion

        #region Icons & Specs
        private void AcObjectBase_IconMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1) {
                new BrandBadgeEditor((CarObject) SelectedAcObject).ShowDialog();
            }
        }

        private void UpgradeIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1) {
                e.Handled = true;
                new UpgradeIconEditor((CarObject)SelectedAcObject).ShowDialog();
            }
        }

        private void ParentBlock_OnMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1) {
                e.Handled = true;
                new ChangeCarParentDialog((CarObject)SelectedAcObject).ShowDialog();
            }
        }

        private void SpecsInfoBlock_OnMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1) {
                e.Handled = true;
                new CarSpecsEditor((CarObject)SelectedAcObject).ShowDialog();
            }
        }
        #endregion
    }
}