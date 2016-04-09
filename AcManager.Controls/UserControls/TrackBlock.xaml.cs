﻿using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using AcManager.Controls.Pages.Dialogs;
using AcManager.Tools.Objects;
using AcTools.Utils.Helpers;

namespace AcManager.Controls.UserControls {
    [ContentProperty("PreviewContent")]
    public partial class TrackBlock {
        public TrackBlock() {
            InitializeComponent();
            InnerTrackBlockPanel.DataContext = this;
        }

        public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(nameof(Track), typeof(TrackBaseObject),
                typeof(TrackBlock));

        public TrackBaseObject Track {
            get { return (TrackBaseObject)GetValue(TrackProperty); }
            set { SetValue(TrackProperty, value); }
        }

        public static readonly DependencyProperty PreviewContentProperty = DependencyProperty.Register(nameof(PreviewContent), typeof(object),
                typeof(TrackBlock));

        public object PreviewContent {
            get { return (object)GetValue(PreviewContentProperty); }
            set { SetValue(PreviewContentProperty, value); }
        }
    }
}