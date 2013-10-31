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

namespace TeleMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker bw = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
            foreach (Device device in Monitor.Instance.Devices)
                device.HasAlerts = false;
            bw.DoWork += new DoWorkEventHandler(CheckDeviceEvents);
            bw.RunWorkerAsync();
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
                if (Monitor.Instance.Events.Exists(e => e.DeviceID == device.ID && e.Type == EventType.Alert))
                {
                    device.HasAlerts = true;
                }
                else
                {
                    device.HasAlerts = false;
                }
            }
            lsDisplay.DataContext = null;
            lsDisplay.DataContext = Monitor.Instance.Devices;            
        }
        void RefreshEvents()
        {
            lvDeviceLog.DataContext = null;
            lvDeviceLog.DataContext = Monitor.Instance.Events;
        }
        
        void CheckDeviceEvents(object sender, DoWorkEventArgs e)
        {
            int updateInterval = Monitor.Instance.UpdateInterval*1000; // seconds to milliseconds
            bool blinker = false;
            while (true)
            {
                bool needToUpdate = false;
                foreach (Device device in Monitor.Instance.Devices)
                {                    
                    // read file
                    string filePath = device.EventSource;
                    string fileName = "history_" + DateTime.Now.Year + "_"
                                                 + DateTime.Now.Month + "_"
                                                 + DateTime.Now.Day + ".log";
                    string fileFullName = filePath + @"\" + fileName;
                    if (!File.Exists(fileFullName))
                    {
                        if(!device.IsDisconnected)
                        {
                            string message  = DateTime.Now.ToShortTimeString() + ". " + fileFullName + " - нет доступа!";
                            Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, EventType.Disconnect));                        
                            device.IsDisconnected = true;
                            needToUpdate = true;
                        }                                               
                    }
                    else
                    {                                                
                        // read in ANSI encoding
                        StreamReader sr = new StreamReader(fileFullName, Encoding.Default);                        
                        int newIndex = device.LastReadRowIndex;
                        
                        for (int i = 0; i < device.LastReadRowIndex; i++)
                        {
                            sr.ReadLine();
                        }
                        bool indexChanged = false;
                        while (!sr.EndOfStream)
                        {
                            indexChanged = true;
                            Monitor.Instance.Events.Add(new Event(sr.ReadLine(), device.Name, device.ID, EventType.Alert));
                            newIndex++;
                        }
                        sr.Close();
                        if (indexChanged)
                        {                            
                            device.LastReadRowIndex = newIndex;
                            needToUpdate = true;
                        }
                        if (device.IsDisconnected)
                        {
                            string message = DateTime.Now.ToShortTimeString() + ". Соединение восстановлено.";
                            Monitor.Instance.Events.Add(new Event(message, device.Name, device.ID, EventType.Disconnect));                                                    
                            device.IsDisconnected = false;
                            needToUpdate = true;
                        }
                    }
                }
                if (needToUpdate)
                {
                    Monitor.Instance.SaveDevices();
                    lsDisplay.Dispatcher.Invoke(new Action(() => { RefreshInfo(); }));
                }
                System.Threading.Thread.Sleep(updateInterval);
                if (blinker)
                {
                    stTime.Dispatcher.Invoke(new Action(() => { stTime.Text = "Мониторинг"; }));
                }
                else
                {
                    stTime.Dispatcher.Invoke(new Action(() => { stTime.Text = ""; }));
                }
                blinker = !blinker;
            }
        }
   
        void VerifyEvent(Event ev)
        {
            Guid deviceID = ev.DeviceID;
            Monitor.Instance.Events.Remove(ev);
            if (!Monitor.Instance.Events.Exists(e => e.DeviceID == deviceID))
            {
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == deviceID).HasAlerts = false;
                Monitor.Instance.SaveDevices();
                RefreshDevices();
            }
            RefreshEvents();
        }                
        void VerifyAllEvents()
        {
            Monitor.Instance.Events.Clear();
            RefreshEvents();            
            RefreshDevices();
        }

        #region HANDLERS
        private void cmDevices_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            DeleteDevice(lsDisplay.SelectedItem as Device);
        }

        private void lbDevices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            EditDevice(lsDisplay.SelectedItem as Device);
        }

        private void lsDisplay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            EditDevice(lsDisplay.SelectedItem as Device);
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
        #endregion
    }
}
