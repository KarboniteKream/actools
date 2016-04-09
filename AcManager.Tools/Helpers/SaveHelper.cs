﻿using System;
using System.Threading.Tasks;
using FirstFloor.ModernUI.Helpers;
using Newtonsoft.Json;

namespace AcManager.Tools.Helpers {
    public interface ISaveHelper {
        void Init();

        void Reset();

        void Load();

        bool HasSavedData { get; }

        string ToSerializedString();

        void FromSerializedString(string data);

        void FromSerializedStringWithoutSaving(string data);

        void Save();

        void SaveLater();
    }


    public class SaveHelper<T> : ISaveHelper {
        private readonly string _key;
        private readonly Func<T> _save;
        private readonly Action<T> _load;
        private readonly Action _reset;

        public SaveHelper(string key, Func<T> save, Action<T> load, Action reset) {
            _key = key;
            _save = save;
            _load = load;
            _reset = reset;
        }

        public void Init() {
            Reset();
            Load();
        }

        public void Reset() {
            _disableSaving = true;
            _reset();
            _disableSaving = false;
        }

        public void Load() {
            var data = ValuesStorage.GetString(_key);
            if (data == null) return;

            try {
                _disableSaving = true;
                _load(JsonConvert.DeserializeObject<T>(data));
            } catch (Exception e) {
                Logging.Warning("cannot load data: " + e);
            } finally {
                _disableSaving = false;
            }
        }

        public bool HasSavedData => ValuesStorage.Contains(_key);

        public string ToSerializedString() {
            var obj = _save();
            return obj == null ? null : JsonConvert.SerializeObject(obj);
        }

        public void FromSerializedString(string data, bool disableSaving) {
            try {
                _disableSaving = disableSaving;
                _load(JsonConvert.DeserializeObject<T>(data));
            } catch (Exception e) {
                Logging.Warning("cannot load data: " + e);
            } finally {
                _disableSaving = false;
            }
        }

        public void FromSerializedString(string data) {
            FromSerializedString(data, false);
        }

        public void FromSerializedStringWithoutSaving(string data) {
            FromSerializedString(data, true);
        }

        public void Save() {
            var serialized = ToSerializedString();
            if (serialized == null) return;
            ValuesStorage.Set(_key, serialized);
        }

        private bool _disableSaving, _savingInProgress;

        public async void SaveLater() {
            if (_disableSaving || _savingInProgress) return;
            _savingInProgress = true;

            await Task.Delay(300);

            if (_disableSaving) {
                return;
            }
            
            Save();
            _savingInProgress = false;
        }
    }
}