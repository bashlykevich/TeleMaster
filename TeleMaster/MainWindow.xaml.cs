using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeleMaster.View;
using TeleMaster.Management;
using TeleMaster.DAO;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using SnmpSharpNet;

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
        BackgroundWorker bwSnmpTrapListener = new BackgroundWorker();
       
        #region Одноразовые функции        
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
        void DisbleAllUPS()
        {
            foreach (Device d in Monitor.Instance.Devices)
                d.DeviceEnabledUPS = false;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            
            bwDevices.WorkerSupportsCancellation = true;
            bwUpdateUI.WorkerSupportsCancellation = true;
            bwSnmpTrapListener.WorkerSupportsCancellation = true;

            bwDevices.DoWork += new DoWorkEventHandler(CheckDeviceEvents);
            bwUpdateUI.DoWork += new DoWorkEventHandler(bwUpdated_DoWork);
            bwSnmpTrapListener.DoWork += new DoWorkEventHandler(bwSnmpTrapListener_DoWork);
            //bwSnmpAsker.DoWork += new DoWorkEventHandler(bwSnmpAsker_DoWork);
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDevices_ListView();
            RefreshDevices();
            //DisplayIcons(false);
            //StartMonitor();
            ////SendSnmpRequest(Monitor.Instance.Devices.FirstOrDefault());
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

        void RefreshDevices_IconView()
        {
            lsDisplay.DataContext = null;
            lsDisplay.DataContext = Monitor.Instance.Devices.OrderBy(d => d.Name);
        }
        void RefreshDevices_ListView()
        {
            lsDisplayList.DataContext = null;
            lsDisplayList.DataContext = Monitor.Instance.Devices.OrderBy(d => d.Host);
        }
        void RefreshDevices()
        {
            if (iconsView)
            {
                RefreshDevices_IconView();
            }
            else
            {
                RefreshDevices_ListView();
            }
        }
        
        void RefreshEvents()
        {
            lvDeviceLog.DataContext = null;
            lvDeviceLog.DataContext = Monitor.Instance.Events;
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
                    Monitor.Instance.AddEvent(device, message, eventType);
                    newIndex++;
                }
                sr.Close();
            }
            return newIndex;
        }
        
        void VerifyEvent(Event ev)
        {
            Guid deviceID = ev.DeviceID;
            Monitor.Instance.Events.Remove(ev);
            if (!Monitor.Instance.Events.Exists(e => e.DeviceID == deviceID))
            {
                if (Monitor.Instance.Devices.Exists(d => d.ID == deviceID))
                {                    
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
        Device selectedDevice = null;

        #region HANDLERS
        private void cmDevices_Events_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedDevice == null)
                return;
            ShowDeviceEventsHistory(this.selectedDevice);
        }
        private void cmDevices_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lsDisplay.SelectedItem == null)
                return;
            DeleteDevice(lsDisplay.SelectedItem as Device);
        }
        private void cmDevices_VerifyEvents_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedDevice == null)
                return;
            VerifyAllEvents(this.selectedDevice);
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
            if(!bwSnmpTrapListener.IsBusy)
                bwSnmpTrapListener.RunWorkerAsync();

            //StartTrapListener();
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
            bwSnmpTrapListener.CancelAsync();            

            stTime.Text = "Остановлено";
            Monitor.Instance.Events.Add(new Event("МОНИТОРИНГ ОСТАНОВЛЕН"));
            RefreshEvents();
        }

        private void lsDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null)
                return;
            if (lsDisplay.SelectedItem == null)
                return;            
            this.selectedDevice = (lsDisplay.SelectedItem as Device);
        }

        private void cmDevices_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedDevice == null)
                return;
            ConnectRDP(this.selectedDevice);
        }

        void ConnectRDP(Device device)
        {            
            Process rdcProcess = new Process();
            rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
            rdcProcess.StartInfo.Arguments = "/generic:TERMSRV/" + device.Host + " /user:" + device.RDPUser + " /pass:" + device.RDPPassword;
            rdcProcess.Start();

            rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
            rdcProcess.StartInfo.Arguments = "/v " + device.Host; // ip or name of computer to connect
            rdcProcess.Start();            
        }

        bool iconsView = true;
        void DisplayIcons(bool display)
        {
            iconsView = display;
            if (display)
            {
                lsDisplay.Visibility = System.Windows.Visibility.Visible;
                lsDisplayList.Visibility = System.Windows.Visibility.Collapsed;

                miViewIcons.IsChecked = true;
                miViewList.IsChecked = false;
            }
            else
            {
                lsDisplay.Visibility = System.Windows.Visibility.Collapsed;
                lsDisplayList.Visibility = System.Windows.Visibility.Visible;

                miViewIcons.IsChecked = false;
                miViewList.IsChecked = true;
            }
        }
        private void miViewIcons_Click(object sender, RoutedEventArgs e)
        {
            DisplayIcons(true);
        }

        private void miViewList_Click(object sender, RoutedEventArgs e)
        {
            DisplayIcons(false);
        }

        private void lsDisplayList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(lsDisplayList.SelectedItem == null)
                return;
            EditDevice(lsDisplayList.SelectedItem as Device);
        }

        #region Задачи в фоне
        
        void bwSnmpTrapListener_DoWork(object sender, DoWorkEventArgs e)
        {
            // Construct a socket and bind it to the trap manager port 162 
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 162);
            EndPoint ep = (EndPoint)ipep;
            socket.Bind(ep);
            // Disable timeout processing. Just block until packet is received 
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            bool run = true;
            //edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run"); })); //);
            while (run)
            {
                ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run"); })); //);
                if (this.bwSnmpTrapListener.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run1"); })); //);
                byte[] indata = new byte[16 * 1024];
                // 16KB receive buffer 
                int inlen = 0;
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                EndPoint inep = (EndPoint)peer;
                ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run2"); }));
                try
                {
                    ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run3"); }));
                    inlen = socket.ReceiveFrom(indata, ref inep);
                    ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run4"); }));
                }
                catch (Exception ex)
                {
                    Monitor.Instance.AddEvent(ex.Message, EventType.ИБП);
                    ///edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add(ex.Message);}));
                    //edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("Exception " + + "", ex.Message);
                    inlen = -1;
                }
                //edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("run5"); }));
                if (inlen > 0)
                {
                    // Check protocol version 
                    int ver = SnmpPacket.GetProtocolVersion(indata, inlen);
                    if (ver == (int)SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet 
                        SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
                        pkt.decode(indata, inlen);
                        string trMessage = inep.ToString() + "\t";
                        /*trMessage += "*** Trap generic: " + pkt.Pdu.Generic + "";
                        trMessage += "*** Trap specific: " + pkt.Pdu.Specific + "";
                        trMessage += "*** Agent address: " + pkt.Pdu.AgentAddress.ToString() + "";
                        trMessage += "*** Timestamp: " + pkt.Pdu.TimeStamp.ToString() + "";
                        trMessage += "*** VarBind count: " + pkt.Pdu.VbList.Count + "";
                        trMessage += "*** VarBind content:";*/
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("** SNMP Version 1 TRAP received from " + inep.ToString() + ":"); })); //, inep.ToString());
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** Trap generic: " + pkt.Pdu.Generic + ""); })); //, pkt.Pdu.Generic);
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** Trap specific: " + pkt.Pdu.Specific + ""); })); //, pkt.Pdu.Specific);
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** Agent address: " + pkt.Pdu.AgentAddress.ToString() + ""); })); //, pkt.Pdu.AgentAddress.ToString());
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** Timestamp: " + pkt.Pdu.TimeStamp.ToString() + ""); })); //, pkt.Pdu.TimeStamp.ToString());
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** VarBind count: " + pkt.Pdu.VbList.Count + ""); })); //, pkt.Pdu.VbList.Count);
                        ///edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("*** VarBind content:");})); //);

                        foreach (Vb v in pkt.Pdu.VbList)
                        {
                            trMessage += " Message: " + v.Oid.ToString() + " " + SnmpConstants.GetTypeName(v.Value.Type) + ": " + v.Value.ToString() + "\t";
                            /*edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("**** " 
                                + v.Oid.ToString() + " "
                                + SnmpConstants.GetTypeName(v.Value.Type) + ": "
                                + v.Value.ToString() + "");
                            }));*/
                            //, v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                        }
                        //edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("** End of SNMP Version 1 TRAP data.");})); //);
                        if(!inep.ToString().Contains("1.80"))
                            Monitor.Instance.AddEvent(trMessage, EventType.ИБП);
                    }
                    else
                    {
                        // Parse SNMP Version 2 TRAP packet 
                        SnmpV2Packet pkt = new SnmpV2Packet();
                        pkt.decode(indata, inlen);
                        string trMessage = inep.ToString() + "\t";
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("** SNMP Version 2 TRAP received from " + inep.ToString() + ":"); })); //, inep.ToString());
                        if (pkt.Pdu.Type != PduType.V2Trap)
                        {
                            ///edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("*** NOT an SNMPv2 trap ****");})); //);
                        }
                        else
                        {
                            /*trMessage +="*** Community: " + pkt.Community.ToString() + "";
                            trMessage +="*** VarBind count: " + pkt.Pdu.VbList.Count + "";
                            trMessage +="*** VarBind content:";*/
                            ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** Community: " + pkt.Community.ToString() + ""); })); //, pkt.Community.ToString());
                            ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("*** VarBind count: " + pkt.Pdu.VbList.Count + ""); })); //, pkt.Pdu.VbList.Count);
                            ///edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("*** VarBind content:");})); //);
                            foreach (Vb v in pkt.Pdu.VbList)
                            {
                                trMessage += " Message " + v.Oid.ToString() + " " + SnmpConstants.GetTypeName(v.Value.Type) + ": " + v.Value.ToString() + "";
                                /*edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("**** "
                                    + v.Oid.ToString() + " "
                                    + SnmpConstants.GetTypeName(v.Value.Type) + ": "
                                    + v.Value.ToString() + "");
                                })); */
                                //,
                                //v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                            }
                            //edtTraps.Dispatcher.Invoke(new Action(() => {edtTraps.Items.Add("** End of SNMP Version 2 TRAP data.");})); //);
                            if (!inep.ToString().Contains("1.80"))
                                Monitor.Instance.AddEvent(trMessage, EventType.ИБП);
                        }
                    }
                }
                else
                {
                    if (inlen == 0)
                    {
                        Monitor.Instance.AddEvent("Zero length packet received.", EventType.ИБП);
                        ///edtTraps.Dispatcher.Invoke(new Action(() => { edtTraps.Items.Add("Zero length packet received."); })); //);
                    }
                }
            }
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
                    RefreshDevices();
                }));
                lvDeviceLog.Dispatcher.Invoke(new Action(() =>
                {
                    RefreshEvents();
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

        void CheckDeviceEvents(object sender, DoWorkEventArgs e)
        {
            int updateInterval = Monitor.Instance.UpdateInterval; // milliseconds
            while (true)
            {
                if (this.bwDevices.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                foreach (Device device in Monitor.Instance.Devices)
                {
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
            GetUPSInfo(device);
        }
        int DisconnectInterval = 5;
        void GetNewEvents(Device device)
        {
            if (!IpAddress.IsIP(device.Host) || device.Host == "0.0.0.0" || String.IsNullOrEmpty(device.Host))
            {
                device.IsDisconnected = true;
                return;
            }
            // ping server
            Ping p = new Ping();            
            PingReply reply = p.Send(device.Host);
            if (reply.Status != IPStatus.Success)
            {
                if (!device.IsDisconnected)
                {
                    TimeSpan ts = DateTime.Now - device.LastOnlineTimeTS;
                    if (ts.TotalSeconds > DisconnectInterval)
                    {
                        string message = "Адрес " + device.Host + " недоступен!";
                        Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                        device.IsDisconnected = true;
                    }
                }
                return;
            }
            // если был отключен, а теперь появился
            if (device.IsDisconnected)
            {
                string message = "Соединение восстановлено.";
                Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                device.IsDisconnected = false;
            }
            
            if (device.DeviceEnabledAnalogue)
            {
                string dir = "logs_a";
                // проверка доступности директории с логами
                string dirPath = @"\\" + device.Host + @"\" + dir;
                if (!Directory.Exists(dirPath))
                {
                    if (!device.IsDisconnected)
                    {
                        TimeSpan ts = DateTime.Now - device.LastOnlineTimeTS;
                        if (ts.TotalSeconds > DisconnectInterval)
                        {
                            string message = "Лог-файл по адресу " + dirPath + " недоступен!";
                            Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                            device.IsDisconnected = true;
                        }
                    }
                    return;
                }
                if (device.LastReadDateForAnalogue.Date != DateTime.Now.Date)
                {
                    device.LastReadRowIndexForAnalogue = 0;
                    device.LastReadDateForAnalogue = DateTime.Now.Date;
                }

                device.LastReadRowIndexForAnalogue = ReadRemoteLogFile(dir, device.LastReadRowIndexForAnalogue, device, EventType.Аналог);
            }
            if (device.DeviceEnabledDigital)
            {
                // проверка доступности директории с логами
                string dir = "logs_d";
                // проверка доступности директории с логами
                string dirPath = @"\\" + device.Host + @"\" + dir;
                if (!Directory.Exists(dirPath))
                {
                    if (!device.IsDisconnected)
                    {
                        TimeSpan ts = DateTime.Now - device.LastOnlineTimeTS;
                        if (ts.TotalSeconds > DisconnectInterval)
                        {
                            string message = "Лог-файл по адресу " + dirPath + " недоступен!";
                            Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                            device.IsDisconnected = true;
                        }
                    }
                    return;
                }
                if (device.LastReadDateForDigital.Date != DateTime.Now.Date)
                {
                    device.LastReadRowIndexForDigital = 0;
                    device.LastReadDateForDigital = DateTime.Now.Date;
                }
                device.LastReadRowIndexForDigital = ReadRemoteLogFile(dir, device.LastReadRowIndexForDigital, device, EventType.Цифра);
                device.LastOnlineTimeTS = DateTime.Now;
            }
        }
        void GetUPSInfo(Device device)
        {
            if (device.DeviceDisabledUPS)
                return;
            // ping server
            Ping p = new Ping();
            PingReply reply = p.Send(device.UpsHost);
            if (reply.Status != IPStatus.Success)
            {
                if (!device.DeviceUpsIsDisconnected)
                {
                    TimeSpan ts = DateTime.Now - device.LastOnlineTimeUps;
                    if(ts.TotalSeconds > 3)
                    {
                        string message = "ИБП по адресу " + device.Host + " недоступен!";
                        Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                        device.DeviceUpsIsDisconnected = true;
                    }
                }
                return;
            }
            // если был отключен, а теперь появился
            if (device.DeviceUpsIsDisconnected)
            {
                string message = "Соединение восстановлено.";
                Monitor.Instance.AddEvent(device, message, EventType.Сеть);
                device.DeviceUpsIsDisconnected = false;
            }
            device.LastOnlineTimeUps = DateTime.Now;

            // send snmp request
            SendSnmpRequest(device);
        }

        #endregion
        void SendSnmpRequest(Device device)
        {
            // SNMP community name
            OctetString community = new OctetString(device.Community);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1 (or 2)
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            //IpAddress agent = new IpAddress("127.0.0.1");
            IpAddress agent = new IpAddress(device.UpsHost);            


            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.Get);

            string oidBatteryCapacityRemaining = ".1.3.6.1.4.1.4555.1.1.1.1.2.4.0";
            string oidBatteryVoltage = ".1.3.6.1.4.1.4555.1.1.1.1.2.5.0";
            string oidOutputLoad = ".1.3.6.1.4.1.4555.1.1.1.1.4.4.1.4.1";
            string oidBatteryStatus = ".1.3.6.1.4.1.4555.1.1.1.1.2.1.0";

            switch(device.UpsType)
            {
                case UPSType.Socomec:
                    oidBatteryCapacityRemaining = ".1.3.6.1.4.1.4555.1.1.1.1.2.4.0";
                    oidBatteryVoltage = ".1.3.6.1.4.1.4555.1.1.1.1.2.5.0";
                    oidOutputLoad = ".1.3.6.1.4.1.4555.1.1.1.1.4.4.1.4.1";
                    oidBatteryStatus = ".1.3.6.1.4.1.4555.1.1.1.1.2.1.0";
                    break;
                case UPSType.AP9630:
                    oidBatteryCapacityRemaining = ".1.3.6.1.4.1.318.1.1.1.2.2.1.0";
                    oidBatteryVoltage = ".1.3.6.1.4.1.318.1.1.1.4.2.1.0";
                    oidOutputLoad = ".1.3.6.1.4.1.318.1.1.1.4.2.3.0";
                    oidBatteryStatus = ".1.3.6.1.4.1.318.1.1.1.2.1.1.0";
                    break;
            }
            pdu.VbList.Add(oidBatteryCapacityRemaining);
            pdu.VbList.Add(oidBatteryVoltage);
            pdu.VbList.Add(oidOutputLoad);
            pdu.VbList.Add(oidBatteryStatus);

            /*
            pdu.VbList.Add("1.3.6.1.2.1.1.1.0"); //sysDescr
            pdu.VbList.Add("1.3.6.1.2.1.1.2.0"); //sysObjectID
            pdu.VbList.Add("1.3.6.1.2.1.1.3.0"); //sysUpTime
            pdu.VbList.Add("1.3.6.1.2.1.1.4.0"); //sysContact
            pdu.VbList.Add("1.3.6.1.2.1.1.5.0"); //sysName
            */
            // Make SNMP request
            SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
            device.BatteryCapacityRemaining = "";
            device.BatteryStatus = "";
            device.BatteryVoltage = "";
            device.OutputLoad = "";
            // If result is null then agent didn't reply or we couldn't parse the reply.
            if (result != null)
            {
                // ErrorStatus other then 0 is an error returned by 
                // the Agent - see SnmpConstants for error definitions
                if (result.Pdu.ErrorStatus != 0)
                {
                    // agent reported an error with the request
                    /*edtLog.Items.Add("Error in SNMP reply. Error {0} index {1}",
                        result.Pdu.ErrorStatus,
                        result.Pdu.ErrorIndex);*/
                }
                else
                {
                    // Reply variables are returned in the same order as they were added
                    //  to the VbList
                    string bc = result.Pdu.VbList[0].Value.ToString();
                    if (String.IsNullOrEmpty(bc))
                        device.BatteryCapacityRemaining = "";
                    else
                        device.BatteryCapacityRemaining = "BC: " + bc + "%";

                    int bvi = Int32.Parse(result.Pdu.VbList[1].Value.ToString());
                    string bv = ((float)bvi/10).ToString();
                    if (String.IsNullOrEmpty(bv))
                        device.BatteryVoltage = "";
                    else
                        device.BatteryVoltage = "BV: " + bv + "V";
                    
                    string ol = result.Pdu.VbList[2].Value.ToString();
                    if(String.IsNullOrEmpty(ol))
                        device.OutputLoad = "";
                    else
                        device.OutputLoad = "OL: " + ol + "%";
                    
                    device.BatteryStatus = result.Pdu.VbList[3].Value.ToString();                    
                    /*edtLog.Items.Add("sysDescr({0}) ({1}): {2}",
                        result.Pdu.VbList[0].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[0].Value.Type),
                        result.Pdu.VbList[0].Value.ToString());
                    edtLog.Items.Add("sysObjectID({0}) ({1}): {2}",
                        result.Pdu.VbList[1].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[1].Value.Type),
                        result.Pdu.VbList[1].Value.ToString());
                    edtLog.Items.Add("sysUpTime({0}) ({1}): {2}",
                        result.Pdu.VbList[2].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[2].Value.Type),
                        result.Pdu.VbList[2].Value.ToString());
                    edtLog.Items.Add("sysContact({0}) ({1}): {2}",
                        result.Pdu.VbList[3].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[3].Value.Type),
                        result.Pdu.VbList[3].Value.ToString());
                    edtLog.Items.Add("sysName({0}) ({1}): {2}",
                        result.Pdu.VbList[4].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[4].Value.Type),
                        result.Pdu.VbList[4].Value.ToString());
                     */
                }
            }
            else
            {
                //edtLog.Items.Add("No response received from SNMP agent.");
            }
            target.Close();
        }
    }
}
