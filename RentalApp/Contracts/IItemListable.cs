namespace RentalApp.Contracts;

/// <summary>
/// Shared interface implemented by all item-listing response types, allowing ViewModels that
/// display item lists to work against a common abstraction.
/// </summary>
public interface IItemListable
{
    int Id { get; }
}
