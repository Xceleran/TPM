using FSM.Helper;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace FSM.Processors
{
    public class QBOManager
    {
        public bool VerifyCompanySetting(string CompanyID, ref QBOSettins qboSettings)
        {
            bool bRetVal = false;

            try
            {
                Database db = new Database();
                //  db.Open();
                string Sql = "select AccessToken, RefreshToken, BQOFileID, CompanyID, ConnectedCompanyName from [myServiceJobs].[dbo].[QBOCompanySettings] where CompanyID='" + CompanyID + "'";

                DataTable dt;

                db.Execute(Sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    qboSettings = new QBOSettins();
                    qboSettings.AccessToken = dr["AccessToken"].ToString();
                    qboSettings.RefreshToken = dr["RefreshToken"].ToString();
                    qboSettings.FileID = dr["BQOFileID"].ToString();
                    qboSettings.ConnectedCompanyName = dr["ConnectedCompanyName"].ToString();
                    qboSettings.isQBConnected = true;
                    bRetVal = true;
                }
                db.Close();
            }
            catch (Exception ex)
            {

            }

            return bRetVal;
        }
        public ServiceContext GetServiceContext(QBOSettins qboSettings, string CompanyID)
        {
            //First Refresh the AccessToken
            RefreshAccessToken(qboSettings, CompanyID);
            ServiceContext serviceContext = null;
            if (!string.IsNullOrWhiteSpace(qboSettings.AccessToken))
            {
                //Get the QBO ServiceContext

                OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(qboSettings.AccessToken);
                serviceContext = new ServiceContext(qboSettings.FileID, IntuitServicesType.QBO, oauthValidator);

                string QBOSandBox = ConfigurationManager.AppSettings["QBOSandBox"];

                if (QBOSandBox == "1")
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
                else
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";//prod

                serviceContext.IppConfiguration.MinorVersion.Qbo = "28";
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            }
            return serviceContext;
        }

        public void RefreshAccessToken(QBOSettins qboSettings, string CompanyID)
        {
            try
            {
                string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
                string clientID = ConfigurationManager.AppSettings["clientID"];
                string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
                string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];

                OAuth2Client client = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
                TokenResponse tokenResp = client.RefreshTokenAsync(qboSettings.RefreshToken).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(tokenResp.AccessToken))
                {
                    Database db = new Database();
                    //  db.Open();
                    string sql = "Update [myServiceJobs].[dbo].[QBOCompanySettings] set " +
                                         " AccessToken = '" + tokenResp.AccessToken + "'," +
                                         " RefreshToken = '" + tokenResp.RefreshToken + "'" +
                                         " Where CompanyID = '" + CompanyID + "'";
                    db.Execute(sql);
                    db.Close();

                    qboSettings.AccessToken = tokenResp.AccessToken;
                    qboSettings.RefreshToken = tokenResp.RefreshToken;
                }
                else
                {
                    qboSettings.AccessToken = "";
                    qboSettings.RefreshToken = "";
                }
            }
            catch (Exception ex)
            {
                qboSettings.AccessToken = "";
                qboSettings.RefreshToken = "";
            }

        }

        public CompanyInfo GetQboCompanyInfo(ServiceContext serviceContext)
        {
            CompanyInfo companyInfo = null;
            try
            {

                QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
                companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").FirstOrDefault();
            }
            catch { }

            return companyInfo;
        }

        public bool SaveCompanySetting(string CompanyID, string AccessToken, string RefreshToken, string BQOFileID)
        {
            bool bRetVal = false;
            Database db = new Database();
            string Sql = "";
            try
            {
                Sql = "Delete From [myServiceJobs].[dbo].[QBOCompanySettings] Where CompanyID = '" + CompanyID + "';";
                Sql += "INSERT INTO [myServiceJobs].[dbo].[QBOCompanySettings] " +
                      "(CompanyID,UserID,AccessToken,RefreshToken,BQOFileID) " +
                      " VALUES (" +
                      "'" + CompanyID + "'," +
                      "'" + HttpContext.Current.Session["LoginUser"].ToString() + "'," +
                      "'" + AccessToken + "'," +
                      "'" + RefreshToken + "'," +
                      "'" + BQOFileID + "');";

                db.Execute(Sql);

                bRetVal = true;
            }
            catch (Exception ex)
            {
                HttpContext.Current.Response.Write(Sql);
            }
            db.Close();

            return bRetVal;
        }
        public bool DeleteCompanySetting(string CompanyID)
        {

            string clientId = ConfigurationManager.AppSettings["clientId"];
            string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
            string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];

            bool bRetVal = false;
            try
            {
                QBOSettins qbs = new QBOSettins();
                QBOManager qBOManager = new QBOManager();
                bool IsQuickBookEnabled = qBOManager.VerifyCompanySetting(CompanyID, ref qbs);
                //  ServiceContext context = QBOManager.GetServiceContext(qbs, CompanyID);
                OAuth2Client oauthClient = new OAuth2Client(clientId, clientSecret, redirectURI, appEnvironment);
                var tokenResp = oauthClient.RevokeTokenAsync(qbs.RefreshToken).GetAwaiter().GetResult();

            }
            catch
            {}

            try
            {
                Database db = new Database();
                //  db.Open();
                string Sql = "Delete From [myServiceJobs].[dbo].[QBOCompanySettings] Where CompanyID = '" + CompanyID + "'";
                db.Execute(Sql);
                bRetVal = true;
                db.Close();
            }
            catch { }

            return bRetVal;
        }

        // Item Synchronization
        public bool ItemSynchronization(QBOSettins qboSettings, string CompanyID, ServiceContext context, string QboLastUpdatedTime)
        {
            try
            {
                Int32 STARTPOSITION = 1;
                Int32 MAXRESULTS = 1000;
                for (int i = 1; i < 1000000; i++)
                {
                    string qboQuery = "Select * From Item Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(context);
                    List<Intuit.Ipp.Data.Item> listItems = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Item>();

                    STARTPOSITION = i * MAXRESULTS;
                    if (listItems.Count > 0)
                    {
                        UpdateInsertLocalItemToQBO(CompanyID, ConfigurationManager.AppSettings["ConnString"].ToString(), listItems, context);

                        if (listItems.Count < MAXRESULTS)
                        {
                            if (Convert.ToBoolean(HttpContext.Current.Session["IsLHG"]))
                            {
                                SyncItemToQBO(qboSettings, CompanyID, context);
                            }
                            i = 1000000;
                            return true;
                        }
                    }
                    else
                    {
                        if (Convert.ToBoolean(HttpContext.Current.Session["IsLHG"]))
                        {
                            SyncItemToQBO(qboSettings, CompanyID, context);
                        }
                        i = 1000000;
                        return true;
                    }
                }
                return true;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                return false;
            }
        }

        private void UpdateInsertLocalItemToQBO(string CompanyID, string ConString, List<Intuit.Ipp.Data.Item> listItems, ServiceContext context)
        {
            try
            {
                Database db = new Database(ConString);
                //  db.Open();
                string Sql = "select Id,isnull(QboId,0) QboId from Items where CompanyID='" + CompanyID + "' and IsDeleted=0  ";
                DataTable dt;
                db.Execute(Sql, out dt);

                string InsertSql = string.Empty;
                bool isExist = false;
                foreach (Intuit.Ipp.Data.Item ilst in listItems)
                {
                    try
                    {
                        if (ilst.Active && ilst.Type != Intuit.Ipp.Data.ItemTypeEnum.Category)
                        {
                            isExist = false;
                            foreach (DataRow dr in dt.Rows)
                            {
                                if (dr["QboId"].ToString().Trim().ToUpper() == ilst.Id.Trim().ToUpper())
                                {
                                    try
                                    {

                                        if (Convert.ToUInt32(dr["QboId"].ToString()) == 0)
                                        {
                                            Sql = "update Items set QboId=" + ilst.Id + " Where CompanyID='" + CompanyID + "' and Id = '" + dr["Id"].ToString() + "'";
                                            db.Execute(Sql);
                                        }
                                        if (dr["QboId"].ToString() != ilst.Id.ToString())
                                        {
                                            Sql = "update Items set QboId=" + ilst.Id + " Where CompanyID='" + CompanyID + "' and Id = '" + dr["Id"].ToString() + "'";
                                            db.Execute(Sql);
                                        }
                                    }
                                    catch { }
                                    isExist = true;
                                }
                            }

                            if (!isExist)
                            {
                                string Taxable = ilst.Taxable == true ? "TRUE" : "FALSE";

                                string name = string.Empty;
                                if (ilst.Name != null)
                                {
                                    name = ilst.Name.Trim().Replace("'", "''");
                                }

                                string Description = string.Empty;
                                if (ilst.Description != null)
                                {
                                    Description = ilst.Description.Trim().Replace("'", "''");
                                }

                                int itemTypeId = GetItemTypeIdByName(ilst.Type.ToString());

                                InsertSql += "INSERT INTO Items" +
                                               " (Name, Description, Barcode, ItemTypeId, Price, IsTaxable,CompanyId,QboId, Quantity, Sku, IsDeleted,CreatedDate )" +
                                               " VALUES  ('" + name + "','" + Description + "','','"+ itemTypeId + "','" + Convert.ToDouble(ilst.UnitPrice) + "','" + Taxable + "','" +
                                               CompanyID + "'," + ilst.Id + ", '" + Convert.ToDouble(ilst.UnitPrice) + "', '" + ilst.Sku + "', 0 , GETDATE()); ";
                                //db.Execute(Sql);
                            }
                        }

                        // db.Close();
                    }
                    catch (Exception ex)
                    {

                    }

                }

                if (!string.IsNullOrEmpty(InsertSql))
                {
                    db.Execute(InsertSql);
                }


            }
            catch (Exception ex) { }
        }

        private void SyncItemToQBO(QBOSettins qboSettings, string CompanyID, ServiceContext context)
        {
            try
            {
                QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income).FirstOrDefault();
                Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());
                string Sql = @"select Id,Name,Description,Sku, IsTaxable,Price, Quantity from Items Where QboId='0'  and  [CompanyID] = @CompanyID";
                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                string InsertSql = string.Empty;
                if (dataSet.Tables[0] != null)
                {
                    foreach (DataRow dr in dataSet.Tables[0].Rows)
                    {
                        try
                        {
                            Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                            Itm.Name = Common.CleanInput(dr["Name"].ToString().Trim());
                            Itm.Description = Common.CleanInput(dr["Description"].ToString().Trim());
                            Itm.Taxable = Convert.ToBoolean(dr["IsTaxable"].ToString().Trim());
                            Itm.UnitPrice = decimal.Parse(dr["Price"].ToString().Trim());
                            Itm.TypeSpecified = true;
                            Itm.Type = ItemTypeEnum.Service;
                            Itm.Sku = Common.CleanInput(dr["Sku"].ToString().Trim());
                            
                            Itm.TrackQtyOnHand = false;
                            Itm.TrackQtyOnHandSpecified = false;
                            Itm.QtyOnHandSpecified = false;
                            Itm.QtyOnHand = 0;
                            //decimal.Parse(dr["Quantity"].ToString()); 

                            Itm.InvStartDateSpecified = true;
                            Itm.InvStartDate = DateTime.Now;
                            Itm.UnitPriceSpecified = true;
                            Itm.PurchaseDesc = "";
                            Itm.PurchaseCostSpecified = true;
                            Itm.PurchaseCost = 0;

                            if (AssetAccountRef != null)
                            {
                                Itm.IncomeAccountRef = new Intuit.Ipp.Data.ReferenceType();
                                Itm.IncomeAccountRef.Value = AssetAccountRef.Id;
                            }
                            DataService dataService = new DataService(context);
                            Intuit.Ipp.Data.Item Item = dataService.Add(Itm);

                            InsertSql += "update [msSchedulerV3].[dbo].[Items] set QboId=" + Item.Id + " Where CompanyID='" + CompanyID + "' and [Id] = '" + dr["Id"].ToString() + "';";

                        }
                        catch (Exception ex)
                        {
                            InsertSql += "update [msSchedulerV3].[dbo].[Items] set QboId=-1 Where CompanyID='" + CompanyID + "' and [Id] = '" + dr["Id"].ToString() + "';";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(InsertSql))
                {
                    db.Execute(InsertSql);
                }
            }
            catch (Exception ex)
            {}
        }


        public int GetItemTypeIdByName(string name)
        {
            string conString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
            Database db = new Database(conString);
            int id = 0;
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"Select Id from [myServiceJobs].[dbo].[ItemTypes] where Name = '"+ name + "'";
                db.Execute(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    DataRow dItem = dt.Rows[0];
                    id = Convert.ToInt32(dItem["Id"].ToString());
                }
            }
            catch (Exception ex)
            {
                return id;
            }
            finally
            {
                db.Close();
            }

            return id;
        }

        public string GetItemTypeNameById(int Id)
        {
            string conString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
            Database db = new Database(conString);
            string name = "";
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"Select Name from [myServiceJobs].[dbo].[ItemTypes] where Id = '" + Id + "'";
                db.Execute(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    DataRow dItem = dt.Rows[0];
                    name = dItem["Name"].ToString();
                }
            }
            catch (Exception ex)
            {
                return name;
            }
            finally
            {
                db.Close();
            }

            return name;
        }

        public void AccountTypeCheck(QBOSettins qboSettings, string CompanyID, string Id, ref string QboId)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
                //  db.Open();
                string Sql = @"select * from ItemTypes where Id='" + Id + "'";
                DataTable dt;
                db.Execute(Sql, out dt);

                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(dr["QboId"].ToString()))
                {
                    QboId = dr["QboId"].ToString();
                }
                else
                {
                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    string qboQuery = "select * from Account ";
                    QueryService<Intuit.Ipp.Data.Account> qsItem = new QueryService<Intuit.Ipp.Data.Account>(context);
                    List<Intuit.Ipp.Data.Account> listItem = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Account>();
                    foreach (Intuit.Ipp.Data.Account ilst in listItem)
                    {
                        if (ilst.Name == dr["Name"].ToString())
                        {
                            QboId = ilst.Id;
                        }
                    }

                    //Search Income Account if Not Found
                    if (string.IsNullOrEmpty(QboId))
                    {
                        foreach (Intuit.Ipp.Data.Account ilst in listItem)
                        {
                            if (ilst.Name == "Sales of Product Income")
                            {
                                QboId = ilst.Id;
                                break;
                            }
                        }
                        //Create Account if Not Found
                        if (string.IsNullOrEmpty(QboId))
                        {
                            Intuit.Ipp.Data.Account Itm = new Intuit.Ipp.Data.Account();
                            Itm.Name = "Sales of Product Income";
                            Itm.AccountTypeSpecified = true;
                            Itm.AccountType = AccountTypeEnum.Income;
                            Itm.statusSpecified = true;
                            DataService dataService = new DataService(context);
                            Intuit.Ipp.Data.Account itemAdd = dataService.Add(Itm);
                            QboId = itemAdd.Id;
                            Sql = "update ItemTypes set QboId=" + itemAdd.Id + " Where Id = '" + Id + "'";
                            db.Execute(Sql);
                        }
                    }
                }
                db.Close();
            }
            catch
            {
            }
        }

    }
}
