using System;
using System.Threading.Tasks;
using BitUnify.Windows.Devices;

namespace BitUnify
{
    class Program
    {
        static void Main()
        {
            WindowsCoreDeviceDiscoveryService iot = new WindowsCoreDeviceDiscoveryService();

            iot.DeviceChangedEvent += Iot_DeviceChangedEvent;
            iot.DeviceDiscoveredEvent += Iot_DeviceDiscoveredEvent;
            iot.DeviceRemovedEvent += Iot_DeviceRemovedEvent;

            Task waitTask = iot.Listen();
            waitTask.Wait();

            iot.Dispose();
        }

        private static void Iot_DeviceRemovedEvent(object sender, WindowsCoreDeviceEventArgs args)
        {
            Console.WriteLine("Removed device {0} with IP address {1}", args.Device.Name, args.Device.NetworkAddress);
        }

        private static void Iot_DeviceDiscoveredEvent(object sender, WindowsCoreDeviceEventArgs args)
        {
            Console.WriteLine("Added device {0} with IP address {1}", args.Device.Name, args.Device.NetworkAddress);
        }

        private static void Iot_DeviceChangedEvent(object sender, WindowsCoreDeviceEventArgs args)
        {
            Console.WriteLine("Changed device {0} with IP address {1}", args.Device.Name, args.Device.NetworkAddress);
        }
    }
}
