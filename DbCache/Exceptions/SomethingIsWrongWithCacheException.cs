namespace FileSyncClient.Exceptions;

/// <summary>
/// Means that something went wrong while accessing or writing to cache database 
/// </summary>
public class SomethingIsWrongWithCacheException : Exception
{
    public SomethingIsWrongWithCacheException()
    {
        
    }
    public SomethingIsWrongWithCacheException(string message)
        : base(message)
    {
    }
    public SomethingIsWrongWithCacheException(string message, Exception inner)
        : base(message, inner)
    {
    }
}