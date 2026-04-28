namespace RentalApp.Services;

public interface ILocationService
{
    Task<(double Lat, double Lon)> GetCurrentLocationAsync();
}
