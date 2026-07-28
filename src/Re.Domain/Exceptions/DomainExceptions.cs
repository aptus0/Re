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
        : base($"'{documentType}' document (Id: {documentId}) is approved and cannot be changed. Create a reversal entry to correct it.")
    { }
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productName, string warehouseName, decimal available, decimal requested)
        : base($"'{productName}' product at '{warehouseName}' warehouse has insufficient stock. Available: {available}, Requested: {requested}")
    { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, Guid id)
        : base($"'{entityType}' record not found. Id: {id}")
    { }
}

