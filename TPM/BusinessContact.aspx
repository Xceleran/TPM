<%@ Page Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="BusinessContact.aspx.cs" Inherits="TPM.BusinessContact" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/css/bootstrap-select.css" />
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/js/bootstrap-select.min.js"></script>
    <style>
        .datatableButton, .dt-down-arrow {
            background-color: #4d78b1 !important; /* Green */
            color: white !important;
        }

        .custom-file {
            position: relative;
            font-family: arial;
            overflow: hidden;
            margin-bottom: 2px;
            width: auto;
            display: inline-block;
            padding: 5px;
        }

        .custom-file-input {
            position: absolute;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            cursor: pointer;
            opacity: 0;
            z-index: 100;
        }

        .custom-file img {
            display: inline-block;
            vertical-align: middle;
            margin-right: 5px;
        }

        ul.file-list {
            font-family: arial;
            list-style: none;
            padding: 0;
        }

            ul.file-list li {
                border-bottom: 1px solid #ddd;
                padding: 5px;
            }

        .remove-list {
            cursor: pointer;
            margin-left: 10px;
            color: red;
        }

        .bc-page .form-label {
            display: block;
            font-size: .8125rem;
            font-weight: 600;
            color: #5b6472;
            margin-bottom: .25rem;
        }

        .bc-page .bc-required::after {
            content: " *";
            color: #dc3545;
        }

        .bc-page .form-control,
        .bc-page .form-select,
        .bc-page .bootstrap-select > .dropdown-toggle {
            height: 40px;
            padding-top: .5rem;
            padding-bottom: .5rem;
            padding-left: .75rem;
            font-size: .9375rem;
            line-height: 1.5;
            color: #1f2937;
            background-color: #fff;
            border: 1px solid #d4dae3;
            border-radius: 8px;
            box-shadow: none;
            transition: border-color .15s, box-shadow .15s;
        }

        .bc-page .bootstrap-select.form-control {
            height: auto;
            padding: 0;
            border: 0;
            background: transparent;
            box-shadow: none;
        }

            .bc-page .bootstrap-select.form-control > .dropdown-toggle {
                width: 100%;
            }

        .bc-page textarea.form-control {
            height: auto;
            resize: vertical;
        }

        .bc-page .form-control::placeholder {
            color: #9ca3af;
            opacity: 1;
        }

        .bc-page .form-control:focus,
        .bc-page .form-select:focus,
        .bc-page .bootstrap-select > .dropdown-toggle:focus {
            border-color: #4d78b1;
            box-shadow: 0 0 0 3px rgba(77, 120, 177, .18);
            outline: 0 !important;
        }

        .bc-page .form-control[readonly] {
            background-color: #f3f5f8;
            color: #6b7280;
        }

        .bc-page .bootstrap-select > .dropdown-toggle.bs-placeholder,
        .bc-page .bootstrap-select > .dropdown-toggle.bs-placeholder:hover {
            color: #9ca3af;
        }

        [data-theme="dark"] .bc-page .form-control,
        [data-theme="dark"] .bc-page .form-select,
        [data-theme="dark"] .bc-page .bootstrap-select > .dropdown-toggle {
            color: var(--bs-body-color);
            background-color: var(--bs-tertiary-bg);
            border-color: var(--bs-border-color);
        }

        .bc-page .nav-tabs .nav-link.disabled {
            color: #9ca3af;
            cursor: not-allowed;
        }

        .bc-form-footer {
            display: flex;
            flex-wrap: wrap;
            justify-content: flex-end;
            gap: 8px;
            padding-top: 16px;
            margin-top: 8px;
           
        }

        .bc-page .btn {
            font-family: inherit;
        }

        .bc-page .card {
            border-radius: 10px;
        }

        [data-theme="dark"] .bc-page .card {
            background: var(--bs-body-bg);
            color: var(--bs-body-color);
        }

        [data-theme="dark"] .bc-page .nav-tabs .nav-link.active {
            background: var(--bs-body-bg);
            color: var(--bs-body-color);
            border-color: var(--bs-border-color) var(--bs-border-color) var(--bs-body-bg);
        }

        [data-theme="dark"] .bc-page .form-label,
        [data-theme="dark"] .bc-stat-label {
            color: #c4cad4;
        }

        .bc-header {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 12px 16px;
        }

        .bc-avatar {
            flex: 0 0 48px;
            height: 48px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 10px;
            background: #e8eef7;
            color: #4d78b1;
            font-size: 1.25rem;
        }

        .bc-title {
            flex: 1 1 220px;
            min-width: 0;
        }

            .bc-title h2 {
                font-size: 1.375rem;
                font-weight: 600;
                margin: 0;
                overflow-wrap: anywhere;
            }

        .bc-actions {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
        }

        .bc-stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 12px;
            margin-top: 16px;
        }

        .bc-stat {
            border: 1px solid var(--bs-border-color);
            border-radius: 8px;
            padding: 10px 14px;
        }

        .bc-stat-label {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: .8125rem;
            color: #5b6472;
        }

            .bc-stat-label .badge:empty {
                display: none;
            }

        .bc-stat-value {
            display: flex;
            align-items: baseline;
            font-size: 1.25rem;
            font-weight: 600;
        }

            .bc-stat-value input {
                flex: 1;
                min-width: 0;
                padding: 0;
                border: 0;
                outline: 0;
                background: transparent;
                color: inherit;
                font: inherit;
            }

        .bc-tab-content .tab-pane {
            display: none;
        }

            .bc-tab-content .tab-pane.active {
                display: block;
            }

        .bc-section + .bc-section {
            margin-top: 20px;
        }

        .bc-section-title {
            font-size: .75rem;
            font-weight: 700;
            letter-spacing: .05em;
            text-transform: uppercase;
            color: #4d78b1;
            padding-bottom: 6px;
            margin-bottom: 12px;
        }

        .bc-section .row > [class*="col-"] {
            margin-bottom: 12px;
        }

        .bc-pane-header {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: space-between;
            gap: 8px;
            margin-bottom: 12px;
        }

            .bc-pane-header h5 {
                margin: 0;
            }

        .dataTables_scrollBody {
            min-height: 400px;
        }

        #example .dropdown {
            position: inherit !important;
        }
    </style>

    <div class="main-content bc-page">
                <asp:HiddenField ID="hdCompanyName"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="hdCompanyTag"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="BusinessID" ClientIDMode ="Static" runat="server" />
                <asp:HiddenField ID="hf_BusinessGuid"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="PrimaryCustomerid"   ClientIDMode ="Static" Value="0" runat="server" />
                <asp:HiddenField ID="BusinessGuID"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="hdMode" ClientIDMode ="Static"    runat="server" />
                <asp:HiddenField ID="hdCustomerGUID" ClientIDMode ="Static"   runat="server" />
                <asp:HiddenField ID="RetVal" ClientIDMode ="Static"   runat="server" />

        <div class="card mb-3">
            <div class="card-body">
                <div class="bc-header">
                    <div class="bc-avatar"><i class="fa fa-building" aria-hidden="true"></i></div>
                    <div class="bc-title">
                        <h2 id="bcHeaderName">New Business Contact</h2>
                        <div id="bcHeaderSub" class="text-muted small">Business Contact</div>
                    </div>
                    <div class="bc-actions">
                        <div id="div_More" runat="server" class="dropdown">
                            <button type="button" class="btn btn-outline-primary dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">
                                More
                            </button>
                            <div class="dropdown-menu dropdown-menu-end">
                                            <a class="dropdown-item" href="#" onclick="return AddLinkedContact();">Add Link Contact</a>
                                            <a class="dropdown-item" href="#" onclick="return CreateProposal('Invoice');">Create Invoice</a>
                                            <a class="dropdown-item" href="#" onclick="return ViewInvoiceList('Invoice');">View Invoice</a>
                                            <a class="dropdown-item" href="#" onclick="return CreateProposal('Proposal');">Create Estimate</a>
                                            <a class="dropdown-item" href="#" onclick="return ViewInvoiceList('Proposal');">View Estimate</a>
                                            <a class="dropdown-item" href="#" onclick="return CreateAppt();">Create Appointment</a>
                                            <a class="dropdown-item" href="#" onclick="return ViewAppointment();">View Appointment</a>
                                            <a class="dropdown-item" href="#" onclick="return ViewFiles();">View Files</a>
                                            <a class="dropdown-item" href="#" onclick="return ViewEmailHistoryList();">View Email History</a>
                                            <a class="dropdown-item" href="#" onclick="return addProject();">Add Project</a>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="bc-stats d-none" id="bcStats">
                    <div class="bc-stat">
                        <div class="bc-stat-label">Invoices Due <span id="TotalInvoice" runat="server" class="badge bg-secondary"></span></div>
                        <div class="bc-stat-value">$<input type="text" placeholder="0.00" name="TotalDueForInvoice" id="TotalDueForInvoice" runat="server" readonly aria-label="Total due for invoices"></div>
                    </div>
                    <div class="bc-stat">
                        <div class="bc-stat-label">Estimates Due <span id="TotalEstimate" runat="server" class="badge bg-secondary"></span></div>
                        <div class="bc-stat-value">$<input type="text" placeholder="0.00" name="TotalDueForEstimate" id="TotalDueForEstimate" runat="server" readonly aria-label="Total due for estimates"></div>
                    </div>
                    <div class="bc-stat">
                        <div class="bc-stat-label">Total Appointments</div>
                        <div class="bc-stat-value"><input type="text" placeholder="0" name="TotalAppoinment" id="TotalAppoinment" runat="server" readonly aria-label="Total appointments"></div>
                    </div>
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-body">
                <ul class="nav nav-tabs" id="bcTabs" role="tablist">
                    <li class="nav-item" role="presentation">
                        <button class="nav-link active" id="bc-details-tab" data-bs-toggle="tab" data-bs-target="#bc-details" type="button" role="tab" aria-controls="bc-details" aria-selected="true">Details</button>
                    </li>
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="bc-linked-tab" data-bs-toggle="tab" data-bs-target="#bc-linked" type="button" role="tab" aria-controls="bc-linked" aria-selected="false">Linked Contacts</button>
                    </li>
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="bc-invoices-tab" data-bs-toggle="tab" data-bs-target="#bc-invoices" type="button" role="tab" aria-controls="bc-invoices" aria-selected="false">Invoices</button>
                    </li>
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="bc-files-tab" data-bs-toggle="tab" data-bs-target="#bc-files" type="button" role="tab" aria-controls="bc-files" aria-selected="false">Files &amp; Communications</button>
                    </li>
                </ul>

                <div class="tab-content bc-tab-content pt-3" id="bcTabContent">
                    <div class="tab-pane active" id="bc-details" role="tabpanel" aria-labelledby="bc-details-tab">
                        <div class="row">
                            <div class="col-lg-6">
                                <div class="bc-section">
                                    <div class="bc-section-title">Business</div>
                                    <%--Nizam--%>
                                    <div class="row" id="RM_details" runat="server">
                                        <div class="col-md-6">
                                            <label for="txt_rmid" class="form-label">AM ID</label>
                                            <input type="text" name="txt_rmid" id="txt_rmid" class="form-control" runat="server" readonly>
                                        </div>
                                        <div class="col-md-6">
                                            <label for="txt_custcode" class="form-label">Customer Code</label>
                                            <input type="text" name="txt_custcode" id="txt_custcode" class="form-control" runat="server" readonly>
                                        </div>
                                    </div>
                                    <div class="row">
                                        <div class="col-12">
                                            <label class="form-label bc-required">Business Name</label>
                                            <input type="text" name="txt_BusinessName" id="txt_BusinessName" class="form-control" runat="server">
                                        </div>
                                        <div class="col-12">
                                            <label class="form-label">Tag</label>
                                            <select id="ddlTag" runat="server" title="Select Tags" style="width: 100%" class="form-control selectpicker" multiple="true" data-live-search="true">
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="bc-section">
                                    <div class="bc-section-title">Primary Contact</div>
                                    <div class="row">
                                        <div class="col-md-3">
                                            <label class="form-label">Title</label>
                                            <asp:TextBox ID="txt_Title" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                        <div class="col-md-5">
                                            <label class="form-label bc-required">First Name</label>
                                            <input type="text" name="txt_FirstName" id="txt_FirstName" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">Last Name</label>
                                            <input type="text" name="txt_LastName" id="txt_LastName" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-6">
                                            <label class="form-label">Main Phone</label>
                                            <input type="text" placeholder="(xxx)xxx-xxxx" name="phone" id="phone" maxlength="15" onkeypress="return isNumberKey(event)" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-6">
                                            <label class="form-label">Mobile</label>
                                            <input type="text" placeholder="(xxx)xxx-xxxx" name="mobile" id="mobile" maxlength="15" onkeypress="return isNumberKey(event)" class="form-control" runat="server">
                                        </div>
                                        <div class="col-12">
                                            <label class="form-label bc-required">Email</label>
                                            <input type="email" placeholder="email@example.com" required="required" name="email" id="email" class="form-control" runat="server">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-lg-6">
                                <div class="bc-section">
                                    <div class="bc-section-title">Address</div>
                                    <div class="row">
                                        <div class="col-12">
                                            <label class="form-label bc-required">Address 1</label>
                                            <input type="text" name="address1" id="address1" class="form-control" runat="server">
                                        </div>
                                        <div class="col-12">
                                            <label class="form-label">Address 2</label>
                                            <input type="text" name="address2" class="form-control" id="address2" runat="server">
                                        </div>
                                        <div class="col-sm-6">
                                            <label class="form-label bc-required">City</label>
                                            <input type="text" placeholder="city name" name="city" id="city" class="form-control" runat="server">
                                        </div>
                                        <div class="col-sm-6" id="div_country" runat="server">
                                            <label for="country" class="form-label">Country</label>
                                            <select name="country" class="form-select" id="country" runat="server" onchange="toggleStateProvince(true)">
                                                   <option value="Canada">Canada</option>
                                                <option value="USA">USA</option>
                                             
                                            </select>
                                        </div>
                                        <div class="col-sm-6" id="div_state" runat="server">
                                            <label for="" id="lbl_state" runat="server" class="form-label bc-required">State</label>
                                            <select name="state" class="form-select" id="state" runat="server">
                                                <option value="select">Select State</option>
                                                <option value="AL">Alabama</option>
                                                <option value="AK">Alaska</option>
                                                <option value="AZ">Arizona</option>
                                                <option value="AR">Arkansas</option>
                                                <option value="CA">California</option>
                                                <option value="CO">Colorado</option>
                                                <option value="CT">Connecticut</option>
                                                <option value="DE">Delaware</option>
                                                <option value="DC">District of Columbia</option>
                                                <option value="FL">Florida</option>
                                                <option value="GA">Georgia</option>
                                                <option value="HI">Hawaii</option>
                                                <option value="ID">Idaho</option>
                                                <option value="IL">Illinois</option>
                                                <option value="IN">Indiana</option>
                                                <option value="IA">Iowa</option>
                                                <option value="KS">Kansas</option>
                                                <option value="KY">Kentucky</option>
                                                <option value="LA">Louisiana</option>
                                                <option value="ME">Maine</option>
                                                <option value="MD">Maryland</option>
                                                <option value="MA">Massachusetts</option>
                                                <option value="MI">Michigan</option>
                                                <option value="MN">Minnesota</option>
                                                <option value="MS">Mississippi</option>
                                                <option value="MO">Missouri</option>
                                                <option value="MT">Montana</option>
                                                <option value="NE">Nebraska</option>
                                                <option value="NV">Nevada</option>
                                                <option value="NH">New Hampshire</option>
                                                <option value="NJ">New Jersey</option>
                                                <option value="NM">New Mexico</option>
                                                <option value="NY">New York</option>
                                                <option value="NC">North Carolina</option>
                                                <option value="ND">North Dakota</option>
                                                <option value="OH">Ohio</option>
                                                <option value="OK">Oklahoma</option>
                                                <option value="OR">Oregon</option>
                                                <option value="PA">Pennsylvania</option>
                                                <option value="RI">Rhode Island</option>
                                                <option value="SC">South Carolina</option>
                                                <option value="SD">South Dakota</option>
                                                <option value="TN">Tennessee</option>
                                                <option value="TX">Texas</option>
                                                <option value="UT">Utah</option>
                                                <option value="VT">Vermont</option>
                                                <option value="VA">Virginia</option>
                                                <option value="WA">Washington</option>
                                                <option value="WV">West Virginia</option>
                                                <option value="WI">Wisconsin</option>
                                                <option value="WY">Wyoming</option>
                                            </select>
                                        </div>
                                        <div class="col-sm-6" id="div_province" runat="server">
                                            <label for="province" id="lbl_province" runat="server" class="form-label bc-required">Province</label>
                                            <select name="province" class="form-select" id="province" runat="server">
                                                <option value="select">Select Province</option>
                                                <option value="Alberta">Alberta</option>
                                                <option value="British Columbia">British Columbia</option>
                                                <option value="Manitoba">Manitoba</option>
                                                <option value="New Brunswick">New Brunswick</option>
                                                <option value="Newfoundland and Labrador">Newfoundland and Labrador</option>
                                                <option value="Northwest Territories">Northwest Territories</option>
                                                <option value="Nova Scotia">Nova Scotia</option>
                                                <option value="Nunavut">Nunavut</option>
                                                <option value="Ontario">Ontario</option>
                                                <option value="Prince Edward Island">Prince Edward Island</option>
                                                <option value="Quebec">Quebec</option>
                                                <option value="Saskatchewan">Saskatchewan</option>
                                                <option value="Yukon">Yukon</option>
                                            </select>
                                        </div>
                                        <div class="col-sm-6">
                                            <label for="zip" id="lb_zip" runat="server" class="form-label bc-required">Zip Code</label>
                                            <input type="text" placeholder="XXXXX" name="zip" maxlength="6" id="zip" class="form-control" runat="server">
                                        </div>
                                    </div>
                                </div>
                                <div class="bc-section">
                                    <div class="bc-section-title">Notes</div>
                                    <textarea name="txt_notes" class="form-control" id="txt_notes" runat="server" rows="6" cols="50" aria-label="Notes"></textarea>
                                </div>
                            </div>
                        </div>
                        <div class="bc-form-footer">
                            <input type="button" name="back" value="Back" class="btn btn-outline-secondary" onclick="BackClicked();">
                            <input type="button" runat="server" id="btn_Delete" name="btn_Delete" value="Delete" class="btn btn-outline-danger" onclick="DeleteCustomer();">
                            <asp:Button ID="btn_Save" runat="server" CssClass="btn btn-primary" Text="Save" OnClientClick="javascript:if(SaveCustomer()){return true;}else return false;" OnClick="btn_Save_Click" />
                        </div>
                    </div>

                    <div class="tab-pane" id="bc-linked" role="tabpanel" aria-labelledby="bc-linked-tab">
                        <div class="bc-pane-header">
                            <h5>Linked Contacts</h5>
                            <button type="button" class="btn btn-sm btn-primary" onclick="return AddLinkedContact();">Add Link Contact</button>
                        </div>
                        <div class="table-responsive">
                            <asp:PlaceHolder ID="ListTable" runat="server"></asp:PlaceHolder>
                        </div>
                        <p id="bcLinkedEmpty" class="text-muted text-center py-4 mb-0 d-none">No linked contacts yet.</p>
                    </div>

                    <div id="div_details" runat="server">
                <div class="accordion" id="accordionExample">
                    <div class="card d-none">
                        <div class="card-header" id="headingOne">
                            <div style="margin-top: 25px;" class="float-start mb-0">
                                <h5 class="mb-0">Current Project Status</h5>
                            </div>
                            <div class="float-end">
                                <div style="width: 440px; height: 20px; margin: 0 auto; position: relative; margin-top: 25px;">
                                    <div style="position: absolute; right: 0; height: 20px; width: 360px;">
                                        <h6 class="mb-0" id="div_projectStatus" runat="server">Current Status</h6>
                                    </div>
                                    <div style="position: absolute; right: 10px; height: 30px; width: 40px;">
                                        <button class="btn btn-link collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseOne" aria-expanded="false" aria-controls="collapseOne">
                                            <i class="fa fa-arrow-circle-down" aria-hidden="true"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div id="collapseOne" class="collapse" aria-labelledby="headingOne" data-parent="#accordionExample">
                            <div class="card-body">
                                <div id="div_projectDeatils" runat="server" class="card-body">
                                    <div class="row mt-2">

                                        <div class="col-md-3 mt-2 " id="LeadSourceDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_LeadSource"></label>
                                            <div class="input-group mb-0">
                                                <select name="leadSourceStatusDropdown" class="form-select" id="leadSourceStatusDropdown" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>
                                        <div class="col-md-3 mt-2" id="LeadTypeDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_LeadType"></label>
                                            <div class="input-group mb-0">
                                                <select name="leadTypeDropdown" class="form-select" id="leadTypeDropdown" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>
                                        <div class="col-md-3 mt-2" id="SalesReqDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_SalesRep"></label>
                                            <div class="input-group mb-0">
                                                <select name="SalesRep" class="form-select" id="SalesRep" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>

                                        <div class="col-md-3 mt-2" id="SalesStatusDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_SalesStatus"></label>
                                            <div class="input-group mb-0">
                                                <select name="salesStatusDropdown" class="form-select" id="salesStatusDropdown" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>
                                        <%--</div>
                                    <div class="row mt-2">--%>
                                        <div class="col-md-3 mt-2" id="ProjectTypeDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_ProjectType"></label>
                                            <div class="input-group mb-0">
                                                <select name="projectTypeDropdown" class="form-select" id="projectTypeDropdown" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>
                                        <div class="col-md-3 mt-2" id="ProjectStatusDiv">
                                            <label for="validationCustom01" class="form-label mb-0 w-100" id="hdr_ProjectStatusList"></label>
                                            <div class="input-group mb-0">
                                                <select name="projectStatus" class="form-select" id="projectStatus" runat="server">
                                                    <option value="select">Select</option>
                                                    <option value="0">Marketing</option>
                                                </select>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                        <div class="tab-pane" id="bc-invoices" role="tabpanel" aria-labelledby="bc-invoices-tab">
                            <div class="spinner-border m-5" id="spinner_ProjectList" role="status" style="display: none;">
                                <span class="sr-only">Loading...</span>
                            </div>
                            <div class="table-responsive">
                                <table id="table_ProjectList" class="table table-striped table-bordered" style="width: 100%">
                                    <thead>
                                        <tr>
                                            <th>Number</th>
                                            <th>Type</th>
                                            <th>Date</th>
                                            <th>Total</th>
                                            <th>Due</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                    </tbody>
                                    <tfoot>
                                    </tfoot>
                                </table>
                            </div>
                        </div>

                        <div class="tab-pane" id="bc-files" role="tabpanel" aria-labelledby="bc-files-tab">
                            <div class="spinner-border m-5" id="spinner_CurrentProject" role="status" style="display: none;">
                                <span class="sr-only">Loading...</span>
                            </div>
                            <div class="table-responsive">
                                <table id="currentProjectTable" class="table table-striped table-bordered" style="width: 100%;">
                                    <thead class="thead-light">
                                        <tr>
                                            <th>Kind</th>
                                            <th>Title</th>
                                            <th>Detail</th>
                                            <th>Date</th>
                                            <th>Status</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script type="text/javascript">
        $(document).ready(function () {
          
            toggleStateProvince(false);
        });

        function toggleStateProvince(resetValue = true) {
            var country = document.getElementById('<%=country.ClientID%>');
            var state = document.getElementById('<%=state.ClientID%>');
            var province = document.getElementById('<%=province.ClientID%>');
            var zip = document.getElementById('<%=zip.ClientID%>');

            var divCountry = document.getElementById('<%=div_country.ClientID%>');
            var divState = document.getElementById('<%=div_state.ClientID%>');
            var divProvince = document.getElementById('<%=div_province.ClientID%>');
            var lblState = document.getElementById('<%=lbl_state.ClientID%>');
            var lblProvince = document.getElementById('<%=lbl_province.ClientID%>');
            var lbZip = document.getElementById('<%=lb_zip.ClientID%>');

            if (resetValue) {
                $(state).val("select");
                $(province).val("select");
                $(zip).val("");
            }

            if (divCountry) {
                if (country.value === "Canada") {
                    divState.style.display = 'none';
                    divProvince.style.display = 'block';
                    lblProvince.innerText = "Province";
                    lbZip.innerText = "Postal Code";
                } else {
                    divState.style.display = 'block';
                    divProvince.style.display = 'none';
                    lblState.innerText = "State";
                    lbZip.innerText = "Zip Code";
                }
            }
        }
        

        function initializeSelectPicker() {
            try {
                var $ddlTag = $('#<%= ddlTag.ClientID %>');
                if ($ddlTag.length > 0) {
                    console.log('Initializing selectpicker for ddlTag, options count: ' + $ddlTag.find('option').length);
                    
                    // Destroy existing selectpicker if it exists (check for bootstrap-select wrapper)
                    if ($ddlTag.next('.bootstrap-select').length > 0) {
                        try {
                            $ddlTag.selectpicker('destroy');
                        } catch (e) {
                            console.log('Error destroying selectpicker: ' + e.message);
                        }
                    }
                    
                    // Small delay to ensure DOM is ready
                    setTimeout(function() {
                        // Re-initialize selectpicker
                        $ddlTag.selectpicker({
                            noneSelectedText: 'Select Tags',
                            selectAllText: 'Select All',
                            deselectAllText: 'Deselect All'
                        });
                        
                        // Refresh to show any server-side selected values and loaded options
                        $ddlTag.selectpicker('refresh');
                        console.log('Selectpicker initialized and refreshed');
                    }, 100);
                } else {
                    console.log('ddlTag element not found');
                }
            } catch (e) {
                console.error('Error in initializeSelectPicker: ' + e.message);
            }
        }

        $(document).ready(function () {
            // Initialize selectpicker for ddlTag
            initializeSelectPicker();
        });
    </script>
    <script src="Scripts/BusinessContact.js?v=14" type="text/javascript"></script>
   
</asp:Content>