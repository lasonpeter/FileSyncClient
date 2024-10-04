using System.Text;
using FileSyncClient.Exceptions;
using RocksDbSharp;

namespace FileSyncClient;

/// <summary>
/// This class is supposed to be used instead of
/// calling to RocksDB directly cleaning up the code and making refactoring easier as
/// well as decreasing the amount of repeatable code
/// </summary>
public class DbCache
{
 
    private RocksDb _cache;

    public DbCache(RocksDb cache)
    {
        _cache = cache;
    }
    /// <summary>
    /// Retrieves the hash value of a given file 
    /// </summary>
    /// <param name="fuuid">File Universal ID</param>
    /// <returns>An ulong encoded hash value</returns>
    /// <exception cref="SomethingIsWrongWithCacheException">Something went wrong, go on, pray to the gods</exception>
    public ulong GetHash(Guid fuuid)
    {
        try
        {
            return BitConverter.ToUInt64(_cache.Get(fuuid.ToByteArray()));
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }
    /// <summary>
    /// Sets the hash value of a given file
    /// </summary>
    /// <param name="fuuid">File Universal ID</param>
    /// <param name="fileHash">New hash of a file</param>
    /// <exception cref="SomethingIsWrongWithCacheException"></exception>
    public void SetHash(Guid fuuid,ulong fileHash)
    {
        try
        {
            _cache.Put(fuuid.ToByteArray(), BitConverter.GetBytes(fileHash));
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }

    /// <summary>
    /// Retrives the FUUID of a given file
    /// </summary>
    /// <param name="filePath">File path of a file</param>
    /// <returns>FUUID of the file</returns>
    /// <exception cref="SomethingIsWrongWithCacheException"></exception>
    public Guid GetFuuid(string filePath)
    {
        try
        {
            return new Guid(_cache.Get(Encoding.UTF8.GetBytes(filePath)));
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }

    /// <summary>
    /// Sets the FUUID of a file in specified file path
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="fuuId">FUUID to set for a given file</param>
    /// <exception cref="SomethingIsWrongWithCacheException"></exception>
    public void SetFuuid(string filePath, Guid fuuId)
    {
        try
        {
            _cache.Put(Encoding.UTF8.GetBytes(filePath), fuuId.ToByteArray());
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }

    /// <summary>
    /// Retrieves the path associated with given FUUID
    /// </summary>
    /// <param name="fuuId">FUUID of the file</param>
    /// <returns>Path to a file</returns>
    /// <exception cref="SomethingIsWrongWithCacheException"></exception>
    public string GetFilePath(Guid fuuId)
    {
        try
        {
            byte[] arr = new byte[fuuId.ToByteArray().Length + 1];
            arr[0] = 0;
            fuuId.ToByteArray().CopyTo(arr, 1);
            return  Encoding.UTF8.GetString(_cache.Get(arr));
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }
    /// <summary>
    /// Sets the association between FUUID and filepath
    /// </summary>
    /// <param name="fuuId">FUUID of the file</param>
    /// <param name="filePath">Path to a file</param>
    /// <exception cref="SomethingIsWrongWithCacheException"></exception>
    public void SetFilePath(Guid fuuId, string filePath)
    {
        try
        {
            byte[] arr = new byte[fuuId.ToByteArray().Length + 1];
            arr[0] = 0;
            fuuId.ToByteArray().CopyTo(arr, 1);
            _cache.Put(arr,Encoding.UTF8.GetBytes(filePath));
        }
        catch (Exception e)
        {
            throw new SomethingIsWrongWithCacheException(message:e.Message,inner:e);
        }
    }
}