using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace jKnepel.NetcodeBenchmark.Controller
{
    public class BenchmarkController : MonoBehaviour
    {
        private enum EBenchmarkCommands : byte
        {
            StartServer = 1,
            StartClient = 2,
            StopServer = 3,
            StopClient = 4,
            DirectionalInput = 5,
            SetObjectNumber = 6
        }
        
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private MemoryStream _memoryStream;
        private bool _isRunning;

        [SerializeField] private UnityEvent startServer;
        [SerializeField] private UnityEvent stopServer;
        [SerializeField] private UnityEvent startClient;
        [SerializeField] private UnityEvent stopClient;
        [SerializeField] private UnityEvent<int> setObjectNumber;
        [SerializeField] private UnityEvent<int, Vector2> directionalInput;

        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 2 || !int.TryParse(args[1], out var port))
            {
                Application.Quit();
                return;
            }
            
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            DontDestroyOnLoad(gameObject);
            ConnectToServer(port);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public async void ConnectToServer(int port)
        {
            try
            {
                _tcpClient = new();
                Debug.Log($"Connecting to server at {port}...");
                await _tcpClient.ConnectAsync(IPAddress.Loopback, port);

                Debug.Log("Connected to server.");
                _stream = _tcpClient.GetStream();
                _memoryStream = new();
                _isRunning = true;

                await ReceiveMessagesAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect: {e.Message}");
            }
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }

            Debug.Log("Disconnected from server.");
        }
        
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024];

            while (_isRunning)
            {
                try
                {
                    if (_stream is not { CanRead: true })
                        break;

                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log("Server disconnected.");
                        break;
                    }

                    _memoryStream.Write(buffer, 0, bytesRead);

                    while (TryProcessMessages(_memoryStream)) {}
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error receiving data: {e.Message}");
                    break;
                }
            }

            Disconnect();
        }

        private bool TryProcessMessages(MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            var buffer = memoryStream.ToArray();

            var index = 0;
            var processedAnyMessage = false;

            while (index < buffer.Length)
            {
                if (index + 1 > buffer.Length)
                    break; // Incomplete message header

                var flag = (EBenchmarkCommands)buffer[index++];
                var length = 0;

                switch (flag)
                {
                    case EBenchmarkCommands.StartServer:
                    case EBenchmarkCommands.StartClient:
                    case EBenchmarkCommands.StopServer:
                    case EBenchmarkCommands.StopClient:
                        HandleMessage(flag, null);
                        processedAnyMessage = true;
                        continue;
                    case EBenchmarkCommands.DirectionalInput:
                        length = 12;
                        break;
                    case EBenchmarkCommands.SetObjectNumber:
                        length = 4;
                        break;
                }

                if (index + length > buffer.Length)
                    break; // Incomplete message body

                var message = new byte[length];
                Array.Copy(buffer, index, message, 0, length);
                index += length;

                HandleMessage(flag, message);
                processedAnyMessage = true;
            }

            var remainingBytes = buffer.Length - index;
            if (remainingBytes > 0)
            {
                memoryStream.SetLength(0);
                memoryStream.Write(buffer, index, remainingBytes);
            }
            else
            {
                memoryStream.SetLength(0); // Clear the stream if no data is left
            }


            return processedAnyMessage;
        }
        
        private void HandleMessage(EBenchmarkCommands flag, byte[] message)
        {
            switch (flag)
            {
                case EBenchmarkCommands.StartServer:
                    startServer?.Invoke();
                    break;
                case EBenchmarkCommands.StartClient:
                    startClient?.Invoke();
                    break;
                case EBenchmarkCommands.StopServer:
                    stopServer?.Invoke();
                    break;
                case EBenchmarkCommands.StopClient:
                    stopClient?.Invoke();
                    break;
                case EBenchmarkCommands.DirectionalInput:
                {
                    directionalInput?.Invoke(BitConverter.ToInt32(message, 0), new (
                        BitConverter.ToSingle(message, 4), 
                        BitConverter.ToSingle(message, 8)
                    ));
                    break;
                }
                case EBenchmarkCommands.SetObjectNumber:
                {
                    setObjectNumber?.Invoke(BitConverter.ToInt32(message, 0));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
