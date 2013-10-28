using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleMaster.DAO
{
    public class Event
    {
        public Event(string message, string device)
        {
            this.deviceName = device;
            this.message = message;
        }
        string deviceName;
        string message;

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public string DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; }
        }        
    }
}
