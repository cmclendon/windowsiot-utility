using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BitUnify.Windows.Devices
{
    public class WindowsCoreDeviceDiscoveryService : IDisposable
    {
        #region Events
        public event EventHandler<WindowsCoreDeviceEventArgs> DeviceDiscoveredEvent = null;
        public event EventHandler<WindowsCoreDeviceEventArgs> DeviceRemovedEvent = null;
        public event EventHandler<WindowsCoreDeviceEventArgs> DeviceChangedEvent = null;
        #endregion

        private UdpClient client;
        private IPEndPoint localEP;

        private Task deviceDiscoverTask;
        private Timer deviceCheckTimer;

        private Dictionary<string, WindowsCoreDevice> devices;

        public WindowsCoreDeviceDiscoveryService()
        {
            deviceDiscoverTask = null;
            deviceCheckTimer = null;

            devices = new Dictionary<string, WindowsCoreDevice>();
        }

        public Task Listen()
        {
            // check if we are already listening
            if (deviceDiscoverTask != null)
                return deviceDiscoverTask;

            // create a worker task to listen for IoT advertisements
            deviceDiscoverTask = new Task(() => ListenForMulticastAdvertisements());
            
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
            WindowsCoreDeviceDiscoveryService iot = (WindowsCoreDeviceDiscoveryService)state;

            lock (iot.devices)
            {
                List<string> removalKeys = new List<string>();

                foreach (WindowsCoreDevice device in iot.devices.Values)
                {
                    if (device.LastPing > 30)
                        removalKeys.Add(device.MacAddress);
                }

                foreach (string key in removalKeys)
                {
                    if (iot.DeviceRemovedEvent != null)
                    {
                        // remove device after 30 seconds of no activity
                        WindowsCoreDevice device = iot.devices[key];
                        if (iot.DeviceRemovedEvent != null) iot.DeviceRemovedEvent(iot, new WindowsCoreDeviceEventArgs(device));
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
                WindowsCoreDevice device = WindowsCoreDevice.FromMessage(message);

                if (device != null)
                {
                    lock (devices)
                    {
                        // check if this device is already in our collection
                        if (devices.ContainsKey(device.MacAddress) == true)
                        {
                            // see if any properties have changed
                            if (device.Equals(devices[device.MacAddress]) == false)
                            {
                                // replace the device with the updated device object
                                devices[device.MacAddress] = device;
                                if (DeviceChangedEvent != null) DeviceChangedEvent(this, new WindowsCoreDeviceEventArgs(device));
                            }
                            else
                            {
                                // ping the current device
                                devices[device.MacAddress].Ping();
                            }
                        }
                        else
                        {
                            // add the device
                            devices.Add(device.MacAddress, device);
                            if (DeviceDiscoveredEvent != null) DeviceDiscoveredEvent(this, new WindowsCoreDeviceEventArgs(device));
                        }
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (client != null)
                    {
                        deviceCheckTimer.Dispose();
                        deviceCheckTimer = null;

                        deviceDiscoverTask.Dispose();
                        deviceDiscoverTask = null;

                        client.Close();
                        client = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}