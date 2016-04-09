﻿using System;
using System.Linq;
using System.Windows;
using AcTools.Utils.Helpers;

namespace AcManager.Tools.Helpers {
    public static class WindowsHelper {
        public const string RestartArg = "--restart";

        public static void RestartCurrentApplication() {
            ProcessExtension.Start(MainExecutingFile.Location, Environment.GetCommandLineArgs().Skip(1).Prepend(RestartArg));
            Application.Current.Shutdown();
        }
    }
}