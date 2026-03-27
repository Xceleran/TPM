//$('#fscb_IsSameAsBusiness').change(function () {
//    if (this.checked) {
//        $('#txt_ContactFirstName').val($('#txt_BusinessName').val());
//        $('#txt_ContactLastName').val(' ');
//        $('#txt_Contactaddress1').val($('#address1').val());
//        $('#txt_Contactcity').val($('#city').val());
//        $('#txt_ContactState').val($('#state').val());
//        $('#txt_Contactzip').val($('#zip').val());
//        $('#txt_Contactphone').val($('#phone').val());
//        $('#txt_Contactmobile').val($('#mobile').val());

//        $('#txt_Contactemail').val($('#email').val());

//    }
//});
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
        console.log(result)
        if (result.isConfirmed) {
            var BusinessID = $('#BusinessID').val();
            $.ajax({
                url: 'BusinessContact.aspx/DeleteCustomer',
                type: "POST",
                contentType: 'application/json',
                data: "{BusinessID:" + BusinessID + "}",
                dataType: 'json',
                success: function (sR) {
                    window.location.href = 'CustomerList.aspx?m=2&Type=Business';
                },
                error: function (error) {
                    console.log(error);
                }
            })
        }
        else {
        }
    });

    return status;
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

function CreateAppt() {
    //  alert("sdfsd");
    window.location.href = "calendar.aspx?CustomerID=" + $('#PrimaryCustomerid').val();
}

function BackClicked() {
    window.location.href = 'CustomerList.aspx?m=2&Type=Business';
}
function CreateInvoice() {
    var RefId = $('#hf_BusinessGuid%>').val();
    window.location.href = "InvoiceCreate.aspx?m=0" + "&cId=" + RefId;
}

function ViewInvoiceList(type) {
    window.location.href = "Invoices.aspx?Id=" + $('#PrimaryCustomerid').val() + "&Type=" + type;
}
function ViewEmailHistoryList(BusinessID) {
    window.location.href = "EmailHistory_List.aspx?Id=" + $('#PrimaryCustomerid').val();
}

function CreateProposal(type) {
    window.location.href = "Invoice.aspx?InvNum=0&cId=" + $('#hf_BusinessGuid').val() + "&InType=" + type;
}
function ViewFiles() {
    window.location.href = "CustomerFiles.aspx?Id=" + $('#PrimaryCustomerid').val();
}

function ViewAppointment() {
    window.location.href = "View_Appointment.aspx?Id=" + $('#PrimaryCustomerid').val();
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

function addProject() {
    window.location.href = "Project.aspx?Cid=" + $('#hf_BusinessGuid').val();
}