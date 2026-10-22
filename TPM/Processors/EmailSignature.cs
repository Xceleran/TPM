using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.Web;

namespace CECPro.Processor
{
    /// <summary>
    /// Company email signature: an image uploaded on Company.aspx, saved in ~/CompanySignature/
    /// and named in tbl_Company.EmailSignatureFile. A sender adds HtmlRow() to the end of its
    /// body table and attaches CreateResource() to the HTML view, the same way the logo is
    /// embedded as cid:photo.
    /// </summary>
    public static class EmailSignature
    {
        public const string Folder = "~/CompanySignature/";
        public const string ContentId = "signature";

        /// <summary>
        /// Physical path of the company's signature image, or "" when none is set, the file is
        /// missing, or the EmailSignatureFile column has not been deployed yet.
        /// </summary>
        public static string GetFilePath(string companyId)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                DataSet ds = db.Get_DataSet(
                    "SELECT EmailSignatureFile FROM [msSchedulerV3].[dbo].[tbl_Company] WHERE CompanyID = @CompanyID;",
                    companyId);

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                    return "";

                string fileName = Convert.ToString(ds.Tables[0].Rows[0]["EmailSignatureFile"]);
                if (string.IsNullOrWhiteSpace(fileName))
                    return "";

                string path = HttpContext.Current.Server.MapPath(Folder + fileName);
                return File.Exists(path) ? path : "";
            }
            catch
            {
                // A missing signature must never stop the email itself from going out.
                return "";
            }
        }

        /// <summary>Table row that shows the signature; the body must be inside a &lt;table&gt;.</summary>
        public static string HtmlRow()
        {
            return "<tr><td align='left' style='padding-left:5px;'><br/>" +
                   "<img src=\"cid:" + ContentId + "\" alt='' style='max-width:400px;' /></td></tr>";
        }

        public static LinkedResource CreateResource(string path)
        {
            return new LinkedResource(path, MimeTypeFor(path)) { ContentId = ContentId };
        }

        private static string MimeTypeFor(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                default: return "image/jpeg";
            }
        }
    }
}
