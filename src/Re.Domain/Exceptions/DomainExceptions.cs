namespace Re.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public class BusinessRuleViolationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message)
        : base(message)
    {
        RuleName = ruleName;
    }
}

public class DocumentLockedException : DomainException
{
    public DocumentLockedException(string documentType, Guid documentId)
        : base($"'{documentType}' belgesi (Id: {documentId}) onaylanmış durumda olduğu için değiştirilemez. Hata için ters kayıt oluşturun.")
    { }
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productName, string warehouseName, decimal available, decimal requested)
        : base($"'{productName}' ürününde '{warehouseName}' deposunda yeterli stok yok. Mevcut: {available}, İstenen: {requested}")
    { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, Guid id)
        : base($"'{entityType}' kaydı bulunamadı. Id: {id}")
    { }
}

