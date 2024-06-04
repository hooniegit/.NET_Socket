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
        private TcpListener _listener;
        private readonly int _port;
        private List<Dictionary<string, string>> _dataList = new List<Dictionary<string, string>>();
        private BlockingCollection<Dictionary<string, string>> _sharedSpace = new BlockingCollection<Dictionary<string, string>>();
        private List<TcpClient> _clients = new List<TcpClient>();
        private int _indexCounter = 0; // 인덱스 카운터
        private bool _running = false; // 서버 실행 상태 플래그

        public Server(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            Console.WriteLine($"[Server] Server started on port {_port}");

            while (_running)
            {
                if (_listener.Pending())
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    lock (_clients)
                    {
                        _clients.Add(client);
                    }
                    Console.WriteLine("[Server] Client connected");
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
                else
                {
                    Thread.Sleep(100); // 잠시 대기하여 CPU 사용률을 낮춥니다.
                }
            }

            _listener.Stop();
            Console.WriteLine("[Server] Server stopped");
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int bytesRead;

            while (_running && (bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
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

                        _indexCounter++; // 인덱스 증가
                        _dataList.Add(dataEntry); // Add to data list
                        _sharedSpace.Add(dataEntry); // Add to shared space
                        BroadcastMessage(dataEntry); // Broadcast message to all connected clients
                    }
                }
            }

            lock (_clients)
            {
                _clients.Remove(client);
            }
            client.Close();
        }

        private void BroadcastMessage(Dictionary<string, string> dataEntry)
        {
            string message = $"{dataEntry["Timestamp"]}: {dataEntry["Message"]} (Index: {dataEntry["Index"]}){Environment.NewLine}";
            byte[] data = Encoding.UTF8.GetBytes(message);

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

        public void Stop()
        {
            _running = false;
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
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        public Producer(string server, int port)
        {
            _client = new TcpClient(server, port);
            _stream = _client.GetStream();
        }

        public void Publish(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            _stream.Write(data, 0, data.Length);
        }

        public void Close()
        {
            _stream.Close();
            _client.Close();
        }
    }

    public class Consumer
    {
        private readonly string _server;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _consumerThread;

        public Consumer(string server, int port)
        {
            _server = server;
            _port = port;
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            _client = new TcpClient(_server, _port);
            _stream = _client.GetStream();
            _consumerThread = new Thread(StartConsuming);
            _consumerThread.Start();
        }

        public void StartConsuming()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messages = receivedData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string message in messages)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Console.WriteLine($"[Consumer] Consumed: {message}");

                        // 파일을 작성할 경로를 지정합니다.
                        string filePath = @"C:\Users\dhkim\Desktop\SAMPLE.log";

                        // 디렉터리가 존재하지 않으면 생성합니다.
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
            // 포트 번호를 지정하여 서버 인스턴스를 생성합니다.
            Server server = new Server(9000);
            Thread serverThread = new Thread(server.Start);
            serverThread.Start();

            Thread.Sleep(1000); // 서버가 시작될 시간을 줌

            Producer producer1 = new Producer("127.0.0.1", 9000);
            Producer producer2 = new Producer("127.0.0.1", 9000);

            // Consumer 인스턴스 생성 및 시작
            Consumer consumer = new Consumer("127.0.0.1", 9000);

            producer1.Publish("Hello from producer 1!");
            producer2.Publish("Hello from producer 2!");
            producer1.Publish("Another message from producer 1.");
            producer2.Publish("Another message from producer 2.");

            // 사용자가 키를 누를 때까지 대기
            Console.ReadKey();

            // 서버 및 프로그램 종료
            Console.WriteLine("프로그램을 종료합니다.");
            server.Stop();
            serverThread.Join(); // 서버 스레드가 종료될 때까지 대기

            producer1.Close();
            producer2.Close();
            consumer.Stop();
        }
    }

}
