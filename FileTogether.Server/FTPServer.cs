using System.Net;
using System.Net.Sockets;

namespace FileTogether.Server;

public class FTPServer
{
    private Socket _listenerSocket;
    private Thread _listenerThread;
    private bool bRunning;
    private string _sharedFolder;
    private int _port;
    
    private List<ClientHandler>  _clientHandlers =  new List<ClientHandler>();
    
    public event Action<string> OnLog; // Event ghi log lên UI
    public event Action<int> OnClientCountChanged; // Event báo số client thay đổi
    
    public bool IsRunning => bRunning;
    public int Port => _port;
    public string ShareFolder => _sharedFolder;
    
    public FTPServer(int port, string sharedFolder)
    {
        _port = port;
        _sharedFolder = sharedFolder;
        
        if (!System.IO.Directory.Exists(_sharedFolder)) System.IO.Directory.CreateDirectory(_sharedFolder);
    }
    
    public void Start()
    {
        if (bRunning) return;
            
        try
        {
            _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
            _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, _port));//Register Address
            _listenerSocket.Listen(10); // Tối đa 10 client chờ trong queue
                
            bRunning = true;
                
            _listenerThread = new Thread(ListenForClients);
            _listenerThread.IsBackground = true;
            _listenerThread.Start();
                
            Log($"Server started on port {_port}");
            Log($"Shared folder: {_sharedFolder}");
        }
        catch (Exception ex)
        {
            Log($"Failed to start server: {ex.Message}");
            throw;
        }
    }

    private void ListenForClients()
    {
        while (bRunning)
            try
            {
                var clientSk =  _listenerSocket.Accept();
                var clientHandler = new ClientHandler(clientSk, _sharedFolder);
                lock (_clientHandlers)
                {
                    _clientHandlers.Add(clientHandler);
                    OnClientCountChanged?.Invoke(_clientHandlers.Count);
                }
                clientHandler.Start();
                Log($"New client accepted. Total clients: {_clientHandlers.Count}");
            }
            catch (Exception e)
            {
                Log($"Error accepting client: {e.Message}");
                Console.WriteLine(e);
            }
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void Stop()
    {
        if (!bRunning) return;
            
        bRunning = false;
            
        try
        {
            _listenerSocket?.Close();
            
            lock (_clientHandlers)
            {
                _clientHandlers.Clear();
            }
                
            Log("Server stopped");
            OnClientCountChanged?.Invoke(0);
        }
        catch (Exception ex)
        {
            Log($"Error stopping server: {ex.Message}");
            Console.WriteLine(ex);
        }
    }
}