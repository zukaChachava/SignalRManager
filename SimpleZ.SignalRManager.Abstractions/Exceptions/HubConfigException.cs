namespace SimpleZ.SignalRManager.Abstractions.Exceptions;

public class HubConfigException : Exception
{
    public HubConfigException(string message, Exception innerException) : base(message, innerException)
    {

    }

    public HubConfigException(string message) : this(message, null)
    {

    }
}