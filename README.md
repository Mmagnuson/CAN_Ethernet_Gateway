# J1939/CAN Ethernet Gateway


CAN-Ethernet Gateway is a C# library that allows for easy communication with a CAN Bus over IP networks. 
 The libary enables bi-directional communications to a PCAN-Ethernet Gateway DR hardware unit sold by Peak-Systems.
  (See: https://www.peak-system.com/PCAN-Ethernet-Gateway-DR.330.0.html?&L=1) 
  Peak's hardware allows for CAN frames are wrapped in TCP or UDP message packets. 

The library also makes it easy to receive and send standard J1939 messages. 


<b>Currently Supported Features</b>
- UDP Protocol
- J1939 and NMEA 2000 Phrasing
- .Net Core

<b>Requirements / Issues</b>
- You must disable the PCAN-Gateway Handshake to allow the connection (Handshake currently not supported).
- TCP protocol currently not supported



         CanGateway canGatewayConnector = new CanGateway();


         var messages = new List<CanMessageDefinition>();
 
         CanMessageDefinition joyStick = new CanMessageDefinition
            {
               IdHex = "18FDD7AA", MessageName = "GPS Position"
            }; 
 

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
            
