function ShowSuccessMsgForBusinessCustomer() {
    $.alert({
        title: 'Xceleran',
        content: "Business Contact Saved successfully.",
        icon: 'fa fa-info-circle',
        animation: 'scale',
        closeAnimation: 'scale',
        opacity: 0.5,
        buttons: {
            okay: {
                text: 'okay',
                btnClass: 'btn-blue',
                action: function () {
                    window.location.href = "BusinessContact.aspx?id=" + $("#BusinessGuID").val();
                }
            }
        }
    });
}

function isNumberKey(evt) {
    var charCode = (evt.which) ? evt.which : evt.keyCode
    if (charCode > 31 && (charCode < 48 || charCode > 57))
        return false;
    return true;
}

function SaveCustomer() {
    if (ValidateData()) {
        if (document.getElementById('<%=BusinessID.ClientID%>').value == "0") {
            document.getElementById("<%=hdMode.ClientID%>").value = "Add"
        }
        else {
            document.getElementById("<%=hdMode.ClientID%>").value = "Modify"
        }

        return true;
    }
}

function DeleteCustomer() {
    Swal.fire({
        title: 'Are you sure you want to delete?',
        showDenyButton: true,
        showCancelButton: false,
        confirmButtonText: 'Yes',
        denyButtonText: 'No',
    }).then((result) => {
        if (result.isConfirmed) {
            var BusinessID = $('#BusinessID').val();
            $.ajax({
                url: 'BusinessContact.aspx/DeleteCustomer',
                type: "POST",
                contentType: 'application/json',
                data: "{BusinessID:" + BusinessID + "}",
                dataType: 'json',
                success: function () {
                    window.location.href = 'TpList.aspx?m=2&Type=Business';
                },
                error: function (error) {
                    console.log(error);
                }
            })
        }
    });

    return false;
}

function ValidateData() {
    var BusinessName = document.getElementById('txt_BusinessName').value;
    var FirstName = document.getElementById('txt_FirstName').value;
    var Address = document.getElementById('address1').value;
    var City = document.getElementById('city').value;
    var State = document.getElementById('state').value;
    var Province = document.getElementById('province').value;
    var Country = document.getElementById('country');
    Country = Country ? Country.value : null;
    var Zip = document.getElementById('zip').value;

    if (BusinessName.trim() == "") {
        alert("Business name cannot be blank");
        return false;
    }

    if (FirstName.trim() == "") {
        alert("First name cannot be blank");
        return false;
    }

    if (Address.trim() == "") {
        alert("Address cannot be blank");
        return false;
    }

    if (City.trim() == "") {
        alert("City cannot be blank");
        return false;
    }

    if (Country == "select" && $('#div_country').is(':visible')) {
        alert("Please select the Country");
        return false;
    }

    if (Country == "Canada" && Province == "select") {
        alert("Please select the Province");
        return false;
    }

    if (Country != "Canada" && State == "select") {
        alert("Please select the State");
        return false;
    }

    if (Zip.trim() == "") {
        alert("Zip code cannot be blank");
        return false;
    }

    return true;
}

function getBusinessCustomerId() {
    return ($('#PrimaryCustomerid').val() || '').trim();
}

function getBusinessGuid() {
    return ($('#hf_BusinessGuid').val() || $('#BusinessGuID').val() || '').trim();
}

function requireSavedBusiness(actionLabel) {
    var customerId = getBusinessCustomerId();
    var businessGuid = getBusinessGuid();
    if (!customerId || customerId === '0' || !businessGuid) {
        alert('Save the business contact first, then try: ' + (actionLabel || 'this action') + '.');
        return false;
    }
    return true;
}

function businessCustomerDetailsUrl(customerId, tab, openAction, extraParams) {
    var url = 'CustomerDetails.aspx?custId=' + encodeURIComponent(customerId) + '&siteId=0';
    if (tab) url += '&tab=' + encodeURIComponent(tab);
    if (openAction) url += '&openAction=' + encodeURIComponent(openAction);
    if (extraParams) {
        for (var key in extraParams) {
            if (extraParams[key]) {
                url += '&' + encodeURIComponent(key) + '=' + encodeURIComponent(extraParams[key]);
            }
        }
    }
    return url;
}

function openCecRedirect(redirectPath, actionLabel) {
    if (!requireSavedBusiness(actionLabel)) return false;

    $.ajax({
        url: 'BusinessContact.aspx/GetCecRedirectUrl',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ redirectPath: redirectPath }),
        success: function (res) {
            var payload = res.d || res;
            if (payload.success && payload.url) {
                window.open(payload.url, '_blank');
            } else {
                alert(payload.message || ('Unable to open ' + actionLabel + '.'));
            }
        },
        error: function () {
            alert('Unable to open ' + actionLabel + '. Check Web.config cecBaseUrl.');
        }
    });
    return false;
}

function AddLinkedContact() {
    var businessGuid = getBusinessGuid();
    if (!requireSavedBusiness('Add Link Contact')) return false;
    return openCecRedirect('customerDetail.aspx?BusinessGuID=' + encodeURIComponent(businessGuid) + '&Mode=Add', 'Add Link Contact');
}

function CreateAppt() {
    var customerId = getBusinessCustomerId();
    if (!requireSavedBusiness('Create Appointment')) return false;
    return openCecRedirect('calendar.aspx?CustomerID=' + encodeURIComponent(customerId), 'Create Appointment');
}

function BackClicked() {
    window.location.href = 'TpList.aspx?m=2&Type=Business';
}

function CreateInvoice() {
    return CreateProposal('Invoice');
}

function CreateProposal(type) {
    var refId = getBusinessGuid();
    if (!requireSavedBusiness('Create ' + (type === 'Proposal' ? 'Estimate' : 'Invoice'))) return false;
    var inType = (type === 'Proposal') ? 'Proposal' : 'Invoice';
    window.location.href = 'InvoiceCreate.aspx?m=0&InvNum=0&cId=' + encodeURIComponent(refId) + '&InType=' + encodeURIComponent(inType);
    return false;
}

function ViewInvoiceList(type) {
    var customerId = getBusinessCustomerId();
    if (!requireSavedBusiness('View ' + (type === 'Proposal' ? 'Estimate' : 'Invoice'))) return false;

    var extra = {};
    if (type === 'Invoice') extra.invoiceType = 'Invoice';
    if (type === 'Proposal') extra.invoiceType = 'Proposal';

    window.location.href = businessCustomerDetailsUrl(customerId, 'invoices', null, extra);
    return false;
}

function ViewEmailHistoryList() {
    var customerId = getBusinessCustomerId();
    if (!requireSavedBusiness('View Email History')) return false;

    $.ajax({
        url: 'BusinessContact.aspx/GetEmailHistoryUrl',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ customerId: customerId }),
        success: function (res) {
            var payload = res.d || res;
            if (payload.success && payload.url) {
                window.open(payload.url, '_blank');
            } else {
                alert(payload.message || 'Email history is not available.');
            }
        },
        error: function () {
            alert('Unable to open email history.');
        }
    });
    return false;
}

function ViewFiles() {
    var customerId = getBusinessCustomerId();
    if (!requireSavedBusiness('View Files')) return false;
    window.location.href = businessCustomerDetailsUrl(customerId, 'files');
    return false;
}

function ViewAppointment() {
    var customerId = getBusinessCustomerId();
    if (!requireSavedBusiness('View Appointment')) return false;
    window.location.href = businessCustomerDetailsUrl(customerId, 'appointments');
    return false;
}

function addProject() {
    var refId = getBusinessGuid();
    if (!requireSavedBusiness('Add Project')) return false;
    return openCecRedirect('Project.aspx?Cid=' + encodeURIComponent(refId), 'Add Project');
}

var _invoiceListLoaded = false;
var _filesCommsLoaded = false;

function LoadProjectList() {
    var customerId = getBusinessCustomerId();
    if (!customerId || customerId === '0') {
        $('#table_ProjectList tbody').html('<tr><td colspan="5" class="text-center text-muted">Save the business contact to load invoices.</td></tr>');
        return false;
    }
    if (_invoiceListLoaded && $.fn.DataTable.isDataTable('#table_ProjectList')) {
        return false;
    }

    $.ajax({
        type: 'POST',
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: 'BusinessContact.aspx/GetBusinessInvoiceList',
        data: JSON.stringify({ customerId: customerId }),
        beforeSend: function () {
            $('#spinner_ProjectList').show();
        },
        success: function (response) {
            $('#spinner_ProjectList').hide();
            var payload = response.d || response;
            var rows = (payload && payload.data) ? payload.data : [];

            if ($.fn.DataTable.isDataTable('#table_ProjectList')) {
                $('#table_ProjectList').DataTable().destroy();
            }

            $('#table_ProjectList').DataTable({
                data: rows,
                columns: [
                    { data: 'number', title: 'Number', defaultContent: '' },
                    { data: 'type', title: 'Type', defaultContent: '' },
                    { data: 'date', title: 'Date', defaultContent: '' },
                    { data: 'total', title: 'Total', defaultContent: '0.00' },
                    { data: 'due', title: 'Due', defaultContent: '0.00' }
                ],
                destroy: true,
                searching: true,
                responsive: true,
                language: { emptyTable: 'No invoices found for this business contact.' }
            });
            _invoiceListLoaded = true;
        },
        error: function (xhr) {
            $('#spinner_ProjectList').hide();
            $('#table_ProjectList tbody').html('<tr><td colspan="5" class="text-center text-danger">Error loading invoices.</td></tr>');
            console.error(xhr.responseText);
        }
    });
    return false;
}

function LoadCurrentProject() {
    var customerId = getBusinessCustomerId();
    if (!customerId || customerId === '0') {
        $('#currentProjectTable tbody').html('<tr><td colspan="5" class="text-center text-muted">Save the business contact to load files and communications.</td></tr>');
        return false;
    }
    if (_filesCommsLoaded && $.fn.DataTable.isDataTable('#currentProjectTable')) {
        return false;
    }

    $.ajax({
        type: 'POST',
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: 'BusinessContact.aspx/GetBusinessFilesAndCommunications',
        data: JSON.stringify({ customerId: customerId }),
        beforeSend: function () {
            $('#spinner_CurrentProject').show();
        },
        success: function (response) {
            $('#spinner_CurrentProject').hide();
            var payload = response.d || response;
            var rows = (payload && payload.data) ? payload.data : [];

            if ($.fn.DataTable.isDataTable('#currentProjectTable')) {
                $('#currentProjectTable').DataTable().destroy();
            }

            $('#currentProjectTable').DataTable({
                data: rows,
                columns: [
                    { data: 'kind', title: 'Kind', defaultContent: '' },
                    { data: 'title', title: 'Title', defaultContent: '' },
                    { data: 'detail', title: 'Detail', defaultContent: '' },
                    { data: 'eventDate', title: 'Date', defaultContent: '' },
                    { data: 'status', title: 'Status', defaultContent: '' }
                ],
                destroy: true,
                searching: true,
                responsive: true,
                order: [[3, 'desc']],
                language: { emptyTable: 'No files or communications found for this business contact.' }
            });
            _filesCommsLoaded = true;
        },
        error: function (xhr) {
            $('#spinner_CurrentProject').hide();
            $('#currentProjectTable tbody').html('<tr><td colspan="5" class="text-center text-danger">Error loading files and communications.</td></tr>');
            console.error(xhr.responseText);
        }
    });
    return false;
}

$("#email").focusout(function () {
    var input = $("#email").val()
    if (input == null || input.trim() == "") {
        return true;
    }
    var validRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/;

    if (!input.match(validRegex)) {
        alert("Invalid email address!");
        $("#email").val("");
        return false;
    }
});
