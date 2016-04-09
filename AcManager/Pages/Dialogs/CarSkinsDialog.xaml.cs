﻿using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using AcManager.Annotations;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;

namespace AcManager.Pages.Dialogs {
    public partial class CarSkinsDialog {
        private class CarSkinsDialogModel {
            [NotNull]
            public CarObject SelectedCar { get; }

            public Uri ListUri => UriExtension.Create("/Pages/Lists/CarSkinsListPage.xaml?CarId={0}", SelectedCar.Id);

            public CarSkinsDialogModel([NotNull] CarObject car) {
                SelectedCar = car;
            }
        }

        private CarSkinsDialogModel Model => (CarSkinsDialogModel) DataContext;

        public CarSkinsDialog([NotNull] CarObject car) {
            if (car == null) throw new ArgumentNullException(nameof(car));

            DataContext = new CarSkinsDialogModel(car);

            DefaultContentSource = Model.ListUri;
            MenuLinkGroups.Add(new LinkGroupFilterable {
                DisplayName = "skins",
                Source = Model.ListUri
            });

            InitializeComponent();
        }

        private void CarSkinsDialog_OnInitialized(object sender, EventArgs e) {
            if (Model?.SelectedCar == null) return;
            Model.SelectedCar.AcObjectOutdated += SelectedCar_AcObjectOutdated;
        }

        private void CarSkinsDialog_OnClosed(object sender, EventArgs e) {
            if (Model?.SelectedCar == null) return;
            Model.SelectedCar.AcObjectOutdated -= SelectedCar_AcObjectOutdated;
        }

        private async void SelectedCar_AcObjectOutdated(object sender, EventArgs e) {
            Hide();

            await Task.Delay(10);
            Close();
        }
    }
}