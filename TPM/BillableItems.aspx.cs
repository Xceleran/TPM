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

namespace FSM
{
    public partial class BillableItems : System.Web.UI.Page
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
                SetCecSsoUrl();
            }

            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            companyId.Attributes.Add("value", companyid);
        }

        private void SetCecSsoUrl()
        {
            try
            {
                string userId = Session["LoginUser"] as string;
                string companyId = Session["CompanyID"] as string;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                {
                    spnAddNew.Attributes["style"] = "display:none;";
                    return;
                }

                string sessionString = $"{userId}|{companyId}";
                string newGuid = Guid.NewGuid().ToString();

                string sql = $"INSERT INTO XinatorCentral.dbo.tbl_Login (SessionGuid, SessionString) VALUES ('{newGuid}', '{sessionString}')";

                Database db = new Database();
                db.UpdateSql(sql);

                string accountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"];
                if (string.IsNullOrEmpty(accountsUrl))
                {
                    spnAddNew.Attributes["style"] = "display:none;";
                    return;
                }

                string cecBaseUrl = accountsUrl.Replace("AccountsXinator", "cec");
                string redirectUrl = HttpUtility.UrlEncode("/cec/settings/items.aspx");

                spnAddNew.HRef = $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating CEC SSO URL: " + ex.Message);
                spnAddNew.Attributes["style"] = "display:none;";
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ItemTypes> GetItemTypes()
        {
            var items = new List<ItemTypes>();
            Database db = new Database(connStrJobs);
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"Select Id, Name, QboId, ImageUrl from [myServiceJobs].[dbo].[ItemTypes]";
                db.Execute(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var itemType = new ItemTypes();
                        itemType.Id = Convert.ToInt32(row["Id"].ToString());
                        itemType.Name = row.Field<string>("Name") ?? "";
                        itemType.QboId = row.Field<string>("QboId") ?? "";
                        itemType.ImageUrl = row.Field<string>("ImageUrl") ?? "";
                        items.Add(itemType);
                    }
                }
            }
            catch (Exception ex)
            {
                return items;
            }
            finally
            {
                db.Close();
            }
            return items;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Item> GetBillableItems()
        {
            var items = new List<Item>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetBillableItems: CompanyID is missing from session");
                return items;
            }
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                // Check if QboType column exists, if not use 0 as default
                string sql = @"select Id,Name as ItemName, ItemTypeId, Description, Location, Sku, Quantity, QboId,(case when IsTaxable = 'FALSE' then 'NO' else 'YES' end )as IsTaxable, Price, 
               ISNULL(QboType, 0) as QboType, 
               ISNULL(ImageUrl, '') as ImageUrl,
               (case when ISNULL(QboType, 0) = 8 then 1 else 0 end) as IsBundle
               from [msSchedulerV3].[dbo].[Items] where IsDeleted = 0 and CompanyId = @CompanyId order by ItemName;";

                db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetBillableItems: Query executed. Found {dt?.Rows?.Count ?? 0} rows for CompanyID: {companyid}");

                db.Close();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var item = new Item();
                        item.CompanyID = companyid;
                        item.Id = row.Field<string>("Id") ?? "";
                        item.ItemName = row.Field<string>("ItemName") ?? "";
                        item.QboId = row.Field<decimal>("QboId").ToString();
                        item.Description = row.Field<string>("Description") ?? "";
                        item.Taxable = row.Field<string>("IsTaxable") ?? "";
                        item.Location = row.Field<string>("Location") ?? "";
                        item.Sku = row.Field<string>("Sku") ?? "";
                        item.Price = row.Field<decimal>("Price");
                        var quantityValue = row["Quantity"];
                        item.Quantity = quantityValue != DBNull.Value ? Convert.ToDecimal(quantityValue) : 0;
                        item.ItemTypeId = Convert.ToInt32(row["ItemTypeId"].ToString());
                        item.QboType = row["QboType"] != DBNull.Value ? Convert.ToInt32(row["QboType"]) : 0;
                        item.ImageUrl = row["ImageUrl"] != DBNull.Value ? row.Field<string>("ImageUrl") : "";
                        item.IsGroup = false; // Groups will be handled separately
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

        private static bool InsertItemType(ItemTypes itemTypeData)
        {
            Database db = new Database(connStrJobs);
            try
            {
                db.Open();
                string sql = "INSERT INTO [myServiceJobs].[dbo].[ItemTypes] (Name, ImageUrl) VALUES (@Name, @ImageUrl)";
                db.AddParameter("@Name", itemTypeData.Name, SqlDbType.NVarChar);
                db.AddParameter("@ImageUrl", (object)itemTypeData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);
                return db.UpdateSql(sql);
            }
            catch (Exception ex)
            {
                // Log ex.Message
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        private static bool UpdateItemType(ItemTypes itemTypeData)
        {
            Database db = new Database(connStrJobs);
            try
            {
                db.Open();
                string sql = "UPDATE [myServiceJobs].[dbo].[ItemTypes] SET Name = @Name, ImageUrl = @ImageUrl WHERE Id = @Id";
                db.AddParameter("@Name", itemTypeData.Name, SqlDbType.NVarChar);
                db.AddParameter("@ImageUrl", (object)itemTypeData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);
                db.AddParameter("@Id", itemTypeData.Id, SqlDbType.Int);
                return db.UpdateSql(sql);
            }
            catch (Exception ex)
            {
                // Log ex.Message
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteItemType(int itemTypeId)
        {
            Database db = new Database(connStrJobs);
            try
            {
                string sql = "DELETE FROM [myServiceJobs].[dbo].[ItemTypes] WHERE Id = @Id";
                db.AddParameter("@Id", itemTypeId, SqlDbType.Int);
                return db.UpdateSql(sql);
            }
            catch (Exception ex)
            {
                // Log ex.Message
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Item GetItemById(string itemId)
        {
            var item = new Item();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                Database db = new Database();
                try
                {
                    db.Open();
                    DataTable dt = new DataTable();
                    string sql = "select * from [msSchedulerV3].[dbo].[Items] where Id='" + itemId + "' and CompanyId= '" + companyid + "';";
                    db.Execute(sql, out dt);
                    db.Close();
                    if (dt.Rows.Count > 0)
                    {
                        DataRow dItem = dt.Rows[0];
                        item.CompanyID = companyid;
                        item.Id = dItem.Field<string>("Id") ?? "";
                        item.QboId = dItem.Field<decimal>("QboId").ToString();
                        item.ItemName = dItem.Field<string>("Name") ?? "";
                        item.Description = dItem.Field<string>("Description") ?? "";
                        item.IsTaxable = bool.Parse(dItem["IsTaxable"].ToString());
                        item.Location = dItem.Field<string>("Location") ?? "";
                        item.Price = dItem.Field<decimal>("Price");
                        item.Sku = dItem.Field<string>("Sku") ?? "";
                        var quantityValue = dItem["Quantity"];
                        item.Quantity = quantityValue != DBNull.Value ? Convert.ToDecimal(quantityValue) : 0;
                        item.ItemTypeId = Convert.ToInt32(dItem["ItemTypeId"].ToString());
                        item.QboType = dItem["QboType"] != DBNull.Value ? Convert.ToInt32(dItem["QboType"]) : 0;
                        item.ImageUrl = dItem["ImageUrl"] != DBNull.Value ? dItem.Field<string>("ImageUrl") : "";
                    }
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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveItem(Item itemData)
        {
            if (!string.IsNullOrWhiteSpace(itemData.Id))
            {
                return UpdateItemData(itemData);
            }
            else
            {
                return InsertItemData(itemData);
            }
        }

        public static bool InsertItemData(Item itemData)
        {
            Database db = new Database(connStr);
            try
            {
                string checkQuery = "SELECT COUNT(*) FROM Items WHERE Name=@Name AND CompanyID=@CompanyID AND IsDeleted=0";
                db.AddParameter("@Name", Common.CleanInput(itemData.ItemName), SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", itemData.CompanyID, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(checkQuery, out dt);

                if (dt.Rows[0][0].ToString() != "0")
                {
                    throw new ApplicationException("An item with this name already exists.");
                }

                int QboItemId = 0;
                try
                {
                    QBSaveItem(ref QboItemId, itemData);
                    itemData.QboId = QboItemId.ToString();
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("QuickBooks Sync Error: " + ex.Message);
                }

                string newId = Guid.NewGuid().ToString();
                itemData.Id = newId;

                string strSQL = @"INSERT INTO [msSchedulerV3].[dbo].[Items]
                (Id, QboId, CompanyId, Name, Description, Sku, Location, Price, Quantity, ItemTypeId, IsTaxable, IsDeleted, CreatedDate, QboType, ImageUrl)
                VALUES (@Id, @QboId, @CompanyId, @Name, @Description, @Sku, @Location, @Price, @Quantity, @ItemTypeId, @IsTaxable, 0, GETDATE(), @QboType, @ImageUrl)";

                db.AddParameter("@Id", newId, SqlDbType.NVarChar);
                db.AddParameter("@QboId", itemData.QboId, SqlDbType.Decimal);
                db.AddParameter("@CompanyId", itemData.CompanyID, SqlDbType.NVarChar);
                db.AddParameter("@Name", itemData.ItemName, SqlDbType.NVarChar);
                db.AddParameter("@Description", itemData.Description, SqlDbType.NVarChar);
                db.AddParameter("@Sku", itemData.Sku, SqlDbType.NVarChar);
                db.AddParameter("@Location", itemData.Location, SqlDbType.NVarChar);
                db.AddParameter("@Price", itemData.Price, SqlDbType.Decimal);
                db.AddParameter("@Quantity", itemData.Quantity, SqlDbType.Decimal);
                db.AddParameter("@ItemTypeId", itemData.ItemTypeId, SqlDbType.Int);
                db.AddParameter("@IsTaxable", itemData.IsTaxable, SqlDbType.Bit);
                db.AddParameter("@QboType", itemData.QboType, SqlDbType.Int);
                db.AddParameter("@ImageUrl", (object)itemData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);

                bool success = db.UpdateSql(strSQL);

                if (success)
                {
                    if (itemData.QboType == 8)
                    {
                        DeleteSubItemsForGroup(newId);
                        if (itemData.BundleItems != null && itemData.BundleItems.Any())
                        {
                            SaveSubItemsForGroupWithQuantities(newId, itemData.BundleItems);
                        }
                        else if (itemData.SubItemIds != null && itemData.SubItemIds.Any())
                        {
                            // Fallback to old method if BundleItems not provided
                            SaveSubItemsForGroup(newId, itemData.SubItemIds);
                        }
                    }
                    SaveItemJobsDb(itemData);
                }

                return success;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                db.Close();
            }
        }


        private static bool SaveItemJobsDb(Item itemData)
        {
            bool success = false;
            Database db = new Database(connStrJobs);
            try
            {
                string id = Guid.NewGuid().ToString();
                if (itemData.CompanyID.All(char.IsNumber))
                {
                    string strSQL = @"INSERT INTO [myServiceJobs].[dbo].[Items]
                        (Id, QboId, CompanyId, Name, Description, Sku, Price, Quantity, ItemTypeId, IsTaxable, IsDeleted, CreatedDate) output INSERTED.ID
                        VALUES (@Id, @QboId, @CompanyId, @Name, @Description, @Sku, @Price, @Quantity, @ItemTypeId, @IsTaxable, @IsDeleted, GETDATE())";
                    db.AddParameter("@Name", itemData.ItemName ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Description", itemData.Description ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Sku", itemData.Sku ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Price", itemData.Price, SqlDbType.Decimal);
                    db.AddParameter("@Quantity", itemData.Quantity, SqlDbType.Decimal);
                    db.AddParameter("@ItemTypeId", itemData.ItemTypeId, SqlDbType.Int);
                    db.AddParameter("@Id", id, SqlDbType.NVarChar);
                    db.AddParameter("@QboId", itemData.QboId, SqlDbType.Decimal);
                    db.AddParameter("@IsTaxable", itemData.IsTaxable, SqlDbType.Bit);
                    db.AddParameter("@IsDeleted", 0, SqlDbType.Bit);
                    db.AddParameter("@CompanyId", itemData.CompanyID ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    object result = db.ExecuteScalarData(strSQL);
                    if (result != null)
                    {
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
            return success;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateItemData(Item itemData)
        {
            Database db = new Database(connStr);
            try
            {
                string strSQL = @"UPDATE [msSchedulerV3].[dbo].[Items] SET 
                            Name = @Name, Description = @Description, Sku = @Sku,
                            Quantity = @Quantity, Location = @Location, Price = @Price,
                            ItemTypeId = @ItemTypeId, IsTaxable = @IsTaxable,
                            QboType = @QboType, ImageUrl = @ImageUrl, ModifiedDate = GETDATE()
                          WHERE Id = @Id AND CompanyId = @CompanyId";

                db.AddParameter("@Name", itemData.ItemName, SqlDbType.NVarChar);
                db.AddParameter("@Description", itemData.Description, SqlDbType.NVarChar);
                db.AddParameter("@Sku", itemData.Sku, SqlDbType.NVarChar);
                db.AddParameter("@Location", itemData.Location, SqlDbType.NVarChar);
                db.AddParameter("@Price", itemData.Price, SqlDbType.Decimal);
                db.AddParameter("@Quantity", itemData.Quantity, SqlDbType.Decimal);
                db.AddParameter("@ItemTypeId", itemData.ItemTypeId, SqlDbType.Int);
                db.AddParameter("@IsTaxable", itemData.IsTaxable, SqlDbType.Bit);
                db.AddParameter("@QboType", itemData.QboType, SqlDbType.Int);
                db.AddParameter("@Id", itemData.Id, SqlDbType.NVarChar);
                db.AddParameter("@CompanyId", itemData.CompanyID, SqlDbType.NVarChar);

                bool success = db.UpdateSql(strSQL);

                if (success)
                {
                    if (itemData.QboType == 8)
                    {
                        DeleteSubItemsForGroup(itemData.Id);
                        if (itemData.SubItemIds != null && itemData.SubItemIds.Any())
                        {
                            SaveSubItemsForGroup(itemData.Id, itemData.SubItemIds);
                        }
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                db.Close();
            }
        }


        private static bool UpdateItemJobsDb(DataTable dtItems, Item itemData)
        {
            bool success = false;
            Database db = new Database(connStrJobs);
            try
            {
                string Sql = "select Id from Items where Name='" + dtItems.Rows[0]["Name"].ToString() + "' and CompanyID='" + itemData.CompanyID + "' ";
                DataTable dt;
                db.Execute(Sql, out dt);
                if (dt.Rows.Count > 0)
                {
                    string jId = dt.Rows[0]["Id"].ToString();
                    string strSQL = @"UPDATE [myServiceJobs].[dbo].[Items] SET 
                                    Name = @Name,
                                    Description = @Description,
                                    Sku = @Sku,
                                    Quantity = @Quantity,
                                    Price = @Price,
                                    ItemTypeId = @ItemTypeId,
                                    IsTaxable = @IsTaxable,
                                    ModifiedDate = GetDate()
                                    WHERE Id = @Id";
                    db.AddParameter("@Name", itemData.ItemName ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Description", itemData.Description ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Sku", itemData.Sku ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Price", itemData.Price, SqlDbType.Decimal);
                    db.AddParameter("@Quantity", itemData.Quantity, SqlDbType.Decimal);
                    db.AddParameter("@ItemTypeId", itemData.ItemTypeId, SqlDbType.Int);
                    db.AddParameter("@Id", jId ?? (object)DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@IsTaxable", itemData.IsTaxable, SqlDbType.Bit);
                    success = db.UpdateSql(strSQL);
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
            return success;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool AssignItemsToType(List<string> itemIds, int itemTypeId)
        {
            if (itemIds == null || !itemIds.Any())
            {
                Database db = new Database(connStr);
                try
                {
                    string unassignSql = @"UPDATE [msSchedulerV3].[dbo].[Items] 
                                   SET ItemTypeId = 0, ModifiedDate = GETDATE()
                                   WHERE ItemTypeId = @ItemTypeId";
                    db.AddParameter("@ItemTypeId", itemTypeId, SqlDbType.Int);
                    db.UpdateSql(unassignSql);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
                finally
                {
                    db.Close();
                }
            }

            var sanitizedIds = itemIds.Select(id => $"'{Common.CleanInput(id)}'").ToList();
            string idList = string.Join(",", sanitizedIds);

            Database updateDb = new Database(connStr);
            try
            {
                string strSQL = $@"UPDATE [msSchedulerV3].[dbo].[Items] 
                           SET ItemTypeId = @ItemTypeId, ModifiedDate = GETDATE()
                           WHERE Id IN ({idList})";

                updateDb.AddParameter("@ItemTypeId", itemTypeId, SqlDbType.Int);

                return updateDb.UpdateSql(strSQL);
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                updateDb.Close();
            }
        }


        private static bool QBUpdateItem(Item itemData)
        {
            string ItemId = itemData.Id;
            Database db = new Database(connStr);
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string query = "select QboId from Items where Id='" + ItemId + "' and CompanyID='" + CompanyID + "' ";
            DataTable dt;
            db.Execute(query, out dt);

            string QId = dt.Rows[0]["QboId"].ToString();

            if (!string.IsNullOrEmpty(QId) && QId != "0")
            {
                QBOSettins qBoStng = new QBOSettins();
                QBOManager qBOManager = new QBOManager();
                if (qBOManager.VerifyCompanySetting(CompanyID, ref qBoStng))
                {
                    try
                    {
                        ServiceContext serviceContext = qBOManager.GetServiceContext(qBoStng, CompanyID);
                        Intuit.Ipp.Data.Item objItemFound = null;
                        if (serviceContext != null)
                        {
                            QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(serviceContext);
                            string qboQuery = string.Format("select * from Item where Id = '" + QId + "'");
                            objItemFound = qsItem.ExecuteIdsQuery(qboQuery).FirstOrDefault<Intuit.Ipp.Data.Item>();
                        }

                        if (objItemFound != null)
                        {
                            string itemName = qBOManager.GetItemTypeNameById(itemData.ItemTypeId);
                            Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                            Itm.Id = objItemFound.Id;
                            Itm.SyncToken = objItemFound.SyncToken;
                            Itm.Name = itemData.ItemName;
                            Itm.Description = itemData.Description;
                            Itm.Taxable = itemData.IsTaxable;
                            Itm.UnitPrice = itemData.Price;
                            Itm.QtyOnHand = itemData.Quantity;
                            Itm.Sku = itemData.Sku;
                            //if (Enum.TryParse<ItemTypeEnum>(itemName, ignoreCase: true, out var itemType))
                            //{
                            //    Itm.Type = itemType;
                            //}
                            //else Itm.Type = objItemFound.Type;

                            Itm.TypeSpecified = objItemFound.TypeSpecified;
                            Itm.TrackQtyOnHand = objItemFound.TrackQtyOnHand;
                            Itm.TrackQtyOnHandSpecified = objItemFound.TrackQtyOnHandSpecified;
                            Itm.QtyOnHandSpecified = objItemFound.QtyOnHandSpecified;
                            Itm.InvStartDateSpecified = objItemFound.InvStartDateSpecified;
                            Itm.InvStartDate = objItemFound.InvStartDate;
                            Itm.UnitPriceSpecified = objItemFound.UnitPriceSpecified;
                            Itm.PurchaseDesc = objItemFound.PurchaseDesc;
                            Itm.PurchaseCostSpecified = objItemFound.PurchaseCostSpecified;
                            Itm.PurchaseCost = objItemFound.PurchaseCost;
                            Itm.AssetAccountRef = objItemFound.AssetAccountRef;
                            Itm.IncomeAccountRef = objItemFound.IncomeAccountRef;
                            Itm.ExpenseAccountRef = objItemFound.ExpenseAccountRef;

                            DataService dataService = new DataService(serviceContext);
                            Intuit.Ipp.Data.Item UpdateEntity = dataService.Update<Intuit.Ipp.Data.Item>(Itm);
                        }
                        return true;
                    }
                    catch (Intuit.Ipp.Exception.IdsException ex)
                    {
                        string errDetail = "";
                        var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                        if (innerException != null)
                        {
                            errDetail = innerException.Detail;
                        }
                        return false;
                    }
                }
                else
                    return false;
            }
            else return false;
        }

        [WebMethod(EnableSession = false)]
        public static string SyncQBOItems()
        {
            bool saveStat = false;
            try
            {
                string QboLastUpdatedTime = string.Empty;
                string Sql = @"SELECT QboLastUpdatedTime FROM [msSchedulerV3].[dbo].[tbl_Company]  Where [CompanyID] = '" + HttpContext.Current.Session["CompanyID"].ToString() + "'";
                Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());
                DataTable dt = new DataTable();
                QboLastUpdatedTime = db.ExecuteScalarString(Sql);
                if (string.IsNullOrEmpty(QboLastUpdatedTime))
                {
                    QboLastUpdatedTime = "1990-01-01T00:00:00";
                }
                QboLastUpdatedTime = Convert.ToDateTime(QboLastUpdatedTime).ToString("yyyy-MM-ddTHH:mm:ss");

                QBOSettins qBoStngPost = new QBOSettins();
                QBOManager qBOManager = new QBOManager();
                if (qBOManager.VerifyCompanySetting(HttpContext.Current.Session["CompanyID"].ToString(), ref qBoStngPost))
                {
                    ServiceContext context = qBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());
                    saveStat = qBOManager.ItemSynchronization(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), context, QboLastUpdatedTime);
                }
                if (saveStat)
                    return "Items have been synchronized.";
                else
                    return "Item Synchronization failed.";
            }
            catch
            {
                return "Item Synchronization failed.";
            }
        }
        private static void SaveSubItemsForGroup(string groupId, List<string> subItemIds)
        {
            Database db = new Database(connStr);
            try
            {
                foreach (var subItemId in subItemIds)
                {
                    string sql = "INSERT INTO [msSchedulerV3].[dbo].[ItemGroupLinks] (GroupId, SubItemId, Quantity) VALUES (@GroupId, @SubItemId, @Quantity)";
                    db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                    db.AddParameter("@SubItemId", subItemId, SqlDbType.NVarChar);
                    db.AddParameter("@Quantity", 1, SqlDbType.Decimal); // Default quantity = 1
                    db.UpdateSql(sql);
                }
            }
            finally
            {
                db.Close();
            }
        }

        private static void SaveSubItemsForGroupWithQuantities(string groupId, List<ItemGroupLink> subItems)
        {
            Database db = new Database(connStr);
            try
            {
                foreach (var subItem in subItems)
                {
                    string sql = "INSERT INTO [msSchedulerV3].[dbo].[ItemGroupLinks] (GroupId, SubItemId, Quantity) VALUES (@GroupId, @SubItemId, @Quantity)";
                    db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                    db.AddParameter("@SubItemId", subItem.SubItemId, SqlDbType.NVarChar);
                    db.AddParameter("@Quantity", subItem.Quantity > 0 ? subItem.Quantity : 1, SqlDbType.Decimal);
                    db.UpdateSql(sql);
                }
            }
            finally
            {
                db.Close();
            }
        }

        private static void DeleteSubItemsForGroup(string groupId)
        {
            Database db = new Database(connStr);
            try
            {
                string sql = "DELETE FROM [msSchedulerV3].[dbo].[ItemGroupLinks] WHERE GroupId = @GroupId";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                db.UpdateSql(sql);
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<string> GetSubItemsForGroup(string groupId)
        {
            var subItemIds = new List<string>();
            Database db = new Database(connStr);
            try
            {
                string sql = "SELECT SubItemId FROM [msSchedulerV3].[dbo].[ItemGroupLinks] WHERE GroupId = @GroupId";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(sql, out dt);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        subItemIds.Add(row["SubItemId"].ToString());
                    }
                }
            }
            finally
            {
                db.Close();
            }
            return subItemIds;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ItemGroupLink> GetSubItemsForGroupWithQuantities(string groupId)
        {
            var subItems = new List<ItemGroupLink>();
            Database db = new Database(connStr);
            try
            {
                string sql = "SELECT SubItemId, Quantity FROM [msSchedulerV3].[dbo].[ItemGroupLinks] WHERE GroupId = @GroupId";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(sql, out dt);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var link = new ItemGroupLink
                        {
                            GroupId = groupId,
                            SubItemId = row["SubItemId"].ToString(),
                            Quantity = row["Quantity"] != DBNull.Value ? Convert.ToDecimal(row["Quantity"]) : 1
                        };
                        subItems.Add(link);
                    }
                }
            }
            finally
            {
                db.Close();
            }
            return subItems;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetBundleItemsForDisplay(string bundleId)
        {
            try
            {
                Database db = new Database(connStr);
                db.Open();
                string sql = @"SELECT i.Name, igl.Quantity 
                               FROM [msSchedulerV3].[dbo].[ItemGroupLinks] igl
                               INNER JOIN [msSchedulerV3].[dbo].[Items] i ON igl.SubItemId = i.Id
                               WHERE igl.GroupId = @BundleId AND i.IsDeleted = 0
                               ORDER BY i.Name";
                db.AddParameter("@BundleId", bundleId, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(sql, out dt);
                db.Close();

                if (dt.Rows.Count == 0)
                {
                    return "";
                }

                var items = new List<string>();
                foreach (DataRow row in dt.Rows)
                {
                    string itemName = row["Name"].ToString();
                    decimal qty = row["Quantity"] != DBNull.Value ? Convert.ToDecimal(row["Quantity"]) : 1;
                    items.Add($"{itemName} ({qty})");
                }

                return string.Join(", ", items);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        // =============================================
        // ItemGroup CRUD Methods
        // =============================================

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ItemGroup> GetItemGroups()
        {
            var groups = new List<ItemGroup>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            string companyIdInt = HttpContext.Current.Session["CompanyIdInt"]?.ToString();

            System.Diagnostics.Debug.WriteLine($"GetItemGroups: CompanyID from session: '{companyid}'");
            System.Diagnostics.Debug.WriteLine($"GetItemGroups: CompanyIdInt from session: '{companyIdInt}'");

            // Try to use CompanyIdInt first (numeric), fallback to CompanyID (string)
            string companyIdToUse = null;
            if (!string.IsNullOrEmpty(companyIdInt))
            {
                companyIdToUse = companyIdInt.Trim();
                System.Diagnostics.Debug.WriteLine($"GetItemGroups: Using CompanyIdInt: '{companyIdToUse}'");
            }
            else if (!string.IsNullOrEmpty(companyid))
            {
                companyIdToUse = companyid.Trim();
                System.Diagnostics.Debug.WriteLine($"GetItemGroups: Using CompanyID: '{companyIdToUse}'");
            }

            if (string.IsNullOrEmpty(companyIdToUse))
            {
                System.Diagnostics.Debug.WriteLine("GetItemGroups: Both CompanyID and CompanyIdInt are missing from session");
                return groups;
            }

            Database db = new Database(connStr);
            try
            {
                db.Open();

                // Build query based on whether companyIdToUse is numeric or not
                string sql;
                DataTable dt;

                if (int.TryParse(companyIdToUse, out int companyIdIntValue))
                {
                    // CompanyId is numeric - try both string and integer comparison
                    sql = @"SELECT Id, CompanyId, Name, Description, ImageUrl, CreatedDate, ModifiedDate, IsDeleted
                          FROM [msSchedulerV3].[dbo].[ItemGroups]
                          WHERE (LTRIM(RTRIM(CAST(CompanyId AS NVARCHAR(50)))) = @CompanyIdStr 
                             OR (ISNUMERIC(CompanyId) = 1 AND CAST(CompanyId AS INT) = @CompanyIdInt))
                            AND IsDeleted = 0
                          ORDER BY Name";
                    db.AddParameter("@CompanyIdStr", companyIdToUse, SqlDbType.NVarChar);
                    db.AddParameter("@CompanyIdInt", companyIdIntValue, SqlDbType.Int);
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: Searching for CompanyId: '{companyIdToUse}' (as string) or {companyIdIntValue} (as int)");
                }
                else
                {
                    // CompanyId is not numeric - only use string comparison
                    sql = @"SELECT Id, CompanyId, Name, Description, ImageUrl, CreatedDate, ModifiedDate, IsDeleted
                          FROM [msSchedulerV3].[dbo].[ItemGroups]
                          WHERE LTRIM(RTRIM(CAST(CompanyId AS NVARCHAR(50)))) = @CompanyIdStr 
                            AND IsDeleted = 0
                          ORDER BY Name";
                    db.AddParameter("@CompanyIdStr", companyIdToUse, SqlDbType.NVarChar);
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: Searching for CompanyId: '{companyIdToUse}' (as string only - not numeric)");
                }

                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetItemGroups: Query executed. Found {dt?.Rows?.Count ?? 0} groups for CompanyId: '{companyIdToUse}'");

                if (dt != null && dt.Rows.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: First row CompanyId value: '{dt.Rows[0]["CompanyId"]}' (Type: {dt.Rows[0]["CompanyId"].GetType().Name})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: No rows found. CompanyId searched: '{companyIdToUse}'");
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: Processing {dt.Rows.Count} rows");
                    foreach (DataRow row in dt.Rows)
                    {
                        var group = new ItemGroup
                        {
                            Id = row["Id"].ToString(),
                            CompanyId = row["CompanyId"].ToString(),
                            Name = row["Name"].ToString(),
                            Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "",
                            ImageUrl = row["ImageUrl"] != DBNull.Value ? row["ImageUrl"].ToString() : "",
                            CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.Now,
                            ModifiedDate = row["ModifiedDate"] != DBNull.Value ? Convert.ToDateTime(row["ModifiedDate"]) : DateTime.Now,
                            IsDeleted = row["IsDeleted"] != DBNull.Value && Convert.ToBoolean(row["IsDeleted"]),
                            ItemIds = new List<string>()
                        };

                        System.Diagnostics.Debug.WriteLine($"GetItemGroups: Processing group - Name: '{group.Name}', CompanyId: '{group.CompanyId}'");

                        // Get items in this group
                        group.ItemIds = GetGroupItemIds(group.Id);
                        groups.Add(group);
                    }
                    System.Diagnostics.Debug.WriteLine($"GetItemGroups: Total groups added to list: {groups.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("GetItemGroups: No rows returned from query. Checking what CompanyIds exist in database...");
                    // Try a direct query to see what CompanyIds exist
                    string checkSql = @"SELECT DISTINCT TOP 20 CompanyId FROM [msSchedulerV3].[dbo].[ItemGroups] WHERE IsDeleted = 0 ORDER BY CompanyId";
                    DataTable checkDt;
                    db.Execute(checkSql, out checkDt);
                    if (checkDt != null && checkDt.Rows.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetItemGroups: Found {checkDt.Rows.Count} distinct CompanyIds in database:");
                        foreach (DataRow checkRow in checkDt.Rows)
                        {
                            var dbCompanyId = checkRow["CompanyId"];
                            System.Diagnostics.Debug.WriteLine($"  - '{dbCompanyId}' (Type: {dbCompanyId.GetType().Name}, Value: {dbCompanyId})");
                        }
                        System.Diagnostics.Debug.WriteLine($"GetItemGroups: Searched for: '{companyIdToUse}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("GetItemGroups: No CompanyIds found in ItemGroups table at all.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetItemGroups: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                // Return empty list on error
                groups = new List<ItemGroup>();
            }
            finally
            {
                if (db != null)
                {
                    db.Close();
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetItemGroups: Returning {groups.Count} groups");
            return groups;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static ItemGroup GetItemGroupById(string groupId)
        {
            var group = new ItemGroup();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetItemGroupById: CompanyID is missing from session");
                return group;
            }
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string sql = @"SELECT Id, CompanyId, Name, Description, ImageUrl, CreatedDate, ModifiedDate, IsDeleted
                              FROM [msSchedulerV3].[dbo].[ItemGroups]
                              WHERE Id = @Id AND CompanyId = @CompanyId AND IsDeleted = 0";
                db.AddParameter("@Id", groupId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    group.Id = row["Id"].ToString();
                    group.CompanyId = row["CompanyId"].ToString();
                    group.Name = row["Name"].ToString();
                    group.Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "";
                    group.ImageUrl = row["ImageUrl"] != DBNull.Value ? row["ImageUrl"].ToString() : "";
                    group.CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.Now;
                    group.ModifiedDate = row["ModifiedDate"] != DBNull.Value ? Convert.ToDateTime(row["ModifiedDate"]) : DateTime.Now;
                    group.IsDeleted = row["IsDeleted"] != DBNull.Value && Convert.ToBoolean(row["IsDeleted"]);
                    group.ItemIds = GetGroupItemIds(group.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetItemGroupById: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                db.Close();
            }
            return group;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveItemGroup(ItemGroup groupData)
        {
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("SaveItemGroup: CompanyID is missing from session");
                return false;
            }
            Database db = new Database(connStr);
            try
            {
                db.Open();

                if (string.IsNullOrWhiteSpace(groupData.Id))
                {
                    // Insert new group
                    groupData.Id = Guid.NewGuid().ToString();
                    string sql = @"INSERT INTO [msSchedulerV3].[dbo].[ItemGroups] 
                                  (Id, CompanyId, Name, Description, ImageUrl, CreatedDate, IsDeleted)
                                  VALUES (@Id, @CompanyId, @Name, @Description, @ImageUrl, GETDATE(), 0)";
                    db.AddParameter("@Id", groupData.Id, SqlDbType.NVarChar);
                    db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                    db.AddParameter("@Name", groupData.Name, SqlDbType.NVarChar);
                    db.AddParameter("@Description", (object)groupData.Description ?? DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@ImageUrl", (object)groupData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);

                    bool success = db.UpdateSql(sql);
                    db.Close();

                    if (success && groupData.ItemIds != null && groupData.ItemIds.Any())
                    {
                        AssignItemsToGroup(groupData.Id, groupData.ItemIds);
                    }

                    return success;
                }
                else
                {
                    // Update existing group
                    string sql = @"UPDATE [msSchedulerV3].[dbo].[ItemGroups]
                                  SET Name = @Name, Description = @Description, ImageUrl = @ImageUrl, ModifiedDate = GETDATE()
                                  WHERE Id = @Id AND CompanyId = @CompanyId";
                    db.AddParameter("@Id", groupData.Id, SqlDbType.NVarChar);
                    db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                    db.AddParameter("@Name", groupData.Name, SqlDbType.NVarChar);
                    db.AddParameter("@Description", (object)groupData.Description ?? DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@ImageUrl", (object)groupData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);

                    bool success = db.UpdateSql(sql);
                    db.Close();

                    if (success && groupData.ItemIds != null)
                    {
                        // Delete existing assignments
                        DeleteGroupAssignments(groupData.Id);
                        // Add new assignments
                        if (groupData.ItemIds.Any())
                        {
                            AssignItemsToGroup(groupData.Id, groupData.ItemIds);
                        }
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveItemGroup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteItemGroup(string groupId)
        {
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("DeleteItemGroup: CompanyID is missing from session");
                return false;
            }
            Database db = new Database(connStr);
            try
            {
                db.Open();
                // Soft delete
                string sql = @"UPDATE [msSchedulerV3].[dbo].[ItemGroups]
                              SET IsDeleted = 1, ModifiedDate = GETDATE()
                              WHERE Id = @Id AND CompanyId = @CompanyId";
                db.AddParameter("@Id", groupId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                bool success = db.UpdateSql(sql);
                db.Close();
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteItemGroup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool AssignItemsToGroup(string groupId, List<string> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("AssignItemsToGroup: No items to assign");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Assigning {itemIds.Count} items to group {groupId}");

            int successCount = 0;
            foreach (var itemId in itemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Skipping empty itemId");
                    continue;
                }

                // Create a new Database instance for each insert to avoid parameter conflicts
                Database db = new Database(connStr);
                try
                {
                    db.Open();
                    // Clear any existing parameters
                    db.Command.Parameters.Clear();

                    string sql = @"INSERT INTO [msSchedulerV3].[dbo].[ItemGroupAssignments] 
                                  (GroupId, ItemId, CreatedDate)
                                  VALUES (@GroupId, @ItemId, GETDATE())";
                    db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                    db.AddParameter("@ItemId", itemId, SqlDbType.NVarChar);
                    bool success = db.UpdateSql(sql);
                    if (success)
                    {
                        successCount++;
                        System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Successfully inserted item {itemId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Failed to insert item {itemId}");
                    }
                    db.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Error inserting item {itemId}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    db.Close();
                    // Continue with next item instead of failing completely
                }
                finally
                {
                    if (db != null)
                    {
                        db.Close();
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"AssignItemsToGroup: Completed. Successfully assigned {successCount} out of {itemIds.Count} items to group {groupId}");
            return successCount > 0;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Item> GetGroupItems(string groupId)
        {
            var items = new List<Item>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string sql = @"SELECT i.Id, i.Name as ItemName, i.ItemTypeId, i.Description, i.Location, i.Sku, 
                              i.Quantity, i.QboId, (case when i.IsTaxable = 'FALSE' then 'NO' else 'YES' end) as IsTaxable, 
                              i.Price, i.QboType, i.ImageUrl
                              FROM [msSchedulerV3].[dbo].[ItemGroupAssignments] iga
                              INNER JOIN [msSchedulerV3].[dbo].[Items] i ON iga.ItemId = i.Id
                              WHERE iga.GroupId = @GroupId AND i.IsDeleted = 0 AND i.CompanyId = @CompanyId
                              ORDER BY i.Name";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                DataTable dt;
                db.Execute(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var item = new Item();
                        item.CompanyID = companyid;
                        item.Id = row["Id"].ToString();
                        item.ItemName = row["ItemName"].ToString();
                        item.QboId = row["QboId"] != DBNull.Value ? row.Field<decimal>("QboId").ToString() : "0";
                        item.Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "";
                        item.Taxable = row["IsTaxable"].ToString();
                        item.Location = row["Location"] != DBNull.Value ? row["Location"].ToString() : "";
                        item.Sku = row["Sku"] != DBNull.Value ? row["Sku"].ToString() : "";
                        item.Price = row["Price"] != DBNull.Value ? row.Field<decimal>("Price") : 0;
                        var quantityValue = row["Quantity"];
                        item.Quantity = quantityValue != DBNull.Value ? Convert.ToDecimal(quantityValue) : 0;
                        item.ItemTypeId = Convert.ToInt32(row["ItemTypeId"].ToString());
                        item.QboType = row["QboType"] != DBNull.Value ? Convert.ToInt32(row["QboType"]) : 0;
                        item.ImageUrl = row["ImageUrl"] != DBNull.Value ? row["ImageUrl"].ToString() : "";
                        item.IsGroup = false;
                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
            finally
            {
                db.Close();
            }
            return items;
        }

        private static List<string> GetGroupItemIds(string groupId)
        {
            var itemIds = new List<string>();
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string sql = "SELECT ItemId FROM [msSchedulerV3].[dbo].[ItemGroupAssignments] WHERE GroupId = @GroupId";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                DataTable dt;
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetGroupItemIds: Found {dt?.Rows?.Count ?? 0} items for GroupId: {groupId}");

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        itemIds.Add(row["ItemId"].ToString());
                    }
                }
                db.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetGroupItemIds: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                db.Close();
            }
            return itemIds;
        }

        private static void DeleteGroupAssignments(string groupId)
        {
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string sql = "DELETE FROM [msSchedulerV3].[dbo].[ItemGroupAssignments] WHERE GroupId = @GroupId";
                db.AddParameter("@GroupId", groupId, SqlDbType.NVarChar);
                db.UpdateSql(sql);
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static int SaveItemType(ItemTypes itemTypeData)
        {
            Database db = new Database(connStrJobs);
            try
            {
                if (itemTypeData.Id > 0)
                {
                    // Update
                    string sql = "UPDATE [myServiceJobs].[dbo].[ItemTypes] SET Name = @Name, ImageUrl = @ImageUrl WHERE Id = @Id";
                    db.AddParameter("@Name", itemTypeData.Name, SqlDbType.NVarChar);
                    db.AddParameter("@ImageUrl", (object)itemTypeData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);
                    db.AddParameter("@Id", itemTypeData.Id, SqlDbType.Int);
                    return db.UpdateSql(sql) ? itemTypeData.Id : 0;
                }
                else
                {
                    // Insert
                    string sql = "INSERT INTO [myServiceJobs].[dbo].[ItemTypes] (Name, ImageUrl) OUTPUT INSERTED.Id VALUES (@Name, @ImageUrl)";
                    db.AddParameter("@Name", itemTypeData.Name, SqlDbType.NVarChar);
                    db.AddParameter("@ImageUrl", (object)itemTypeData.ImageUrl ?? DBNull.Value, SqlDbType.NVarChar);
                    object result = db.ExecuteScalar(sql);
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                db.Close();
            }
        }

        private static bool QBSaveItem(ref int ItemId, Item itemData)
        {
            string cId = itemData.CompanyID;
            QBOSettins qBoStng = new QBOSettins();
            QBOManager qBOManager = new QBOManager();
            if (qBOManager.VerifyCompanySetting(cId, ref qBoStng))
            {
                try
                {
                    ServiceContext serviceContext = qBOManager.GetServiceContext(qBoStng, cId);
                    string qboQuery = "select * from Item ";
                    QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(serviceContext);
                    List<Intuit.Ipp.Data.Item> listItems = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Item>();

                    bool isExists = false;
                    foreach (Intuit.Ipp.Data.Item ilst in listItems)
                    {
                        if (ilst.Name == itemData.ItemName)
                        {
                            ItemId = Convert.ToInt16(ilst.Id);
                            isExists = true;
                        }
                    }
                    if (!isExists)
                    {
                        Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                        string itemName = qBOManager.GetItemTypeNameById(itemData.ItemTypeId);
                        Itm.Name = itemData.ItemName;
                        Itm.Description = itemData.Description;
                        Itm.Taxable = itemData.IsTaxable;
                        Itm.UnitPrice = itemData.Price;
                        Itm.TypeSpecified = true;
                        //if (Enum.TryParse<ItemTypeEnum>(itemName, ignoreCase: true, out var itemType))
                        //{
                        //    Itm.Type = itemType;
                        //}
                        //else
                        //    Itm.Type = ItemTypeEnum.Service;

                        Itm.Sku = itemData.Sku;
                        Itm.TrackQtyOnHand = false;
                        Itm.TrackQtyOnHandSpecified = false;
                        Itm.QtyOnHandSpecified = false;
                        Itm.QtyOnHand = 0;
                        Itm.InvStartDateSpecified = true;
                        Itm.InvStartDate = DateTime.Now;
                        Itm.UnitPriceSpecified = true;
                        Itm.PurchaseDesc = "";
                        Itm.PurchaseCostSpecified = true;
                        Itm.PurchaseCost = 0;

                        string AccTypeId = "";
                        qBOManager.AccountTypeCheck(qBoStng, cId, itemData.ItemTypeId.ToString(), ref AccTypeId);
                        Itm.IncomeAccountRef = new ReferenceType();
                        Itm.IncomeAccountRef.Value = AccTypeId;

                        DataService dataService = new DataService(serviceContext);
                        Intuit.Ipp.Data.Item Item = dataService.Add(Itm);
                        ItemId = Convert.ToInt16(Item.Id);
                    }
                    return true;
                }
                catch (Intuit.Ipp.Exception.IdsException ex)
                {
                    string errDetail = "";
                    var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                    if (innerException != null)
                    {
                        errDetail = innerException.Detail;
                        throw new ApplicationException(innerException.Detail);
                    }
                    return false;
                }
            }
            else
                return false;
        }
    }

    public class Item
    {
        public string CompanyID { get; set; }
        public string Id { get; set; }
        public string QboId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Barcode { get; set; }
        public string Sku { get; set; }
        public decimal Quantity { get; set; }
        public string Taxable { get; set; }
        public bool IsTaxable { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public int ItemTypeId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public int QboType { get; set; }
        public List<string> SubItemIds { get; set; }
        public List<ItemGroupLink> BundleItems { get; set; }
        public string ImageUrl { get; set; }
        public bool IsGroup { get; set; }
    }
    public class ItemGroupLink
    {
        public string GroupId { get; set; }
        public string SubItemId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class ItemGroup
    {
        public string Id { get; set; }
        public string CompanyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> ItemIds { get; set; }
    }

    public class ItemTypes
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string QboId { get; set; }
        public string ImageUrl { get; set; }
    }

    public enum ItemTypeFSMEnum
    {
        Group,
        Inventory,
        NonInventory = 3,
        OtherCharge = 4,
        Payment,
        Service = 1,
        Bundle
    }
}