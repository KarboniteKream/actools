﻿using System;
using System.Linq;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.Managers.Directories;
using AcManager.Tools.Objects;

namespace AcManager.Tools.Managers {
    public class PpFiltersManager : AcManagerFileSpecific<PpFilterObject> {
        public static PpFiltersManager Instance { get; private set; }

        public static PpFiltersManager Initialize() {
            if (Instance != null) throw new Exception("already initialized");
            return Instance = new PpFiltersManager();
        }

        public override string SearchPattern => "*.ini";

        public override PpFilterObject GetDefault() {
            var v = WrappersList.FirstOrDefault(x => x.Value.Id.Contains("default"));
            return v == null ? base.GetDefault() : EnsureWrapperLoaded(v);
        }

        public override BaseAcDirectories Directories => AcRootDirectory.Instance.PpFiltersDirectories;

        protected override PpFilterObject CreateAcObject(string id, bool enabled) {
            return new PpFilterObject(this, id, enabled);
        }
    }
}