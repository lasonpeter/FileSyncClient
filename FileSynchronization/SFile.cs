using System.Net.Sockets;
using ProtoBuf;
using Serilog;
using TransferLib;
using XXHash3NET;

namespace FileSyncClient.FileSynchronization;

public class SFile
{
    public SFile(Socket socket, string filePath,byte fileId)
    {
        _socket = socket;
        _filePath = filePath;
        _fileId = fileId;
    }

    private Socket _socket;
    public string _filePath { get;}
    private byte _fileId;


    public bool SyncFile()
    {
        if (!File.Exists(_filePath))
        {
            Log.Warning("}File doesn't exist {file}, aborting",_filePath);
            return false;
        }
        using Stream file = File.OpenRead(_filePath);
        using MemoryStream stream = new MemoryStream();
        Serializer.Serialize(stream,new FSInit()
        {
            FilePath =  new FileInfo(_filePath).DirectoryName!,
            FileSize = new FileInfo(_filePath).Length,
            FileId = _fileId,
            FileName = new FileInfo(_filePath).Name
        });
        Packet packet = new Packet(stream.ToArray(),PacketType.FileSyncInit);
        _socket.SendAsync(packet.ToBytes());
        return true;
    }
    
    public void StartFileUpload()
    {

        Task upload = new Task(() =>
        {
            //Thread.Sleep(5000);
            //Console.WriteLine("WEEEEEEEEEE");
            using FileStream fileStream = File.OpenRead(_filePath);
            byte[] buffer = new byte[4000];
            int read = 0;
            int x = 0;
            while ((read = fileStream.Read(buffer)) > 0)
            {
                if (read<10)
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
                //Log.Information(stream.Length.ToString());
                lock (_socket)
                {
                    _socket.Send(packet.ToBytes());
                }
                /*if(_socket.Send(packet.ToBytes()) >4099)
                    Log.Error($"YOU SHIET{stream.Length}");*/
                x++;
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
            _socket.SendAsync(new Packet(memoryStream.ToArray(), PacketType.FileSyncCheckHash).ToBytes());
        });
        upload.Start();
        
        
    }
}