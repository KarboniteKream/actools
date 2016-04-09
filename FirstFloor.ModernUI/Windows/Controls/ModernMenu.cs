﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernMenu : Control {
        private readonly Dictionary<string, ReadOnlyLinkGroupCollection> _groupMap = new Dictionary<string, ReadOnlyLinkGroupCollection>();
        private bool _isSelecting;

        static ModernMenu() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ModernMenu), new FrameworkPropertyMetadata(typeof(ModernMenu)));
        }

        public ModernMenu() {
            InputBindings.AddRange(new[] {
                new InputBinding(new RelayCommand(NewTab), new KeyGesture(Key.T, ModifierKeys.Control)),
                new InputBinding(new RelayCommand(CloseTab), new KeyGesture(Key.W, ModifierKeys.Control)),
                new InputBinding(new RelayCommand(CloseTab), new KeyGesture(Key.F4, ModifierKeys.Control)),
                new InputBinding(new RelayCommand(RestoreTab), new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift)),
                new InputBinding(new RelayCommand(FocusCurrentTab), new KeyGesture(Key.F6)),
                new InputBinding(new RelayCommand(NextTab), new KeyGesture(Key.Tab, ModifierKeys.Control)),
                new InputBinding(new RelayCommand(PreviousTab), new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift)),
            });

            foreach (var i in Enumerable.Range(0, 9)) {
                InputBindings.Add(new InputBinding(new RelayCommand(o => SwitchTab(i, false)), new KeyGesture(Key.D1 + i, ModifierKeys.Control)));
            }
        }

        private bool _initialized;

        public override void EndInit() {
            base.EndInit();

            if (_initialized) return;
            _initialized = true;

            foreach (var linkGroup in LinkGroups) {
                linkGroup.Initialize();
            }

            var uri = ValuesStorage.GetUri($"{SaveKey}_link");
            if (!SelectUriIfLinkExists(uri)) {
                SelectUriIfLinkExists(DefaultSource);
            }
        }

        #region Browser-like commands
        private void NewTab(object param) {
            if (!(SelectedLinkGroup is LinkGroupFilterable) || _subMenuListBox == null) return;
            var textBox = _subMenuListBox.ItemContainerGenerator
                    .ContainerFromIndex(_subMenuListBox.Items.Count - 1)?.FindChild<TextBox>("NameTextBox");
            if (textBox == null) return;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void CloseTab(object param) {
            if (!(SelectedLinkGroup is LinkGroupFilterable) || _subMenuListBox == null) return;
            _subMenuListBox.ItemContainerGenerator
                    .ContainerFromIndex(_subMenuListBox.SelectedIndex)?.FindChild<Button>("CloseButton")?.Command?.Execute(null);
            _subMenuListBox.Focus();
        }

        private void RestoreTab(object param) {
            if (!(SelectedLinkGroup is LinkGroupFilterable) || _subMenuListBox == null) return;
            ((LinkGroupFilterable)SelectedLinkGroup).RestoreLastClosed();
            _subMenuListBox.Focus();
        }

        private void FocusCurrentTab(object param) {
            if (!(SelectedLinkGroup is LinkGroupFilterable) || _subMenuListBox == null) return;
            _subMenuListBox.ItemContainerGenerator
                    .ContainerFromIndex(_subMenuListBox.SelectedIndex)?.FindChild<TextBox>("NameTextBox")?.Focus();
        }

        private void SwitchTab(int index, bool cycle) {
            if (_subMenuListBox == null) return;
            var count = _subMenuListBox.Items.Count - (SelectedLinkGroup is LinkGroupFilterable ? 1 : 0);
            _subMenuListBox.SelectedIndex = index >= count ? cycle ? 0 : count - 1 :
                    index < 0 ? cycle ? count - 1 : 0 :
                            index;
            _subMenuListBox.Focus();
        }

        private void NextTab(object param) {
            if (_subMenuListBox == null) return;
            SwitchTab(_subMenuListBox.SelectedIndex + 1, true);
        }

        private void PreviousTab(object param) {
            if (_subMenuListBox == null) return;
            SwitchTab(_subMenuListBox.SelectedIndex - 1, true);
        }
        #endregion

        #region LinkGroups
        public static readonly DependencyProperty LinkGroupsProperty = DependencyProperty.Register("LinkGroups", typeof(LinkGroupCollection),
                typeof(ModernMenu), new PropertyMetadata(new LinkGroupCollection(), OnLinkGroupsChanged));

        public LinkGroupCollection LinkGroups {
            get { return (LinkGroupCollection)GetValue(LinkGroupsProperty); }
            set { SetValue(LinkGroupsProperty, value); }
        }

        private static void OnLinkGroupsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernMenu)o).OnLinkGroupsChanged((LinkGroupCollection)e.OldValue, (LinkGroupCollection)e.NewValue);
        }

        private void OnLinkGroupsChanged(LinkGroupCollection oldValue, LinkGroupCollection newValue) {
            if (oldValue != null) {
                oldValue.CollectionChanged -= OnLinkGroupsCollectionChanged;
            }

            if (newValue != null) {
                newValue.CollectionChanged += OnLinkGroupsCollectionChanged;
            }
            
            RebuildMenu(newValue);
        }
        #endregion

        #region SelectedLinkGroup
        public static readonly DependencyProperty SelectedLinkGroupProperty = DependencyProperty.Register("SelectedLinkGroup", typeof(LinkGroup),
                typeof(ModernMenu), new PropertyMetadata(OnSelectedLinkGroupChanged));

        public LinkGroup SelectedLinkGroup => (LinkGroup)GetValue(SelectedLinkGroupProperty);

        private static void OnSelectedLinkGroupChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernMenu)o).OnSelectedLinkGroupChanged((LinkGroup)e.OldValue, (LinkGroup)e.NewValue);
        }

        private void OnSelectedLinkGroupChanged(LinkGroup oldValue, LinkGroup newValue) {
            if (oldValue != null) {
                oldValue.PropertyChanged -= Group_PropertyChanged;
            }
            
            if (newValue != null) {
                newValue.PropertyChanged += Group_PropertyChanged;
                SelectedLink = newValue.SelectedLink;
            } else {
                SelectedLink = null;
            }
        }

        private void Group_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(LinkGroup.SelectedLink)) return;
            SelectedLink = (sender as LinkGroup)?.SelectedLink;
        }
        #endregion

        #region SelectedLink
        public static readonly DependencyProperty SelectedLinkProperty = DependencyProperty.Register(nameof(SelectedLink), typeof(Link),
                typeof(ModernMenu), new PropertyMetadata(OnSelectedLinkChanged));

        public Link SelectedLink {
            get { return (Link)GetValue(SelectedLinkProperty); }
            set { SetValue(SelectedLinkProperty, value); }
        }

        private static void OnSelectedLinkChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernMenu)o).OnSelectedLinkChanged((Link)e.OldValue, (Link)e.NewValue);
        }

        private void OnSelectedLinkChanged(Link oldValue, Link newValue) {
            if (oldValue != null) {
                oldValue.PropertyChanged -= Link_PropertyChanged;
            }

            if (newValue != null) {
                newValue.PropertyChanged += Link_PropertyChanged;
            }

            SelectedSource = newValue?.NonSelectable == false ? newValue.Source : null;

            if (newValue == null || SaveKey == null || newValue.NonSelectable) return;
            var group = (from g in LinkGroups
                         where g.Links.Contains(newValue)
                         select g).FirstOrDefault();
            if (group != null) {
                group.SelectedLink = newValue;
                ValuesStorage.Set($"{SaveKey}__{group.GroupKey}", newValue.Source);
            }
            ValuesStorage.Set($"{SaveKey}_link", newValue.Source);
        }
        #endregion

        #region SelectedSource
        public static readonly DependencyProperty SelectedSourceProperty = DependencyProperty.Register("SelectedSource", typeof(Uri),
                typeof(ModernMenu), new PropertyMetadata(OnSelectedSourceChanged));

        public Uri SelectedSource {
            get { return (Uri)GetValue(SelectedSourceProperty); }
            set { SetValue(SelectedSourceProperty, value); }
        }

        private static void OnSelectedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernMenu)o).OnSelectedSourceChanged((Uri)e.OldValue, (Uri)e.NewValue);
        }

        private void OnSelectedSourceChanged(Uri oldValue, Uri newValue) {
            if (_isSelecting) return;
            if (newValue?.Equals(oldValue) == true) return;
            UpdateSelection();
        }
        #endregion

        #region VisibleLinkGroups
        private static readonly DependencyPropertyKey VisibleLinkGroupsPropertyKey = DependencyProperty.RegisterReadOnly("VisibleLinkGroups",
                typeof(ReadOnlyLinkGroupCollection), typeof(ModernMenu), null);

        public static readonly DependencyProperty VisibleLinkGroupsProperty = VisibleLinkGroupsPropertyKey.DependencyProperty;

        public ReadOnlyLinkGroupCollection VisibleLinkGroups => (ReadOnlyLinkGroupCollection)GetValue(VisibleLinkGroupsProperty);
        #endregion

        private ListBox _subMenuListBox;

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _subMenuListBox = GetTemplateChild("PART_SubMenu") as ListBox;
        }

        public bool SelectUriIfLinkExists(Uri uri) {
            if (uri == null) return false;

            var selected = (from g in LinkGroups
                            from l in g.Links
                            where l.Source == uri
                            select l).FirstOrDefault();
            if (selected == null) return false;
            
            SelectedLink = selected;
            return true;
        }

        public void SwitchToGroupByKey(string key) {
            if (SaveKey != null) {
                var uri = ValuesStorage.GetUri($"{SaveKey}__{key}");
                if (SelectUriIfLinkExists(uri)) return;
            }
            
            SelectedLink = (from g in LinkGroups
                            where g.GroupKey == key
                            from l in g.Links
                            select l).FirstOrDefault();
        }

        private void Link_PropertyChanged(object sender, PropertyChangedEventArgs args) {
            if (args.PropertyName != nameof(Link.Source)) return;
            SelectedSource = (sender as Link)?.Source;
        }

        private void OnLinkGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            RebuildMenu((LinkGroupCollection)sender);
        }

        private void RebuildMenu(LinkGroupCollection groups) {
            _groupMap.Clear();
            if (groups != null) {
                // fill the group map based on group key
                foreach (var group in groups) {
                    ReadOnlyLinkGroupCollection groupCollection;
                    if (!_groupMap.TryGetValue(group.GroupKey, out groupCollection)) {
                        // create a new collection for this group key
                        groupCollection = new ReadOnlyLinkGroupCollection(new LinkGroupCollection());
                        _groupMap.Add(group.GroupKey, groupCollection);
                    }

                    // add the group
                    groupCollection.List.Add(group);
                }
            }

            // update current selection
            UpdateSelection();
        }

        private void UpdateSelection() {
            if (!_initialized) return;

            LinkGroup selectedGroup = null;
            Link selectedLink = null;

            if (LinkGroups != null) {
                // find the current select group and link based on the selected source
                var linkInfo = (from g in LinkGroups
                                from l in g.Links
                                where l.Source == SelectedSource
                                select new {
                                    Group = g,
                                    Link = l
                                }).FirstOrDefault();

                if (linkInfo != null) {
                    selectedGroup = linkInfo.Group;
                    selectedLink = linkInfo.Link;
                } else {
                    // could not find link and group based on selected source, fall back to selected link group
                    selectedGroup = SelectedLinkGroup;

                    // if selected group doesn't exist in available groups, select first group
                    if (LinkGroups.All(g => g != selectedGroup)) {
                        selectedGroup = LinkGroups.FirstOrDefault();
                    }
                }
            }

            ReadOnlyLinkGroupCollection groups = null;
            if (selectedGroup != null) {
                // ensure group itself maintains the selected link
                if (selectedLink == null) {
                    /* very questionable place */
                    selectedLink = selectedGroup.SelectedLink;
                } else {
                    selectedGroup.SelectedLink = selectedLink;
                }

                // find the collection this group belongs to
                var groupKey = selectedGroup.GroupKey;
                _groupMap.TryGetValue(groupKey, out groups);
            }

            _isSelecting = true;
            // update selection
            SetValue(VisibleLinkGroupsPropertyKey, groups);
            SetCurrentValue(SelectedLinkGroupProperty, selectedGroup);
            SetCurrentValue(SelectedLinkProperty, selectedLink);
            _isSelecting = false;
        }

        public static readonly DependencyProperty SaveKeyProperty = DependencyProperty.Register(nameof(SaveKey), typeof(string),
                typeof(ModernMenu));

        public string SaveKey {
            get { return (string)GetValue(SaveKeyProperty); }
            set { SetValue(SaveKeyProperty, value); }
        }

        public static readonly DependencyProperty DefaultSourceProperty = DependencyProperty.Register(nameof(DefaultSource), typeof(Uri),
                typeof(ModernMenu));

        public Uri DefaultSource {
            get { return (Uri)GetValue(DefaultSourceProperty); }
            set { SetValue(DefaultSourceProperty, value); }
        }
    }
}