# CAN_Ethernet_Gateway


CAN-Ethernet Gateway is a C# library that allows for easy communication with a CAN Bus over IP networks. 
 The libary enables bi-directional communications to a PCAN-Ethernet Gateway DR hardware unit sold by Peak-Systems.
  (See: https://www.peak-system.com/PCAN-Ethernet-Gateway-DR.330.0.html?&L=1) 
  Peak's hardware allows for CAN frames are wrapped in TCP or UDP message packets. 

The library also makes it easy to receive and send standard J1939 messages. 


<b>Currently Supported Features</b>
- UDP Protocol
- J1939 Phrasing
- .Net Core

<b>Requirements / Issues</b>
- You must disable the PCAN-Gateway Handshake to allow the connection (Handshake currently not supported).
- TCP protocol currently not supported
