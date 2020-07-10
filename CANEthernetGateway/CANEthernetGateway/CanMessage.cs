using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CANEthernetGateway
{
    public class CanMessageDefinition
    {
        public int Id;
        public int Length;

        public string IdHex
        {
            get => String.Format("0x{0:X}", Id);

            set => Id = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
        }

        public bool IsMulticast;
        public string MessageName;
        public List<MessageParameter> Parameters;
        public TimeSpan Age => DateTime.Now.Subtract(_lastReadTime);
        public DateTime LastRead => _lastReadTime;


        private DateTime _lastReadTime;

        public CanMessageDefinition()
        {
            this.Parameters = new List<MessageParameter>();
            _lastReadTime = DateTime.MinValue;
            IsMulticast = false;
        }

        public void AddParameter(MessageParameter parameter)
        {
            lock (this.Parameters)
            {
                this.Parameters.Add(parameter);
            }
        }

        public Dictionary<string, object> GetValues()
        {
            lock (this.Parameters)
            {
                return this.Parameters.ToDictionary(x => x.Name, x => x.Value);
            }
        }

        public IpcanMsg GenerateIpCanMsg()
        {
            var canMsg = new IpcanMsg {MessageId = Id, CanData = new byte[Length]};
            var tmpBitAsIntArray = new int[Length * 8];

            foreach (var messageParameter in Parameters)
            {
                if (messageParameter.Type != typeof(double)) continue;

                double intValue = (Convert.ToDouble(messageParameter.Value) - Convert.ToDouble(messageParameter.OffSet));
                if (Math.Abs(messageParameter.Factor) > 0.0) // Can't divide by zero
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

            var tmpBitArray = new BitArray(new bool[tmpBitAsIntArray.Length]);
            tmpBitArray.SetAll(false);

            for (var i = 0; i < tmpBitAsIntArray.Length; i++)
            {
                tmpBitArray.Set(i, tmpBitAsIntArray[i] == 1);
            }

            canMsg.CanData = BitArrayToByteArray(tmpBitArray);

            return canMsg;
        }

        public void DecodeMessage(IpcanMsg message)
        {
            if (message.MessageId != Id)
            {
                return;
            }
            lock (this.Parameters)
            {
                _lastReadTime = DateTime.Now;
                foreach (MessageParameter x in Parameters)
                {
                    x.SetValue(message.CanData);
                }
            }
        }

        public void DecodeMessageMultiplex(IpcanMsg message)
        {
            if (message.MessageId != Id)
            {
                return;
            }
            lock (this.Parameters)
            {
                _lastReadTime = DateTime.Now;
                var multiplexIdMessage = Parameters.First(x => x.IsMultiplexId);

                multiplexIdMessage.SetValue(message.CanData);
                int multiplexId = Convert.ToInt16((double)multiplexIdMessage.Value);

                foreach (var x in Parameters.Where(x => x.MultiplexId == multiplexId))
                {
                    x.SetValue(message.CanData);
                }
            }
        }

        private static byte[] BitArrayToByteArray(BitArray bits)
        {
            var ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }
    }

}
