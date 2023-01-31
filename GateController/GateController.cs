using GateController.Models;
using GateController.Repository;
using GateController.Utility;
using Symbol.RFID3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace GateController
{
    public partial class GateController : ServiceBase
    {

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        List<GetParametersResponse> data;
        public static bool isRunning = false;

        private static void fetchTriggerParams(DeviceParameters item)
        {
            if (null == item.m_TriggerInfo)
            {
                item.m_TriggerInfo = new TriggerInfo();
            }
        }

        public static Symbol.RFID3.AntennaInfo getInfo(DeviceParameters item)
        {
            return item.m_AntennaList;
        }

        public static Symbol.RFID3.TriggerInfo getTriggerInfo(DeviceParameters item)
        {
            if (null == item.m_TriggerInfo)
            {
                item.m_TriggerInfo = new TriggerInfo();
                fetchTriggerParams(item);
            }

            return item.m_TriggerInfo;
        }

        public static Symbol.RFID3.PostFilter getFilter(DeviceParameters item)
        {
            return item.m_PostFilter;
        }



        private int eventId = 1;

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        public GateController()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("GateController"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "GateController", "GateControllerLog");
            }
            eventLog1.Source = "GateController";
            eventLog1.Log = "GateControllerLog";
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("In OnStart.");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            data = new List<GetParametersResponse>();
            isRunning = false;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

        }

        public void CheckConnection(object obj)
        {
            string connStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
            DeviceParameters item = (DeviceParameters)obj;
            TcpClient tc = null;
            try
            {
                tc = new TcpClient(item.IpAddress, int.Parse(item.Port));
                DbClientFactory<PoolUserDbClient>.Instance.UpdateDeviceStatus(obj,true, connStr, eventLog1);
            }
            catch (SocketException se)
            {
                DbClientFactory<PoolUserDbClient>.Instance.UpdateDeviceStatus(obj, false, connStr, eventLog1);
                eventLog1.WriteEntry(item.IpAddress + " Cihaz bağlanamıyor... Hata: " + se.Message + "-" + se.InnerException);
                item.upConnection = false;

            }
            finally
            {

                if (tc != null)
                {
                    tc.Close();
                }


            }
        }
        public void GetRFID(object obj)
        {
            DeviceParameters item = (DeviceParameters)obj;
            bool bAlarmed = false;
            item.Executing = true;

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
                int relativeDistance;
                if (item.m_ReaderAPI==null || !item.upConnection)
                {
                    item.m_ReaderAPI = new RFIDReader(item.IpAddress, uint.Parse(item.Port), 0);
                    Thread.Sleep(500);
                    item.m_ReaderAPI.Connect();
                    Thread.Sleep(150);
                    item.m_ReaderAPI.Actions.PurgeTags();
                    item.m_ReaderAPI.Actions.Inventory.Perform(
                        getFilter(item),
                        getTriggerInfo(item),
                        getInfo(item));
                    bAlarmed = true;
                    item.upConnection = true;
                }

                if (bAlarmed)
                {
                    item.m_ReaderAPI.Config.GPO[1].PortState = GPOs.GPO_PORT_STATE.FALSE;
                    eventLog1.WriteEntry(item.IpAddress + " Cihaz üstündeki alarm kapatıldı");
                } else SetAlarm(item);

                eventLog1.WriteEntry("Cihaz Bilgisi: "+item.IpAddress+"-"+item.Port.ToString());

                eventLog1.WriteEntry(item.IpAddress + " Cihazdan RFID bilgileri alınıyor...");
                Thread.Sleep(200);

                List<DeviceTransactionInFo> infos = new List<DeviceTransactionInFo>();
                Symbol.RFID3.TagData[] tagData = item.m_ReaderAPI.Actions.GetReadTags(1000);
                if (tagData != null)
                {

                    eventLog1.WriteEntry(item.IpAddress+" nolu Anten Etiket sayısı:" +tagData.Length.ToString());

                    for (int nIndex = 0; nIndex < tagData.Length; nIndex++)
                    {
                        if (tagData[nIndex].ContainsLocationInfo)
                        {
                            relativeDistance = tagData[nIndex].LocationInfo.RelativeDistance;
                        }

                        if (tagData[nIndex].OpCode == ACCESS_OPERATION_CODE.ACCESS_OPERATION_NONE ||
                            (tagData[nIndex].OpCode == ACCESS_OPERATION_CODE.ACCESS_OPERATION_READ &&
                            tagData[nIndex].OpStatus == ACCESS_OPERATION_STATUS.ACCESS_SUCCESS))
                        {
                            Symbol.RFID3.TagData tag = tagData[nIndex];
                            DeviceTransactionInFo info = new DeviceTransactionInFo();
                            DeviceTransactionInFo filtered = infos.Find(x => x.TagID == tag.TagID);
                            if (filtered == null)
                            {
                                info.TagID = tag.TagID;
                                info.IpAddress = item.IpAddress;
                                info.GateCode = item.GateCode;
                                infos.Add(info);
                            }
                        }
                    }

                    if (infos.Count > 0)
                    {
                        eventLog1.WriteEntry(item.IpAddress + " Cihaz RFID bilgisi kontrol ediliyor....");
                        string result = DbClientFactory<PoolUserDbClient>.Instance.PostParameterInfo(infos, connStr, eventLog1);
                    }
                }
                else
                    eventLog1.WriteEntry(item.IpAddress + " Cihaz RFID bulunamadı...");

                SetAlarm(item);
                item.Executing = false;
            }
            catch (Exception ex)
            {
               
                eventLog1.WriteEntry(item.IpAddress + " Sistem Hatası..." + ex.Message + "-" + ex.InnerException, EventLogEntryType.Information);
                item.Executing = false;
     
            }

        }


        public void SetAlarm(object obj)
        {
            DeviceParameters item = (DeviceParameters)obj;
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
                for (int index = 0; index < item.m_ReaderAPI.ReaderCapabilities.NumGPOPorts; index++)
                {


                    List<GetAlarmInfoResponse> responseList = DbClientFactory<PoolUserDbClient>.Instance.GetAlarmInfo(item, connStr);
                    bool isanyAlarmed = false;
                    foreach (var response in responseList)
                    {

                        if (item.m_ReaderAPI.HostName == response.IpAddress && item.m_ReaderAPI.Port == uint.Parse(item.Port) && response.Alarmed)
                        {
                            item.m_ReaderAPI.Config.GPO[1].PortState = GPOs.GPO_PORT_STATE.TRUE;
                            isanyAlarmed = true;
                        }

                    }

                    if (!isanyAlarmed && item.m_ReaderAPI.HostName == item.IpAddress && item.m_ReaderAPI.Port == uint.Parse(item.Port))
                        item.m_ReaderAPI.Config.GPO[1].PortState = GPOs.GPO_PORT_STATE.FALSE;

                }
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(item.IpAddress + " Cihaz alarm bağlantı hatası ...." + ex.Message, EventLogEntryType.Information);
            }
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (!isRunning)
                {
                    isRunning = true;
                    bool blSqlConnection = false;
                    string connStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
                    SqlParameter[] param = null;
                    try
                    {
                        if (!blSqlConnection)
                        {
                            data = DbClientFactory<PoolUserDbClient>.Instance.GetParameters(connStr, param);
                            blSqlConnection = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        isRunning = false;
                        blSqlConnection = false;
                        eventLog1.WriteEntry(" SQL server bağlantı hatası sonrası hata oluştu. Hata: " + ex.Message + "-" + ex.InnerException);
                    }
                }

                foreach (var item in data)
                {

                    try
                    {
                        if (!item.Executing)
                        {
                            Thread thread3 = new Thread(CheckConnection);
                            thread3.Start(item);

                            Thread thread = new Thread(GetRFID);
                            thread.Start(item);
                        }

                    }
                    catch { }

                }


                eventLog1.WriteEntry("Sistem izleniyor...", EventLogEntryType.Information, eventId++);
            }
            catch (Exception ex)
            {
                isRunning = false;
                eventLog1.WriteEntry("Servis hata..." + ex.Message + "-" + ex.InnerException, EventLogEntryType.Information, eventId++);
            }



        }

    }
}
