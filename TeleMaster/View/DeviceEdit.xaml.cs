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
using System.Net;

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
            IPAddress ip;
            if (!IPAddress.TryParse(edtSource.Text, out ip))
            {
                MessageBox.Show("Ошибка формата IP-адреса сервера телесканеров!");
                return;
            }
            int upsType = 0;
            if (edtEnabledUPS.IsChecked.Value)
            {
                IPAddress ipUPS;
                if (!IPAddress.TryParse(edtUPSSource.Text, out ipUPS))
                {
                    MessageBox.Show("Ошибка формата IP-адреса ИБП!");
                    return;
                }

                if (!Int32.TryParse(edtUPSType.Text, out upsType))
                {
                    MessageBox.Show("Ошибка формата типа ИБП!");
                    return;
                }
            }

            bool enabledAnalogue = edtEnabledAnalogue.IsChecked.HasValue ? edtEnabledAnalogue.IsChecked.Value : false;
            bool enabledDigital = edtEnabledDigital.IsChecked.HasValue ? edtEnabledDigital.IsChecked.Value : false;
            bool enabledUps = edtEnabledUPS.IsChecked.HasValue ? edtEnabledUPS.IsChecked.Value : false;
            string community = edtCommunity.Text;

            if (this.item == null)
            { //create
                Device d = new Device(edtName.Text, edtSource.Text, edtUPSSource.Text, enabledAnalogue, enabledDigital, enabledUps, upsType, community);
                Monitor.Instance.Devices.Add(d);                
            }
            else
            { // edit                
                int ind = Monitor.Instance.Devices.IndexOf(this.item);
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Name = edtName.Text;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Host = edtSource.Text;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).UpsHost = edtUPSSource.Text;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).DeviceEnabledAnalogue = enabledAnalogue;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).DeviceEnabledDigital = enabledDigital;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).DeviceEnabledUPS = enabledUps;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).UpsType = (UPSType)upsType;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Community = edtCommunity.Text;

            }
            Monitor.Instance.SaveDevices();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            edtEnabledUPS.IsChecked = false;
            if (this.item != null)
            {
                edtName.Text = item.Name;
                edtSource.Text = item.Host;
                edtUPSSource.Text = item.UpsHost;
                edtEnabledAnalogue.IsChecked = item.DeviceEnabledAnalogue;
                edtEnabledDigital.IsChecked = item.DeviceEnabledDigital;
                edtEnabledUPS.IsChecked = item.DeviceEnabledUPS;
                edtUPSType.Text = ((int)item.UpsType).ToString();
                edtCommunity.Text = item.Community;
            }
            CheckUpsState();
        }

        private void edtEnabledUPS_Checked(object sender, RoutedEventArgs e)
        {
            CheckUpsState();
        }
        void CheckUpsState()
        {
            if (edtEnabledUPS.IsChecked.Value)
            {
                edtUPSSource.IsEnabled = true;
            }
            else
            {
                edtUPSSource.IsEnabled = false;
            }
        }

        private void edtEnabledUPS_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUpsState();
        }
    }
}
