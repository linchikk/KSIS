using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private static TcpListener listener;
    private static List<TcpClient> clients = new List<TcpClient>();
    private static bool isRunning = false;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter server IP:");
        var ip = IPAddress.Parse(Console.ReadLine());

        Console.WriteLine("Enter server port:");
        var port = int.Parse(Console.ReadLine());

        try
        {
            listener = new TcpListener(ip, port);
            listener.Start();
            isRunning = true;
            Console.WriteLine($"Server started on {ip}:{port}");

            _ = Task.Run(AcceptClientsAsync);
            Console.WriteLine("Press Q to stop server");

            while (isRunning && Console.ReadKey(true).Key != ConsoleKey.Q) ;

            StopServer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task AcceptClientsAsync()
    {
        while (isRunning)
        {
            var client = await listener.AcceptTcpClientAsync();
            lock (clients) clients.Add(client);
            var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            Console.WriteLine($"New client connected from: {clientIp}");
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

        try
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            while (isRunning)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var formattedMessage = $"[{timestamp}] [{clientIp}] {message}";
                Console.WriteLine($"Received: {formattedMessage}");
                BroadcastMessage(formattedMessage, client);
            }
        }
        finally
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var disconnectMessage = $"[{timestamp}] [Server] Client {clientIp} disconnected";

            lock (clients)
            {
                clients.Remove(client);
                BroadcastMessage(disconnectMessage, null);
            }

            Console.WriteLine(disconnectMessage);
            client.Dispose();
        }
    }

    private static void BroadcastMessage(string message, TcpClient sender)
    {
        var data = Encoding.UTF8.GetBytes(message);
        lock (clients)
        {
            foreach (var client in clients)
            {
                if (client != sender && client.Connected)
                {
                    client.GetStream().WriteAsync(data, 0, data.Length);
                }
            }
        }
    }

    private static void StopServer()
    {
        isRunning = false;
        listener.Stop();
        lock (clients)
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
            clients.Clear();
        }
        Console.WriteLine("Server stopped");
    }
}