using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketModule
{
    public class Server
    {
        // Instance & Connection Status
        private TcpListener _listener;
        private readonly int _port;
        private bool _running = false;

        // Data Set Status
        private List<Dictionary<string, string>> _dataList = new List<Dictionary<string, string>>();
        private BlockingCollection<Dictionary<string, string>> _sharedSpace = new BlockingCollection<Dictionary<string, string>>();
        private List<TcpClient> _clients = new List<TcpClient>();
        private int _indexCounter = 0;

        // Initialization
        public Server(int port)
        {
            _port = port;
        }

        // Server Task
        public void Start()
        {
            // Start Server
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            Console.WriteLine($"[Server] Server started on port {_port}"); // Log

            // Threads
            while (_running)
            {
                if (_listener.Pending())
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    lock (_clients)
                    {
                        _clients.Add(client);
                    }
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                    Console.WriteLine("[Server] Client connected"); // Log
                }
                else
                {
                    Thread.Sleep(100); // Await
                }
            }
        }

        // Tasks
        private void HandleClient(object obj)
        {
            // Define Client Stream
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            // Set Buffer
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (_running && (bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                // Decode Data & Load
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messages = receivedData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string message in messages)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        var dataEntry = new Dictionary<string, string>
                        {
                            { "Index", _indexCounter.ToString() },
                            { "Message", message.Trim() },
                            { "Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") }
                        };

                        _indexCounter++;
                        _dataList.Add(dataEntry);
                        _sharedSpace.Add(dataEntry);
                        BroadcastMessage(dataEntry);
                        Console.WriteLine("[Server] Data Received"); // Log
                    }
                }
            }
        }

        // Broadcast Message to Client
        private void BroadcastMessage(Dictionary<string, string> dataEntry)
        {
            // Reform & Encode Data
            string message = $"{dataEntry["Timestamp"]}: {dataEntry["Message"]} (Index: {dataEntry["Index"]}){Environment.NewLine}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Broadcast
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server] Error sending message to client: {ex.Message}");
                    }
                }
            }
        }

        public BlockingCollection<Dictionary<string, string>> GetSharedSpace()
        {
            return _sharedSpace;
        }

        public List<Dictionary<string, string>> GetDataList()
        {
            return _dataList;
        }

        // Stop Server
        public void Stop()
        {
            // Close Listener
            _listener.Stop();
            Console.WriteLine("[Server] Server stopped");
            _running = false;

            // Close Client Connection
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                }
            }
        }
    }

    public class Producer
    {
        // Instance
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        // Initialization
        public Producer(string server, int port)
        {
            _client = new TcpClient(server, port);
            _stream = _client.GetStream();
        }

        // Publish Message
        public void Publish(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            _stream.Write(data, 0, data.Length);
        }

        // Close Producer
        public void Close()
        {
            _stream.Close();
            _client.Close();
        }
    }

    public class Consumer
    {
        // Instance & Connection Status
        private readonly string _server;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _consumerThread;

        // Initialization
        public Consumer(string server, int port)
        {
            _server = server;
            _port = port;
            ConnectToServer();
        }

        // Connect to Server
        private void ConnectToServer()
        {
            _client = new TcpClient(_server, _port);
            _stream = _client.GetStream();
            _consumerThread = new Thread(StartConsuming);
            _consumerThread.Start();
        }

        // Consume Published Datas
        public void StartConsuming()
        {
            // Set Bufffer
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                // Decode Data & Reform
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messages = receivedData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string message in messages)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Console.WriteLine($"[Consumer] Consumed: {message}"); // Log

                        // for TEST
                        string filePath = @"C:\Users\dhkim\Desktop\SAMPLE.log";
                        string directoryPath = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        File.AppendAllText(filePath, message + Environment.NewLine);
                    }
                }
            }
        }

        // Stop Consumer
        public void Stop()
        {
            _stream.Close();
            _client.Close();
            _consumerThread.Join();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Start Server
            Server server = new Server(9000);
            Thread serverThread = new Thread(server.Start);
            serverThread.Start();

            Thread.Sleep(1000); // Await

            // Start Producer
            Producer producer1 = new Producer("127.0.0.1", 9000);
            Producer producer2 = new Producer("127.0.0.1", 9000);

            // Start Consumer
            Consumer consumer = new Consumer("127.0.0.1", 9000);

            // Publish Message
            producer1.Publish("Hello, Producer 1's 1st Message!");
            producer2.Publish("Hello, Producer 2's 1st Message!");
            producer1.Publish("Hello, Producer 1's 2nd Message!");
            producer2.Publish("Hello, Producer 2's 2nd Message!");

            // Read Key for Stop
            Console.ReadKey();

            // Stop Server
            Console.WriteLine(">> Will Stop Program.. ");
            server.Stop();
            serverThread.Join();

            // Stop Clients
            producer1.Close();
            producer2.Close();
            consumer.Stop();
        }
    }

}
