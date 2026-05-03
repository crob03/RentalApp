namespace RentalApp.Services.Location;

/// <summary>
/// Abstraction over the device geolocation API, allowing the location provider to be swapped or mocked in tests.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Returns the device's current latitude and longitude.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when location permission is denied, location services are disabled, or the position cannot be determined.
    /// </exception>
    Task<(double Lat, double Lon)> GetCurrentLocationAsync();
}
