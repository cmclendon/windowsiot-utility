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
using System.Text.RegularExpressions;

namespace BitUnify.Windows.Devices.Enumeration
{
    /// <summary>
    /// Windows 10 Core IoT Device 
    /// </summary>
    public class Device
    {
        private DateTime lastPing;

        private string name;
        private string macAddress;
        private string networkAddress;

        /// <summary>
        /// Device factory 
        /// </summary>
        /// <param name="multicastMessage">Device multicast discovery message</param>
        /// <returns></returns>
        public static Device Create(string multicastMessage)
        {
            Device discoveredDevice = null;

            // support limited special characters for computer name 
            string exp1 = "((?:[a-zA-Z0-9]|[_.\\-])+)";
            string exp2 = ".*?"; // Non-greedy match on filler
            string exp3 = "((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?![\\d])";   // IPv4 IP Address 1
            string exp4 = ".*?"; // Non-greedy match on filler
            string exp5 = "((?:[0-9A-F][0-9A-F]:){5}(?:[0-9A-F][0-9A-F]))(?![:0-9A-F])"; // Mac Address 1

            Regex matchExp = new Regex(exp1 + exp2 + exp3 + exp4 + exp5, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match fields = matchExp.Match(multicastMessage);

            if (fields.Success)
            {
                discoveredDevice = new Device();

                discoveredDevice.name = fields.Groups[1].ToString();
                discoveredDevice.networkAddress = fields.Groups[2].ToString();
                discoveredDevice.macAddress = fields.Groups[3].ToString();
                discoveredDevice.lastPing = DateTime.Now;
            }

            return discoveredDevice;
        }

        #region Properties
        /// <summary>
        /// NetBIOS name of the device
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// MAC address of the device
        /// </summary>
        public string MacAddress
        {
            get
            {
                return macAddress;
            }
        }

        /// <summary>
        /// IP address of device
        /// </summary>
        public string NetworkAddress
        {
            get
            {
                return networkAddress;

            }
        }

        /// <summary>
        /// Number of seconds since the device was last seen
        /// </summary>
        public UInt16 LastPing
        {
            get
            {
                TimeSpan span = DateTime.Now - lastPing;
                return (UInt16)span.TotalSeconds;
            }
        }
        #endregion

        /// <summary>
        /// Call this method each time the device broadcasts its presence on the network
        /// </summary>
        public void Ping()
        {
            lastPing = DateTime.Now;
        }

        #region Overrides
        /// <summary>
        /// Compares two Device objects based on properties
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            bool equal = false;

            if (obj is Device)
            {
                Device enumDevice = (Device)obj;
                if ((enumDevice.NetworkAddress == NetworkAddress) &&
                    (enumDevice.MacAddress == MacAddress) &&
                    (enumDevice.Name == Name))
                {
                    // objects match
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }
}
