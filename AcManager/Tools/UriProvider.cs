using System;
using AcManager.Controls.Helpers;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Tools.AcErrors {
    public class UriProvider : IAcObjectsUriProvider {
        Uri IAcObjectsUriProvider.GetUri(AcObjectNew obj) {
            if (obj is CarObject) {
                return UriExtension.Create("/Pages/Selected/SelectedCarPage.xaml?Id={0}", obj.Id);
            }

            if (obj is TrackObject) {
                return UriExtension.Create("/Pages/Selected/SelectedTrackPage.xaml?Id={0}", obj.Id);
            }

            if (obj is ShowroomObject) {
                return UriExtension.Create("/Pages/Selected/SelectedShowroomPage.xaml?Id={0}", obj.Id);
            }

            if (obj is WeatherObject) {
                return UriExtension.Create("/Pages/Selected/SelectedWeatherPage.xaml?Id={0}", obj.Id);
            }

            if (obj is ReplayObject) {
                return UriExtension.Create("/Pages/Selected/SelectedReplayPage.xaml?Id={0}", obj.Id);
            }

            var carSkinObject = obj as CarSkinObject;
            if (carSkinObject != null) {
                return UriExtension.Create("/Pages/Selected/SelectedCarSkinPage.xaml?Id={0}&CarId={1}", carSkinObject.Id, carSkinObject.CarId);
            }

            throw new NotImplementedException("Not supported type: " + obj.GetType());
        }
    }
}