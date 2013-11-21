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
            string hostIP = "";
            string hostIPUps = "";
            IPAddress ipTmp;
            // HOST IP
            if (edtEnabledAnalogue.IsChecked.Value || edtEnabledDigital.IsChecked.Value)
            {
                hostIP = edtSource.Text;
                if (!IPAddress.TryParse(hostIP, out ipTmp))
                {
                    MessageBox.Show("Ошибка формата IP-адреса сервера телесканеров!");
                    return;
                }
                
            }
            // UPS HOST IP
            int upsType = 0;
            if (edtEnabledUPS.IsChecked.Value)
            {
                hostIPUps = edtUPSSource.Text;
                if (!IPAddress.TryParse(hostIPUps, out ipTmp))
                {
                    MessageBox.Show("Ошибка формата IP-адреса ИБП!");
                    return;
                }
                upsType = edtUPSType.SelectedIndex;
            }

            bool enabledAnalogue = edtEnabledAnalogue.IsChecked.HasValue ? edtEnabledAnalogue.IsChecked.Value : false;
            bool enabledDigital = edtEnabledDigital.IsChecked.HasValue ? edtEnabledDigital.IsChecked.Value : false;
            bool enabledUps = edtEnabledUPS.IsChecked.HasValue ? edtEnabledUPS.IsChecked.Value : false;
            string community = edtCommunity.Text;

            if (this.item == null)
            { //create
                Device d = new Device(edtName.Text, hostIP, edtUPSSource.Text, enabledAnalogue, enabledDigital, enabledUps, upsType, community);
                Monitor.Instance.Devices.Add(d);                
            }
            else
            { // edit
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Name = edtName.Text;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).Host = hostIP;
                Monitor.Instance.Devices.FirstOrDefault(d => d.ID == item.ID).UpsHost = hostIPUps;
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
                edtUPSType.SelectedIndex = (int)item.UpsType;
                edtCommunity.Text = item.Community;
            }
            CheckTSState();
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
                edtUPSType.IsEnabled = true;
                edtCommunity.IsEnabled = true;
            }
            else
            {
                edtUPSSource.IsEnabled = false;
                edtUPSType.IsEnabled = false;
                edtCommunity.IsEnabled = false;
            }
        }
        void CheckTSState()
        {
            bool aEn = edtEnabledAnalogue.IsChecked.HasValue ? edtEnabledAnalogue.IsChecked.Value : false;
            bool dEn = edtEnabledDigital.IsChecked.HasValue ? edtEnabledDigital.IsChecked.Value : false;
            bool tsIsEnabled = aEn || dEn;
            if (tsIsEnabled)
            {
                edtSource.IsEnabled = true;
            }
            else
            {
                edtSource.IsEnabled = false;
            }
        }

        private void edtEnabledUPS_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUpsState();
        }

        private void edtEnabledAnalogue_Checked(object sender, RoutedEventArgs e)
        {
            CheckTSState();
        }

        private void edtEnabledDigital_Checked(object sender, RoutedEventArgs e)
        {
            CheckTSState();
        }

        private void edtEnabledAnalogue_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckTSState();
        }

        private void edtEnabledDigital_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckTSState();
        }
    }
}
