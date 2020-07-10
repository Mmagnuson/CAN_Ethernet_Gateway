using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CANEthernetGateway
{
    public class CanGateway
    {
        public object SyncRoot;

        private static Thread _thread;
        private int _inboundPort;
        private IPAddress _remoteAddress;
        private int _remotePort;
        private Dictionary<int, CanMessageDefinition> _messageDict;
        private readonly ManualResetEvent _stopping;
       // private ManualResetEvent _dataReceived; // ToDo: Add events for data recieved;
        private IAsyncResult _udpResult;
        private UdpClient _udpcanServer;
        private UdpClient _udpcanClient;
        private IPEndPoint _remoteClient;
        private IPEndPoint _remoteSender;
        private DateTime _lastPacketTime;

        public IList<CanMessageDefinition> Messages { get; private set; }

        public CanGateway()
        {
            SyncRoot = new object();

           // _dataReceived = new ManualResetEvent(false);
            _stopping = new ManualResetEvent(false);
        }

        public bool Connected => (DateTime.Now - _lastPacketTime).TotalMilliseconds <= 500;

        public void CanBusDefLoad(IList<CanMessageDefinition> messages)
        {
            this.Messages = messages;
            this._messageDict = Messages.ToDictionary(x => x.Id);
        }

        public void Configuration(int inboundPort, IPAddress remoteAddress, int remotePort)

        {
            _inboundPort = inboundPort;
            _remoteAddress = remoteAddress;
            _remotePort = remotePort;
        }

        public void StartProcess()
        {
            lock (SyncRoot)
            {
                _thread = new Thread(ThreadWorker) {IsBackground = true, Name = "CAN Bus UDP Thread "};
                _thread.Start();
            }
        }

        public void StopProcess()
        {
            try
            {
                this._stopping.Set();
                _udpcanServer.Client.Shutdown(SocketShutdown.Receive);
                _udpcanServer.Client.Close();
            }
            catch
            {
                // ignored
            }
        }

        private void ThreadWorker()
        {
        }

        public void Connect()
        {
            _remoteClient = new IPEndPoint(_remoteAddress, _remotePort);
            _udpcanClient = new UdpClient();
            _udpcanClient.Connect(_remoteClient);
            lock (SyncRoot)
            {
                _remoteSender = new IPEndPoint(IPAddress.Any, 0);
                _udpcanServer = new UdpClient(_inboundPort);
                UdpState state = new UdpState(_udpcanServer, _remoteSender);
                _udpResult = _udpcanServer.BeginReceive(DataReceived, state);
            }
        }

        public void Disconnect()
        {
            _udpcanClient.Close();
            try
            {
                this._stopping.Set();
                _udpcanServer.Client.Shutdown(SocketShutdown.Receive);
                _udpcanServer.Client.Close();
            }
            catch
            {
                // ignored
            }
        }

        public void DataSend(byte[] msg)
        {
            _udpcanClient.Send(msg, msg.Length);
        }

        private void DataReceived(IAsyncResult ar)
        {
            try
            {
                UdpClient c = ((UdpState)ar.AsyncState).C;
                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);

                IpcanMsg canFrame = new IpcanMsg();

                canFrame.DecodeCanPacket(receiveBytes);

                if (_messageDict.ContainsKey(canFrame.MessageId))
                {
                    if (_messageDict[canFrame.MessageId].IsMulticast == false)
                    {
                        _messageDict[canFrame.MessageId].DecodeMessage(canFrame);
                        _lastPacketTime = DateTime.Now;
                    }
                    else
                    {
                        _messageDict[canFrame.MessageId].DecodeMessageMultiplex(canFrame);
                        _lastPacketTime = DateTime.Now;
                    }
                }

                if (!_stopping.WaitOne(0))
                {
                    lock (SyncRoot)
                    {
                        _udpResult = c.BeginReceive(DataReceived, ar.AsyncState);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    internal class UdpState
    {
        internal UdpClient C;
        internal IPEndPoint E;

        internal UdpState(UdpClient c, IPEndPoint e)
        {
            this.C = c;
            this.E = e;
        }
    }
}