using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BitUnify.WindowsIoT
{
    public class WindowsIoTDevice
    {
        private DateTime lastSeen;
        private string name;
        private string macAddress;
        private string ipAddress;

        public static WindowsIoTDevice FromMessage(string message)
        {
            WindowsIoTDevice device = null;

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
                device = new WindowsIoTDevice();

                device.name = fields.Groups[1].ToString();
                device.ipAddress = fields.Groups[2].ToString();
                device.macAddress = fields.Groups[3].ToString();
                device.lastSeen = DateTime.Now;
            }

            return device;
        }

        public DateTime LastSeen
        {
            get
            {
                return lastSeen;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string MACAddress
        {
            get
            {
                return macAddress;
            }
        }

        public string IPAddress
        {
            get
            {
                return ipAddress;

            }
        }

        public void Ping()
        {
            lastSeen = DateTime.Now;
        }

        public UInt32 GetTimeSinceLastAdvertisementInSeconds()
        {
            TimeSpan span = DateTime.Now - lastSeen;
            return (UInt32)span.TotalSeconds;
        }

        public override bool Equals(object obj)
        {
            bool equal = false;

            if (obj is WindowsIoTDevice)
            {
                WindowsIoTDevice device = (WindowsIoTDevice)obj;
                if ((device.IPAddress == IPAddress) &&
                    (device.MACAddress == MACAddress) &&
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
