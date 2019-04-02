using System;
using CANEthernetGateway;


namespace ExampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
           
            CANGateway Can1Process = new CANGateway();







            Can1Process.CanBusDefLoad(CANMessageDefinitions());

       

            Can1Process.StartProcess(LocalPort, System.Net.IPAddress.Parse(RemoteIP), RemotePort);
            Can1Process.Connect();




            Console.WriteLine("Hello World!");


        }
    }
}
