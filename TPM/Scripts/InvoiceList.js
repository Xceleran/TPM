function collectPaymentByMerchant() {
    var dueAmount = parseFloat($("input#txt_DueAmountM").val().replace('$', '').replace(',', '').trim()) || 0;
    var paymentAmount = parseFloat($("input#txt_DepositAmountM").val().replace('$', '').replace(',', '').trim()) || 0;

    if (paymentAmount <= 0) {
        $.alert({
            title: 'Xceleran',
            content: "Please enter a valid payment amount.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });
        return false;
    }
    if (paymentAmount > dueAmount) {
        $.alert({
            title: 'Xceleran',
            content: "Pay amount can not be greater than due amount.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });

        return false;
    }

    var cID = $("#lblCustomerID").val();
    if (!cID) cID = $("#CustomerID").val();
    var id = $("#_InvoiceNo").val();

    var qboCustID = $("#QBOCustomerId").val();
    var qboID = $("#MqboID").val();
    var qboCustID = $("#McustomerID").val();
    var isDeposite = false;
    if ($("#MisDiposite").val() == 'true') {
        isDeposite = true;
    }
    var paymentProcess = "gpi";
    var selectPaymentProcess = $("#PaymentProcessSelect").val();
    if (selectPaymentProcess != null && selectPaymentProcess != "") {
        paymentProcess = selectPaymentProcess;
    }

    if (selectPaymentProcess == "") {
        return;
    }
    if (paymentProcess == "gpi") {
        document.getElementById("SubminBtn").style.display = "block";
        document.getElementById("EmailBtn").style.display = "none";

        $.ajax({
            url: 'Invoices.aspx/GetPaymenUrl',
            type: "POST",
            contentType: 'application/json',
            data: "{InvoiceNo: '" + id + "',CustomerID:'" + cID + "',qboCustID:'" + qboCustID +
                "',qboID:'" + qboID + "',isDeposite:'" + isDeposite + "',paymentProcess:'" + paymentProcess + "',ctype:'" + $("#ctype").val() + "'}",
            dataType: 'json',
            success: function (sR) {
                var Url = sR.d;
                console.log(Url);
                //window.open(Url, '_blank').focus();
                var win = window.open(Url, "vt");
            },
            error: function (error) {
                // alert(error);
            }
        })
    }
    if (paymentProcess == "wisetack") {
        console.log(id)
        //  $("#modal_financialInfo").text("");
        //$("#FinancingOptions").attr("href", "#");
        var invid = $("#InvViewNumber").val();
        console.log(invid)
        $.ajax({
            url: 'InvoiceList.aspx/GetWiseTackPaymentLink',
            type: "POST",
            contentType: 'application/json',
            data: "{InvoiceNo: '" + invid + "',CustomerID:'" + cID + "',InvoiceID:'" + id + "'}",
            dataType: 'json',
            success: function (sR) {
                if (sR.d != null || sR.d != "") {
                    var loanInfo = JSON.parse(sR.d);
                    $("#wisetackDiv").show();
                    if (loanInfo.AsLowAsAmount == null || loanInfo.AsLowAsAmount == "") {
                        $("#financialInfo").text("No loan info found for this invoice amount");
                    }
                    $("#financialInfo").text(loanInfo.AsLowAsAmount);

                    //  $("#FinancingOptions").attr("href", loanInfo.PaymentLink);
                    if (loanInfo.Status == "New" || loanInfo.Status == "") {
                        //if need to do something will do later
                        $("#LoanStatus").text("");
                    } else {
                        $("#wisetackDiv").addClass("disabled");
                        $("#LoanStatus").text("Loan Status: " + loanInfo.Status);
                    }
                    document.getElementById("SubminBtn").style.display = "none";
                    document.getElementById("EmailBtn").style.display = "block";

                    // console.log(loanInfo.PaymentLink)
                }

                //console.log(Url);
                //window.open(Url, '_blank').focus();
            },
            error: function (error) {
                // alert(error);
            }
        })
    }
}

function Collect(IsDeposit, Total, Number, paymentstatus) {
    if (paymentstatus === 'Paid') {
        $.alert({
            title: 'Xceleran',
            content: "Invoice already paid.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });
        return;
    }
    $("#IsDeposit").val(IsDeposit);
    //console.log("IsDeposit=" + IsDeposit);
    //if (IsDeposit == '1') {
    //    console.log("IsDepositsdf=" + IsDeposit);
    //    console.log($("#h_CollectType").html());
    //    $("#h_CollectType").html("Collect Deposit.");
    //}
    //else {
    //    $("#h_CollectType").html("Collect Payment.");
    //}
    $("#txt_depositAmount").val(Total);
    $("#txt_TotalAmount").val("$" + Total);
    $("#_InvoiceNo").val(Number);

    $("#Collect_deposit").modal('show');
}
function ShowCollect_depositPopup() {
    $("#Collect_deposit").modal('show');
}
function CloseCollect_depositPopup() {
    $("#Collect_deposit").modal('hide');
}
function ViewDepositList(ivId) {
    $("#Modal_ShowDeposit").modal('show');
    $("#tblNewInvoice").html("");
    var prgBar = document.getElementById("ProgressGIF");
    prgBar.style.display = "block";

    $.ajax({
        type: "POST",
        url: "Invoices.aspx/GetDepositsById",
        data: JSON.stringify({ 'iId': ivId }),
        beforeSend: function (xhr) {
            $("#div_Loading").show();
        },
        contentType: "application/json",
        dataType: "json",
        success: function (Data) {
            prgBar.style.display = "none";
            $("#tblNewInvoice").html(Data.d.TableRow);
            $("#div_Loading").hide();
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            $("#div_Loading").hide();
        }
    });
}
function CloseCollect_DepositListPopup() {
    $("#Modal_ShowDeposit").modal('hide');
}
function CollectWithMerchant(CustomerID, DepositAmount, Total, Number, qboID, QBOCustomerId, isDiposite, InvViewNumber, paymentStatus) {
    if (paymentStatus == 'Paid') {
        $.alert({
            title: 'Xceleran',
            content: "Invoice already paid.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });
        return;
    } else {
        $("#txt_DepositAmountM").val(Total);
        $("#MqboID").val(qboID);
        $("#McustomerID").val(QBOCustomerId);
        $("#MisDiposite").val(isDiposite);
        $("#lblCustomerID").val(CustomerID);
        $("#txt_DueAmountM").val("$" + Total);
        $("#_InvoiceNo").val(Number);
        $("#InvViewNumber").val(InvViewNumber);

        if (isDiposite == 'true') {
            $(".TypeOfModal").text('Deposit');
        } else {
            $(".TypeOfModal").text('Payment');
        }
        $("#Collect_WithMerchant").modal('show');
        $("#Modal_PaymentProcessSelect").val("");
        var isBusCut = $("#ctype").val();
        if (isBusCut == "2") {
            $("#wisteckOpt").hide();
        } else {
            $("#wisteckOpt").show();
        }
        toggoleButton();
    }
}
function toggoleButton() {
    $("#wisetackDiv").hide();
    document.getElementById("SubminBtn").style.display = "block";
    document.getElementById("EmailBtn").style.display = "none";
}
function CloseCollect_depositByMPopup() {
    $("#Collect_WithMerchant").modal('hide');
}
function DownloadPdf() {
    // alert('DownloadPdf');

    $.ajax({
        url: 'InvoiceList.aspx/printPdf',
        type: "POST",
        contentType: 'application/json',
        data: "{InvoiceNo:'" + invid + "',CustomerID:'" + cID + "',DocType:'" + $("#hf_CurrentPdfType").val() + "'}",
        dataType: 'json',
        success: function (sR) {
            //  alert(sR.d);
            var Url = sR.d;
            console.log(Url);
            window.open(Url, '_blank').focus();
        },
        error: function (error) {
            //  alert(error);
        }
    })
}
$(document).on({
    ajaxStart: function () {
        $("body").addClass("loading");
    },
    ajaxStop: function () {
        $("body").removeClass("loading");
    }
});

$("#wisetackDiv").hide();

function CollectPayment() {
    var invid = $("#InvPrimaryID").val();

    var id = $("#lblInvoiceNo").text();
    var cID = $("#lblCustomerID").text();

    var qboCustID = $("#qboCustID").text();
    var qboInvID = $("#qboInvID").text();
    var qboEstID = $("#qboEstID").text();
    var paymentProcess = "gpi";

    console.log("CollectPayment");
    var selectPaymentProcess = $("#PaymentProcessSelect").val();
    if (selectPaymentProcess != null && selectPaymentProcess != "") {
        paymentProcess = selectPaymentProcess;
    }

    if (selectPaymentProcess == "") {
        return;
    }
    if (paymentProcess == "gpi") {
        $.ajax({
            url: 'InvoiceList.aspx/GetPaymenUrl',
            type: "POST",
            contentType: 'application/json',
            data: "{InvoiceNo: '" + invid + "',CustomerID:'" + cID + "',qboCustID:'" + qboCustID +
                "',qboInvID:'" + qboInvID + "',qboEstID:'" + qboEstID + "',paymentProcess:'" + paymentProcess + "',ctype:'" + $("#ctype").val() + "'}",
            dataType: 'json',
            success: function (sR) {
                var Url = sR.d;
                console.log(Url);
                window.open(Url, '_blank').focus();
            },
            error: function (error) {
                //  alert(error);
            }
        })
    }
    //if (paymentProcess == "wisetack") {
    //getWisetackPayment();

    //}
}
function getWisetackPayment() {
    var invid = $("#InvPrimaryID").val();
    console.log("wisetack ", invid)
    var id = $("#lblInvoiceNo").text();
    var cID = $("#lblCustomerID").text();

    $("#financialInfo").text("");
    // $("#FinancingOptions").attr("href", "#");

    $.ajax({
        url: 'InvoiceList.aspx/GetWiseTackPaymentLink',
        type: "POST",
        contentType: 'application/json',
        data: "{InvoiceNo: '" + id + "',CustomerID:'" + cID + "',InvoiceID:'" + invid + "'}",
        dataType: 'json',
        success: function (sR) {
            if (sR.d != null || sR.d != "") {
                var loanInfo = JSON.parse(sR.d);
                $("#wisetackDiv").show();
                if (loanInfo.AsLowAsAmount == null || loanInfo.AsLowAsAmount == "") {
                    $("#financialInfo").text("No loan info found for this invoice amount");
                }
                $("#financialInfo").text(loanInfo.AsLowAsAmount);
                console.log(loanInfo.Status)
                if (loanInfo.Status == "New" || loanInfo.Status == "") {
                    $("#LoanStatus").text("");
                } else {
                    $("#wisetackDiv").addClass("disabled");
                    $("#LoanStatus").text("Loan Status: " + loanInfo.Status);
                }
                console.log(loanInfo.PaymentLink)
            }

            //console.log(Url);
            //window.open(Url, '_blank').focus();
        },
        error: function (error) {
            //  alert(error);
        }
    })
}
function GoBackClicked() {
    window.location.href = "customers.asp?m=2";
}
//function sendMail() {
//    // console.log(RemainderCommunicationList);
//    var invid = $("#InvPrimaryID").val();

//    var invoiceNo = $("#lblInvoiceNo").text();
//    var customerID = $("#lblCustomerID").text();
//    var customerName = $("#lblCustomerName").text();

//    var isSend = "false";
//    var checkbox = document.getElementById("IsSendPaymentLink");

//    if (checkbox.checked) {
//        isSend = "true";
//    } else {
//        isSend = "false";
//    }
//    $.ajax({
//        url: 'Invoices.aspx/SendMail',
//        type: "POST",
//        contentType: 'application/json',
//        data: "{InvoiceNo: '" + invid + "',CustomerID:'" + customerID + "',IsToCustomer:'True',IsSendPaymentLink:'" + isSend + "'}",
//        dataType: 'json',
//        success: function (sR) {
//            alert("Send Successfully");
//            //window.location = "RemainderCommunication.aspx?m=4";
//        },
//        error: function (error) {
//            alert("Error:" + error);
//        }
//    })
//}
function checkValidDate() {
    // alert("enter")
    var from = $("#FromDate").val();
    var to = $("#ToDate").val();

    if (Date.parse(from) > Date.parse(to)) {
        alert("Invalid Date Range");
        $("#ToDate").val("");
        $('#ToDate').css("border-color", "red");
    }
}
function OpenInvoice(invoiceNumber, customerId) {
    //  alert(customerId)
    /*   window.location.href = "InvoiceCreate.aspx?cId=" + customerId + "&m=" + invoiceNumber;*/
    CloseMailDiv();
    ShowPopup();
    $("#div_DepositBody").hide();
    $("#div_Depositheader").hide();
    $("#div_Converted").hide();
    var prgBar = document.getElementById("InvocieProgress");
    //  $('input:radio[name="imgbackground"][value="AsaInvoice"]').attr('checked', true);
    $("input[name='imgbackground'][value='AsaInvoice']").prop('checked', true);

    $("#CustomerID").val(customerId);

    $("#hf_CurrentPdfType").val('AsaInvoice');
    prgBar.style.display = "block";
    $.ajax({
        url: 'InvoiceList.aspx/OpenInvoice',
        type: "POST",
        contentType: 'application/json',
        data: "{InvoiceNo: '" + invoiceNumber + "',CustomerID:'" + customerId + "'}",
        dataType: 'json',
        success: function (sR) {
            //console.log(sR)
            const jsonData = JSON.parse(sR.d);

            // const InvoiceDataSet = jsonData.Tables;
            // var InvoiceDataSet = JSON.parse(sR.d);
            console.log(jsonData.Table)
            console.log(jsonData.Table1)
            console.log(jsonData.Table2)

            var CustomerData = jsonData.Table[0];
            var InvoiceData = jsonData.Table1[0];
            var invDetailsData = jsonData.Table2;
            if (CustomerData.ctype.toString() == '2') {
                $("#InvoiceDesclimer").hide();
                $("#IsSendWisetackPaymentLinkDIV").hide();
            } else {
                $("#InvoiceDesclimer").show();
                $("#IsSendWisetackPaymentLinkDIV").show();
            }
            //  alert(InvoiceData);
            if (InvoiceData != null) {
                console.log("IsConverted", InvoiceData.IsConverted)
                if (InvoiceData.IsConverted == '1') {
                    $("#div_Converted").show();

                    var convertHtmlText = "<a href=Invoice.aspx?InvNum=" + InvoiceData.ConvertedInvocieID.toString() + "&cId=" + CustomerData.CustomerGuid.toString() + "&InType=Invoice&AppID=" + InvoiceData.AppointmentId.toString() + ">" +
                        "<span class='badge badge-success' style='color: #fff;background-color: #28a745;font-size: 14px !important;font-weight: 400 !important;'><i class='fa fa-check-circle' style='font-size:14px;color: #fff !important;margin-right: 8px;'></i>" +
                        "Converted To Invoice # " + InvoiceData.ConvertedInvocieNumber.toString() + "</span></a>";

                    $("#div_Converted").html(convertHtmlText);
                }
                $("#qboCustID").text(CustomerData.qboCustID.toString().replace("0", ""));
                $("#qboInvID").text(InvoiceData.qboInvID.toString().replace("0", ""));
                $("#InvPrimaryID").val(invoiceNumber);

                $("#qboEstID").text(InvoiceData.qboEstID.toString().replace("0", ""));
                $("#ctype").val(InvoiceData.ctype);
                $("#lblCustomerID").text(CustomerData.CustomerID);
                $("#lblCustomerName").text(CustomerData.FullName);
                $("#lblAddress").text(CustomerData.Address1);
                $("#lblCity").text(CustomerData.City);
                $("#lblState").text(CustomerData.State);
                $("#lblZip").text(CustomerData.ZipCode);
                $("#lblPhone").text(CustomerData.Phone);
                $("#lblEmail").text(CustomerData.Email);

                $("#lblInvoiceNo").text(InvoiceData.Number);

                var link = "Invoice.aspx?InvNum=" + invoiceNumber + "&cId=" + CustomerData.CustomerGuid.toString() + "&InType=" + InvoiceData.Type.toString() + "&AppID=" + InvoiceData.AppointmentId.toString() + "";
                //$("#Link_Invocie").html(InvoiceData.Number);
                //$("#Link_Invocie").attr("href", link);

                $("#lblInvoiceNo").html("<a href=Invoice.aspx?InvNum=" + invoiceNumber + "&cId=" + CustomerData.CustomerGuid.toString() + "&InType=" + InvoiceData.Type.toString() + "&AppID=" + InvoiceData.AppointmentId.toString() + "&FromInvList=1>" + InvoiceData.Number + "</a>");
                $("#lblInvoiceDisplayNo").text(InvoiceData.DisplayNumber);
                $("#lblIssueDate").text(InvoiceData.IssueDate);
                $("#lblInvoiceTotal").text(InvoiceData.Total);
                $("#lblPaid").text(InvoiceData.AmountCollect);
                $("#lblPONO").text(InvoiceData.PONO);
                $("#bottom_Subtotal").text("$" + InvoiceData.Subtotal.toFixed(2));
                $("#discount").text("$" + InvoiceData.Discount.toFixed(2));
                $("#tax").text("$" + InvoiceData.Tax.toFixed(2));
                $("#bottom_Total").text("$" + InvoiceData.Total.toFixed(2));

                $("#_InvoiceNo").val(invoiceNumber);
                $('#table tbody > tr').remove();
                for (var i = 0; i < invDetailsData.length; i++) {
                    var itemDetails = "<tr>" +
                        "<th scope='row'>" + invDetailsData[i].ItemName + "</th>" +
                        "<td>" + invDetailsData[i].Description + "</td>" +
                        "<td>$ " + invDetailsData[i].uPrice + "</td>" +
                        "<td>" + invDetailsData[i].Quantity + "</td>" +
                        "<td>$ " + invDetailsData[i].TotalPrice + "</td>" +
                        "</tr>";
                    $('#itemBody').append(itemDetails);
                }
                if (InvoiceData.Type.toString() == "Invoice") {
                    $('#btn_Collect_Payment').show();
                    $('#h_Type').html("Invoice");
                    $('#Inv_Type').html("Invoice No:");
                    $('#IsSendPaymentLinkDIV').show();
                    $('#PaymentProcessSelect').show();
                }
                else {
                    $('#Inv_Type').html("Estimate No:");
                    $('#h_Type').html("Estimate");
                    //$('#btn_Collect_Payment').hide();
                    //$('#IsSendPaymentLinkDIV').hide();
                    //$('#PaymentProcessSelect').hide();
                }

                prgBar.style.display = "none";

                FillEmailModal(CustomerData.CustomerGuid.toString(), invoiceNumber);

                getWisetackPayment();
            }
        },
        error: function (error) {
            prgBar.style.display = "none";
            //alert(error);
        }
    })
}

function OpenInvoiceQB(invoiceNumber) {
    $.ajax({
        url: 'InvoiceList.aspx/OpenInvoiceQB',
        type: "POST",
        contentType: 'application/json',
        data: "{InvoiceId: '" + invoiceNumber + "'}",
        dataType: 'json',
        success: function (Data) {
            $("#itemBody").html(Data.d.TableData);

            $("#lblCustomerID").text(Data.d.CustID);
            $("#lblCustomerName").text(Data.d.CustName);
            $("#lblAddress").text(Data.d.Address);
            $("#lblCity").text(Data.d.City);
            $("#InvPrimaryID").val(invoiceNumber);
            console.log("qbid ", invoiceNumber)
            //$("#lblState").text(Data.d.TableData);
            //$("#lblZip").text(Data.d.TableData);
            //$("#lblPhone").text(Data.d.TableData);
            //$("#lblEmail").text(Data.d.TableData);
            $("#lblInvoiceNo").text(Data.d.InvNo);

            $("#lblInvoiceDisplayNo").text(Data.d.DisplayNumber);

            $("#lblIssueDate").text(Data.d.InvDate);
            $("#lblInvoiceTotal").text(Data.d.InvAmount);
            $("#lblPaid").text(Data.d.PaidAmount);

            ShowPopup();
        },
        error: function (error) {
            //   alert(error);
        }
    })
}

function ShowPopup() {
    var modal = document.getElementById('modal');
    modal.style.display = "block";
    $("#PaymentProcessSelect").val("");
    $("#wisetackDiv").hide();
}

function ClosePopup() {
    var modal = document.getElementById('modal');
    modal.style.display = "none";
    document.body.style.overflow = "auto";
}
$("#SearchBy").change(function () {
    var searchPerameter = $('#SearchBy').val();
    if (searchPerameter == "All") {
        $("#SearchValue").val("")
        $("#SearchValue").prop('disabled', true);
    } else if (searchPerameter == "Estimate") {
        $("#SearchValue").val("All Estimates")
        $("#SearchValue").prop('disabled', true);
    }
    else if (searchPerameter == "Invoice") {
        $("#SearchValue").val("All Invoices")
        $("#SearchValue").prop('disabled', true);
    }
    else {
        $("#SearchValue").css("display", "block");
        $("#SearchValue").prop('disabled', false);
    }
});

$('input[type=radio][name=imgbackground]').change(function () {
    $("#hf_CurrentPdfType").val($("input[name='imgbackground']:checked").val());
});
$(document).ready(function () {
    if ($("#SearchBy").val() == "All") {
        $("#SearchValue").val("")
        $("#SearchValue").prop('disabled', true);
    }
    $("#SearchBy").change();
    $('#example').dataTable({
        "scrollX": true,
        "scrollY": true,
        "order": [],
        "dom": 'Bfrtip',
        //"buttons": [
        //    'copy', 'csv', 'excel', 'pdf', 'print'
        //]
        "buttons": [

            {
                extend: 'excel',
                text: 'Excel',
                className: 'datatableButton',
                exportOptions: {
                    columns: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]
                }
            },
            {
                extend: 'print',
                exportOptions: {
                    columns: ':visible'
                },
                className: 'datatableButton',
                exportOptions: {
                    columns: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]
                }
            }
        ],
    });
})
//Added By Munem
function setEstimateToDefaultValue() {
    debugger;
    // Modified By Nobir
    var defaultTemplate = $("#h_Type").text(); // This could come from server-side too
    var isLHG = $('#isLHG').val();

    if (isLHG === 'true' && defaultTemplate != "Estimate") {
        defaultTemplate = "AsaInvoice";
    }
    else if (isLHG === 'true' && defaultTemplate == "Estimate") {
        defaultTemplate = "Estimate";
        $("#hf_CurrentPdfType").val("Estimate");
    }
    // Nobir

    // Set radio input as checked based on value
    $("input[name='imgbackground'][value='" + defaultTemplate + "']").prop("checked", true);
}
function SyncQuickBook() {
    $.alert({
        title: 'Xceleran',
        content: "<div id='contentArea' class='float-start'><p>QuickBooks Online Sync in progress.<img width='30' height='30' src='images/Rolling.gif' /><br><strong>Please Do not close this window.</strong></p></div>",
        icon: 'fa fa-info-circle',
        animation: 'scale',
        closeAnimation: 'scale',
        opacity: 0.5,
        onContentReady: function () {
            var returnvalue = false;
            $.ajax({
                type: "POST",
                url: "InvoiceList.aspx/SyncQBOItems",
                contentType: "application/json",
                dataType: "json",
                success: function (msg) {
                    $("#contentArea").html(msg.d);
                    $("#contentArea").append('<br>You can close this window now.');
                    returnvalue = true;
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    $("#contentArea").append('<br>Unexpected Error.');
                    returnvalue = true;
                }
            });
        },
        buttons: {
            moreButtons: {
                text: 'Close',
                action: function () {
                    window.location.href = "InvoiceList.aspx";
                    return returnvalue;
                }
            }
        }
    });
}

function FillEmailModal(customerGuid, InvoiceNo) {
    // alert("caled");
    //alert(customerGuid);
    $.ajax({
        url: 'Invoice.aspx/FillEmailModal',
        type: "Post",
        contentType: 'application/json',
        data: "{CustomerID:'" + customerGuid + "',InvoiceNo:'" + InvoiceNo + "',FromInvDtl:0}",
        dataType: 'json',
        success: function (sR) {
            if (sR.d != "0") {
                var data = $.parseJSON(sR.d);
                var doctype = $('#h_Type').html();
                $('#_Type').val(doctype);

                console.log(data)
                //  document.getElementById('fixAttachment').innerHTML = " " + doctype + ".pdf";
                if (doctype == "Invoice") {
                    $("#_EmailSubject").val(data.InvoiceMailSubject);
                    $("#EmailBody").val(data.InvoiceMailBody);
                }
                if (doctype == "Estimate") {
                    $("#_EmailSubject").val(data.ProposalMailSubject);
                    $("#EmailBody").val(data.ProposalMailBody);
                }

                $("#_CC").val(data.EmailCC);
                $("#_BCC").val(data.EmailBCC);
                $("#_To").val(data.EmailTo);
                $("#txt_ReqDepo").val('$' + data.ReqDepoAmt.toFixed(2));
                $("#file").val();
                $("#fileList").empty();
                // document.getElementById('fileList').innerHTML='<li><span class="remove-list fa fa-check"></span>Proposal.pdf</li>';
                $("#_InvoiceNo").val(InvoiceNo);
                //  $("#CustomerID").val(customerid);
            }
        },
        error: function (error) {
            //  alert(JSON.stringify(error));
        }
    })
}
function sendMailDivToggole() {
    //document.getElementById("InvoiceBlock").style.display = "none"
    //document.getElementById("modalCustomerMail").style.display = "block";
    $("#EmailBLock").show();
    $("#InvoiceBlock").hide();
    // added By Munem
    var isLHG = $('#isLHG').val();
    if (isLHG === 'true') {
        setEstimateToDefaultValue();
    }
}
function ShowDeposit() {
    $("#div_DepositBody").show();
    $("#div_Depositheader").show();
    var prgBar = document.getElementById("InvocieProgress");
    prgBar.style.display = "block";

    $.ajax({
        type: "POST",
        url: "Invoices.aspx/GetDepositsById",
        data: JSON.stringify({ 'iId': $("#InvPrimaryID").val() }),
        contentType: "application/json",
        dataType: "json",
        success: function (Data) {
            prgBar.style.display = "none";
            $("#DepositBody").html(Data.d.TableRow);
        }
    });
}

function CloseMailDiv() {
    $("#EmailBLock").hide();
    $("#InvoiceBlock").show();
}
updateList = function () {
    var input = document.getElementById('file');
    var output = document.getElementById('fileList');
    var children = "";
    for (var i = 0; i < input.files.length; ++i) {
        // children += '<li>' + input.files.item(i).name + '<span class="remove-list fa fa-check" onclick="return this.parentNode.remove()"></span>' + '</li>'
        children += '<li><span class="remove-list fa fa-check"></span>' + input.files.item(i).name + '</li>';

        // input.files.item(i).parent().remove();
    }

    output.innerHTML = children;
}
function validateDeposit() {
    // Get the values
    var totalAmount = parseFloat(document.getElementById('txt_TotalAmount').value.replace('$', '').trim()) || 0;
    var depositAmount = parseFloat(document.getElementById('txt_depositAmount').value.trim()) || 0;

    if (depositAmount > totalAmount) {
        $.alert({
            title: 'Xceleran',
            content: "Pay amount can not be greater than due amount.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });

        return false; // Prevent postback
    }

    return true; // Allow postback
}

// Nobir
function CollectPaymentNewPopup() {
    $("#CollectPaymentModalNew").modal('hide');
}
function checkIsAireMaster() {
    var isAireMasterValue = $("#isAireMaster").val();
    if (isAireMasterValue == 'true') {
        CollectPaymentNew();
    } else {
        CollectPayment();
    }
}
function CollectPaymentNew() {
    var totalStr = $("#lblInvoiceTotal").text().replace('$', '').replace(',', '').trim();
    var paidStr = $("#lblPaid").text().replace('$', '').replace(',', '').trim();
    var total = parseFloat(totalStr);
    var paid = parseFloat(paidStr);
    var due = total - paid;

    if (due <= 0) {
        $.alert({
            title: 'Xceleran',
            content: "Invoice already paid.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'Okay',
                    btnClass: 'btn-blue',
                    action: function () { }
                }
            }
        });
        return;
    }

    $("#txt_DueAmountNew").val(due);
    $("#txt_PaymentAmountNew").val("$" + due);

    $("#CollectPaymentModalNew").modal('show');
}

function ValidationNew() {
    var dueAmount = parseFloat($("input#txt_DueAmountNew").val().replace('$', '').replace(',', '').trim()) || 0;
    var paymentAmount = parseFloat($("input#txt_PaymentAmountNew").val().replace('$', '').replace(',', '').trim()) || 0;

    //console.log("Due Amount: " + $("input#txt_DueAmountNew").val());
    //console.log("Payment Amount: " + paymentAmount);
    //debugger;

    if (paymentAmount <= 0) {
        $.alert({
            title: 'Xceleran',
            content: "Please enter a valid payment amount.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });
        return false;
    }
    if (paymentAmount > dueAmount) {
        $.alert({
            title: 'Xceleran',
            content: "Pay amount can not be greater than due amount.",
            icon: 'fa fa-info-circle',
            animation: 'scale',
            closeAnimation: 'scale',
            opacity: 0.5,
            buttons: {
                okay: {
                    text: 'okay',
                    btnClass: 'btn-blue',
                    action: function () {
                    }
                }
            }
        });

        return false;
    }

    return true;
}

$("#proceedToPaymentNew").click(function () {
    if (!ValidationNew()) {
        return;
    }

    var invid = $("#InvPrimaryID").val();
    var cID = $("#lblCustomerID").text();
    var qboCustID = $("#qboCustID").text();
    var qboInvID = $("#qboInvID").text();
    var qboEstID = $("#qboEstID").text();
    var paymentProcess = "gpi";
    var selectPaymentProcess = $("#PaymentProcessSelect").val();

    if (selectPaymentProcess != null && selectPaymentProcess != "") {
        paymentProcess = selectPaymentProcess;
    }

    if (paymentProcess === "gpi") {
        $.ajax({
            url: 'InvoiceList.aspx/GetPaymenUrl',
            type: "POST",
            contentType: 'application/json',
            data: JSON.stringify({
                InvoiceNo: invid,
                CustomerID: cID,
                qboCustID: qboCustID,
                qboInvID: qboInvID,
                qboEstID: qboEstID,
                paymentProcess: paymentProcess,
                ctype: $("#ctype").val()
            }),
            dataType: 'json',
            success: function (sR) {
                var url = sR.d;
                console.log(url);
                window.open(url, '_blank').focus();
                $("#CollectPaymentModalNew").modal('hide');
            },
            error: function (error) {
                console.error("Error fetching payment URL:", error);
                alert("An error occurred while fetching the payment URL.");
            }
        });
    }
});

// Nobir
