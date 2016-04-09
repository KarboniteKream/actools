﻿using System;
using System.Linq;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Managers.Directories;
using AcManager.Tools.Objects;
using AcTools.Utils.Helpers;

namespace AcManager.Tools.Managers {
    public class ShowroomsManager : AcManagerNew<ShowroomObject> {
        public static ShowroomsManager Instance { get; private set; }

        public static ShowroomsManager Initialize() {
            if (Instance != null) throw new Exception("already initialized");
            return Instance = new ShowroomsManager();
        }

        protected override ShowroomObject CreateAcObject(string id, bool enabled) {
            return new ShowroomObject(this, id, enabled);
        }

        public override BaseAcDirectories Directories => AcRootDirectory.Instance.ShowroomsDirectories;

        public override ShowroomObject GetDefault() {
            return GetById("showroom") ?? base.GetDefault();
        }

        private static readonly string[] WatchedFiles = {
            @"ui\ui_showroom.json",
            @"preview.jpg",
            @"track.wav",
            // @"settings.ini"
        };

        protected override bool ShouldSkipFile(string objectLocation, string filename) {
            if (base.ShouldSkipFile(objectLocation, filename)) return true;
            var inner = filename.SubstringExt(objectLocation.Length + 1);
            if (WatchedFiles.Contains(inner.ToLowerInvariant())) return false;
            return !filename.EndsWith(".kn5", StringComparison.OrdinalIgnoreCase) &&
                   !filename.EndsWith(".bank", StringComparison.OrdinalIgnoreCase);
        }
    }
}