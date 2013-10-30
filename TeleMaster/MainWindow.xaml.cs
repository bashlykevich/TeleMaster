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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void cmDevices_Create_Click(object sender, RoutedEventArgs e)
        {
            CreateDevice();
        }
        void CreateDevice()
        {
            DeviceEdit w = new DeviceEdit();
            w.ShowDialog();
            RefreshDevices();
        }
        void EditDevice(Device toEdit)
        {
            DeviceEdit w = new DeviceEdit(toEdit);
            w.ShowDialog();
            RefreshDevices();
        }
        
        void RefreshDevices()
        {        
            Monitor.Instance.LoadDevices();
            lsDisplay.DataContext = null;
            lsDisplay.DataContext = Monitor.Instance.Devices;
            lvDeviceLog.DataContext = events;
        }
        List<Event> events = new List<Event>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
            foreach (Device device in Monitor.Instance.Devices)
                device.HasAlerts = false;                  
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Action upd = new Action(() =>
                        {
                            lvDeviceLog.DataContext = null;
                            lvDeviceLog.DataContext = events;

                        });
            int updateInterval = Monitor.Instance.UpdateInterval*1000;
            bool blinker = false;
            while (true)
            {                
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
                        events.Add(new Event(DateTime.Now.ToShortTimeString() + ". " + fileFullName + " - нет доступа!", device.Name, device.ID));
                        lvDeviceLog.Dispatcher.Invoke(upd);
                        device.HasAlerts = true;
                        Monitor.Instance.SaveDevices();
                        lsDisplay.Dispatcher.Invoke(new Action(() => { RefreshDevices(); }));
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
                        bool hasNewEvents = false;
                        while (!sr.EndOfStream)
                        {
                            hasNewEvents = true;
                            events.Add(new Event(sr.ReadLine(), device.Name, device.ID));
                            newIndex++;
                        }
                        sr.Close();
                        if (hasNewEvents)
                        {
                            lvDeviceLog.Dispatcher.Invoke(upd);
                            device.LastReadRowIndex = newIndex;
                            device.HasAlerts = true;
                            Monitor.Instance.SaveDevices();
                            lsDisplay.Dispatcher.Invoke(new Action(() => { RefreshDevices(); }));
                        }
                        
                    }
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
        BackgroundWorker bw = new BackgroundWorker();

        void DeleteDevice(Device toDelete)
        {
            Monitor.Instance.Devices.Remove(toDelete);
            Monitor.Instance.SaveDevices();
            RefreshDevices();
        }
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
        
        void VerifyEvent(Event ev)
        {
            Guid deviceID = ev.DeviceID;
            events.Remove(ev);
            if (!events.Exists(e => e.DeviceID == deviceID))
            {
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == deviceID).HasAlerts = false;
                Monitor.Instance.SaveDevices();
                RefreshDevices();
            }
            RefreshEvents();
        }
        void RefreshEvents()
        {
            lvDeviceLog.DataContext = null;
            lvDeviceLog.DataContext = events;            
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
        void VerifyAllEvents()
        {
            events.Clear();
            RefreshEvents();
            foreach (Device device in Monitor.Instance.Devices)
            {
                if (!events.Exists(e => e.DeviceID == device.ID))
                {
                    Monitor.Instance.Devices.FirstOrDefault(d => d.ID == device.ID).HasAlerts = false;                    
                }
            }
            Monitor.Instance.SaveDevices();
            RefreshDevices();
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
    }
}
