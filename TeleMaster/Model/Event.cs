using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleMaster.DAO
{
    public enum EventType { AlertForAnalogue, AlertForDigital, Disconnect, NoType};
    public enum EventState { Новое, Просмотрено};
    public class Event
    {
        Guid deviceID = Guid.Empty;
        EventType type = EventType.NoType;

        public EventType Type
        {
            get { return type; }
            set { type = value; }
        }

        public Guid DeviceID
        {
            get { return deviceID; }            
        }
        EventState state = EventState.Новое;

        public EventState State
        {
            get { return state; }
            set { state = value; }
        }
        public void Verify()
        {
            state = EventState.Просмотрено;
        }
        public Event(string message)
        {
            this.deviceName = "";
            this.message = message;
            this.deviceID = Guid.Empty;
            this.type = EventType.NoType;
        }
        public Event(string message, string device, Guid deviceID, EventType type)
        {
            this.deviceName = device;
            this.message = message;
            this.deviceID = deviceID;
            this.type = type;
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
        DateTime createdOn = DateTime.Now;

        public DateTime CreatedOn
        {
            get { return createdOn; }            
        }
    }
}
