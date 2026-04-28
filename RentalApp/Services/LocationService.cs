using Microsoft.Maui.Devices.Sensors;

namespace RentalApp.Services;

public class LocationService : ILocationService
{
    private readonly IGeolocation _geolocation;

    public LocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation;
    }

    public async Task<(double Lat, double Lon)> GetCurrentLocationAsync()
    {
        try
        {
            var location = await _geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (location == null)
                throw new InvalidOperationException("Unable to determine current location.");

            return (location.Latitude, location.Longitude);
        }
        catch (PermissionException)
        {
            throw new InvalidOperationException("Location permission denied.");
        }
        catch (FeatureNotEnabledException)
        {
            throw new InvalidOperationException("Location services are disabled on this device.");
        }
    }
}
