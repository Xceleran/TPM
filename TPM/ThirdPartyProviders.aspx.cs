using FSM.Entity.Customer;
using FSM.Helper;
using FSM.Processors;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Data;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Script.Services;
using System.Web.Services;


namespace TPM
{
   
    public partial class ThirdPartyProviders : System.Web.UI.Page
    {
        static string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        static string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("Dashboard.aspx");
            }
            if (!IsPostBack)
            {
               
            }

            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            companyId.Attributes.Add("value", companyid);
        }

        
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static WarrantyCompany AssignWarrentyCompany(string WarrentyCompanyID)
        {
            var item = new WarrantyCompany();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            if (!string.IsNullOrWhiteSpace(WarrentyCompanyID))
            {
                Database db = new Database();
                try
                {
                   
                    DataTable dt = new DataTable();

                    string Sql = @"(Select IsNull(MAX(CustomerID),0) +1 as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID = @CompanyID)";
                    Sql += @"(Select IsNull(MAX(CustomerID),0) +1 as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID = @CompanyID)";

                    DataSet dataSet = db.Get_DataSet(Sql, companyid);

                    string CustomerID = dataSet.Tables[0].Rows[0]["NewCustomerID"].ToString();
                    string InsertSQL = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
                                   ([CompanyID]
                                   ,[CustomerID]
                                   ,[FirstName]
                                   ,[LastName]
                                   ,[Title]
                                   ,[JobTitle]
                                   ,[Address1]
                                   ,[City]
                                   ,[State]
                                   ,[ZipCode]
                                   ,[Phone]
                                   ,[Mobile]
                                   ,[Email]
                                   ,[CustomerGuid]
                                   ,[IsPrimaryContact]
                                   ,IsBusinessContact
                                   ,[Notes]
                                   ,CompanyName
                                   ,BusinessName
                                   ,[BusinessID]
                                   ,Country
                                   ,CSLTagId
                                   ,CSLTagString,WarrentyCompanyID)";

                    InsertSQL += @" SELECT '" + companyid + "','" + CustomerID +
                              "',CompanyName,'','',''" +
                              ",[Address],[City],[State],[Zip],'','','','" + Guid.NewGuid().ToString().ToUpper() + "',1,1,'',CompanyName,CompanyName,0,'USA',0,'','" + WarrentyCompanyID + "'" +
                          @" FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] where [WarrantyCompanyUID]="  + WarrentyCompanyID + "; ";

                    InsertSQL += " Insert Into [tbl_WarrentyCompanyCustomer] ([WarrentyCompanyID],[CustomerID],[CompanyID]) " +
                        " values ('" + WarrentyCompanyID + "','" + CustomerID + "','" + companyid + "');";

                    InsertSQL += " Insert Into [tbl_AssignWarrantyCompany] ([CompanyID],[WarrantyCompanyUID]) " +
                        " values ('" + companyid + "','" + WarrentyCompanyID + "');";

             
        db.Open();
                     db.ExecuteScalarData(InsertSQL);
                    db.Close();
                }
                catch (Exception ex)
                {
                    return item;
                }
                finally
                {
                    db.Close();
                }
            }
            return item;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<WarrantyCompany> GetBillableItems()
        {
            var items = new List<WarrantyCompany>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetBillableItems: CompanyID is missing from session");
                return items;
            }
            Database db = new Database();
            try
            {
               
                DataTable dt = new DataTable();
                // Check if QboType column exists, if not use 0 as default
                string sql = @"SELECT  [WarrantyCompanyUID],WarrantyCompanyGuID
                              ,[CompanyName]
                              ,[IsActive]
                              ,[ShortName]
                              ,[Address]
                              ,[City]
                              ,[State]
                              ,[Zip]
                          FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] where [IsActive] = 1 order by CompanyName;";
                sql += "select [CompanyID],[WarrantyCompanyUID] from tbl_AssignWarrantyCompany where companyid=@CompanyId;";
                DataSet dataSet= db.Get_DataSet(sql, companyid);

                dt = dataSet.Tables[0];
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var item = new WarrantyCompany();
                        item.CompanyID = companyid;
                        item.Id = row["WarrantyCompanyUID"].ToString().Trim() ;
                        item.CompanyName = row.Field<string>("CompanyName") ?? "";
                        item.Address = row.Field<string>("Address") ?? "";
                        item.City = row.Field<string>("City") ?? "";
                        item.State = row.Field<string>("State") ?? "";
                        item.Zip = row.Field<string>("Zip") ?? "";
                        item.WarrantyCompanyGuID = row.Field<string>("WarrantyCompanyGuID") ?? "";
                        foreach (DataRow _row in dataSet.Tables[1].Rows)
                        {
                            if (string.Equals(row["WarrantyCompanyUID"].ToString().Trim(), _row["WarrantyCompanyUID"].ToString().Trim()))
                            {
                                item.IsEnable = true;
                                break;
                            }
                        }

                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetBillableItems: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return items;
            }
            finally
            {
                db.Close();
            }
            return items;
        }

     


       
    }

    public class WarrantyCompany
    {
        public string CompanyID { get; set; }
        public string Id { get; set; }
        public string Zip { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string WarrantyCompanyGuID { get; set; }
        
        public Boolean IsEnable { get; set; }
    }
    
}