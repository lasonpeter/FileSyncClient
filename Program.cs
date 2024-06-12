using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Newtonsoft.Json;
using FileSyncClient.Config;
using Serilog;
using TransferLib;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace FileSyncClient;

class Program
{
    public delegate void OnPingEvent (Packet pingPacket);
    
    static async Task<int> Main(string[] args)
    {
        var set = new Settings();
        set.SynchronizedObjects = new List<SynchronizedObject>();
        set.SynchronizedObjects.Add(new SynchronizedObject()
        {
            IsSynchronized = true,
            Recursive = true,
            SynchronizedObjectPath = "/home/xenu/FactorioServer/bin/x64/saves/"
        });
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(set, new JsonSerializerOptions()));
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogFiles", "Log.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7, // Optional: Retain the last 7 log files
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();
        //Establish connection with "server"
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
                ipAddress =IPAddress.Parse(settings.HostName);
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
        FileSyncController fileSyncController = new FileSyncController(socket); 
        PacketDistributor packetDistributor = new PacketDistributor();
        packetDistributor.AwaitPacket(socket,ipAddress, settings.Port, tcpClient);
        packetDistributor.Ping(socket);
        packetDistributor.OnPing += Ping;
        packetDistributor.OnFileSyncInitResponse += fileSyncController.StartUpload;
        packetDistributor.OnFileSyncCheckHashResponse += fileSyncController.FileHashCheckResponse;
        //Start up file watcher
        FileWatcher fileWatcher = new FileWatcher(fileSyncController);
        fileWatcher.LoadObjects(settings.SynchronizedObjects);
        fileWatcher.Watch();
        return 0;
    }



    public static void Ping(object? sender, PacketEventArgs e)
    {
        Console.WriteLine("PING EVENT RAISED");
    }
}
