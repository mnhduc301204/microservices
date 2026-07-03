namespace ECommerce.Contracts;

public static class KafkaTopics
{
    public const string ProductCreated = "catalog.product-created";
    public const string BasketCheckedOut = "basket.checked-out";
    public const string OrderCreated = "ordering.order-created";
    public const string OrderConfirmed = "ordering.order-confirmed";
    public const string OrderCancelled = "ordering.order-cancelled";
    public const string StockReserved = "inventory.stock-reserved";
    public const string StockReservationFailed = "inventory.stock-reservation-failed";
    public const string ReleaseStockReservation = "inventory.release-stock-reservation";
    public const string PaymentSucceeded = "payment.payment-succeeded";
    public const string PaymentFailed = "payment.payment-failed";
}
