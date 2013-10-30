using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleMaster.DAO
{
    public enum EventState { Новое, Просмотрено};
    public class Event
    {
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
        DateTime createdOn = DateTime.Now;

        public DateTime CreatedOn
        {
            get { return createdOn; }            
        }
    }
}
