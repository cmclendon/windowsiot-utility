using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BitUnify.WindowsIoT
{
    class WindowsIoTDeviceDiscovery
    {
        #region Events
        public delegate void DeviceDiscovered(object sender, WindowsIoTDevice device);
        public event DeviceDiscovered DeviceDiscoveredEvent = null;

        public delegate void DeviceRemoved(object sender, WindowsIoTDevice device);
        public event DeviceRemoved DeviceRemovedEvent = null;

        public delegate void DeviceChanged(object sender, WindowsIoTDevice device);
        public event DeviceChanged DeviceChangedEvent = null;
        #endregion

        private UdpClient client;
        private IPEndPoint localEP;

        private Task deviceDiscoverTask;
        private Timer deviceCheckTimer;

        private Dictionary<string, WindowsIoTDevice> devices;

        public WindowsIoTDeviceDiscovery()
        {
            deviceDiscoverTask = null;
            deviceCheckTimer = null;

            devices = new Dictionary<string, WindowsIoTDevice>();
        }

        public Task Listen()
        {
            if (deviceDiscoverTask != null)
            {
                throw new SystemException("Discovery background task already running");
            }
            else
            {
                // create a worker task to listen for IoT advertisements
                deviceDiscoverTask = new Task(() => ListenForMulticastAdvertisements());
            }

            lock (deviceDiscoverTask)
            {
                // initialize our udp client
                if (client == null)
                {
                    client = new UdpClient();
                    client.ExclusiveAddressUse = false;

                    // Windows 10 Core IoT devices advertise on UDP port 6
                    localEP = new IPEndPoint(IPAddress.Any, 6);

                    // Set socket options
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    client.Client.Bind(localEP);

                    // Specify the multicast address that Windows 10 Core IoT devices are advertising on
                    // and join the multicast group
                    IPAddress mcastAddress = IPAddress.Parse("239.0.0.222");
                    client.JoinMulticastGroup(mcastAddress);

                    // start our background listening task
                    deviceDiscoverTask.Start();

                    // start our check timer
                    deviceCheckTimer = new Timer(CheckForOfflineDevices, this, 0, 1000);
                }
            }

            return deviceDiscoverTask;
        }

        private static void CheckForOfflineDevices(object state)
        {
            WindowsIoTDeviceDiscovery iot = (WindowsIoTDeviceDiscovery)state;

            lock (iot.devices)
            {
                List<string> removalKeys = new List<string>();

                foreach (WindowsIoTDevice device in iot.devices.Values)
                {
                    if (device.GetTimeSinceLastAdvertisementInSeconds() > 30)
                        removalKeys.Add(device.MACAddress);
                }

                foreach (string key in removalKeys)
                {
                    if (iot.DeviceRemovedEvent != null)
                    {
                        // remove device after 30 seconds of no activity
                        WindowsIoTDevice device = iot.devices[key];
                        if (iot.DeviceRemovedEvent != null) iot.DeviceRemovedEvent(iot, device);
                    }

                    // remove the key from the collection
                    iot.devices.Remove(key);
                }
            }
        }

        private void ListenForMulticastAdvertisements()
        {
            while (deviceDiscoverTask.Status == TaskStatus.Running)
            {
                byte[] data = client.Receive(ref localEP);

                string message = Encoding.Unicode.GetString(data);
                WindowsIoTDevice device = WindowsIoTDevice.FromMessage(message);

                if (device != null)
                {
                    lock(devices)
                    {
                        // check if this device is already in our collection
                        if (devices.ContainsKey(device.MACAddress) == true)
                        {
                            // see if any properties have changed
                            if (device.Equals(devices[device.MACAddress]) == false)
                            {
                                // replace the device with the updated device object
                                devices[device.MACAddress] = device;
                                if (DeviceChangedEvent != null) DeviceChangedEvent(this, device);
                            }
                            else
                            {
                                // ping the current device
                                devices[device.MACAddress].Ping();
                            }
                        }
                        else
                        {
                            // add the device
                            devices.Add(device.MACAddress, device);
                            if (DeviceDiscoveredEvent != null) DeviceDiscoveredEvent(this, device);
                        }
                    }
                }
            }
        }
    }
}