using System;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;

namespace AcManager.Controls.UserControls {
    internal interface IWebSomething {
        FrameworkElement Initialize();

        event EventHandler<PageLoadedEventArgs> Navigated;

        [NotNull]
        string GetUrl();

        void SetScriptProvider(ScriptProviderBase provider);

        void SetUserAgent([NotNull] string userAgent);

        void ModifyPage();

        void Execute([NotNull] string js);

        void Navigate([NotNull] string url);

        void GoBack();

        bool CanGoBack();

        void GoForward();

        bool CanGoForward();

        void OnLoaded();

        void OnUnloaded();

        void OnError(string error, string url, int line, int column);

        [ItemCanBeNull]
        Task<string> GetImageUrlAsync([CanBeNull] string filename);
    }
}