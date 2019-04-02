using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CANEthernetGateway
{
    public class IPCANMsg
    {
        public ushort Length { get { return (ushort)(CANData.Length + 28); } } // 2 Bytes Fixed at 0x24 This corresponds to the 36 and indicates total length of of the data packet including this field in bytes.
        public ushort MessageType = 0x80;   // 2 Bytes Fixed value 0x80 This value represents the CAN data frame.
        public Int64 Tag = 0x00;            // 8 Bytes Not used in the current version.
        public Int32 TimeStampLow;          // 4 Bytes
        public Int32 TimeStampHigh;         // 4 Bytes
        public Byte Channel = 0x00;         // 1 Byte Chan Chanel (Not used)
        public Byte DLC { get { return (byte)CANData.Length; } }  // 1 Byte The Data Lenth Count gives the length of the CAN data in bytes;
        public ushort Flags = 0x00;         // 2 Bytes Not used in the current version.
        public Int32 MessageID;                 // 4 Bytes See notes on building CAN ID
        public Byte[] CANData;              // 8x8 data array;
        public TCANTimestamp TIME;

        private byte LEN;

        public string MessageIDHex
        {
            get { return String.Format("0x{0:X}", MessageID); }

            set { MessageID = int.Parse(value, System.Globalization.NumberStyles.HexNumber); }
        }

        public byte[] EncodeCANPacket()
        {
            var packet = new byte[Length];

            packet[0] = (byte)(((Length) & 0xff00) >> 8);
            packet[1] = (byte)(((Length) & 0xff));
            packet[2] = (byte)((MessageType & 0xff00) >> 8);
            packet[3] = (byte)((MessageType & 0xff));
            packet[20] = (byte)(1 & 0xff); // chanel
            packet[21] = (byte)DLC;
            packet[23] = (byte)(6 & 0xff); // MSG Type
            packet[24] = (byte)(((MessageID & 0xff000000) >> 24)); // 128
            packet[25] = (byte)((MessageID & 0xff0000) >> 16);
            packet[26] = (byte)((MessageID & 0xff00) >> 8);
            packet[27] = (byte)((MessageID & 0xff));

            Array.Copy(CANData, 0, packet, 28, DLC);

            return packet;
        }

        public void DecodeCANPacket(Byte[] packet)
        {
            try
            {
                if (packet != null)
                {
                    MessageID = (packet[24] & 0x1f) << 24 | (packet[25] & 0xff) << 16 | (packet[26] & 0xff) << 8 | (packet[27] & 0xff);
                    //MSGTYPE = ((byte)(packet[24] >> 6));
                    LEN = packet[21];
                    if (LEN > 0)
                    {
                        CANData = new byte[LEN];
                        Array.Copy(packet, 28, CANData, 0, LEN);
                    }
                    long timel = ((packet[12] & 0xff) << 24 | (packet[13] & 0xff) << 16 | (packet[14] & 0xff) << 8 | (packet[15] & 0xff));
                    long timeh = ((packet[16] & 0xff) << 24 | (packet[17] & 0xff) << 16 | (packet[18] & 0xff) << 8 | (packet[19] & 0xff));
                    long time = ((timeh & 0xffffffff) << 32);
                    time |= (timel & 0xffffffffL);
                    long millies = time / 1000;
                    TIME = new TCANTimestamp();
                    TIME.micros = ((ushort)(time - millies * 1000));
                    TIME.millis = ((uint)(millies & 0xffffffff));
                    TIME.millis_overflow = ((ushort)(millies >> 32));
                }
            }
            catch { }
        }
    }

    public struct TCANTimestamp
    {
        public uint millis;
        public ushort millis_overflow;
        public ushort micros;
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
                int myoffset = Convert.ToInt16(step2 * 8);
                number = ((data[start / 8] >> myoffset) & (int)(Math.Pow(length, 2.0) - 1));
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
            string retString = string.Empty;
            for (int i = 0; (length / 8) > i; i++)
            {
                char ch = (char)data[(start / 8) + i];
                retString = retString + ch.ToString();
            }
            return retString;
        }

        public static string CanDataParseEnum(byte[] data, double factor, int offSet, int start, int length, List<KeyValuePair<int, string>> enumValue)
        {
            int id = Convert.ToInt16(CanDataParseDouble(data, factor, offSet, start, length));
            var result = enumValue.Where(x => x.Key == id).FirstOrDefault().Value;
            return result;
        }
    }

}
