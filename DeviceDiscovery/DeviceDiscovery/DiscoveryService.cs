/*

    Copyright (c) Christopher McLendon (www.bitunify.com).  All rights reserved.
 
The MIT License (MIT)
 
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
 
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/


using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BitUnify.Windows.Devices.Enumeration
{
    public class DiscoveryService : IDisposable
    {
        #region Events
        public event EventHandler<EnumerationEventArgs> DeviceDiscoveredEvent = null;
        public event EventHandler<EnumerationEventArgs> DeviceRemovedEvent = null;
        public event EventHandler<EnumerationEventArgs> DeviceUpdatedEvent = null;
        public event EventHandler<DiscoveryServiceErrorEventArgs> ConnectionExceptionEvent = null;
        #endregion

        private UdpClient client;
        private IPEndPoint localEP;

        private UInt16 maxPingInterval;

        private Task deviceDiscoverTask;

        private TaskFactory factory;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancelToken;

        private Timer deviceCheckTimer;

        private Dictionary<string, Device> devices;

        public DiscoveryService() : this(30)
        {

        }
        public DiscoveryService(UInt16 maxPingInterval)
        {
            this.maxPingInterval = maxPingInterval;

            deviceDiscoverTask = null;
            deviceCheckTimer = null;

            // device collection also used as our synchronization object
            devices = new Dictionary<string, Device>();            
        }

        public void Start()
        {
            lock (devices)
            {
                // verify the listening task is not already running
                if (deviceDiscoverTask != null) return;

                // create a background worker task to listen for IoT advertisements
                deviceDiscoverTask = new Task(() => ListenForMulticastAdvertisements());

                // initialize our udp client
                if (client == null)
                {
                    // initialize our task factory
                    tokenSource = new CancellationTokenSource();
                    cancelToken = tokenSource.Token;

                    factory = new TaskFactory(cancelToken);

                    // initialize our UDP client and listen on port 6
                    client = new UdpClient();
                    client.ExclusiveAddressUse = false;
                    localEP = new IPEndPoint(IPAddress.Any, 6);

                    // Set socket options
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    client.Client.Bind(localEP);

                    // Join the multicast group for listening to device broadcast
                    IPAddress mcastAddress = IPAddress.Parse("239.0.0.222");
                    client.JoinMulticastGroup(mcastAddress);

                    // start our background listening task and device check timer
                    deviceDiscoverTask = factory.StartNew(new Action(ListenForMulticastAdvertisements));
                    deviceCheckTimer = new Timer(UpdateDeviceAvailability, this, 0, 5000);
                }
            }
        }

        public void Stop()
        {
            lock (devices)
            {
                if (deviceDiscoverTask != null)
                {
                    // start the process of shutting down backgroud task
                    tokenSource.Cancel();

                    // wait for the discovery task to exit and dispose of our timer
                    deviceDiscoverTask.Wait();
                    deviceDiscoverTask = null;

                    deviceCheckTimer.Dispose();
                    deviceCheckTimer = null;

                    // close our client UDP connection
                    client.Close();
                    client = null;

                    // dispose of our task factory
                    factory = null;

                    // clear the device collection
                    devices.Clear();
                }
            }
        }

        private static void UpdateDeviceAvailability(object state)
        {
            DiscoveryService iot = (DiscoveryService)state;

            lock (iot.devices)
            {
                if (iot.cancelToken.IsCancellationRequested == false)
                {
                    List<string> removalKeys = new List<string>();

                    // build a list of devices that need to be removed
                    foreach (Device device in iot.devices.Values)
                    {
                        if (device.LastPing > iot.maxPingInterval)
                            removalKeys.Add(device.MacAddress);
                    }

                    // remove devices that have exceeded their maximum ping interval
                    foreach (string key in removalKeys)
                    {
                        if (iot.DeviceRemovedEvent != null)
                        {
                            // notify subscribers of device removal
                            Device device = iot.devices[key];

                            if (iot.DeviceRemovedEvent != null)
                                iot.DeviceRemovedEvent(iot, new EnumerationEventArgs(device));
                        }

                        // remove the item from the device collection
                        iot.devices.Remove(key);
                    }
                }
            }
        }

        private void ListenForMulticastAdvertisements()
        {
            while (cancelToken.IsCancellationRequested==false)
            {
                // listen for a device advertisement and wait until data is received or our task is cancelled
                Task<UdpReceiveResult> receiveTask = client.ReceiveAsync();

                try
                {
                    // wait for data and exit if we are cancelled
                    receiveTask.Wait(cancelToken);
                }
                catch(OperationCanceledException)
                {
                    // exit
                    break;
                }
                
                if (receiveTask.IsCompleted == true)
                {
                    string message = Encoding.Unicode.GetString( receiveTask.Result.Buffer );
                    Device device = Device.Create(message);

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
                                    if (DeviceUpdatedEvent != null) DeviceUpdatedEvent(this, new EnumerationEventArgs(device));
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
                                if (DeviceDiscoveredEvent != null) DeviceDiscoveredEvent(this, new EnumerationEventArgs(device));
                            }
                        }
                    }
                }
                else
                {
                    // check if we faulted
                    if (receiveTask.IsFaulted == true)
                    {
                        // terminate the connection
                        tokenSource.Cancel();
                        if (ConnectionExceptionEvent != null) ConnectionExceptionEvent(this, new DiscoveryServiceErrorEventArgs(receiveTask.Exception));
                        break;
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

                        tokenSource.Dispose();
                        tokenSource = null;

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