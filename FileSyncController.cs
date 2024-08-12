using System.Net.Sockets;
using System.Text;
using FileSyncClient.FileStructureIntrospection;
using FileSyncClient.FileSynchronization;
using ProtoBuf;
using rbnswartz.LinuxIntegration.Notifications;
using RocksDbSharp;
using Serilog;
using TransferLib;
using XXHash3NET;

namespace FileSyncClient;

public class FileSyncController
{
    private Socket _socket;
    public Dictionary<string,FileChange> Queue { get; } = new(0);
    private readonly Dictionary<byte, SFile?> _syncedFilesLookup = new();
    private readonly bool[] _availableIds = new bool[256];
    private long _lastAccessTime=0; //For upload time counter
    private readonly object _socketLock;
    private RocksDb _rocksDb;

    public FileSyncController(Socket socket,object socketLock,RocksDb rocksDb)
    {
        _rocksDb = rocksDb;
        _socket = socket;
        _socketLock = socketLock;
    }

    public void Watch()
    {
        long timeElapsed = 0;
        new Thread(_ =>
        {
            while(true){
                timeElapsed = Environment.TickCount64 - _lastAccessTime;
                if (Queue.Count>0)
                    Console.WriteLine(timeElapsed);
                if (timeElapsed > 5_000 && Queue.Count > 0 )
                {
                    Console.WriteLine("SYNCING");
                    Sync();
                    _lastAccessTime = Environment.TickCount64;
                }
                Thread.Sleep(1_000);
            }
        }).Start();
    }
    public void AddNewChange(FileChange fileChange)
    {
        //TODO: CHANGE IT TO SUPPORT NOT ONLY CREATION
        if(fileChange.FileOperation == FileOperation.FileCreated)
        {
            if (!Queue.TryAdd(fileChange.FilePath, fileChange))
            {
                Queue.Remove(fileChange.FilePath);
                Queue.Add(fileChange.FilePath, fileChange);
            }
            _lastAccessTime = Environment.TickCount64;
        }
    }

    /// <summary>
    /// Starts the upload sequence of a given SFile, which is a result of FileSyncInitResponse
    /// </summary>
    /// <param name="sender">eventparam</param>
    /// <param name="packetEventArgs">Wrapped packet</param>
    public void StartUpload(object? sender, PacketEventArgs packetEventArgs)
    {
        Console.WriteLine("INITIATING UPLOAD");
        MemoryStream memoryStream = new MemoryStream(packetEventArgs.Packet.Payload, 0, packetEventArgs.Packet.MessageLength);
        FSInitResponse fsInitResponse = Serializer.Deserialize<FSInitResponse>(memoryStream);
        if (!fsInitResponse.IsAccepted)
        {
            
            _syncedFilesLookup.Remove(fsInitResponse.FileId);
        }
        SFile? sFile;
        if (_syncedFilesLookup.TryGetValue(fsInitResponse.FileId, out sFile))
        {
            Queue.Remove(sFile._filePath);
            Console.WriteLine("STARTING");
            sFile.StartFileUpload();
        }
    }

    public void FileHashCheckResponse(object? sender, PacketEventArgs eventArgs)
    {
        MemoryStream memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        FSCheckHashResponse fsCheckHashResponse = Serializer.Deserialize<FSCheckHashResponse>(memoryStream);
        if (fsCheckHashResponse.IsCorrect)
        {
            _syncedFilesLookup.Remove(fsCheckHashResponse.FileId);
        }
        else //TODO THIS IS FOR TESTING
        {
            _syncedFilesLookup.Remove(fsCheckHashResponse.FileId);
        }

        MemoryStream ms = new MemoryStream();
        Serializer.Serialize(ms,new FSFinish(){FileId = fsCheckHashResponse.FileId});
        
            _socket.SendAsync(new Packet(ms.ToArray(), PacketType.FileSyncFinish, (int)ms.Length).ToBytes());
        
        Console.WriteLine("FINISH");
    }
//TODO
    public void ContinuousSync()
    {
        new Thread((o =>
        {
            while (true)
            {
                if (_syncedFilesLookup.Count == 0)
                {
                    Thread.Sleep(1000);
                    byte x = 0;
                    lock(Queue){
                        foreach (var fileChange in Queue)
                        {
                            if (x >= 255)
                                break;
                            _syncedFilesLookup.Add(x, new SFile(_socket, fileChange.Value.FilePath, x,_socketLock));
                            SFile? sFile;
                            if (_syncedFilesLookup.TryGetValue(x, out sFile))
                                sFile.SyncFile(ref _rocksDb);
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
            //Console.WriteLine("TRY START>");
            //Console.WriteLine($"Queue: {Queue.Count}, synced file lookup: {_syncedFilesLookup.Count}");
            if (_syncedFilesLookup.Count == 0)
            {
                List<string> toRemove = new();
                Console.WriteLine("START");
                byte x = 0;
                lock (Queue)
                {
                    foreach (var fileChange in Queue)
                    {
                        //Console.WriteLine("TAKING");//DON'T TOUCH IT, MAGIC HAPPENS
                        if (x >= 1)
                        {
                            break;
                        }
                        SFile sFile = new SFile(_socket, fileChange.Value.FilePath, x,_socketLock);
                        if (sFile.SyncFile(ref _rocksDb))
                        {
                            _syncedFilesLookup.Add(x,sFile );
                            toRemove.Add(fileChange.Key);
                        }
                        else
                        {
                            toRemove.Add(fileChange.Key);
                        }
                        x++; 

                    }

                    foreach (var removed in toRemove)
                    {
                        if (Queue.Remove(removed))
                        {
                            Console.WriteLine($"Removed from queue:{removed}");
                        }
                        else
                        {
                            Console.WriteLine($"FAILED from queue:{removed}");

                        }
                    }
                }
                Log.Information("Syncing: {filesAm2ount} files",x);
            }
            if(Queue.Count==0)
                break;

            Thread.Sleep(100);
        }
    }
}