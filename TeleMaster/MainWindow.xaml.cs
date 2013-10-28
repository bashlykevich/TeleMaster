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
        void EditDevice()
        {
            if (lbDevices.SelectedItem != null)
            {
                Device d = Monitor.Instance.Devices.FirstOrDefault(dev => dev.Name == lbDevices.SelectedValue);
                DeviceEdit w = new DeviceEdit(d);
                w.ShowDialog();
                RefreshDevices();
            }
        }
        void RefreshDevices()
        {
            lbDevices.Items.Clear();
            Monitor.Instance.LoadDevices();
            foreach(Device d in Monitor.Instance.Devices)
            {
                lbDevices.Items.Add(d.Name);
            }
            lsDisplay.DataContext = Monitor.Instance.Devices;            
        }
        List<Event> events = new List<Event>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
            
            lvDeviceLog.DataContext = events;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerAsync();
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
            lvDeviceLog.DataContext = null;
            lvDeviceLog.DataContext = events;
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Action upd = new Action(() =>
                        {
                            lvDeviceLog.DataContext = null;
                            lvDeviceLog.DataContext = events;
                        });
            int updateInterval = Monitor.Instance.UpdateInterval*1000;
            while(true)
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
                        break;
                    }                    
                    StreamReader sr = new StreamReader(fileFullName);
                    int newIndex =device.LastReadRowIndex;
                    for (int i = 0; i < device.LastReadRowIndex; i++)
                    {
                        sr.ReadLine();                        
                    }
                    while(!sr.EndOfStream)
                    {                        
                        events.Add(new Event(sr.ReadLine(), device.Name));
                        newIndex++;                        
                    }
                    lvDeviceLog.Dispatcher.Invoke(upd);
                    device.LastReadRowIndex = newIndex;                    
                }
                System.Threading.Thread.Sleep(updateInterval);
            }
        }
        BackgroundWorker bw = new BackgroundWorker();
      

        private void cmDevices_Delete_Click(object sender, RoutedEventArgs e)
        {            
            if(lbDevices.SelectedItem != null)
            {
                Device d = Monitor.Instance.Devices.FirstOrDefault(dev => dev.Name == lbDevices.SelectedValue);
                Monitor.Instance.Devices.Remove(d);
                Monitor.Instance.SaveDevices();
                RefreshDevices();
            }
        }

        private void lbDevices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditDevice();
        }

        private void lsDisplay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            EditDevice(lsDisplay.SelectedItem as Device);
        }
    }
}
