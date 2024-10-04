using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using FileSyncClient.Config;
using FileSyncClient.FileStructureIntrospection;
using RocksDbSharp;
using Serilog;
using TransferLib;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace FileSyncClient;

class Program
{
    public readonly static object socketLock = new object();
    static async Task<int> Main(string[] args)
    {
        
        Thread.Sleep(4000);
        //INITIALIZINGs LOGGING
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogFiles", "Log.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7, // Optional: Retain the last 7 log files
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();
        //TESTING !!!!!!!!!!!!!!!!!!!!!!!!
        Console.WriteLine("TESINGGGGGGGGGGGGGGGG");
        //LOADING CONFIG
        JsonSerializer jsonSerializer = new JsonSerializer();
        Settings settings;
        try
        {
            settings = jsonSerializer.Deserialize<Settings>(new JsonTextReader(File.OpenText("config.json")));
            if (settings is null)
            {
                Log.Error("Couldn't load config");
                return -1;
            }
        }
        catch (Exception e)
        {
            Log.Error("Couldn't load config");
            Console.WriteLine(e);
            throw;
        }
        //INITIALIZING DB
        DbCache dbCache;
        try
        {
            var options = new DbOptions()
                .SetCreateIfMissing(true);
            dbCache = new DbCache(RocksDb.Open(options, "rocks.db"));
        }
        catch (Exception e)
        {
            Log.Error("Couldn't load/create database");
            Console.WriteLine(e);
            throw;
        }
        //Establish connection with server
        int x = 0;
        while(true){
            try
            {
                TcpClient tcpClient = new TcpClient();
                IPHostEntry ipHostInfo; 
                IPAddress ipAddress;
                try
                {
                    ipHostInfo = await Dns.GetHostEntryAsync(settings.HostName);
                    ipAddress = ipHostInfo.AddressList[0];

                }
                catch (Exception e)
                {
                    try
                    {
                        ipAddress = IPAddress.Parse(settings.HostName);
                        Console.WriteLine("Trying parsing");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        Log.Error("WRONG HOST IP");
                        throw;
                    }
                }

                tcpClient.Connect(ipAddress, settings.Port);
                Socket socket = tcpClient.Client;
                FileSyncController fileSyncController = new FileSyncController(socket,socketLock);
                PacketDistributor packetDistributor = new PacketDistributor(socket);
                //TODO add a version handshake
                packetDistributor.OnPing += Ping;
                packetDistributor.OnFileSyncInitResponse += fileSyncController.StartUpload;
                packetDistributor.OnFileSyncUploadCheckHashResponse += fileSyncController.FileSyncUploadHashCheckResponse;
                packetDistributor.OnFileSyncCheckHashResponse += fileSyncController.FileSyncHashCheckResponse;
                packetDistributor.VersionHandshake();
                packetDistributor.Ping();
                //Start up file watcher
                FileWatcher fileWatcher = new FileWatcher(fileSyncController);
                fileWatcher.LoadSynchronizedObjects(settings.SynchronizedObjects);
                fileWatcher.AddScanner();
                fileWatcher.CheckHashesWithServer(
                    new DirectoryInfo(settings.SynchronizedObjects[0].SynchronizedObjectPath),
                    new List<HashCheckPair>());
                //fileWatcher.UpdateSyncedFilesHashes();
                packetDistributor.AwaitPacket();
                Console.WriteLine("DISCONNECTED");
            }
            catch (Exception e)
            {
                /*ads*/
                //Console.WriteLine(e);
                Thread.Sleep(1000);
                Console.WriteLine("Failed to connect:"+e);
            }

            x++;
        }
        return 0;
    }
    


    public static void Ping(object? sender, PacketEventArgs e)
    {
        //Console.WriteLine("PING EVENT RAISED");
    }
}
