Imports System

Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

' 서버 클래스
Public Class SocketServer
    ' 객체 생성
    Private listener As TcpListener
    Private client As TcpClient
    Private networkStream As NetworkStream
    Private port As Integer
    Private serverThread As Thread

    ' 인스턴스 초기화 (생성 시 사용할 포트를 지정)
    Public Sub New(port As Integer)
        Me.port = port
        listener = New TcpListener(IPAddress.Any, port)
    End Sub

    ' 서버 시작 - 별도의 스레드에서 시작
    Public Sub StartServer()
        serverThread = New Thread(New ThreadStart(AddressOf ListenForClients))
        serverThread.Start()
        Console.WriteLine("Server started on port " & port)
    End Sub

    ' 클라이언트 리스닝
    Private Sub ListenForClients()
        ' 리스너 시작
        listener.Start()

        While True
            ' 스트림 시작
            client = listener.AcceptTcpClient()
            networkStream = client.GetStream()
            ' 클라이언트 스레드 정의 및 시작
            Dim clientThread As New Thread(New ThreadStart(AddressOf HandleClientCommunication))
            clientThread.Start()
        End While
    End Sub

    ' 클라이언트 통신 처리 (해당 함수는 문자열 데이터 한정으로 처리)
    Private Sub HandleClientCommunication()
        Dim buffer(1024) As Byte
        Dim bytesRead As Integer

        While True
            ' 읽은 데이터(buffer) 및 바이트 수(bytesRead) 저장
            bytesRead = networkStream.Read(buffer, 0, buffer.Length)
            If bytesRead = 0 Then
                Exit While
            End If

            ' 수신 데이터 변환 (바이트 수 > 문자열)
            Dim message As String = Encoding.ASCII.GetString(buffer, 0, bytesRead)
            Console.WriteLine("Received: " & message)

            ' 응답 메세지 인코딩 & 클라이언트로 전송
            Dim response As String = "Message received"
            Dim responseBytes() As Byte = Encoding.ASCII.GetBytes(response)
            networkStream.Write(responseBytes, 0, responseBytes.Length)
        End While

        client.Close()
    End Sub

    ' 서버 종료
    Public Sub StopServer()
        listener.Stop()
        If client IsNot Nothing Then client.Close()
        If networkStream IsNot Nothing Then networkStream.Close()
        If serverThread IsNot Nothing Then serverThread.Abort()
        Console.WriteLine("Server stopped.")
    End Sub
End Class

' 클라이언트 클래스
Public Class SocketClient
    Private client As TcpClient
    Private networkStream As NetworkStream
    Private serverAddress As String
    Private port As Integer

    ' 인스턴스 초기화
    Public Sub New(serverAddress As String, port As Integer)
        Me.serverAddress = serverAddress
        Me.port = port
    End Sub

    ' 서버 연결
    Public Sub Connect()
        client = New TcpClient(serverAddress, port)
        networkStream = client.GetStream()
        Console.WriteLine("Connected to server.")
    End Sub

    ' 메세지 또는 데이터 전송
    Public Sub SendMessage(message As String)
        ' 메세지 인코딩 및 전송
        Dim messageBytes() As Byte = Encoding.ASCII.GetBytes(message)
        networkStream.Write(messageBytes, 0, messageBytes.Length)
        Console.WriteLine("Sent: " & message)

        ' 응답 메세지 인코딩 및 출력
        Dim buffer(1024) As Byte
        Dim bytesRead As Integer = networkStream.Read(buffer, 0, buffer.Length)
        Dim response As String = Encoding.ASCII.GetString(buffer, 0, bytesRead)
        Console.WriteLine("Received: " & response)
    End Sub

    ' 연결 종료
    Public Sub Disconnect()
        If networkStream IsNot Nothing Then networkStream.Close()
        If client IsNot Nothing Then client.Close()
        Console.WriteLine("Disconnected from server.")
    End Sub
End Class

Module MainModule
    Sub Main()
        ' 5000번 포트에 소켓 서버 구성 & 시작
        Dim server As New SocketServer(5000)
        server.StartServer()

        ' 서버 실행을 위한 시간 대기
        Threading.Thread.Sleep(1000)

        ' 클라이언트 통신 & 데이터 전송
        Dim client As New SocketClient("127.0.0.1", 5000)
        client.Connect()
        client.SendMessage("Hello, Socket!")
        client.Disconnect()

        ' 테스트 완료 후 서버 종료
        server.StopServer()
    End Sub
End Module
