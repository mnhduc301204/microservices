using ECommerce.Basket.Contracts;

namespace ECommerce.Basket.Data;

public interface IBasketStore
{
    Task<IReadOnlyCollection<BasketItemDto>> GetItems(Guid customerId, CancellationToken cancellationToken);

    Task<BasketCheckoutState?> GetPendingCheckout(Guid customerId, CancellationToken cancellationToken);

    Task<BasketCheckoutState> CreatePendingCheckout(
        Guid customerId,
        Guid checkoutId,
        IReadOnlyCollection<BasketItemDto> items,
        CancellationToken cancellationToken);

    Task CompleteCheckout(Guid customerId, Guid checkoutId, CancellationToken cancellationToken);

    Task<BasketItemDto> AddOrUpdateItem(Guid customerId, string sku, string productName, decimal unitPrice, int quantity, CancellationToken cancellationToken);

    Task RemoveItem(Guid customerId, string sku, CancellationToken cancellationToken);

    Task Clear(Guid customerId, CancellationToken cancellationToken);
}

public sealed record BasketCheckoutState(Guid CheckoutId, IReadOnlyCollection<BasketItemDto> Items);
