using System.Net;
using System.Net.Sockets;
using TransferLib;

namespace FileSyncClient;

public class PeerConnection
{
    public async Task<Socket> ConnectToServer(string host)
    {
        IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(host);
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        var hostName = Dns.GetHostName();
        IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
// This is the IP address of the local machine
        IPAddress localIpAddress = localhost.AddressList[0];
        IPEndPoint ipEndPoint = new(ipAddress, 11_000);
        
        using Socket listener = new(
            ipEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        listener.Bind(ipEndPoint);
        listener.Listen(100);
        var socket= await listener.AcceptAsync();
        socket.ReceiveTimeout = 10_000;
        return socket;
    }
}