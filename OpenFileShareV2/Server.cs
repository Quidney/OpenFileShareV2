using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OpenFileShareV2
{
    internal static class Server
    {
        static TcpListener? Listener = null;
        static TcpClient? Client = null;
        static NetworkStream? Stream = null;

        internal const int BufferSize = 16384;

        internal const char Delimiter = '\n';
        internal const char COMMAND_DELIMITER = '*';
        internal const char DATA_DELIMITER = '?';

        //Initializes the TcpListener
        internal static async Task Host(int port)
        {
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();

            await Console.Out.WriteLineAsync("Server is now open. Waiting for connection...");

            await AwaitClient();
        }

        //Awaits a single client before continuing
        private static async Task AwaitClient()
        {
            if (Listener == null)
                return;

            Client = await Listener.AcceptTcpClientAsync();
            Stream = Client.GetStream();

            await Console.Out.WriteLineAsync("Client connected.");
        }

        internal static async Task HandleCommunication()
        {
            if (Client == null || Stream == null) return;

            try
            {
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await Stream.ReadAsync(buffer)) > 0)
                {
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    await ProcessData(data);
                }
            }
            catch
            {

            }
        }

        private static Task ProcessData(string rawData)
        {
            if (Stream == null || Client == null)
                return Task.CompletedTask;

            string[] rawSplit = rawData.Split(COMMAND_DELIMITER);
            string command = rawSplit[0];

            switch (command)
            {
                case "FileReceived":
                    Client.Close();
                    break;

                default: break;
            }

            return Task.CompletedTask;
        }

        internal static async Task SendData(string data)
        {
            if (Stream == null)
                return;

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data + Delimiter);

                await Stream.WriteAsync(buffer);
            }
            catch { throw; }
        }

        internal static async Task SendFile(string filePath, string fileName)
        {
            if (Stream == null) return;

            using FileStream fileStream = new(filePath, FileMode.Open);
            long fileSizeBytes = fileStream.Length;

            await Console.Out.WriteLineAsync($"File name: {fileName}");
            await Console.Out.WriteLineAsync($"File size: {fileSizeBytes} bytes");

            await Console.Out.WriteLineAsync($"Sending file...");

            await SendData($"StartReceive{COMMAND_DELIMITER}{fileName}{DATA_DELIMITER}{fileStream.Length}");

            long bytesLeft = fileSizeBytes;
            long totalBytesRead = 0;

            await Console.Out.WriteLineAsync("");
            while (bytesLeft > BufferSize)
            {
                int bytesRead = await WriteToStream(fileStream, BufferSize);
                await LogProgress(totalBytesRead, fileSizeBytes);

                bytesLeft -= bytesRead;
                totalBytesRead += bytesRead;
            }

            await WriteToStream(fileStream, bytesLeft);
            await LogProgress(totalBytesRead, fileSizeBytes);

            await Console.Out.WriteLineAsync("File sending successful");
        }

        internal static async Task<int> WriteToStream(FileStream fs, long bufferSize)
        {
            if (Stream == null) return 0;

            byte[] buffer = new byte[bufferSize];
            int bytesRead = await fs.ReadAsync(buffer);
            await Stream.WriteAsync(buffer.AsMemory(0, bytesRead));

            return bytesRead;
        }

        internal static async Task LogProgress(long totalBytesRead, long fileSizeBytes)
        {
            Console.CursorTop--;
            await Console.Out.WriteLineAsync($"Progress: {Math.Round(100f * totalBytesRead / fileSizeBytes, 2)}%");
        }
    }
}
