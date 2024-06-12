using System.Net;
using System.Net.Sockets;
using TransferLib;

namespace FileSyncClient;

class PacketDistributor
{
    public event EventHandler<PacketEventArgs>? OnPing;
    public event EventHandler<PacketEventArgs>? OnData;
    public event EventHandler<PacketEventArgs>? OnFileSyncInitResponse;
    public event EventHandler<PacketEventArgs>? OnFileSyncCheckHashResponse;

    public void AwaitPacket(Socket socket, IPAddress ipAddress, int port, TcpClient tcpClient)
    {
        new Thread(o =>
        {
            var buffer = new byte[4_099];
            Packet packet = new Packet();
            while (true)
            {
                Array.Clear(buffer);
                // Receive message.
                var received = socket.Receive(buffer, SocketFlags.None);
                if (received == 0)
                {
                    Console.WriteLine("Connection Lost");
                    socket.Disconnect(true);
                    int x = 0;
                    while (x <5)
                    {
                        try
                        {
                            try
                            {
                                tcpClient = new TcpClient();
                                tcpClient.Connect(ipAddress,port);
                                socket = tcpClient.Client;
                                Console.WriteLine("Connection has been reestablished");
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        Thread.Sleep(1000);
                        x++;
                    }
                    
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
                }
            } 
        }).Start();
    }

    public void Ping(Socket socket)
    {
        byte[] pingPacket = new Packet(PacketType.Ping).ToBytes();
        var socketPing = new Thread( o =>
        {
            lock (socket)
            {
                while(true){
                    try
                    {
                        var we = socket.SendAsync(pingPacket);
                        Thread.Sleep(1000);
                        Console.WriteLine("SENT:"+we.Result);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }   
            }
        });
        socketPing.Start();
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
