using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleMaster.DAO
{
    public class Device
    {
        public Device()
        {
            
        }
        public Device(string name, string logSrc)
        {
            this.name = name;
            this.eventSource = logSrc;
        }
        string name;
        string eventSource;
        bool hasAlerts = false;

        int lastReadRowIndex = 0;

        public int LastReadRowIndex
        {
            get { return lastReadRowIndex; }
            set { lastReadRowIndex = value; }
        }

        public bool HasAlerts
        {
            get { return hasAlerts; }
            set { hasAlerts = value; }
        }
        public string EventSource
        {
            get { return eventSource; }
            set { eventSource = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        //EventSource source;
        List<Event> events;
        DeviceIcon icon;
    }
}
