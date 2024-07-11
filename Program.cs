using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using FileSyncClient.Config;
using FileSyncClient.FileStructureIntrospection;
using Serilog;
using TransferLib;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace FileSyncClient;

class Program
{
    public readonly static object socketLock = new object();
    static async Task<int> Main(string[] args)
    {
        //INITIALIZING LOGGING
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogFiles", "Log.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7, // Optional: Retain the last 7 log files
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();
        
        //TESTING !!!!!!!!!!!!!!!!!!!!!!!!
        Console.WriteLine("TESINGGGGGGGGGGGGGGGG");
        DirectoryInfo directoryInfo = new DirectoryInfo("/home/xenu/");
        List<FileChangeInfo> fileChangeInfos = new List<FileChangeInfo>();
        List<FileChangeInfo> scanned = Scan(directoryInfo,fileChangeInfos);
        JsonSerializer serializer = new JsonSerializer();
        Console.WriteLine("eheheheh " +scanned.Count);
        StreamWriter stringWriter = new StreamWriter("scan.json");
        stringWriter.Write(JsonConvert.SerializeObject(scanned));
        stringWriter.Flush();
        stringWriter.Close();
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
                PacketDistributor packetDistributor = new PacketDistributor();
                packetDistributor.Ping(socket,socketLock);
                packetDistributor.OnPing += Ping;
                packetDistributor.OnFileSyncInitResponse += fileSyncController.StartUpload;
                packetDistributor.OnFileSyncCheckHashResponse += fileSyncController.FileHashCheckResponse;
                //Start up file watcher
                FileWatcher fileWatcher = new FileWatcher(fileSyncController);
                fileWatcher.LoadObjects(settings.SynchronizedObjects);
                fileWatcher.AddScanner();
                packetDistributor.AwaitPacket(socket);
                Console.WriteLine("DISCONNECTED");
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                Thread.Sleep(1000);
                Console.WriteLine("Failed to connect");
            }

            x++;
        }
        return 0;
    }

    public static List<FileChangeInfo> Scan(DirectoryInfo directoryInfo, List<FileChangeInfo> fileChangeInfosRoot)
    {
        //DirectoryChangeInfo directoryChangeInfo = new DirectoryChangeInfo();
        if (directoryInfo.GetDirectories().Length > 0)
        {
            foreach (var dict in directoryInfo.GetDirectories())
            {
                fileChangeInfosRoot =Scan(dict,fileChangeInfosRoot);
            }
        }
        if (directoryInfo.GetFiles().Length > 0)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                //Console.WriteLine(fileInfo.FullName);
                fileChangeInfosRoot.Add(new FileChangeInfo()
                {
                    FilePath = fileInfo.FullName,
                    Hash = fileInfo.LastWriteTime.ToBinary()
                });
            }
        }
        //Console.WriteLine($"returning: {fileChangeInfosRoot.Count}");
        return fileChangeInfosRoot;

    }


    public static void Ping(object? sender, PacketEventArgs e)
    {
        //Console.WriteLine("PING EVENT RAISED");
    }
}
