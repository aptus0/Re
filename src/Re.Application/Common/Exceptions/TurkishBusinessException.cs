using System;

namespace Re.Application.Common.Exceptions;

public class TurkishBusinessException : Exception
{
    public string ErrorCode { get; }

    public TurkishBusinessException(string message, string errorCode = "BUSINESS_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public TurkishBusinessException(string message, Exception innerException, string errorCode = "BUSINESS_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
