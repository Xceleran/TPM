using FSM.Models;
using FSM.Models.LoginModels;
using FSM.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FSM.Processors
{
    public class LoginProcessor
    {
        public bool VerifyUser(string UserID, string CompanyID)
        {
            LoginObject _loginObject = new LoginObject();

            string sql = "";
            Database db = new Database();
            DataTable dt = new DataTable();
            if (!string.IsNullOrEmpty(UserID) && !string.IsNullOrEmpty(CompanyID))
            {
                sql = "Select c.CompanyIdInt,c.TimeZone,c.CompanyID,c.CompanyGUID,c.CompanyName,c.CompanyTag, u.UserID,u.Password,u.email,u.FirstName,u.LastName " +
                     "From [XinatorCentral].[dbo].[tbl_User] u inner join msSchedulerV3.dbo.tbl_Company c " +
                     " on u.CompanyID= c.CompanyID " +
                     " where u.CompanyID='" + CompanyID + "'" +
                     " and u.UserID='" + UserID + "'";

                sql += @"SELECT [ProductID] FROM [XinatorCentral].[dbo].[tbl_ProductsByCompany] where ProductID = '11' and ProductAccess=1 and CompanyID='" + CompanyID + "'";

                sql += @"SELECT [CompanyType],IsMMSAllowed,IsInboundSMSAllowed FROM [XinatorCentral].dbo.tbl_Company where  CompanyID='" + CompanyID + "'";
                DataSet dataSet = db.Get_DataSet(sql, CompanyID);
                dt = dataSet.Tables[0];

                _loginObject.IsParent = false;
                _loginObject.DealerName = "";
                _loginObject.DealerAddress = "";
                _loginObject.ParentID = CompanyID;

                _loginObject.LoginUser = UserID;

                _loginObject.UserFirstName = UserID;

                //if (dataSet.Tables[1].Rows.Count > 0)
                //{
                System.Web.HttpContext.Current.Session["IsCecPro"] = true;

              
                //}
                //else
                //{
                //    Session["IsCecPro"] = false;
                //}

                HttpContext.Current.Session["CompanyType"] = "Central";
                System.Web.HttpContext.Current.Session["IsLHG"] = false;
                System.Web.HttpContext.Current.Session["IsAireMaster"] = false;
                System.Web.HttpContext.Current.Session["IsPCS"] = false;
                System.Web.HttpContext.Current.Session["mXP"] = false;
                System.Web.HttpContext.Current.Session["mSFHome"] = false;
                System.Web.HttpContext.Current.Session["TEST"] = false;
                if (dataSet.Tables[2].Rows.Count > 0)
                {
                    HttpContext.Current.Session["CompanyType"] = dataSet.Tables[2].Rows[0]["CompanyType"].ToString();
                    _loginObject.IsMMSAllowed = Convert.ToBoolean(dataSet.Tables[2].Rows[0]["IsMMSAllowed"]);
                    _loginObject.IsInboundSMSAllowed = Convert.ToBoolean(dataSet.Tables[2].Rows[0]["IsInboundSMSAllowed"]);
                    System.Web.HttpContext.Current.Session["IsMMSAllowed"] = Convert.ToBoolean(dataSet.Tables[2].Rows[0]["IsMMSAllowed"]);
                    System.Web.HttpContext.Current.Session["IsInboundSMSAllowed"] = Convert.ToBoolean(dataSet.Tables[2].Rows[0]["IsInboundSMSAllowed"]);
                }

                if (System.Web.HttpContext.Current.Session["CompanyType"]?.ToString() == "LHG")
                {
                    System.Web.HttpContext.Current.Session["IsLHG"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "Aire-Master")
                {
                    System.Web.HttpContext.Current.Session["IsAireMaster"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "PCS")
                {
                    System.Web.HttpContext.Current.Session["IsPCS"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "mXP")
                {
                    System.Web.HttpContext.Current.Session["mXP"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "TEST")
                {
                    System.Web.HttpContext.Current.Session["TEST"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "Demo")
                {
                    System.Web.HttpContext.Current.Session["Demo"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "XSI")
                {
                    System.Web.HttpContext.Current.Session["XSI"] = true;
                }
                if (System.Web.HttpContext.Current.Session["CompanyType"].ToString() == "Central")
                {
                    System.Web.HttpContext.Current.Session["Central"] = true;
                }
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];

                    System.Web.HttpContext.Current.Session["LoginObj"] = _loginObject;

                    System.Web.HttpContext.Current.Session["LoginUser"] = UserID;
                    System.Web.HttpContext.Current.Session["UserFirstName"] = dr["FirstName"].ToString();
                    System.Web.HttpContext.Current.Session["UserLastName"] = dr["LastName"].ToString();
                    System.Web.HttpContext.Current.Session["CompanyID"] = dr["CompanyID"].ToString();
                    System.Web.HttpContext.Current.Session["CompanyName"] = dr["CompanyName"].ToString();
                    System.Web.HttpContext.Current.Session["CompanyTag"] = dr["CompanyTag"].ToString();

                    System.Web.HttpContext.Current.Session["CurrentTimeZone"] = dr["TimeZone"].ToString();

                    //if (dr["TimeZone"].ToString() == "EST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Eastern Standard Time (EST)"; }
                    //else if (dr["TimeZone"].ToString() == "AKST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Alaska Standard Time (AKST)"; }
                    //else if (dr["TimeZone"].ToString() == "AST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Atlantic Standard Time (AST)"; }
                    //else if (dr["TimeZone"].ToString() == "CST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Central Standard Time (CST)"; }
                    //else if (dr["TimeZone"].ToString() == "HAST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Hawaii-Aleutian Standard Time (HAST)"; }
                    //else if (dr["TimeZone"].ToString() == "MST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "EMountain Standard Time (MST)"; }
                    //else if (dr["TimeZone"].ToString() == "PST") { System.Web.HttpContext.Current.Session["CurrentTimeZone"] = "Pacific Standard Time (PST)"; }

                    System.Web.HttpContext.Current.Session["CompanyGUID"] = dr["CompanyGUID"].ToString();
                    System.Web.HttpContext.Current.Session["CompanyIdInt"] = dr["CompanyIdInt"].ToString();
                    System.Web.HttpContext.Current.Session["hf_IsShowQBOMsg"] = "false";
                    
                    SetDefaultData(CompanyID);
                   
                    UserLogProcessor userLogProcessor = new UserLogProcessor();

                    userLogProcessor.AddLog(new UserLog
                    {
                        UserID = UserID,
                        CompanyID = CompanyID,
                        Text = "Logged Into TPM"
                    });

                    return true;
                }
            }

            return false;
        }

        public void SetDefaultData(string CompanyID)
        {
            try
            {
                DataSet _dataSet = new DataSet();
                Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());

                db.Init("SetDefaultValues");
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.Execute(out _dataSet);
                HttpContext.Current.Session["Status"] = _dataSet.Tables[0];
            }
            catch(Exception ex) {

            }

        }

        public void LoadPrivilege(string userId)
        {
            Database db = new Database();
            string Sql = @"SELECT  tbl_Privelege.id,tbl_Privelege.Name,tbl_User_Privileage.UserID FROM tbl_Privelege INNER JOIN
                        tbl_User_Privileage ON tbl_Privelege.id = tbl_User_Privileage.PreviliageID" +
                         " Where tbl_User_Privileage.UserID = '" + userId + "'";
            DataTable dt = new DataTable();
            db.Execute(Sql, out dt);
            db.Close();
            UserPrivilege userPrivilege = new UserPrivilege();
            List<Privelege> Priveleges = new List<Privelege>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    Priveleges.Add(new Privelege
                    {
                        id = row["id"].ToString(),
                        text = row["Name"].ToString()

                    });
                }
            }
            userPrivilege.CanAccessSetting = Priveleges.Where(x => x.text == "CanAccessSetting").ToList().Count() > 0 ? true : false;
            userPrivilege.CanAccessVtPayment = Priveleges.Where(x => x.text == "CanAccessVtPayment").ToList().Count() > 0 ? true : false;
            userPrivilege.CanAddCustomerBooking = Priveleges.Where(x => x.text == "CanAddCustomerBooking").ToList().Count() > 0 ? true : false;
            userPrivilege.CanDeleteCustomer = Priveleges.Where(x => x.text == "CanDeleteCustomer").ToList().Count() > 0 ? true : false;
            userPrivilege.CanEditBooking = Priveleges.Where(x => x.text == "CanEditBooking").ToList().Count() > 0 ? true : false;
            userPrivilege.CanEditCustomer = Priveleges.Where(x => x.text == "CanEditCustomer").ToList().Count() > 0 ? true : false;
            userPrivilege.CanEditPayment = Priveleges.Where(x => x.text == "CanEditPayment").ToList().Count() > 0 ? true : false;
            userPrivilege.CanAccessQuickBooks = Priveleges.Where(x => x.text == "CanAccessQuickBooks").ToList().Count() > 0 ? true : false;
            userPrivilege.CanAccessUserInfo = Priveleges.Where(x => x.text == "CanAccessUserInfo").ToList().Count() > 0 ? true : false;
            userPrivilege.CanAccessInvoice = Priveleges.Where(x => x.text == "CanAccessInvoice").ToList().Count() > 0 ? true : false;

            HttpContext.Current.Session["userPrivilege"] = userPrivilege;
            HttpContext.Current.Session["CanAccessQuickBooks"] = userPrivilege.CanAccessQuickBooks;
            HttpContext.Current.Session["CanAccessUserInfo"] = userPrivilege.CanAccessUserInfo;
            HttpContext.Current.Session["CanAccessInvoice"] = userPrivilege.CanAccessInvoice;
        }
    }
}
