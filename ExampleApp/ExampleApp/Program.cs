using CANEthernetGateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ExampleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CANGateway CanGatewayConnector = new CANGateway();

            var messages = new List<CanMessageDefinition>();

            // Example SAE J1939 Message definition 1
            CanMessageDefinition JoyStick = new CanMessageDefinition();
            JoyStick.MessageIDHex = "18FDD7AA";
            JoyStick.MessageName = "GPS Position";
            JoyStick.AddParameter(new MessageParameter(.0000001, 0, 0, 32, -2147483648, 2147483647, "Latitude", typeof(double), "Joystick1GripXAxis"));
            JoyStick.AddParameter(new MessageParameter(.0000001, 0, 32, 32, -2147483648, 2147483647, "Longitude", typeof(double), "Joystick1GripYAxis"));
            messages.Add(JoyStick);  // Add J1939 message definition to definition list

            // Example SAE J1939 Message definition 2
            CanMessageDefinition Position = new CanMessageDefinition();
            Position.MessageIDHex = "09F80100";
            Position.MessageName = "GPS Position";
            Position.AddParameter(new MessageParameter(.0000001, 0, 0, 32, -2147483648, 2147483647, "Latitude", typeof(double), "Latitude"));
            Position.AddParameter(new MessageParameter(.0000001, 0, 32, 32, -2147483648, 2147483647, "Longitude", typeof(double), "Longitude"));
            messages.Add(Position);

            CanGatewayConnector.CanBusDefLoad(messages); // Load messages into CAN Gateway connector;

            int LocalPort = 2001;               // Port o open on local machine
            string RemoteIP = "192.168.0.10";   // IP of remote CAN Gateway device
            int RemotePort = 5050;              // Port on remote CAN Gateway configured to the specific CAN Device

            CanGatewayConnector.Configuration(LocalPort, IPAddress.Parse(RemoteIP), RemotePort); // Load configuration into CAN Gateway Connector
            CanGatewayConnector.StartProcess(); // Start the thread //

            CanGatewayConnector.Connect();



            /////////////////////////////////////////////////////////////
            /// Read from message from the CANGateway 
            /////////////////////////////////////////////////////////////
            Console.WriteLine("Hit any key to read GPS position");
            Console.ReadKey();

            lock (CanGatewayConnector.SyncRoot)
            {
                var CanPosition = CanGatewayConnector.Messages.Where(x => x.MessageIDHex == "0x9F80100").First();
                var Latitude = (double)CanPosition.GetValues()["Latitude"];
                var Longitude = (double)CanPosition.GetValues()["Longitude"];

                Console.WriteLine("Longitude: {0}  Longitude: {1}", Longitude.ToString(), Latitude.ToString());
            }


            /////////////////////////////////////////////////////////////
            /// Write from message from the CANGateway                ///
            /////////////////////////////////////////////////////////////  


            Console.WriteLine("Hit any key to send message");
            Console.ReadKey();

            var x18FDD7AA = CanGatewayConnector.Messages.Where(x => x.MessageIDHex == "0x18FDD7AA").First();       // Pull the message definition
            var Joystick1GripXAxis = x18FDD7AA.Parameters.Where(x => x.Name == "Joystick1GripXAxis").First().Value = 59;  // set the x value
            var Joystick1GripYAxis = x18FDD7AA.Parameters.Where(x => x.Name == "Joystick1GripYAxis").First().Value = 50; // set the y value

            CanGatewayConnector.DataSend(  x18FDD7AA.GenerateIPCANMsg().EncodeCANPacket() ); // Encode and send message


            Console.WriteLine("Message sent.. ");

            Console.WriteLine("Hit any key to exit");
            Console.ReadKey();
        }
    }
}