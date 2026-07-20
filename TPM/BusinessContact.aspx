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

        .form-label {
            float: left;
            width: 20%;
        }

        .dataTables_scrollBody {
            min-height: 400px;
        }

        #example .dropdown {
            position: inherit !important;
        }
    </style>

    <div class="col-lg-12">
        <div class="row">
            <div class="row">

                <asp:HiddenField ID="hdCompanyName"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="hdCompanyTag"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="BusinessID" ClientIDMode ="Static" runat="server" />
                <asp:HiddenField ID="hf_BusinessGuid"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="PrimaryCustomerid"   ClientIDMode ="Static" Value="0" runat="server" />
                <asp:HiddenField ID="BusinessGuID"  ClientIDMode ="Static"  runat="server" />
                <asp:HiddenField ID="hdMode" ClientIDMode ="Static"    runat="server" />
                <asp:HiddenField ID="hdCustomerGUID" ClientIDMode ="Static"   runat="server" />
                <asp:HiddenField ID="RetVal" ClientIDMode ="Static"   runat="server" />

                <div class="col-md-12">
                    <div class="card mb-2">
                        <div class="card-body">
                            <div class="row">
                                <div class="col-12">
                                    <%--Nizam--%>
                                    <div class="row" id="RM_details" runat="server">
                                        <div class="col-md-6 mt-0">
                                            <label for="validationCustom0tyu1" class="form-label mb-0">AM ID</label>
                                            <input type="text" name="txt_rmid" id="txt_rmid" class="form-control" runat="server" readonly>
                                            <%-- <textarea  name="txt_rmid" class="form-control" id="txt_rmid" runat="server" rows="1"></textarea> --%>
                                        </div>
                                        <div class="col-md-6 mt-0">
                                            <label for="validationCustom0tyu1" class="form-label mb-0">Customer Code</label>
                                            <input type="text" name="txt_custcode" id="txt_custcode" class="form-control" runat="server" readonly>
                                            <%--<textarea  name="txt_custcode" class="form-control" id="txt_custcode" runat="server" rows="1"></textarea> --%>
                                        </div>
                                    </div>

                                    <div class="row mt-2">
                                        <div class="col-md-6 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Business Name</label>
                                            <input type="text" name="txt_BusinessName" id="txt_BusinessName" class="form-control" runat="server">
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-2 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Title</label>
                                            <asp:TextBox ID="txt_Title" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                        <div class="col-md-5 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">First Name</label>
                                            <input type="text" name="txt_FirstName" id="txt_FirstName" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-5 mt-0">
                                            <label for="" class="form-label mb-0">Last Name</label>
                                            <input type="text" name="txt_LastName" id="txt_LastName" class="form-control" runat="server">
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-3 mt-0">
                                            <label class="form-label mb-0">Tag</label>
                                            <select id="ddlTag" runat="server" title="Select Tags" style="width: 100%" class="form-control selectpicker" multiple="true" data-live-search="true">
                                            </select>
                                        </div>
                                        <div class="col-md-9 mt-0">
                                            <label for="validationCustom01" class="form-label mb-0">Address 1</label>
                                            <input type="text" name="address1" id="address1" class="form-control" runat="server">
                                        </div>
                                        <div class="col-md-12 mt-0">
                                            <label for="" class="form-label mb-0">Address 2</label>
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
                                            <label for="validationCustom01" class="form-label mb-0">Main Phone</label>
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
                                            Total Due (For <span id="TotalInvoice" runat="server"></span>Invoice) :
                                                <div class="input-group mb-3">
                                                    <div class="input-group-prepend">
                                                        <span class="input-group-text">$</span>
                                                    </div>
                                                    <input type="text" placeholder="0.00" name="TotalDueForInvoice" id="TotalDueForInvoice" class="form-control" runat="server" readonly>
                                                </div>
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            Total Due (For <span id="TotalEstimate" runat="server"></span>Estimate) :
                                                <div class="input-group mb-3">
                                                    <div class="input-group-prepend">
                                                        <span class="input-group-text">$</span>
                                                    </div>
                                                    <input type="text" placeholder="0.00" name="TotalDueForEstimate" id="TotalDueForEstimate" class="form-control" runat="server" readonly>
                                                </div>
                                        </div>
                                        <div class="col-md-4 mt-0">
                                            Total Appointment :
                                                <div class="input-group mb-3">
                                                    <div class="input-group-prepend">
                                                        <span class="input-group-text">&nbsp;</span>
                                                    </div>
                                                    <input type="text" name="TotalAppoinment" id="TotalAppoinment" class="form-control" runat="server" readonly>
                                                </div>
                                        </div>
                                    </div>
                                    <div class="row mt-2">
                                        <div class="col-md-12 mt-0">
                                            <label for="validationCustom0tyu1" class="form-label mb-0">Notes :</label>
                                            <textarea name="txt_notes" class="form-control" id="txt_notes" runat="server" rows="4" cols="50"></textarea>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <hr>
                            <div class="row">
                                <div class="col-sm-12">
                                    <div class="d-flex justify-content-end">
                                        <div id="div_More" runat="server" class="dropdown">
                                            <button type="button" class="btn btn-primary dropdown-toggle" data-bs-toggle="dropdown">
                                                More
                                            </button>
                                            <div class="dropdown-menu" aria-labelledby="dropdownMenuButton">
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
                                        &nbsp;
                                        <input type="button" name="back" value="Back" class="btn btn-secondary" onclick="BackClicked();">&nbsp;

                                            <asp:Button ID="btn_Save" runat="server" CssClass="btn btn-primary" Text="Save" OnClientClick="javascript:if(SaveCustomer()){return true;}else return false;" OnClick="btn_Save_Click" />&nbsp;
                                           <input type="button" runat="server" id="btn_Delete" name="btn_Delete" value="Delete" class="btn btn-danger" onclick="DeleteCustomer();">
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12">
                    <div class="card mb-2">
                        <div class="card-body">
                            <div class="row">
                                <div class="col-sm-12">
                                    <h4 class="mb-0 text-black-50">Linked Contacts</h4>
                                </div>
                            </div>
                            <hr>

                            <div class="row">
                                <div class="col-12 table-responsive" style="overflow-y: hidden; height: 250px;">

                                    <asp:PlaceHolder ID="ListTable" runat="server"></asp:PlaceHolder>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
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
                                <h5 class="mb-0">Files, and History of Communications</h5>
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

                    <hr />

                    <div class="card">
                        <div class="card-header" id="headingTwo">
                            <div style="margin-top: 25px;" class="float-start mb-0">
                                <h5 class="mb-0">Invoice List</h5>
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
                                    <div class="spinner-border m-5" id="spinner_ProjectList" role="status" style="display: none;">
                                        <span class="sr-only">Loading...</span>
                                    </div>
                                    <div class="col-12 table-responsive">
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
    <script src="Scripts/BusinessContact.js?v=10" type="text/javascript"></script>
   
</asp:Content>