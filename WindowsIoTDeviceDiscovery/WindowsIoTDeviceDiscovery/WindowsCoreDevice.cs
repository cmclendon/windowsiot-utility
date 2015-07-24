using System;
using System.Text.RegularExpressions;

namespace BitUnify.Windows.Devices
{
    public class WindowsCoreDevice
    {
        private DateTime lastSeen;
        private string name;
        private string macAddress;
        private string networkAddress;

        public static WindowsCoreDevice FromMessage(string message)
        {
            WindowsCoreDevice device = null;

            // support limited special characters for computer name 
            string exp1 = "((?:[a-zA-Z0-9]|[_\\-])+)";   
            string exp2 = ".*?"; // Non-greedy match on filler
            string exp3 = "((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?![\\d])";   // IPv4 IP Address 1
            string exp4 = ".*?"; // Non-greedy match on filler
            string exp5 = "((?:[0-9A-F][0-9A-F]:){5}(?:[0-9A-F][0-9A-F]))(?![:0-9A-F])"; // Mac Address 1

            Regex matchExp = new Regex(exp1 + exp2 + exp3 + exp4 + exp5, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match fields = matchExp.Match(message);

            if (fields.Success)
            {
                device = new WindowsCoreDevice();

                device.name = fields.Groups[1].ToString();
                device.networkAddress = fields.Groups[2].ToString();
                device.macAddress = fields.Groups[3].ToString();
                device.lastSeen = DateTime.Now;
            }

            return device;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string MacAddress
        {
            get
            {
                return macAddress;
            }
        }

        public string NetworkAddress
        {
            get
            {
                return networkAddress;

            }
        }

        public void Ping()
        {
            lastSeen = DateTime.Now;
        }

        /// <summary>
        /// Number of seconds since the device was last seen
        /// </summary>
        public UInt16 LastPing
        {
            get
            {
                TimeSpan span = DateTime.Now - lastSeen;
                return (UInt16)span.TotalSeconds;
            }
        }

        public override bool Equals(object obj)
        {
            bool equal = false;

            if (obj is WindowsCoreDevice)
            {
                WindowsCoreDevice device = (WindowsCoreDevice)obj;
                if ((device.NetworkAddress == NetworkAddress) &&
                    (device.MacAddress == MacAddress) &&
                    (device.Name == Name))
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
    }
}
