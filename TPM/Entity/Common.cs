using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class Common
{

    public enum AddressType
    {
        USA,
        Canada
    }

    public static string DateFormat = "MM/dd/yyyy hh:mm tt";
    public static string DateFormat2 = "MM/dd/yyyy h:mm tt";
    public static string DateFormat3 = "MM/dd/yyyy";
    public static string DealerText = "Dealer";
    public DateTime Formatdate(string DateString)
    {
        DateTime dateTime = DateTime.Now;

        try

        {


            if (DateTime.TryParseExact(DateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
            if (DateTime.TryParseExact(DateString, DateFormat2, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
            if (DateTime.TryParseExact(DateString, DateFormat3, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }


        }
        catch
        {


        }
        return dateTime;
    }


    public enum Pages
    {
        AppointmentDetail,
        Invoices,
        Appoinmnet,
        InvocieList,
        Customers,
        CustomerDetail,
        AppoinmnetList,
        Announcement,
        AnnouncementList,
        AppointmentConfirmation,
        AppointmentReminder,
        AppointmentReminderList,
        BusinessContact,
        Calender,
        CalenderCustomization,
        Company,
        CreateAppointment,
        CustomerFiles,
        EditUser,
        EmailHistory_List,
        FollowUpAdd,
        FollowUpList,
        HolidayList,
        Home,
        Invoice,
        InvoiceList,
        Item,
        ItemList,
        QboConnection,
        Report,
        ResouceList,
        Role,
        RptApptRequestedByDate,
        RptAssignmentByDate,
        ServiceType,
        ServiceTypeList,
        Survey,
        SurveyResult,
        SurveySettings,
        Tax,
        TaxList,
        TimeBlock,
        UserAdd,
        UserRole,
        WisetackSignup,
        WisetackTransections,
        ReportSch,
        View_Appointment,
        Disclaimer,
        ResourceWiseAppoinments,
        ProjectList,
        Project,
        ProjectSalesStatus,
        ResourceGroups
    }
    public enum EmailType
    {
        AppoinmnetConfirmation,
        AppoinmnetCcancel,
        ResourceEmail
    }

    public static string CleanInput(string sInput, int iLength = 0)
    {
        if (sInput == null)
        {
            return "";
        }
        if (iLength > 0)
        {
            if (sInput.Length > iLength)
            {
                sInput = sInput.Substring(0, iLength);
            }
        }

        sInput = sInput.Replace("'", "");
        sInput = sInput.Replace(";", "");
        sInput = sInput.Replace("--", "");
        sInput = sInput.Replace("<", "");
        sInput = sInput.Replace(">", "");
        sInput = sInput.Replace("script", "");
        sInput = sInput.Replace("html", "");
        sInput = sInput.Replace("(", "");
        sInput = sInput.Replace(")", "");
        sInput = sInput.Replace("=", "");
        sInput = sInput.Replace("*", "");
        sInput = sInput.Replace("href", "");
        sInput = sInput.Replace("&lt", "");
        sInput = sInput.Replace("&gt", "");
        sInput = sInput.Replace("&quot", "");

        return sInput;

    }


    public static string EncryptString(string key, string plainText)
    {
        try
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string DecryptString(string key, string cipherText)
    {
        try
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }


    }


}
public class StatusData
{
    public Int32 StatusID { get; set; }
    public string StatusName { get; set; }
    public string CompanyID { get; set; }
    public int Order { get; set; }
}

public class MasterData
{
    public Int32 ID { get; set; }
    public string Title { get; set; }
    public int SortOrder { get; set; }
    public string CompanyID { get; set; }
}
