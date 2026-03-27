using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Processors
{
    public class BusinessContactProcessor
    {
        private string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private static string connstr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public List<BusinessContacts> GetAllBusinessContactsByCompany(string CompanyId)
        {
            List<BusinessContacts> businessContacts = new List<BusinessContacts>();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"SELECT [CompanyID]
                                  ,[CustomerID]
                                  ,[CustomerGuid]
                                  ,[Title]
                                  ,[BusinessName]
                                  ,[FirstName]
                                  ,[LastName]
                                  ,[Address1]
                                  ,[Address2]
                                  ,[City]
                                  ,[State]
                                  ,[Country]
                                  ,[ZipCode]
                                  ,[Phone]
                                  ,[Mobile]
                                  ,[Email]
                                  ,[CreatedDateTime]
                                  ,[CallPopAppId]
                                  ,[QboId],[CustomerCode]
                              FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyID=@CompanyID and [IsBusinessContact] = 1";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);

            db.Execute(out dt);

            db.Close();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    BusinessContacts businessContact = new BusinessContacts();
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Title = dr["Title"].ToString();
                    businessContact.FirstName = dr["FirstName"].ToString();
                    businessContact.LastName = dr["LastName"].ToString();
                    businessContact.Address1 = dr["Address1"].ToString();
                    businessContact.Address2 = dr["Address2"].ToString();
                    businessContact.CallPopAppId = dr["CallPopAppId"].ToString();
                    businessContact.CustomerID = dr["CustomerID"].ToString();
                    businessContact.CustomerGuid = dr["CustomerGuid"].ToString();
                    businessContact.City = dr["City"].ToString();
                    businessContact.Country = dr["Country"].ToString();
                    businessContact.CompanyID = dr["CompanyID"].ToString();
                    businessContact.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    businessContact.Email = dr["Email"].ToString();
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Mobile = dr["Mobile"].ToString();
                    businessContact.Phone = dr["Phone"].ToString();
                    businessContact.State = dr["State"].ToString();
                    businessContact.CustomerCode = dr["CustomerCode"].ToString();
                    businessContacts.Add(businessContact);
                }
            }

            return businessContacts;
        }

        public List<BusinessContacts> GetBusinessContacts(string CompanyId, string BusinessID)
        {
            List<BusinessContacts> businessContacts = new List<BusinessContacts>();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"SELECT  [CompanyID]
                                  ,[CustomerID]
                                  ,[CustomerGuid]
                                  ,[FirstName]
                                  ,[LastName]
                                  ,Title
                                  ,JobTitle
                                  ,[Address1]
                                  ,[Address2]
                                  ,[City]
                                  ,[State]
                                  ,[Country]
                                  ,[ZipCode]
                                  ,[Phone]
                                  ,[Mobile]
                                  ,[Email]
                                  ,[CreatedDateTime]
                                  ,[CallPopAppId]
                                  ,[QboId]
                                  ,[IsPrimaryContact]
                                  ,[BusinessID],[CustomerCode]
                              FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyID=@CompanyID and BusinessID=@BusinessID  ORDER BY [LastName]";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);
            db.Command.Parameters.AddWithValue("@BusinessID", BusinessID);

            db.Execute(out dt);

            db.Close();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    BusinessContacts businessContact = new BusinessContacts();
                    businessContact.FirstName = dr["FirstName"].ToString();
                    businessContact.Address1 = dr["Address1"].ToString();
                    businessContact.Address2 = dr["Address2"].ToString();
                    businessContact.LastName = dr["LastName"].ToString();
                    businessContact.BusinessID = dr["BusinessID"].ToString();
                    businessContact.CallPopAppId = dr["CallPopAppId"].ToString();
                    businessContact.CustomerGuid = dr["CustomerGuid"].ToString();
                    businessContact.City = dr["City"].ToString();
                    businessContact.CompanyID = dr["CompanyID"].ToString();
                    businessContact.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    businessContact.Email = dr["Email"].ToString();
                    businessContact.Mobile = dr["Mobile"].ToString();
                    businessContact.Phone = dr["Phone"].ToString();
                    businessContact.Country = dr["Country"].ToString();
                    businessContact.State = dr["State"].ToString();
                    businessContact.ZipCode = dr["ZipCode"].ToString();
                    businessContact.IsPrimaryContact = Convert.ToBoolean(dr["IsPrimaryContact"]);
                    businessContact.CustomerID = dr["CustomerID"].ToString();
                    businessContact.Title = dr["Title"].ToString();
                    businessContact.JobTitle = dr["JobTitle"].ToString();
                    businessContact.CustomerCode = dr["CustomerCode"].ToString();

                    businessContacts.Add(businessContact);
                }
            }

            return businessContacts;
        }

        public BusinessContacts GetBusinessContactByGuid(string CompanyId, string BusinesGuid)
        {
            BusinessContacts businessContact = new BusinessContacts();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"SELECT [CompanyID]
                                  ,[CustomerID]
                                  ,[CustomerGuid]
                                  ,[BusinessName]
                                  ,[Title]
                                  ,[FirstName]
                                  ,[LastName]
                                  ,[Address1]
                                  ,[Address2]
                                  ,[City]
                                  ,[State]
                                  ,[Country]
                                  ,[ZipCode]
                                  ,[Phone]
                                  ,[Mobile]
                                  ,[Email]
                                  ,Notes
                                  ,[CreatedDateTime]
                                  ,[CallPopAppId]
                                  ,[QboId],[AMCustomerID],[CustomerCode],CSLTagId,CSLTagString
                              FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyID=@CompanyID  and [IsBusinessContact] = 1 and [CustomerGuid]=@BusinesGuid";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);
            db.Command.Parameters.AddWithValue("@BusinesGuid", BusinesGuid);

            db.Execute(out dt);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Title = dr["Title"].ToString();
                    businessContact.FirstName = dr["FirstName"].ToString();
                    businessContact.LastName = dr["LastName"].ToString();
                    businessContact.Address1 = dr["Address1"].ToString();
                    businessContact.Address2 = dr["Address2"].ToString();
                    businessContact.CustomerGuid = dr["CustomerGuid"].ToString();
                    businessContact.CustomerID = dr["CustomerID"].ToString();
                    businessContact.CallPopAppId = dr["CallPopAppId"].ToString();
                    businessContact.City = dr["City"].ToString();
                    businessContact.CompanyID = dr["CompanyID"].ToString();
                    businessContact.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    businessContact.Email = dr["Email"].ToString();
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Mobile = dr["Mobile"].ToString();
                    businessContact.Phone = dr["Phone"].ToString();
                    businessContact.Country = dr["Country"].ToString();
                    businessContact.State = dr["State"].ToString();
                    businessContact.ZipCode = dr["ZipCode"].ToString();
                    businessContact.Notes = dr["Notes"].ToString();
                    businessContact.AMCustomerId = dr["AMCustomerID"].ToString();
                    businessContact.CustomerCode = dr["CustomerCode"].ToString();
                    if (dr["CSLTagId"] != DBNull.Value && !string.IsNullOrEmpty(dr["CSLTagId"].ToString()))
                    {
                        businessContact.CSLTagId = Convert.ToInt32(dr["CSLTagId"]);
                    }
                    else
                    {
                        businessContact.CSLTagId = null;
                    }
                    businessContact.CSLTagString = dr["CSLTagString"] != DBNull.Value ? dr["CSLTagString"].ToString() : "";
                }
            }

            return businessContact;
        }

        public BusinessContacts GetBusinessContactById(string CompanyId, string Businessid)
        {
            BusinessContacts businessContact = new BusinessContacts();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"SELECT [CompanyID]
                                  ,[CustomerID]
                                  ,[CustomerGuid]
                                  ,[BusinessName]
                                  ,[Title]
                                  ,[FirstName]
                                  ,[LastName]
                                  ,[Address1]
                                  ,[Address2]
                                  ,[City]
                                  ,[State]
                                  ,[Country]
                                  ,[ZipCode]
                                  ,[Phone]
                                  ,[Mobile]
                                  ,[Email]
                                  ,Notes
                                  ,[CreatedDateTime]
                                  ,[CallPopAppId]
                                  ,[QboId],[AMCustomerID],[CustomerCode],CSLTagId,CSLTagString
                              FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyID=@CompanyID and CustomerID=@CustomerID";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);
            db.Command.Parameters.AddWithValue("@CustomerID", Businessid);

            db.Execute(out dt);

            db.Close();

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Title = dr["Title"].ToString();
                    businessContact.FirstName = dr["FirstName"].ToString();
                    businessContact.LastName = dr["LastName"].ToString();
                    businessContact.Address1 = dr["Address1"].ToString();
                    businessContact.Address2 = dr["Address2"].ToString();
                    businessContact.CustomerGuid = dr["CustomerGuid"].ToString();
                    businessContact.CustomerID = dr["CustomerID"].ToString();
                    businessContact.CallPopAppId = dr["CallPopAppId"].ToString();
                    businessContact.City = dr["City"].ToString();
                    businessContact.CompanyID = dr["CompanyID"].ToString();
                    businessContact.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    businessContact.Email = dr["Email"].ToString();
                    businessContact.BusinessName = dr["BusinessName"].ToString();
                    businessContact.Mobile = dr["Mobile"].ToString();
                    businessContact.Phone = dr["Phone"].ToString();
                    businessContact.Country = dr["Country"].ToString();
                    businessContact.State = dr["State"].ToString();
                    businessContact.ZipCode = dr["ZipCode"].ToString();
                    businessContact.Notes = dr["Notes"].ToString();
                    businessContact.AMCustomerId = dr["AMCustomerID"].ToString();
                    businessContact.CustomerCode = dr["CustomerCode"].ToString();
                    if (dr["CSLTagId"] != DBNull.Value && !string.IsNullOrEmpty(dr["CSLTagId"].ToString()))
                    {
                        businessContact.CSLTagId = Convert.ToInt32(dr["CSLTagId"]);
                    }
                    else
                    {
                        businessContact.CSLTagId = null;
                    }
                    businessContact.CSLTagString = dr["CSLTagString"] != DBNull.Value ? dr["CSLTagString"].ToString() : "";
                }
            }

            return businessContact;
        }

        public Boolean AddBusinessContact(BusinessContacts businessContact)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
                               (CustomerID,Title,FirstName,LastName,[IsBusinessContact]
                               ,CompanyID
                               ,[BusinessName]
                               ,[Address1]
                               ,[Address2]
                               ,[City]
                               ,[State]
                               ,[ZipCode]
                               ,[Phone]
                               ,[Mobile]
                               ,[Email]
                               ,[Notes]
                               ,CustomerGuid
                               ,QboId
                               ,[CustomerCode]
                               ,[Country]
                               ,CSLTagId,CSLTagString
                               )
                         VALUES
                               ((Select IsNull(MAX(CustomerID), 0) + 1 as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID = '" + businessContact.CompanyID + "')" +
                               @",@Title,@FirstName,@LastName,@IsBusinessContact
                               ,@CompanyID
                               ,@BusinessName
                               ,@Address1
                               ,@Address2
                               ,@City
                               ,@State
                               ,@ZipCode
                               ,@Phone
                               ,@Mobile
                               ,@Email
                               ,@Notes
                               ,@CustomerGuid
                               ,@QboId
                               ,@CustomerCode
                               ,@Country,
                                @CSLTagId,@CSLTagString)";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            if (businessContact.QboId == null)
            {
                businessContact.QboId = "0";
            }
            if (businessContact.Notes == null)
            {
                businessContact.Notes = "";
            }
            db.Command.Parameters.AddWithValue("@CompanyID", businessContact.CompanyID);
            db.Command.Parameters.AddWithValue("@BusinessName", businessContact.BusinessName);
            db.Command.Parameters.AddWithValue("@Title", businessContact.Title);
            db.Command.Parameters.AddWithValue("@FirstName", businessContact.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", businessContact.LastName);
            db.Command.Parameters.AddWithValue("@Address1", businessContact.Address1);
            db.Command.Parameters.AddWithValue("@Address2", businessContact.Address2);
            db.Command.Parameters.AddWithValue("@City", businessContact.City);
            db.Command.Parameters.AddWithValue("@Country", businessContact.Country);
            db.Command.Parameters.AddWithValue("@State", businessContact.State);
            db.Command.Parameters.AddWithValue("@ZipCode", businessContact.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", businessContact.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", businessContact.Mobile);
            db.Command.Parameters.AddWithValue("@Email", businessContact.Email);
            db.Command.Parameters.AddWithValue("@CustomerGuid", businessContact.CustomerGuid);
            db.Command.Parameters.AddWithValue("@QboId", businessContact.QboId);
            db.Command.Parameters.AddWithValue("@IsBusinessContact", "1");
            db.Command.Parameters.AddWithValue("@Notes", businessContact.Notes);
            db.Command.Parameters.AddWithValue("@CustomerCode", businessContact.CustomerCode);
            db.Command.Parameters.AddWithValue("@CSLTagId", businessContact.CSLTagId.HasValue ? (object)businessContact.CSLTagId.Value : DBNull.Value);
            db.Command.Parameters.AddWithValue("@CSLTagString", businessContact.CSLTagString);

            return db.ExecuteCommand();
        }

        public Boolean Delete_BusinessContact(BusinessContacts businessContact)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            //string Sql = @"Delete From  [msSchedulerV3].[dbo].[tbl_BusinessContact] Where CompanyID=@CompanyID and BusinessID=@BusinessID;";
            //Sql += @"Delete From [msSchedulerV3].[dbo].[tbl_Customer] Where CompanyID=@CompanyID and BusinessID=@BusinessID and IsPrimaryContact = 1;";

            string Sql = @"Delete From  [msSchedulerV3].[dbo].[tbl_Customer] Where CompanyID=@CompanyID and CustomerID=@CustomerID;";
            //  Sql += @"Delete From [msSchedulerV3].[dbo].[tbl_Customer] Where CompanyID=@CompanyID and BusinessID=@BusinessID and IsPrimaryContact = 1;";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CustomerID", businessContact.BusinessID);
            db.Command.Parameters.AddWithValue("@CompanyID", businessContact.CompanyID);

            return db.ExecuteCommand();
        }

        public Boolean Update_BusinessContact(BusinessContacts businessContact)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"Update [msSchedulerV3].[dbo].[tbl_Customer]
                                Set [BusinessName]=@BusinessName,[Address1]=@Address1
                               ,[Title]=@Title
                                ,[FirstName]=@FirstName
                                ,[LastName]=@LastName
                                ,[Address2]=@Address2
                               ,[Country]=@Country
                               ,[City]=@City
                               ,[State]=@State
                               ,[ZipCode]=@ZipCode
                               ,[Phone]=@Phone
                               ,[Mobile]=@Mobile
                                ,[Notes]=@Notes
                               ,[Email]=@Email,[CustomerCode]=@CustomerCode,CSLTagId=@CSLTagId,CSLTagString=@CSLTagString Where CompanyID=@CompanyID and CustomerID=@CustomerID;";

            Sql += @"Update [msSchedulerV3].[dbo].[tbl_Customer] Set [BusinessName]=@BusinessName Where CompanyID=@CompanyID and [BusinessID]=@CustomerID;";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CustomerID", businessContact.CustomerID);
            db.Command.Parameters.AddWithValue("@CompanyID", businessContact.CompanyID);
            db.Command.Parameters.AddWithValue("@BusinessName", businessContact.BusinessName);
            db.Command.Parameters.AddWithValue("@Address1", businessContact.Address1);
            db.Command.Parameters.AddWithValue("@Address2", businessContact.Address2);
            db.Command.Parameters.AddWithValue("@FirstName", businessContact.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", businessContact.LastName);
            db.Command.Parameters.AddWithValue("@Title", businessContact.Title);
            db.Command.Parameters.AddWithValue("@Country", businessContact.Country);
            db.Command.Parameters.AddWithValue("@City", businessContact.City);
            db.Command.Parameters.AddWithValue("@State", businessContact.State);
            db.Command.Parameters.AddWithValue("@ZipCode", businessContact.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", businessContact.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", businessContact.Mobile);
            db.Command.Parameters.AddWithValue("@Email", businessContact.Email);
            db.Command.Parameters.AddWithValue("@Notes", businessContact.Notes);
            db.Command.Parameters.AddWithValue("@CustomerCode", businessContact.CustomerCode);
            db.Command.Parameters.AddWithValue("@CSLTagId", businessContact.CSLTagId.HasValue ? (object)businessContact.CSLTagId.Value : DBNull.Value);
            db.Command.Parameters.AddWithValue("@CSLTagString", string.IsNullOrEmpty(businessContact.CSLTagString) ? (object)DBNull.Value : businessContact.CSLTagString);

            return db.ExecuteCommand();
        }

        public Boolean Update_PrimaryContact(BusinessContacts businessContact)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"Update [msSchedulerV3].[dbo].[tbl_Customer]
                                Set [FirstName]=@FirstName,[Address1]=@Address1
                               ,[LastName]=@LastName
                                ,[Title]=@Title
                                ,[JobTitle]=@JobTitle
                               ,[Country]=@Country
                               ,[City]=@City
                               ,[State]=@State
                               ,[ZipCode]=@ZipCode
                               ,[Phone]=@Phone
                               ,[Mobile]=@Mobile
                               ,[Email]=@Email,[CustomerCode]=@CustomerCode Where CompanyID=@CompanyID and CustomerID=@CustomerID";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CustomerID", businessContact.CustomerID);
            db.Command.Parameters.AddWithValue("@CompanyID", businessContact.CompanyID);
            db.Command.Parameters.AddWithValue("@FirstName", businessContact.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", businessContact.LastName);
            db.Command.Parameters.AddWithValue("@Title", businessContact.Title);
            db.Command.Parameters.AddWithValue("@JobTitle", businessContact.JobTitle);
            db.Command.Parameters.AddWithValue("@Address1", businessContact.Address1);
            db.Command.Parameters.AddWithValue("@FirstName", businessContact.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", businessContact.LastName);
            db.Command.Parameters.AddWithValue("@Title", businessContact.Title);
            db.Command.Parameters.AddWithValue("@Country", businessContact.Country);
            db.Command.Parameters.AddWithValue("@City", businessContact.City);
            db.Command.Parameters.AddWithValue("@State", businessContact.State);
            db.Command.Parameters.AddWithValue("@ZipCode", businessContact.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", businessContact.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", businessContact.Mobile);
            db.Command.Parameters.AddWithValue("@Email", businessContact.Email);
            db.Command.Parameters.AddWithValue("@CustomerCode", businessContact.CustomerCode);

            return db.ExecuteCommand();
        }

        public Boolean AddBusinessPrimaryContact(BusinessContacts businessContact)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
                               ([CompanyID]
                               ,CustomerID
                               ,[FirstName]
                               ,[LastName]
                               ,Title
                               ,JobTitle
                               ,[Address1]
                               ,[City]
                               ,[State]
                               ,[ZipCode]
                               ,[Phone]
                               ,[Mobile]
                               ,[Email]
                               ,IsPrimaryContact
                               ,QboId
                               ,BusinessID
                               ,CustomerCode
                               ,[Country]
                               )
                         VALUES
                               (@CompanyID" +
                               ",(Select IsNull(MAX(CustomerID),0) + 1 as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID='" + businessContact.CompanyID + "')" +
                               @",@FirstName
                               ,@LastName
                                ,@Title
                               ,@JobTitle
                               ,@Address1
                               ,@City
                               ,@State
                               ,@ZipCode
                               ,@Phone
                               ,@Mobile
                               ,@Email
                               ,@IsPrimaryContact
                               , @QboId" +
                               "," + "(Select BusinessID from msSchedulerV3.dbo.tbl_BusinessContact where BusinesGuid = '" + businessContact.CustomerGuid + "')" +
                               ",@CustomerCode" +
                               ",@Country)";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            if (businessContact.QboId == null)
            {
                businessContact.QboId = "0";
            }
            db.Command.Parameters.AddWithValue("@CompanyID", businessContact.CompanyID);
            db.Command.Parameters.AddWithValue("@FirstName", businessContact.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", businessContact.LastName);
            db.Command.Parameters.AddWithValue("@Title", businessContact.Title);
            db.Command.Parameters.AddWithValue("@JobTitle", businessContact.JobTitle);
            db.Command.Parameters.AddWithValue("@Address1", businessContact.Address1);
            db.Command.Parameters.AddWithValue("@City", businessContact.City);
            db.Command.Parameters.AddWithValue("@Country", businessContact.Country);
            db.Command.Parameters.AddWithValue("@State", businessContact.State);
            db.Command.Parameters.AddWithValue("@ZipCode", businessContact.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", businessContact.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", businessContact.Mobile);
            db.Command.Parameters.AddWithValue("@Email", businessContact.Email);
            db.Command.Parameters.AddWithValue("@IsPrimaryContact", businessContact.IsPrimaryContact);
            db.Command.Parameters.AddWithValue("@QboId", businessContact.QboId);
            db.Command.Parameters.AddWithValue("@CustomerCode", businessContact.CustomerCode);

            return db.ExecuteCommand();
        }
    }

    public class BusinessContacts
    {
        public string CompanyID { get; set; }
        public string BusinessID { get; set; }
        public string CustomerID { get; set; }
        public string Notes { get; set; }

        //public string BusinesGuid { get; set; }
        public string CustomerGuid { get; set; }

        public string BusinessName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; } = "";
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string JobTitle { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string CallPopAppId { get; set; }
        public string QboId { get; set; }
        public bool IsPrimaryContact { get; set; }

        public bool IsBusinessContact { get; set; }

        //Nizam
        public string AMCustomerId { get; set; } = "0";

        public string CustomerCode { get; set; } = "";
        public int? CSLTagId { get; set; } = null;
        public string CSLTagString { get; set; } = "";
    }
}
