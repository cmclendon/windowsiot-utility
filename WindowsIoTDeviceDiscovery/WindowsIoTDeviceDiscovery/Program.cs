using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace BitUnify.WindowsIoT.Discovery
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowsIoTDeviceDiscovery iot = new WindowsIoTDeviceDiscovery();

            iot.DeviceChangedEvent += Iot_DeviceChangedEvent;
            iot.DeviceDiscoveredEvent += Iot_DeviceDiscoveredEvent;
            iot.DeviceRemovedEvent += Iot_DeviceRemovedEvent;

            Task waitTask = iot.Listen();
            waitTask.Wait();    
        }

        private static void Iot_DeviceRemovedEvent(object sender, WindowsIoTDevice device)
        {
            Console.WriteLine("Removed device {0} with IP address {1}", device.Name, device.IPAddress);
        }

        private static void Iot_DeviceDiscoveredEvent(object sender, WindowsIoTDevice device)
        {
            Console.WriteLine("Added device {0} with IP address {1}", device.Name, device.IPAddress);
        }

        private static void Iot_DeviceChangedEvent(object sender, WindowsIoTDevice device)
        {
            Console.WriteLine("Changed device {0} with IP address {1}", device.Name, device.IPAddress);
        }
    }
}
