namespace FikaForecast.Application.Sync;

/// <summary>Thrown when the backend returns 401 (wrong or missing bearer token).</summary>
public class SyncAuthException : Exception
{
    public SyncAuthException() : base("Authentication failed — check the sync token.") { }
    public SyncAuthException(string message) : base(message) { }
    public SyncAuthException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown on 5xx / connection failures from the backend.</summary>
public class SyncTransportException : Exception
{
    public SyncTransportException() : base("Sync server unreachable.") { }
    public SyncTransportException(string message) : base(message) { }
    public SyncTransportException(string message, Exception inner) : base(message, inner) { }
}
