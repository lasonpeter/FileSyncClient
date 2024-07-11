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

    public void AwaitPacket(Socket socket)
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
                try
                {
                    socket.Disconnect(true);
                    socket.Close();
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

    public void Ping(Socket socket,object socketLock)
    {
        byte[] pingPacket = new Packet(PacketType.Ping).ToBytes();
        var socketPing = new Thread( o =>
        {
                while(true){
                    try
                    {

                       {
                            var we = socket.SendAsync(pingPacket);
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
