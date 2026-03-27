using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace TPM.Entity
{
    public class CommonClass
    {
        public string GenerateMeetingICS(DateTime start, DateTime end, string subject, string description, string location, string uid, string attendeeEmail, string organizerEmail)
        {
            //string organizerEmail = "noreply@yourcompany.com";  // replace with actual sender
            //string attendeeEmail = "client@example.com";        // optional: replace or pass in as param

            return "BEGIN:VCALENDAR\r\n" +
                   "PRODID:-//msScheduler//EN\r\n" +
                   "VERSION:2.0\r\n" +
                   "METHOD:REQUEST\r\n" +
                   "BEGIN:VEVENT\r\n" +
                   "UID:" + uid + "\r\n" +
                   "DTSTAMP:" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'") + "\r\n" +
                   "DTSTART:" + start.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'") + "\r\n" +
                   "DTEND:" + end.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'") + "\r\n" +
                   "SUMMARY:" + subject + "\r\n" +
                   "DESCRIPTION:" + description + "\r\n" +
                   "LOCATION:" + location + "\r\n" +
                   "STATUS:CONFIRMED\r\n" +
                   "SEQUENCE:0\r\n" +
                   "ORGANIZER;CN=Appointment Scheduler:MAILTO:" + organizerEmail + "\r\n" +
                   "ATTENDEE;CN=Client;RSVP=TRUE:MAILTO:" + attendeeEmail + "\r\n" +
                   "BEGIN:VALARM\r\n" +
                   "TRIGGER:-PT10M\r\n" +
                   "ACTION:DISPLAY\r\n" +
                   "DESCRIPTION:Reminder\r\n" +
                   "END:VALARM\r\n" +
                   "END:VEVENT\r\n" +
                   "END:VCALENDAR";
        }

        //Added By Munem
        public string GetGoogleCalenderLink(string startTimeString, string endTimeString)
        {
            string tzone = HttpContext.Current.Session["CurrentTimeZone"].ToString();

            CommonClass cct = new CommonClass();
            DateTime startTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(startTimeString), tzone);
            DateTime endTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(endTimeString), tzone);

            string gStarttime = startTimewithZone.ToString("yyyyMMdd'T'HHmmss");
            string gEndtime = endTimewithZone.ToString("yyyyMMdd'T'HHmmss");
            string google_calender_url = "https://calendar.google.com/calendar/u/0/r/eventedit?dates=" + gStarttime + "/" + gEndtime + "&text=Appointment+Confirmation";
            string google_calender_linkText = "Add this to my Google calendar";
            return $"<p> <a href=\"{google_calender_url}\">{google_calender_linkText}</a></p>";
        }

        public string GetOutlookLink(string startTimeString, string endTimeString)
        {
            string tzone = HttpContext.Current.Session["CurrentTimeZone"].ToString();

            CommonClass cct = new CommonClass();
            DateTime startTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(startTimeString), tzone);
            DateTime endTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(endTimeString), tzone);

            string oStarttime = startTimewithZone.ToString("yyyy-MM-dd'T'HH:mm:ss");
            string oEndtime = endTimewithZone.ToString("yyyy-MM-dd'T'HH:mm:ss");
            string outlook_url = "https://outlook.office.com/calendar/0/deeplink/compose?&enddt=" + oEndtime + "&startdt=" + oStarttime + "&subject=Appointment%20Confirmation";
            string outlook_linkText = "Add this to my OutLook calendar";
            return $"<p> <a href=\"{outlook_url}\">{outlook_linkText}</a></p>";
        }

        //End

        public DateTime ConvertToTimeZone(DateTime dt, string timeZoneAbbreviation)
        {
            DateTime utcConvertedTime = TimeZoneInfo.ConvertTimeToUtc(dt, TimeZoneInfo.Local);
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            return TimeZoneInfo.ConvertTimeFromUtc(utcConvertedTime, localTimeZone);
        }

        public string GetGoogleCalenderLink(string ApptId)
        {
            string tzone = HttpContext.Current.Session["CurrentTimeZone"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            string sql_Qry = @"SELECT [CompanyID],[StartDateTime],[EndDateTime] FROM [msSchedulerV3].[dbo].[tbl_Appointment]
                                    Where tbl_Appointment.ApptID='" + ApptId + "' and CompanyID='" + HttpContext.Current.Session["CompanyID"].ToString() + "'";
            DataSet dataSet = db.Get_DataSet(sql_Qry, ApptId);
            dt = dataSet.Tables[0];
            string startTimeString = string.Empty, endTimeString = string.Empty;
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                startTimeString = dr["StartDateTime"].ToString();
                endTimeString = dr["EndDateTime"].ToString();
            }

            CommonClass cct = new CommonClass();
            DateTime startTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(startTimeString), tzone);
            DateTime endTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(endTimeString), tzone);

            string gStarttime = startTimewithZone.ToString("yyyyMMdd'T'HHmmss");
            string gEndtime = endTimewithZone.ToString("yyyyMMdd'T'HHmmss");
            string google_calender_url = "https://calendar.google.com/calendar/u/0/r/eventedit?dates=" + gStarttime + "/" + gEndtime + "&text=Appointment+Confirmation";
            string google_calender_linkText = "Add this to my Google calendar";
            return $"<p> <a href=\"{google_calender_url}\">{google_calender_linkText}</a></p>";
        }
        public TwilioSetting GettwilioSetting()
        {
            TwilioSetting twilioSetting = new TwilioSetting();

            try
            {
                Database db = new Database();
                DataTable dt = new DataTable();
                string sql_Qry = @"SELECT  
                          [CompanyID]
                          ,[TwilioAccountSid]
                          ,[TwilioAccountAuthToken]
                          ,[TwilioPhoneNumber]
                      FROM [msSchedulerV3].[dbo].[tbl_TwilioSetting] Where CompanyID=@CompanyID;";
                DataSet dataSet = db.Get_DataSet(sql_Qry, HttpContext.Current.Session["CompanyID"].ToString());
                dt = dataSet.Tables[0];

                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    twilioSetting.CompanyID = dr["CompanyID"].ToString().Trim();
                    twilioSetting.TwilioAccountAuthToken = dr["TwilioAccountAuthToken"].ToString().Trim();
                    twilioSetting.TwilioAccountSid = dr["TwilioAccountSid"].ToString().Trim();
                    twilioSetting.TwilioPhoneNumber = dr["TwilioPhoneNumber"].ToString().Trim();

                }
            }
            catch (Exception ex) { }



            return twilioSetting;
        }

        public string GetOutlookLink(string ApptId)
        {
            string tzone = HttpContext.Current.Session["CurrentTimeZone"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            string sql_Qry = @"SELECT [CompanyID],[StartDateTime],[EndDateTime] FROM [msSchedulerV3].[dbo].[tbl_Appointment]
                                    Where tbl_Appointment.ApptID='" + ApptId + "' and CompanyID='" + HttpContext.Current.Session["CompanyID"].ToString() + "'";
            DataSet dataSet = db.Get_DataSet(sql_Qry, ApptId);
            dt = dataSet.Tables[0];
            string startTimeString = string.Empty, endTimeString = string.Empty;
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                startTimeString = dr["StartDateTime"].ToString();
                endTimeString = dr["EndDateTime"].ToString();
            }
            CommonClass cct = new CommonClass();
            DateTime startTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(startTimeString), tzone);
            DateTime endTimewithZone = cct.ConvertToTimeZone(Convert.ToDateTime(endTimeString), tzone);

            string oStarttime = startTimewithZone.ToString("yyyy-MM-dd'T'HH:mm:ss");
            string oEndtime = endTimewithZone.ToString("yyyy-MM-dd'T'HH:mm:ss");
            string outlook_url = "https://outlook.office.com/calendar/0/deeplink/compose?&enddt=" + oEndtime + "&startdt=" + oStarttime + "&subject=Appointment%20Confirmation";
            string outlook_linkText = "Add this to my OutLook calendar";
            return $"<p> <a href=\"{outlook_url}\">{outlook_linkText}</a></p>";
        }

        public bool IsFt_User()
        {
            bool Result = false;
            string session = HttpContext.Current.Session["LoginUser"].ToString();
            //if (session == "demoSchedulerUser@myserviceforce.com" || session == "cecpro@xinator.com" || session == "demousr@myserviceforce.com")
            //{
            //    Result = true;
            //}
            if (session == "demoSchedulerUser@myserviceforce.com" || session == "cecpro@xinator.com")
            {
                Result = true;
            }
            return Result;
        }

    
        public Int32 Get_TotalResource_ForCalender(String CompanyID)
        {
            Int32 TotalResource = 0;
            try
            {
                Database db = new Database();
                DataTable dt_ServiceType_Resource = new DataTable();
                string sql_Qry = @"SELECT        tbl_ServiceType.ServiceTypeID,tbl_ServiceType.CompanyID,tbl_ServiceType.Hour
                            FROM     msSchedulerV3.dbo.tbl_ServiceType INNER JOIN
                            msSchedulerV3.dbo.tbl_ServiceType_Resource ON
                            tbl_ServiceType.ServiceTypeID = tbl_ServiceType_Resource.ServiceTypeId
                            AND tbl_ServiceType.CompanyID = tbl_ServiceType_Resource.CompanyID
                            Where tbl_ServiceType_Resource.Companyid=  '" + CompanyID + "'";

                db.Execute(sql_Qry, out dt_ServiceType_Resource);
                DataTable dt_ServiceTime = new DataTable();

                string sSQL = "SELECT CompanyID,StartTime,StartTimeAMPM,EndTime,EndTimeAMPM,BlockSize FROM msSchedulerV3.dbo.tbl_ServiceTime Where CompanyID = '" + CompanyID + "'";
                db.Execute(sSQL, out dt_ServiceTime);

                DateTime dtDayStart = DateTime.Now;// Convert.ToDateTime(optStart.Value + " " + optAMPM1.Value);
                DateTime dtDayEnd = DateTime.Now; //Convert.ToDateTime(optEnd.Value + " " + optAMPM2.Value);
                int iBlockSize = 0;// Convert.ToInt32(optBlockSize.Value);

                Int32 TotalWorkHour = 0;
                if (dt_ServiceTime.Rows.Count > 0)
                {
                    try
                    {
                        DataRow dr = dt_ServiceTime.Rows[0];

                        dtDayStart = Convert.ToDateTime(dt_ServiceTime.Rows[0]["StartTime"] + " " + dt_ServiceTime.Rows[0]["StartTimeAMPM"]);
                        dtDayEnd = Convert.ToDateTime(dt_ServiceTime.Rows[0]["EndTime"] + " " + dt_ServiceTime.Rows[0]["EndTimeAMPM"]);
                        iBlockSize = Convert.ToInt32(dt_ServiceTime.Rows[0]["BlockSize"]);
                        TotalWorkHour = (dtDayEnd - dtDayStart).Hours;
                    }
                    catch { }
                }

                // db.ExecuteScalar(Sql);

                foreach (DataRow dataRow in dt_ServiceType_Resource.Rows)
                {
                    Int32 TimeRequired = Convert.ToInt32(dataRow["Hour"]);
                    TotalResource += TotalWorkHour / TimeRequired;
                }
            }
            catch
            { }
            return TotalResource;
        }

        public string getNextNumberForAppointmentId()
        {
            DataTable table;
            string New_Number = string.Empty;

            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
                Database db = new Database(connStr);

                string sql = @"SELECT
                          [AppointmentPrefix]
                          ,[AppointmentSeedNumber]
                      FROM [msSchedulerV3].[dbo].[tbl_AppointmentAutoGenerate]" +
                            " Where CompanyID= '" + CompanyID + "'";

                db.Execute(sql, out table);

                if (table.Rows.Count > 0)
                {

                    Int64 AppointmnetSeed = Convert.ToInt64(table.Rows[0]["AppointmentSeedNumber"]) + 1;
                    string AppointmentPrefix = table.Rows[0]["AppointmentPrefix"].ToString();

                    if (AppointmnetSeed < 10001)
                    {
                        AppointmnetSeed = 10001;
                    }

                    New_Number = string.Format("{0}-{1}-{2}", AppointmentPrefix, CompanyID, AppointmnetSeed);

                    sql = @"Update [msSchedulerV3].[dbo].[tbl_AppointmentAutoGenerate] set AppointmentSeedNumber=" + AppointmnetSeed + " Where CompanyID= '" + CompanyID + "'";

                    db.Execute(sql);

                }
                db.Close();

                return New_Number;
            }
            catch
            {
                return DateTime.Now.ToString("hmmss");
            }
        }
        public IEnumerable<States> ListOfStates()
        {
            var states = CreateStateList();
            return states.ToList();
        }

        public IEnumerable<States> ListOfProvience()
        {
            var states = CreateStateListCanada();
            return states.ToList();
        }

        private IList<States> CreateStateList()
        {
            List<States> states = new List<States>();

            states.Add(new States() { Abbreviations = "AL", Name = "Alabama" });
            states.Add(new States() { Abbreviations = "AK", Name = "Alaska" });
            states.Add(new States() { Abbreviations = "AR", Name = "Arkansas" });
            states.Add(new States() { Abbreviations = "AZ", Name = "Arizona" });
            states.Add(new States() { Abbreviations = "CA", Name = "California" });
            states.Add(new States() { Abbreviations = "CO", Name = "Colorado" });
            states.Add(new States() { Abbreviations = "CT", Name = "Connecticut" });
            states.Add(new States() { Abbreviations = "DC", Name = "District of Columbia" });
            states.Add(new States() { Abbreviations = "DE", Name = "Delaware" });
            states.Add(new States() { Abbreviations = "FL", Name = "Florida" });
            states.Add(new States() { Abbreviations = "GA", Name = "Georgia" });
            states.Add(new States() { Abbreviations = "HI", Name = "Hawaii" });
            states.Add(new States() { Abbreviations = "ID", Name = "Idaho" });
            states.Add(new States() { Abbreviations = "IL", Name = "Illinois" });
            states.Add(new States() { Abbreviations = "IN", Name = "Indiana" });
            states.Add(new States() { Abbreviations = "IA", Name = "Iowa" });
            states.Add(new States() { Abbreviations = "KS", Name = "Kansas" });
            states.Add(new States() { Abbreviations = "KY", Name = "Kentucky" });
            states.Add(new States() { Abbreviations = "LA", Name = "Louisiana" });
            states.Add(new States() { Abbreviations = "ME", Name = "Maine" });
            states.Add(new States() { Abbreviations = "MD", Name = "Maryland" });
            states.Add(new States() { Abbreviations = "MA", Name = "Massachusetts" });
            states.Add(new States() { Abbreviations = "MI", Name = "Michigan" });
            states.Add(new States() { Abbreviations = "MN", Name = "Minnesota" });
            states.Add(new States() { Abbreviations = "MS", Name = "Mississippi" });
            states.Add(new States() { Abbreviations = "MO", Name = "Missouri" });
            states.Add(new States() { Abbreviations = "MT", Name = "Montana" });
            states.Add(new States() { Abbreviations = "NE", Name = "Nebraska" });
            states.Add(new States() { Abbreviations = "NH", Name = "New Hampshire" });
            states.Add(new States() { Abbreviations = "NJ", Name = "New Jersey" });
            states.Add(new States() { Abbreviations = "NM", Name = "New Mexico" });
            states.Add(new States() { Abbreviations = "NY", Name = "New York" });
            states.Add(new States() { Abbreviations = "NC", Name = "North Carolina" });
            states.Add(new States() { Abbreviations = "NV", Name = "Nevada" });
            states.Add(new States() { Abbreviations = "ND", Name = "North Dakota" });
            states.Add(new States() { Abbreviations = "OH", Name = "Ohio" });
            states.Add(new States() { Abbreviations = "OK", Name = "Oklahoma" });
            states.Add(new States() { Abbreviations = "OR", Name = "Oregon" });
            states.Add(new States() { Abbreviations = "PA", Name = "Pennsylvania" });
            states.Add(new States() { Abbreviations = "RI", Name = "Rhode Island" });
            states.Add(new States() { Abbreviations = "SC", Name = "South Carolina" });
            states.Add(new States() { Abbreviations = "SD", Name = "South Dakota" });
            states.Add(new States() { Abbreviations = "TN", Name = "Tennessee" });
            states.Add(new States() { Abbreviations = "TX", Name = "Texas" });
            states.Add(new States() { Abbreviations = "UT", Name = "Utah" });
            states.Add(new States() { Abbreviations = "VT", Name = "Vermont" });
            states.Add(new States() { Abbreviations = "VA", Name = "Virginia" });
            states.Add(new States() { Abbreviations = "WA", Name = "Washington" });
            states.Add(new States() { Abbreviations = "WV", Name = "West Virginia" });
            states.Add(new States() { Abbreviations = "WI", Name = "Wisconsin" });
            states.Add(new States() { Abbreviations = "WY", Name = "Wyoming" });
            return states.ToList();
        }

        private IList<States> CreateStateListCanada()
        {
            List<States> states = new List<States>();

            states.Add(new States() { Abbreviations = "AB", Name = "Alberta" });
            states.Add(new States() { Abbreviations = "BC", Name = "British Columbia" });
            states.Add(new States() { Abbreviations = "MB", Name = "Manitoba" });
            states.Add(new States() { Abbreviations = "NB", Name = "New Brunswick" });
            states.Add(new States() { Abbreviations = "NL", Name = "Newfoundland and Labrador" });
            states.Add(new States() { Abbreviations = "NS", Name = "Nova Scotia" });
            states.Add(new States() { Abbreviations = "NT", Name = "Northwest Territories" });
            states.Add(new States() { Abbreviations = "NU", Name = "Nunavut" });
            states.Add(new States() { Abbreviations = "ON", Name = "Ontario" });
            states.Add(new States() { Abbreviations = "PE", Name = "Prince Edward Island" });
            states.Add(new States() { Abbreviations = "QC", Name = "Quebec" });
            states.Add(new States() { Abbreviations = "SK", Name = "Saskatchewan" });
            states.Add(new States() { Abbreviations = "YT", Name = "Yukon" });

            return states.ToList();
        }
    }

    public class LogWriter
    {
        private string m_exePath = string.Empty;

        public LogWriter(string logMessage)
        {
            LogWrite(logMessage);
        }

        public void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(HttpContext.Current.Server.MapPath("~/"));
            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "log.txt"))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
            }
        }
    }

  
}
public partial class States
{
    public string Name { get; set; }
    public string Abbreviations { get; set; }
}
public partial class TwilioSetting
{
    public string CompanyID { get; set; }
    public string TwilioAccountSid { get; set; }
    public string TwilioAccountAuthToken { get; set; }
    public string TwilioPhoneNumber { get; set; }
}