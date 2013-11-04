using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TeleMaster.View;
using System.Xml;
using TeleMaster.Management;
using TeleMaster.DAO;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;

namespace TeleMaster
{
    enum MonitoringState { InProgress, Stopped };
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker bwDevices = new BackgroundWorker();
        BackgroundWorker bwUpdateUI = new BackgroundWorker();

        void CreateDevices()
        {
            for (int i = 4; i < 26;i++)
            {
                Device d = new Device();
                d.DeviceEnabledAnalogue = true;
                d.DeviceEnabledDigital = true;
                d.DeviceEnabledUPS = false;
                d.Host = "10.90.1." + i.ToString();
                d.ID = Guid.NewGuid();
                d.Name = d.Host;
                Monitor.Instance.Devices.Add(d);
            }
            Monitor.Instance.SaveDevices();
        }
        public MainWindow()
        {
            InitializeComponent();

            bwDevices.WorkerSupportsCancellation = true;
            bwUpdateUI.WorkerSupportsCancellation = true;

            bwDevices.DoWork += new DoWorkEventHandler(CheckDeviceEvents);
            bwUpdateUI.DoWork += new DoWorkEventHandler(bwUpdated_DoWork);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
            StartMonitor();
        }

        void bwUpdated_DoWork(object sender, DoWorkEventArgs e)
        {
            bool blinker = true;
            while (true)
            {
                if (this.bwUpdateUI.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                lsDisplay.Dispatcher.Invoke(new Action(() =>
                {
                    lsDisplay.DataContext = null;
                    lsDisplay.DataContext = Monitor.Instance.Devices;
                }));
                lvDeviceLog.Dispatcher.Invoke(new Action(() =>
                    {
                        lvDeviceLog.DataContext = null;
                        lvDeviceLog.DataContext = Monitor.Instance.Events;
                    }));
                if (blinker)
                {
                    stTime.Dispatcher.Invoke(new Action(() => { stTime.Text = "Мониторинг"; }));
                }
                else
                {
                    stTime.Dispatcher.Invoke(new Action(() => { stTime.Text = ""; }));
                }
                blinker = !blinker;
                System.Threading.Thread.Sleep(1000);
            }
        }

        void CreateDevice()
        {
            DeviceEdit w = new DeviceEdit();
            w.ShowDialog();
            RefreshInfo();
        }
        void EditDevice(Device toEdit)
        {
            DeviceEdit w = new DeviceEdit(toEdit);
            w.ShowDialog();
            RefreshInfo();
        }
        void DeleteDevice(Device toDelete)
        {
            Monitor.Instance.Devices.Remove(toDelete);
            Monitor.Instance.SaveDevices();
            RefreshInfo();
        }

        void RefreshInfo()
        {
            RefreshDevices();
            RefreshEvents();
        }
        void RefreshDevices()
        {
            foreach (Device device in Monitor.Instance.Devices)
            {
                if (Monitor.Instance.Events.Exists(e => e.DeviceID == device.ID && e.Type == EventType.AlertForAnalogue))
                {
                    device.DeviceAnalogueHasAlerts = true;
                }
                else
                {
                    device.DeviceAnalogueHasAlerts = false;
                }
                if (Monitor.Instance.Events.Exists(e => e.DeviceID == device.ID && e.Type == EventType.AlertForDigital))
                {
                    device.DeviceDigitalHasAlerts = true;
                }
                else
                {
                    device.DeviceDigitalHasAlerts = false;
                }
            }
            lsDisplay.DataContext = null;
            lsDisplay.DataContext = Monitor.Instance.Devices.OrderBy(d => d.Name);
        }
        void RefreshEvents()
        {
            lvDeviceLog.DataContext = null;
            lvDeviceLog.DataContext = Monitor.Instance.Events;
        }

        void GetNewEvents(Device device)
        {
            // ping server
            Ping p = new Ping();
            PingReply reply = p.Send(device.Host);
            if (reply.Status != IPStatus.Success)
            {
                if (!device.IsDisconnected)
                {
                    string message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
                                        + "\tАдрес " + device.Host + " недоступен!";
                    device.LogEventToFile(message);
                    Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, EventType.Disconnect));
                    device.IsDisconnected = true;
                }
                return;
            }
            // если был отключен, а теперь появился
            if (device.IsDisconnected)
            {
                string message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "\tСоединение восстановлено.";
                Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, EventType.Disconnect));

                message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "\tДоступ восстановлен.";
                device.LogEventToFile(message);

                device.IsDisconnected = false;
            }
            if (device.DeviceEnabledAnalogue)
            {
                if (device.LastReadDateForAnalogue.Date != DateTime.Now.Date)
                {
                    device.LastReadRowIndexForAnalogue = 0;
                    device.LastReadDateForAnalogue = DateTime.Now.Date;
                }
                string dir = "logs_a";
                device.LastReadRowIndexForAnalogue = ReadRemoteLogFile(dir, device.LastReadRowIndexForAnalogue, device, EventType.AlertForAnalogue);
            }
            if (device.DeviceEnabledDigital)
            {
                if (device.LastReadDateForDigital.Date != DateTime.Now.Date)
                {
                    device.LastReadRowIndexForDigital = 0;
                    device.LastReadDateForDigital = DateTime.Now.Date;
                }
                string dir = "logs_d";
                device.LastReadRowIndexForDigital = ReadRemoteLogFile(dir, device.LastReadRowIndexForDigital, device, EventType.AlertForDigital);
            }
            if (device.DeviceEnabledUPS)
            {
            }            
        }
        int ReadRemoteLogFile(string dir, int lastReadIndex, Device device, EventType eventType)
        {
            int newIndex = lastReadIndex;
            // read file
            int d = DateTime.Now.Day;
            string dd = d.ToString();
            if (d < 10)
                dd = "0" + d;
            string filename = "history_" + DateTime.Now.Year + "_"
                                         + DateTime.Now.Month + "_"
                                         + dd + ".log";
            string filePath = @"\\" + device.Host + @"\" + dir + @"\" + filename;

            if (File.Exists(filePath))
            {
                // read in ANSI encoding
                StreamReader sr = new StreamReader(filePath, Encoding.Default);
           
                for (int i = 0; i < lastReadIndex; i++)
                {
                    sr.ReadLine();
                }                
                while (!sr.EndOfStream)
                {                    
                    string message = sr.ReadLine();
                    Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, eventType));
                    device.LogEventToFile(message);
                    newIndex++;
                }
                sr.Close();
            }
            return newIndex;
        }
        void CheckDeviceEvents(object sender, DoWorkEventArgs e)
        {
            int updateInterval = Monitor.Instance.UpdateInterval * 1000; // seconds to milliseconds
            while (true)
            {
                if (this.bwDevices.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                foreach (Device device in Monitor.Instance.Devices)
                {
                    stState.Dispatcher.Invoke(new Action(() => stState.Text = device.Name));
                    BackgroundWorker deviceWorker = new BackgroundWorker();
                    deviceWorker.DoWork += new DoWorkEventHandler(deviceWorker_DoWork);
                    deviceWorker.RunWorkerAsync(device);
                }
                System.Threading.Thread.Sleep(updateInterval);
            }
        }
        void deviceWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Device device = e.Argument as Device;
            GetNewEvents(device);
        }

        void VerifyEvent(Event ev)
        {
            Guid deviceID = ev.DeviceID;
            Monitor.Instance.Events.Remove(ev);
            if (!Monitor.Instance.Events.Exists(e => e.DeviceID == deviceID))
            {
                if (Monitor.Instance.Devices.Exists(d => d.ID == deviceID))
                {
                    Monitor.Instance.Devices.FirstOrDefault(d => d.ID == deviceID).DeviceAnalogueHasAlerts = false;
                    Monitor.Instance.Devices.FirstOrDefault(d => d.ID == deviceID).DeviceDigitalHasAlerts = false;
                    Monitor.Instance.SaveDevices();
                    RefreshDevices();
                }
            }
            RefreshEvents();
        }
        void VerifyAllEvents()
        {
            Monitor.Instance.Events.Clear();
            RefreshEvents();
            RefreshDevices();
        }
        void VerifyAllEvents(Device device)
        {
            Monitor.Instance.Events.RemoveAll(e => e.DeviceID == device.ID);
            RefreshEvents();
            RefreshDevices();
        }
        void ShowDeviceEventsHistory(Device device)
        {
            DeviceEventsWindow w = new DeviceEventsWindow(device);
            w.ShowDialog();
        }

        #region HANDLERS
        private void cmDevices_Events_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            ShowDeviceEventsHistory(lsDisplay.SelectedItem as Device);
        }
        private void cmDevices_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            DeleteDevice(lsDisplay.SelectedItem as Device);
        }
        private void cmDevices_VerifyEvents_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            VerifyAllEvents(lsDisplay.SelectedItem as Device);
        }

        private void lsDisplay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            ShowDeviceEventsHistory(lsDisplay.SelectedItem as Device);
        }
        private void cmDevices_Create_Click(object sender, RoutedEventArgs e)
        {
            CreateDevice();
        }
        private void miVerifyAllEvents_Click(object sender, RoutedEventArgs e)
        {
            VerifyAllEvents();
        }

        private void miMainAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow wind = new AboutWindow();
            wind.ShowDialog();
        }

        private void miMainExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void lvDeviceLog_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvDeviceLog.SelectedItem == null)
            {
                return;
            }
            Event ev = lvDeviceLog.SelectedItem as Event;
            VerifyEvent(ev);
        }
        private void cmDevices_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            EditDevice(lsDisplay.SelectedItem as Device);
        }
        private void miMonitorStart_Click(object sender, RoutedEventArgs e)
        {
            StartMonitor();
        }

        private void miMonitorStop_Click(object sender, RoutedEventArgs e)
        {
            StopMonitor();
        }
        #endregion

        void StartMonitor()
        {
            Monitor.Instance.Events.Add(new Event("СТАРТ МОНИТОРИНГА"));
            // UI
            miMonitorStart.IsEnabled = false;
            miMonitorStop.IsEnabled = true;
            cmDevices_Create.IsEnabled = false;
            cmDevices_Edit.IsEnabled = false;
            cmDevices_Delete.IsEnabled = false;

            bwUpdateUI.RunWorkerAsync();
            bwDevices.RunWorkerAsync();

        }
        void StopMonitor()
        {
            // UI
            miMonitorStart.IsEnabled = true;
            miMonitorStop.IsEnabled = false;
            cmDevices_Create.IsEnabled = true;
            cmDevices_Edit.IsEnabled = true;
            cmDevices_Delete.IsEnabled = true;

            bwDevices.CancelAsync();
            bwUpdateUI.CancelAsync();

            stTime.Text = "Остановлено";
            Monitor.Instance.Events.Add(new Event("МОНИТОРИНГ ОСТАНОВЛЕН"));
            RefreshEvents();
        }
    }
}
