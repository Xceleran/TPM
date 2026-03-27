var titleData;

loadHeaderForDropDowns();
getCustomerSitesList();
getCustomerBillingAddress();
getCustomerNotes();
function getCustomerBillingAddress() {
    var CustomerID = $('#hdCustomerGUID').val();
    $.ajax({
        url: "TpDetail.aspx/GetCustomerBillingAddress",
        type: 'POST',
        data: "{customerID: '" + CustomerID + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        success: function (response) {
            const data = response.d;
            $('#custAddress').empty();

            if (data.length > 0) {
                // Table structure for CSL data
                var table = `
                    <div class="table-responsive">
                        <table class="table table-striped table-bordered" id="custAddressTable" style="width: 100%;">
                            <thead class="thead-light">
                                <tr>
                                    <th>Address</th>
                                    <th>City</th>
                                    <th>Zip Code</th>
                                    <th>State</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                `;

                // Populating table rows
                for (var i = 0; i < data.length; i++) {
                    table += `
                        <tr>
                            <td>${data[i].Address}</td>
                            <td>${data[i].City}</td>
                            <td>${data[i].ZipCode}</td>
                            <td>${data[i].State}</td>
                           <td class="text-center">
                                <button class="btn btn-primary btn-sm edit-address-btn" type="button" data-address-id="${data[i].ID}">Edit</button>
                            </td>
                        </tr>
                    `;
                }

                // Table structure ends here
                table += `
                            </tbody>
                        </table>
                    </div>
                `;

                // Appending table to the sites container
                $('#custAddress').append(table);

                // Initializing DataTable for better interactivity
                $('#custAddressTable').DataTable({
                    paging: true,
                    searching: true,
                    ordering: true,
                    responsive: true,
                    destroy: true
                });

                // Bind click event to edit buttons using delegation
                $('#custAddress').on('click', '.edit-address-btn', function () {
                    var addressId = $(this).data('address-id');
                    getCustomerBillAddressDataById(addressId);
                });
              
            } else {
                // Optional message if no data is available
                $('#custAddress').append('<p>No address found.</p>');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error fetching sites: ", error);
            $('#custAddress').append('<p>Error loading sites.</p>');
        }
    });
}

//Note Releted Code
function getCustomerNotes() {
    var CustomerID = $('#hdCustomerGUID').val();

    $.ajax({
        url: "TpDetail.aspx/CustomerNotes",
        type: 'POST',
        data: "{customerGuid: '" + CustomerID + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        success: function (response) {

            const data = response.d;
            $('#addNoteContent').empty();

            if (data.length > 0) {

                var table = `
                    <div class="table-responsive">
                        <table class="table table-striped table-bordered" id="customerNotesTable" style="width: 100%;">
                            <thead class="thead-light">
                                <tr>
                                    <th>Note</th>
                                    <th>Created Date</th>
                                    <th>Added By</th>
                                </tr>
                            </thead>
                            <tbody>
                `;

                // Populate rows
                for (var i = 0; i < data.length; i++) {
                    table += `
                        <tr>
                            <td>${data[i].Description}</td>
                         <td>${convertJsonDate(data[i].CreatedAt)}</td>
                            <td>${data[i].UserId}</td>
                          
                        </tr>
                    `;
                }

                table += `
                            </tbody>
                        </table>
                    </div>
                `;

                $("#addNoteContent").append(table);

                // Initialize datatable
                $('#customerNotesTable').DataTable({
                    paging: true,
                    searching: true,
                    ordering: true,
                    responsive: true,
                    destroy: true
                });

                // Edit button click event
                $('#addNoteContent').on('click', '.edit-note-btn', function () {
                    var noteId = $(this).data('note-id');
                    getCustomerNoteById(noteId); // your edit function
                });

            } else {
                $('#addNoteContent').append('<p>No notes found.</p>');
            }
        },

        error: function (xhr, status, error) {
            console.error("Error loading notes: ", error);
            $('#addNoteContent').append('<p>Error loading notes.</p>');
        }
    });
}
function convertJsonDate(jsonDate) {
    if (!jsonDate) return "";

    var timestamp = parseInt(jsonDate.replace(/[^0-9]/g, ""));
    var date = new Date(timestamp);

    // US Format: MM/DD/YYYY
    return ("0" + (date.getMonth() + 1)).slice(-2) + "/" +
        ("0" + date.getDate()).slice(-2) + "/" +
        date.getFullYear();
}
function loadTags() {
    $.ajax({
        type: "POST",
        url: "TpDetail.aspx/GetTags",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {

            let tagList = response.d;
            let dropdown = $("#tagDropdownMenu");
            dropdown.empty();

            $.each(tagList, function (i, tag) {
                dropdown.append(
                    '<li><a class="dropdown-item" href="#" data-value="' + tag.Id + '">' + tag.Name + '</a></li>'
                );
            });
        }
    });
}
$(document).on("click", "#tagDropdownMenu .dropdown-item", function (e) {
    e.preventDefault();

    var id = $(this).data("value");
    var name = $(this).text();

    if ($('#hiddenTagSelect option[value="' + id + '"]').length === 0) {
        $('#hiddenTagSelect').append('<option value="' + id + '" selected>' + name + '</option>');
    }

    var selectedTags = [];
    $('#hiddenTagSelect option').each(function () {
        selectedTags.push($(this).text());
    });

    $("#tagSearch").val(selectedTags.join(", "));
});
$(document).on("click", "#addBtn", function () {
    var customerGuid = $("#hdCustomerGUID").val(); 
    var noteText = $("#noteField").val();
    var tagIds = [];

    $("#hiddenTagSelect option").each(function () {
        tagIds.push($(this).val());
    });

    $.ajax({
        type: "POST",
        url: "TpDetail.aspx/AddNote",
        data: JSON.stringify({
            customerGuid: customerGuid,
            noteText: noteText,
            tagIds: tagIds.join(",")
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            alert(response.d); 
            getCustomerNotes();
            $("#noteModal").modal("hide");
            $("#noteField").val("");
            $("#hiddenTagSelect").empty();
            $("#tagSearch").val("");
        },
        error: function (err) {
            alert("Error: " + err.responseText);
        }
    });
});

//Note Related Code Ends
function getCustomerSitesList() {
    var CustomerID = $('#CustomerID').val();
    alert(CustomerID)
    alert($('#hdCustomerGUID').val().trim())
    
    $.ajax({
        url: "TpDetail.aspx/GetCustomerSiteData",
        type: 'POST',
        data: "{customerID: '" + CustomerID + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        success: function (response) {
            const data = response.d;
            $('#sites').empty();

            if (data.length > 0) {
                // Table structure for CSL data
                var table = `
                    <div class="table-responsive">
                        <table class="table table-striped table-bordered" id="sitesTable" style="width: 100%;">
                            <thead class="thead-light">
                                <tr>
                                    <th>Site Name</th>
                                    <th>Address</th>
                                    <th>Contact</th>
                                    <th>Email</th>
                                    <th>Phone No</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                `;

                // Populating table rows
                for (var i = 0; i < data.length; i++) {
                    table += `
                        <tr>
                            <td>${data[i].SiteName}</td>
                            <td>${data[i].Address}</td>
                            <td>${data[i].Contact == null ? "" : data[i].Contact}</td>
                            <td>${data[i].Email == null ? "" : data[i].Email}</td>
                            <td>${data[i].PhoneNumber == null ? "" : data[i].PhoneNumber}</td>
                            <td class="text-center">
                                <button class="btn btn-primary btn-sm edit-site-btn" type="button" data-site-id="${data[i].Id}">Edit</button>
                            </td>
                        </tr>
                    `;
                }

                // Table structure ends here
                table += `
                            </tbody>
                        </table>
                    </div>
                `;

                // Appending table to the sites container
                $('#sites').append(table);

                // Initializing DataTable for better interactivity
                $('#sitesTable').DataTable({
                    paging: true,
                    searching: true,
                    ordering: true,
                    responsive: true,
                    destroy: true
                });

                // Bind click event to edit buttons using delegation
                $('#sites').on('click', '.edit-site-btn', function () {
                    var siteId = $(this).data('site-id');
                    getCustomerSiteDataById(siteId);
                });
            } else {
                // Optional message if no data is available
                $('#sites').append('<p>No sites found.</p>');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error fetching sites: ", error);
            $('#sites').append('<p>Error loading sites.</p>');
        }
    });
}
function fnEdit(index) {
    $('#Edit' + index).click(function () {
        getCustomerSiteDataById(index);
    })
}
function getCustomerSiteDataById(siteId) {   
    $.ajax({
        url: "TpDetail.aspx/GetCustomerSiteDataBySiteId",
        type: 'POST',
        data: "{siteId: '" + siteId + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        success: function (response) {
           
            $('#addSiteModal').modal('show');
            $('#btnSaveSite').text('Update Site')
            const data = response.d;                   
          
            if (data.length > 0) {
                $('#SiteId').val(data[0].Id);
                $('#CustomerID').val(data[0].CustomerID);
                $('#siteContact').val(data[0].Contact);
                $('#address').val(data[0].Address);
                $('#siteName').val(data[0].SiteName);
                $('#txtEmail').val(data[0].Email);
                $('#txtPhoneNum').val(data[0].PhoneNumber);
                $('#note').val(data[0].Note);
                $('#cslFirstname').val(data[0].FirstName);
                $('#cslLastName').val(data[0].LastName);
                $('#cslCountry').val(data[0].Country);
                if (data[0].Country === 'CANADA') {
                    $('#cslProvence').val(data[0].State);
                    $("#cslProvenceDiv").show();
                    $("#cslStateDiv").hide();
                } else {
                    $('#cslSate').val(data[0].State);
                    $("#cslProvenceDiv").hide();
                    $("#cslStateDiv").show();
                }
             
                $('#cslZip').val(data[0].Zip);
                if (data[0].IsActive) $('#isActive').prop('checked',true);
                else $('#isActive').prop('checked', false);
            }
        }

    });
}
function getCustomerBillAddressDataById(billAddressId) {
    $.ajax({
        url: "TpDetail.aspx/GetCustomerBillAddressDataByBillAddressId",
        type: 'POST',
        data: "{billAddressId: '" + billAddressId + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        success: function (response) {

            $('#addAddressModal').modal('show');
            $('#btnSaveBillAddress').text('Update Address');
            const data = response.d;

            if (data.length > 0) {
                $('#BillAddressId').val(data[0].ID);
                $('#hdCustomerGUID').val(data[0].Customer_ID);
                $('#txtAddress').val(data[0].Address);
                $('#txtCity').val(data[0].City);
                $('#txtZipCode').val(data[0].ZipCode);
                $('#txtState').val(data[0].State);
                if (data[0].IsActive) $('#isActive').prop('checked', true);
                else $('#isActive').prop('checked', false);
            }
        }

    });
}
function loadHeaderForDropDowns() {
    $.ajax({
        type: "POST",
        dataType: "json",
        beforeSend: function () { },
        contentType: "application/json; charset=utf-8",
        url: "Project.aspx/LoadHeaderForDropDowns",
        data: JSON.stringify({}),
        success: function (response) {
            titleData = response.d;
            $('#hdr_SalesRep').text(response.d.SalesRep + " :");
            $('#hdr_MarketterList').text(response.d.Marketter + " :");
            $('#hdr_LeadSource').text(response.d.LeadSource + " :");
            $('#hdr_LeadType').text(response.d.LeadType + " :");
            $('#hdr_SalesStatus').text(response.d.SalesStatus + " :");
            $('#hdr_ProjectType').text(response.d.ProjectType + " :");
            $('#hdr_ProjectStatusList').text(response.d.ProjectStatus + " :");

            $("#LeadSourceDiv").toggle(response.d.LeadSourceIsActive);
            $("#LeadTypeDiv").toggle(response.d.LeadTypeIsActive);
            $("#SalesStatusDiv").toggle(response.d.SalesStatusIsActive);
            $("#ProjectTypeDiv").toggle(response.d.ProjectTypeIsActive);
            $("#ProjectStatusDiv").toggle(response.d.ProjectStatusIsActive);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert("Status: " + textStatus);
            alert("Error: " + XMLHttpRequest.responseText);
        }
    });
}

//function loadHeaderForDropDowns() {
//    $.ajax({
//        type: "POST",
//        dataType: "json",
//        beforeSend: function () {
//        },
//        contentType: "application/json; charset=utf-8",
//        url: "Project.aspx/LoadHeaderForDropDowns",
//        data: JSON.stringify({}),
//        success: function (response) {
//            console.log(response.d);
//            $('#hdr_SalesRep').text(response.d.SalesRep + " :");

//            $('#hdr_MarketterList').text(response.d.Marketter + " :");

//            $('#hdr_LeadSource').text(response.d.LeadSource + " :");
//            $('#hdr_LeadType').text(response.d.LeadType + " :");
//            $('#hdr_SalesStatus').text(response.d.SalesStatus + " :");
//            $('#hdr_ProjectType').text(response.d.ProjectType + " :");
//            $('#hdr_ProjectStatusList').text(response.d.ProjectStatus + " :");
//            //end

//            if (response.d.LeadSourceIsActive == false) {
//                $("#LeadSourceDiv").hide();
//            } else {
//                $("#LeadSourceDiv").show();
//            }
//            if (response.d.LeadTypeIsActive == false) {
//                $("#LeadTypeDiv").hide();
//            } else {
//                $("#LeadTypeDiv").show();
//            }
//            if (response.d.SalesStatusIsActive == false) {
//                $("#SalesStatusDiv").hide();
//            } else {
//                $("#SalesStatusDiv").show();
//            }
//            if (response.d.ProjectTypeIsActive == false) {
//                $("#ProjectTypeDiv").hide();
//            } else {
//                $("#ProjectTypeDiv").show();
//            }
//            if (response.d.ProjectStatusIsActive == false) {
//                $("#ProjectStatusDiv").hide();
//            } else {
//                $("#ProjectStatusDiv").show();
//            }
//        },
//        error: function (XMLHttpRequest, textStatus, errorThrown) {
//            alert("Status: " + textStatus); alert("Error: " + XMLHttpRequest.responseText);
//        }
//    });
//}

//Nizam
function BackClicked() {
    var Src = $('#hdSrc').val();
    var CustomerID = $('#CustomerID').val();

    if (Src == "cal") {
        window.location.href = "calendar.aspx";
    }
    else if (Src == "Customer") {
        window.location.href = "View_Appointment.aspx?Id=" + CustomerID;
    }
    else if (Src == "ResourceWiseDispatch") {
        window.location.href = "ResourceWiseDispatch.aspx";
    }
    else if (Src == "BusinessContact") {
        window.location.href = "BusinessContact.aspx";
    }
    else {
        window.location.href = "Appointmentlist.aspx";
    }
}

var IsAccessLogVisible = false;
function AccesslogBtnClick() {
    if (!IsAccessLogVisible) {
        var customerGuid = $('#hdCustomerGUID').val();
        LoadLogList(customerGuid);
    }

    return false;
}
var IsProjectVisible = false;
function LoadProjectList() {
    $.ajax({
        type: "POST",
        dataType: "json",
        beforeSend: function () {
            $("#spinner_ProjectList").show();
        },
        contentType: "application/json; charset=utf-8",
        url: "ProjectList.aspx/LoadProjectListByCustomer",
        data: JSON.stringify({ CustomerGuid: $("#hdCustomerGUID").val() }),
        success: function (response) {
            $("#spinner_ProjectList").hide();
            $('#table_ProjectList').dataTable({
                data: response.d,
                //data : response,
                columns: [
                    {
                        "data": "ProjectId",
                        "render": function (data, type, row, meta) {
                            console.log(JSON.stringify(row));
                            if (row.HaveAccess) {
                                return '<a href="Project.aspx?PID=' + row.ID + '&cid=' + row.CustomerID + '">' + data + '</a>';
                            } else {
                                return '' + row.ProjectId + '';
                            }
                        }
                    },
                    { "data": "ProjectType" },
                    { "data": "ProjectStatus" }
                ],
                fixedColumns: true,
                //"bPaginate": false,
                "bDestroy": true,
                fixedColumns: {
                    left: 0
                },
                autoWidth: false,
                searching: true,
                responsive: true
            });
            IsProjectVisible = true;
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert("Status: " + textStatus); alert("Error: " + XMLHttpRequest.responseText);
        }
    });
};

function LoadCurrentProject() {
    $.ajax({
        type: "POST",
        dataType: "json",
        beforeSend: function () {
            $("#spinner_CurrentProject").show();
        },
        contentType: "application/json; charset=utf-8",
        url: "ProjectList.aspx/LoadCurrentProject",
        data: JSON.stringify({ CustomerGuid: $("#hdCustomerGUID").val() }),
        success: function (response) {
            $("#spinner_CurrentProject").hide();

            // Destroy existing DataTable instance if it exists
            if ($.fn.DataTable.isDataTable('#currentProjectTable')) {
                $('#currentProjectTable').DataTable().destroy();
            }

            var table = $('#currentProjectTable').DataTable({
                data: response.d,
                columns: [
                    { data: 'ProjectId', title: 'Project Id', defaultContent: 'N/A' },
                    { data: 'CustomerName', title: 'Customer Name', defaultContent: 'N/A' },
                    { data: 'CustomerDetail', title: 'Customer Detail', defaultContent: 'N/A' },
                    { data: 'ProjectType', title: 'Project Type', defaultContent: 'N/A' },
                    { data: 'ProjectStatus', title: 'Project Status', defaultContent: 'N/A', visible: titleData && titleData.ProjectStatusIsActive },
                    { data: 'SalesRep', title: 'Sales Rep', defaultContent: 'N/A', visible: titleData && titleData.SalesRepIsActive },
                    { data: 'Market', title: 'Market', defaultContent: 'N/A', visible: titleData && titleData.MArketIsActive },
                    { data: 'ORG', title: 'ORG', defaultContent: 'N/A', visible: titleData && titleData.ORGIsActive },
                    { data: 'Marketer', title: 'Marketer', defaultContent: 'N/A', visible: titleData && titleData.MarketterIsActive },
                    { data: 'Shore', title: 'Shore', defaultContent: 'N/A', visible: titleData && titleData.ShoreIsActive },
                    { data: 'Pm', title: 'Pm', defaultContent: 'N/A', visible: titleData && titleData.PMIsActive },
                    { data: 'Ip', title: 'Ip', defaultContent: 'N/A', visible: titleData && titleData.IPIsActive },
                    { data: 'Stage', title: 'Stage', defaultContent: 'N/A', visible: titleData && titleData.StageIsActive },
                    { data: 'LeadSource', title: 'LeadSource', defaultContent: 'N/A', visible: titleData && titleData.LeadSourceIsActive },
                    { data: 'LeadType', title: 'Lead Type', defaultContent: 'N/A', visible: titleData && titleData.LeadTypeIsActive },
                    { data: 'SalesStatus', title: 'Sales Status', defaultContent: 'N/A', visible: titleData && titleData.SalesStatusIsActive }
                ],
                paging: false,
                searching: false,
                ordering: false,
                info: false,
                responsive: true,
                language: {
                    emptyTable: "No current project found."
                }
            });
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            $("#spinner_CurrentProject").hide();
            alert("Status: " + textStatus);
            alert("Error: " + XMLHttpRequest.responseText);
        }
    });
}

function LoadLogList(customerGuid) {
    $.ajax({
        type: "POST",
        dataType: "json",
        beforeSend: function () {
            $("#imgSpinner1").show();
        },
        contentType: "application/json; charset=utf-8",
        url: "TpDetail.aspx/GetAccessLogList",
        data: JSON.stringify({ customerGuid: customerGuid }),
        success: function (response) {
            $("#imgSpinner1").hide();
            console.log(response.d);
            $('#accessLogTable').dataTable({
                data: response.d,

                columns: [

                    { "data": "CreatedDate" },
                    { "data": "Message" }

                ],
                fixedColumns: true,
                //"bPaginate": false,
                "bDestroy": true,
                fixedColumns: {
                    left: 0
                },
                autoWidth: false,
                searching: true,
                responsive: true,
                searching: false,
                lengthChange: false,
                order: [[0, 'desc']]
            });
            IsAccessLogVisible = true;
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert("Status: " + textStatus); alert("Error: " + XMLHttpRequest.responseText);
        }
    });
};

function validateSiteForm() {
    let isValid = true;
    let errorMessage = "";

    // Required field validation
    if ($("#siteName").val().trim() === "") {
        errorMessage += "Site Name is required.\n";
        isValid = false;
    }
    if ($("#address").val().trim() === "") {
        errorMessage += "Adress is required.\n";
        isValid = false;
    }

    if (!isValid) {
        alert(errorMessage);
    }
    return isValid;

}
function saveSite(event) {
    event.preventDefault();
    var country = $('#cslCountry').val().trim();
    var stateOrProvince = "";

    if (country === "CANADA") {
        stateOrProvince = $('#cslProvence').val().trim();
    } else if (country === "USA") {
        stateOrProvince = $('#cslSate').val().trim();
    }

    if (validateSiteForm()) {
        const site = {
            Id: parseInt($('#SiteId').val()),
            CustomerID: $('#CustomerID').val(),
            CustomerGuid: $('#hdCustomerGUID').val(),
            Note: $('#note').val().trim(),
            Contact: $('#siteContact').val().trim(),
            Address: $('#address').val().trim(),
            SiteName: $('#siteName').val().trim(),
            Email: $('#txtEmail').val().trim(),
            PhoneNumber: $('#txtPhoneNum').val().trim(),
            IsActive: document.getElementById("isActive").checked,
            FirstName: $('#cslFirstname').val().trim(),
            LastName: $('#cslLastName').val().trim(),
            Country: $('#cslCountry').val().trim(),
            State: stateOrProvince,
            Zip: $('#cslZip').val().trim(),
        };

        let message = "saved";
        if (site.Id > 0) {
            message = "updated";
        }

        $.ajax({
            type: "POST",
            url: "TpDetail.aspx/SaveCustomerSiteData",
            data: JSON.stringify({ site: site }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
         
                if (response.d) {
                    alert("Site " + message + " successfully!");
                    $('#addSiteModal').modal('hide');
                    getCustomerSitesList();
                }
                else {
                    alert("Something went wrong!");
                }
            },
            error: function (xhr, status, error) {
                console.error("Error updating details: ", error);
            }
        });
    }
}
function validateBillAddressForm() {
    let isValid = true;
    let errorMessage = "";

    // Required field validation
    if ($("#txtCity").val().trim() === "") {
        errorMessage += "City is required.\n";
        isValid = false;
    }
    if ($("#txtZipCode").val().trim() === "") {
        errorMessage += "Zip Code is required.\n";
        isValid = false;
    }
    if ($("#txtCity").val().trim() === "") {
        errorMessage += "City is required.\n";
        isValid = false;
    }
    if ($("#txtState").val().trim() === "") {
        errorMessage += "State is required.\n";
        isValid = false;
    }

    if (!isValid) {
        alert(errorMessage);
    }
    return isValid;

}
function saveBillingAddress(event) {
    event.preventDefault();
    if (validateBillAddressForm()) {
        const billingAddressInfo = {
            ID: $('#BillAddressId').val() == "" ? "00000000-0000-0000-0000-000000000000" : $('#BillAddressId').val(),
            Address: $('#txtAddress').val().trim(),
            Customer_ID: $('#hdCustomerGUID').val(),
            City: $('#txtCity').val().trim(),
            ZipCode: $('#txtZipCode').val().trim(),
            State: $('#txtState').val().trim(),          
            IsActive: document.getElementById("isActiveCustAddress").checked
        };       

        let message = "saved";
        if (billingAddressInfo.ID !="00000000-0000-0000-0000-000000000000") {
            message = "updated";
        }

        $.ajax({
            type: "POST",
            url: "TpDetail.aspx/SaveCustomerBillingAddress",
            data: JSON.stringify({ billingAddressInfo: billingAddressInfo }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                if (response.d) {
                    alert("Billing Address " + message + " successfully!");
                    $('#addAddressModal').modal('hide');
                    getCustomerBillingAddress();
                }
                else {
                    alert("Something went wrong!");
                }
            },
            error: function (xhr, status, error) {
                console.error("Error updating details: ", error);
            }
        });
    }
}

$('.panel-collapse').on('show.bs.collapse', function () {
    $(this).siblings('.panel-heading').addClass('active');
});

$('.panel-collapse').on('hide.bs.collapse', function () {
    $(this).siblings('.panel-heading').removeClass('active');
});
function toggleCSLStateProvince() {
    let country = $("#cslCountry").val();

    if (country === "CANADA") {
        $("#cslProvenceDiv").show();
        $("#cslStateDiv").hide();
    } else if (country === "USA") {
        $("#cslStateDiv").show();
        $("#cslProvenceDiv").hide();
    } else {
        $("#cslStateDiv").hide();
        $("#cslProvenceDiv").hide();
    }
}

// Run on load (for pre-filled edit form case)
toggleCSLStateProvince();

// Run on change
$("#cslCountry").change(function () {
    toggleCSLStateProvince();
});

