using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Client
{
    private static TcpClient client;
    private static NetworkStream stream;
    private static bool isConnected = false;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter YOUR local IP:");
        var localIp = IPAddress.Parse(Console.ReadLine());

        Console.WriteLine("Enter server IP:");
        var serverIp = IPAddress.Parse(Console.ReadLine());

        Console.WriteLine("Enter server port:");
        var port = int.Parse(Console.ReadLine());

        try
        {
            
            var localEndPoint = new IPEndPoint(localIp, 0);
            client = new TcpClient(localEndPoint);

            await client.ConnectAsync(serverIp, port);
            stream = client.GetStream();
            isConnected = true;

            Console.WriteLine($"Connected from {localIp} to server {serverIp}:{port}");
            Console.WriteLine("Type messages and press Enter");
            Console.WriteLine("Type '/exit' to disconnect");

            _ = Task.Run(ReceiveMessagesAsync);

            while (isConnected)
            {
                var message = Console.ReadLine();
                if (message == "/exit") break;

                if (!string.IsNullOrEmpty(message))
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    private static async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];
        while (isConnected)
        {
            try
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"{message}");
            }
            catch
            {
                break;
            }
        }
        Disconnect();
    }

    private static void Disconnect()
    {
        if (!isConnected) return;

        isConnected = false;
        try
        {
            client?.Close();
            Console.WriteLine("Disconnected from server");
        }
        catch { }
    }
}