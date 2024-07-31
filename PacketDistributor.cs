using System.Net;
using System.Net.Sockets;
using FileSyncClient.Config;
using ProtoBuf;
using Serilog;
using TransferLib;

namespace FileSyncClient;

class PacketDistributor
{
    private Socket _socket;

    public PacketDistributor(Socket socket)
    {
        _socket = socket;
    }

    public event EventHandler<PacketEventArgs>? OnPing;
    public event EventHandler<PacketEventArgs>? OnData;
    public event EventHandler<PacketEventArgs>? OnFileSyncInitResponse;
    public event EventHandler<PacketEventArgs>? OnFileSyncCheckHashResponse;

    public void AwaitPacket()
    {
        
        var buffer = new byte[4_099];
        Packet packet = new Packet();
        while (true)
        {
            Array.Clear(buffer);
            // Receive message.
            var received = _socket.Receive(buffer, SocketFlags.None);
            if (received == 0)
            {
                Console.WriteLine("Connection Lost");
                try
                {
                    _socket.Disconnect(true);
                    _socket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return;
            }
            packet.DecodePacket(buffer);
            switch (packet.PacketType)
            {
                case PacketType.Ping:
                    OnPingPacket(new PacketEventArgs(packet));
                    break;
                case PacketType.Data:
                    OnDataPacket(new PacketEventArgs(packet));
                    break;
                case PacketType.FileSyncInitResponse: 
                    OnFileSyncInitResponsePacket(new PacketEventArgs(packet));
                    break;
                case PacketType.FileSyncCheckHashResponse:
                    OnFileSyncCheckHashResponsePacket(new PacketEventArgs(packet));
                    break;
                /*default:
                    throw new Exception("FATAL SOCKET ERROR");*/
            }
        } 
        
    }

    public void Ping()
    {
        byte[] pingPacket = new Packet(PacketType.Ping).ToBytes();
        var socketPing = new Thread( o =>
        {
                while(true){
                    try
                    {

                       {
                            var we = _socket.SendAsync(pingPacket);
                            Thread.Sleep(1000);
                       }
                       //Console.WriteLine("SENT:"+we.Result);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        break;
                    }
                }   
        });
        socketPing.Start();
    }

    public bool VersionHandshake()
    {
        var stream = new MemoryStream();
        Serializer.Serialize(stream, new VersionHandshake()
        {
            Version = Settings.Instance.Version
        });
        _socket.Send(new Packet(stream.ToArray(), PacketType.VersionHandshake).ToBytes());
        Console.WriteLine("SENT HANDSHAKE");
        var packet = new Packet();
        var buffer = new byte[4_099];
        var size = 4099;
        var total = 0;
        var dataLeft = size;
        while (total < size)
        {
            try
            {
                int recv = _socket.Receive(buffer, total, dataLeft, SocketFlags.None);
                //Console.WriteLine(recv);

                /*if (recv == 0)
                {
                    break;
                }*/

                total += recv;
                dataLeft -= recv;
                //Console.WriteLine(total);}
            }
            catch (Exception e)
            {
                Console.WriteLine("Client abruptly disconnected ");
                Log.Warning("Client abruptly disconnected ");
                break;
            }
        }
        Console.WriteLine("RECEIVED RESPONSE");
        try
        {
            packet.DecodePacket(buffer);
            if (packet.PacketType is PacketType.VersionHandshakeResponse)
            {
                MemoryStream memoryStream = new MemoryStream(packet.Payload, 0, packet.MessageLength);
                VersionHandshakeResponse versionHandshakeResponse =
                    Serializer.Deserialize<VersionHandshakeResponse>(memoryStream);
                switch (versionHandshakeResponse.ApplicationVersionCompatibilityLevel)
                {
                    case ApplicationVersionCompatibilityLevel.FullyCompatible:
                    {
                        Console.WriteLine("Fully compatible");
                        Log.Information("Fully compatible !");
                        return true;
                    }
                    case ApplicationVersionCompatibilityLevel.PartiallyCompatible:
                    {
                        Console.WriteLine(
                            $"Partially compatible | client:{Settings.Instance.Version} server:{packet.Version}");
                        Log.Warning(
                            $"Partially compatible | client:{Settings.Instance.Version} server:{packet.Version}");
                        return true;
                    }
                    case ApplicationVersionCompatibilityLevel.Incompatible:
                    {
                        Console.WriteLine(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}");
                        Log.Warning(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}");
                        throw (new Exception(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}"));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return false;
    }

    protected virtual void OnPingPacket(PacketEventArgs e)
    {
        EventHandler<PacketEventArgs>? raiseEvent = OnPing;
        
        if (raiseEvent != null)
        {
            raiseEvent.Invoke(this, e);
        }
    }
    protected virtual void  OnDataPacket(PacketEventArgs e)
    {
        EventHandler<PacketEventArgs>? raiseEvent = OnData;
        
        if (raiseEvent != null)
        {
            raiseEvent.Invoke(this, e);
        }
    }
    protected virtual void OnFileSyncInitResponsePacket(PacketEventArgs e)
    {
        EventHandler<PacketEventArgs>? raiseEvent = OnFileSyncInitResponse;
        
        if (raiseEvent != null)
        {
            raiseEvent.Invoke(this, e);
        }
    }
    protected virtual void  OnFileSyncCheckHashResponsePacket(PacketEventArgs e)
    {
        EventHandler<PacketEventArgs>? raiseEvent = OnFileSyncCheckHashResponse;
        
        if (raiseEvent != null)
        {
            raiseEvent.Invoke(this, e);
        }
    }
}
