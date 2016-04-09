﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using AcManager.Tools.AcObjectsNew;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;

namespace AcManager.Tools.AcErrors {
    public interface IAcError {
        AcErrorCategory Category { get; }

        AcErrorType Type { get; }

        string Message { get; }

        ICommand StartErrorFixerCommand { get; }
    }

    public abstract class AcErrorWrapper : IAcError {
        protected AcErrorWrapper(IAcError baseError) {
            Category = baseError.Category;
            Type = baseError.Type;
            Message = baseError.Message;
        }

        public AcErrorCategory Category { get; }

        public AcErrorType Type { get; }

        public string Message { get; }

        public abstract ICommand StartErrorFixerCommand { get; }
    }

    public class AcError : IAcError {
        public AcErrorCategory Category { get; }

        public AcErrorType Type { get; }

        public string Message { get; }

        public Exception BaseException { get; private set; }

        public AcError(AcErrorType type, params object[] args) {
            Category = CategoryFromType(type);
            Type = type;

            try {
                Message = string.Format(MessageFromType(type), args.Select(x => (x as Exception)?.Message ?? x).ToArray());
            } catch (FormatException) {
                Message = Regex.Replace(MessageFromType(type), @"\{\d+\}", "?");
            }

            BaseException = args.OfType<Exception>().FirstOrDefault();

            if (type != AcErrorType.Data_JsonIsMissing || !Equals(args.FirstOrDefault(), "ui_skin.json")) {
                Logging.Write("[ACERROR] " + Message);
            }

            foreach (var exception in args.OfType<Exception>()) {
                Logging.Write("[ACERROR] Exception: " + exception);
            }
        }

        private static string MessageFromType(AcErrorType type) {
            return type.GetDescription() ?? "?";
        }

        private static AcErrorCategory CategoryFromType(AcErrorType type) {
            AcErrorCategory result;
            if (!Enum.TryParse(type.ToString().Split(new[] {'_'}, 2)[0], out result)) {
                throw new Exception("Can't get category for AcErrorType: " + type);
            }

            return result;
        }

        private static IUiAcErrorFixer _uiAcErrorFixer;

        public static void Register(IUiAcErrorFixer fixer) {
            _uiAcErrorFixer = fixer;
        }

        private ICommand _startErrorFixerCommand;

        public ICommand StartErrorFixerCommand => _startErrorFixerCommand ?? (_startErrorFixerCommand = new RelayCommand(o => {
            _uiAcErrorFixer?.Run((AcObjectNew)o, this);
        }, o => _uiAcErrorFixer != null));
    }

    public interface IUiAcErrorFixer {
        void Run(AcObjectNew acObject, AcError error);
    }
}