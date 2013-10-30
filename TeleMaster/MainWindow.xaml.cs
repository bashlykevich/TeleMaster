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
                        events.Add(new Event(DateTime.Now.ToShortTimeString() + ". " + fileFullName + " не найден", device.Name));
                        lvDeviceLog.Dispatcher.Invoke(upd);
                    }
                    else
                    {                        
                        // read in ANSI encoding
                        // для тестовой версии - DEFAULT
                        StreamReader sr = new StreamReader(fileFullName, Encoding.Default);
                        // для работей версии ANSI
                        // StreamReader sr = new StreamReader(fileFullName, Encoding.GetEncoding(1252));
                        int newIndex = device.LastReadRowIndex;
                        for (int i = 0; i < device.LastReadRowIndex; i++)
                        {
                            sr.ReadLine();
                        }
                        while (!sr.EndOfStream)
                        {
                            events.Add(new Event(sr.ReadLine(), device.Name));
                            newIndex++;
                        }
                        lvDeviceLog.Dispatcher.Invoke(upd);
                        device.LastReadRowIndex = newIndex;
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
            events.Remove(ev);
            RefreshEvents();
        }
        void RefreshEvents()
        {
            lvDeviceLog.DataContext = events;
        }

        private void lvDeviceLog_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvDeviceLog.SelectedItem == null)
                return;
            Event ev = lvDeviceLog.SelectedItem as Event;
            VerifyEvent(ev);            
        }
    }
}
