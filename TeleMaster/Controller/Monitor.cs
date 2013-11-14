using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeleMaster.DAO;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Media;

namespace TeleMaster.Management
{
    class Monitor
    {
        private static Monitor instance = new Monitor();
        protected Monitor()
        {            
            this.LoadDevices();
            events = new List<Event>();
        }
        public static Monitor Instance
        {
            get
            {
                return instance;
            }
        }
        List<Event> events;

        public List<Event> Events
        {
            get { return events; }            
        }
        public void AddEvent(Device device, string message, EventType type)
        {
            if (type == EventType.Сеть)
                message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "\t" + message;
            
            device.LogEventToFile(message, type);
            Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, type));
            ///SystemSounds.Beep.Play();
        }
        public void AddEvent(string message, EventType type)
        {
            if (type == EventType.ИБП)
                message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "\t" + message;
            
            Monitor.Instance.Events.Add(new Event(message, "SNMP", Guid.Empty, type));
            ///SystemSounds.Beep.Play();
        }
        List<Device> devices;
        internal List<Device> Devices
        {
            get { return devices; }            
        }
        int updateInterval = 2000; // // milliseconds

        public int UpdateInterval
        {
            get { return updateInterval; }            
        }

        private void LoadDevices()
        {
            devices = new List<Device>();
            if(File.Exists(filename))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Device>));
                TextReader stream = new StreamReader(filename);
                devices = (List<Device>)serializer.Deserialize(stream);
                stream.Close();                
            }
        }
        string filename = "devices2.xml";

        public void SaveDevices()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Device>));
            TextWriter stream = new StreamWriter(filename);
            serializer.Serialize(stream, devices);
            stream.Close();           
        }
    }
}
