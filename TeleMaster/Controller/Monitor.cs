using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeleMaster.DAO;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

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
