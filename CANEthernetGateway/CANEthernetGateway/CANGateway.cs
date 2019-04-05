using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CANEthernetGateway
{
    public class CANGateway
    {
        public object SyncRoot;

        private static Thread _thread;

        private int InboundPort;
        private IPAddress RemoteAddress;
        private int RemotePort;

        private Dictionary<int, CanMessageDefinition> messageDict;
        private ManualResetEvent stopping = new ManualResetEvent(false);
        private ManualResetEvent dataReceived = new ManualResetEvent(false);
        private IAsyncResult udpResult;
        private UdpClient UDPCANServer;
        private UdpClient UDPCANClient;
        private IPEndPoint remoteClient;
        private IPEndPoint remoteSender;
        private DateTime lastPacketTime;

        public IList<CanMessageDefinition> Messages { get; private set; }

        public CANGateway()
        {
            SyncRoot = new object();
        }

        public bool Connected
        {
            get
            {
                if ((DateTime.Now - lastPacketTime).TotalMilliseconds <= 500)
                {
                    return true;
                }
                return false;
            }
        }

        public void CanBusDefLoad(IList<CanMessageDefinition> messages)
        {
            this.Messages = messages;
            this.messageDict = Messages.ToDictionary(x => x.MessageID);
        }

        public void Configuration(int inboundPort, IPAddress remoteAddress, int remotePort)

        {
            InboundPort = inboundPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
        }

        public void StartProcess()
        {
            lock (SyncRoot)
            {
                _thread = new System.Threading.Thread(ThreadWorker);
                _thread.IsBackground = true;
                _thread.Name = "CAN Bus UDP Thread ";
                _thread.Start();
            }
        }

        public void StopProcess()
        {
            try
            {
                this.stopping.Set();
                UDPCANServer.Client.Shutdown(SocketShutdown.Receive);
                UDPCANServer.Client.Close();
            }
            catch (Exception ex)
            {

            }
        }

        private void ThreadWorker()
        {
        }

        public void Connect()
        {
            remoteClient = new IPEndPoint(RemoteAddress, RemotePort);
            UDPCANClient = new UdpClient();
            UDPCANClient.Connect(remoteClient);
            lock (SyncRoot)
            {
                remoteSender = new IPEndPoint(IPAddress.Any, 0);
                UDPCANServer = new UdpClient(InboundPort);
                UdpState state = new UdpState(UDPCANServer, remoteSender);
                udpResult = UDPCANServer.BeginReceive(new AsyncCallback(DataReceived), state);
            }
        }

        public void Disconnect()
        {
            UDPCANClient.Close();
            try
            {
                this.stopping.Set();
                UDPCANServer.Client.Shutdown(SocketShutdown.Receive);
                UDPCANServer.Client.Close();
            }
            catch (Exception ex)
            {
                int error = 1;
            }
        }

        public void DataSend(byte[] msg)
        {
            UDPCANClient.Send(msg, msg.Length);
        }

        private void DataReceived(IAsyncResult ar)
        {
            try
            {
                UdpClient c = (UdpClient)((UdpState)ar.AsyncState).c;
                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);

                IPCANMsg CANFrame = new IPCANMsg();

                CANFrame.DecodeCANPacket(receiveBytes);

                if (messageDict.ContainsKey(CANFrame.MessageID))
                {
                    if (messageDict[CANFrame.MessageID].IsMulticast == false)
                    {
                        messageDict[CANFrame.MessageID].DecodeMessage(CANFrame);
                        lastPacketTime = DateTime.Now;
                    }
                    else
                    {
                        messageDict[CANFrame.MessageID].DecodeMessageMultiplex(CANFrame);
                        lastPacketTime = DateTime.Now;
                    }
                }

                if (!stopping.WaitOne(0))
                {
                    lock (SyncRoot)
                    {
                        udpResult = c.BeginReceive(new AsyncCallback(DataReceived), ar.AsyncState);
                    }
                }
            }
            catch { }
        }
    }

    internal class UdpState
    {
        internal UdpClient c;
        internal IPEndPoint e;

        internal UdpState(UdpClient c, IPEndPoint e)
        {
            this.c = c;
            this.e = e;
        }
    }
}