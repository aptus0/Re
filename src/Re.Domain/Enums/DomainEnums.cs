namespace Re.Domain.Enums;

/// <summary>
/// Belge (fatura, sipariş, teklif vb.) yaşam döngüsü durumları.
/// Onaylanmış belgeler değiştirilemez; hata için ters kayıt oluşturulur.
/// </summary>
public enum DocumentStatus
{
    Draft = 0,          // Taslak – düzenlenebilir
    Approved = 1,       // Onaylandı – değiştirilemez
    Posted = 2,         // Muhasebeleştirildi
    Cancelled = 3,      // İptal edildi
    Reversed = 4,       // Ters kayıt oluşturuldu
    PartiallyPaid = 5,  // Kısmen tahsil edildi
    FullyPaid = 6       // Tamamen tahsil edildi
}

public enum StockMovementType
{
    PurchaseReceipt = 1,       // Alış mal kabul
    SalesShipment = 2,         // Satış sevkiyat
    PurchaseReturn = 3,        // Alış iade
    SalesReturn = 4,           // Satış iade
    WarehouseTransfer = 5,     // Depolar arası transfer
    Counting = 6,              // Stok sayımı
    Waste = 7,                 // Fire
    Production = 8,            // Üretim giriş
    ProductionConsumption = 9, // Üretim ham madde çıkışı
    Opening = 10               // Açılış bakiyesi
}

public enum PaymentType
{
    Cash = 1,
    CreditCard = 2,
    BankTransfer = 3,
    Cheque = 4,
    Promissory = 5,   // Senet
    OpenAccount = 6,  // Açık hesap (cari)
    Voucher = 7,      // Fiş / yemek çeki
    Mixed = 99        // Parçalı ödeme
}

public enum AccountType
{
    Customer = 1,
    Supplier = 2,
    CustomerSupplier = 3,  // Hem müşteri hem tedarikçi
    Employee = 4,
    Other = 5
}

public enum MovementDirection
{
    Debit = 1,   // Borç
    Credit = 2   // Alacak
}

public enum OrderType
{
    Sales = 1,
    Purchase = 2
}

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    PartiallyFulfilled = 2,
    Fulfilled = 3,
    Invoiced = 4,
    Cancelled = 5
}

