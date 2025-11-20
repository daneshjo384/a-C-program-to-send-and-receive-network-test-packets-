using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class NetworkTest
{
    private const int Port = 12345;
    private const string TestMessage = "Network Test Packet";
    private const int TimeoutMs = 5000;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Network Test Program");
        Console.WriteLine("Select Protocol:");
        Console.WriteLine("1. TCP");
        Console.WriteLine("2. UDP");
        Console.WriteLine("3. ICMP (Ping)");
        Console.Write("Enter choice (1-3): ");

        var choice = Console.ReadLine();
        Console.Write("Enter target IP address: ");
        var targetIp = Console.ReadLine();

        if (!IPAddress.TryParse(targetIp, out var ipAddress))
        {
            Console.WriteLine("Invalid IP address!");
            return;
        }

        switch (choice)
        {
            case "1":
                await TcpTest(ipAddress);
                break;
            case "2":
                await UdpTest(ipAddress);
                break;
            case "3":
                await IcmpTest(ipAddress);
                break;
            default:
                Console.WriteLine("Invalid choice!");
                break;
        }
    }

    private static async Task TcpTest(IPAddress targetIp)
    {
        Console.WriteLine("\nStarting TCP Test...");
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(targetIp, Port);
            var timeoutTask = Task.Delay(TimeoutMs);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                Console.WriteLine("TCP: Connection timed out");
                return;
            }

            if (connectTask.IsCompletedSuccessfully)
            {
                using var stream = client.GetStream();
                var data = Encoding.ASCII.GetBytes(TestMessage);
                await stream.WriteAsync(data, 0, data.Length);

                var buffer = new byte[256];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                timeoutTask = Task.Delay(TimeoutMs);

                if (await Task.WhenAny(readTask, timeoutTask) == timeoutTask)
                {
                    Console.WriteLine("TCP: Receive timed out");
                    return;
                }

                var received = Encoding.ASCII.GetString(buffer, 0, readTask.Result);
                Console.WriteLine($"TCP: Sent '{TestMessage}', Received '{received}'");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"TCP Error: {e.Message}");
        }
    }

    private static async Task UdpTest(IPAddress targetIp)
    {
        Console.WriteLine("\nStarting UDP Test...");
        try
        {
            using var client = new UdpClient();
            var endPoint = new IPEndPoint(targetIp, Port);

            var data = Encoding.ASCII.GetBytes(TestMessage);
            await client.SendAsync(data, data.Length, endPoint);

            var receiveTask = client.ReceiveAsync();
            var timeoutTask = Task.Delay(TimeoutMs);

            if (await Task.WhenAny(receiveTask, timeoutTask) == timeoutTask)
            {
                Console.WriteLine("UDP: Receive timed out");
                return;
            }

            var response = await receiveTask;
            var received = Encoding.ASCII.GetString(response.Buffer);
            Console.WriteLine($"UDP: Sent '{TestMessage}', Received '{received}' from {response.RemoteEndPoint}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"UDP Error: {e.Message}");
        }
    }

    private static async Task IcmpTest(IPAddress targetIp)
    {
        Console.WriteLine("\nStarting ICMP Test...");
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(targetIp, TimeoutMs);

            switch (reply.Status)
            {
                case IPStatus.Success:
                    Console.WriteLine($"ICMP: Reply from {targetIp} - Time={reply.RoundtripTime}ms TTL={reply.Options.Ttl}");
                    break;
                case IPStatus.TimedOut:
                    Console.WriteLine("ICMP: Request timed out");
                    break;
                default:
                    Console.WriteLine($"ICMP: {reply.Status}");
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"ICMP Error: {e.Message}");
        }
    }
}