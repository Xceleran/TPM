using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using TPM.AHSServices;

namespace TPM.Processors
{
    public class AhsStatusProcessor
    {
        public bool SentAhsStatus(string DispatchId,
            string StatusCode,
            string sTimestamp,
            string companyid,
            string ApptID,
            string TPName)
        {

            bool result = false;
            try
            {
                DispatchStatusUpdateSoapClient AHSClient = new DispatchStatusUpdateSoapClient();
                AHSClient.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["AHSUser"].ToString();
                AHSClient.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["AHSPassword"].ToString();
                DispatchStatusUpdateInformation AHSRetVal;


                DataTable dt = new DataTable();
                Database db = new Database();

                string strSQL = @"INSERT INTO [msSchedulerV3].dbo.[tbl_StatusBuffer]
                                (CompanyID,ApptID
                                   ,DispatchID
                                   ,StatusCode
                                   ,SentDate
                                   ,IsApproved
                                   ,ErrorMsg
                                   ,TPName)
                                VALUES (@CompanyID, @ApptID, @DispatchID, @StatusCode, @SentDate, @IsApproved, @ErrorMsg, @TPName);";

                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@ApptID", ApptID, SqlDbType.NVarChar);
                db.AddParameter("@DispatchID", DispatchId, SqlDbType.NVarChar);
                db.AddParameter("@StatusCode", StatusCode, SqlDbType.NVarChar);
                db.AddParameter("@SentDate", sTimestamp, SqlDbType.NVarChar);
                db.AddParameter("@TPName", TPName, SqlDbType.NVarChar);




                try
                {




                    AHSRetVal = AHSClient.updateDispatchStatus(DispatchId, StatusCode, sTimestamp, "EST");

                    if (AHSRetVal.DispatchStatusUpdate.Equals("SUCCESSFUL", StringComparison.OrdinalIgnoreCase))
                    {

                        db.AddParameter("@ErrorMsg", "", SqlDbType.NVarChar);
                        db.AddParameter("@IsApproved", "1", SqlDbType.NVarChar);
                        result= true;
                    }
                    else
                    {
                        db.AddParameter("@ErrorMsg", AHSRetVal.ErrorMessage, SqlDbType.NVarChar);
                        db.AddParameter("@IsApproved", "0", SqlDbType.NVarChar);
                        result =false;
                    }
                     db.ExecuteScalarData(strSQL);

                    return result;

                }
                catch (Exception ex)
                {
                    return false;

                }





            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
        private DateTime UtcToEstern(DateTime value)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, "UTC", "Eastern Standard Time");
        }
    }
}