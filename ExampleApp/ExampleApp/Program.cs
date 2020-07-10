using CANEthernetGateway;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace ExampleApp
{
    internal class Program
    {
        private static void Main()
        {
            CanGateway canGatewayConnector = new CanGateway();

            var messages = new List<CanMessageDefinition>();

            // Example SAE J1939 Message definition 1
            CanMessageDefinition joyStick = new CanMessageDefinition
            {
                IdHex = "18FDD7AA", MessageName = "GPS Position"
            };
            joyStick.AddParameter(new MessageParameter(.0000001, 0, 0, 32, -2147483648, 2147483647, "Latitude", typeof(double), "Joystick1GripXAxis"));
            joyStick.AddParameter(new MessageParameter(.0000001, 0, 32, 32, -2147483648, 2147483647, "Longitude", typeof(double), "Joystick1GripYAxis"));
            messages.Add(joyStick);  // Add J1939 message definition to definition list

            // Example SAE J1939 Message definition 2
            var position = new CanMessageDefinition {IdHex = "09F80100", MessageName = "GPS Position"};
            position.AddParameter(new MessageParameter(.0000001, 0, 0, 32, -2147483648, 2147483647, "Latitude", typeof(double), "Latitude"));
            position.AddParameter(new MessageParameter(.0000001, 0, 32, 32, -2147483648, 2147483647, "Longitude", typeof(double), "Longitude"));
            messages.Add(position);

            canGatewayConnector.CanBusDefLoad(messages); // Load messages into CAN Gateway connector;

            int localPort = 2001;               // Port o open on local machine
            string remoteIp = "192.168.0.10";   // IP of remote CAN Gateway device
            int RemotePort = 5050;              // Port on remote CAN Gateway configured to the specific CAN Device

            canGatewayConnector.Configuration(localPort, IPAddress.Parse(remoteIp), RemotePort); // Load configuration into CAN Gateway Connector
            canGatewayConnector.StartProcess(); // Start the thread //

            canGatewayConnector.Connect();


            Console.WriteLine("Hit any key to read GPS position");
            Console.ReadKey();

            lock (canGatewayConnector.SyncRoot)
            {
                var canPosition = canGatewayConnector.Messages.First(x => x.IdHex == "0x9F80100");
                var latitude = (double)canPosition.GetValues()["Latitude"];
                var longitude = (double)canPosition.GetValues()["Longitude"];

                Console.WriteLine("Longitude: {0}  Longitude: {1}", longitude.ToString(CultureInfo.CurrentCulture), latitude.ToString(CultureInfo.CurrentCulture));
            }


            Console.WriteLine("Hit any key to send message");
            Console.ReadKey();

            var x18Fdd7Aa = canGatewayConnector.Messages.First(x => x.IdHex == "0x18FDD7AA");       // Pull the message definition
            x18Fdd7Aa.Parameters.First(x => x.Name == "Joystick1GripXAxis").Value = 59;
            x18Fdd7Aa.Parameters.First(x => x.Name == "Joystick1GripYAxis").Value = 50;

            canGatewayConnector.DataSend(x18Fdd7Aa.GenerateIpCanMsg().EncodeCanPacket()); // Encode and send message


            Console.WriteLine("Message sent.. ");

            Console.WriteLine("Hit any key to exit");
            Console.ReadKey();
        }
    }
}