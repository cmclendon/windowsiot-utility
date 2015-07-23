using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace IoTBoardDiscovery
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 6);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEp);

            IPAddress mcastAddress = IPAddress.Parse("239.0.0.222");
            client.JoinMulticastGroup(mcastAddress);
            
            while (true)
            {
                byte[] data = client.Receive(ref localEp);
                string strData = Encoding.Unicode.GetString(data);
                Console.WriteLine(strData);


                string[] list = trData.Split("\0", StringSplitOptions.RemoveEmptyEntries);

                string pattern = @"\w+(?=\0)\d*\.\d";
                foreach (Match match in Regex.Matches(strData, pattern, RegexOptions.IgnoreCase))
                    Console.WriteLine(match.Value);
                
            }          
        }
    }
}
