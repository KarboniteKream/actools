﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AcManager.Tools.Managers.Plugins;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Controls.UserControls {
    public partial class WebBlock {
        private readonly IWebSomething _something;

        public WebBlock() {
            _something = PluginsManager.Instance.IsPluginEnabled("Awesomium") ?
                    (IWebSomething)new AwesomiumWrapper() : new WebBrowserWrapper();
            _something.Navigated += OnNavigated;
            var child = _something.Initialize();

            InitializeComponent();
            Children.Add(child);
        }

        [ItemCanBeNull]
        public Task<string> GetImageUrlAsync([CanBeNull] string filename) {
            return _something.GetImageUrlAsync(filename);
        }

        public void OnError(string error, string url, int line, int column) {
            _something.OnError(error, url, line, column);
        }

        private void OnNavigated(object sender, PageLoadedEventArgs e) {
            UrlTextBox.Text = e.Url;
            ModifyPage();

            if (SaveKey != null && e.Url.StartsWith(@"http", StringComparison.OrdinalIgnoreCase)) {
                ValuesStorage.Set(SaveKey, e.Url);
            }
        }

        public void SetScriptProvider(ScriptProviderBase provider) {
            _something.SetScriptProvider(provider);
            provider.Associated = this;
        }

        public void Execute(string js, bool onload = false) {
            _something.Execute(onload ?
                        @"(function(){ var f = function(){" + js + @"}; if (!document.body) window.addEventListener('load', f, false); else f(); })();" :
                        @"(function(){" + js + @"})();");
        }

        public static readonly DependencyProperty OpenNewWindowsExternallyProperty = DependencyProperty.Register(nameof(OpenNewWindowsExternally), typeof(bool),
                typeof(WebBlock), new PropertyMetadata(true));

        public bool OpenNewWindowsExternally {
            get { return (bool)GetValue(OpenNewWindowsExternallyProperty); }
            set { SetValue(OpenNewWindowsExternallyProperty, value); }
        }

        public static readonly DependencyProperty UserAgentProperty = DependencyProperty.Register(nameof(UserAgent), typeof(string),
                typeof(WebBlock), new PropertyMetadata(OnUserAgentChanged));

        public string UserAgent {
            get { return (string)GetValue(UserAgentProperty); }
            set { SetValue(UserAgentProperty, value); }
        }

        private static void OnUserAgentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((WebBlock)o).OnUserAgentChanged((string)e.NewValue);
        }

        private void OnUserAgentChanged([CanBeNull] string newValue) {
            if (newValue == null) return;
            _something.SetUserAgent(newValue);
        }

        public static readonly DependencyProperty UserStyleProperty = DependencyProperty.Register(nameof(UserStyle), typeof(string),
                typeof(WebBlock), new PropertyMetadata(OnUserStyleChanged));

        [CanBeNull]
        public string UserStyle {
            get { return (string)GetValue(UserStyleProperty); }
            set { SetValue(UserStyleProperty, value); }
        }

        private static void OnUserStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((WebBlock)o).OnUserStyleChanged((string)e.NewValue);
        }

        private void OnUserStyleChanged(string newValue) {
            SetUserStyle(newValue);
        }

        public static readonly DependencyProperty SaveKeyProperty = DependencyProperty.Register(nameof(SaveKey), typeof(string),
                typeof(WebBlock));

        [CanBeNull]
        public string SaveKey {
            get { return (string)GetValue(SaveKeyProperty); }
            set { SetValue(SaveKeyProperty, value); }
        }

        [Localizable(false)]
        private void SetUserStyle([CanBeNull] string userStyle) {
            if (string.IsNullOrWhiteSpace(userStyle)) return;

            const string jsMark = "/* JS part:";
            string jsPart = null;
            if (userStyle.Contains(jsMark)) {
                var splitted = userStyle.Split(new[] { jsMark }, StringSplitOptions.None);
                userStyle = splitted[0];
                jsPart = splitted[1];
            }

            Execute($@"
var s = document.getElementById('__cm_style');
if (s && s.parentNode) s.parentNode.removeChild(s);
s = document.createElement('style');
s.id = '__cm_style';
s.innerHTML = '{userStyle.Replace("\r", "").Replace("\n", "\\n").Replace("'", "\\'")}';
if (document.body){{
    document.body.appendChild(s);
    {jsPart ?? ""}
}} else if (document.head){{
    var p = document.createElement('style');
    p.innerHTML = 'body{{display:none!important}}html{{background:black!important}}'
    document.head.appendChild(p);

    function onload(){{
        if (s.parentNode == document.head){{
            document.head.removeChild(p);
            document.head.removeChild(s);
            document.body.appendChild(s);
            {jsPart ?? ""}
        }}
    }}

    document.head.appendChild(s);
    document.addEventListener('DOMContentLoaded', onload, false);
    window.addEventListener('load', onload, false);
}}");
        }

        public static readonly DependencyProperty StartPageProperty = DependencyProperty.Register(nameof(StartPage), typeof(string),
                typeof(WebBlock), new PropertyMetadata(OnStartPageChanged));

        [CanBeNull]
        public string StartPage {
            get { return (string)GetValue(StartPageProperty); }
            set { SetValue(StartPageProperty, value); }
        }

        private static void OnStartPageChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((WebBlock)o).OnStartPageChanged((string)e.NewValue);
        }

        private void OnStartPageChanged([CanBeNull] string newValue) {
            if (_loaded) {
                Navigate(newValue);
            }
        }

        public void Navigate([CanBeNull] string url) {
            _something.Navigate(url ?? @"about:blank");
        }

        public event EventHandler<PageLoadedEventArgs> PageLoaded;

        private void UrlTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                Navigate(UrlTextBox.Text);
            }
        }

        private void UrlTextBox_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                e.Handled = true;
            }
        }

        internal void ModifyPage() {
            CommandManager.InvalidateRequerySuggested();
            _something.ModifyPage();
            SetUserStyle(UserStyle);
            PageLoaded?.Invoke(this, new PageLoadedEventArgs(_something.GetUrl()));
        }

        private void BrowseBack_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = _something?.CanGoBack() == true;
        }

        private void BrowseBack_Executed(object sender, ExecutedRoutedEventArgs e) {
            _something.GoBack();
        }

        private void BrowseForward_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = _something?.CanGoForward() == true;
        }

        private void BrowseForward_Executed(object sender, ExecutedRoutedEventArgs e) {
            _something.GoForward();
        }

        private void GoToPage_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void GoToPage_Executed(object sender, ExecutedRoutedEventArgs a) {
            Navigate(UrlTextBox.Text);
        }

        private void BrowseHome_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void BrowseHome_Executed(object sender, ExecutedRoutedEventArgs e) {
            Navigate(StartPage);
        }

        private bool _loaded;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (_loaded) return;
            _loaded = true;
            _something.OnLoaded();
            Navigate((SaveKey == null ? null : ValuesStorage.GetString(SaveKey)) ?? StartPage);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            if (!_loaded) return;
            _loaded = false;
            _something.OnUnloaded();
        }
    }
}
