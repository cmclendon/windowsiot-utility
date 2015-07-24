using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitUnify.Windows.Devices
{
    public class WindowsCoreDeviceEventArgs : EventArgs
    {
        private WindowsCoreDevice device;

        public WindowsCoreDeviceEventArgs(WindowsCoreDevice device)
        {
            this.device = device;
        }

        public WindowsCoreDevice Device
        {
            get
            {
                return this.device;
            }
        }
    }
}
