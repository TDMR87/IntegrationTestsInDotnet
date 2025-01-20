namespace Bloqqer.Core.Exceptions;

public class BloqqerUnauthorizedException : Exception
{
    public BloqqerUnauthorizedException() { }
    public BloqqerUnauthorizedException(string message) : base(message) { }
}
