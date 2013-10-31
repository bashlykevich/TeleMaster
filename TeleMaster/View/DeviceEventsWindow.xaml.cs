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
using System.Windows.Shapes;
using TeleMaster.DAO;
using System.IO;

namespace TeleMaster.View
{
    /// <summary>
    /// Interaction logic for DeviceEventsWindow.xaml
    /// </summary>
    public partial class DeviceEventsWindow : Window
    {
        Device device = null;
        public DeviceEventsWindow(Device device)
        {
            InitializeComponent();
            this.device = device;
            edtDate.SelectedDate = DateTime.Now;
            LoadLog(DateTime.Now);
        }
        private void edtDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (edtDate.SelectedDate.HasValue)
            {
                LoadLog(edtDate.SelectedDate.Value);
            }
        }
        void LoadLog(DateTime date)
        {
            edtLog.Items.Clear();
            string filePath = "logs";
            string fileName = device.Name + "_" + date.Year + "_"
                                         + date.Month + "_"
                                         + date.Day + ".log";
            string fileFullName = filePath + @"\" + fileName;
            if (File.Exists(fileFullName))
            {
                StreamReader sr = new StreamReader(fileFullName);
                while (!sr.EndOfStream)
                {
                    edtLog.Items.Add(sr.ReadLine());
                }
                sr.Close();
            }
            else
            {
                edtLog.Items.Add("На указанную дату логов нет.");
            }
        }
    }
}
