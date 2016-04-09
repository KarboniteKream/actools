﻿using System;
using System.Linq;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;
using NLua;

namespace AcManager.Tools.Miscellaneous {
    public static class LuaHelper {
        public static bool CompareStrings(string a, string b) {
            return string.Equals(a, b, StringComparison.InvariantCulture);
        }

        public static bool CompareStringsIgnoringCase(string a, string b) {
            return string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }

        public static double GetNumberValue(string s) {
            return FlexibleParser.TryParseDouble(s) ?? double.NaN;
        }

        public static void Log(params object[] str) {
            Logging.Write("Lua: " + str.Select(x => $"“{x}”").JoinToString(", "));
        }

        [CanBeNull]
        public static Lua GetExtended() {
            // I have no idea why this place sometimes (very rarely) doesn't work
            // but I know that I have the same problem with another app which uses
            // the same library
            for (var i = 0; i < 5; i++) {
                try {
                    var result = new Lua();
                    result.LoadCLRPackage();
                    result.DoString(@"
import ('AcManager.Tools', 'AcManager.Tools.Miscellaneous')
log = LuaHelper.Log
numutils = {}
numutils.numvalue = LuaHelper.GetNumberValue
strutils = {}
strutils.equals = LuaHelper.CompareStrings
strutils.equals_i = LuaHelper.CompareStringsIgnoringCase");

                    if (i != 0) {
                        Logging.Warning("[LUAHELPER] Next attempt worked!");
                    }

                    return result;
                } catch (Exception e) {
                    Logging.Warning($"[LUAHELPER] Can't initialize ({i}): " + e);
                }
            }

            return null;
        }
    }
}