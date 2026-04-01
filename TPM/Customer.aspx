<%@ Page Title="Customers" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="Customer.aspx.cs" Inherits="FSM.Customer" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <link rel="stylesheet" href="https://cdn.datatables.net/2.2.2/css/dataTables.dataTables.min.css">
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/select/3.0.0/css/select.dataTables.min.css">

    <!-- Local Styles and Scripts -->
    <link rel="stylesheet" href="Content/customer.css?v=1">

    <style>
        .loading-overlay {
                position: relative;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background-color: rgba(255, 255, 255, 0.8); /* Semi-transparent white background */
                display: flex; /* Use flexbox to center content */
                justify-content: center; /* Center horizontally */
                align-items: center; /* Center vertically */
                z-index: 10; /* Ensure it is on top of other content */
            }

        .cust-action-btns {
            display: flex;
            gap: 10px;
            / align-items: center;
        }

        .cust-action-btn {
            background: none;
            border: none;
            cursor: pointer;
            color: #444;
            font-size: 22px;
            padding: 6px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
        }

            .cust-action-btn:hover {
                background: #f0f0f0;
                color: #007bff;
            }

        .sms-btn {
            color: green;
            font-size: 22px;
        }

        .edit-btn svg {
            width: 22px;
            height: 22px;
        }
    </style>


    <div class="cust-page-container">
        <!-- Page Header -->
        <header class="cust-header mt-0 mb-0">
          <div class="cec-btn">
             <a id="LaunchCecButton" runat="server" href="#" class="custom-launch-btn" role="button" target="_blank">
                    <span>
                        <span>CEC Customer</span>
                        <span aria-hidden="true">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 19.5l15-15m0 0H8.25m11.25 0v11.25" />
                            </svg>
                        </span>
                    </span>
                </a>
            </div>
        </header>


        <!-- Customer Section -->
        <section class="cust-section">
            <!-- Customer List -->
            <div class="cust-list-container">
                <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                    <div class="pt-2 ps-2 d-flex align-items-center gap-3 d-none ">
                        <div>
                            <label for="statusFilter" class="form-label">Filter by Status:</label>
                            <asp:DropDownList ID="statusFilter" runat="server" CssClass="form-select w-auto">
                                <asp:ListItem Text="All Statuses" Value=""></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="toggle-switch">
                        <label for="hideNA" class="form-label" style="font-size: 0.8rem;">Hide Customer with No Appointments:</label>
                        <input type="checkbox" id="hideNA"  />
                    </div>
                </div>

                <table id="customerTable" class="display" style="width: 100%">
                    <thead>
                        <tr>
                         <th>Tp Name</th>
                            <th>Email</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                </table>
            </div>
            <!-- Customer Details -->
            <div class="cust-details-container">
                <div class="cust-details-header">
                    <div class="cust-details-left">

                        <h2 class="cust-details-title" id="customerName">Select a Customer</h2>
                    </div>
                    <div class="cust-details-actions">

                        <button class="cust-table-edit-btn" title="Edit Customer" id="editCustomerBtn">
                            <i class="fa-solid fa-user-pen"></i>
                        </button>
                    </div>
                </div>

                <!-- This is the moved section -->
                <div id="contact">
                    <!-- Row 1: Address -->
                    <div class="ci-row">
                        <div class="ci-item" id="customerAddress-container">
                            <i class="ci-icon fas fa-map-marker-alt"></i>
                            <span class="ci-value" id="customerAddress">-</span>
                        </div>
                    </div>
                    <!-- Row 2: Phone and Mobile -->
                    <div class="ci-row">
                        <!-- Phone -->
                        <div class="ci-item" id="customerPhone-container">
                            <i class="ci-icon fas fa-phone-alt"></i>
                            <span class="ci-value" id="customerPhone">-</span>
                        </div>
                        <!-- Mobile -->
                        <div class="ci-item" id="customerMobile-container">
                            <i class="ci-icon fas fa-mobile-alt"></i>
                            <span class="ci-value" id="customerMobile">-</span>
                        </div>
                    </div>
                    <!-- Row 3: Email & Job Title -->
                    <div class="ci-row">
                        <div class="ci-item" id="customerEmail-container">
                            <i class="ci-icon fas fa-envelope"></i>
                            <span class="ci-value" id="customerEmail">-</span>
                        </div>
                        <div class="ci-item" id="customerJobTitle-container">
                            <i class="ci-icon fas fa-briefcase"></i>
                            <span class="ci-value" id="customerJobTitle">-</span>
                        </div>
                    </div>
                </div>
                <!-- End of moved section -->

                <div class="cust-details-content">
                    <!-- Note: The original cust-section-block wrappers have been removed as requested -->
                    <div class="cust-section-block">
                        <button class="cust-section-toggle" data-section="sites" id="sitesBtn">Sites & Locations</button>
                        <div class="cust-section-content" id="sites">
                            <div class="sites-header">
                            </div>
                            <div class="sites-filter-bar" id="sitesFilterBar">
                                <div class="sites-filter-group">
                                    <label for="siteApptDateRangeSelect">Appointment Date:</label>
                                    <select id="siteApptDateRangeSelect" class="sites-filter-input">
                                        <option value="">All Dates</option>
                                        <option value="today">Today</option>
                                        <option value="this_week">This Week</option>
                                        <option value="this_month">This Month</option>
                                        <option value="this_year">This Year</option>
                                        <option value="custom">Custom</option>
                                    </select>
                                </div>
                                <div class="sites-filter-group" id="customDateRange" style="display:none;">
                                    <label>From:</label>
                                    <input type="date" id="siteApptDateFrom" class="sites-filter-input" />
                                    <label>To:</label>
                                    <input type="date" id="siteApptDateTo" class="sites-filter-input" />
                                </div>
                                <div class="sites-filter-group">
                                    <label for="siteApptStatusFilter">Status:</label>
                                    <select id="siteApptStatusFilter" class="sites-filter-input">
                                        <option value="">All Statuses</option>
                                    </select>
                                </div>
                                <button type="button" id="siteFilterSearchBtn" class="sites-filter-btn"><i class="fas fa-search" style="margin-right:5px;"></i>Search</button>
                                <button type="button" id="siteFilterClearBtn" class="sites-filter-btn sites-filter-clear-btn"><i class="fas fa-times" style="margin-right:5px;"></i>Clear</button>
                            </div>
                             <div class="loading-overlay" id="loading-spinner">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Loading...</span>
                                </div>
                            </div>
                             <table id="customerSiteTable" class="display" style="width: 100%">
                                    <thead>
                                        <tr>
                                             <th>FIRSTNAME</th>
                                          
                                        </tr>
                                    </thead>
                                </table>
                            <div class="sites-list">
                                
                            </div>
                            
                        </div>
                    </div>

                </div>
            </div>
        </section>
    </div>
    
    <div class="cust-modal" id="ApptListModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeAPPListIcon">×</button>
            <h2 class="cust-modal-title">Add New Customer</h2>
            <form id="adddsfgsdCustomerForm" class="cust-modal-form">
                <div class="cust-modal-field">
                    <label class="cust-modal-label">First Name</label>
                    <input type="text" name="firstName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Last Name</label>
                    <input type="text" name="lastName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Email</label>
                    <input type="email" name="email" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Phone</label>
                    <input type="text" name="phone" class="cust-modal-input" />
                </div>
                <div class="cust-modal-btns">
                    <button type="button" class="cust-modal-cancel" id="closeApptlListFoother">Close</button>
                </div>
            </form>
        </div>
    </div>

    <div class="cust-modal" id="addCustomerModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeAddCustomerIcon">×</button>
            <h2 class="cust-modal-title">Add New Customer</h2>
            <form id="addCustomerForm" class="cust-modal-form">
                <div class="cust-modal-field">
                    <label class="cust-modal-label">First Name</label>
                    <input type="text" name="firstName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Last Name</label>
                    <input type="text" name="lastName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Email</label>
                    <input type="email" name="email" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Phone</label>
                    <input type="text" name="phone" class="cust-modal-input" />
                </div>
                <div class="cust-modal-btns">
                    <button type="button" class="cust-modal-cancel" id="closeAddCustomer">Cancel</button>
                    <button type="submit" class="cust-modal-submit">Add Customer</button>
                </div>
            </form>
        </div>
    </div>

    <!-- Edit Customer Modal -->
    <div class="cust-modal" id="editCustomerModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeEditCustomerIcon">×</button>
            <h2 class="cust-modal-title">Edit Customer</h2>
            <form id="editCustomerForm" class="cust-modal-form">
                <div class="cust-modal-field">
                    <label class="cust-modal-label">First Name</label>
                    <input type="text" name="firstName" id="editFirstName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Last Name</label>
                    <input type="text" name="lastName" id="editLastName" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Email</label>
                    <input type="email" name="email" id="editEmail" class="cust-modal-input" required />
                </div>
                <div class="cust-modal-field">
                    <label class="cust-modal-label">Phone</label>
                    <input type="text" name="phone" id="editPhone" class="cust-modal-input" />
                </div>
                <div class="cust-modal-btns">
                    <button type="button" class="cust-modal-cancel" id="closeEditCustomer">Cancel</button>
                    <button type="submit" class="cust-modal-submit">Save Changes</button>
                </div>
            </form>
        </div>
    </div>
     <!-- Site Appointment Details Modal (Refactored to Bootstrap) -->
    <div class="modal fade" id="siteAppointmentDetailsModal" tabindex="-1"
        aria-labelledby="siteAppointmentDetailsModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-xl modal-dialog-scrollable" style="max-width: 98%; width: 98%;">
            <div class="modal-content">
                <div class="modal-header flex justify-between items-center bg-white shadow-md rounded-t-lg">
                    <div class="d-flex flex-wrap align-items-center gap-3 pt-3 px-3 bg-gray-50 rounded-lg w-100">
                        <ul class="nav nav-tabs-modal flex gap-3 mb-0" id="editAppointmentTabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <h4> Appointment Details</h4>
                                <button
                                    class="nav-link-modal active d-none px-4 py-2 text-sm font-semibold text-gray-700 bg-white rounded-md shadow-sm transition-all duration-200 hover:bg-blue-100 hover:text-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                    id="appointment-tab" data-bs-toggle="tab" data-bs-target="#appointment-details"
                                    type="button" role="tab" aria-controls="appointment-details"
                                    aria-selected="true">
                                    </button>
                            </li>
                            <li class="nav-item d-none" role="presentation">
                                <button
                                    class="nav-link-modal px-4 py-2 text-sm font-semibold text-gray-700 bg-white rounded-md shadow-sm transition-all duration-200 hover:bg-blue-100 hover:text-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                    id="forms-tab" data-bs-toggle="tab" data-bs-target="#forms-section"
                                    type="button" role="tab" aria-controls="forms-section"
                                    aria-selected="false">
                                    Forms</button>
                            </li>
                        </ul>
                        <div class="ms-auto">
                            <button type="button"
                                class="btn-close text-gray-500 opacity-80 hover:opacity-100 transition-opacity duration-200"
                                data-bs-dismiss="modal" aria-label="Close modal">
                            </button>
                        </div>
                    </div>
                </div>

                <div class="modal-body">
                    <form id="editAppointmentForm">
                        <input type="hidden" id="editApptId" />
                        <input type="hidden" id="editCustomerId" />
                        <!-- Hidden fields from appointments.aspx that might be needed or placeholders -->
                        <input type="hidden" id="AppoinmentId" />
                        <!-- Mapping to editApptId in JS? No, customer.js uses editApptId -->

                        <div class="tab-content" id="editAppointmentTabsContent">
                            <!-- Appointment Details Tab -->
                            <div class="tab-pane fade show active" id="appointment-details" role="tabpanel"
                                aria-labelledby="appointment-tab">
                                <div class="row g-4">
                                    <!-- Column 1: Customer / Site Info (Read Only) -->
                                    <div class="col-md-4">
                                        <h5 class="mb-3">Customer / Site Info</h5>
                                        <div class="row">
                                            <div class="col-md-12">
                                                <label class="form-label">Customer Name</label>
                                                <input type="text" id="custModal_CustomerName" class="form-control"
                                                    readonly>
                                            </div>
                                            <div class="col-md-6 d-none">
                                                <label class="form-label">Email</label>
                                                <input type="text" id="custModal_Email" class="form-control"
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
                                        <div class="col-md-6 mb-1">
                                            <label class="form-label">City</label>
                                            <input type="text" id="custModal_City" class="form-control" readonly>
                                        </div>
                                        <div class="col-md-6 mb-1">
                                            <label class="form-label">State/Province</label>
                                            <input type="text" id="custModal_State" class="form-control" readonly>
                                        </div>
                                        <div class="col-md-6 mb-1">
                                            <label class="form-label">Country</label>
                                            <input type="text" id="custModal_Country" class="form-control" readonly>
                                        </div>

                                        <div class="col-md-6 mb-1">
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
                        </div>
                    </form>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary d-none" onclick="saveAppointmentChanges()">Update</button>
                </div>
            </div>
        </div>
    </div>

    <div class="cust-modal" id="addSiteModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeAddSiteIcon">×</button>
            <h2 class="cust-modal-title">Add New Site</h2>

            <!-- Hidden fields for IDs -->
            <input type="number" id="SiteId" value="0" hidden />
            <input type="text" id="CustomerID" hidden />
            <input type="text" id="CustomerGuid" hidden />

            <form id="addSiteForm" class="cust-modal-form">

                <!-- Site Name -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="siteName" class="cust-modal-label">Site Name</label>
                        <input type="text" id="siteName" name="siteName" class="cust-modal-input" required />
                    </div>
                </div>

                <!-- Contact Person -->
                <div class="form-row">
                    <div class="cust-modal-field half-width">
                        <label for="firstName" class="cust-modal-label">First Name</label>
                        <input type="text" id="firstName" name="firstName" class="cust-modal-input" />
                    </div>
                    <div class="cust-modal-field half-width">
                        <label for="lastName" class="cust-modal-label">Last Name</label>
                        <input type="text" id="lastName" name="lastName" class="cust-modal-input" />
                    </div>
                </div>

                <!-- Phone + Email -->
                <div class="form-row">
                    <div class="cust-modal-field half-width">
                        <label for="phoneNumber" class="cust-modal-label">Phone Number</label>
                        <input type="tel" id="phoneNumber" name="phoneNumber" class="cust-modal-input" />
                    </div>
                    <div class="cust-modal-field half-width">
                        <label for="email" class="cust-modal-label">Email</label>
                        <input type="email" id="email" name="email" class="cust-modal-input" />
                    </div>
                </div>

                <hr class="form-divider" />

                <!-- Address -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="address" class="cust-modal-label">Street Address</label>
                        <input type="text" id="address" name="address" class="cust-modal-input" required />
                    </div>
                </div>

                <!-- Country / State / Zip -->
                <div class="form-row">
                    <div class="cust-modal-field third-width">
                        <label for="country" class="cust-modal-label">Country</label>
                        <select id="country" name="country" class="cust-modal-input">
                            <option value="USA">USA</option>
                            <option value="Canada">Canada</option>
                        </select>
                    </div>
                    <div class="cust-modal-field third-width">
                        <label for="state" class="cust-modal-label">State / Province</label>
                        <select id="state" name="state" class="cust-modal-input"></select>
                    </div>
                    <div class="cust-modal-field third-width">
                        <label for="zip" id="zipLabel" class="cust-modal-label">Zip Code</label>
                        <input type="text" id="zip" name="zip" class="cust-modal-input" />
                    </div>
                </div>

                <hr class="form-divider" />

                <!-- Note -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="note" class="cust-modal-label">Note</label>
                        <textarea id="note" name="note" class="cust-modal-input" rows="2"></textarea>
                    </div>
                </div>

                <!-- Active Switch -->
                <div class="form-row">
                    <div class="cust-modal-field cust-toggle-switch-container">
                        <label id="isActiveText" class="cust-modal-label">Active</label>
                        <div class="cust-toggle-switch">
                            <input type="checkbox" id="isActive" name="isActive" checked />
                            <label for="isActive"></label>
                        </div>
                    </div>
                </div>

                <!-- Buttons -->
                <div class="cust-modal-btns">
                    <button type="button" class="cust-modal-cancel" id="closeAddSite">Cancel</button>
                    <button type="button" onclick="saveSite(event )" class="cust-modal-submit">Add Site</button>
                </div>
            </form>
        </div>
    </div>



    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.datatables.net/2.2.2/js/dataTables.min.js"></script>
    <script type="text/javascript" src="https://cdn.datatables.net/select/3.0.0/js/dataTables.select.min.js"></script>
  
    <script>

        $(document).ready(function () {
            $('#customerTable tbody').on('click', 'tr', function () {
                $('#contact').show();
                $('#sites').show();

                // Optionally, adding 'active' class to toggle styling
                $('#contactBtn').addClass('active');
                $('#sitesBtn').addClass('active');
            });
        });

        function OpenCustomerChatHistory(mobile, name, customerId) {
            if (!mobile || mobile.trim() === "") {
                Swal.fire('Validation Error', 'Please insert phone number for this customer.', 'warning');
                return;
            }

            window.open('CustomerChatHistory.aspx?mobile=' + encodeURIComponent(mobile) +
                '&name=' + encodeURIComponent(name) +
                '&customerId=' + encodeURIComponent(customerId), '_blank');
        }


    </script>
    <script>
        // JavaScript to handle close icon functionality
        $(document).ready(function () {
            $('#closeAddCustomerIcon').on('click', function () {
                $('#addCustomerModal').hide();
            });
            $('#closeEditCustomerIcon').on('click', function () {
                $('#editCustomerModal').hide();
            });
            $('#closeAddSiteIcon').on('click', function () {
                $('#addSiteModal').hide();
            });
        });

    </script>
    <script>
        $('#customerTable tbody').on('click', '.cust-table-edit-btn', function () {
            const customerId = $(this).data('customer-id');
            const customerData = table.row($(this).closest('tr')).data();
            if (customerData) {
                document.getElementById('editFirstName').value = customerData.FirstName || '';
                document.getElementById('editLastName').value = customerData.LastName || '';
                document.getElementById('editEmail').value = customerData.Email || '';
                document.getElementById('editPhone').value = customerData.Phone || '';
                document.getElementById('editCustomerForm').dataset.customerId = customerData.CustomerID;
                document.getElementById('editCustomerForm').dataset.customerGuid = customerData.CustomerGuid;
                openModal('editCustomerModal');
            }
        });
    </script>
      <script src="Scripts/customer.js?v=13"></script>
</asp:Content>
