using System.Net.Sockets;
using ProtoBuf;
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
        Console.WriteLine("SYNCING FILE, ONCEEEEEEEEE ?");
        if (!File.Exists(_filePath))
        {
            Log.Warning("}File doesn't exist {file}, aborting",_filePath);
            return false;
        }
        using Stream file = File.OpenRead(_filePath);
        using MemoryStream stream = new MemoryStream();
        FileInfo fileInfo = new FileInfo(_filePath);
        Serializer.Serialize(stream,new FSInit
        {
            FileId = _fileId,
            FilePath = fileInfo.DirectoryName!,
            FileSize = fileInfo.Length,
            FileName = fileInfo.Name,
            LastAccessTime = fileInfo.LastAccessTime,
            LastWriteTime = fileInfo.LastWriteTime,
            CreationTime = fileInfo.CreationTime,
            FuuId = Guid.NewGuid().ToByteArray()
        });
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
            Serializer.Serialize(memoryStream, new FSCheckHash()
            {
                FileId = _fileId,
                Hash = hash64
            });
            {
                _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncCheckHash).ToBytes());
            }
        });
        upload.Start();
    }
}