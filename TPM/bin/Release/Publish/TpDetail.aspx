<%@ Page Title="" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="TpDetail.aspx.cs" Inherits="TPM.TpDetail" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
      <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/css/bootstrap-select.css" />
 <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/js/bootstrap-select.min.js"></script>
    <style>
        #modal,
        .modal {
            z-index: 2000;
        }

        .isAcChk {
            margin-top: 12px;
        }

        .custom-file {
            position: relative;
            font-family: arial;
            overflow: hidden;
            margin-bottom: 2px;
            width: auto;
            display: inline-block;
            padding: 2px;
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

        .form-label {
            float: left;
            width: 20%;
        }

        .form-labelTotal {
            float: left;
            width: 55%;
        }

        .dataTables_scrollBody {
            min-height: 400px;
        }

        #example {
            border-bottom: 1px solid #ccc !important;
        }

        .wisetack {
            background-color: #07c0ca;
            color: white;
            cursor: not-allowed
        }

        .disabled {
            opacity: 0.5; /* Reduce opacity to make it appear disabled */
            pointer-events: none; /* Prevent interactions with the div */
        }

        .table-loan td label {
            padding: 0px;
        }

        .table-loan td label {
            padding: 0px;
        }

        .table-loan td {
            border: 0px solid #fff;
            padding: 12px 8px;
        }

        .semi-circle {
            width: 100%;
            height: 80px;
            background-color: #265598;
            border-radius: 0 0 6rem 6rem;
        }
        /* General styling for note sections within the modal */
.form-section {
    background-color: #f8f9fa;
    padding: 20px;
    border-radius: 8px;
    margin-bottom: 20px;
}

    .form-section.highlight {
        background-color: #fff3cd;
        border: 1px solid #ffeaa7;
    }

/* For the custom tag input dropdown */
.tag-input-container {
    position: relative;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    padding: 5px;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    background-color: white;
    cursor: text;
}

.tag-input {
    flex-grow: 1;
    border: none;
    outline: none;
    padding: 5px;
    min-width: 120px;
}

.tag {
    display: inline-flex;
    align-items: center;
    background-color: #e9ecef;
    padding: 4px 12px;
    margin: 2px;
    border-radius: 20px;
    font-size: 0.875em;
    color: #495057;
    white-space: nowrap;
}

    .tag i {
        margin-left: 8px;
        cursor: pointer;
        color: #6c757d;
        font-size: 1em;
    }

        .tag i:hover {
            color: #dc3545;
        }

.dropdown-menu {
    width: 100%;
}
    </style>
    <div class="col-lg-12">
        <div class="row">
            <div class="row">
                <div class="col-12 mb-3">
                    <div class="card card-custom gutter-b card-stretch p-3">
                        <div class="card-body">
                            <div class="row">
                                <%--<form name="frmCustomer" id="frmCustomer" method="post" action="CustomerProcessor.asp">--%>
                                <%-- <input type="hidden" id="hdCompanyID" class="form-control" name="hdCompanyID" value="<%=CompanyID %>" />
                                    <input type="hidden" id="CustomerID" class="form-control" name="CustomerID" value="<%=CustomerID %>" />
                                    <input type="hidden" id="hdMode" class="form-control" name="hdMode" value="" />
                                     <input type="hidden" id="hdCustomerGUID" name="hdCustomerGUID" value="" />
                                --%>

                                <asp:HiddenField ID="hdCompanyName" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="hdCompanyTag" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="CustomerID" ClientIDMode="Static" runat="server" />
                                <input type="number" id="SiteId" value="0"  hidden="hidden" />
                                <input type="text" id="BillAddressId" hidden="hidden" />
                                <asp:HiddenField ID="hdMode" runat="server" />
                                <asp:HiddenField ID="hdCustomerGUID" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="Job_TitleList" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="RetVal" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="BusinessID" ClientIDMode="Static" runat="server" />
                                <asp:HiddenField ID="hdSrc" ClientIDMode="Static" runat="server" />
                                <div class="col-12">

                                    <div class="row">
                                        <div class="col-md-2 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Title</label>
                                            <asp:TextBox ID="txt_title" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                        <div class="col-md-5 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">First Name</label>
                                            <input type="text" name="fname" id="fname" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-5 mt-0">
                                            <label for="" class="form-label mb-0">Last Name</label>
                                            <input type="text" name="lname" id="lname" class="form-control" runat="server">
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-6 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">
                                                Job Title
                                            </label>
                                            <asp:TextBox ID="txt_JobTitle" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                        <div class="col-md-6 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">
                                                Company Name
                                            </label>
                                            <asp:TextBox ID="txt_CompanyName" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-3 mt-0">
                                            <label class="form-label mb-0">Tag</label>
                                                                <select id="ddlTag" runat="server" title="Select Tags" style="width: 100%" class="form-control selectpicker" multiple="true" data-live-search="true">
	                                                            </select>
                                           <%-- <asp:DropDownList
                                                ID="ddlTag"
                                                runat="server"
                                                CssClass="form-control">
                                            </asp:DropDownList>--%>
                                        </div>
                                        <div class="col-md-9  mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Address</label>
                                            <input type="text" name="address1" id="address1" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-6 mt-0" style="display: none">
                                            <label for="" class="form-label mb-0">Address Line 2</label>
                                            <input type="text" name="address2" class="form-control" id="address2" runat="server">
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">City</label>
                                            <input type="text" placeholder="city name" name="city" id="city" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-4 mt-0" id="div_country" runat="server">
                                            <label for="country" class="form-label mb-0">Country</label>
                                            <select name="country" class="form-select" id="country" runat="server" onchange="toggleStateProvince(true)">
                                                <option value="Canada">Canada</option>
                                                <option value="USA">USA</option>

                                            </select>
                                        </div>
                                        <div class="col-md-4 mt-0" id="div_state" runat="server">
                                            <label for="" id="lbl_state" runat="server" class="form-label mb-0">State</label>
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

                                        <%--  <script>
		                                    var optstate = document.getElementById("state");
		                                    optstate.value = '<%=State%>';
                                            </script>--%>
                                        <div class="col-md-4 mt-0" id="div_province" runat="server">
                                            <label for="province" id="lbl_province" runat="server" class="form-label mb-0">Province</label>
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
                                        <div class="col-md-4 mt-0">
                                            <label for="zip" id="lb_zip" runat="server" class="form-label mb-0">Zip Code</label>
                                            <input type="text" placeholder="XXXXX" name="zip" maxlength="6" id="zip" class="form-control" runat="server">
                                        </div>
                                    </div>

                                    <div class="row mt-2">
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Phone</label>
                                            <input type="text" placeholder="(xxx)xxx-xxxx" name="phone" id="phone" maxlength="15" onkeypress="return isNumberKey(event)" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            <label for="" class="form-label mb-0">Mobile</label>
                                            <input type="text" placeholder="(xxx)xxx-xxxx" name="mobile" id="mobile" maxlength="15" onkeypress="return isNumberKey(event)" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            <label for="" class="form-label mb-0">Email</label>
                                            <input type="email" placeholder="email@example.com" required="required" name="email" id="email" class="form-control" runat="server">
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-labelTotal mb-0">Total Due (For <span id="TotalInvoice" runat="server"></span>Invoice)</label>
                                            <div class="input-group mb-3">
                                                <div class="input-group-prepend">
                                                    <span class="input-group-text">$</span>
                                                </div>
                                                <input type="text" placeholder="0.00" name="TotalDueForInvoice" id="TotalDueForInvoice" class="form-control" runat="server" readonly>
                                            </div>
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-labelTotal mb-0">Total Due (For <span id="TotalEstimate" runat="server"></span>Estimate)</label>
                                            <div class="input-group mb-3">
                                                <div class="input-group-prepend">
                                                    <span class="input-group-text">$</span>
                                                </div>
                                                <input type="text" placeholder="0.00" name="TotalDueForEstimate" id="TotalDueForEstimate" class="form-control" runat="server" readonly>
                                            </div>
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-labelTotal mb-0">Total Appointment</label>
                                            <div class="input-group mb-3">
                                                <div class="input-group-prepend">
                                                    <span class="input-group-text">&nbsp;</span>
                                                </div>
                                                <input type="text" name="TotalAppoinment" id="TotalAppoinment" class="form-control" runat="server" readonly>
                                            </div>
                                        </div>
                                    </div>
                                    <div id="div_Business" runat="server" style="display:none;" class="row mt-2">
                                        <div class="col-md-4 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Business :</label>
                                            <div class="input-group mb-3">
                                                <select name="se_Business" class="form-select" id="se_Business" runat="server">
                                                    <option value="0">None</option>
                                                </select>
                                            </div>
                                            <small id="div_PrimaryContact" runat="server" class="text-black-50 mb-3">This is primary contact.Dont change it.</small>
                                        </div>
                                    </div>



                                    <div class="row mt-2" style="display: none;">
                                        <div class="col-md-12 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Notes:</label>
                                            <textarea name="txt_notes" class="form-control" id="txt_notes" runat="server" rows="4" cols="50"></textarea>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-lg-12 mt-3">

                                    <hr />
                                    <div class="d-flex justify-content-start">
                                    </div>
                                    <div class="d-flex justify-content-end">
                                        <div class="dropdown">
                                            <button class="btn btn-secondary dropdown-toggle" type="button" id="dropdownMenuButton" data-bs-toggle='dropdown' aria-expanded='false'>
                                                More
                                            </button>
                                            <ul class="dropdown-menu" aria-labelledby="dropdownMenuButton">

                                                <li><a class="dropdown-item" href="#" onclick="CreateProposal('Invoice');">Create Invoice</a>     </li>
                                                <li><a class="dropdown-item" href="#" onclick="CreateProposal( 'Proposal');">Create Estimate</a>  </li>
                                                <li><a class="dropdown-item" href="#" onclick="CreateAppt();">Create Appointment</a>              </li>
                                                <li>
                                                    <hr class='dropdown-divider'>
                                                </li>
                                                <li><a class="dropdown-item" href="#" onclick="ViewInvoiceList('Invoice');">Invoices</a>   </li>
                                                <li><a class="dropdown-item" href="#" onclick="ViewInvoiceList( 'Proposal');">Estimates</a></li>
                                                <li>
                                                    <hr class='dropdown-divider'>
                                                </li>
                                                <li><a class="dropdown-item" href="#" onclick="ViewAppointment();">Appointments</a>        </li>
                                                <li><a class="dropdown-item" href="#" onclick="ViewFiles();">Files</a>                       </li>
                                                <li><a class="dropdown-item" href="#" onclick="ViewEmailHistoryList();">E-Mails</a>  </li>
                                                <li><a class="dropdown-item" href="#" onclick="OpenMailPopUp();">Send Email</a>  </li>
                                                <li><a class="dropdown-item" href="#" onclick="OpenSmartFormModal();">Create Form</a></li>
                                                <li>
                                                    <hr class='dropdown-divider'>
                                                </li>

                                                <li><a class="dropdown-item" href="#" onclick="OpenSMSHistory();">SMS Chat</a>  </li>
                                                <li><a class="dropdown-item" href="#" onclick="OpenAllHistory();">Text History</a>  </li>
                                                <li><a class="dropdown-item" href="#" onclick="OpenSMSPopUp();">Send Texts</a>  </li>
                                                <li id="btnMMS" runat="server" ><a class="dropdown-item" href="#" onclick="OpenMMSPopUp();">Send MMS</a>  </li>

                                            </ul>
                                        </div>
                                        &nbsp;
                                            <%--Nizam--%>
                                        <input type="button" name="back" value="Back" class="btn btn-secondary" onclick="history.back()">&nbsp;
                                        <%--<input type="button" name="back" value="Back" class="btn btn-secondary" onclick="BackClicked();">&nbsp;--%>

                                        <asp:Button ID="btn_Save" runat="server" CssClass="btn btn-primary" Text="Save" OnClientClick="javascript:if(SaveCustomer()){return true;}else return false;" OnClick="btn_Save_Click" />&nbsp;
										   <input type="button" runat="server" id="btn_delete" name="delete" value="Delete" class="btn btn-danger" onclick="DeleteCustomer();">
                                    </div>
                                </div>
                                <%--  </form>--%>
                            </div>
                        </div>
                    </div>
                </div>
               <div class="col-12 mb-3">
                 <div class="card card-custom mb-2" id="Div1" runat="server" style="text-align: left;">
                <div class="cust-section-block">
                    <div class="csl-header">
                        <h3 class="">Notes</h3>
                        <button type="button" class="btn btn-primary" id="addNote">+ Add Note</button>
                    </div>
                    <div class="note-section-content" id="addNoteContent">
                    </div>
                </div>
            </div>
            <div class="card card-custom mb-2" id="billingAddressLHG" runat="server" style="text-align: left;">
                <div class="cust-section-block">
                    <div class="csl-header">
                        <h3 class="">Billing Address</h3>
                        <button type="button" class="btn btn-primary" id="addAddress">+ Add Billing Address</button>
                    </div>
                    <div class="cust-section-content" id="custAddress">
                    </div>
                </div>
            </div>
            <!-- Site LHG Card Stars Here -->
            <div class="card card-custom" id="siteLHG" runat="server" style="text-align: left;">
                <div class="cust-section-block">
                    <div class="csl-header">
                        <h3 class="">Sites & Locations</h3>
                        <button type="button" class="btn btn-primary" id="addSiteBtn">+ Add Site</button>
                    </div>
                    <div class="cust-section-content" id="sites">
                        <%-- <div class="cust-site-card" data-site-id="1">
                                                <h3 class="cust-site-title">Main Residence</h3>
                                                <p class="cust-site-info" id="address-1">Address: 123 Elm St, City, ST 12345</p>
                                                <p class="cust-site-info">Contact: Jane Smith (555-987-6543)</p>
                                                <div class="cust-site-actions">
                                                    <button class="cust-site-edit-btn" data-site-id="1">Edit</button>
                                                    <a href="CustomerDetails.aspx?siteId=1" class="cust-site-view-link">View Details</a>
                                                </div>
                                            </div>
                                            <div class="cust-site-card" data-site-id="2">
                                                <h3 class="cust-site-title">Vacation Home</h3>
                                                <p class="cust-site-info" id="address-2">Address: 456 Oak Rd, City, ST 12345</p>
                                                <p class="cust-site-info">Contact: Bob Johnson (555-456-7890)</p>
                                                <div class="cust-site-actions">
                                                    <button class="cust-site-edit-btn" data-site-id="2">Edit</button>
                                                    <a href="CustomerDetails.aspx?siteId=2" class="cust-site-view-link">View Details</a>
                                                </div>--%>
                    </div>
                </div>
            </div>
                   </div>
            </div>

           
            <!-- Site LHG Card Ends Here -->
            <hr />
            <div id="div_details" runat="server" class="col-lg-12 mt-3">
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

                    <div class="card">
                        <div class="card-header" id="headingThree">
                            <div style="margin-top: 25px;" class="float-start mb-0">
                                <h5 class="mb-0">Current Project</h5>
                            </div>
                            <div class="float-end">
                                <div style="width: 440px; height: 20px; margin: 0 auto; position: relative; margin-top: 25px;">
                                    <div style="position: absolute; right: 10px; height: 30px; width: 40px;">
                                        <button onclick="LoadCurrentProject()" class="btn btn-link collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseThree" aria-expanded="false" aria-controls="collapseThree">
                                            <i class="fa fa-arrow-circle-down" aria-hidden="true"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div id="collapseThree" class="collapse" aria-labelledby="headingThree" data-parent="#accordionExample">
                            <div class="card-body">
                                <div class="row">
                                    <div class="col-12">
                                        <div class="spinner-border m-5" id="spinner_CurrentProject" role="status" style="display: none;">
                                            <span class="sr-only">Loading...</span>
                                        </div>
                                        <div class="table-responsive">
                                            <table id="currentProjectTable" class="table table-striped table-bordered" style="width: 100%;">
                                                <thead class="thead-light">
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

                    <hr />

                    <div class="card">
                        <div class="card-header" id="headingTwo">
                            <div style="margin-top: 25px;" class="float-start mb-0">
                                <h5 class="mb-0">Project List</h5>
                            </div>
                            <div class="float-end">
                                <div style="width: 440px; height: 20px; margin: 0 auto; position: relative; margin-top: 25px;">
                                    <div style="position: absolute; right: 10px; height: 30px; width: 40px;">
                                        <button onclick="LoadProjectList()" class="btn btn-link collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseTwo" aria-expanded="false" aria-controls="collapseTwo">
                                            <i class="fa fa-arrow-circle-down" aria-hidden="true"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div id="collapseTwo" class="collapse" aria-labelledby="headingTwo" data-parent="#accordionExample">
                            <div class="card-body">
                                <div class="row">
                                    <div class="spinner-border m-5" id="spinner_ProjectList" role="status">
                                        <span class="sr-only">Loading...</span>
                                    </div>
                                    <div class="col-12 table-responsive">
                                        <table id="table_ProjectList" class="table table-striped table-bordered" style="width: 100%">
                                            <thead>
                                                <tr>
                                                    <th>Project Id</th>
                                                    <th>Project Type</th>
                                                    <th>Project Status</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                            </tbody>
                                            <tfoot>
                                            </tfoot>
                                        </table>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Add Site Modal -->
        <div class="modal fade" id="addSiteModal">
            <div class="modal-dialog modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="CollectPaymentModalLabelNew">Add/Update Customer Site</h5>
                        <%--<button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">×</span>
                    </button>--%>
                    </div>
                    <div class="modal-body">

                        <div class="row mt-2">
                            <b style="text-align: center">Contact Person Information</b>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">First Name:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="cslFirstname" name="cslFirstname" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">Last Name:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="cslLastName" name="cslLastName" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">Country :</label>
                                <div class="col-sm-12">
                                    <select name="cslCountry" class="form-select" id="cslCountry">
                                        <option value="select">Select State</option>
                                        <option value="USA">USA</option>
                                        <option value="CANADA">CANADA</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2" id="cslStateDiv">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">State:</label>
                                <div class="col-sm-12">

                                    <select name="cslSate" class="form-select" id="cslSate">
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
                            </div>
                        </div>
                        <div class="row mt-2" id="cslProvenceDiv">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">Provence:</label>
                                <div class="col-sm-12">
                                    <select name="cslProvence" class="form-select" id="cslProvence">
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
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">Zip/Postal Code:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="cslZip" name="cslZip" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <b style="text-align: center">Site Information</b>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteName" style="float: left;" class="col-form-label">Site Name:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="siteName" name="siteName" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="address" style="float: left;" class="col-form-label">Address:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="address" name="address" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="siteContact" style="float: left;" class="col-form-label">Site Contact:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="siteContact" name="siteContact" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtEmail" style="float: left;" class="col-form-label">Email:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtEmail" name="txtEmail" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtPhoneNum" style="float: left;" class="col-form-label">Phone Number:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtPhoneNum" name="txtPhoneNum" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="note" style="float: left;" class="col-form-label">Note:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="note" name="note" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="isActive" style="float: left;" class="col-form-label">Active:</label>

                                <input type="checkbox" class="isAcChk" id="isActive" name="isActive" checked />
                            </div>

                        </div>
                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-secondary btn-block ml-1" data-dismiss="modal" id="closeAddSite">Cancel</button>
                        <button type="button" id="btnSaveSite" class="btn btn-primary text-nowrap" onclick="saveSite(event)">Add Site</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Add Customer Address -->
        <div class="modal fade" id="addAddressModal">
            <div class="modal-dialog modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="AddressModalLabelNew">Add/Update New Address</h5>
                        <%--<button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">×</span>
                    </button>--%>
                    </div>
                    <div class="modal-body">

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtAddress" style="float: left;" class="col-form-label">Address:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtAddress" name="txtAddress" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtCity" style="float: left;" class="col-form-label">City:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtCity" name="txtCity" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtZipCode" style="float: left;" class="col-form-label">Zip Code:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtZipCode" name="txtZipCode" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="txtState" style="float: left;" class="col-form-label">State:</label>
                                <div class="col-sm-12">
                                    <input type="text" id="txtState" name="txtState" class="form-control" />
                                </div>
                            </div>
                        </div>

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="isActiveCustAddress" style="float: left;" class="col-form-label">Active:</label>

                                <input type="checkbox" class="isAcChk" id="isActiveCustAddress" name="isActiveCustAddress" checked />
                            </div>

                        </div>
                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-secondary btn-block ml-1" data-dismiss="modal" id="closeAddCust">Cancel</button>
                        <button type="button" id="btnSaveBillAddress" class="btn btn-primary text-nowrap" onclick="saveBillingAddress(event)">Add Address</button>
                    </div>
                </div>
            </div>
        </div>


        <div class="modal fade" id="modalCustomerMail" tabindex="-1" role="dialog" aria-labelledby="modalCustomerMail" aria-hidden="true">
            <div class="modal-dialog modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLongTitle">Send Email</h5>
                    </div>
                    <div class="modal-body">

                        <asp:HiddenField ID="HiddenField1" runat="server" />

                        <div class="row mt-3">
                            <div class="col-12 mt-0">
                                <label for="validationCustom01" class="form-label mb-0">To</label>
                                <input type="email" id="_To" placeholder="you@yourcompany.com" class="form-control" runat="server" />
                            </div>
                        </div>

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="validationCustom01" class="form-label mb-0">CC</label>
                                <input type="email" id="_CC" placeholder="you@yourcompany.com" class="form-control" runat="server" />
                                <small class="form-text text-black-50">Add multiple email with comma.(xyz@w.com,Abc@T.com)</small>
                            </div>
                        </div>

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="validationCustom01" class="form-label mb-0">BCC</label>
                                <input type="email" id="_BCC" placeholder="someoneelse@yourcompany.com" class="form-control" runat="server" />
                                <small class="form-text text-black-50">Add multiple email with comma.(xyz@w.com,Abc@T.com)</small>
                            </div>
                        </div>

                        <br />

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="EmailSubject" class="col-sm-2 col-form-label">Subject</label>
                                <div class="col-sm-12">
                                    <input type="text" id="_EmailSubject" name="EmailSubject" runat="server" class="form-control col-6" max="500" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <div class="custom-file">

                                    <asp:FileUpload ID="file" ClientIDMode="Static" runat="server" AllowMultiple="true" MaxLength="100" CssClass="custom-file-input" onchange="javascript:updateList()" />

                                    <label class="custom-file-label" for="file">
                                        <span class="fa fa-paperclip"></span>Attach Files</label>
                                    <ul id="fileList" class="file-list"></ul>
                                </div>
                            </div>
                        </div>

                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="EmailBody" class="col-sm-2 col-form-label"><span>Email Body</span></label>
                                <div class="col-sm-12">
                                    <textarea id="EmailBody" name="EmailBody" rows="5" runat="server" class="form-control"></textarea>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-secondary  btn-block ml-1" data-dismiss="modal" onclick="ClosePopup()" style="float: left">Close</button>
                        <asp:Button ID="btnSendMail" runat="server" Text="Send" CssClass="btn btn-secondary text-nowrap" OnClientClick="return sendMail();" OnClick="btnSendMail_Click" />
                    </div>
                </div>
            </div>
        </div>


    </div>
    <!-- ✅ Smart Form Modal -->
    <div class="modal fade" id="modalSmartForm" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-xl modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Smart Forms</h5>
                    <button type="button" class="btn-close" onclick="CloseSmartFormModal()"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label">Select Form Template</label>
                        <div class="input-group">
                            <select class="form-select" id="ddlFormTemplates" onchange="LoadTemplatePreview()"></select>
                            <%--<button class="btn btn-outline-success" onclick="CreateNewFormTemplate()">+ New</button>
                            <button class="btn btn-outline-primary" onclick="OpenFormEditor()">Edit</button>
                            <button class="btn btn-outline-danger" onclick="DeleteTemplate()">Delete</button>--%>
                        </div>
                    </div>
                    <div id="smartFormPreview" class="border p-3" style="min-height: 400px;"></div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" onclick="CloseSmartFormModal()">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="DownloadFormPDF()">Download PDF</button>
                    <button class="btn btn-success" onclick="SendFormEmail()">Send to Customer</button>
                </div>
            </div>
        </div>
    </div>

    <!-- ✅ Form Editor Modal -->
    <div class="modal fade" id="modalFormEditor" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Create/Edit Smart Form</h5>
                    <button type="button" class="btn-close" onclick="CloseFormEditor()"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" id="editingTemplateId" />
                    <div class="mb-2">
                        <label class="form-label">Form Name</label>
                        <input type="text" class="form-control" id="templateName" placeholder="e.g. Intake Form" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label">Form HTML</label>
                        <textarea class="form-control" id="templateHtml" rows="12" placeholder="<h3>Form</h3>\n<p><strong>Name:</strong> {{CustomerName}}</p>"></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" onclick="CloseFormEditor()">Cancel</button>
                    <button class="btn btn-primary" onclick="SaveFormTemplate()">Save Template</button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal fade" id="modalSendSMS" tabindex="-1" role="dialog" aria-labelledby="modalSendSMS" aria-hidden="true">
        <div class="modal-dialog modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="smsModalLongTitle">Send SMS</h5>

                </div>
                <div class="modal-body">


                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="smsNumber" class="col-sm-2 col-form-label"><span>Mobile No:</span></label>
                            <div class="col-sm-12">
                                <input id="smsNumber" name="smsNumber" rows="5" runat="server" class="form-control" />
                            </div>
                        </div>
                        <div class="col-12 mt-0">
                            <label for="EmailBody" class="col-sm-2 col-form-label"><span>Text</span></label>
                            <div class="col-sm-12">
                                <textarea id="txtSMS" name="SMSBody" rows="5" runat="server" class="form-control"></textarea>
                            </div>
                        </div>
                    </div>


                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-warning  btn-block ml-1" data-dismiss="modal" onclick="CloseSMSPopup()" style="float: left">Close</button>
                    <asp:Button ID="btnSendSms" ClientIDMode="Static" runat="server" Text="Send SMS" CssClass="btn btn-success text-nowrap" OnClick="btnSendSMS_Click" />
                </div>
            </div>
        </div>
    </div>


    <div class="modal fade" id="modalSendMMS" tabindex="-1" role="dialog" aria-labelledby="modalSendMMS" aria-hidden="true">
        <div class="modal-dialog modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="mmsModalLongTitle">Send MMS</h5>

                </div>
                <div class="modal-body">
                    <div class="col-12 mt-0">
                        <label for="mmsNumber" class="col-sm-2 col-form-label"><span>Mobile No:</span></label>
                        <div class="col-sm-12">
                            <input id="mmsNumber" name="mmsNumber" rows="5" runat="server" class="form-control" />
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="EmailBody" class="col-sm-2 col-form-label"><span>MMS Body</span></label>
                            <div class="col-sm-12">
                                <textarea id="txtMMSBody" name="MMSBody" rows="4" runat="server" class="form-control"></textarea>
                            </div>
                        </div>
                    </div>

                    <div class="row mt-3">
                        <div class="cold-12 mt-0">
                            <label for="fuAttachment" class="form-label mb-0">Attachment</label>
                            <asp:FileUpload ID="fuAttachment" runat="server" CssClass="form-control" />
                            <p id="fileHint" class="text-muted small mb-0">Allowed file types: .pdf, .jpg, .jpeg, .png</p>
                            <span id="fileError" class="text-danger small"></span>
                        </div>
                    </div>

                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-warning  btn-block ml-1" data-dismiss="modal" onclick="CloseMMSPopup()" style="float: left">Close</button>
                    <asp:Button ID="btnSendMMS" ClientIDMode="Static" runat="server" Text="Send SMS" CssClass="btn btn-success text-nowrap" OnClick="btnSendMMS_Click" />
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
                    <div class="form-section highlight">
                        <label class="form-label">Tag To <i class="bi bi-info-circle text-warning"></i></label>
                        <p class="text-muted small mb-2">Click to select tags from the list.</p>
                        <div class="dropdown">
                            <div class="tag-input-container" id="tagInputContainer" data-bs-toggle="dropdown" aria-expanded="false">
                                <input type="text" class="tag-input" id="tagSearch" placeholder="Select tags...">
                            </div>
                            <ul class="dropdown-menu" id="tagDropdownMenu" aria-labelledby="tagInputContainer">
                            </ul>
                        </div>
                        <select id="hiddenTagSelect" multiple style="display: none;"></select>
                    </div>
                    <div class="form-section">
                        <label for="noteField" class="form-label">Note</label>
                        <textarea class="form-control" id="noteField" rows="4" placeholder="Enter your detailed note here..."></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" id="clearBtn">Clear</button>
                    <button type="button" class="btn btn-primary" id="addBtn">Add Note <i class="bi bi-plus-circle"></i></button>
                </div>
            </div>
        </div>
    </div>

    <!-- ✅ JS scripts -->
    <script>
        // Initialize selectpicker after page load
        $(document).ready(function () {
            initializeSelectPicker();
        });

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

        $("#addNote").click(function () {
            loadTags();
            $("#noteModal").modal("show");

        });
        function OpenSmartFormModal() {
            $('#modalSmartForm').modal('show');
            LoadFormTemplates();
        }

        function CloseSmartFormModal() {
            $('#modalSmartForm').modal('hide');
        }

        function LoadFormTemplates() {
            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/GetAllTemplates",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (res) {
                    const list = JSON.parse(res.d);
                    let options = list.map(t => `<option value="${t.Id}">${t.TemplateName}</option>`).join('');
                    $('#ddlFormTemplates').html(options);
                    LoadTemplatePreview();
                }
            });
        }

        function LoadTemplatePreview() {
            var customerID = $('#<%=CustomerID.ClientID %>').val()
            const id = $('#ddlFormTemplates').val();
            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/GetTemplateHtmlById",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ templateId: id, customerId: customerID }),
                dataType: "json",
                success: function (res) {
                    let html = res.d;
                    //html = html.replace(/{{CustomerName}}/g, $('#fname').val() + " " + $('#lname').val());
                    //html = html.replace(/{{Phone}}/g, $('#phone').val());
                    //html = html.replace(/{{Email}}/g, $('#email').val());
                    //html = html.replace(/{{Address}}/g, $('#address1').val() + ", " + $('#city').val());
                    //html = html.replace(/{{Company}}/g, $('#txt_CompanyName').val());
                    $('#smartFormPreview').html(html);
                }
            });
        }

        function DownloadFormPDF() {
            const html = document.getElementById("smartFormPreview").innerHTML;
            const templateName = $("#ddlFormTemplates option:selected").text() || "Form";

            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/GeneratePDF",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ html, templateName }),
                success: function (res) {
                    const fileName = res.d;
                    if (!fileName) return alert("PDF generation failed.");

                    const link = document.createElement('a');
                    link.href = 'EmailHistoryContent/temp/' + fileName;
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            });
        }

        function SendFormEmail() {
            const html = document.getElementById("smartFormPreview").innerHTML;
            const templateName = $("#ddlFormTemplates option:selected").text() || "Form";
            const customerId = $("#CustomerID").val();
            const email = $("#email").val();
            const customerName = $("#fname").val() + " " + $("#lname").val();

            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/EmailFormPdfToCustomer",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ html, templateName, customerId, email, customerName }),
                success: function () {
                    Swal.fire("Success", "Form sent to customer email.", "success");
                },
                error: function () {
                    Swal.fire("Error", "Failed to send email.", "error");
                }
            });
        }

        function CreateNewFormTemplate() {
            $('#editingTemplateId').val("0");
            $('#templateName').val("");
            $('#templateHtml').val(`<h3>New Smart Form</h3>
            <p><strong>Name:</strong> {{CustomerName}}</p>
            <p><strong>Email:</strong> {{Email}}</p>
            <p><strong>Phone:</strong> {{Phone}}</p>
            <p><strong>Address:</strong> {{Address}}</p>`);
            $('#modalFormEditor').modal('show');
        }
 

        function OpenFormEditor() {
            const id = $('#ddlFormTemplates').val();
            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/GetTemplateById",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ templateId: id }),
                dataType: "json",
                success: function (res) {
                    const t = JSON.parse(res.d);
                    $('#editingTemplateId').val(t.Id);
                    $('#templateName').val(t.TemplateName);
                    $('#templateHtml').val(t.HtmlBody);
                    $('#modalFormEditor').modal('show');
                }
            });
        }

        function CloseFormEditor() {
            $('#modalFormEditor').modal('hide');
        }

        function SaveFormTemplate() {
            const id = $('#editingTemplateId').val();
            const name = $('#templateName').val();
            const html = $('#templateHtml').val();

            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/SaveTemplate",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ id, name, html }),
                dataType: "json",
                success: function () {
                    alert("Template saved.");
                    $('#modalFormEditor').modal('hide');
                    LoadFormTemplates();
                }
            });
        }

        function DeleteTemplate() {
            const id = $('#ddlFormTemplates').val();
            if (!confirm("Are you sure to delete this template?")) return;
            $.ajax({
                type: "POST",
                url: "customerDetail.aspx/DeleteTemplate",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ templateId: id }),
                dataType: "json",
                success: function () {
                    alert("Deleted.");
                    LoadFormTemplates();
                }
            });
        }
    </script>

    <script type="text/javascript">

        $(document).ready(function () {

            var data = ["Mr.", "Ms.", "Mx.", "Mrs.", "Miss", "Master", "Madam"];
            $('#txt_title').autocomplete({ source: data });
            var country = $('#<%= country.ClientID %>').val();
            if (country === "USA") {
                $('#<%= div_state.ClientID %>').show();
                $('#<%= div_province.ClientID %>').hide();
                $('#<%= lbl_state.ClientID %>').text("State");
                $('#<%= lb_zip.ClientID %>').text("Zip Code");
            }

            var JobTitle_data = '<%=txt_JobTitle %>';

            var parsedTest = JSON.parse(JobTitle_data);

            $('#txt_JobTitle').autocomplete({ source: parsedTest });

            //.bind('focus', function () { $(this).autocomplete("search"), $(this).val(); });

            // $("#seed_one").autocomplete({ source: data });
            // $("#title").autocomplete(data);         

            // Initialize selectpicker for ddlTag
            initializeSelectPicker();

        });
    </script>
    <script type="text/javascript">
        $(document).ready(function () {
            toggleStateProvince(false);

            $('#addAddress').click(function () {
                $('#addAddressModal').modal('show');
                $('#btnSaveBillAddress').text('Add Address');

                $('#BillAddressId').val("");
                $('#txtAddress').val("");
                $('#txtCity').val("");
                $('#txtZipCode').val("");
                $('#txtState').val("");
                $('#isActive').prop('checked', false);
            });

            $('#closeAddCust').click(function () {
                $('#addAddressModal').modal('hide');
            });

            $('#addSiteBtn').click(function () {
                $('#addSiteModal').modal('show');
                $('#btnSaveSite').text('Add Site');

                $('#SiteId').val("0");
                $('#siteContact').val("");
                $('#address').val("");
                $('#siteName').val("");
                $('#note').val("");
                $('#isActive').prop('checked', false);
            });
            $('#closeAddSite').click(function () {
                $('#addSiteModal').modal('hide');
            });
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

            //console.log(divProvince);
            //debugger

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

        function ShowSuccessMsgForCustomer() {
            $.alert({
                title: 'Xceleran',
                content: "Customer data saved successfully.",
                icon: 'fa fa-info-circle',
                animation: 'scale',
                closeAnimation: 'scale',
                opacity: 0.5,
                buttons: {
                    okay: {
                        text: 'okay',
                        btnClass: 'btn-blue',
                        action: function () {
                            window.location.href = "CustomerList.aspx";
                        }
                    }
                }
            });
        }
        function ShowSuccessMsgForLinkedCustomer() {
            $.alert({
                title: 'Xceleran',
                content: "Linked customer data saved successfully.",
                icon: 'fa fa-info-circle',
                animation: 'scale',
                closeAnimation: 'scale',
                opacity: 0.5,
                buttons: {
                    okay: {
                        text: 'okay',
                        btnClass: 'btn-blue',
                        action: function () {
                            window.location.href = "BusinessContact.aspx?id=" + $("#BusinessID").val();
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
        var retVal = $('#<%=RetVal.ClientID%>').val();

        //if (retVal == "1") {
        //    alert("Data has been saved successfully");
        //}  function SaveCustomer() {

        function SaveCustomer() {

            if (ValidateData()) {

                if (document.getElementById('<%=CustomerID.ClientID%>').value == "0") {

                    document.getElementById("<%=hdMode.ClientID%>").value = "Add"
                }
                else {
                    document.getElementById("<%=hdMode.ClientID%>").value = "Modify"
                }
                return true;
            }
        }

        function DeleteCustomer() {
            $.confirm({
                title: 'Xceleran',
                content: "Are you sure you want to delete?",
                icon: 'fa fa-info-circle',
                animation: 'scale',
                closeAnimation: 'zoom',
                buttons: {
                    confirm: function () {
                        var CustomerID = document.getElementById('<%=CustomerID.ClientID%>').value;
                        $.ajax({
                            url: 'customerDetail.aspx/DeleteCustomer',
                            type: "POST",
                            contentType: 'application/json',
                            data: "{CustomerID:" + CustomerID + "}",
                            dataType: 'json',
                            success: function (sR) {
                                window.location.href = 'CustomerList.aspx?m=2';

                            },
                            error: function (error) {
                                console.log(error);
                            }
                        })
                    },
                    cancel: function () {
                        text: 'No'
                    }
                }
            });
        }

        function ValidateData() {
            var FirstName = document.getElementById('<%=fname.ClientID%>').value;
            var LastName = document.getElementById('<%=lname.ClientID%>').value;
            var Address = document.getElementById('<%=address1.ClientID%>').value;
            var City = document.getElementById('<%=city.ClientID%>').value;
            var State = document.getElementById('<%=state.ClientID%>').value;
            var Province = document.getElementById('<%=province.ClientID%>').value;
            var Zip = document.getElementById('<%=zip.ClientID%>').value;
            var Country = document.getElementById('<%=country.ClientID%>');
            Country = Country ? Country.value : null;

            if (FirstName.trim() == "") {
                alert("First name cannot be blank");
                return false;
            }

            if (City.trim() == "") {
                alert("City cannot be blank");
                return false;
            }

            if (Country == "select" && $('#<%=div_country.ClientID%>').is(':visible')) {
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
            window.location.href = "calendar.aspx?CustomerID=" + $('#<%=CustomerID.ClientID %>').val();

        }
        function OpenMailPopUp() {
            var CustomerID = document.getElementById('<%=CustomerID.ClientID%>').value;
            FillEmailBody(CustomerID);

            //$("#EmailBody").val("");
            //$("#_CC").val("");
            //$("#_BCC").val("");
            //$("#_From").val("");
            //$("#EmailHeader").val("0");
            //$("#CustomerID").val(CustomerID);
            // ShowPopup();
        }
        function FillEmailBody(customerid) {
            //  alert("caled");
            $.ajax({
                url: 'customerDetail.aspx/FillEmailModal',
                type: "Post",
                contentType: 'application/json',
                data: "{CustomerID:'" + customerid + "'}",
                dataType: 'json',
                success: function (sR) {
                    // $("#form1")[0].reset();
                    $("#_EmailSubject").val("");
                    $("#EmailBody").val("");
                    $("#_CC").val("");
                    $("#_BCC").val("");
                    $("#_To").val("");

                    if (sR.d != "0") {
                        var data = $.parseJSON(sR.d);
                        $("#_EmailSubject").val(data.StandardMailSubject);
                        $("#EmailBody").val(data.StandardMailBody);
                        $("#_CC").val(data.EmailCC);
                        $("#_BCC").val(data.EmailBCC);
                        $("#_To").val(data.EmailTo);

                        $("#file").val();
                        $("#fileList").empty();
                        ShowPopup();
                    }

                },
                error: function (error) {
                    alert(error);
                }
            })

        }
        function ShowPopup() {

            $("#modalCustomerMail").modal('show');
        }
        function ClosePopup() {

            $("#modalCustomerMail").modal('hide');
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
        function BackClicked() {
            window.location.href = "CustomerList.aspx?m=2";
        }
        function CreateInvoice() {

            var RefId = $('#<%=hdCustomerGUID.ClientID %>').val();
            window.location.href = "InvoiceCreate.aspx?m=0" + "&cId=" + RefId;

        }

        function ViewInvoiceList(type) {

            window.location.href = "Invoices.aspx?Id=" + $('#<%=CustomerID.ClientID %>').val() + "&Type=" + type + "&Form=CustomerDetail&FromCustomerDetail=1";
        }
        function ViewEmailHistoryList(CustomerID) {

            window.location.href = "EmailHistory_List.aspx?Id=" + $('#<%=CustomerID.ClientID %>').val();
        }

        function CreateProposal(type) {

            window.location.href = "Invoice.aspx?InvNum=0&cId=" + $('#<%=hdCustomerGUID.ClientID %>').val() + "&InType=" + type + "&FormType='CustomerDetail'&FromCustomerDetail=1";
        }
        function ViewFiles() {

            window.location.href = "CustomerFiles.aspx?Id=" + $('#<%=CustomerID.ClientID %>').val();
        }

        function ViewAppointment() {

            window.location.href = "View_Appointment.aspx?Id=" + $('#<%=CustomerID.ClientID %>').val() + "&Form=CustomerDetail";
        }
        $("#<%=email.ClientID%>").focusout(function () {
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
        function sendMail() {
            evt.preventDefault();
            Swal.fire({
                title: 'Are you sure you want to send  email?',
                showDenyButton: true,
                showCancelButton: false,
                confirmButtonText: 'Yes',
                denyButtonText: 'No',
                customClass: {
                    actions: 'my-actions',
                    cancelButton: 'order-1 right-gap',
                    confirmButton: 'order-2',
                    denyButton: 'order-3',
                }
            }).then((result) => {

                if (result.isConfirmed) {
                    return false;

                } else if (result.isDenied) {
                    evt.preventDefault();

                }
                //return true;
            })
        }

        function OpenSMSHistory() {
            var mobile = $("#mobile").val();
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }

            var firstName = $("#fname").val();
            var lastName = $("#lname").val();
            var customerName = firstName + ' ' + lastName;
            var customerId = $("#CustomerID").val();
            var url = "CustomerTextHistory.aspx?mobile=" + encodeURIComponent(mobile)
                + "&name=" + encodeURIComponent(customerName)
                + "&customerId=" + encodeURIComponent(customerId);

            window.open(url, '_blank');
        }

        function OpenAllHistory() {
            var mobile = $("#mobile").val();
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            var firstName = $("#fname").val();
            var lastName = $("#lname").val();
            var customerName = firstName + ' ' + lastName;
            var customerId = $("#CustomerID").val();
            var url = "CustomerChatHistory.aspx?mobile=" + encodeURIComponent(mobile)
                + "&name=" + encodeURIComponent(customerName)
                + "&customerId=" + encodeURIComponent(customerId);

            window.open(url, '_blank');
        }

        function OpenSMSPopUp() {
            var mobile = $("#mobile").val();
            $("#smsNumber").val(mobile);
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }

            $('#modalSendSMS').modal('show');
        }
        function CloseSMSPopup() {
            $("#txtSMS").val('');
            $("#modalSendSMS").modal('hide');
        }

        document.getElementById("btnSendSms").addEventListener("click", function (e) {
            var sms = document.getElementById("txtSMS").value.trim();

            if (!sms) {
                Swal.fire('Validation Error', 'SMS text cannot be empty.', 'warning');
                e.preventDefault();
            }

        });

        function OpenMMSPopUp() {
            var mobile = $("#mobile").val();
           $("#mmsNumber").val(mobile);

            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            $('#modalSendMMS').modal('show');
        }

        function CloseMMSPopup() {
            $("#txtMMSBody").val('');
            $("#fuAttachment").val('');
            $("#modalSendMMS").modal('hide');
        }

        document.getElementById("btnSendMMS").addEventListener("click", function (e) {
            var file = document.getElementById("fuAttachment").value.trim();
            var sms = document.getElementById("txtMMSBody").value.trim();

            if (!file || !sms) {
                Swal.fire('Validation Error', 'MMS body and file cannot be empty.', 'warning');
                e.preventDefault();
            }
        });

        document.addEventListener("DOMContentLoaded", function () {
            var fileInput = document.getElementById("<%= fuAttachment.ClientID %>");
            var errorLabel = document.getElementById("fileError");

            fileInput.addEventListener("change", function () {
                errorLabel.innerText = "";

                if (fileInput.files.length > 0) {
                    var fileName = fileInput.files[0].name.toLowerCase();
                    var allowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

                    var isValid = allowedExtensions.some(function (ext) {
                        return fileName.endsWith(ext);
                    });

                    if (!isValid) {
                        errorLabel.innerText = "❌ Invalid file type! Only PDF, JPG, JPEG, PNG are allowed.";
                        fileInput.value = "";
                    }
                }
            });
        });
    </script>

    <script src="Scripts/TpDetail.js?v2" type="text/javascript"></script>
</asp:Content>
