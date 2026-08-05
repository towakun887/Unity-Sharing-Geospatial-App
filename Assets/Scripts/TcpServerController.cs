using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TcpServerController : MonoBehaviour
{
    public int ServerPort = 5000;
    private TcpListener _tcpListener;
    private Thread _serverThread;
    private bool _isRunning = false;

    public bool IsServerRunning => _isRunning;
    private int _requestCount = 0;
    public int RequestCount => _requestCount;

    public string ExpectedDeviceId { get; set; }
    public string LastConnectionInfo { get; private set; } = "No connection yet";

    public GeospatialController geospatialController;

    public void StartServer()
    {
        if (_isRunning) return;
        _isRunning = true;
        _serverThread = new Thread(ListenForIncomingRequests)
        {
            IsBackground = true
        };
        _serverThread.Start();
        Debug.Log($"TCP Server started on port {ServerPort}");
    }

    private void ListenForIncomingRequests()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, ServerPort);
            _tcpListener.Start();

            while (_isRunning)
            {
                if (_tcpListener.Pending())
                {
                    TcpClient connectedTcpClient = _tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, connectedTcpClient);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.LogError($"SocketException: {socketException}");
        }
    }

    private void HandleClient(object clientObj)
    {
        using (TcpClient client = (TcpClient)clientObj)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] bytes = new byte[1024];
            int length;
            // Read incoming message
            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                var incomingData = new byte[length];
                Array.Copy(bytes, 0, incomingData, 0, length);
                string clientMessage = Encoding.ASCII.GetString(incomingData);
                Interlocked.Increment(ref _requestCount);
                Debug.Log($"Client request received: {clientMessage}");

                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                // Validate Device ID
                if (!string.IsNullOrEmpty(ExpectedDeviceId) && !clientMessage.Contains(ExpectedDeviceId))
                {
                    LastConnectionInfo = $"[{DateTime.Now:HH:mm:ss}] Rejected: Invalid ID from {clientIp}";
                    Debug.LogWarning($"Rejected connection from {clientIp}. Expected ID: {ExpectedDeviceId}");
                    
                    // Send error reply
                    byte[] errorMsg = Encoding.UTF8.GetBytes("{\"error\": \"Invalid Device ID\"}");
                    stream.Write(errorMsg, 0, errorMsg.Length);
                }
                else
                {
                    LastConnectionInfo = $"[{DateTime.Now:HH:mm:ss}] Connected: {clientIp}";
                    
                    // Get Geospatial Data JSON
                    string locationJson = geospatialController.GetLocationJson();
                    
                    // Send reply
                    byte[] serverMessageAsByteArray = Encoding.UTF8.GetBytes(locationJson);
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                    Debug.Log("Server replied with location data.");
                }
                
                // We close the connection after answering the request to simplify.
                break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
        _tcpListener?.Stop();
        if (_serverThread != null && _serverThread.IsAlive)
        {
            _serverThread.Join(500);
        }
    }

    public static string GetLocalIPAddress()
    {
        // Use UDP socket connection to an arbitrary endpoint to force OS to resolve the preferred local IP.
        // This is much more reliable on mobile devices and systems with multiple adapters.
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            try 
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint != null)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch 
            {
                // Fallback if network is completely down
            }
        }

        // Fallback to iterating host entries
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
