﻿using System;
using System.IO;
using System.Linq;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.Managers.Directories;
using AcManager.Tools.Managers.InnerHelpers;
using AcManager.Tools.Objects;
using AcTools.Utils.Helpers;

namespace AcManager.Tools.Managers {
    public class CarsManager : AcManagerNew<CarObject> {
        public static CarsManager Instance { get; private set; }

        public static CarsManager Initialize() {
            if (Instance != null) throw new Exception("already initialized");
            return Instance = new CarsManager();
        }

        //private class SkinsHelper : IDirectoryListener {
        //    public void FileOrDirectoryChanged(object sender, FileSystemEventArgs e) {
        //    }
        //    public void FileOrDirectoryCreated(object sender, FileSystemEventArgs e) {
        //    }
        //    public void FileOrDirectoryDeleted(object sender, FileSystemEventArgs e) {
        //    }
        //    public void FileOrDirectoryRenamed(object sender, RenamedEventArgs e) {
        //    }
        //}

        //private bool _subscribed;

        //public override void ActualScan() {
        //    base.ActualScan();
        //    if (_subscribed) return;
        //    // Directories.Subscribe(new SkinsHelper());
        //    _subscribed = true;
        //}

        public override BaseAcDirectories Directories => AcRootDirectory.Instance.CarsDirectories;

        public override CarObject GetDefault() {
            return GetById("abarth500") ?? base.GetDefault();
        }

        private static readonly string[] WatchedFiles = {
            @"logo.png",
            @"ui\badge.png",
            @"ui\ui_car.json"
        };

        private static readonly string[] WatchedSkinFileNames = {
            @"livery.png",
            @"preview.jpg",
            @"ui_skin.json"
        };

        protected override bool ShouldSkipFile(string objectLocation, string filename) {
            if (base.ShouldSkipFile(objectLocation, filename)) return true;

            var inner = filename.SubstringExt(objectLocation.Length + 1);
            if (WatchedFiles.Contains(inner.ToLowerInvariant())) {
                return false;
            }

            // return true;

            if (!inner.StartsWith("skins\\") || // sfx\…, data\…
                    inner.Count(x => x == '\\') > 2 || // skins\abc\def\file.png
                    inner.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            var name = Path.GetFileName(inner);
            return !WatchedSkinFileNames.Contains(name.ToLowerInvariant());
        }

        protected override CarObject CreateAcObject(string id, bool enabled) {
            return new CarObject(this, id, enabled);
        }
    }
}