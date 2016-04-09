﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Tools.Lists {
    public interface IBaseAcObjectObservableCollection : INotifyCollectionChanged {
        event EventHandler CollectionReady;

        void RefreshFilter(AcPlaceholderNew valueObject);

        void RefreshFilter(AcItemWrapper wrapperObject);

        event PropertyChangedEventHandler ItemPropertyChanged;

        event WrappedValueChangedEventHandler WrappedValueChanged;
    }

    public interface IAcWrapperObservableCollection : IBaseAcObjectObservableCollection, IList<AcItemWrapper> { }

    /// <summary>
    /// Specially for ListCollectionView.
    /// </summary>
    public interface IAcObjectList : IBaseAcObjectObservableCollection, IList { }

    public class AcWrapperObservableCollection : BetterObservableCollection<AcItemWrapper>, IAcWrapperObservableCollection, IAcObjectList {
        protected void Subscribe(AcItemWrapper item) {
            item.ValueChanged += Item_ValueChanged;
            item.Value.PropertyChanged += Value_PropertyChanged;
        }

        protected void Unsubscribe(AcItemWrapper item) {
            item.ValueChanged -= Item_ValueChanged;
            item.Value.PropertyChanged -= Value_PropertyChanged;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (AcItemWrapper item in e.OldItems) {
                    Unsubscribe(item);
                }
            }

            if (e.NewItems != null) {
                foreach (AcItemWrapper item in e.NewItems) {
                    Subscribe(item);
                }
            }

            CollectionChangedInner?.Invoke(this, e);
        }

        public override void AddRange(IEnumerable<AcItemWrapper> range) {
            var list = range.ToList();
            base.AddRange(list);

            foreach (var item in list) {
                Subscribe(item);
            }
        }

        public override void ReplaceEverythingBy(IEnumerable<AcItemWrapper> range) {
            foreach (var item in Items) {
                Unsubscribe(item);
            }

            var list = range.ToList();
            base.ReplaceEverythingBy(list);

            foreach (var item in list) {
                Subscribe(item);
            }
        }

        public event PropertyChangedEventHandler ItemPropertyChanged;

        public event WrappedValueChangedEventHandler WrappedValueChanged;

        private void Item_ValueChanged(object sender, WrappedValueChangedEventArgs e) {
            e.OldValue.PropertyChanged -= Value_PropertyChanged;
            e.NewValue.PropertyChanged += Value_PropertyChanged;
            WrappedValueChanged?.Invoke(sender, e);
        }

        private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            ItemPropertyChanged?.Invoke(sender, e);
        }

        public void RefreshFilter([NotNull]AcPlaceholderNew valueObject) {
            var wrapperObject = Items.FirstOrDefault(x => x.Value == valueObject);
            if (wrapperObject == null) {
                Logging.Warning("[ACWRAPPEROC] Wrapper object is null");
                return;
            }

            RefreshFilter(wrapperObject);
        }

        public void RefreshFilter([NotNull]AcItemWrapper wrapperObject) {
            CollectionChangedInner?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, wrapperObject, wrapperObject));
        }

        public int ListenersCount { get; private set; }

        public bool HasListeners => CollectionChangedInner != null;

        protected void CollectionChangedInnerInvoke(NotifyCollectionChangedEventArgs eventArgs) {
            CollectionChangedInner?.Invoke(this, eventArgs);
        }

        private event NotifyCollectionChangedEventHandler CollectionChangedInner;

        public event ListenersChangedEventHandler ListenersChanged;

        public override event NotifyCollectionChangedEventHandler CollectionChanged {
            add {
                CollectionChangedInner += value;
                ListenersCount++;
                ListenersChanged?.Invoke(this, new ListenersChangedEventHandlerArgs(ListenersCount, ListenersCount - 1));
            }
            remove {
                CollectionChangedInner -= value;
                ListenersCount--;
                ListenersChanged?.Invoke(this, new ListenersChangedEventHandlerArgs(ListenersCount, ListenersCount + 1));
            }
        }

        public new void Clear() {
            base.Clear();
            IsReady = false;
        }

        public event EventHandler CollectionReady;

        public bool IsReady { get; private set; }

        internal void Ready() {
            IsReady = true;
            Update();
        }

        public void Update() {
            if (!IsReady) return;
            CollectionReady?.Invoke(this, new EventArgs());
        }
    }

    public delegate void ListenersChangedEventHandler(object sender, ListenersChangedEventHandlerArgs args);

    public class ListenersChangedEventHandlerArgs {
        public readonly int NewListenersCount, OldListenersCount;

        public ListenersChangedEventHandlerArgs(int newCount, int oldCount) {
            NewListenersCount = newCount;
            OldListenersCount = oldCount;
        }
    }
}