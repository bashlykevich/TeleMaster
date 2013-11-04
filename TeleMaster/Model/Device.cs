using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TeleMaster.Helpers;

namespace TeleMaster.DAO
{
    public class Device
    {
        Guid id;
        // Название точки (адрес)
        string name;
        // адрес сервера
        string host = "";
        bool deviceEnabledAnalogue;
        bool deviceEnabledDigital;
        bool deviceEnabledUPS;        

        #region getters-setters
        public Guid ID
        {
            get { return id; }
            set { id = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string DisplayName
        {
            get { return name + @"/" + this.host; }
        }
        public string Host
        {
            get { return host; }
            set { host = value; }
        }
        public bool DeviceEnabledAnalogue
        {
            get { return deviceEnabledAnalogue; }
            set { deviceEnabledAnalogue = value; }
        }
        public bool DeviceEnabledDigital
        {
            get { return deviceEnabledDigital; }
            set { deviceEnabledDigital = value; }
        }
        public bool DeviceEnabledUPS
        {
            get { return deviceEnabledUPS; }
            set { deviceEnabledUPS = value; }
        }
        public bool DeviceDisabledAnalogue
        {
            get { return !deviceEnabledAnalogue; }            
        }
        public bool DeviceDisabledDigital
        {
            get { return !deviceEnabledDigital; }            
        }
        public bool DeviceDisabledUPS
        {
            get { return !deviceEnabledUPS; }            
        }
        #endregion


        // Analog
        int lastReadRowIndexForAnalogue = 0;

        public int LastReadRowIndexForAnalogue
        {
            get { return lastReadRowIndexForAnalogue; }
            set { lastReadRowIndexForAnalogue = value; }
        }
        DateTime lastReadDateForAnalogue;

        public DateTime LastReadDateForAnalogue
        {
            get { return lastReadDateForAnalogue; }
            set { lastReadDateForAnalogue = value; }
        }

        // Digital
        int lastReadRowIndexForDigital = 0;

        public int LastReadRowIndexForDigital
        {
            get { return lastReadRowIndexForDigital; }
            set { lastReadRowIndexForDigital = value; }
        }
        DateTime lastReadDateForDigital;

        public DateTime LastReadDateForDigital
        {
            get { return lastReadDateForDigital; }
            set { lastReadDateForDigital = value; }
        }
        // UPS

        
        // флаги для отображения        

        public bool DeviceAnalogueHasAlerts
        {
            get
            {
                if (!this.deviceEnabledAnalogue)
                    return false;
                bool hasAlerts = TeleMaster.Management.Monitor.Instance.Events.Exists(e => e.DeviceID == this.id && e.Type == EventType.AlertForAnalogue);
                return hasAlerts; 
            }            
        }
        
        public bool DeviceDigitalHasAlerts
        {
            get
            {
                if (!this.deviceEnabledDigital)
                    return false;
                bool hasAlerts = TeleMaster.Management.Monitor.Instance.Events.Exists(e => e.DeviceID == this.id && e.Type == EventType.AlertForDigital);
                return hasAlerts; 
            }
        }
        bool deviceUpsHasAlerts = false;

        public bool DeviceUpsHasAlerts
        {
            get 
            {
                if (!this.deviceEnabledUPS)
                    return false;
                return deviceUpsHasAlerts;
            }            
        }
        
        bool isDisconnected = false;

        public bool IsDisconnectedA
        {
            get 
            {
                if (this.deviceEnabledAnalogue)
                    return isDisconnected;
                else
                    return false;
            }
        }
        public bool IsDisconnectedD
        {
            get
            {
                if (this.deviceEnabledDigital)
                    return isDisconnected;
                else
                    return false;
            }
        }
        public bool IsDisconnected
        {
            get { return isDisconnected; }
            set { isDisconnected = value; }
        }
        bool deviceUpsIsDisconnected = false;

        public bool DeviceUpsIsDisconnected
        {
            get { return deviceUpsIsDisconnected; }
            set { deviceUpsIsDisconnected = value; }
        }        
        
                        
        public Device()
        {
            
        }
        public Device(string name, string host, bool enabledAnalogue, bool enabledDigital, bool enabledUps)
        {
            this.name = name;
            this.host = host;
            this.deviceEnabledAnalogue = enabledAnalogue;
            this.deviceEnabledDigital = enabledDigital;
            this.deviceEnabledUPS = enabledUps;

            this.lastReadDateForAnalogue = DateTime.Now.Date;
            this.lastReadDateForDigital = DateTime.Now.Date;
        }                
        
        public void LogEventToFile(string message)
        {
            string filePath = "logs";
            if(!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string safeName = IOHelper.GetSafeFilename(this.name);
            string fileName = safeName + "_" + DateTime.Now.Year + "_"
                                         + DateTime.Now.Month + "_"
                                         + DateTime.Now.Day + ".log";
            string fileFullName = filePath + @"\" + fileName;
            bool wasWritten = false;
            while(!wasWritten)
            {
                try
                {
                    TextWriter stream = new StreamWriter(fileFullName, true);
                    stream.WriteLine(message);
                    stream.Close();
                    wasWritten = true;
                } catch(Exception e)
                {

                }
            }
        }
    }
}
