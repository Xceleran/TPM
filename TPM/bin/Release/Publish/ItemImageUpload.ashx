using static System.Net.Mime.MediaTypeNames;
using System.IO;

using System;

<%@ WebHandler Language = "C#" Class="ItemImageUpload" %>

using System;
using System.Web;
using System.IO;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Drawing.Imaging;

public class ItemImageUpload : IHttpHandler, System.Web.SessionState.IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        try
        {
            // Check if file was uploaded
            if (context.Request.Files.Count == 0)
            {
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    status = "Error",
                    error = "No file uploaded"
                }));
                return;
            }

            HttpPostedFile file = context.Request.Files[0];
            string type = context.Request.Form["Type"] ?? "Item"; // Bundle, Group, or Item
            string id = context.Request.Form["Id"] ?? "";
            string companyId = context.Session["CompanyID"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(companyId))
            {
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    status = "Error",
                    error = "Company ID not found in session"
                }));
                return;
            }

            // Validate file
            if (file.ContentLength == 0)
            {
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    status = "Error",
                    error = "File is empty"
                }));
                return;
            }

            // Validate file size (max 5MB)
            if (file.ContentLength > 5 * 1024 * 1024)
            {
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    status = "Error",
                    error = "File size exceeds 5MB limit"
                }));
                return;
            }

            // Validate file type
            string extension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            if (Array.IndexOf(allowedExtensions, extension) == -1)
            {
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    status = "Error",
                    error = "Invalid file type. Only image files are allowed."
                }));
                return;
            }

            // Create upload directory if it doesn't exist
            string uploadPath = HttpContext.Current.Server.MapPath("~/Content/Images/Items/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            string fileName = $"{type}_{companyId}_{(string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id)}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            string filePath = Path.Combine(uploadPath, fileName);

            // Save file
            file.SaveAs(filePath);

            // Optionally resize image for optimization
            try
            {
                using (Image img = Image.FromFile(filePath))
                {
                    // Resize if larger than 800x800
                    if (img.Width > 800 || img.Height > 800)
                    {
                        int newWidth, newHeight;
                        if (img.Width > img.Height)
                        {
                            newWidth = 800;
                            newHeight = (int)(img.Height * (800.0 / img.Width));
                        }
                        else
                        {
                            newHeight = 800;
                            newWidth = (int)(img.Width * (800.0 / img.Height));
                        }

                        using (Bitmap resized = new Bitmap(newWidth, newHeight))
                        {
                            using (Graphics g = Graphics.FromImage(resized))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(img, 0, 0, newWidth, newHeight);
                            }

                            // Save resized image
                            resized.Save(filePath, ImageFormat.Jpeg);
                        }
                    }
                }
            }
            catch
            {
                // If resize fails, continue with original file
            }

            // Return image URL
            string imageUrl = $"/Content/Images/Items/{fileName}";

            context.Response.Write(new JavaScriptSerializer().Serialize(new
            {
                status = "Success",
                imageUrl = imageUrl,
                fileName = fileName
            }));
        }
        catch (Exception ex)
        {
            context.Response.Write(new JavaScriptSerializer().Serialize(new
            {
                status = "Error",
                error = ex.Message
            }));
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}
