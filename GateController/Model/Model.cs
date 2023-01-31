using Symbol.RFID3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GateController.Models
{

    public class DeviceParameters
    {
        public string GateCode { get; set; }

        public string IpAddress { get; set; }

        public string Port { get; set; }

        public string Status { get; set; }

        public bool Executing { get; set; }

        public RFIDReader m_ReaderAPI = null;

        public Symbol.RFID3.PostFilter m_PostFilter = null;

        public Symbol.RFID3.AntennaInfo m_AntennaList = null;

        public Symbol.RFID3.TriggerInfo m_TriggerInfo = null;

        public bool upConnection = false;

    }

    public class GetParametersResponse : DeviceParameters { }


    public class GetAlarmInfoResponse
    {
        public string IpAddress { get; set; }
        public string RFTagId { get; set; }
        public bool Alarmed { get; set; }

    }

    public class ProcessGetItemsRequest
    {
        public string RFID { get; set; }
        public bool Alarmed { get; set; }

    }



    public class ProcessGetItemsResponse : ProcessGetItemsRequest
    { }


    public class DeviceTransactionInFo
    {
        public string IpAddress { get; set; }

        public string GateCode { get; set; }

        public string TagID { get; set; }
    }



}