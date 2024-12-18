using System.Net.Sockets;
using System.Text;
using ProtoBuf;
using RocksDbSharp;
using Serilog;
using TransferLib;
using XXHash3NET;

namespace FileSyncClient.FileSynchronization;

public class SFile
{
    public SFile(Socket socket, string filePath,byte fileId, object socketLock)
    {
        _socket = socket;
        _filePath = filePath;
        _fileId = fileId;
        _socketLock = socketLock;
    }

    private Socket _socket;
    public string _filePath { get;}
    private byte _fileId;
    private object _socketLock;

    /// <summary>
    /// Starts the synchronization procedure for this file object
    /// </summary>
    /// <returns></returns>
    public bool SyncFile()
    {
        Guid fuuid = Guid.NewGuid();
        Console.WriteLine($"SYNCING FILE, ONCEEEEEEEEE {_filePath}");
        if (!File.Exists(_filePath))//Checks if file exists
        {
            Log.Warning("File doesn't exist {file}, aborting",_filePath);
            return false;
        }
        try
        {
            //var fuuidBytes = rocksDb.Get(Encoding.UTF8.GetBytes(_filePath));
            if(!DbCache.Instance.HasFilePath(_filePath))//Checks if there is a record with specified filepath
            {//Creates a new hash for the file as well as fuuid if a file is NOT in the database yet
                Console.WriteLine("Creating new FUUID & hash");
                ulong hash64;
                using var memoryStream = new MemoryStream();
                {
                    hash64 = XXHash3.Hash64(File.OpenRead(_filePath));
                    Console.WriteLine(hash64);
                }
                
                //rocksDb.Put(fuuid.ToByteArray(),BitConverter.GetBytes(hash64)); //ALWAYS USE Guid.NewGuid().ToByteArray() to get fuuid
                DbCache.Instance.SetHash(fuuid,hash64);
                //rocksDb.Put(Encoding.UTF8.GetBytes(_filePath),fuuid.ToByteArray());
                DbCache.Instance.SetFuuid(_filePath,fuuid);
                Console.WriteLine($"Created new FUUID: {fuuid.ToString()}");
            }
            else
            {
                fuuid = DbCache.Instance.GetFuuid(_filePath);
            }
            ulong hash = DbCache.Instance.GetHash(fuuid);
            //byte[] hash = rocksDb.Get(fuuid.ToByteArray());
            Console.WriteLine($"File with FUUID: {fuuid.ToString()} HASH: {hash}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        using Stream file = File.OpenRead(_filePath);
        using MemoryStream stream = new MemoryStream();
        FileInfo fileInfo = new FileInfo(_filePath);
        FsInit fsInit = new FsInit()
        {
            FileId = _fileId,
            FilePath = fileInfo.DirectoryName!,
            FileSize = fileInfo.Length,
            FileName = fileInfo.Name,
            LastAccessTime = fileInfo.LastAccessTime,
            LastWriteTime = fileInfo.LastWriteTime,
            CreationTime = fileInfo.CreationTime,
            FuuId = fuuid
        };
        Console.WriteLine($"THIS:{fsInit.FuuId}");
        var memoryStream2 = new MemoryStream(stream.ToArray(), 0, (int)stream.Length);
        var fsInit2 = Serializer.Deserialize<FsInit>(memoryStream2);
        Console.WriteLine($"THIS2:{fsInit2.FuuId}");
        Console.WriteLine($"CHEEEEEEEEEEEKIN: {fsInit.FuuId.ToString()}");
        Serializer.Serialize(stream,fsInit);
        Packet packet = new Packet(stream.ToArray(),PacketType.FileSyncInit);
        
            _socket.SendAsync(packet.ToBytes());
        
        return true;
    }
    
    
    /// <summary>
    /// Starts the upload of current file object
    /// </summary>
    public void StartFileUpload()
    {

        Task upload = new Task(() =>
        {
            //Thread.Sleep(5000);
            Console.WriteLine("WEEEEEEEEEE");
            using FileStream fileStream = File.OpenRead(_filePath);
            byte[] buffer = new byte[4000];
            int read = 0;
            int x = 0;
            while ((read = fileStream.Read(buffer)) > 0)
            {
                //Console.WriteLine("heh ?");
                if (read <= 0)
                {
                    Log.Error("NOTENOUGH");
                    Console.WriteLine("WEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEZ");
                }
                FSSyncData fsData = new FSSyncData()
                {
                    FileData = buffer,
                    FileId = _fileId,
                    Length = (short)read
                };
                //Console.WriteLine(read);
                using MemoryStream stream = new MemoryStream();
                Serializer.Serialize(stream, fsData);
                Packet packet = new Packet(stream.ToArray(), PacketType.FileSyncData, (int)stream.Length);
                /*foreach (var VARIABLE in packet.Payload)
                {
                    Console.Write(VARIABLE);
                }*/
                //Log.Information(stream.Length.ToString());
                
                {
                    _socket.SendAsync(packet.ToBytes());
                    //Console.WriteLine("?");
                }
                /*if(_socket.Send(packet.ToBytes()) >4099)
                    Log.Error($"YOU SHIET{stream.Length}");*/
                x++;
                //Console.WriteLine(read);
            }
            Console.WriteLine($"WROTE: {x} packets");
            //Console.WriteLine(read);

            Console.WriteLine("CHECK");
            ulong hash64;
            using (FileStream fs = File.OpenRead(_filePath))
            {
                hash64 = XXHash3.Hash64(fs);
                Console.WriteLine(hash64);
            }

            using MemoryStream memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, new FSUploadCheckHash()
            {
                FileId = _fileId,
                Hash = hash64
            });
            {
                _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncUploadCheckHash).ToBytes());
            }
        });
        upload.Start();
    }
}