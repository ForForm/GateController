using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using GateController.Models;
using GateController.Utility;
using GateController.Mapper;
using System.Diagnostics;

namespace GateController.Repository
{
    public class PoolUserDbClient
    {

        public List<GetParametersResponse> GetParameters(string connString, SqlParameter[] param)
        {
            return SqlHelper.ExecuteProcedureReturnData<List<GetParametersResponse>>(connString, "ControlGate_SELECT", r => r.GetParametersMapper(), param);
        }

        public void UpdateDeviceStatus(object obj, bool status, string connString, EventLog eventLog1)
        {
            DeviceParameters item = (DeviceParameters)obj;
            SqlParameter[] param ={
                    new SqlParameter("@IpAddress",item.IpAddress),
                    new SqlParameter("@IsConnected",status)
                       };
            SqlHelper.ExecuteProcedureReturnString(connString, "LogGateStatus_UPDATE", param);
        }


        public string PostParameterInfo(List<DeviceTransactionInFo> infos, string connString, EventLog eventLog1)
        {
            try
            {
                WhsService.TWhsWebServiceSoapClient service = new WhsService.TWhsWebServiceSoapClient("TWhsWebServiceSoap12");
                List<WhsService.TGateControlRequest> requestList = new List<WhsService.TGateControlRequest>();
                WhsService.TGateControlRequest request = new WhsService.TGateControlRequest();
                foreach (var item in infos)
                {
                    request.RFTagID = item.TagID;
                    request.Alarmed = false;
                    request.IpAddress = item.IpAddress;
                    request.GateCode = item.GateCode;
                    requestList.Add(request);
                }

                

                WhsService.TGateControlResponse[] response = service.D_ProcessControlGates(requestList.ToArray());
                string result = string.Empty;


                for (int i = 0; i < response.Length; i++)
                {
                    var outParam = new SqlParameter("@ReturnCode", SqlDbType.NVarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };
                    

                    SqlParameter[] param ={
                    new SqlParameter("@GateCode",response[i].GateCode),
                    new SqlParameter("@IpAddress",response[i].IpAddress),
                    new SqlParameter("@RFTagID",response[i].RFTagID),
                    new SqlParameter("@ErrorDesc",response[i].ErrorDesc),
                    new SqlParameter("@Alarmed",response[i].Alarmed),
                    outParam
                       };


                    SqlHelper.ExecuteProcedureReturnString(connString, "LogGateTransactions_INSERT", param);
                    result = (string)outParam.Value;
                }

                eventLog1.WriteEntry(requestList.ToArray()[0].IpAddress + " Cihaz RFID bilgisi kontrol edildi. Cevap Kodu: " + result + " Başarılı.");
                return result;

            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(" Host web servis hata: "+ infos.ToArray()[0].IpAddress+"-" + ex.Message);
                return string.Empty;
            }
        }

        public List<GetAlarmInfoResponse> GetAlarmInfo(DeviceParameters info, string connString)
        {
            string result = string.Empty;
            SqlParameter[] param ={
            new SqlParameter("@IpAddress",info.IpAddress)};
            return SqlHelper.ExecuteProcedureReturnData<List<GetAlarmInfoResponse>>(connString, "LogGateAlerts_SELECT", r => r.GetAlarmInfoMapper(), param);
        }

    }

}
