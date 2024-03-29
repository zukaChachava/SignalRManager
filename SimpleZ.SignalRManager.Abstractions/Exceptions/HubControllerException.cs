﻿namespace SimpleZ.SignalRManager.Abstractions.Exceptions;

public class HubControllerException : Exception
{
    public HubControllerException(string message, Exception? innerException) : base(message, innerException)
    {

    }

    public HubControllerException(string message) : this(message, null)
    {

    }
}