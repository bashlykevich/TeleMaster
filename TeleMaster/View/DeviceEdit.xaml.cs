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
using TeleMaster.Management;

namespace TeleMaster.View
{
    /// <summary>
    /// Interaction logic for DeviceEdit.xaml
    /// </summary>
    public partial class DeviceEdit : Window
    {
        private Device item = null;
        public DeviceEdit()
        {
            InitializeComponent();
        }
        public DeviceEdit(Device d)
        {
            InitializeComponent();
            this.item = d;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (this.item == null)
            { //create
                Device d = new Device(edtName.Text, edtSource.Text);
                Monitor.Instance.Devices.Add(d);                
            }
            else
            { // edit                
                int ind = Monitor.Instance.Devices.IndexOf(this.item);
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Name = edtName.Text;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).EventSource = edtSource.Text;                
            }
            Monitor.Instance.SaveDevices();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            if (this.item != null)
            {
                edtName.Text = item.Name;
                edtSource.Text = item.EventSource;
            }
        }
    }
}
