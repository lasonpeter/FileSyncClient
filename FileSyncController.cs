using System.Net.Sockets;
using FileSyncClient.FileSynchronization;
using ProtoBuf;
using Serilog;
using TransferLib;

namespace FileSyncClient;

public class FileSyncController
{
    private Socket _socket;
    public Dictionary<string,FileChange> Queue { get; } = new(0);
    private Dictionary<byte, SFile?> fileLookup = new();
    private bool[] availableIds = new bool[256];
    public FileSyncController(Socket socket)
    {
        _socket = socket;
    }

    public void AddNewChange(FileChange fileChange)
    {
        //TODO: CHANGE IT TO SUPPORT NOT ONLY CREATION
        if(fileChange.FileOperation == FileOperation.FileCreated && !fileChange.FilePath.Contains(".goutputstream"))
        {
            if (!Queue.TryAdd(fileChange.FilePath, fileChange))
            {
                Queue.Remove(fileChange.FilePath);
                Queue.Add(fileChange.FilePath, fileChange);
            }
        }
    }

    public void StartUpload(object? sender, PacketEventArgs eventArgs)
    {
        Console.WriteLine("INITIATING UPLOAD");
        MemoryStream memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        FSInitResponse fsInitResponse = Serializer.Deserialize<FSInitResponse>(memoryStream);
        if (!fsInitResponse.IsAccepted)
        {
            fileLookup.Remove(fsInitResponse.FileId);
        }
        SFile? sFile;
        if (fileLookup.TryGetValue(fsInitResponse.FileId, out sFile))
        {
            Queue.Remove(sFile._filePath);
            sFile.StartFileUpload();
        }
    }

    public void FileHashCheckResponse(object? sender, PacketEventArgs eventArgs)
    {
        MemoryStream memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        FSCheckHashResponse fsCheckHashResponse = Serializer.Deserialize<FSCheckHashResponse>(memoryStream);
        if (fsCheckHashResponse.IsCorrect)
        {
            fileLookup.Remove(fsCheckHashResponse.FileId);
        }

        MemoryStream ms = new MemoryStream();
        Serializer.Serialize(ms,new FSFinish(){FileId = fsCheckHashResponse.FileId});
        _socket.SendAsync(new Packet(ms.ToArray(), PacketType.FileSyncFinish, (int)ms.Length).ToBytes());

    }

    public void ContinuousSync()
    {
        new Thread((o =>
        {
            while (true)
            {
                if (fileLookup.Count == 0)
                {
                    Thread.Sleep(1000);
                    byte x = 0;
                    lock(Queue){
                        foreach (var fileChange in Queue)
                        {
                            if (x >= 255)
                                break;
                            fileLookup.Add(x, new SFile(_socket, fileChange.Value.FilePath, x));
                            SFile? sFile;
                            if (fileLookup.TryGetValue(x, out sFile))
                                sFile.SyncFile();
                            x++;
                        }
                    }
                }

                Thread.Sleep(500);
            }
        })).Start();
    }

    public void Sync()
    {

        while (true)
        {
            Console.WriteLine("TRY START");
            if (fileLookup.Count == 0)
            {
                List<string> toRemove = new();
                Console.WriteLine("START");
                byte x = 0;
                lock (Queue)
                {
                    foreach (var fileChange in Queue)
                    {
                        Console.WriteLine("TAKING");//DON'T TOUCH IT, MAGIC HAPPENS
                        if (x >= 10)
                            break;
                        SFile sfile = new SFile(_socket, fileChange.Value.FilePath, x);
                        if (sfile.SyncFile())
                        {
                            fileLookup.Add(x,sfile );
                            x++;
                        }
                        else
                        {
                            toRemove.Add(fileChange.Key);
                        }
                    }

                    foreach (var removed in toRemove)
                    {
                        Queue.Remove(removed);
                    }
                }
                Log.Information("Syncing: {filesAmount} files",x);
                break;
            }
            if(Queue.Count==0)
                break;

            Thread.Sleep(1_000);
        }
    }

    private byte? GetUniqueId()
    {
        for (byte x = 0; x < 255; x++)
        {
            if (availableIds[x] == true)
            {
                availableIds[x] = false;
                return x;
            }
        }

        return null;
    }
}