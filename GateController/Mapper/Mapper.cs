using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using GateController.Models;
using GateController.Utility;

namespace GateController.Mapper
{
    public static class Mapper
    {
        public static List<GetParametersResponse> GetParametersMapper(this SqlDataReader reader)
        {
            List<GetParametersResponse> list = new List<GetParametersResponse>();
            while (reader.Read())
            {
                GetParametersResponse item = new GetParametersResponse();
                item.GateCode = SqlHelper.GetNullableString(reader, "GateCode");
                item.IpAddress = SqlHelper.GetNullableString(reader, "IpAddress");
                item.Port = SqlHelper.GetNullableString(reader, "Port");
                item.Status = SqlHelper.GetNullableString(reader, "Status");
                list.Add(item);
            }

            return list ;
        }

        public static List<GetAlarmInfoResponse> GetAlarmInfoMapper(this SqlDataReader reader)
        {
            List<GetAlarmInfoResponse> list = new List<GetAlarmInfoResponse>();
            while (reader.Read())
            {
                GetAlarmInfoResponse item = new GetAlarmInfoResponse();
                item.IpAddress = SqlHelper.GetNullableString(reader, "IpAddress");
                item.RFTagId = SqlHelper.GetNullableString(reader, "RFTagId");
                item.Alarmed = SqlHelper.GetBoolean(reader, "Alarmed");
                list.Add(item);
            }

            return list;
        }


    }
}