<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="FSM.Settings" MasterPageFile="~/TPM.Master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <link rel="stylesheet" href="Content/settings.css">

    <div class="container-fluid py-4">

        <!-- Main Tab Navigation -->
        <ul class="nav nav-tabs" id="settingsTabs" role="tablist">
            <li class="nav-item" role="presentation">
                <button class="nav-link active" id="custom-fields-tab" data-bs-toggle="tab" data-bs-target="#custom-fields" type="button" role="tab" aria-controls="custom-fields" aria-selected="true">
                    <i class="bi bi-ui-checks me-2"></i>Custom Fields
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="sms-settings-tab" data-bs-toggle="tab" data-bs-target="#sms-settings" type="button" role="tab" aria-controls="sms-settings" aria-selected="false">
                    <i class="bi bi-chat-text me-2"></i>Old Message Settings
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="automated-messages-main-tab" data-bs-toggle="tab" data-bs-target="#automated-messages-container" type="button" role="tab" aria-controls="automated-messages-container" aria-selected="false">
                    <i class="bi bi-robot me-2"></i>Communication
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="optional-status-tab" data-bs-toggle="tab" data-bs-target="#optional-status-content" type="button" role="tab" aria-controls="optional-status-content" aria-selected="false">
                    <i class="bi bi-list-ol me-2"></i>Optional Status
                </button>
            </li>
        </ul>

        <!-- Main Tab Content -->
        <div class="tab-content" id="settingsTabContent">

            <!--  Custom Fields Tab -->
            <div class="tab-pane fade show active" id="custom-fields" role="tabpanel" aria-labelledby="custom-fields-tab">
                <div class="d-flex justify-content-end align-items-center mb-4">
                    <div>
                        <button type="button" id="btnCreateNew" class="btn btn-success">
                            <i class="bi bi-plus-circle me-1"></i>Create New Field
                        </button>
                    </div>
                </div>

                <div class="card">
                    <div class="card-body">
                        <ul id="customFieldsList" class="list-group">
                        </ul>
                    </div>
                </div>
            </div>

            <!-- SMS Settings Tab -->
            <div class="tab-pane fade" id="sms-settings" role="tabpanel" aria-labelledby="sms-settings-tab">
                <div class="card">
                    <div class="card-header bg-light">
                        <div class="card-title">
                            <h4 class="mb-0">Check the types of communications you want to send to your customers</h4>
                        </div>
                    </div>

                    <input type="hidden" id="hdCompanyID" name="hdCompanyID" runat="server" />

                    <div class="card-body">
                        <div class="row">
                            <div class="col-12 col-md-6">

                                <!-- Pending Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Pending</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="PendingYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="PendingYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtPending" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                                <!-- Cancelled Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Cancelled</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="CancelledYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="CancelledYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtCancelled" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                                <!-- Installation In Progress Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Installation In Progress</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="ProgressYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="ProgressYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtProgress" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                            </div>

                            <div class="col-12 col-md-6">

                                <!-- Scheduled Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Confirmed</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="ScheduledYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="ScheduledYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtScheduled" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                                <!-- Closed Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Closed</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="ClosedYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="ClosedYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtClosed" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                                <!-- Completed Status -->
                                <div class="sms-section">
                                    <div class="sms-legend">Appointment Status: <strong>Completed</strong></div>
                                    <div class="sms-checkbox-container">
                                        <div class="form-check">
                                            <asp:CheckBox ID="CompletedYN" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="CompletedYN">Add Yes/No Option</label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Message Body</label>
                                        <textarea id="txtCompleted" rows="5" runat="server" class="form-control sms-textarea"></textarea>
                                    </div>
                                </div>

                            </div>
                        </div>

                        <div class="row">
                            <div class="col-12 col-md-3">
                                <asp:Button ID="SubmitData" runat="server" Text="Save SMS Settings" CssClass="btn btn-success w-100" OnClick="SubmitData_Click" />
                            </div>
                        </div>

                        <!-- Placeholder Information -->
                        <div class="placeholder-info">
                            <h6 class="mb-3">Available Placeholders:</h6>
                            <div class="row">
                                <div class="col-md-6">
                                    <small>[First Name] = Customer First Name</small>
                                    <small>[Last Name] = Customer Last Name</small>
                                    <small>[Full Name] = Customer Full Name</small>
                                    <small>[Title] = Customer Title</small>
                                </div>
                                <div class="col-md-6">
                                    <small>[Job Title] = Customer Job Title</small>
                                    <small>[Company Name] = Company Full Name</small>
                                    <small>[Time] = Appointment Time</small>
                                    <small>[Date] = Appointment Date</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!--  Messages Tab Container -->
            <div class="tab-pane fade" id="automated-messages-container" role="tabpanel" aria-labelledby="automated-messages-main-tab">
                <div class="row">
                    <!-- Vertical Navigation -->
                    <div class="col-md-2">
                        <div class="nav flex-column nav-pills nav-pills-vertical" id="v-pills-tab" role="tablist" aria-orientation="vertical">
                            <button class="nav-link active" id="v-pills-templates-tab" data-bs-toggle="pill" data-bs-target="#v-pills-templates" type="button" role="tab" aria-controls="v-pills-templates" aria-selected="true">Automated Messages</button>
                            <button class="nav-link" id="v-pills-fa-id-tab" data-bs-toggle="pill" data-bs-target="#v-pills-fa-id" type="button" role="tab" aria-controls="v-pills-fa-id" aria-selected="false">FA Messages</button>
                        </div>
                    </div>

                    <!-- Vertical Tab Content -->
                    <div class="col-md-10">
                        <div class="tab-content automated-messages-content" id="v-pills-tabContent">
                            <!-- Customization Template Tab -->
                            <div class="tab-pane fade show active" id="v-pills-templates" role="tabpanel" aria-labelledby="v-pills-templates-tab">

                                <div class="card border-0 shadow-sm">
                                    <div class="card-header bg-light border-bottom-0">
                                        <h4 class="mb-0">
                                            <i class="bi bi-robot me-2 text-primary"></i>Automated Message Customization
                                        </h4>
                                    </div>
                                    <div class="card-body p-4">
                                        <!-- Message Type Dropdown -->
                                        <div class="row mb-4">
                                            <div class="col-md-6">
                                                <label for="messageTypeDropdown" class="form-label fw-bold">Select Message Type</label>
                                                <select id="messageTypeDropdown" class="form-select">
                                                    <option value="AcceptTPWorkOrder">Accept TP Work Order</option>
                                                   <%-- <option value="Confirmation">Appointment Confirmation</option>
                                                    <option value="Dispatch">Field Agent Assigned</option>
                                                    <option value="FA-ID">Field Agent ID</option>
                                                    <option value="In-Route">FA In-Route</option>--%>
                                                </select>
                                            </div>

                                            <div class="col-md-6 d-flex align-items-end">
                                                <div class="d-flex justify-content-between w-100">
                                                    <!-- Status Trigger Display -->
                                                    <div id="statusTriggerContainer" class="d-none">
                                                        <label class="form-label fw-bold mb-1">Triggered by Status:</label>
                                                        <div id="statusTriggerDisplay" class="badge bg-secondary fs-6 fw-normal"></div>
                                                    </div>
                                                </div>
                                            </div>

                                        </div>

                                        <!-- Template Editor Section  -->
                                        <div id="templateEditorContainer" class="d-none">
                                            <hr />
                                            <h5 class="mt-4 mb-3">2. Customize Templates & Options</h5>
                                            <div class="row gx-4">
                                                <!-- Email Template -->
                                                <div class="col-lg-6 mb-4">
                                                    <div class="p-3 border rounded h-100">
                                                        <div class="d-flex align-items-center mb-3">
                                                            <i class="fa fa-envelope fs-5 text-primary me-3"></i>
                                                            <h5 class="mb-0">Email Template</h5>
                                                            <div class="form-check form-switch ms-auto">
                                                                <input class="form-check-input" type="checkbox" id="enableEmail">
                                                                <label class="form-check-label" for="enableEmail">Enable</label>
                                                            </div>
                                                        </div>
                                                         <textarea id="emailSubject" class="form-control" style="margin-bottom:4px;" rows="1" placeholder="Email subject..." ></textarea>
                                                        
                                                        <textarea id="emailBody" class="form-control" rows="8" placeholder="Email content..." ></textarea>
                                                    </div>
                                                </div>
                                                <!-- SMS Template -->
                                                <div class="col-lg-6 mb-4">
                                                    <div class="p-3 border rounded h-100">
                                                        <div class="d-flex align-items-center mb-3">
                                                            <i class="fa fa-comment-alt fs-5 text-primary me-3"></i>
                                                            <h5 class="mb-0">SMS Template</h5>
                                                            <div class="form-check form-switch ms-auto">
                                                                <input class="form-check-input" type="checkbox" id="enableSms">
                                                                <label class="form-check-label" for="enableSms">Enable</label>
                                                            </div>
                                                        </div>
                                                        <textarea id="smsBody" class="form-control" rows="8" placeholder="SMS content..." maxlength="160"></textarea>
                                                        <div class="text-end text-muted small mt-1" id="smsCharCounter">0 / 500</div>
                                                    </div>
                                                </div>
                                                <div id="additionalOptionsContainer" class="d-none">
                                                    <div class="d-flex align-items-start">
                                                        <!-- Left side: Switches -->
                                                        <div class="me-4">
                                                            <div id="ynSwitchContainer">
                                                                <div class="form-check form-switch">
                                                                    <input class="form-check-input" type="checkbox" id="smsYN">
                                                                    <label class="form-check-label" for="smsYN">Enable "Reply Y/N"</label>
                                                                </div>
                                                            </div>
                                                            <div class="form-check form-switch" id="faIdSwitchContainer" style="display: none;">
                                                                <input class="form-check-input" type="checkbox" id="sendFaIdCheck">
                                                                <label class="form-check-label" for="sendFaIdCheck">Send FA-ID Automatically</label>
                                                            </div>
                                                        </div>

                                                        <!-- Right side: Y/N Response Text boxes -->
                                                        <div id="ynResponseFields" class="flex-grow-1 d-none">
                                                            <div class="row gx-3">
                                                                <div class="col-md-6">
                                                                    <label for="yesResponseText" class="form-label fw-bold">"Yes" Response Text</label>
                                                                    <textarea id="yesResponseText" class="form-control" rows="2" placeholder="e.g., Thank you for confirming."></textarea>
                                                                </div>
                                                                <div class="col-md-6">
                                                                    <label for="noResponseText" class="form-label fw-bold">"No" Response Text</label>
                                                                    <textarea id="noResponseText" class="form-control" rows="2" placeholder="e.g., A representative will contact you."></textarea>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="mt-4 text-end">
                                                <button id="btnSaveMessageTemplates" type="button" onclick="return SaveMessageTemplates()" class="btn btn-primary px-4 py-2">
                                                    Save Settings for This Message Type
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                            </div>

                            <!-- Message Types/Triggers Tab -->
                            <div class="tab-pane fade" id="v-pills-triggers" role="tabpanel" aria-labelledby="v-pills-triggers-tab">
                                <div class="card">
                                    <div class="card-header bg-light">
                                        <h5 class="mb-0">Message Types / Triggers</h5>
                                    </div>
                                    <div class="card-body">
                                        <div class="table-responsive">
                                            <table class="table table-bordered table-hover table-vertical-align">
                                                <thead class="table-light">
                                                    <tr>
                                                        <th>Message Type</th>
                                                        <th>Status Trigger</th>
                                                        <th colspan="3" class="text-center">Message Options</th>
                                                        <th>Other Actions</th>
                                                    </tr>
                                                    <tr>
                                                        <th></th>
                                                        <th></th>
                                                        <th class="text-center">Email</th>
                                                        <th class="text-center">SMS</th>
                                                        <th class="text-center">Y/N Option</th>
                                                        <th></th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    <tr>
                                                        <td>Confirmation of Appointment Date and Time</td>
                                                        <td>Confirmed</td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td>None</td>
                                                    </tr>
                                                    <tr>
                                                        <td>Dispatch assignments</td>
                                                        <td>Dispatched</td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td>
                                                            <div class="form-check">
                                                                <input class="form-check-input" type="checkbox" id="sendFaIdCheck"><label class="form-check-label" for="sendFaIdCheck">Send FA-ID Autoatically</label>
                                                            </div>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>Automatic or Manual Send FA-ID</td>
                                                        <td>Auto w/Dispatch or Manual from Appointment</td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" disabled></td>
                                                        <td></td>
                                                    </tr>
                                                    <tr>
                                                        <td>FA on the way</td>
                                                        <td>In-route</td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" checked></td>
                                                        <td class="text-center">
                                                            <input type="checkbox" class="form-check-input" disabled></td>
                                                        <td></td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </div>
                                        <button class="btn btn-success mt-3">Save Triggers</button>
                                    </div>
                                </div>
                            </div>


                            <!--  FA Messages (Field Agent ID) Tab -->
                            <div class="tab-pane fade" id="v-pills-fa-id" role="tabpanel" aria-labelledby="v-pills-fa-id-tab">
                                <div class="card">
                                    <div class="card-header bg-light">
                                        <h5 class="mb-0">Field Agent (FA) ID Messages</h5>
                                    </div>
                                    <div class="card-body">
                                        <!-- Main Template -->
                                        <div class="p-4 border rounded bg-light mb-5">
                                            <div class="row">
                                                <!-- Left Column: Switches -->
                                                <div class="col-md-4">
                                                    <label class="form-label fw-bold">Default Message Format</label>
                                                    <div class="form-check form-switch mb-2">
                                                        <input class="form-check-input" type="checkbox" id="faEnableSms" checked>
                                                        <label class="form-check-label" for="faEnableSms">Enable SMS (Text Only)</label>
                                                    </div>
                                                    <div class="form-check form-switch">
                                                        <input class="form-check-input" type="checkbox" id="faEnableMms">
                                                        <label class="form-check-label" for="faEnableMms">Enable MMS (Include FA Picture)</label>
                                                    </div>
                                                    <div class="mb-3 mt-3">
                                                        <label for="faDaysBeforeAppointment" class="form-label fw-bold">When to Send</label>
                                                        <select id="faDaysBeforeAppointment" class="form-select">
                                                            <option value="0">0 (Day of Appointment)</option>
                                                            <option value="1">1 Day Before</option>
                                                            <option value="2">2 Days Before</option>
                                                            <option value="3">3 Days Before</option>
                                                            <option value="4">4 Days Before</option>
                                                            <option value="5">5 Days Before</option>
                                                            <option value="6">6 Days Before</option>
                                                            <option value="7">7 Days Before</option>
                                                            <option value="8">8 Days Before</option>
                                                            <option value="9">9 Days Before</option>
                                                            <option value="10">10 Days Before</option>
                                                        </select>
                                                    </div>
                                                </div>
                                                <!-- Right Column: Content and Save Button -->
                                                <div class="col-md-8">
                                                    <div class="mb-3">
                                                        <label for="faStandardContent" class="form-label fw-bold">Standard Company Content</label>
                                                        <textarea id="faStandardContent" class="form-control" rows="4" placeholder="This text is included in every FA message. Use [FA_Custom_Content] as a placeholder for the agent's personal text." readonly></textarea>

                                                    </div>
                                                    <div class="text-end">
                                                        <button type="button" id="btnClearMasterTemplate" class="btn btn-outline-secondary me-2">
                                                            <i class="fas fa-times me-1"></i>Clear
                                                        </button>
                                                        <button type="submit" id="btnSaveMasterTemplate" class="btn btn-outline-success position-relative">                                                      
                                                            <i id="tempCheckIcon" class="fas fa-check temp-check-icon"></i>
                                                            <!-- The button's text content will be in a span -->
                                                            <span id="btnSaveMasterTemplateText"></span>
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>

                                        <!-- FA Content Profile Table -->
                                        <div class="d-flex justify-content-between align-items-center mb-3">
                                            <h5 class="mb-0">Field Agent Content Profiles</h5>
                                            <button type="button" class="btn btn-outline-primary" data-bs-toggle="modal" data-bs-target="#faProfileModal">
                                                <i class="fa fa-user-plus me-1"></i>Add New 
                                            </button>
                                        </div>
                                        <div class="table-responsive">
                                            <table class="table table-bordered table-hover align-middle">
                                                <thead class="table-light">
                                                    <tr>

                                                        <th>FA Name</th>
                                                        <th>Mobile Phone</th>
                                                        <th>Custom Content</th>
                                                        <th>Picture</th>
                                                        <th class="text-center">Actions</th>
                                                    </tr>
                                                </thead>
                                                <tbody id="faProfileTableBody">
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!--Optional Statuses-->
            <div class="tab-pane fade" id="optional-status-content" role="tabpanel" aria-labelledby="optional-status-tab">
                <div class="card">
                    <div class="card-header bg-light">
                        <h5 class="mb-0">Status List Information</h5>
                    </div>
                    <div class="card-body">
                        <div class="table-responsive">
                            <table class="table table-bordered align-middle">
                                <thead class="status-table">
                                    <tr>
                                        <th>Status</th>
                                        <th class="text-center">Optional (Y/N)</th>
                                        <th>Triggered By</th>
                                        <th>Triggers</th>
                                        <th>Modify In</th>
                                    </tr>
                                </thead>
                                <tbody id="statusInfoTableBody">
                                </tbody>
                            </table>
                        </div>
                        <div class="text-end mt-3">
                            <button id="btnSaveOptionalStatuses" class="btn btn-primary px-4">
                                <i class="bi bi-check-circle me-1"></i>Save
                            </button>
                        </div>
                    </div>
                </div>
            </div>

        </div>
    </div>

    <!-- Custom Fields Modal  -->
    <div class="modal fade" id="addCustomFieldModal" tabindex="-1" aria-labelledby="addCustomFieldModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="addCustomFieldModalLabel">Create Custom Field</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" id="hfFieldId" value="">
                    <div class="row">
                        <div class="col-md-6">
                            <div class="mb-3">
                                <label for="fieldName" class="form-label">Field Name *</label>
                                <input type="text" id="fieldName" class="form-control" placeholder="e.g., Gate Code">
                            </div>
                            <div class="mb-3">
                                <label for="fieldType" class="form-label">Field Type</label>
                                <select id="fieldType" class="form-select">
                                    <option value="text">Text</option>
                                    <option value="number">Number</option>
                                    <option value="date">Date</option>
                                    <option value="dropdown">Drop-down List</option>
                                    <option value="checklist">Checklist</option>
                                </select>
                            </div>
                            <div class="mb-3" id="divOptions" style="display: none;">
                                <label class="form-label">Field Options *</label>
                                <div id="optionsContainer" class="p-3 border rounded bg-light"></div>
                                <button type="button" id="btnAddOption" class="btn btn-link text-success p-0 mt-2">
                                    <i class="bi bi-plus-circle"></i>Add New Option
                                </button>
                            </div>
                            <div class="form-check form-switch mt-3">
                                <input class="form-check-input" type="checkbox" id="isActive" checked>
                                <label class="form-check-label" for="isActive">Field is Active</label>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="mt-4" id="previewSection">
                                <label class="form-label">Preview:</label>
                                <div id="fieldPreview" class="preview-container"></div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" id="btnSave" class="btn btn-primary">Save Field</button>
                </div>
            </div>
        </div>
    </div>
    <!-- FA Profile Modal -->
    <div class="modal fade" id="faProfileModal" tabindex="-1" aria-labelledby="faProfileModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="faProfileModalLabel">Create New Field Agent Profile</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label for="faProfileName" class="form-label">Field Agent Name*</label>
                            <input type="text" id="faProfileName" class="form-control" placeholder="e.g., John Doe">
                        </div>
                        <div class="col-md-6 mb-3">
                            <label for="faProfilePhone" class="form-label">Mobile Phone (Optional )</label>
                            <input type="text" id="faProfilePhone" class="form-control" placeholder="e.g., 555-123-4567">
                        </div>
                    </div>
                    <div class="mb-3">
                        <label for="faProfileCustomContent" class="form-label">Custom Content</label>
                        <textarea id="faProfileCustomContent" class="form-control" rows="4" placeholder="e.g., I have 15 years of experience and enjoy fishing on weekends."></textarea>
                    </div>
                    <div class="mb-3">
                        <label for="faProfilePicture" class="form-label">Agent Picture</label>
                        <input type="file" id="faProfilePicture" class="form-control">
                        <small class="text-muted">Upload a picture to be included in MMS messages.</small>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary">Save Profile</button>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/sortablejs@latest/Sortable.min.js"></script>
    <script>

        document.addEventListener('DOMContentLoaded', function () {
            const triggerTabList = document.querySelectorAll('#settingsTabs button');
            triggerTabList.forEach(triggerEl => {
                const tabTrigger = new bootstrap.Tab(triggerEl);
                triggerEl.addEventListener('click', event => {
                    event.preventDefault();
                    tabTrigger.show();
                });
            });

            initializeCustomFields();
            initializeMessageTemplates();
            initializeFaProfiles();
            initializeStatusInfoTable();
        });

        function initializeStatusInfoTable() {
            const tableBody = document.getElementById('statusInfoTableBody');
            const saveBtn = document.getElementById('btnSaveOptionalStatuses');


            const statusDescriptions = {
                "Pending": { optional: 'Y', triggeredBy: "New Appointment - CEC", triggers: "Customer Message", modifyIn: "FSM" },
                "Confirmed": { optional: 'Y', triggeredBy: "Status Change in CEC", triggers: "Customer & FA Message & Status Update", modifyIn: "FSM" },
                "Scheduled": { optional: 'Y', triggeredBy: "Status Change in FSM", triggers: "Customer & FA Message & Status Update", modifyIn: "FSM" },
                "Dispatched": { optional: 'Y', triggeredBy: "Status Change in FSM", triggers: "Customer & FA Message & Status Update", modifyIn: "FSM" },
                "In-Route": { optional: 'Y', triggeredBy: "Status Change in FSM", triggers: "Customer Message & Status Update", modifyIn: "FSM" },
                "Arrived": { optional: 'Y', triggeredBy: "Status Change in FA App", triggers: "Status Update", modifyIn: "FSM" },
                "Completed": { optional: 'N', triggeredBy: "Status Change in FSM/CEC/FA App", triggers: "Status Update - Removal from FA-APP Appointment List", modifyIn: "N/A" },
                "Closed": { optional: 'Y', triggeredBy: "Status Change in FSM/CEC/FA App", triggers: "Status Update - Removal from FA-APP Appointment List", modifyIn: "FSM" },
                "On-Hold": { optional: 'N', triggeredBy: "Status Change in FSM/CEC/FA App", triggers: "N/A", modifyIn: "CEC" },
                "Cancelled": { optional: 'Y', triggeredBy: "Status Change in FSM/CEC/FA App", triggers: "Status Update - Removal from FA-APP Appointment List", modifyIn: "FSM" },
                "Parts on Order": { optional: 'Y', triggeredBy: "Status Change in FSM/CEC/FA App", triggers: "N/A", modifyIn: "CEC" }
            };

            function loadStatusTable() {
                PageMethods.GetStatuses(function (statuses) {
                    tableBody.innerHTML = '';
                    if (!statuses || statuses.length === 0) {
                        tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No statuses found.</td></tr>';
                        return;
                    }

                    statuses.forEach(status => {
                        const desc = statusDescriptions[status.StatusName] || { optional: 'N', triggeredBy: "N/A", triggers: "N/A", modifyIn: "N/A" };
                        const isOptional = desc.optional === 'Y';


                        const isSwitchEnabled = desc.modifyIn === 'FSM';

                        const rowClass = isOptional ? '' : 'tr-disabled';

                        let optionalControl;
                        if (isOptional) {

                            const disabledAttr = isSwitchEnabled ? '' : 'disabled';
                            optionalControl = `<div class="form-check form-switch d-flex justify-content-center">
                                           <input class="form-check-input" type="checkbox" data-status-id="${status.StatusID}" ${disabledAttr} checked>
                                       </div>`;
                        } else {

                            optionalControl = 'N';
                        }
                        
                        let displayName = status.StatusName;
                        if (status.StatusName === 'Scheduled') {
                            displayName = 'Confirmed';
                        }


                        const rowHtml = `
                    <tr class="${rowClass}">
                        <td><strong>${displayName}</strong></td>
                        <td class="text-center">${optionalControl}</td>
                        <td>${desc.triggeredBy}</td>
                        <td>${desc.triggers}</td>
                        <td>${desc.modifyIn}</td>
                    </tr>
                `;
                        tableBody.insertAdjacentHTML('beforeend', rowHtml);
                    });

                }, function (error) {
                    console.error("Error loading statuses for info table:", error);
                    tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Error loading statuses.</td></tr>';
                });
            }

            saveBtn.addEventListener('click', () => {
                const optionalSettings = [];
                const switches = tableBody.querySelectorAll('input[type="checkbox"]:not(:disabled)');

                switches.forEach(sw => {
                    optionalSettings.push({
                        statusId: sw.dataset.statusId,
                        isEnabled: sw.checked
                    });
                });

                console.log("Saving optional status settings:", optionalSettings);
                alert("Optional status settings saved to console. Implement server-side saving next.");
            });

            const statusTab = document.getElementById('optional-status-tab');
            statusTab.addEventListener('shown.bs.tab', loadStatusTable, { once: true });
        }


        function initializeFaProfiles() {
            // --- Get References to all necessary UI Elements ---
            const faProfileModalEl = document.getElementById('faProfileModal');
            const faProfileModal = new bootstrap.Modal(faProfileModalEl);
            const modalTitle = document.getElementById('faProfileModalLabel');
            const saveProfileBtn = faProfileModalEl.querySelector('.btn-primary');
            const faProfileName = document.getElementById('faProfileName');
            const faProfilePhone = document.getElementById('faProfilePhone');
            const faProfileCustomContent = document.getElementById('faProfileCustomContent');
            const faProfilePicture = document.getElementById('faProfilePicture');
            const btnSaveMasterTemplate = document.getElementById('btnSaveMasterTemplate');
            const faEnableSms = document.getElementById('faEnableSms');
            const faEnableMms = document.getElementById('faEnableMms');
            const faDaysBeforeAppointment = document.getElementById('faDaysBeforeAppointment');
            const faStandardContent = document.getElementById('faStandardContent');
            const tableBody = document.getElementById('faProfileTableBody');
            const addProfileBtn = document.querySelector('button[data-bs-target="#faProfileModal"]');
            const btnClearMasterTemplate = document.getElementById('btnClearMasterTemplate');
            const tempCheckIcon = document.getElementById('tempCheckIcon');
            const btnSaveMasterTemplateText = document.getElementById('btnSaveMasterTemplateText');

            // --- State Management ---
            let profiles = []; 
            let editingProfileId = 0; 
            let isSaving = false;
            let originalContent = "";
            let originalDaysBefore = 0;
            let currentMode = 'view';

            function updateButtonState(mode) {             
                currentMode = mode;
                btnSaveMasterTemplateText.style.opacity = 1;
                btnSaveMasterTemplateText.classList.remove('fade-in-text');
                switch (mode) {
                    case 'change':
                        btnSaveMasterTemplateText.innerHTML = '<i class="fas fa-pencil-alt me-1"></i>Change Template';
                        btnSaveMasterTemplate.classList.remove('btn-outline-success');
                        btnSaveMasterTemplate.classList.add('btn-outline-primary');
                        faStandardContent.readOnly = true;
                        faDaysBeforeAppointment.disabled = true;
                        break;
                    case 'save':
                        btnSaveMasterTemplateText.innerHTML = '<i class="fas fa-save me-1"></i>Save Template';
                        btnSaveMasterTemplate.classList.remove('btn-outline-primary');
                        btnSaveMasterTemplate.classList.add('btn-outline-success');
                        faStandardContent.readOnly = false;
                        faDaysBeforeAppointment.disabled = false;
                        break;
                    case 'saving':
                        btnSaveMasterTemplateText.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
                        break;
                }
            }


            btnSaveMasterTemplate.addEventListener('click', (event) => {
                
                event.preventDefault();
                if (isSaving) return;
                if (currentMode === 'change') {
                    faStandardContent.readOnly = false;
                    faDaysBeforeAppointment.disabled = false;
                    faStandardContent.focus();
                } else if (currentMode === 'save') {
                    isSaving = true;
                    btnSaveMasterTemplate.disabled = true;
                    updateButtonState('saving');
                    const enableSms = faEnableSms.checked;
                    const enableMms = faEnableMms.checked;
                    const standardContent = faStandardContent.value;
                    const daysBeforeAppointment = parseInt(faDaysBeforeAppointment.value);
                    PageMethods.SaveFaMasterTemplate(enableSms, enableMms, standardContent, daysBeforeAppointment,
                        function (response) {
                            if (response.success) {
                                originalContent = standardContent;
                                originalDaysBefore = daysBeforeAppointment;
                                btnSaveMasterTemplateText.style.opacity = 0;
                                tempCheckIcon.classList.add('animate-check');
                                setTimeout(() => {
                                    tempCheckIcon.classList.remove('animate-check');
                                    updateButtonState('change');
                                    btnSaveMasterTemplateText.classList.add('fade-in-text');
                                    btnSaveMasterTemplate.disabled = false;
                                    isSaving = false;
                                }, 1500);
                            } else {
                                Swal.fire('Error!', response.message, 'error');
                                updateButtonState('save');
                                btnSaveMasterTemplate.disabled = false;
                                isSaving = false;
                            }
                        },
                        function (error) {
                            console.error('SaveFaMasterTemplate API Error:', error);
                            Swal.fire('Error!', 'A server error occurred.', 'error');
                            updateButtonState('save');
                            btnSaveMasterTemplate.disabled = false;
                            isSaving = false;
                        }
                    );
                }
            });

            // --- Event handler to detect changes in the textarea ---
            faStandardContent.addEventListener('input', () => {
                
                if (!faStandardContent.readOnly && (faStandardContent.value !== originalContent || parseInt(faDaysBeforeAppointment.value) !== originalDaysBefore)) {
                    if (currentMode !== 'save') { updateButtonState('save'); }
                } else if (!faStandardContent.readOnly && faStandardContent.value === originalContent && parseInt(faDaysBeforeAppointment.value) === originalDaysBefore) {
                    if (currentMode !== 'change') {
                        updateButtonState('change');
                        faStandardContent.readOnly = false;
                        faDaysBeforeAppointment.disabled = false;
                    }
                }
            });

            faDaysBeforeAppointment.addEventListener('change', () => {
                if (!faDaysBeforeAppointment.disabled && (faStandardContent.value !== originalContent || parseInt(faDaysBeforeAppointment.value) !== originalDaysBefore)) {
                    if (currentMode !== 'save') { updateButtonState('save'); }
                } else if (!faDaysBeforeAppointment.disabled && faStandardContent.value === originalContent && parseInt(faDaysBeforeAppointment.value) === originalDaysBefore) {
                    if (currentMode !== 'change') {
                        updateButtonState('change');
                        faStandardContent.readOnly = false;
                        faDaysBeforeAppointment.disabled = false;
                    }
                }
            });

            // --- Event Handler for the "Clear" button ---
            btnClearMasterTemplate.addEventListener('click', () => {
               
                faStandardContent.value = '';
                faDaysBeforeAppointment.value = '0';
                updateButtonState('save');
                faStandardContent.readOnly = false;
                faDaysBeforeAppointment.disabled = false;
                faStandardContent.focus();
            });

            // --- Function to load data for the entire FA Messages tab ---
            function loadFaTabData() {              
                faStandardContent.value = "Loading...";
                PageMethods.GetFaMasterTemplate(
                    function (response) {
                        if (response.success && response.data) {
                            faEnableSms.checked = response.data.enableSms;
                            faEnableMms.checked = response.data.enableMms;
                            faStandardContent.value = response.data.standardContent;
                            faDaysBeforeAppointment.value = response.data.daysBeforeAppointment;
                            originalContent = response.data.standardContent;
                            originalDaysBefore = response.data.daysBeforeAppointment;
                            const hasContent = originalContent && originalContent.trim() !== '';
                            updateButtonState(hasContent ? 'change' : 'save');
                        } else {
                            console.error("Failed to load master template:", response.message);
                            faStandardContent.value = "Error loading template.";
                            faDaysBeforeAppointment.value = '0';
                            updateButtonState('save');
                        }
                    },
                    function (error) {
                        console.error('GetFaMasterTemplate API Error:', error);
                        alert('A critical error occurred while loading page data.');
                        faStandardContent.value = "Error loading template.";
                        faDaysBeforeAppointment.value = '0';
                        updateButtonState('save');
                    }
                );

                
                PageMethods.GetFaProfiles(function (response) {
                    if (response.success) {
                        profiles = response.data; 
                        renderTable(); 
                    } else {
                        console.error("Failed to load FA profiles:", response.message);
                        tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Error loading profiles.</td></tr>';
                    }
                }, function (error) {
                    console.error("GetFaProfiles API error:", error);
                    tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">A server error occurred.</td></tr>';
                });
            } 
            const faTab = document.getElementById('v-pills-fa-id-tab');
            faTab.addEventListener('shown.bs.tab', function () {
                loadFaTabData();
            });

            function renderTable() {
                tableBody.innerHTML = '';
                if (!profiles || profiles.length === 0) {
                    tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No agent profiles created yet. Click "Add New" to begin.</td></tr>';
                    return;
                }
                profiles.forEach((profile) => {
                    const rowHtml = `
                <tr data-profile-id="${profile.ProfileID}">
                    <td>${escapeHtml(profile.FaName)}</td>
                    <td>${escapeHtml(profile.MobilePhone)}</td>
                    <td>${escapeHtml(profile.CustomContent)}</td>
                    <td class="text-center"><img src="${profile.PictureUrl || 'https://via.placeholder.com/50'}" class="rounded-circle" alt="Agent Picture" width="50" height="50"></td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm btn-outline-primary me-1 edit-btn"><i class="fas fa-pencil-alt"></i></button>
                        <button type="button" class="btn btn-sm btn-outline-danger delete-btn"><i class="fas fa-trash"></i></button>
                    </td>
                </tr>`;
                    tableBody.insertAdjacentHTML('beforeend', rowHtml);
                });
            }

            // --- Helper function to clear the FA profile modal form ---
            function clearModalForm() {
                editingProfileId = 0; 
                faProfileName.value = '';
                faProfilePhone.value = '';
                faProfileCustomContent.value = '';
                faProfilePicture.value = '';
            }

            function escapeHtml(text) {
                if (!text) return '';
                const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
                return text.replace(/[&<>"']/g, m => map[m]);
            }

            addProfileBtn.addEventListener('click', () => {
                clearModalForm();
                modalTitle.textContent = 'Create New Field Agent Profile';
                faProfileCustomContent.value = faStandardContent.value;
                faProfileModal.show();
            });

            saveProfileBtn.addEventListener('click', () => {
                const name = faProfileName.value.trim();
                const phone = faProfilePhone.value.trim();
                const content = faProfileCustomContent.value.trim();

                if (!name) {
                    alert('Field Agent Name is required.');
                    return;
                }

                PageMethods.SaveFaProfile(editingProfileId, name, phone, content,
                    function (response) {
                        if (response.success) {
                            const savedProfile = response.data;
                            if (editingProfileId === 0) {
                                profiles.push(savedProfile);
                            } else {
                                const index = profiles.findIndex(p => p.ProfileID === savedProfile.ProfileID);
                                if (index !== -1) {
                                    profiles[index] = savedProfile;
                                }
                            }
                            renderTable(); 
                            faProfileModal.hide();
                            Swal.fire('Saved!', 'The agent profile has been saved.', 'success');
                        } else {
                            Swal.fire('Error!', response.message, 'error');
                        }
                    },
                    function (error) {
                        console.error("SaveFaProfile API error:", error);
                        Swal.fire('Error!', 'A server error occurred while saving the profile.', 'error');
                    }
                );
            });

            // --- Event delegation for Edit/Delete buttons in the FA profiles table ---
            tableBody.addEventListener('click', (e) => {
                const target = e.target.closest('button');
                if (!target) return;

                const tr = target.closest('tr');
                const profileId = parseInt(tr.dataset.profileId);
                const profile = profiles.find(p => p.ProfileID === profileId);
                if (!profile) return;

                if (target.classList.contains('edit-btn')) {
                    editingProfileId = profile.ProfileID;
                    modalTitle.textContent = 'Edit Field Agent Profile';
                    faProfileName.value = profile.FaName;
                    faProfilePhone.value = profile.MobilePhone;
                    faProfileCustomContent.value = profile.CustomContent;
                    faProfileModal.show();
                }

                if (target.classList.contains('delete-btn')) {
                    Swal.fire({
                        title: 'Are you sure?',
                        text: `Do you want to delete the profile for ${profile.FaName}?`,
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#d33',
                        confirmButtonText: 'Yes, delete it!'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            profiles = profiles.filter(p => p.ProfileID !== profileId);
                            renderTable();
                            Swal.fire('Deleted!', 'The profile has been removed.', 'success');
                        }
                    });
                }
            });
        }

         const messageTypeConfig = {
                "Confirmation": { status: "Confirmed", faIdSwitch: false, ynAllowed: true },
                "AcceptTPWorkOrder": { status: "Accept", faIdSwitch: false, ynAllowed: true },
                "Dispatch": { status: "Dispatched", faIdSwitch: true, ynAllowed: true },
                "FA-ID": { status: "Dispatched", faIdSwitch: false, ynAllowed: false },
                "In-Route": { status: "In-Route", faIdSwitch: false, ynAllowed: false }
        };

        function initializeMessageTemplates() {

            const messageTypeDropdown = document.getElementById('messageTypeDropdown');
            const templateEditorContainer = document.getElementById('templateEditorContainer');
            const statusTriggerContainer = document.getElementById('statusTriggerContainer');
            const statusTriggerDisplay = document.getElementById('statusTriggerDisplay');


            const enableEmail = document.getElementById('enableEmail');
            const emailBody = document.getElementById('emailBody');
            const emailCharCounter = document.getElementById('emailCharCounter');
            const enableSms = document.getElementById('enableSms');
            const smsBody = document.getElementById('smsBody');
            const smsCharCounter = document.getElementById('smsCharCounter');


            const smsYN = document.getElementById('smsYN');
            const faIdSwitchContainer = document.getElementById('faIdSwitchContainer');
            const additionalOptionsContainer = document.getElementById('additionalOptionsContainer');
            const sendFaIdCheck = document.getElementById('sendFaIdCheck');
            const ynSwitchContainer = document.getElementById('ynSwitchContainer');
          //  const saveButton = document.getElementById('btnSaveTemplates');
             const btnSaveMessageTemplates = document.getElementById('btnSaveMessageTemplates');
            
            const ynResponseFields = document.getElementById('ynResponseFields');
            const yesResponseText = document.getElementById('yesResponseText');
            const noResponseText = document.getElementById('noResponseText');
            
           

            let allMessageData = {};
            messageTypeDropdown.addEventListener('change', function () {
                const selectedType = this.value;
                updateEditorUI(selectedType);
            });
            smsBody.addEventListener('input', () => {
                smsCharCounter.textContent = `${smsBody.value.length} / 320`;
            });
            smsYN.addEventListener('change', function () {
                if (this.checked) {
                    ynResponseFields.classList.remove('d-none');
                } else {
                    ynResponseFields.classList.add('d-none');
                }
            });
      
           

          

            
            function updateEditorUI(type) {
                if (!type) {
                    templateEditorContainer.classList.add('d-none');
                    statusTriggerContainer.classList.add('d-none');
                    additionalOptionsContainer.classList.add('d-none');
                    return;
                }

                const config = messageTypeConfig[type];
                templateEditorContainer.classList.remove('d-none');
                statusTriggerContainer.classList.remove('d-none');
                additionalOptionsContainer.classList.remove('d-none');

                statusTriggerDisplay.textContent = config.status;
                faIdSwitchContainer.style.display = config.faIdSwitch ? 'block' : 'none';

                if (config.ynAllowed) {
                    ynSwitchContainer.style.display = 'block';
                } else {
                    ynSwitchContainer.style.display = 'none';
                    smsYN.checked = false;
                }


                smsYN.dispatchEvent(new Event('change'));

                loadTemplateData(type);
            }

            
            function init() {

                messageTypeDropdown.value = 'AcceptTPWorkOrder';
                messageTypeDropdown.dispatchEvent(new Event('change'));
            }
            init();
        }
         

        function initializeCustomFields() {
            const modalEl = document.getElementById('addCustomFieldModal');
            const modalInstance = new bootstrap.Modal(modalEl);
            const customFieldsList = document.getElementById('customFieldsList');

            const modalTitle = document.getElementById('addCustomFieldModalLabel');
            const hfFieldId = document.getElementById('hfFieldId');
            const fieldName = document.getElementById('fieldName');
            const fieldType = document.getElementById('fieldType');
            const isActive = document.getElementById('isActive');
            const divOptions = document.getElementById('divOptions');
            const optionsContainer = document.getElementById('optionsContainer');
            const btnAddOption = document.getElementById('btnAddOption');
            const fieldPreview = document.getElementById('fieldPreview');

            function refreshFieldsList() {
                customFieldsList.innerHTML = '<div class="list-group-placeholder">Loading fields...</div>';
                PageMethods.GetFields(onGetFieldsSuccess, onApiError);
            }

            function onGetFieldsSuccess(response) {
                if (response.success) {
                    renderFields(response.data);
                } else {
                    showError(response.message);
                    customFieldsList.innerHTML = '<div class="list-group-placeholder">Could not load fields.</div>';
                }
            }

            function renderFields(fields) {
                customFieldsList.innerHTML = '';
                if (fields.length === 0) {
                    customFieldsList.innerHTML = '<div class="list-group-placeholder">No custom fields found. Click "Create New Field" to get started.</div>';
                    return;
                }
                fields.forEach(field => {
                    const activeStatus = field.IsActive ? '' : "<span class='badge bg-light text-dark ms-2'>Inactive</span>";
                    const toggleButton = field.IsActive
                        ? `<button type="button" class="btn btn-sm btn-outline-warning me-2 active-btn" data-fieldid="${field.FieldId}"><i class="bi bi-eye-slash-fill me-1"></i> Deactivate</button>`
                        : `<button type="button" class="btn btn-sm btn-outline-info me-2 active-btn" data-fieldid="${field.FieldId}"><i class="bi bi-eye-fill me-1"></i> Activate</button>`;

                    const li = document.createElement('li');
                    li.className = 'list-group-item d-flex justify-content-between align-items-center';
                    li.innerHTML = `
                        <div>
                            <i class="bi bi-grip-vertical text-muted me-2"></i>
                            <strong>${escapeHtml(field.FieldName)}</strong>
                            <span class="badge bg-secondary ms-2">${escapeHtml(field.FieldType)}</span>
                            ${activeStatus}
                        </div>
                        <div>
                            <button type="button" class="btn btn-sm btn-outline-primary me-2 edit-btn" data-fieldid="${field.FieldId}">
                                <i class="bi bi-pencil-fill me-1"></i> Edit
                            </button>
                            ${toggleButton}
                            <button type="button" class="btn btn-sm btn-outline-danger delete-btn" data-fieldid="${field.FieldId}">
                                <i class="bi bi-trash-fill me-1"></i> Delete
                            </button>
                        </div>`;
                    customFieldsList.appendChild(li);
                });
            }

            document.getElementById('btnCreateNew').addEventListener('click', () => {
                clearModalForm();
                modalTitle.textContent = 'Create Custom Field';
                modalInstance.show();
            });

            document.getElementById('btnSave').addEventListener('click', () => {
                if (validateForm()) {
                    const fieldId = parseInt(hfFieldId.value) || 0;
                    const name = fieldName.value;
                    const type = fieldType.value;
                    const options = collectOptions();
                    const active = isActive.checked;

                    PageMethods.SaveField(fieldId, name, type, options, active, onSaveSuccess, onApiError);
                }
            });

            function onSaveSuccess(response) {
                if (response.success) {
                    modalInstance.hide();
                    refreshFieldsList();
                } else {
                    showError(response.message);
                }
            }

            customFieldsList.addEventListener('click', (e) => {
                const target = e.target.closest('button');
                if (!target) return;

                const fieldId = parseInt(target.dataset.fieldid);

                if (target.classList.contains('edit-btn')) {
                    PageMethods.GetFields(function (response) {
                        if (response.success) {
                            const field = response.data.find(f => f.FieldId === fieldId);
                            if (field) loadFieldDataForEdit(field);
                        }
                    }, onApiError);
                }
                if (target.classList.contains('delete-btn')) {
                    if (confirm('Are you sure you want to delete this field? This will also remove all data entered for this field from existing appointments.')) {
                        PageMethods.DeleteField(fieldId, onModifySuccess, onApiError);
                    }
                }
                if (target.classList.contains('active-btn')) {
                    PageMethods.ToggleFieldActive(fieldId, onModifySuccess, onApiError);
                }
            });

            function onModifySuccess(response) {
                if (response.success) {
                    refreshFieldsList();
                } else {
                    showError(response.message);
                }
            }

            fieldType.addEventListener('change', () => { updateVisibility(); buildPreview(); });
            fieldName.addEventListener('input', buildPreview);
            btnAddOption.addEventListener('click', () => {
                addOptionInput('');
                const lastInput = optionsContainer.querySelector('.option-input:last-of-type');
                if (lastInput) lastInput.focus();
            });

            function updateVisibility() {
                divOptions.style.display = (fieldType.value === 'dropdown' || fieldType.value === 'checklist') ? 'block' : 'none';
            }

            function addOptionInput(value) {
                const div = document.createElement('div');
                div.className = 'input-group option-input-group';
                div.innerHTML = `
                    <span class="input-group-text"><i class="bi bi-grip-vertical text-muted"></i></span>
                    <input type="text" class="form-control option-input" value="${escapeHtml(value)}" placeholder="Option value">
                    <button type="button" class="btn btn-outline-danger remove-option"><i class="bi bi-x-lg"></i></button>`;
                optionsContainer.appendChild(div);
                div.querySelector('.remove-option').addEventListener('click', () => {
                    div.remove();
                    buildPreview();
                });
                div.querySelector('.option-input').addEventListener('input', buildPreview);
            }

            function collectOptions() {
                if (fieldType.value !== 'dropdown' && fieldType.value !== 'checklist') return '';
                const inputs = optionsContainer.querySelectorAll('.option-input');
                const options = Array.from(inputs).map(i => i.value.trim()).filter(v => v);
                return JSON.stringify(options);
            }

            function validateForm() {
                if (!fieldName.value.trim()) {
                    showError('Field name is required.');
                    fieldName.focus();
                    return false;
                }
                if ((fieldType.value === 'dropdown' || fieldType.value === 'checklist') && collectOptions() === '[]') {
                    showError('At least one option is required for this field type.');
                    return false;
                }
                return true;
            }

            function buildPreview() {
                const name = fieldName.value.trim() || 'Field Name';
                const type = fieldType.value;
                const options = JSON.parse(collectOptions() || '[]');
                let html = `<label class="form-label mb-2"><strong>${escapeHtml(name)}</strong></label>`;
                switch (type) {
                    case 'text': html += `<input type="text" class="form-control" disabled>`; break;
                    case 'number': html += `<input type="number" class="form-control" disabled>`; break;
                    case 'date': html += `<input type="date" class="form-control" disabled>`; break;
                    case 'dropdown':
                        html += `<select class="form-select" disabled><option>-- Select --</option>`;
                        options.forEach(o => html += `<option>${escapeHtml(o)}</option>`);
                        html += `</select>`;
                        break;
                    case 'checklist':
                        if (options.length === 0) {
                            html += '<p class="text-muted small">Add options to see preview.</p>';
                        } else {
                            options.forEach(o => html += `<div class="form-check"><input type="checkbox" disabled><label class="form-check-label ms-2">${escapeHtml(o)}</label></div>`);
                        }
                        break;
                }
                fieldPreview.innerHTML = html;
            }

            function clearModalForm() {
                hfFieldId.value = '0';
                fieldName.value = '';
                fieldType.selectedIndex = 0;
                isActive.checked = true;
                optionsContainer.innerHTML = '';
                updateVisibility();
                buildPreview();
            }

            function loadFieldDataForEdit(field) {
                clearModalForm();
                modalTitle.textContent = 'Edit Custom Field';
                hfFieldId.value = field.FieldId;
                fieldName.value = field.FieldName;
                fieldType.value = field.FieldType;
                isActive.checked = field.IsActive;

                if (field.FieldType === 'dropdown' || field.FieldType === 'checklist') {
                    const options = JSON.parse(field.Options || '[]');
                    options.forEach(option => addOptionInput(option));
                }

                updateVisibility();
                buildPreview();
                modalInstance.show();
            }

            function showError(message) {
                alert('Error: ' + message);
            }

            function onApiError(error) {
                console.error('API Error:', error);
                showError('An unexpected error occurred. Please try again.');
            }

            function escapeHtml(text) {
                const map = {
                    '&': '&amp;',
                    '<': '&lt;',
                    '>': '&gt;',
                    '"': '&quot;',
                    "'": '&#039;'
                };
                return text.replace(/[&<>"']/g, function (m) { return map[m]; });
            }
            refreshFieldsList();
        }
        function loadTemplateData(type) {
                const currentType = messageTypeDropdown.value;
                if (!currentType) return;
                const dataToSave = {
                    messageType: currentType,
                    triggerStatus: messageTypeConfig[currentType].status,
                    emailEnabled: enableEmail.checked,
                    emailContent: emailBody.value,
                    smsEnabled: enableSms.checked,
                    smsContent: smsBody.value,
                    ynEnabled: smsYN.checked,
                    yesResponse: yesResponseText.value,
                    noResponse: noResponseText.value,
                    sendFaId: sendFaIdCheck.checked
                };
               

                  $.ajax({
                type: "POST",
                url: "Settings.aspx/loadTemplateData",
                data: JSON.stringify({ communicationSettings: dataToSave }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                console.error(response.d);
                     if (response.d.emailEnabled == 'true') {
                        enableEmail.checked = true;
                    }
                    else {
                          enableEmail.checked = false;
                    }

                    emailBody.value = response.d.emailContent;
                    emailSubject.value = response.d.emailSubject;
                    if (response.d.smsEnabled == 'true') {
                        enableSms.checked = true;
                    }
                    else {
                          enableSms.checked = false;
                    }
                     
                        smsBody.value = response.d.smsContent;
                       smsYN.checked = mockData.ynEnabled;
                        sendFaIdCheck.checked = mockData.sendFaId;
                        emailBody.dispatchEvent(new Event('input'));
                        smsBody.dispatchEvent(new Event('input'));
                    smsYN.dispatchEvent(new Event('change'));

                    
                },
              error: function(jqXHR, textStatus, errorThrown) {
                        // Handle an error response
                        console.error("AJAX Error Details:");
                        console.log("Status Code:", jqXHR.status); // e.g., 400, 500
                        console.log("Status Text:", textStatus); // e.g., "error"
                        console.log("Error Thrown:", errorThrown); // e.g., "Bad Request"
                        console.log("Server Response Text:", jqXHR.responseText); // The actual message from the server

                        // You can display this information to the user or log it for debugging
                        alert("An error occurred: " + jqXHR.status + " " + errorThrown);
                    }
                  });

            }
         function SaveMessageTemplates() {
               const currentType = messageTypeDropdown.value;
                if (!currentType) return;
                const dataToSave = {
                    messageType: currentType,
                    triggerStatus: messageTypeConfig[currentType].status,
                    emailEnabled: enableEmail.checked,
                    emailContent: emailBody.value,
                    emailSubject: emailSubject.value,
                    smsEnabled: enableSms.checked,
                    smsContent: smsBody.value,
                    ynEnabled: smsYN.checked,
                    yesResponse: yesResponseText.value,
                    noResponse: noResponseText.value,
                    sendFaId: sendFaIdCheck.checked
             };
            
                 console.log('dataToSave' + dataToSave);
                 $.ajax({
                type: "POST",
                url: "Settings.aspx/Save_TPMCommunicationSettings",
                data: JSON.stringify({ communicationSettings: dataToSave }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                  
                        alert('Data Saved Successfully.');
                   
                },
              error: function(jqXHR, textStatus, errorThrown) {
                        // Handle an error response
                        console.error("AJAX Error Details:");
                        console.log("Status Code:", jqXHR.status); // e.g., 400, 500
                        console.log("Status Text:", textStatus); // e.g., "error"
                        console.log("Error Thrown:", errorThrown); // e.g., "Bad Request"
                        console.log("Server Response Text:", jqXHR.responseText); // The actual message from the server

                        // You can display this information to the user or log it for debugging
                       // alert("An error occurred: " + jqXHR.status + " " + errorThrown);
                    }
                 });
            }
    </script>

</asp:Content>

