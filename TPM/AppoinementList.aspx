<%@ Page Title="TPM Appointments" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="AppoinementList.aspx.cs" Inherits="TPM.AppoinementList" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <link rel="stylesheet" href="https://cdn.datatables.net/2.2.2/css/dataTables.dataTables.min.css">
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/select/3.0.0/css/select.dataTables.min.css">

    <!-- Local Styles and Scripts -->
    <link rel="stylesheet" href="Content/customer.css">

    <style>
        .cust-action-btns {
            display: flex;
            gap: 10px;
           
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
                    <div class="pt-2 ps-2 d-flex align-items-center gap-3">
                        <div>
                            <label for="statusFilter" class="form-label">Filter by Status:</label>
                            <asp:DropDownList ID="statusFilter"  ClientIDMode="Static" runat="server" CssClass="form-select w-auto">
                                <asp:ListItem Text="All Statuses" Value="ALL"></asp:ListItem>
                                 <asp:ListItem Text="Accept" Value="Accept"></asp:ListItem>
                                <asp:ListItem Text="Pending" Value="Pending"></asp:ListItem>
                                <asp:ListItem Text="Cancel" Value="Cancel"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                   <%-- <div class="toggle-switch">
                        <label for="hideNA" class="form-label" style="font-size: 0.8rem;">Hide Customer with No Appointments:</label>
                        <input type="checkbox" id="hideNA" checked />
                    </div>--%>
                </div>

                <table id="AppointmentListTable" class="display" style="width: 100%">
                    <thead>
                        <tr>
                             <th>Source</th>
                          <th>Site Name</th>
                               <th>Date</th>
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
                        <button class="cust-section-toggle" data-section="sites" id="sitesBtn">Contact & Appointments details:-</button>
                        <div class="cust-section-content" id="sites">
                            <div class="sites-header">
                            </div>
                           
                            <div class="sites-list">
                                
                            </div>
                        </div>
                    </div>

                </div>
            </div>
        </section>
    </div>
    <div class="cust-modal" id="MsgViewModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeMsgView">×</button>
            <h2 class="cust-modal-title">Message</h2>
          <div class="text-center ajax-loader">
  <div class="spinner-border" role="status">
    <span class="visually-hidden">Loading...</span>
  </div>
</div>
                <div class="cust-modal-field">
                    <div id="div_Msg" ></div>
                </div>
               
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

     <script src="Scripts/AppointmentList.js?v=9"></script>
</asp:Content>