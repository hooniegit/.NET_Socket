Imports System

Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

' ���� Ŭ����
Public Class SocketServer
    ' ��ü ����
    Private listener As TcpListener
    Private client As TcpClient
    Private networkStream As NetworkStream
    Private port As Integer
    Private serverThread As Thread

    ' �ν��Ͻ� �ʱ�ȭ (���� �� ����� ��Ʈ�� ����)
    Public Sub New(port As Integer)
        Me.port = port
        listener = New TcpListener(IPAddress.Any, port)
    End Sub

    ' ���� ���� - ������ �����忡�� ����
    Public Sub StartServer()
        serverThread = New Thread(New ThreadStart(AddressOf ListenForClients))
        serverThread.Start()
        Console.WriteLine("Server started on port " & port)
    End Sub

    ' Ŭ���̾�Ʈ ������
    Private Sub ListenForClients()
        ' ������ ����
        listener.Start()

        While True
            ' ��Ʈ�� ����
            client = listener.AcceptTcpClient()
            networkStream = client.GetStream()
            ' Ŭ���̾�Ʈ ������ ���� �� ����
            Dim clientThread As New Thread(New ThreadStart(AddressOf HandleClientCommunication))
            clientThread.Start()
        End While
    End Sub

    ' Ŭ���̾�Ʈ ��� ó�� (�ش� �Լ��� ���ڿ� ������ �������� ó��)
    Private Sub HandleClientCommunication()
        Dim buffer(1024) As Byte
        Dim bytesRead As Integer

        While True
            ' ���� ������(buffer) �� ����Ʈ ��(bytesRead) ����
            bytesRead = networkStream.Read(buffer, 0, buffer.Length)
            If bytesRead = 0 Then
                Exit While
            End If

            ' ���� ������ ��ȯ (����Ʈ �� > ���ڿ�)
            Dim message As String = Encoding.ASCII.GetString(buffer, 0, bytesRead)
            Console.WriteLine("Received: " & message)

            ' ���� �޼��� ���ڵ� & Ŭ���̾�Ʈ�� ����
            Dim response As String = "Message received"
            Dim responseBytes() As Byte = Encoding.ASCII.GetBytes(response)
            networkStream.Write(responseBytes, 0, responseBytes.Length)
        End While

        client.Close()
    End Sub

    ' ���� ����
    Public Sub StopServer()
        listener.Stop()
        If client IsNot Nothing Then client.Close()
        If networkStream IsNot Nothing Then networkStream.Close()
        If serverThread IsNot Nothing Then serverThread.Abort()
        Console.WriteLine("Server stopped.")
    End Sub
End Class

' Ŭ���̾�Ʈ Ŭ����
Public Class SocketClient
    Private client As TcpClient
    Private networkStream As NetworkStream
    Private serverAddress As String
    Private port As Integer

    ' �ν��Ͻ� �ʱ�ȭ
    Public Sub New(serverAddress As String, port As Integer)
        Me.serverAddress = serverAddress
        Me.port = port
    End Sub

    ' ���� ����
    Public Sub Connect()
        client = New TcpClient(serverAddress, port)
        networkStream = client.GetStream()
        Console.WriteLine("Connected to server.")
    End Sub

    ' �޼��� �Ǵ� ������ ����
    Public Sub SendMessage(message As String)
        ' �޼��� ���ڵ� �� ����
        Dim messageBytes() As Byte = Encoding.ASCII.GetBytes(message)
        networkStream.Write(messageBytes, 0, messageBytes.Length)
        Console.WriteLine("Sent: " & message)

        ' ���� �޼��� ���ڵ� �� ���
        Dim buffer(1024) As Byte
        Dim bytesRead As Integer = networkStream.Read(buffer, 0, buffer.Length)
        Dim response As String = Encoding.ASCII.GetString(buffer, 0, bytesRead)
        Console.WriteLine("Received: " & response)
    End Sub

    ' ���� ����
    Public Sub Disconnect()
        If networkStream IsNot Nothing Then networkStream.Close()
        If client IsNot Nothing Then client.Close()
        Console.WriteLine("Disconnected from server.")
    End Sub
End Class

Module MainModule
    Sub Main()
        ' 5000�� ��Ʈ�� ���� ���� ���� & ����
        Dim server As New SocketServer(5000)
        server.StartServer()

        ' ���� ������ ���� �ð� ���
        Threading.Thread.Sleep(1000)

        ' Ŭ���̾�Ʈ ��� & ������ ����
        Dim client As New SocketClient("127.0.0.1", 5000)
        client.Connect()
        client.SendMessage("Hello, Socket!")
        client.Disconnect()

        ' �׽�Ʈ �Ϸ� �� ���� ����
        server.StopServer()
    End Sub
End Module
