﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FirstFloor.ModernUI.Presentation;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Represents a Modern UI styled dialog window.
    /// </summary>
    public class ModernDialog
        : DpiAwareWindow {
        /// <summary>
        /// Identifies the BackgroundContent dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundContentProperty = DependencyProperty.Register("BackgroundContent", typeof(object), typeof(ModernDialog));
        /// <summary>
        /// Identifies the Buttons dependency property.
        /// </summary>
        public static readonly DependencyProperty ButtonsProperty = DependencyProperty.Register("Buttons", typeof(IEnumerable<Button>), typeof(ModernDialog));

        private Button _okButton;
        private Button _goButton;
        private Button _cancelButton;
        private Button _yesButton;
        private Button _noButton;
        private Button _closeButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernDialog"/> class.
        /// </summary>
        public ModernDialog() {
            DefaultStyleKey = typeof(ModernDialog);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            CloseCommand = new RelayCommand(o => CloseWithResult(o as MessageBoxResult?));
            Buttons = new[] { CloseButton };

            // set the default owner
            if (Application.Current != null && !ReferenceEquals(Application.Current.MainWindow, this)) {
                Owner = Application.Current.Windows.OfType<DpiAwareWindow>().FirstOrDefault(x => x.IsActive)
                        ?? (Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : null);
            }
        }

        protected void CloseWithResult(MessageBoxResult? result) {
            if (result.HasValue) {
                MessageBoxResult = result.Value;

                try {
                    // sets the Window.DialogResult as well
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (result.Value) {
                        case MessageBoxResult.OK:
                        case MessageBoxResult.Yes:
                            DialogResult = true;
                            break;
                        case MessageBoxResult.Cancel:
                        case MessageBoxResult.No:
                            DialogResult = false;
                            break;
                        default:
                            DialogResult = null;
                            break;
                    }
                } catch (InvalidOperationException) {
                    // TODO: Maybe there is a better way?
                }
            }

            Close();
        }

        protected static Button CreateExtraDialogButton(string content, ICommand command) {
            return new Button {
                Content = content /*.ToLower()*/,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Command = command
            };
        }

        protected static Button CreateExtraDialogButton(string content, Action<object> action, Func<object, bool> canExecute = null) {
            return CreateExtraDialogButton(content, new RelayCommand(action, canExecute));
        }

        protected static Button CreateExtraDialogButton(string content, Action action) {
            return CreateExtraDialogButton(content, new RelayCommand(o => action()));
        }

        protected Button CreateExtraStyledDialogButton(string styleKey, string content, ICommand command) {
            return new Button {
                Content = content /*.ToLower()*/,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Style = FindResource(styleKey) as Style,
                Command = command
            };
        }

        protected Button CreateExtraStyledDialogButton(string styleKey, string content, Action<object> action, Func<object, bool> canExecute = null) {
            return CreateExtraStyledDialogButton(styleKey, content, new RelayCommand(action, canExecute));
        }

        private Button CreateCloseDialogButton(string content, bool isDefault, bool isCancel, MessageBoxResult result) {
            return new Button {
                Content = content,
                Command = CloseCommand,
                CommandParameter = result,
                IsDefault = isDefault,
                IsCancel = isCancel,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0)
            };
        }

        private Button CreateStyledCloseDialogButton(string styleKey, string content, bool isDefault, bool isCancel, MessageBoxResult result) {
            return new Button {
                Content = content,
                Command = CloseCommand,
                CommandParameter = result,
                IsDefault = isDefault,
                IsCancel = isCancel,
                MinHeight = 21,
                MinWidth = 65,
                Margin = new Thickness(4, 0, 0, 0),
                Style = FindResource(styleKey) as Style
        };
        }

        /// <summary>
        /// Gets the close window command.
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Gets the Ok button.
        /// </summary>
        public Button OkButton => _okButton ??
                                  (_okButton = CreateCloseDialogButton(ModernUI.Resources.Ok, true, false, MessageBoxResult.OK));

        /// <summary>
        /// Gets the Go button (result is MessageBoxResult.OK).
        /// </summary>
        public Button GoButton => _goButton ??
                                      (_goButton =
                                       CreateStyledCloseDialogButton("Go.Button", ModernUI.Resources.Go, true, false, MessageBoxResult.OK));

        /// <summary>
        /// Gets the Cancel button.
        /// </summary>
        public Button CancelButton => _cancelButton ??
                                      (_cancelButton = CreateCloseDialogButton(ModernUI.Resources.Cancel, false, true, MessageBoxResult.Cancel));

        /// <summary>
        /// Gets the Yes button.
        /// </summary>
        public Button YesButton => _yesButton ??
                                   (_yesButton = CreateCloseDialogButton(ModernUI.Resources.Yes, true, false, MessageBoxResult.Yes));

        /// <summary>
        /// Gets the No button.
        /// </summary>
        public Button NoButton => _noButton ??
                                  (_noButton = CreateCloseDialogButton(ModernUI.Resources.No, false, true, MessageBoxResult.No));

        /// <summary>
        /// Gets the Close button.
        /// </summary>
        public Button CloseButton => _closeButton ??
                                     (_closeButton =
                                      CreateCloseDialogButton(ModernUI.Resources.Close, true, false, MessageBoxResult.None));

        /// <summary>
        /// Gets or sets the background content of this window instance.
        /// </summary>
        public object BackgroundContent {
            get { return GetValue(BackgroundContentProperty); }
            set { SetValue(BackgroundContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the dialog buttons.
        /// </summary>
        public IEnumerable<Button> Buttons {
            get { return (IEnumerable<Button>)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        /// <summary>
        /// Gets the message box result.
        /// </summary>
        /// <value>
        /// The message box result.
        /// </value>
        public MessageBoxResult MessageBoxResult { get; private set; } = MessageBoxResult.None;

        public bool IsResultCancel => MessageBoxResult == MessageBoxResult.Cancel;

        public bool IsResultOk => MessageBoxResult == MessageBoxResult.OK;

        public bool IsResultYes => MessageBoxResult == MessageBoxResult.Yes;

        /// <summary>
        /// Displays a messagebox.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="title">The title.</param>
        /// <param name="button">The button.</param>
        /// <param name="owner">The window owning the messagebox. The messagebox will be located at the center of the owner.</param>
        /// <returns></returns>
        public static MessageBoxResult ShowMessage(string text, string title, MessageBoxButton button, Window owner = null) {
            var dlg = new ModernDialog {
                Title = title,
                Content = new BbCodeBlock { BbCode = text, Margin = new Thickness(0, 0, 0, 8) },
                MinHeight = 0,
                MinWidth = 0,
                MaxHeight = 480,
                MaxWidth = 640
            };
            if (owner != null) {
                dlg.Owner = owner;
            }

            dlg.Buttons = GetButtons(dlg, button);
            dlg.ShowDialog();
            return dlg.MessageBoxResult;
        }

        public static MessageBoxResult ShowMessage(string text) {
            return ShowMessage(text, "", MessageBoxButton.OK);
        }

        private static IEnumerable<Button> GetButtons(ModernDialog owner, MessageBoxButton button) {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (button) {
                case MessageBoxButton.OK:
                    yield return owner.OkButton;
                    break;
                case MessageBoxButton.OKCancel:
                    yield return owner.OkButton;
                    yield return owner.CancelButton;
                    break;
                case MessageBoxButton.YesNo:
                    yield return owner.YesButton;
                    yield return owner.NoButton;
                    break;
                case MessageBoxButton.YesNoCancel:
                    yield return owner.YesButton;
                    yield return owner.NoButton;
                    yield return owner.CancelButton;
                    break;
            }
        }

        public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(nameof(IconSource), typeof(string),
                typeof(ModernDialog));

        public string IconSource {
            get { return (string)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        public static readonly DependencyProperty ButtonsRowContentProperty = DependencyProperty.Register(nameof(ButtonsRowContent), typeof(object),
                typeof(ModernDialog));

        public object ButtonsRowContent {
            get { return GetValue(ButtonsRowContentProperty); }
            set { SetValue(ButtonsRowContentProperty, value); }
        }
    }
}