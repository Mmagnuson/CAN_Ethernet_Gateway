using System;
using System.Collections.Generic;
using System.Linq;

namespace CANEthernetGateway
{
    public class IpcanMsg
    {
        public ushort Length => (ushort)(CanData.Length + 28); // 2 Bytes Fixed at 0x24 This corresponds to the 36 and indicates total length of of the data packet including this field in bytes.
        public ushort MessageType = 0x80;   // 2 Bytes Fixed value 0x80 This value represents the CAN data frame.
        public Int64 Tag = 0x00;            // 8 Bytes Not used in the current version.
        public Int32 TimeStampLow;          // 4 Bytes
        public Int32 TimeStampHigh;         // 4 Bytes
        public Byte Channel = 0x00;         // 1 Byte Chan Chanel (Not used)
        public byte Dlc => (byte)CanData.Length; // 1 Byte The Data Lenth Count gives the length of the CAN data in bytes;
        public ushort Flags = 0x00;         // 2 Bytes Not used in the current version.
        public Int32 MessageId;                 // 4 Bytes See notes on building CAN ID
        public Byte[] CanData;              // 8x8 data array;
        public TcanTimestamp Time;

        private byte _len;

        public string MessageIdHex
        {
            get => $"0x{MessageId:X}";

            set => MessageId = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
        }

        public byte[] EncodeCanPacket()
        {
            var packet = new byte[Length];

            packet[0] = (byte)(((Length) & 0xff00) >> 8);
            packet[1] = (byte)(((Length) & 0xff));
            packet[2] = (byte)((MessageType & 0xff00) >> 8);
            packet[3] = (byte)((MessageType & 0xff));
            packet[20] = 1 & 0xff; // Channel
            packet[21] = Dlc;
            packet[23] = 6 & 0xff; // MSG Type
            packet[24] = (byte)(((MessageId & 0xff000000) >> 24)); // 128
            packet[25] = (byte)((MessageId & 0xff0000) >> 16);
            packet[26] = (byte)((MessageId & 0xff00) >> 8);
            packet[27] = (byte)((MessageId & 0xff));

            Array.Copy(CanData, 0, packet, 28, Dlc);

            return packet;
        }

        public void DecodeCanPacket(Byte[] packet)
        {
            try
            {
                if (packet != null)
                {
                    MessageId = (packet[24] & 0x1f) << 24 | (packet[25] & 0xff) << 16 | (packet[26] & 0xff) << 8 | (packet[27] & 0xff);
                    _len = packet[21];
                    if (_len > 0)
                    {
                        CanData = new byte[_len];
                        Array.Copy(packet, 28, CanData, 0, _len);
                    }
                    long timel = ((packet[12] & 0xff) << 24 | (packet[13] & 0xff) << 16 | (packet[14] & 0xff) << 8 | (packet[15] & 0xff));
                    long timeh = ((packet[16] & 0xff) << 24 | (packet[17] & 0xff) << 16 | (packet[18] & 0xff) << 8 | (packet[19] & 0xff));
                    long time = ((timeh & 0xffffffff) << 32);
                    time |= (timel & 0xffffffffL);
                    long millies = time / 1000;
                    Time = new TcanTimestamp
                    {
                        Micros = ((ushort) (time - millies * 1000)),
                        Millis = ((uint) (millies & 0xffffffff)),
                        MillisOverflow = ((ushort) (millies >> 32))
                    };
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    public struct TcanTimestamp
    {
        public uint Millis;
        public ushort MillisOverflow;
        public ushort Micros;
    }

    public static class MessageParsing
    {
        public static double CanDataParseDouble(byte[] data, double factor, int offSet, int start, int length)
        {
            int number = 0;

            if (length == 1)
            {
                int startValue = start / 8;
                int offsetValue = start - (startValue * 8);
                number = (data[startValue] >> offsetValue) & 0x1;
                return (Convert.ToDouble(number));
            }

            if (length <= 8)
            {
                var step1 = Decimal.Divide(start, 8);
                var step2 = step1 % 1;
                int myOffset = Convert.ToInt16(step2 * 8);
                number = ((data[start / 8] >> myOffset) & (int)(Math.Pow(length, 2.0) - 1));
            }
            if (length == 8)
            {
                number = data[(start / 8)];
            }
            if (length == 16)
            {
                number = BitConverter.ToUInt16(data, (start / 8));
            }
            if (length == 32)
            {
                number = BitConverter.ToInt32(data, (start / 8));
            }

            return (Convert.ToDouble(number) * factor) + offSet;
        }

        public static string CanDataParseString(byte[] data, double factor, int offSet, int start, int length)
        {
            var retString = string.Empty;
            for (var i = 0; (length / 8) > i; i++)
            {
                var ch = (char)data[(start / 8) + i];
                retString += ch.ToString();
            }
            return retString;
        }

        public static string CanDataParseEnum(byte[] data, double factor, int offSet, int start, int length, List<KeyValuePair<int, string>> enumValue)
        {
            int id = Convert.ToInt16(CanDataParseDouble(data, factor, offSet, start, length));
            var result = enumValue.FirstOrDefault(x => x.Key == id).Value;
            return result;
        }
    }

}
