using FSM.Entity.Customer;
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
    public class CustomerProcessor
    {
        string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        static string connstr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public Boolean CheckIfValidCustomer(CustomerEntity customers)
        {
            if (customers == null || string.IsNullOrEmpty(customers.CompanyID) || string.IsNullOrEmpty(customers.CustomerGuid))
                return false;

            Database db = new Database(connStr);
            string Sql = @"SELECT COUNT(1) FROM [msSchedulerV3].[dbo].[tbl_Customer]
                           WHERE CompanyID = @CompanyID AND CustomerGuid = @CustomerGuid";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", customers.CompanyID);
            db.Command.Parameters.AddWithValue("@CustomerGuid", customers.CustomerGuid);

            return db.ExecuteExecuteScalar() > 0;
        }
        public CustomerEntity GetCustomerDetails(string customerId, string companyId)
        {
            var customer = new CustomerEntity();
            string connectionString = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connectionString);
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_Customer] WHERE CustomerID = @CustomerID AND CompanyID = @CompanyID;";
                db.AddParameter("@CompanyID", companyId, System.Data.SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, System.Data.SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    DataRow dataRow = dt.Rows[0];
                    customer.CustomerID = customerId;
                    customer.CompanyID = companyId;
                    customer.CustomerGuid = dataRow.Field<string>("CustomerGuid") ?? "";
                    customer.FirstName = dataRow.Field<string>("FirstName") ?? "";
                    customer.LastName = dataRow.Field<string>("LastName") ?? "";
                    customer.Phone = dataRow.Field<string>("Phone") ?? "";
                    customer.Mobile = dataRow.Field<string>("Mobile") ?? "";
                    customer.Email = dataRow.Field<string>("Email") ?? "";
                    customer.Address1 = dataRow.Field<string>("Address1") ?? "";
                    customer.Address2 = dataRow.Field<string>("Address2") ?? "";
                    customer.City = dataRow.Field<string>("City") ?? "";
                    customer.State = dataRow.Field<string>("State") ?? "";
                    customer.ZipCode = dataRow.Field<string>("ZipCode") ?? "";
                    customer.CompanyName = dataRow.Field<string>("CompanyName") ?? "";
                    customer.BusinessName = dataRow.Field<string>("BusinessName") ?? "";
                    customer.Title = dataRow.Field<string>("Title") ?? "";
                    customer.JobTitle = dataRow.Field<string>("JobTitle") ?? "";
                    customer.Notes = dataRow.Field<string>("Notes") ?? "";
                    customer.BusinessID = dataRow.Field<int?>("BusinessID") ?? 0;
                    customer.IsBusinessContact = dataRow.Field<bool?>("IsBusinessContact") ?? false;
                    customer.IsPrimaryContact = dataRow.Field<bool?>("IsPrimaryContact") ?? false;
                    customer.CreatedDateTime = dataRow.Field<DateTime?>("CreatedDateTime") ?? DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                db.Close();
                // Return empty customer object on error
                return customer;
            }
            return customer;
        }
        public Boolean Add_Customer(CustomerEntity customers)
        {

            Database db = new Database(connStr);
            DataTable dt = new DataTable();

            string CustomerID = @"(Select IsNull(MAX(CustomerID),0) +1 as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID = @CompanyID)";

            //if(HttpContext.Current.Session["IsAireMaster"] != null)
            //{
            //    if((bool)HttpContext.Current.Session["IsAireMaster"])
            //    {
            //        CustomerID = @"(Select IsNull(MAX(CustomerID),0)+ 1 + '-' + @CompanyID as NewCustomerID from msSchedulerV3.dbo.tbl_Customer where CompanyID = @CompanyID)";
            //    }
            //}

            string Sql = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
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
                                     ,CSLTagString)
                         VALUES
                               (@CompanyID," +
                                CustomerID +
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
                               ,@CustomerGuid
                               ,@IsPrimaryContact
                               ,@IsBusinessContact
                               ,@Notes
                               ,@CompanyName
                               ,@BusinessName
                               ,@BusinessID,
                                @Country,
                                @CSLTagId,
                                @CSLTagString)";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            if (customers.Notes == null)
            {
                customers.Notes = "";
            }
            if (customers.CompanyName == null)
            {
                customers.CompanyName = "";
            }
            if (customers.BusinessName == null)
            {
                customers.BusinessName = "";
            }


            db.Command.Parameters.AddWithValue("@CompanyID", customers.CompanyID);
            db.Command.Parameters.AddWithValue("@FirstName", customers.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", customers.LastName);
            db.Command.Parameters.AddWithValue("@Title", customers.Title);
            db.Command.Parameters.AddWithValue("@JobTitle", customers.JobTitle);
            db.Command.Parameters.AddWithValue("@Address1", customers.Address1);
            db.Command.Parameters.AddWithValue("@City", customers.City);
            db.Command.Parameters.AddWithValue("@State", string.IsNullOrEmpty(customers.State) ? (object)DBNull.Value : customers.State);
            db.Command.Parameters.AddWithValue("@ZipCode", customers.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", customers.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", customers.Mobile);
            db.Command.Parameters.AddWithValue("@Email", customers.Email);
            db.Command.Parameters.AddWithValue("@CustomerGuid", customers.CustomerGuid);
            db.Command.Parameters.AddWithValue("@IsPrimaryContact", customers.IsPrimaryContact);
            db.Command.Parameters.AddWithValue("@IsBusinessContact", customers.IsBusinessContact);
            db.Command.Parameters.AddWithValue("@BusinessID", customers.BusinessID);
            db.Command.Parameters.AddWithValue("@Notes", customers.Notes);
            db.Command.Parameters.AddWithValue("@CompanyName", customers.CompanyName);
            db.Command.Parameters.AddWithValue("@BusinessName", customers.BusinessName);
            db.Command.Parameters.AddWithValue("@Country", customers.Country);
            db.Command.Parameters.AddWithValue("@CSLTagId", customers.CSLTagId.HasValue ? (object)customers.CSLTagId.Value : DBNull.Value);
            db.Command.Parameters.AddWithValue("@CSLTagString", customers.CSLTagString);

            return db.ExecuteCommand();
        }
        public Boolean Update_Customer(CustomerEntity customers)
        {

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            if (customers.Notes == null)
            {
                customers.Notes = "";
            }
            if (customers.CompanyName == null)
            {
                customers.CompanyName = "";
            }
            if (customers.BusinessName == null)
            {
                customers.BusinessName = "";
            }

            string Sql = @"UPDATE msSchedulerV3.dbo.tbl_Customer SET FirstName =@FirstName,
                            LastName = @LastName, 
                            Address1 =@Address1,
                            Title=@Title,
                            JobTitle=@JobTitle,
                            City = @City, 
                            State = @State, 
                            ZipCode = @ZipCode, 
                            Phone =@Phone, 
                            Mobile = @Mobile, 
                            Email = @Email, 
                            Notes = @Notes,
                            CompanyName = @CompanyName,
                            BusinessName = @BusinessName,
                            BusinessID = @BusinessID,
                            Country = @Country,
                            CSLTagId = @CSLTagId,
                            CSLTagString = @CSLTagString
                            WHERE CompanyID =@CompanyID and CustomerID = @customerID ";


            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            if (customers.Notes == null)
            {
                customers.Notes = "";
            }
            if (customers.CompanyName == null)
            {
                customers.CompanyName = "";
            }
            db.Command.Parameters.AddWithValue("@CompanyID", customers.CompanyID);
            db.Command.Parameters.AddWithValue("@customerID", customers.CustomerID);
            db.Command.Parameters.AddWithValue("@FirstName", customers.FirstName);
            db.Command.Parameters.AddWithValue("@LastName", customers.LastName);
            db.Command.Parameters.AddWithValue("@Title", customers.Title);
            db.Command.Parameters.AddWithValue("@JobTitle", customers.JobTitle);
            db.Command.Parameters.AddWithValue("@Address1", customers.Address1);
            db.Command.Parameters.AddWithValue("@City", customers.City);
            db.Command.Parameters.AddWithValue("@State", string.IsNullOrEmpty(customers.State) ? (object)DBNull.Value : customers.State);
            db.Command.Parameters.AddWithValue("@ZipCode", customers.ZipCode);
            db.Command.Parameters.AddWithValue("@Phone", customers.Phone);
            db.Command.Parameters.AddWithValue("@Mobile", customers.Mobile);
            db.Command.Parameters.AddWithValue("@Email", customers.Email);
            db.Command.Parameters.AddWithValue("@Notes", customers.Notes);
            db.Command.Parameters.AddWithValue("@CompanyName", customers.CompanyName);
            db.Command.Parameters.AddWithValue("@BusinessName", customers.BusinessName);
            db.Command.Parameters.AddWithValue("@BusinessID", customers.BusinessID);
            db.Command.Parameters.AddWithValue("@Country", customers.Country);
            db.Command.Parameters.AddWithValue("@CSLTagId", customers.CSLTagId.HasValue ? (object)customers.CSLTagId.Value : DBNull.Value);
            db.Command.Parameters.AddWithValue("@CSLTagString", customers.CSLTagString == null ? "" : customers.CSLTagString);


            return db.ExecuteCommand();
        }
        public CustomerEntity GetCustomerByid(string id, string CompanyId)
        {
            CustomerEntity customerEntity = new CustomerEntity();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"Select * From [msSchedulerV3].[dbo].[tbl_Customer] Where CompanyID=@CompanyID And Customerid=@Customerid";

            db.Command.CommandText = Sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);
            db.Command.Parameters.AddWithValue("@Customerid", id);

            db.Execute(out dt);

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    customerEntity.FirstName = dr["FirstName"].ToString();
                    customerEntity.LastName = dr["LastName"].ToString();
                    customerEntity.Title = dr["Title"].ToString();
                    customerEntity.JobTitle = dr["JobTitle"].ToString();
                    customerEntity.Address1 = dr["Address1"].ToString();
                    customerEntity.CustomerGuid = dr["CustomerGuid"].ToString();
                    customerEntity.CustomerID = dr["CustomerID"].ToString();
                    customerEntity.BusinessID = Convert.ToInt32(dr["BusinessID"]);
                    customerEntity.CallPopAppId = dr["CallPopAppId"].ToString();
                    customerEntity.City = dr["City"].ToString();
                    customerEntity.CompanyID = dr["CompanyID"].ToString();
                    customerEntity.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    customerEntity.Email = dr["Email"].ToString();
                    customerEntity.Mobile = dr["Mobile"].ToString();
                    customerEntity.Phone = dr["Phone"].ToString();
                    customerEntity.State = dr["State"].ToString();
                    customerEntity.Notes = dr["Notes"].ToString();
                    customerEntity.CompanyName = dr["CompanyName"].ToString();
                    customerEntity.BusinessName = dr["BusinessName"].ToString();
                    customerEntity.IsBusinessContact = Convert.ToBoolean(dr["IsBusinessContact"]);
                    customerEntity.IsPrimaryContact = Convert.ToBoolean(dr["IsPrimaryContact"]);
                    customerEntity.Country = dr["Country"].ToString();


                }
            }

            return customerEntity;
        }
        public CustomerEntity GetCustomerByGuid(string Guid, string CompanyId)
        {
            CustomerEntity customerEntity = new CustomerEntity();

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string Sql = @"Select * From [msSchedulerV3].[dbo].[tbl_Customer] Where CompanyID=@CompanyID And CustomerGuid=@CustomerGuid";

            db.Command.CommandText = Sql;
            db.Command.Parameters.AddWithValue("@CompanyID", CompanyId);
            db.Command.Parameters.AddWithValue("@CustomerGuid", Guid);

            db.Execute(out dt);

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    customerEntity.FirstName = dr["FirstName"].ToString();
                    customerEntity.LastName = dr["LastName"].ToString();
                    customerEntity.Title = dr["Title"].ToString();
                    customerEntity.JobTitle = dr["JobTitle"].ToString();
                    customerEntity.Address1 = dr["Address1"].ToString();
                    customerEntity.CustomerGuid = dr["CustomerGuid"].ToString();
                    customerEntity.CustomerID = dr["CustomerID"].ToString();
                    customerEntity.BusinessID = Convert.ToInt32(dr["BusinessID"]);
                    customerEntity.CallPopAppId = dr["CallPopAppId"].ToString();
                    customerEntity.City = dr["City"].ToString();
                    customerEntity.CompanyID = dr["CompanyID"].ToString();
                    customerEntity.CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"].ToString());
                    customerEntity.Email = dr["Email"].ToString();
                    customerEntity.Mobile = dr["Mobile"].ToString();
                    customerEntity.Phone = dr["Phone"].ToString();
                    customerEntity.Notes = dr["Notes"].ToString();
                    customerEntity.State = dr["State"].ToString();
                    customerEntity.CompanyName = dr["CompanyName"].ToString();
                    customerEntity.BusinessName = dr["BusinessName"].ToString();
                    customerEntity.IsBusinessContact = Convert.ToBoolean(dr["IsBusinessContact"].ToString());
                    customerEntity.IsPrimaryContact = Convert.ToBoolean(dr["IsPrimaryContact"]);
                    customerEntity.Country = dr["Country"].ToString();


                }
            }

            return customerEntity;
        }
    }
}
