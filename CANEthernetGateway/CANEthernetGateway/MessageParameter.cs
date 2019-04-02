using System;
using System.Collections.Generic;
using System.Text;

namespace CANEthernetGateway
{
    public class MessageParameter
    {
        public double Factor;
        public int OffSet;
        public int Start;
        public int Length;
        public Type Type;
        public object Value;
        public string Name;
        public double Min;
        public double Max;
        public string Unit;
        public int MultiplexID;
        public bool IsMultiplexID;

        public MessageParameter(double factor, int offSet, int start, int length, double min, double max, string unit, Type type, string name, int multiplexID = -1, bool isMultiplexId = false)
        {
            Factor = factor;
            OffSet = offSet;
            Start = start;
            Length = length;
            Type = type;
            Name = name;
            Unit = unit;
            Max = max;
            MultiplexID = multiplexID;
            IsMultiplexID = isMultiplexId;

            if (type.IsValueType)
            {
                Value = Activator.CreateInstance(type);
            }
            if (Type == typeof(string))
            {
                Value = string.Empty;
            }
        }

        public void SetValue(byte[] data)
        {
            if (Type == typeof(string))
            {
                Value = MessageParsing.CanDataParseString(data, Factor, OffSet, Start, Length);
            }
            if (Type == typeof(double))
            {
                Value = MessageParsing.CanDataParseDouble(data, Factor, OffSet, Start, Length);
            }
        }
    }
}
