using System.Net;

namespace OpenFileShareV2
{
    internal class Program
    {
        const int Port = 5402;

        static string FilePath = string.Empty;
        static string FileName = string.Empty;

        static IPAddress? RemoteIP;

        static string WrongArgStr = "Use argument \"host\" or \"connect\" to use the program.";

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                await Console.Out.WriteLineAsync(WrongArgStr);
                return;
            }

            string argument = args[0].ToLower();

            bool host;
            try
            {
                host = argument switch
                {
                    "host" => true,
                    "connect" => false,
                    _ => throw new NotImplementedException(WrongArgStr)
                };
            }
            catch (NotImplementedException notImpEx)
            {
                await Console.Out.WriteLineAsync(notImpEx.Message);
                return;
            }

            await Initialize(host);
        }

        static async Task Initialize(bool host)
        {
            if (host)
                await Host();
            else
                await Join();
        }

        static async Task Host()
        {
            Console.Title = "Server";

            while (!SetFile());

            await Server.Host(Port);

            _ = Server.SendFile(FilePath, FileName);

            await Server.HandleCommunication();
        }

        static async Task Join()
        {
            Console.Title = "Client";

#if (DEBUG)
            Console.WriteLine("Since the program is run in Debug mode, using ip 127.0.0.1");
            RemoteIP = IPAddress.Parse("127.0.0.1");
#else
            while (!SetIP()) ;
#endif
            if (RemoteIP == null)
                return;

            await Client.Connect(RemoteIP, Port);
        }

        static bool SetFile()
        {
            try
            {
                Console.Write("Enter File Name: ");
                string fileName = Console.ReadLine() ?? string.Empty;

                if (!Path.Exists(fileName))
                    throw new IOException("Specified file cannot be found");                

                FilePath = Path.GetFullPath(fileName);
                FileName = Path.GetFileName(fileName);
                return true;
            }
            catch (IOException ioEx)
            {
                Console.WriteLine(ioEx.Message);
                return false;
            }
        }

        static bool SetIP()
        {
            try
            {
                Console.Write("Enter IP of the Server: ");
                IPAddress ip = IPAddress.Parse(Console.ReadLine() ?? string.Empty);

                RemoteIP = ip;
                return true;
            }
            catch (FormatException)
            {
                Console.WriteLine("Can't parse IP");
                return false;
            }
            catch
            {
                throw;
            }
        }
    }
}