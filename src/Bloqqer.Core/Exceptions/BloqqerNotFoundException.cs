namespace Bloqqer.Core.Exceptions;

public class BloqqerNotFoundException : Exception
{
    public BloqqerNotFoundException() { }

    public BloqqerNotFoundException(string message) : base(message) { }
}
