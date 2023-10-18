using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TuioNet.Common
{
    public class UdpTuioReceiver : TuioReceiver
    {
        private readonly int _port;

        private bool isUdpClientActive;

        internal UdpTuioReceiver(int port, bool isAutoProcess) : base(isAutoProcess)
        {
            _port = port;
        }
        
        /// <summary>
        /// Establish a connection to the TUIO sender over UDP.
        /// </summary>
        internal override void Connect()
        {
            if (isUdpClientActive) return;

            CancellationToken cancellationToken = CancellationTokenSource.Token;
            Task.Run(async () =>
            {
                using (var udpClient = new UdpClient(_port))
                {
                    isUdpClientActive = true;

                    while (true)
                    {
                        try
                        {
                            var receivedResults = await udpClient.ReceiveAsync();

                            if (!IsConnected && udpClient.Available > 0) IsConnected = true;

                            OnBuffer(receivedResults.Buffer, receivedResults.Buffer.Length);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                isUdpClientActive = false;
                IsConnected = false;
            });

            isUdpClientActive = false;
            IsConnected = false;
        }
    }
}