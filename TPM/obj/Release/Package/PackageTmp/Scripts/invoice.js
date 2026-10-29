const sendBtn = document.getElementById('sendBtn');
const viewBtn = document.getElementById('viewBtn');
const progressGIF = document.getElementById('progressGIF');
const selectAll = document.getElementById('selectAll');
const searchByPaymentStatus = document.getElementById('searchByPaymentStatus');
sendBtn.addEventListener('click', sendInvoices);
viewBtn.addEventListener('click', viewInvoice);
let table = null;
ClosePopup();
loadData();

function loadData() {
    table = $('#invoiceTable').DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": 'Invoice.aspx/GetCustomerInvoices',
            "type": "POST",
            "contentType": "application/json; charset=utf-8",
            "dataType": "json",
            "data": function (d) {
                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    searchValue: d.search.value,
                    sortColumn: d.columns[d.order[0].column].data,
                    sortDirection: d.order[0].dir,
                    invoiceStatus: $('#hiddenSearchByPaymentStatus').val(),
                    fromDate: $('#fromDate').val(),
                    toDate: $('#toDate').val()
                });
            },
            "dataSrc": function (json) {
                const parsed = JSON.parse(json.d);
                json.draw = parsed.draw;
                json.recordsTotal = parsed.recordsTotal;
                json.recordsFiltered = parsed.recordsFiltered;
                console.log(parsed);
                if (parsed.error) {
                    alert(parsed.error);
                    return [];
                }
                return parsed.data;
            }
        },
        "paging": true,
        "pageLength": 10,
        "lengthMenu": [5, 10, 25, 50],
        "columns": [
            {
                "data": 'ID',
                render: function (data, type, row) {
                    const rowData = encodeURIComponent(JSON.stringify(row));
                    return `
                        <div class="action-dropdown" data-row="${rowData}">
                            <input type="checkbox" class="row-select" value="${rowData}" style="display: none;" />
                            <i class="fas fa-ellipsis-v action-icon"></i>
                            <div class="action-menu">
                                <div class="action-menu-item" data-action="send">Send</div>
                                <div class="action-menu-item" data-action="view">View</div>
                            </div>
                        </div>
                    `;
                },
                orderable: false
            },
            { "data": 'CustomerName', orderable: false },
            { "data": 'InvoiceType' },
            { "data": 'InvoiceNumber' },
            { "data": 'InvoiceDate' },
            { "data": 'Subtotal' },
            { "data": 'Discount' },
            { "data": 'Tax' },
            { "data": 'Total' },
            { "data": 'Due' },
            { "data": 'DepositAmount' },
            { "data": 'InvoiceStatus' }
        ],
    });

    // Handle dropdown click
    $('#invoiceTable').on('click', '.action-icon', function (e) {
        e.stopPropagation();
        const dropdown = $(this).parent();
        const menu = dropdown.find('.action-menu');
        $('.action-menu').not(menu).hide(); // Close other open menus
        menu.toggle(); // Toggle the clicked menu
    });

    // Handle menu item click
    $('#invoiceTable').on('click', '.action-menu-item', function (e) {
        e.stopPropagation();
        const action = $(this).data('action');
        const dropdown = $(this).closest('.action-dropdown');
        const rowDataStr = decodeURIComponent(dropdown.data('row'));
        const rowData = JSON.parse(rowDataStr);
        $('.action-menu').hide(); // Close the menu

        if (action === 'send') {
            // Simulate checkbox selection for send
            dropdown.find('.row-select').prop('checked', true);
            sendInvoices({ preventDefault: () => { } });
        } else if (action === 'view') {
            // Simulate checkbox selection for view
            dropdown.find('.row-select').prop('checked', true);
            viewInvoice({ preventDefault: () => { } });
        }
    });

    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.action-dropdown').length) {
            $('.action-menu').hide();
        }
    });
}

$('#searchBtn').on('click', function (e) {
    e.preventDefault();
    let fromDate = $('#fromDate').val();
    let toDate = $('#toDate').val();
    if (!fromDate || !toDate) {
        alert("Please select both From Date and To Date.");
        return;
    }
    if (new Date(fromDate) > new Date(toDate)) {
        alert("From Date cannot be after To Date.");
        return;
    }
    table.draw();
});

$('#clearBtn').on('click', function (e) {
    e.preventDefault();
    $('#fromDate').val('');
    $('#toDate').val('');
    $('#hiddenSearchByPaymentStatus').val('');
    $('#searchByPaymentStatus .dropdown-selected span').text('All');
    table.draw();
});

function sendInvoices(e) {
    e.preventDefault();
    const selected = getSelectedIds();
    if (selected.length === 0) {
        alert('Please select at least one invoice/estimate to send');
        return;
    }
    progressGIF.style.display = 'inline';
    setTimeout(() => {
        progressGIF.style.display = 'none';
        alert(`Sending ${selected.length} selected items (simulated)`);
        // Clear selections after sending
        document.querySelectorAll('.row-select').forEach(cb => cb.checked = false);
    }, 2000);
}

function viewInvoice(e) {
    e.preventDefault();
    const selected = getSelectedRows();
    if (selected.length === 0 || selected.length > 1) {
        alert('Please select one invoice/estimate to view');
        return;
    }
    console.log(selected[0].CustomerID);
    console.log(selected[0].ID);
    loadInvoiceData(selected[0].ID, selected[0].CustomerID);
    // Clear selections after viewing
    document.querySelectorAll('.row-select').forEach(cb => cb.checked = false);
}

selectAll.addEventListener('change', (e) => {
    document.querySelectorAll('.row-select').forEach(cb => {
        cb.checked = e.target.checked;
    });
});

function getSelectedIds() {
    const checkboxes = document.querySelectorAll('.row-select:checked');
    return Array.from(checkboxes).map(cb => parseInt(cb.value));
}

function getSelectedRows() {
    const checkboxes = document.querySelectorAll('.row-select:checked');
    return Array.from(checkboxes).map(cb => {
        const rowDataStr = decodeURIComponent(cb.value);
        return JSON.parse(rowDataStr);
    });
}

function loadInvoiceData(invoiceNumber, customerId) {
    $.ajax({
        url: 'Invoice.aspx/OpenInvoice',
        type: "POST",
        contentType: 'application/json',
        data: "{InvoiceNo: '" + invoiceNumber + "',CustomerID:'" + customerId + "'}",
        dataType: 'json',
        success: function (sR) {
            console.log('OpenInvoice response:', sR);
            const jsonData = JSON.parse(sR.d);
            console.log('Table:', jsonData.Table);
            console.log('Table1:', jsonData.Table1);
            console.log('Table2:', jsonData.Table2);

            if (!jsonData.Table1 || !jsonData.Table1[0]) {
                console.error('Invoice data not found');
                alert('Failed to load invoice details.');
                return;
            }

            var CustomerData = jsonData.Table[0];
            var InvoiceData = jsonData.Table1[0];
            var invDetailsData = jsonData.Table2;

            // Set Subtotal with fallback
            $("#MainContent_bottom_Subtotal").text("$" + (InvoiceData.Subtotal || '0.00'));

            // Rest of the existing code (CustomerName, Address, etc.)
            if (CustomerData.ctype.toString() == '2') {
                $("#InvoiceDesclimer").hide();
                $("#IsSendWisetackPaymentLinkDIV").hide();
            } else {
                $("#InvoiceDesclimer").show();
                $("#IsSendWisetackPaymentLinkDIV").show();
            }

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
            $("#lblInvoiceNo").html("<a href=Invoice.aspx?InvNum=" + invoiceNumber + "&cId=" + CustomerData.CustomerGuid.toString() + "&InType=" + InvoiceData.Type.toString() + "&AppID=" + InvoiceData.AppointmentId.toString() + ">" + InvoiceData.Number + "</a>");
            $("#lblInvoiceDisplayNo").text(InvoiceData.DisplayNumber);
            $("#lblIssueDate").text(InvoiceData.IssueDate);
            $("#lblInvoiceTotal").text(InvoiceData.Total);
            $("#bottom_Subtotal").text("$" + (InvoiceData.Subtotal || '0.00'));
            $("#MainContent_discount").text("$" + (InvoiceData.Discount || '0.00'));
            $("#MainContent_tax").text("$" + (InvoiceData.Tax || '0.00'));
            $("#MainContent_bottom_Total").text("$" + (InvoiceData.Total || '0.00'));
            $("#_InvoiceNo").val(invoiceNumber);

            $('#table tbody > tr').remove();
            for (var i = 0; i < invDetailsData.length; i++) {
                var itemDetails = "<tr>" +
                    "<td>" + invDetailsData[i].ItemName + "<br>" + invDetailsData[i].Description + "</td>" +
                    "<td>$ " + invDetailsData[i].uPrice + "</td>" +
                    "<td>" + invDetailsData[i].Quantity + "</td>" +
                    "<td>$ " + invDetailsData[i].TotalPrice + "</td>" +
                    "</tr>";
                $('#itemBody').append(itemDetails);
            }

            if (InvoiceData.Type.toString() == "Invoice") {
                $('#btn_Collect_Payment').show();
                $('#h_Type').html("Invoice " + InvoiceData.Number);
                $('#IsSendPaymentLinkDIV').show();
                $('#PaymentProcessSelect').show();
            } else {
                $('#h_Type').html("Estimate " + InvoiceData.Number);
            }

            ShowPopup();
        },
        error: function (error) {
            console.error('AJAX error:', error);
            alert('Error loading invoice data.');
        }
    });
}

function ShowPopup() {
    var modal = document.getElementById('modal');
    modal.style.display = "block";
    $("#PaymentProcessSelect").val("");
}

function ClosePopup() {
    var modal = document.getElementById('modal');
    modal.style.display = "none";
    document.body.style.overflow = "auto";
}

function FillEmailModal(customerGuid, InvoiceNo) {
    $.ajax({
        url: 'Invoice.aspx/FillEmailModal',
        type: "Post",
        contentType: 'application/json',
        data: "{CustomerID:'" + customerGuid + "',InvoiceNo:'" + InvoiceNo + "'}",
        dataType: 'json',
        success: function (sR) {
            if (sR.d != "0") {
                var data = $.parseJSON(sR.d);
                var doctype = $('#h_Type').html();
                $('#_Type').val(doctype);
                console.log(data);
                if (doctype.includes("Invoice")) {
                    $("#_EmailSubject").val(data.InvoiceMailSubject);
                    $("#EmailBody").val(data.InvoiceMailBody);
                }
                if (doctype.includes("Estimate")) {
                    $("#_EmailSubject").val(data.ProposalMailSubject);
                    $("#EmailBody").val(data.ProposalMailBody);
                }
                $("#_CC").val(data.EmailCC);
                $("#_BCC").val(data.EmailBCC);
                $("#_To").val(data.EmailTo);
                $("#file").val();
                $("#fileList").empty();
                $("#_InvoiceNo").val(InvoiceNo);
            }
        },
        error: function (error) {
            console.error('Error:', error);
        }
    });
}

function sendMailDivToggole() {
    $('#EmailBLock').toggleClass('d-none');
    $('#InvoiceBlock').toggleClass('d-none');
}

function CloseMailDiv() {
    $('#EmailBLock').addClass('d-none');
    $('#InvoiceBlock').removeClass('d-none');
}

function ShowDeposit() {
}

function CollectPayment() {
}

// Custom Dropdown Functionality for Payment Status
document.addEventListener('DOMContentLoaded', function () {
    const dropdown = document.querySelector('#searchByPaymentStatus');
    const selected = dropdown.querySelector('.dropdown-selected');
    const optionsContainer = dropdown.querySelector('.dropdown-options');
    const options = dropdown.querySelectorAll('.dropdown-option');
    const hiddenInput = dropdown.querySelector('#hiddenSearchByPaymentStatus');

    // Toggle dropdown on click
    selected.addEventListener('click', function (e) {
        e.stopPropagation();
        const isOpen = optionsContainer.style.display === 'block';
        optionsContainer.style.display = isOpen ? 'none' : 'block';
        // Disable pointer events on buttons when dropdown is open
        document.querySelector('#sendBtn').style.pointerEvents = isOpen ? 'auto' : 'none';
        document.querySelector('#viewBtn').style.pointerEvents = isOpen ? 'auto' : 'none';
    });

    // Handle option selection
    options.forEach(option => {
        option.addEventListener('click', function () {
            const value = this.getAttribute('data-value');
            const text = this.textContent;
            selected.querySelector('span').textContent = text;
            hiddenInput.value = value;
            optionsContainer.style.display = 'none';
            // Re-enable pointer events on buttons
            document.querySelector('#sendBtn').style.pointerEvents = 'auto';
            document.querySelector('#viewBtn').style.pointerEvents = 'auto';

            // Trigger DataTable redraw
            table.draw();
        });
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function () {
        optionsContainer.style.display = 'none';
        // Re-enable pointer events on buttons
        document.querySelector('#sendBtn').style.pointerEvents = 'auto';
        document.querySelector('#viewBtn').style.pointerEvents = 'auto';
    });
});