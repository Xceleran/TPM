using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Processors
{
    public class UserLogProcessor
    {
        public void AddLog(UserLog userLog)
        {
            try
            {
                Database db = new Database();

                string Sql = @"INSERT INTO [XinatorCentral].[dbo].[tbl_UserLog]
                                   ([CompanyID]
                                  ,[UserID]
                                  ,[Text])
                         VALUES
                               (@CompanyID
                                  ,@UserID
                                  ,@Text);";

                db.Command.CommandText = Sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", userLog.CompanyID);
                db.Command.Parameters.AddWithValue("@UserID", userLog.UserID);
                db.Command.Parameters.AddWithValue("@Text", userLog.Text);
                db.ExecuteCommand();
            }
            catch { }
        }
    }

    public class UserLog
    {
        public string id { get; set; }
        public string CompanyID { get; set; }
        public string UserID { get; set; }
        public string Text { get; set; }
    }
}
