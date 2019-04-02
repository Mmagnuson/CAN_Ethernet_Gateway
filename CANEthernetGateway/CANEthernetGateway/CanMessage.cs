using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CANEthernetGateway
{
    public class CanMessageDefinition
    {
        public int MessageID;
        public int Length;

        public string MessageIDHex
        {
            get { return String.Format("0x{0:X}", MessageID); }

            set { MessageID = int.Parse(value, System.Globalization.NumberStyles.HexNumber); }
        }

        public bool IsMulticast;

        public string MessageName;
        public List<MessageParameter> Parameters;
        public TimeSpan Age { get { return DateTime.Now.Subtract(lastReadTime); } }
        public DateTime LastRead { get { return lastReadTime; } }
        private DateTime lastReadTime;

        public CanMessageDefinition()
        {
            this.Parameters = new List<MessageParameter>();
            lastReadTime = DateTime.MinValue;
            IsMulticast = false;
        }

        public void AddParameter(MessageParameter paramemter)
        {
            lock (this.Parameters)
            {
                this.Parameters.Add(paramemter);
            }
        }

        public Dictionary<string, object> GetValues()
        {
            lock (this.Parameters)
            {
                return this.Parameters.ToDictionary(x => x.Name, x => x.Value);
            }
        }

        public IPCANMsg GenerateIPCANMsg()
        {
            IPCANMsg CANMsg = new IPCANMsg();
            CANMsg.MessageID = MessageID;
            CANMsg.CANData = new byte[Length];
            int[] tmpBitAsIntArray = new int[Length * 8];

            foreach (MessageParameter messageParameter in Parameters)
            {
                if (messageParameter.Type == typeof(double))
                {
                    double intValue = (Convert.ToDouble(messageParameter.Value) - Convert.ToDouble(messageParameter.OffSet));
                    if (messageParameter.Factor != 0) // Can't divide by zero
                    {
                        intValue = intValue / messageParameter.Factor;
                    }

                    int[] bits = Convert.ToString(Convert.ToInt32(intValue), 2)
                                .PadLeft(messageParameter.Length, '0') // Add 0's from left
                                .Select(c => int.Parse(c.ToString())) // convert each char to int
                                .ToArray();

                    Array.Reverse(bits);
                    Array.Copy(bits, 0, tmpBitAsIntArray, messageParameter.Start, messageParameter.Length);
                }
            }

            BitArray tmpBitArray = new BitArray(new bool[tmpBitAsIntArray.Length]);
            tmpBitArray.SetAll(false);

            for (int i = 0; i < tmpBitAsIntArray.Length; i++)
            {
                if (tmpBitAsIntArray[i] == 1)
                {
                    tmpBitArray.Set(i, true);
                }
                else
                {
                    tmpBitArray.Set(i, false);
                }
            }

            CANMsg.CANData = BitArrayToByteArray(tmpBitArray);

            return CANMsg;
        }

        public void DecodeMessage(IPCANMsg message)
        {
            if (message.MessageID != MessageID)
            {
                return;
            }
            lock (this.Parameters)
            {
                lastReadTime = DateTime.Now;
                foreach (MessageParameter x in Parameters)
                {
                    x.SetValue(message.CANData);
                }
            }
        }

        public void DecodeMessageMultiplex(IPCANMsg message)
        {
            if (message.MessageID != MessageID)
            {
                return;
            }
            lock (this.Parameters)
            {
                lastReadTime = DateTime.Now;
                var muliplexIdMessage = this.Parameters.Where(x => x.IsMultiplexID).First();

                muliplexIdMessage.SetValue(message.CANData);
                int multiplexId = Convert.ToInt16((double)muliplexIdMessage.Value);

                foreach (MessageParameter x in Parameters.Where(x => x.MultiplexID == multiplexId))
                {
                    x.SetValue(message.CANData);
                }
            }
        }

        private byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }
    }

}
