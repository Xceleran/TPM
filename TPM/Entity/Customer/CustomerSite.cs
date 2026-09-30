using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Entity.Customer
{
    public class CustomerSite : CustomerBaseEntity
    {
        public int Id { get; set; }
        public string SiteName { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; } 
        public string PhoneNumber { get; set; } 
        public string Note { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string MobileNumber { get; set; }
        public Int32 TotalAppointment { get; set; }
        
    }

    public class Equipment : CustomerBaseEntity
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string Notes { get; set; }
        public string Make { get; set; }
        public int EquipmentTypeID { get; set; }
        public string EquipmentType { get; set; }
        public string SerialNumber { get; set; }
        public string Barcode { get; set; }
        public string Model { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string InstallDate { get; set; }

        public string WarrantyStart { get; set; }
        public string WarrantyEnd { get; set; }
        public string LaborWarrantyStart { get; set; }
        public string LaborWarrantyEnd { get; set; }
        public string CustomerName { get; set; }
    }

    // One page of a site's equipment. Paged because tbl_Equipment holds 79,243 rows on
    // Live and one site alone carries 31,718 of them.
    public class EquipmentPage
    {
        public List<Equipment> Equipment { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }

    public class EquipmentType
    {
        public string Id { get; set; }
        public string TypeName { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? UpdateDateTime { get; set; }
    }
}
