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

namespace BitUnify.Windows.Devices.Enumeration
{
    /// <summary>
    /// Sample Program to demonstrate the use of the DiscoveryService and Device classes
    /// </summary>
    class Program
    {
        static private DiscoveryService service;

        static void Main()
        {
            ConsoleKeyInfo key;
            service = new DiscoveryService();

            // register for events
            service.DeviceUpdatedEvent += Iot_DeviceUpdatedEvent;
            service.DeviceDiscoveredEvent += Iot_DeviceDiscoveredEvent;
            service.DeviceRemovedEvent += Iot_DeviceRemovedEvent;
            service.ConnectionExceptionEvent += DiscoveryService_ConnectionExceptionEvent;

            // start the discovery service
            service.Start();

            // if the user press 'c' start cleaning up and exit
            Console.WriteLine("Press 'c' to cancel listening");

            do
            {
                key = Console.ReadKey();
            } while (key.KeyChar != 'c');
        }

        private static void Cleanup()
        {
            // stop the service and cleanup
            service.Stop();
            service.Dispose();
            service = null;
        }

        private static void DiscoveryService_ConnectionExceptionEvent(object sender, DiscoveryServiceErrorEventArgs args)
        {
            Console.WriteLine("[EXCEPTION]: CONNECTION EXCEPTION MESSAGE '{0}'", args.ServiceException.Message);
            Console.WriteLine("\tCLEANING UP AND CLOSING CONNECTION...");

            Cleanup();

            Console.WriteLine("\tDONE.");
        }

        private static void Iot_DeviceRemovedEvent(object sender, EnumerationEventArgs args)
        {
            Console.WriteLine("[REMOVED] NAME: {0} IPADDRESS: {1} MAC: {2}", 
                args.Device.Name, args.Device.NetworkAddress, args.Device.MacAddress);
        }

        private static void Iot_DeviceDiscoveredEvent(object sender, EnumerationEventArgs args)
        {
            Console.WriteLine("[DISCOVERED] NAME: {0} IPADDRESS: {1} MAC: {2}",
                args.Device.Name, args.Device.NetworkAddress, args.Device.MacAddress);
        }

        private static void Iot_DeviceUpdatedEvent(object sender, EnumerationEventArgs args)
        {
            Console.WriteLine("[UPDATED] NAME: {0} IPADDRESS: {1} MAC: {2}",
                args.Device.Name, args.Device.NetworkAddress, args.Device.MacAddress);
        }
    }
}
