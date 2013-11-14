using System;
using System.IO;
using TeleMaster.Helpers;

namespace TeleMaster.DAO
{
    

    public enum UPSType { NoType, Socomec, AP9630};
    public class Device
    {
        Guid id;
        // Название точки (адрес)
        string name;
        // адрес сервера
        string host = "";
        string upsHost = "";
        string community = "public";

        public string Community
        {
            get { return community; }
            set { community = value; }
        }

        DateTime lastOnlineTimeTS = DateTime.MinValue;
        DateTime lastOnlineTimeUps = DateTime.MinValue;

        public DateTime LastOnlineTimeUps
        {
            get { return lastOnlineTimeUps; }
            set { lastOnlineTimeUps = value; }
        }

        public DateTime LastOnlineTimeTS
        {
            get { return lastOnlineTimeTS; }
            set { lastOnlineTimeTS = value; }
        }

        UPSType upsType = UPSType.NoType;

        public UPSType UpsType
        {
            get { return upsType; }
            set { upsType = value; }
        }

        public string RDPUser = "user";
        public string RDPPassword = "1";

        public string UpsHost
        {
            get { return upsHost; }
            set { upsHost = value; }
        }
        bool deviceEnabledAnalogue;
        bool deviceEnabledDigital;
        bool deviceEnabledUPS;

        string batteryCapacityRemaining = "";
        string batteryVoltage = "";
        string outputLoad = "";
        string batteryStatus = "";

        public string BatteryStatus
        {
            get 
            {
                string res = "";
                switch (batteryStatus)
                {
                    case "2":
                        res = "normal";
                        break;
                    case "3":
                        res = "low";
                        break;
                    case "4":
                        res = "depleted";
                        break;
                    case "5":
                        res = "discharging";
                        break;
                    case "6":
                        res = "failure";
                        break;
                    default:
                        res = "unknown";
                        break;
                }
                return res;
            }
            set { batteryStatus = value; }
        }

        public string OutputLoad
        {
            get {
                /*if (outputLoad == "")
                    return "OL: N/A";
                else*/
                    return outputLoad;
            }
            set { outputLoad = value; }
        }
        public string BatteryVoltage
        {
            get
            {
                /*if (batteryVoltage == "")
                    return "BV: N/A";
                else*/
                    return batteryVoltage;
            }
            set { batteryVoltage = value; }
        }

        public string BatteryCapacityRemaining
        {
            get 
            {
                //if (batteryCapacityRemaining == "")
                //    return "BC: N/A";
                //else
                    return batteryCapacityRemaining;
            }
            set { batteryCapacityRemaining = value; }
        }


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

        public bool ShowUpsInfo
        {
            get
            {
                return DeviceEnabledUPS && !deviceUpsIsDisconnected;
            }
        }   

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
                bool hasAlerts = TeleMaster.Management.Monitor.Instance.Events.Exists(e => e.DeviceID == this.id && e.Type == EventType.Аналог);
                return hasAlerts;
            }
        }

        public bool DeviceDigitalHasAlerts
        {
            get
            {
                if (!this.deviceEnabledDigital)
                    return false;
                bool hasAlerts = TeleMaster.Management.Monitor.Instance.Events.Exists(e => e.DeviceID == this.id && e.Type == EventType.Цифра);
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
        public Device(string name, string host, string hostUps, bool enabledAnalogue, bool enabledDigital, bool enabledUps, int upsType, string comm)
        {
            this.name = name;
            this.host = host;
            this.upsHost = hostUps;
            this.deviceEnabledAnalogue = enabledAnalogue;
            this.deviceEnabledDigital = enabledDigital;
            this.deviceEnabledUPS = enabledUps;

            this.community = comm;
            this.lastReadDateForAnalogue = DateTime.Now.Date;
            this.lastReadDateForDigital = DateTime.Now.Date;

            this.upsType = (UPSType)upsType;
        }

        public void LogEventToFile(string message, EventType type)
        {
            message = type.ToString() + "\t" + message;
            string filePath = "logs";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string safeName = IOHelper.GetSafeFilename(this.name);
            string fileName = safeName + "_" + DateTime.Now.Year + "_"
                                         + DateTime.Now.Month + "_"
                                         + DateTime.Now.Day + ".log";
            string fileFullName = filePath + @"\" + fileName;
            bool wasWritten = false;
            while (!wasWritten)
            {
                try
                {
                    TextWriter stream = new StreamWriter(fileFullName, true);
                    stream.WriteLine(message);
                    stream.Close();
                    wasWritten = true;
                }
                catch (Exception e)
                {

                }
            }
        }
    }
}
