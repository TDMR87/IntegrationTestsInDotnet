namespace Bloqqer.Core.Exceptions;

public class BloqqerValidationException : Exception
{
    public BloqqerValidationException() { }
    public BloqqerValidationException(string message) : base(message) { }
}
