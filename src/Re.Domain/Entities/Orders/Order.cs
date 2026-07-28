using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Orders;

public sealed class Order : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string OrderNumber { get; private set; } = "";
    public string? CustomerReference { get; private set; }
    public OrderType Type { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public DateTime OrderDate { get; private set; }
    public DateTime? RequestedDeliveryDate { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1;
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? ConfirmedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public ICollection<OrderLine> Lines { get; private set; } = new List<OrderLine>();

    private Order() { }

    public static Order Create(Guid companyId, Guid branchId, Guid accountId, Guid warehouseId,
        string number, OrderType type, DateTime date, DateTime? deliveryDate, string currency,
        decimal exchangeRate, string? reference, string? notes)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("Order number is required.");
        if (exchangeRate <= 0) throw new DomainException("Exchange rate must be positive.");
        return new Order
        {
            Id = Guid.NewGuid(), CompanyId = companyId, BranchId = branchId, AccountId = accountId,
            WarehouseId = warehouseId, OrderNumber = number.Trim(), Type = type, OrderDate = date,
            RequestedDeliveryDate = deliveryDate, Currency = currency, ExchangeRate = exchangeRate,
            CustomerReference = reference, Notes = notes, CreatedAt = DateTime.UtcNow
        };
    }

    public void AddLine(OrderLine line)
    {
        if (Status != OrderStatus.Draft) throw new DocumentLockedException("Order", Id);
        if (line.Quantity <= 0) throw new DomainException("Order quantity must be positive.");
        Lines.Add(line); Recalculate();
    }

    public void Confirm(Guid userId)
    {
        if (Status != OrderStatus.Draft) throw new DocumentLockedException("Order", Id);
        if (Lines.Count == 0) throw new DomainException("An empty order cannot be confirmed.");
        Status = OrderStatus.Confirmed; ConfirmedBy = userId; ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fulfil(Guid lineId, decimal quantity)
    {
        if (Status is not (OrderStatus.Confirmed or OrderStatus.PartiallyFulfilled))
            throw new DomainException("Only confirmed orders can be fulfilled.");
        var line = Lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new EntityNotFoundException("OrderLine", lineId);
        line.Fulfil(quantity);
        Status = Lines.All(x => x.RemainingQuantity == 0)
            ? OrderStatus.Fulfilled : OrderStatus.PartiallyFulfilled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkInvoiced(Guid invoiceId)
    {
        if (Type != OrderType.Sales) throw new DomainException("Only sales orders can create sales invoices.");
        if (Status is not (OrderStatus.Confirmed or OrderStatus.PartiallyFulfilled or OrderStatus.Fulfilled))
            throw new DomainException("Order is not ready for invoicing.");
        InvoiceId = invoiceId; Status = OrderStatus.Invoiced; UpdatedAt = DateTime.UtcNow;
    }

    private void Recalculate()
    {
        SubTotal = Lines.Sum(x => x.NetAmount);
        TaxAmount = Lines.Sum(x => x.TaxAmount);
        TotalAmount = SubTotal + TaxAmount;
    }
}

public sealed class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal FulfilledQuantity { get; private set; }
    public decimal RemainingQuantity => Quantity - FulfilledQuantity;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount => Quantity * UnitPrice * (1 - DiscountPercent / 100m);
    public decimal TaxAmount => NetAmount * VatRate / 100m;
    public string? Notes { get; set; }
    public Order Order { get; set; } = null!;

    public void Fulfil(decimal quantity)
    {
        if (quantity <= 0 || quantity > RemainingQuantity)
            throw new DomainException("Fulfilment quantity exceeds the remaining order quantity.");
        FulfilledQuantity += quantity; UpdatedAt = DateTime.UtcNow;
    }
}
