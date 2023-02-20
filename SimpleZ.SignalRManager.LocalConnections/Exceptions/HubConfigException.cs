namespace SimpleZ.SignalRManager.LocalConnections.Exceptions;

public class HubConfigException : Exception
{
    public HubConfigException(string message, Exception innerException) : base(message, innerException)
    {

    }

    public HubConfigException(string message) : this(message, null)
    {

    }
}