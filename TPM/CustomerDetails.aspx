<%@ Page Title="Customer Site Details" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true"
    CodeBehind="CustomerDetails.aspx.cs" Inherits="TPM.CustomerDetails" %>

    <asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
        <!-- External CSS -->
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet"
            integrity="sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN" crossorigin="anonymous">
        <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css">
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" rel="stylesheet">
        <link rel="stylesheet" type="text/css"
            href="https://cdn.jsdelivr.net/npm/daterangepicker/daterangepicker.css" />

        <link rel="stylesheet" href="Content/customerdetails.css">
        <link rel="stylesheet" href="Content/customer.css">
        <style>
            #siteAppointmentDetailsModal_PopUP .modal-dialog {
                max-width: 95vw;
                width: 95vw;
                margin: auto;
            }
            #siteAppointmentDetailsModal_PopUP .modal-content {
                max-width: 100%;
                width: 100%;
                height: 90vh;
            }
        </style>

        <!-- Inline Styles -->
        <style>
            #appointments .table-responsive {
                overflow: visible;
            }

            #appointments .dropdown-menu {
                z-index: 1080;
            }

            .icon-btn.dropdown-toggle::after {
                display: none !important;
            }
        </style>

        <div class="custdet-main-container">
            <h1 class="display-6 mb-4">
                <asp:Label ID="lblSiteName" runat="server" />
            </h1>

            <!-- Hidden Labels to pass data from Server to JavaScript -->
            <asp:Label Style="display: none;" ID="lblCustomerId" runat="server" />
            <asp:Label Style="display: none;" ID="lblSiteId" runat="server" />
            <asp:Label Style="display: none;" ID="lblCustomerGuid" runat="server" />
            <asp:Label Style="display: none;" ID="lblAppointmentId" runat="server" />

            <asp:Label Style="display: none;" ID="lblStreetAddress" runat="server" />
            <asp:Label Style="display: none;" ID="lblCity" runat="server" />
            <asp:Label Style="display: none;" ID="lblState" runat="server" />
            <asp:Label Style="display: none;" ID="lblZip" runat="server" />
            <asp:Label Style="display: none;" ID="lblCountry" runat="server" />

            <!-- Tab Navigation -->
            <div class="mb-3">
                <h3 id="cslHeader" class="h5 mb-0"><span id="cslCustomerNameSpan"></span>: <span
                        id="cslSiteNameSpan"></span></h3>
            </div>
            <script type="text/javascript">
                // Set CSL header from the main labels
                document.addEventListener('DOMContentLoaded', function () {
                    var customerNameLabel = document.getElementById('<%= lblCustomerName.ClientID %>');
                    var siteNameLabel = document.getElementById('<%= lblSiteName.ClientID %>');
                    var cslCustomerNameSpan = document.getElementById('cslCustomerNameSpan');
                    var cslSiteNameSpan = document.getElementById('cslSiteNameSpan');

                    if (customerNameLabel && cslCustomerNameSpan) {
                        var customerNameText = customerNameLabel.innerText || customerNameLabel.textContent || '';
                        // Remove any HTML tags
                        customerNameText = customerNameText.replace(/<[^>]*>/g, '').trim();
                        cslCustomerNameSpan.textContent = customerNameText;
                    }

                    if (siteNameLabel && cslSiteNameSpan) {
                        // Extract just the site name (remove HTML tags if any)
                        var siteNameText = siteNameLabel.innerText || siteNameLabel.textContent || '';
                        // Remove the "(Customer Location - Default)" part if it exists
                        siteNameText = siteNameText.replace(/\s*\(Customer Location - Default\)\s*/gi, '').trim();
                        // If it still contains the customer name, extract just the site part
                        if (siteNameText.includes('(')) {
                            var match = siteNameText.match(/\(([^)]+)\)/);
                            if (match) {
                                siteNameText = match[1];
                            }
                        }
                        // If empty or still contains customer name, use default
                        if (!siteNameText || siteNameText.length < 3) {
                            siteNameText = 'Customer Location (Default)';
                        }
                        cslSiteNameSpan.textContent = siteNameText;
                    }
                });
            </script>
            <ul class="nav nav-tabs mb-3" id="custdetTabs" role="tablist">
                <li class="nav-item" role="presentation">
                    <button class="nav-link active" id="basic-tab" data-bs-toggle="tab" data-bs-target="#basic"
                        type="button" role="tab" aria-controls="basic" aria-selected="true">Basic Information</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="appointments-tab" data-bs-toggle="tab" data-bs-target="#appointments"
                        type="button" role="tab" aria-controls="appointments"
                        aria-selected="false">Appointments</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="invoices-tab" data-bs-toggle="tab" data-bs-target="#invoices"
                        type="button" role="tab" aria-controls="invoices"
                        aria-selected="false">Invoices/Estimates</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="notes-tab" data-bs-toggle="tab" data-bs-target="#notes" type="button"
                        role="tab" aria-controls="notes" aria-selected="false">Notes</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="forms-tab" data-bs-toggle="tab" data-bs-target="#forms" type="button"
                        role="tab" aria-controls="forms" aria-selected="false">Forms</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="equipment-tab" data-bs-toggle="tab" data-bs-target="#equipment"
                        type="button" role="tab" aria-controls="equipment" aria-selected="false">Equipment</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="pictures-tab" data-bs-toggle="tab" data-bs-target="#pictures"
                        type="button" role="tab" aria-controls="pictures" aria-selected="false">Pictures</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="files-tab" data-bs-toggle="tab" data-bs-target="#files" type="button"
                        role="tab" aria-controls="files" aria-selected="false">Files</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="agreements-tab" data-bs-toggle="tab" data-bs-target="#agreements"
                        type="button" role="tab" aria-controls="agreements" aria-selected="false">Maintenance
                        Agreements</button>
                </li>
            </ul>

            <!-- Tab Content -->
            <div class="tab-content" id="custdetTabContent">
                <!-- Basic Information -->
                <div class="tab-pane fade show active" id="basic" role="tabpanel" aria-labelledby="basic-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Basic Information</h2>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col">Field</th>
                                        <th scope="col">Value</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Site Name</td>
                                        <td>
                                            <asp:Label ID="lblSiteNameTable" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Customer Name</td>
                                        <td>
                                            <asp:Label ID="lblCustomerName" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Customer Location</td>
                                        <td>
                                            <asp:Label ID="lblCustomerLocation" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Site Contact</td>
                                        <td>
                                            <!-- Main Contact Name -->
                                            <div class="contact-line">
                                                <i class="fas fa-user fa-fw me-2"></i>
                                                <asp:Label ID="lblContact" runat="server" />
                                            </div>
                                            <!-- Phone Number -->
                                            <div class="contact-line">
                                                <i class="fas fa-phone-alt fa-fw me-2"></i>
                                                <asp:HyperLink ID="hlPhone" runat="server">
                                                </asp:HyperLink>
                                            </div>

                                            <!-- Mobile Number -->
                                            <div class="contact-line">
                                                <i class="fas fa-mobile-alt fa-fw me-2"></i>
                                                <asp:HyperLink ID="hlMobile" runat="server">
                                                </asp:HyperLink>
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td>Email</td>
                                        <td>
                                            <asp:HyperLink ID="hlEmail" runat="server" CssClass="details-email-link" />
                                            <a id="btnSendEmail" class="btn btn-sm btn-outline-primary ms-2" href="#"
                                                title="Send Email" data-bs-toggle="modal"
                                                data-bs-target="#sendEmailModal">
                                                <i class="fas fa-envelope me-1"></i>Send
                                            </a>
                                            <a id="btnEmailHistory" runat="server"
                                                class="btn btn-sm btn-outline-info ms-2" href="#"
                                                title="View Email History" target="_blank">
                                                <i class="fas fa-history me-1"></i>History
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Address</td>
                                        <td>
                                            <div class="cust-site-address-group">
                                                <p class="cust-site-info">
                                                    <i class="fas fa-map-marker-alt fa-fw"></i> <strong>Street:</strong> <asp:Label ID="lblAddress" runat="server" />
                                                </p>
                                                <p class="cust-site-info">
                                                    <i class="fas fa-city fa-fw"></i> <strong>City:</strong> <asp:Label ID="lbl_City" runat="server" />
                                                </p>
                                                <p class="cust-site-info">
                                                    <i class="fas fa-flag fa-fw"></i> <strong>State:</strong> <asp:Label ID="lbl_State" runat="server" />
                                                </p>
                                                <p class="cust-site-info">
                                                    <i class="fas fa-mail-bulk fa-fw"></i> <strong>Zip:</strong> <asp:Label ID="lbl_Zip" runat="server" />
                                                </p>
                                                <p class="cust-site-info">
                                                    <i class="fas fa-globe-americas fa-fw"></i> <strong>Country:</strong> USA
                                                </p>
                                            </div>

                                            
                                        </td>
                                    </tr>
                                     
                                   
                                    <tr>
                                        <td>Status</td>
                                        <td>
                                            <asp:Label ID="lblActive" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Special Instructions</td>
                                        <td>
                                            <asp:Label ID="lblNote" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Created On</td>
                                        <td>
                                            <asp:Label ID="lblCreatedOn" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Communications</td>
                                        <td>
                                            <a id="btnSms" class="btn btn-sm btn-outline-primary me-2"
                                                href="javascript:void(0);" onclick="return false;" title="Send SMS">
                                                <i class="fa-solid fa-message me-1"></i>SMS
                                            </a>
                                            <a id="btnMms" class="btn btn-sm btn-outline-dark me-2"
                                                href="javascript:void(0);" onclick="return false;" title="Send MMS">
                                                <i class="fa-solid fa-photo-film me-1"></i>MMS
                                            </a>
                                            <%--<a id="btnTextHistory" class="btn btn-sm btn-outline-info" href="#"
                                                title="View Text Message History">
                                                <i class="fas fa-history me-1"></i>Text History
                                                </a>--%>
                                                <small id="smsHint" class="text-muted ms-2 d-none">Works best on
                                                    mobile</small>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Appointments -->
                <div class="tab-pane fade" id="appointments" role="tabpanel" aria-labelledby="appointments-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Appointments</h2>
                        <div class="custdet-controls mb-3 d-flex align-items-center flex-wrap gap-2">
                            <div class="input-group">
                                <input type="text" id="apptSearch" class="form-control"
                                    placeholder="Search appointments...">
                                <asp:DropDownList ID="apptFilter" runat="server" CssClass="form-select" />
                                <asp:DropDownList ID="ticketStatus" runat="server" CssClass="form-select" />
                            </div>
                            <div class="input-group" style="width: auto;">
                                <input type="text" id="apptDateRangePicker" class="form-control"
                                    placeholder="Search by Date">
                                <button class="btn btn-outline-secondary" type="button" id="btnApptClearDate"
                                    title="Clear Date Filter">
                                    <i class="fa fa-times text-danger"></i>
                                </button>
                                <span class="input-group-text"><i class="fa fa-calendar"></i></span>
                            </div>
                            <button type="button" id="apptExport" class="btn btn-primary d-none">Export to
                                Excel</button>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppoinmentId">Appointment ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="RequestDate">Request Date
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="TimeSlot">Time Slot <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="ServiceType">Appointment Type
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="AppoinmentStatus">Status <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="ResourceName">Resource Name
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="TicketStatus">Ticket Status
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col">CSL Updates</th>

                                    </tr>
                                </thead>
                                <tbody id="apptTableBody"></tbody>
                            </table>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-3">
                            <button type="button" id="apptPrev" class="btn btn-outline-secondary">Previous</button>
                            <span id="apptPageInfo" class="text-muted">Page 1 of 1</span>
                            <button type="button" id="apptNext" class="btn btn-outline-secondary">Next</button>
                        </div>
                    </div>
                </div>

                <div class="tab-pane fade show" id="notes" role="tabpanel" aria-labelledby="notes-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Notes</h2>
                        <div
                            class="custdet-controls mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                            <div class="d-flex align-items-center flex-wrap gap-2">
                                <input type="text" id="notesSearch" class="form-control" placeholder="Search notes..."
                                    style="width: 200px;">
                                <select id="notesSortColumn" class="form-select" style="width: auto;">
                                    <option value="Date/Time">Sort by Date/Time</option>
                                    <option value="User ID">Sort by User ID</option>
                                </select>
                                <select id="notesSortDirection" class="form-select" style="width: auto;">
                                    <option value="desc">Newest First</option>
                                    <option value="asc">Oldest First</option>
                                </select>
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <button type="button" id="addNoteBtn" class="btn btn-outline-primary eqp"
                                    data-bs-toggle="modal" data-bs-target="#noteModal">
                                    <i class="fas fa-plus me-2"></i>Create Note
                                </button>
                                <button type="button" id="notesExport" class="btn btn-outline-success">
                                    <i class="fas fa-file-excel me-2"></i>Export
                                </button>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppointmentId">Appointment ID
                                        </th>
                                        <th scope="col">Note</th>
                                        <th scope="col" class="sortable-header" data-sort="CreatedAt">Date Created</th>
                                        <th scope="col" class="sortable-header" data-sort="Reference">Reference</th>
                                        <th scope="col" class="sortable-header" data-sort="UserId">User ID</th>
                                        <th scope="col" style="width: 1%;">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="notesTableBody"></tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Invoices & Estimates -->
                <div class="tab-pane fade" id="invoices" role="tabpanel" aria-labelledby="invoices-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Invoices & Estimates</h2>
                        <div class="custdet-controls mb-3 d-flex align-items-center flex-wrap gap-2">
                            <div class="input-group">
                                <input type="text" id="invSearch" class="form-control" placeholder="Search invoices...">
                                <select id="invFilter" class="form-select">
                                    <option value="all">All Status</option>
                                    <option value="unpaid">Due</option>
                                    <option value="paid">Paid</option>
                                </select>
                                <select id="invFilterType" class="form-select">
                                    <option value="all">All Type</option>
                                    <option value="invoice">Invoice</option>
                                    <option value="estimate">Estimate</option>
                                </select>
                                <button id="invDateRangePicker" class="btn btn-outline-secondary">
                                    <i class="fa fa-calendar"></i>&nbsp;
                                    <span>Date Range</span> <i class="fa fa-caret-down"></i>
                                </button>

                            </div>
                            <!-- Create Invoice/Estimate buttons removed per requirements -->
                            <button type="button" id="invExport" class="btn btn-primary d-none">Export to Excel</button>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppointmentId">Appointment ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="InvoiceNumber">Number <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="InvoiceType">Type <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="InvoiceDate">Date <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Subtotal">Subtotal <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Discount">Discount <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Tax">Tax <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Total">Total <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Due">Due <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="DepositAmount">Deposit <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="InvoiceStatus">Status <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="invTableBody"></tbody>
                            </table>

                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-3">
                            <button type="button" id="invPrev" class="btn btn-outline-secondary">Previous</button>
                            <span id="invPageInfo" class="text-muted">Page 1 of 1</span>
                            <button type="button" id="invNext" class="btn btn-outline-secondary">Next</button>
                        </div>
                    </div>
                </div>

                <!-- Equipment -->
                <div class="tab-pane fade" id="equipment" role="tabpanel" aria-labelledby="equipment-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Equipment</h2>
                        <div
                            class="custdet-controls mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                            <div class="input-group" style="width: auto;">
                                <input type="text" id="equipSearch" class="form-control col-4"
                                    placeholder="Search equipment..." style="width: 200px;">
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <button type="button" id="equipAdd" class="btn btn-outline-primary eqp"
                                    data-bs-toggle="modal" data-bs-target="#equipModal">
                                    <i class="fas fa-wrench me-2"></i>Add Equipment
                                </button>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="EquipmentType">Type <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="SerialNumber">Serial Number
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="Make">Make <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Model">Model <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="WarrantyStart">Warranty Start
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="WarrantyEnd">Warranty End <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="LaborWarrantyStart">Labor
                                            Warranty Start <i class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="LaborWarrantyEnd">Labor
                                            Warranty End <i class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Barcode">SKU <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="InstallDate">Install Date <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col">Notes</th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="equipTableBody"></tbody>
                            </table>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-3">
                            <button type="button" id="eqpPrev" class="btn btn-outline-secondary">Previous</button>
                            <span id="eqpPageInfo" class="text-muted">Page 1 of 1</span>
                            <button type="button" id="eqpNext" class="btn btn-outline-secondary">Next</button>
                        </div>
                    </div>
                </div>

                <!-- Forms Tab -->
                <div class="tab-pane fade" id="forms" role="tabpanel" aria-labelledby="forms-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Forms</h2>
                        <div class="custdet-controls mb-3 d-flex align-items-center flex-wrap gap-2">
                            <div class="input-group">
                                <input type="text" id="formsSearch" class="form-control" placeholder="Search forms...">
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppointmentId">Appointment ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="FormName">Form Name/Content
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="DateAdded">Date Added <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Reference">Reference</th>
                                        <th scope="col" class="sortable-header" data-sort="UploadedBy">User ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="formsTableBody">
                                    <tr>
                                        <td colspan="6" class="text-center text-muted">Loading forms...</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Pictures Tab -->
                <div class="tab-pane fade" id="pictures" role="tabpanel" aria-labelledby="pictures-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Pictures</h2>
                        <div
                            class="custdet-controls mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                            <div class="input-group" style="width: auto;">
                                <input type="text" id="picturesSearch" class="form-control"
                                    placeholder="Search pictures..." style="width: 200px;">
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <button type="button" id="uploadPictureBtnModal" class="btn btn-outline-primary eqp"
                                    data-bs-toggle="modal" data-bs-target="#pictureUploadModal">
                                    <i class="fas fa-camera me-2"></i>Upload Pictures
                                </button>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppointmentId">Appointment ID
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="FileName">Picture
                                            Name/Content <i class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="UploadDate">Date Added <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Reference">Reference</th>
                                        <th scope="col" class="sortable-header" data-sort="UploadedBy">User ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="picturesTableBody"></tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Maintenance Agreements -->
                <div class="tab-pane fade" id="agreements" role="tabpanel" aria-labelledby="agreements-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Maintenance Agreements</h2>
                        <div
                            class="custdet-controls mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                            <div class="input-group" style="width: auto;">
                                <input type="text" id="agreeSearch" class="form-control"
                                    placeholder="Search agreements..." style="width: 200px;">
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <button type="button" id="addAgreementBtn" class="btn btn-outline-primary eqp"
                                    data-bs-toggle="modal" data-bs-target="#agreeModal">
                                    <i class="fas fa-file-contract me-2"></i>Upload Agreement
                                </button>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="UploadDate">Date Added <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="FileName">Agreement
                                            Name/Content <i class="fas fa-sort"></i></th>
                                        <th scope="col">Expiration Date</th>
                                        <th scope="col">Alarm Date</th>
                                        <th scope="col">Alarm Set</th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="agreementTableBody"></tbody>
                            </table>
                        </div>
                    </div>
                </div>
                <!-- Files Tab -->
                <div class="tab-pane fade" id="files" role="tabpanel" aria-labelledby="files-tab">
                    <div class="custdet-container">
                        <h2 class="h4 mb-3">Files</h2>
                        <div
                            class="custdet-controls mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
                            <div class="input-group" style="width: auto;">
                                <input type="text" id="filesSearch" class="form-control" placeholder="Search files..."
                                    style="width: 200px;">
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <button type="button" id="uploadFileBtnModal" class="btn btn-outline-primary eqp"
                                    data-bs-toggle="modal" data-bs-target="#fileUploadModal">
                                    <i class="fas fa-file-upload me-2"></i>Upload Files
                                </button>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered table-hover">
                                <thead class="table-light">
                                    <tr>
                                        <th scope="col" class="sortable-header" data-sort="AppointmentId">Appointment ID
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="FileName">File Name/Content
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col" class="sortable-header" data-sort="UploadDate">Date Added <i
                                                class="fas fa-sort"></i></th>
                                        <th scope="col" class="sortable-header" data-sort="Reference">Reference</th>
                                        <th scope="col" class="sortable-header" data-sort="UploadedBy">User ID
                                            <i class="fas fa-sort"></i>
                                        </th>
                                        <th scope="col">Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="filesTableBody"></tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Back Button -->
            <div class="custdet-container text-end mt-4">
                <a href="javascript:void(0);" onclick="history.back();" class="btn btn-outline-secondary">Back to Customers</a>
            </div>
            <!-- Status History Modal -->
            <div class="modal fade" id="statusHistoryModal" tabindex="-1" aria-labelledby="statusHistoryModalLabel"
                aria-hidden="true">
                <div class="modal-dialog modal-dialog-scrollable modal-lg">
                    <div class="modal-content">
                        <div class="modal-header bg-light">
                            <h5 class="modal-title" id="statusHistoryModalLabel">
                                <i class="fas fa-history me-2"></i>Appointment Status History
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body p-0">
                            <ul class="list-group list-group-flush" id="statusHistoryList">
                                <!-- History items will be populated here by JavaScript -->
                            </ul>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                <i class="fas fa-times me-1"></i>Close
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            <!-- Note Creation Modal -->
            <div class="modal fade" id="noteModal" tabindex="-1" aria-labelledby="noteModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="noteModalLabel">Create a New Note</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <input type="hidden" id="noteId" value="0">
                            <div class="mb-3">
                                <label for="noteReference" class="form-label">Reference</label>
                                <input type="text" class="form-control" id="noteReference"
                                    placeholder="Enter reference (optional)">
                            </div>
                            <div class="form-section">
                                <label for="noteField" class="form-label">Note</label>
                                <textarea class="form-control" id="noteField" rows="4"
                                    placeholder="Enter your detailed note here..."></textarea>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary" id="clearBtn">Clear</button>
                            <button type="button" class="btn btn-primary" id="addBtn">Add Note <i
                                    class="bi bi-plus-circle"></i></button>
                        </div>
                    </div>
                </div>
            </div>


            <!-- Equipment Modal -->
            <div class="modal fade" id="equipModal" tabindex="-1" aria-labelledby="equipModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="equipModalLabel">Add Equipment</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <form id="equipForm" novalidate>
                                <input type="number" id="equipId" value="0" hidden="hidden">
                                <div class="row mb-4">
                                    <div class="col-6">
                                        <label for="SerialNumber" class="form-label">Serial Number</label>
                                        <input type="text" id="SerialNumber" class="form-control">
                                    </div>
                                    <div class="col-6">
                                        <label for="equipType" class="form-label">Type</label>
                                        <select id="equipType" class="form-select">
                                            <option value="">Select Type</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="row mb-4">
                                    <div class="col-4">
                                        <label for="Make" class="form-label">Make</label>
                                        <input type="text" id="Make" class="form-control">
                                    </div>
                                    <div class="col-4">
                                        <label for="Model" class="form-label">Model</label>
                                        <input type="text" id="Model" class="form-control">
                                    </div>
                                    <div class="col-4">
                                        <label for="Barcode" class="form-label">SKU</label>
                                        <input type="text" id="Barcode" class="form-control">
                                    </div>
                                </div>
                                <div class="row mb-4">
                                    <div class="col-6">
                                        <label for="WarrantyStart" class="form-label">Warranty Start</label>
                                        <input type="date" id="WarrantyStart" class="form-control">
                                    </div>
                                    <div class="col-6">
                                        <label for="WarrantyEnd" class="form-label">Warranty End</label>
                                        <input type="date" id="WarrantyEnd" class="form-control">
                                    </div>
                                </div>
                                <div class="row mb-4">
                                    <div class="col-6">
                                        <label for="LaborWarrantyStart" class="form-label">Labor Warranty Start</label>
                                        <input type="date" id="LaborWarrantyStart" class="form-control">
                                    </div>
                                    <div class="col-6">
                                        <label for="LaborWarrantyEnd" class="form-label">Labor Warranty End</label>
                                        <input type="date" id="LaborWarrantyEnd" class="form-control">
                                    </div>
                                </div>
                                <div class="row mb-4">
                                    <div class="col-6">
                                        <label for="equipInstallDate" class="form-label">Install Date</label>
                                        <input type="date" id="equipInstallDate" class="form-control">
                                    </div>
                                    <div class="col-6">
                                        <label for="instruction" class="form-label">Notes</label>
                                        <input type="text" id="instruction" class="form-control">
                                    </div>
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary"
                                data-bs-dismiss="modal">Close</button>
                            <button type="button" id="equipSave" class="btn btn-primary">Save</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Maintenance Agreement Modal -->
            <div class="modal fade" id="agreeModal" tabindex="-1" aria-labelledby="agreeModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="agreeModalLabel">Add Maintenance Agreement</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <form id="agreeForm">
                                <input type="hidden" id="agreeId" value="0">
                                <div class="mb-3" id="agreeFileUploadSection">
                                    <label for="agreeFile" class="form-label">Upload PDF <span
                                            class="text-danger">*</span></label>
                                    <input type="file" id="agreeFile" accept=".pdf" class="form-control" required>
                                    <div id="agreeFilePreview" class="custdet-pdf-preview mt-2" style="display: none;">
                                    </div>
                                </div>
                                <div class="row mb-3">
                                    <div class="col-6">
                                        <label for="agreeExpirationDate" class="form-label">Expiration Date</label>
                                        <input type="date" id="agreeExpirationDate" class="form-control">
                                    </div>
                                    <div class="col-6">
                                        <label for="agreeAlarmDate" class="form-label">Alarm Date</label>
                                        <input type="datetime-local" id="agreeAlarmDate" class="form-control">
                                    </div>
                                </div>
                                <div class="mb-3">
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" id="agreeAlarmSet">
                                        <label class="form-check-label" for="agreeAlarmSet">
                                            Set Alarm
                                        </label>
                                    </div>
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary"
                                data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="btnSaveAgreement" class="btn btn-primary">Upload Agreement <i
                                    class="bi bi-upload"></i></button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Toast Container -->
            <div class="toast-container position-fixed bottom-0 end-0 p-3">
                <div id="toast" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
                    <div class="toast-body"></div>
                </div>
            </div>

            <!-- Send Email Modal -->
            <div class="modal fade" id="sendEmailModal" tabindex="-1" aria-labelledby="sendEmailModalLabel"
                aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="sendEmailModalLabel">Send Email</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"
                                id="closeSendEmailIcon"></button>
                        </div>
                        <form id="sendEmailForm" class="cust-modal-form">
                            <div class="modal-body">
                                <input type="hidden" id="emailCustomerID" />
                                <div class="mb-3">
                                    <label for="emailTo" class="form-label">To</label>
                                    <input type="email" name="emailTo" id="emailTo" class="form-control" required />
                                </div>
                                <div class="mb-3">
                                    <label for="emailCC" class="form-label">CC</label>
                                    <input type="email" name="emailCC" id="emailCC" class="form-control" />
                                </div>
                                <div class="mb-3">
                                    <label for="emailBCC" class="form-label">BCC</label>
                                    <input type="email" name="emailBCC" id="emailBCC" class="form-control" />
                                </div>
                                <div class="mb-3">
                                    <label for="emailSubject" class="form-label">Subject</label>
                                    <input type="text" name="emailSubject" id="emailSubject" class="form-control"
                                        required />
                                </div>
                                <div class="mb-3">
                                    <textarea name="emailBody" id="emailBody" class="form-control" rows="5"
                                        required></textarea>
                                </div>
                                <div id="preAttachedFile" class="mb-3" style="display:none;">
                                    <label class="form-label">Attachment (Pre-selected)</label>
                                    <div class="d-flex align-items-center p-2 border rounded bg-light">
                                        <i class="fas fa-paperclip me-2"></i>
                                        <span id="preAttachedFileName" class="flex-grow-1"></span>
                                        <button type="button" class="btn-close ms-2" id="removePreAttached"
                                            style="font-size: 0.75rem;"></button>
                                    </div>
                                    <input type="hidden" id="attachedFileName" />
                                    <input type="hidden" id="attachedFileContent" />
                                    <input type="hidden" id="attachedFileType" />
                                </div>
                                <div class="mb-3" id="fileInputContainer">
                                    <label for="emailAttachment" class="form-label">Add Attachment</label>
                                    <input type="file" name="emailAttachment" id="emailAttachment"
                                        class="form-control" />
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                                    id="closeSendEmail">Cancel</button>
                                <button type="submit" class="btn btn-primary">Send</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>

        <!-- External Scripts -->
        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"
            integrity="sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL"
            crossorigin="anonymous"></script>
        <script src="https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.18.5/xlsx.full.min.js"></script>
        <script type="text/javascript" src="https://cdn.jsdelivr.net/momentjs/latest/moment.min.js"></script>
        <script type="text/javascript"
            src="https://cdn.jsdelivr.net/npm/daterangepicker/daterangepicker.min.js"></script>

        
        <!-- Site Appointment Details Modal (Refactored to Bootstrap) -->
        <div class="modal fade" id="siteAppointmentDetailsModal_PopUP" tabindex="-1"
            aria-labelledby="siteAppointmentDetailsModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable">
                <div class="modal-content">
                    <div
                        class="modal-header d-flex justify-content-between align-items-center bg-white shadow-sm rounded-top">
                        <div class="d-flex flex-wrap align-items-center gap-3 rounded w-100">
                            <ul class="nav nav-tabs-modal d-flex gap-3 mb-0" id="editAppointmentTabs" role="tablist">
                                <li class="nav-item" role="presentation">
                                    <button class="nav-link-modal active px-4 py-2" id="appointment-tab"
                                        data-bs-toggle="tab" data-bs-target="#appointment-details" type="button"
                                        role="tab" aria-controls="appointment-details" aria-selected="true"
                                        style="font-weight: 600; color: #4b5563; background-color: white; border-radius: 0.375rem; box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05); transition: all 0.2s;"
                                        onmouseover="this.style.backgroundColor='#dbeafe'; this.style.color='#1e40af';"
                                        onmouseout="if(!this.classList.contains('active')){this.style.backgroundColor='white'; this.style.color='#4b5563';}">Appointment
                                        Details <span id="editAppointmentIdDisplay"
                                            class="ms-2 badge bg-light text-dark font-monospace"
                                            style="font-size: 0.8rem; border: 1px solid #dee2e6;"></span></button>
                                </li>
                              
                            </ul>
                            <div class="d-flex flex-wrap gap-2">
                                
                            </div>
                            <div class="ms-auto">
                                <button type="button" class="btn-close" data-bs-dismiss="modal"
                                    aria-label="Close modal"></button>
                            </div>
                        </div>
                    </div>

                    <div class="modal-body">
                        <form id="editAppointmentForm">
                            <input type="hidden" id="editApptId" />
                            <input type="hidden" id="editCustomerId" />

                            <div class="tab-content" id="editAppointmentTabsContent">
                                <!-- Appointment Details Tab -->
                                <div class="tab-pane fade show active" id="appointment-details" role="tabpanel"
                                    aria-labelledby="appointment-tab">
                                    <div class="row g-4">
                                        <!-- Column 1: Customer / Site Info (Read Only) -->
                                        <div class="col-md-4">
                                            <h5 class="mb-3">Customer / Site Info</h5>
                                            <div class="row">
                                                <div class="col-md-6">
                                                    <label class="form-label">Customer Name</label>
                                                    <input type="text" id="custModal_CustomerName" class="form-control"
                                                        readonly>
                                                </div>
                                                <div class="col-md-6">
                                                    <label class="form-label">Email</label>
                                                    <input type="text" id="custModal_Email" class="form-control"
                                                        readonly>
                                                </div>
                                            </div>
                                            <div class="row mt-2">
                                                <div class="col-md-6">
                                                    <label class="form-label">Phone</label>
                                                    <input type="text" id="custModal_Phone" class="form-control"
                                                        readonly>
                                                </div>
                                                <div class="col-md-6">
                                                    <label class="form-label">Mobile</label>
                                                    <input type="text" id="custModal_Mobile" class="form-control"
                                                        readonly>
                                                </div>
                                            </div>
                                            <div class="row mt-2">
                                                <div class="col-12">
                                                    <label class="form-label">Service Location</label>
                                                    <input type="text" id="custModal_SiteName" class="form-control"
                                                        readonly>
                                                </div>
                                            </div>
                                            <div class="row mt-2">
                                                <div class="col-12">
                                                    <label class="form-label">Street Address</label>
                                                    <input type="text" id="custModal_Address" class="form-control"
                                                        readonly>
                                                </div>
                                            </div>
                                            <div class="row mt-2">
                                                <div class="col-md-6">
                                                    <label class="form-label">City</label>
                                                    <input type="text" id="custModal_City" class="form-control"
                                                        readonly>
                                                </div>
                                                <div class="col-md-6">
                                                    <label class="form-label">Country</label>
                                                    <input type="text" id="custModal_Country" class="form-control"
                                                        readonly>
                                                </div>
                                            </div>
                                            <div class="row mt-2">
                                                <div class="col-md-6">
                                                    <label class="form-label">State/Province</label>
                                                    <input type="text" id="custModal_State" class="form-control"
                                                        readonly>
                                                </div>
                                                <div class="col-md-6">
                                                    <label class="form-label">Zip Code</label>
                                                    <input type="text" id="custModal_Zip" class="form-control" readonly>
                                                </div>
                                            </div>
                                        </div>

                                        <!-- Column 2: Appointment Info (Editable) -->
                                        <div class="col-md-4">
                                            <h5 class="mb-3">Appointment Info</h5>
                                            <div class="row">
                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Service Type</label>
                                                    <select id="MainContent_ServiceTypeFilter_Edit" class="form-select"
                                                        onchange="calculateTimeRequired(event)" required>
                                                        <option value="">Select a Service</option>
                                                        <!-- Options populated by JS -->
                                                    </select>
                                                </div>
                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Resource</label>
                                                    <select id="resource_list" class="form-select">
                                                        <option value="0">Unassigned</option>
                                                    </select>
                                                </div>

                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Date</label>
                                                    <input type="date" class="form-control" id="dateInput" required
                                                        onchange="updateDate(event)">
                                                </div>
                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Time Required</label>
                                                    <input type="text" id="duration" class="form-control"
                                                        placeholder="e.g., 1 Hr : 30 Min">
                                                </div>

                                                <div class="mb-3 col-12">
                                                    <label class="form-label">Time Slot</label>
                                                    <select id="time_slot" class="form-select" required
                                                        onchange="calculateTimeRequired(event)">
                                                        <!-- Options populated by JS -->
                                                    </select>
                                                </div>

                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Appointment Start Date</label>
                                                    <input type="text" class="form-control" id="txt_StartDate"
                                                        placeholder="MM/DD/YYYY hh:mm AM/PM">
                                                </div>

                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Appointment End Date</label>
                                                    <input type="text" class="form-control" id="txt_EndDate"
                                                        placeholder="MM/DD/YYYY hh:mm AM/PM">
                                                    <small id="customer_EndDate" style="display: none;"
                                                        class="text-warning">End date time can’t be smaller than start
                                                        date time.</small>
                                                </div>

                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Appointment Status</label>
                                                    <select id="MainContent_StatusTypeFilter_Edit" class="form-select"
                                                        required>
                                                        <option value="">Select a status..</option>
                                                    </select>
                                                </div>

                                                <div class="mb-1 col-6">
                                                    <label class="form-label">Ticket Status</label>
                                                    <select id="MainContent_TicketStatusFilter_Edit"
                                                        class="form-select">
                                                        <option value="">Select a ticket status..</option>
                                                    </select>
                                                </div>
                                            </div>
                                        </div>

                                        <!-- Column 3: Custom Fields & Notes -->
                                        <div class="col-md-4">
                                            <h5 class="mb-3">Custom Fields & Notes</h5>
                                            <div id="customFieldsContainer" class="mb-3">
                                                <!-- Populated via JS -->
                                            </div>
                                            <div class="mb-3">
                                                <label class="form-label">Any details</label>
                                                <textarea id="editApptNote" name="note" class="form-control"
                                                    rows="6"></textarea>
                                            </div>
                                            <!-- Appointment-Specific Links -->
                                            <div class="mb-3">
                                                <h6 class="mb-2">Appointment-Specific Items</h6>
                                                <div id="appointmentSpecificLinks" class="border rounded p-2"
                                                    style="min-height: 60px;">
                                                    <small class="text-muted">No items attached to this
                                                        appointment</small>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Forms Tab -->
                                <div class="tab-pane fade" id="forms-section" role="tabpanel"
                                    aria-labelledby="forms-tab">
                                    <div class="d-flex justify-content-between align-items-center mb-3">
                                        <h6>Attached Forms</h6>
                                        <div>
                                            <button type="button" class="btn btn-sm btn-outline-primary"
                                                onclick="openFormsSelectionModal('edit')">
                                                <i class="fa fa-plus"></i>Add Forms
                                            </button>
                                            <button type="button" class="btn btn-sm btn-outline-info"
                                                onclick="openAppointmentFormsModal()">
                                                <i class="fa fa-list"></i>View Forms
                                            </button>
                                        </div>
                                    </div>
                                    <div id="selectedFormsEdit" class="selected-forms-container"
                                        style="min-height: 60px; border: 1px solid #dee2e6; border-radius: 0.375rem; padding: 8px;">
                                        <small class="text-muted">No forms attached to this appointment</small>
                                    </div>
                                </div>

                                <!-- CSL Basic Info Tab -->
                                <div class="tab-pane fade" id="csl-basic-section" role="tabpanel"
                                    aria-labelledby="csl-basic-tab">
                                    <div id="cslBasicInfoContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Appointments Tab -->
                                <div class="tab-pane fade" id="csl-appointments-section" role="tabpanel"
                                    aria-labelledby="csl-appointments-tab">
                                    <div id="cslAppointmentsContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Invoices/Estimates Tab -->
                                <div class="tab-pane fade" id="csl-invoices-section" role="tabpanel"
                                    aria-labelledby="csl-invoices-tab">
                                    <div id="cslInvoicesContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Notes Tab -->
                                <div class="tab-pane fade" id="csl-notes-section" role="tabpanel"
                                    aria-labelledby="csl-notes-tab">
                                    <div id="cslNotesContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Equipment Tab -->
                                <div class="tab-pane fade" id="csl-equipment-section" role="tabpanel"
                                    aria-labelledby="csl-equipment-tab">
                                    <div id="cslEquipmentContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Pictures Tab -->
                                <div class="tab-pane fade" id="csl-pictures-section" role="tabpanel"
                                    aria-labelledby="csl-pictures-tab">
                                    <div id="cslPicturesContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Files Tab -->
                                <div class="tab-pane fade" id="csl-files-section" role="tabpanel"
                                    aria-labelledby="csl-files-tab">
                                    <div id="cslFilesContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- CSL Maintenance Agreements Tab -->
                                <div class="tab-pane fade" id="csl-agreements-section" role="tabpanel"
                                    aria-labelledby="csl-agreements-tab">
                                    <div id="cslAgreementsContent" class="p-3">
                                        <div class="text-center p-5">
                                            <div class="spinner-border" role="status">
                                                <span class="visually-hidden">Loading...</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>

                    <div class="modal-footer">
                        <button type="button" class="btn btn-danger d-none" onclick="deleteAppointment()">
                            Delete</button>
                        <button type="button" class="btn btn-secondary d-none" onclick="unscheduleAppointment()">
                            Unschedule</button>
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="saveAppointmentChanges()">Update</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Forms Selection Modal -->
        <div class="modal fade" id="formsSelectionModal" tabindex="-1" role="dialog">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Select Forms for Appointment</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <h6>Available Forms</h6>
                                <div class="form-check">
                                    <input type="checkbox" class="form-check-input" id="autoAssignForms" checked>
                                    <label class="form-check-label" for="autoAssignForms">
                                        Auto-assign forms based on service type
                                    </label>
                                </div>
                                <hr>
                                <div id="availableFormsList" class="available-forms-list">
                                    <!-- Available forms will be loaded here -->
                                </div>
                            </div>
                            <div class="col-md-6">
                                <h6>Selected Forms</h6>
                                <div id="selectedFormsList" class="selected-forms-list">
                                    <!-- Selected forms will appear here -->
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="applyFormsSelection()">Apply
                            Selection</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Appointment Forms Management Modal -->
        <div class="modal fade" id="appointmentFormsModal" tabindex="-1" role="dialog">
            <div class="modal-dialog modal-xl" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Appointment Forms</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-4">
                                <h6>Forms List</h6>
                                <div id="appointmentFormsList" class="appointment-forms-list">
                                    <!-- Appointment forms will be loaded here -->
                                </div>

                            </div>
                            <div class="col-md-8">
                                <div class="d-flex justify-content-between align-items-center mb-3">
                                    <h3 id="formName" class="mb-0"></h3>

                                    <!-- Form Actions -->
                                    <div class="form-actions mt-2" id="formActionsContainer" style="display: none;">
                                        <div class="btn-group" role="group">
                                            <button type="button" class="btn btn-sm btn-primary"
                                                onclick="openCustomerResponseModal()"
                                                title="Save forms to this appointment">
                                                <i class="fa fa-eye"></i>Respone
                                            </button>
                                            <button type="button" class="btn btn-sm btn-info"
                                                onclick="sendFormsViaEmail()" title="Send forms to customer email">
                                                <i class="fa fa-envelope"></i>Email
                                            </button>
                                            <button type="button" class="btn btn-sm btn-warning"
                                                onclick="sendFormsViaSMS()" title="Send forms to customer phone">
                                                <i class="fa fa-mobile"></i>SMS
                                            </button>
                                        </div>
                                    </div>
                                </div>

                                <div id="loader" style="display:none;">
                                    <div class="spinner-border text-primary" role="status"><span
                                            class="visually-hidden">Loading...</span></div>
                                </div>

                                <div id="formViewerContainer">
                                    <div class="form-viewer-placeholder text-center p-5">
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                            onclick="openAppointmentModal()">Close</button>
                    </div>
                </div>
            </div>
        </div>


        <!-- File Upload Modal -->
        <div class="modal fade" id="fileUploadModal" tabindex="-1" aria-labelledby="fileUploadModalLabel"
            aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="fileUploadModalLabel">Upload Files</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label for="fileUploadInputModal" class="form-label">Select Files</label>
                            <input type="file" id="fileUploadInputModal" class="form-control" multiple>
                        </div>
                        <div class="mb-3">
                            <label for="fileUploadReference" class="form-label">Reference</label>
                            <input type="text" id="fileUploadReference" class="form-control"
                                placeholder="Enter reference (optional)">
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" id="saveFileBtn" class="btn btn-primary">Save Files</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Picture Upload Modal -->
        <div class="modal fade" id="pictureUploadModal" tabindex="-1" aria-labelledby="pictureUploadModalLabel"
            aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="pictureUploadModalLabel">Upload Pictures</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label for="pictureUploadInputModal" class="form-label">Select Pictures</label>
                            <input type="file" id="pictureUploadInputModal" class="form-control" accept="image/*"
                                multiple>
                        </div>
                        <div class="mb-3">
                            <label for="pictureUploadReference" class="form-label">Reference</label>
                            <input type="text" id="pictureUploadReference" class="form-control"
                                placeholder="Enter reference (optional)">
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" id="savePictureBtn" class="btn btn-primary">Save Pictures</button>
                    </div>
                </div>
            </div>
        </div>
        <!-- SMS Modal -->
        <div class="modal fade" id="modalSendSMS" tabindex="-1" role="dialog" aria-labelledby="modalSendSMS"
            aria-hidden="true">
            <div class="modal-dialog modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="smsModalLongTitle">Send SMS</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row mt-3">
                            <div class="col-12 mt-0">
                                <label for="txtMobile" class="form-label mb-0">Phone/Mobile Number</label>
                                <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" />
                                <asp:HiddenField ID="txtCustomerId" runat="server" />
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtSMS" class="form-label mb-0">Text</label>
                                <asp:TextBox ID="txtSMS" runat="server" TextMode="MultiLine" Rows="5"
                                    CssClass="form-control" />
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-info" onclick="OpenSMSHistoryFromModal()"
                            title="View SMS History">
                            <i class="fas fa-history me-1"></i>View History
                        </button>
                        <div>
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                                onclick="CloseSMSPopup()">Close</button>
                            <asp:Button ID="btnSendSms" ClientIDMode="Static" runat="server" Text="Send SMS"
                                CssClass="btn btn-success" OnClick="btnSendSMS_Click" />
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- MMS Modal -->
        <div class="modal fade" id="modalSendMMS" tabindex="-1" role="dialog" aria-labelledby="modalSendMMS"
            aria-hidden="true">
            <div class="modal-dialog modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="mmsModalLongTitle">Send MMS</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row mt-3">
                            <div class="col-12 mt-0">
                                <label for="txtCustMob" class="form-label mb-0">Phone/Mobile Number</label>
                                <asp:TextBox ID="txtCustMob" runat="server" CssClass="form-control" />
                                <asp:HiddenField ID="txtCustId" runat="server" />
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtMMSBody" class="form-label mb-0">MMS Body</label>
                                <asp:TextBox ID="txtMMSBody" runat="server" TextMode="MultiLine" Rows="4"
                                    CssClass="form-control" />
                            </div>
                        </div>
                        <div class="row mt-3">
                            <div class="col-12 mt-0">
                                <label for="fuAttachment" class="form-label mb-0">Attachment</label>
                                <asp:FileUpload ID="fuAttachment" runat="server" CssClass="form-control" />
                                <p id="fileHint" class="text-muted small mb-0">Allowed file types: .pdf, .jpg, .jpeg,
                                    .png (Max size: 20MB)</p>
                                <span id="fileError" class="text-danger small"></span>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-info" onclick="OpenSMSHistoryFromModal()"
                            title="View SMS History">
                            <i class="fas fa-history me-1"></i>View History
                        </button>
                        <div>
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                                onclick="CloseMMSPopup()">Close</button>
                            <asp:Button ID="btnSendMMS" ClientIDMode="Static" runat="server" Text="Send MMS"
                                CssClass="btn btn-success" OnClick="btnSendMMS_Click" />
                        </div>
                    </div>
                </div>
            </div>
        </div>

 
        <script>
            // SMS Modal Functions
            function OpenSMSPopUp(mobile, customerID) {
                //if (!mobile) {
                //    Swal.fire('Validation Error', 'Please add mobile or phone number for this customer.', 'warning');
                //    return;
                //}
                $('#<%= txtMobile.ClientID %>').val(mobile);
                $('#<%= txtCustomerId.ClientID %>').val(customerID);
                $('#modalSendSMS').modal('show');
            }

            function CloseSMSPopup() {
                $('#<%= txtMobile.ClientID %>').val('');
                $('#<%= txtCustomerId.ClientID %>').val('');
                $('#<%= txtSMS.ClientID %>').val('');
                $('#modalSendSMS').modal('hide');
            }

            // MMS Modal Functions
            function OpenMMSPopUp(mobile, customerID) {
                //if (!mobile) {
                //    Swal.fire('Validation Error', 'Please add mobile or phone number for this customer.', 'warning');
                //    return;
                //}
                $('#<%= txtCustMob.ClientID %>').val(mobile);
                $('#<%= txtCustId.ClientID %>').val(customerID);
                $('#modalSendMMS').modal('show');
            }

            function CloseMMSPopup() {
                $('#<%= txtCustMob.ClientID %>').val('');
                $('#<%= txtCustId.ClientID %>').val('');
                $('#<%= txtMMSBody.ClientID %>').val('');
                $('#<%= fuAttachment.ClientID %>').val('');
                $("#fileError").text('');
                $("#fileError").removeClass('text-success').addClass('text-danger');
                $('#modalSendMMS').modal('hide');
            }

            // Open SMS History from Modal
            function OpenSMSHistoryFromModal() {
                var mobile = $('#<%= txtMobile.ClientID %>').val().trim() || $('#<%= txtCustMob.ClientID %>').val().trim();
                var customerId = $('#<%= txtCustomerId.ClientID %>').val().trim() || $('#<%= txtCustId.ClientID %>').val().trim();
                var customerName = $('#<%= lblCustomerName.ClientID %>').text().trim();

                if (!mobile) {
                    Swal.fire('Validation Error', 'Mobile number is required to view history.', 'warning');
                    return;
                }

                // Close modal first
                $('#modalSendSMS').modal('hide');
                $('#modalSendMMS').modal('hide');

                // Redirect to SMS history page (CustomerChatHistory.aspx expects: mobile, name, customerId)
                var url = "CustomerChatHistory.aspx?mobile=" + encodeURIComponent(mobile)
                    + "&name=" + encodeURIComponent(customerName)
                    + "&customerId=" + encodeURIComponent(customerId);

                window.location.href = url;
            }

            // Wire up SMS button - unbind any existing handlers first
            $(document).ready(function () {
                // Remove any existing handlers from customerdetails.js
                $('#btnSms').off('click');
                $('#btnMms').off('click');

                // Bind new handler for SMS button
                $('#btnSms').on('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();

                    // Try to get mobile number first
                    var mobileLink = $('#<%= hlMobile.ClientID %>');
                    var mobile = mobileLink.text().trim();
                    if (!mobile && mobileLink.attr('href')) {
                        mobile = mobileLink.attr('href').replace('tel:', '').trim();
                    }

                    // If no mobile, try phone number
                 <%--   if (!mobile) {
                        var phoneLink = $('#<%= hlPhone.ClientID %>');
                        mobile = phoneLink.text().trim();
                        if (!mobile && phoneLink.attr('href')) {
                            mobile = phoneLink.attr('href').replace('tel:', '').trim();
                        }
                    }--%>

                    var customerId = $('#<%= lblCustomerId.ClientID %>').text().trim();

                    //if (!mobile) {
                    //    Swal.fire('Validation Error', 'Please add mobile or phone number for this customer.', 'warning');
                    //    return false;
                    //}

                    OpenSMSPopUp(mobile, customerId);
                    return false;
                });

                // Bind new handler for MMS button
                $('#btnMms').on('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();

                    // Try to get mobile number first
                    var mobileLink = $('#<%= hlMobile.ClientID %>');
                    var mobile = mobileLink.text().trim();
                    if (!mobile && mobileLink.attr('href')) {
                        mobile = mobileLink.attr('href').replace('tel:', '').trim();
                    }

                    // If no mobile, try phone number
                 <%--   if (!mobile) {
                        var phoneLink = $('#<%= hlPhone.ClientID %>');
                        mobile = phoneLink.text().trim();
                        if (!mobile && phoneLink.attr('href')) {
                            mobile = phoneLink.attr('href').replace('tel:', '').trim();
                        }
                    }--%>

                    var customerId = $('#<%= lblCustomerId.ClientID %>').text().trim();

                    //if (!mobile) {
                    //    Swal.fire('Validation Error', 'Please add mobile or phone number for this customer.', 'warning');
                    //    return false;
                    //}

                    OpenMMSPopUp(mobile, customerId);
                    return false;
                });

                // File validation for MMS
                var fileInput = document.getElementById('<%= fuAttachment.ClientID %>');
                var errorLabel = document.getElementById("fileError");

                if (fileInput) {
                    fileInput.addEventListener("change", function () {
                        errorLabel.innerText = "";

                        if (fileInput.files.length > 0) {
                            var file = fileInput.files[0];
                            var fileName = file.name.toLowerCase();
                            var fileSize = file.size; // Size in bytes
                            var maxSize = 20 * 1024 * 1024; // 20MB in bytes
                            var allowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

                            // Check file type
                            var isValid = allowedExtensions.some(function (ext) {
                                return fileName.endsWith(ext);
                            });

                            if (!isValid) {
                                errorLabel.innerText = "❌ Invalid file type! Only PDF, JPG, JPEG, PNG are allowed.";
                                fileInput.value = "";
                                return;
                            }

                            // Check file size
                            if (fileSize > maxSize) {
                                errorLabel.innerText = "❌ File size exceeds 20MB limit. Please choose a smaller file.";
                                fileInput.value = "";
                                return;
                            }

                            // Show success message
                            var fileSizeMB = (fileSize / (1024 * 1024)).toFixed(2);
                            errorLabel.innerText = "✓ File selected: " + fileSizeMB + " MB";
                            errorLabel.className = "text-success small";
                        }
                    });
                }

                // Validation for SMS send button
                $('#<%= btnSendSms.ClientID %>').on('click', function (e) {
                    var mobile = $('#<%= txtMobile.ClientID %>').val().trim();
                    var sms = $('#<%= txtSMS.ClientID %>').val().trim();

                    if (!mobile || !sms) {
                        Swal.fire('Validation Error', 'Phone/Mobile number and SMS text cannot be empty.', 'warning');
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    }
                    // If validation passes, allow the postback to proceed
                    return true;
                });

                // Validation for MMS send button
                $('#<%= btnSendMMS.ClientID %>').on('click', function (e) {
                    var mobile = $('#<%= txtCustMob.ClientID %>').val().trim();
                    var mmsBody = $('#<%= txtMMSBody.ClientID %>').val().trim();
                    var fileInput = document.getElementById('<%= fuAttachment.ClientID %>');

                    if (!mobile || !mmsBody) {
                        Swal.fire('Validation Error', 'Phone/Mobile number and MMS body cannot be empty.', 'warning');
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    }

                    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
                        Swal.fire('Validation Error', 'Please select a file to attach.', 'warning');
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    }
                    // If validation passes, allow the postback to proceed
                    return true;
                });
            });
        </script>
        <script>
            // Deep-link tab / action support from Supported TP Providers actions menu
            (function () {
                var params = new URLSearchParams(window.location.search);
                var tab = params.get('tab');
                var openAction = params.get('openAction');
                var mobileParam = params.get('mobile');
                if (!tab && !openAction) return;

                document.addEventListener('DOMContentLoaded', function () {
                    if (tab) {
                        var tabBtn = document.getElementById(tab + '-tab');
                        if (tabBtn && window.bootstrap && bootstrap.Tab) {
                            bootstrap.Tab.getOrCreateInstance(tabBtn).show();
                        }
                    }
                    if (openAction === 'sendEmail') {
                        var emailBtn = document.getElementById('btnSendEmail');
                        if (emailBtn) emailBtn.click();
                    } else if (openAction === 'sendSms') {
                        var mobile = mobileParam || '';
                        if (!mobile) {
                            var mobileLink = document.getElementById('<%= hlMobile.ClientID %>');
                            mobile = mobileLink ? (mobileLink.textContent || mobileLink.innerText || '').trim() : '';
                            if (!mobile && mobileLink && mobileLink.getAttribute('href')) {
                                mobile = mobileLink.getAttribute('href').replace('tel:', '').trim();
                            }
                        }
                        if (!mobile) {
                            var phoneLink = document.getElementById('<%= hlPhone.ClientID %>');
                            mobile = phoneLink ? (phoneLink.textContent || phoneLink.innerText || '').trim() : '';
                            if (!mobile && phoneLink && phoneLink.getAttribute('href')) {
                                mobile = phoneLink.getAttribute('href').replace('tel:', '').trim();
                            }
                        }
                        var custIdEl = document.getElementById('<%= lblCustomerId.ClientID %>');
                        var custId = custIdEl ? (custIdEl.textContent || custIdEl.innerText || '').trim() : '';
                        if (typeof OpenSMSPopUp === 'function') {
                            OpenSMSPopUp(mobile, custId);
                        }
                    }
                });
            })();
        </script>
               <script src="Scripts/customerdetails.js?v=10"></script>
    </asp:Content>