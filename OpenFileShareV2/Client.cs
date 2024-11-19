using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OpenFileShareV2
{
    internal static class Client
    {
        static TcpClient? TcpClient = null;
        static NetworkStream? Stream = null;

        const int BufferSize = Server.BufferSize;

        const char Delimiter = Server.Delimiter;
        const char COMMAND_DELIMITER = Server.COMMAND_DELIMITER;
        const char DATA_DELIMITER = Server.DATA_DELIMITER;

        internal static async Task Connect(IPAddress ip, int port)
        {
            await Console.Out.WriteLineAsync($"Connecting to remote host {ip}:{port}");

            TcpClient = new TcpClient();
            await TcpClient.ConnectAsync(ip, port);
            Stream = TcpClient.GetStream();

            await Console.Out.WriteLineAsync("Connected.");

            await HandleCommunication();
        }
        private static async Task HandleCommunication()
        {
            if (Stream == null)
                return;

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
            catch (IOException ioEx)
            {
                await Console.Out.WriteLineAsync(ioEx.Message);
            }
            catch { throw; }
        }

        private static async Task ProcessData(string rawData)
        {
            string[] rawSplit = rawData.Split(COMMAND_DELIMITER);
            string command = rawSplit[0];

            string[] dataSplit;
            if (rawSplit.Length > 1)
            {
                dataSplit = rawSplit[1].Split(DATA_DELIMITER);
            }
            else
            {
                dataSplit = [];
            }

            switch (command)
            {
                case "StartReceive":
                    await Console.Out.WriteLineAsync($"Response from the server: Start File Transfer");

                    string fileName = dataSplit[0];
                    await Console.Out.WriteLineAsync($"File Name: {fileName}");

                    string fileSizeBytesStr = dataSplit[1];
                    long fileSizeBytes = long.Parse(fileSizeBytesStr);

                    (long, string) FileSizeTuple = GetFileSize(fileSizeBytes);
                    await Console.Out.WriteLineAsync($"File Size: {fileSizeBytes} bytes ({FileSizeTuple.Item1} {FileSizeTuple.Item2})");

                    await ReceiveFile(fileName, fileSizeBytes);

                    break;

                default: break;
            }
        }

        internal static async Task SendData(string data)
        {
            if (Stream == null)
                return;

            await Console.Out.WriteLineAsync($"Sending data to server: " + data);

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data + Delimiter);

                await Stream.WriteAsync(buffer);
            }
            catch { throw; }
        }

        internal static (long, string) GetFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
            int index = 0;
            
            while (bytes > 1024)
            {
                bytes /= 1024;
                index++;
            }

            return (bytes, sizes[index]);
        }

        internal static async Task ReceiveFile(string fileName, long fileSizeBytes)
        {
            if (Stream == null)
                return;

            await Console.Out.WriteLineAsync($"File Mode Active.");

            using FileStream fileStream = new(fileName, FileMode.Create);

            long bytesLeft = fileSizeBytes;
            long totalBytesRead = 0;

            await Console.Out.WriteLineAsync("");
            while (bytesLeft > BufferSize)
            {
                int bytesRead = await ReceiveFile(fileStream, BufferSize);
                await LogProgress(totalBytesRead, fileSizeBytes);

                bytesLeft -= bytesRead;
                totalBytesRead += bytesRead;
            }

            await ReceiveFile(fileStream, bytesLeft);
            await LogProgress(totalBytesRead, fileSizeBytes);

            await Console.Out.WriteLineAsync("File received.");

            fileStream.Close();

            await SendData($"FileReceived{COMMAND_DELIMITER}");
        }

        internal static async Task<int> ReceiveFile(FileStream fs, long bufferSize)
        {
            if (Stream == null) return 0;

            byte[] buffer = new byte[bufferSize];
            int bytesRead = await Stream.ReadAsync(buffer);
            fs.Write(buffer, 0, bytesRead);

            return bytesRead;
        }

        internal static async Task LogProgress(long totalBytesRead, long fileSizeBytes)
        {
            Console.CursorTop--;
            await Console.Out.WriteLineAsync($"Progress: {Math.Round(100f * totalBytesRead / fileSizeBytes, 2)}%");
        }
    }
}