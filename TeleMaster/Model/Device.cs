using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleMaster.DAO
{
    public enum DeviceType {TelescanerA=0, TelescanerD=1, UPS=2};

    public class Device
    {
        DeviceType type = DeviceType.TelescanerA;
        
        public bool TypeIsTelescanerA
        {
            get
            {
                return (type == DeviceType.TelescanerA);
            }
        }
        public bool TypeIsTelescanerD
        {
            get
            {
                return (type == DeviceType.TelescanerD);
            }
        }
        public bool TypeIsUPS
        {
            get
            {
                return (type == DeviceType.UPS);
            }
        }
        bool isDisconnected = false;

        public bool IsDisconnected
        {
            get { return isDisconnected; }
            set { isDisconnected = value; }
        }

        public DeviceType Type
        {
            get { return type; }
            set { type = value; }
        }
        Guid id = Guid.NewGuid();
        public Guid ID
        {
            get { return id; }
            set { id = value; }
        }
        public Device()
        {
            
        }
        public Device(string name, string logSrc, int type)
        {
            this.name = name;
            this.eventSource = logSrc;
            this.type = (DeviceType)type;
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
