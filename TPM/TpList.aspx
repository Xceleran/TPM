<%@ Page Title="" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="TpList.aspx.cs" Inherits="TPM.TpList" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
      <%-- new for datable print --%>
    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/css/bootstrap-select.css" />
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/js/bootstrap-select.min.js"></script>
     <link rel="stylesheet" type="text/css" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.0.1/css/bootstrap.min.css" />
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/2.2.2/css/dataTables.dataTables.min.css" />
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/buttons/3.2.0/css/buttons.dataTables.min.css" />

    <%-- The DataTables CORE library. It was missing entirely: the page loaded the DataTables
         stylesheet and the Buttons extension but never the library those depend on, so
         $('#example').dataTable() threw "is not a function" and the grid rendered as a plain
         static table with no search, sort, paging or export. Core must come before Buttons.
         Versions match Customer.aspx / AppoinementList.aspx (DataTables 2.2.2 + Buttons 3.x). --%>
    <script src="https://cdn.datatables.net/2.2.2/js/dataTables.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/3.2.0/js/dataTables.buttons.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jszip/3.10.1/jszip.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/3.2.0/js/buttons.html5.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/3.2.0/js/buttons.print.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/3.2.0/js/buttons.colVis.min.js"></script>
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
        /* The grid no longer uses DataTables' scrolling mode, so the old
           .dataTables_scrollBody min-height rule (a DataTables 1.x class name that
           DataTables 2.x does not emit anyway) has been dropped. */

        /* The row action menu sits inside .table-responsive, whose overflow would clip a
           dropdown. Let it escape; the table is only seven columns wide. */
        .table-responsive:has(.dropdown-menu.show) {
            overflow: visible;
        }

        td.tp-actions {
            width: 1%;
            white-space: nowrap;
        }
    </style>


    <div class="d-flex flex-column-fluid home-1stsec">
        <div class="container-fluid" style="width: 95%">
            <div class="row">

                <asp:HiddenField ID="hdCompanyID" runat="server" />
                <asp:HiddenField ID="hdCompanyName" runat="server" />
                <asp:HiddenField ID="hdCompanyGUID" runat="server" />
                <asp:HiddenField ID="hdCompanyTag" runat="server" />
                <asp:HiddenField ID="hdCusID" runat="server" />

                <div class="col-12 mb-3">
                    <div class="card card-custom gutter-b card-stretch p-0">
                        <div class="card-header bg-light" style="display:none;">
                            <div class="card-title d-flex justify-content-between align-items-center w-100">

                                <div class="float-start">
                                    <h3 class="card-label">Search Customers</h3>
                                </div>
                                <div class="float-end">
                                    <a class="btn btn-primary float-end" id="Add_new_Customer" runat="server" href="customerDetail.aspx?m=0&cid=0&Mode=Add"><i class="fas fa-plus"></i></a>

                                    <div id="Add_new_Customer_dropdown" runat="server" class="dropdown">
                                        <button class="btn btn-primary float-end dropdown-toggle" type="button" id="dropdownMenuButton1" data-bs-toggle="dropdown" aria-expanded="false">
                                            <i class="fas fa-plus"></i>
                                        </button>
                                        <ul class="dropdown-menu" aria-labelledby="dropdownMenuButton1">
                                            <li><a class="dropdown-item" href="BusinessContact.aspx?Mode=Add">Business</a></li>
                                            <li><a class="dropdown-item" href="customerDetail.aspx?m=0&cid=0&Mode=Add">Individual</a></li>
                                        </ul>
                                    </div>


                                </div>

                            </div>

                        </div>

                        <div class="card-body">


                            <div class="row">
                                <div style="display:none" runat="server" id="div_SearchFor" class="col-lg-2 mt-0">
                                    <h6>Search For :</h6>
                                    <asp:DropDownList ID="ddl_SearchFor" ClientIDMode="Static" runat="server" class="form-select">
                                        <asp:ListItem Selected="True" Value="Customer">Customer</asp:ListItem>
                                        <asp:ListItem Value="Contact"> Contact</asp:ListItem>
                                        <asp:ListItem Value="Business"> Business</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="col-lg-2 mt-0">
                                    <h6>Search By</h6>
                                    <%-- ClientIDMode=Static: the inline JS below binds to #SearchBy / #ddl_SearchFor /
                                         #ddlTag by their bare ids, which never matched the ASP.NET-generated ids. --%>
                                    <asp:DropDownList ID="SearchBy" ClientIDMode="Static" runat="server" class="form-select">
                                        <asp:ListItem Value="LastName">Last Name</asp:ListItem>
                                        <asp:ListItem Value="FirstName">First Name</asp:ListItem>
                                        <asp:ListItem Value="BusinessName">Business Name</asp:ListItem>
                                        <asp:ListItem Value="JobTitle">Job Title</asp:ListItem>
                                        <asp:ListItem Value="City"> City </asp:ListItem>
                                        <asp:ListItem Value="Address"> Address </asp:ListItem>
                                        <asp:ListItem Value="Mobile"> Mobile </asp:ListItem>
                                        <asp:ListItem Value="Phone"> Phone </asp:ListItem>
                                        <asp:ListItem Value="Email"> Email </asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="col-lg-2 mt-0">
                                    <h6>Tag</h6>
                                    <select id="ddlTag" clientidmode="Static" runat="server" title="Select Tags" style="width: 100%" class="form-control selectpicker" multiple="true" data-live-search="true">
                                    </select>
                                </div>
                                <div class="col-lg-4 mt-0">
                                    <h6>Search Value</h6>

                                    <asp:TextBox ID="SearchValue" ClientIDMode="Static" runat="server" class="form-control"></asp:TextBox>

                                </div>

                                <div class="col-lg-2 mt-0">
                                    <h6>&nbsp;</h6>
                                    <asp:Button ID="Search" ClientIDMode="Static" runat="server" OnClick="Search_Click" Text="Search" class="btn btn-secondary w-100" />

                                </div>

                                <%-- Removed: a permanently hidden QuickBooks Online / Aire-Master sync block whose
                                     handlers posted to CustomerList.aspx/SyncCustomerToQBO and
                                     CustomerList.aspx/SyncCustomerAPItoDB. CustomerList.aspx does not exist in TPM,
                                     so both were guaranteed 404s. --%>
                            </div>
                            <hr />
                            <div class="row">
                                <div class="col-lg-12 mt-3">
                                    <div class="col-12 table-responsive" style="overflow-y: hidden;">

                                        <asp:PlaceHolder ID="ListTable" runat="server"></asp:PlaceHolder>

                                    </div>
                                </div>
                            </div>
                        </div>

                    </div>
                </div>

                <div class="modal fade" id="SurveyMail" tabindex="-1" role="dialog" aria-labelledby="exampleModalCenterTitle" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="AddEditLongTitle">Ratings </h5>
                                <%-- btn-close + data-bs-dismiss: the page runs Bootstrap 5, where data-dismiss
                                     is a no-op. This × was the only close control with no onclick fallback,
                                     so the Ratings modal could not be closed at all. --%>
                                <button type="button" class="btn-close" onclick="hideModal();" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body">
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">Email To :</label>
                                        <input type="hidden" id="txt_CustID" runat="server" clientidmode="Static" />
                                        <asp:TextBox ID="txt_EmailTO" runat="server" class="form-control" ClientIDMode="Static"></asp:TextBox>

                                    </div>
                                </div>
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">Title :</label>
                                        <%-- <select id="optSurvey" runat="server" title="Select Survey" style="width: 100%" class="form-select">
                                       <option value=""> Select Survey Title</option>
                                    </select>--%>
                                        <asp:DropDownList ID="optSurvey" ClientIDMode="Static" onchange="FillSurveyEmailBody()" runat="server" CssClass="form-select">
                                            <asp:ListItem Value="">Select Ratings</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">Email Subject :</label>
                                        <asp:TextBox ID="txt_EmailSubject" ClientIDMode="Static" Rows="2" TextMode="MultiLine" runat="server" class="form-control"></asp:TextBox>

                                    </div>
                                </div>
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">Email Body :</label>
                                        <asp:TextBox ID="txt_EmailBody" ClientIDMode="Static" Rows="5" TextMode="MultiLine" runat="server" class="form-control"></asp:TextBox>

                                    </div>
                                </div>
                                <%--  <div class="row mt-2">
                                <div class="col-12 mt-0">
                                    <label for="validationCustom01" class="form-label mb-0">Text Message :</label>
                                    <asp:TextBox ID="txt_TextMessage" Rows="5" TextMode="MultiLine" runat="server" class="form-control"></asp:TextBox>

                                </div>
                            </div>--%>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" onclick="hideModal();" data-bs-dismiss="modal">Close</button>
                                <asp:LinkButton ToolTip="Send Ratings/Survey Email." CssClass="float-end btn btn-secondary m-2" ID="lnkEdit"
                                    runat="server" Text='Send Ratings/Survey Email.' OnClick="lnkFollowUP_Click1"><i class="fas fa-envelope"></i> Send Ratings Email.</asp:LinkButton>&nbsp;
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

                                <%-- ClientIDMode=Static throughout this modal: FillEmailBody() populates these
                                     by bare id ($("#_To"), $("#EmailBody") ...), which never matched the
                                     ASP.NET-generated ids, so the modal always opened blank. --%>
                                <asp:HiddenField ID="CustomerID" ClientIDMode="Static" runat="server" />

                                <div class="row mt-3">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">To</label>
                                        <input type="email" id="_To" clientidmode="Static" placeholder="you@yourcompany.com" class="form-control" runat="server" />
                                    </div>
                                </div>

                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">CC</label>
                                        <input type="email" id="_CC" clientidmode="Static" placeholder="you@yourcompany.com" class="form-control" runat="server" />
                                        <small class="form-text text-black-50">Add multiple email with comma.(xyz@w.com,Abc@T.com)</small>
                                    </div>
                                </div>

                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">BCC</label>
                                        <input type="email" id="_BCC" clientidmode="Static" placeholder="someoneelse@yourcompany.com" class="form-control" runat="server" />
                                        <small class="form-text text-black-50">Add multiple email with comma.(xyz@w.com,Abc@T.com)</small>
                                    </div>
                                </div>

                                <br />

                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="EmailSubject" class="col-sm-2 col-form-label">Subject</label>
                                        <div class="col-sm-12">
                                            <input type="text" id="_EmailSubject" clientidmode="Static" name="EmailSubject" runat="server" class="form-control col-6" max="500" />
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
                                            <textarea id="EmailBody" clientidmode="Static" name="EmailBody" rows="5" runat="server" class="form-control"></textarea>
                                        </div>
                                    </div>
                                </div>


                            </div>
                            <div class="modal-footer justify-content-between">
                                <button type="button" class="btn btn-secondary  btn-block ml-1" data-bs-dismiss="modal" onclick="ClosePopup()" style="float: left">Close</button>
                                <asp:Button ID="btnSendMail" ClientIDMode="Static" runat="server" Text="Send" CssClass="btn btn-secondary text-nowrap" OnClick="btnSendMail_Click" />
                            </div>
                        </div>
                    </div>
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


                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="validationCustom01" class="form-label mb-0">Mobile Number</label>
                            <%-- ClientIDMode=Static: OpenSMSPopUp/CloseSMSPopup and the btnSendSms validation
                                 handler address these by bare id. Without it the handler threw
                                 "Cannot read properties of null" and, because it threw before
                                 e.preventDefault(), the postback went through with an empty number. --%>
                            <input type="text" id="txtMobile" clientidmode="Static" class="form-control" runat="server" readonly />
                            <input runat="server" type="text" id="txtCustomerId" clientidmode="Static" hidden />

                        </div>
                    </div>

                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="EmailBody" class="col-sm-2 col-form-label"><span>Text</span></label>
                            <div class="col-sm-12">
                                <textarea id="txtSMS" clientidmode="Static" name="SMSBody" rows="5" runat="server" class="form-control"></textarea>
                            </div>
                        </div>
                    </div>


                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-warning  btn-block ml-1" data-bs-dismiss="modal" onclick="CloseSMSPopup()" style="float: left">Close</button>
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


                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="txtCustMob" class="form-label mb-0">Mobile Number</label>
                            <input type="text" id="txtCustMob" clientidmode="Static" class="form-control" runat="server" readonly />
                            <input runat="server" type="text" id="txtCustId" clientidmode="Static" hidden />

                        </div>
                    </div>

                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="EmailBody" class="col-sm-2 col-form-label"><span>MMS Body</span></label>
                            <div class="col-sm-12">
                                <textarea id="txtMMSBody" clientidmode="Static" name="MMSBody" rows="4" runat="server" class="form-control"></textarea>
                            </div>
                        </div>
                    </div>

                    <div class="row mt-3">
                        <div class="cold-12 mt-0">
                            <label for="fuAttachment" class="form-label mb-0">Attachment</label>
                            <%-- ClientIDMode=Static on fuAttachment and btnSendMMS. btnSendMMS lacked it while
                                 btnSendSms had it, so document.getElementById("btnSendMMS") returned null and
                                 the resulting TypeError aborted the rest of the inline script block — which is
                                 why the .pdf/.jpg/.jpeg/.png attachment check below never got registered. --%>
                            <asp:FileUpload ID="fuAttachment" ClientIDMode="Static" runat="server" CssClass="form-control" />
                            <p id="fileHint" class="text-muted small mb-0">Allowed file types: .pdf, .jpg, .jpeg, .png</p>
                            <span id="fileError" class="text-danger small"></span>
                        </div>
                    </div>

                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-warning  btn-block ml-1" data-bs-dismiss="modal" onclick="CloseMMSPopup()" style="float: left">Close</button>
                    <asp:Button ID="btnSendMMS" ClientIDMode="Static" runat="server" Text="Send MMS" CssClass="btn btn-success text-nowrap" OnClick="btnSendMMS_Click" />
                </div>
            </div>
        </div>
    </div>






    <script>
        // ---------------------------------------------------------------------------
        // Modal helpers.
        // The page ends up with Bootstrap 5 in charge ($.fn.modal is whatever the last
        // bundle registered), so go through the native BS5 API and only fall back to the
        // jQuery plugin. Previously every close button used the Bootstrap 4 data-dismiss
        // attribute, which BS5 ignores.
        // ---------------------------------------------------------------------------
        function showBsModal(id) {
            var el = document.getElementById(id);
            if (!el) return;
            if (window.bootstrap && bootstrap.Modal) bootstrap.Modal.getOrCreateInstance(el).show();
            else $(el).modal('show');
        }
        function hideBsModal(id) {
            var el = document.getElementById(id);
            if (!el) return;
            if (window.bootstrap && bootstrap.Modal) {
                var inst = bootstrap.Modal.getInstance(el);
                if (inst) inst.hide();
            } else {
                $(el).modal('hide');
            }
        }
        function htmlEscape(s) {
            return $('<div>').text(s == null ? '' : s).html();
        }

        $("#SearchValue").on("keypress", function (e) {
            if (e.keyCode == 13) {
                e.preventDefault();   // stop the browser firing its own default submit as well
                $("#Search").click();
            }
        });

        // Attachment list for the Send Email modal. The previous change handler built a
        // FormData, POSTed it to the literal placeholder URL "/path/to/server" and then
        // called showFileCount(), which is not defined anywhere - so picking a file threw
        // and fired a 404. Only the list rendering was ever wanted.
        function updateList() {
            var input = document.getElementById('file');
            var output = document.getElementById('fileList');
            if (!input || !output) return;
            var children = "";
            for (var i = 0; i < input.files.length; ++i) {
                children += '<li><span class="remove-list fa fa-check"></span>' + htmlEscape(input.files.item(i).name) + '</li>';
            }
            output.innerHTML = children;
        }

        // ---------------------------------------------------------------------------
        // Ratings / survey email
        // ---------------------------------------------------------------------------
        function OpenSurveyMailPopUp(emailto, customerID) {
            if (!emailto) {
                Swal.fire('Validation Error', 'This provider has no email address on file.', 'warning');
                return;
            }
            $("#txt_EmailTO").val(emailto);
            $("#txt_CustID").val(customerID);
            $("#optSurvey").val("");
            $("#txt_EmailSubject").val("");
            $("#txt_EmailBody").val("");
            showModal();
        }
        function showModal() {
            showBsModal('SurveyMail');
        }
        function hideModal() {
            hideBsModal('SurveyMail');
            $('.modal-backdrop').remove();
        }
        function FillSurveyEmailBody() {
            var id = $("#optSurvey").val();
            var customerid = $("#txt_CustID").val();
            if (!id) {
                $("#txt_EmailSubject").val("");
                $("#txt_EmailBody").val("");
                return;
            }

            $.ajax({
                // was CustomerList.aspx/optSurvey_Changed - CustomerList.aspx does not exist
                // in TPM, so this was always a 404. The WebMethod lives on this page.
                url: 'TpList.aspx/optSurvey_Changed',
                type: "POST",
                contentType: 'application/json',
                data: JSON.stringify({ SurveyID: id, CustomerID: customerid }),
                dataType: 'json',
                success: function (sR) {
                    var d = sR.d || [];
                    $("#txt_EmailSubject").val(d[0] || "");
                    $("#txt_EmailBody").val(d[1] || "");
                },
                error: function (xhr) {
                    Swal.fire('Error', 'Could not load the ratings template.', 'error');
                    console.error('optSurvey_Changed failed', xhr.status, xhr.responseText);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Ad-hoc email
        // ---------------------------------------------------------------------------
        function ShowPopup() {
            showBsModal('modalCustomerMail');
        }
        function ClosePopup() {
            hideBsModal('modalCustomerMail');
        }
        function OpenMailPopUp(CustomerID) {
            $("#CustomerID").val(CustomerID);
            FillEmailBody(CustomerID);
        }

        function FillEmailBody(customerid) {
            $.ajax({
                // was CustomerList.aspx/FillEmailModal - see note above.
                url: 'TpList.aspx/FillEmailModal',
                type: "POST",
                contentType: 'application/json',
                data: JSON.stringify({ CustomerID: customerid }),
                dataType: 'json',
                success: function (sR) {
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

                        $("#file").val('');
                        $("#fileList").empty();
                        ShowPopup();
                    } else {
                        Swal.fire('Not configured', 'No standard email template is set up for this company.', 'info');
                    }
                },
                error: function (xhr) {
                    Swal.fire('Error', 'Could not load the email template.', 'error');
                    console.error('FillEmailModal failed', xhr.status, xhr.responseText);
                }
            });
        }

        $('#btnSendMail').on('click', function (evt) {
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
                    window.__doPostBack("<%= btnSendMail.UniqueID %>", "");
                }
            });
        });

        // ---------------------------------------------------------------------------
        // Tag picker
        // ---------------------------------------------------------------------------
        function initializeSelectPicker() {
            try {
                var $ddlTag = $('#<%= ddlTag.ClientID %>');
                if ($ddlTag.length > 0) {
                    if ($ddlTag.next('.bootstrap-select').length > 0) {
                        try { $ddlTag.selectpicker('destroy'); }
                        catch (e) { console.log('Error destroying selectpicker: ' + e.message); }
                    }
                    setTimeout(function () {
                        $ddlTag.selectpicker({
                            noneSelectedText: 'Select Tags',
                            selectAllText: 'Select All',
                            deselectAllText: 'Deselect All'
                        });
                        $ddlTag.selectpicker('refresh');
                    }, 100);
                } else {
                    console.log('ddlTag element not found');
                }
            } catch (e) {
                console.error('Error in initializeSelectPicker: ' + e.message);
            }
        }

        // ---------------------------------------------------------------------------
        // Grid
        // ---------------------------------------------------------------------------
        var providerTable = null;

        $(document).ready(function () {
            initializeSelectPicker();

            providerTable = $('#example').DataTable({
                // "scrollY": true was invalid (it expects a CSS length) and scrollX put the
                // grid inside its own overflow container, which clipped the row action menu.
                // The surrounding .table-responsive already handles narrow viewports.
                "order": [],
                "pageLength": 25,
                "lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
                "columnDefs": [
                    // action column: not sortable, not searchable, not exported
                    { targets: 0, orderable: false, searchable: false, className: 'no-export' },
                    { targets: '_all', orderSequence: ['asc', 'desc'] }
                ],
                "layout": {
                    topStart: {
                        buttons: [
                            {
                                extend: 'excel',
                                text: 'Excel',
                                className: 'datatableButton',
                                title: 'Third Party Providers',
                                // was columns: [1,2,3,4,5,6] against a 6-column table (0-5),
                                // which dropped Business Name and asked for a column that did
                                // not exist. Select by class instead so it cannot drift again.
                                exportOptions: { columns: ':visible:not(.no-export)' }
                            },
                            {
                                extend: 'print',
                                text: 'Print',
                                className: 'datatableButton',
                                title: 'Third Party Providers',
                                exportOptions: { columns: ':visible:not(.no-export)' }
                            }
                        ]
                    },
                    topEnd: 'search',
                    bottomStart: ['pageLength', 'info'],
                    bottomEnd: 'paging'
                }
            });

            if ($("#SearchBy").val() == "All") {
                $("#SearchValue").val("");
                $("#SearchValue").prop('disabled', true);
            }
        });

        $("#ddl_SearchFor").change(function () {
            if ($("#ddl_SearchFor").val() != "Business") {
                $("#SearchValue").prop('disabled', false);
            }
        });

        $("#SearchBy").change(function () {
            if ($("#SearchBy").val() == "All") {
                $("#SearchValue").val("");
                $("#SearchValue").prop('disabled', true);
            } else {
                $("#SearchValue").prop('disabled', false);
            }
        });

        // ---------------------------------------------------------------------------
        // SMS
        // ---------------------------------------------------------------------------
        function OpenSMSPopUp(mobile, customerID) {
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            $("#txtMobile").val(mobile);
            $("#txtCustomerId").val(customerID);
            $("#txtSMS").val('');
            showBsModal('modalSendSMS');
        }
        function CloseSMSPopup() {
            $("#txtMobile").val('');
            $("#txtCustomerId").val('');
            $("#txtSMS").val('');
            hideBsModal('modalSendSMS');
        }

        (function () {
            var btn = document.getElementById("btnSendSms");
            if (!btn) return;
            btn.addEventListener("click", function (e) {
                var mobileEl = document.getElementById("txtMobile");
                var smsEl = document.getElementById("txtSMS");
                var mobile = mobileEl ? mobileEl.value.trim() : "";
                var sms = smsEl ? smsEl.value.trim() : "";

                // This used to throw before reaching preventDefault (the ids did not resolve),
                // so the postback went through and an empty SMS was handed to Twilio.
                if (!mobile || !sms) {
                    e.preventDefault();
                    Swal.fire('Validation Error', 'Mobile number and SMS text cannot be empty.', 'warning');
                }
            });
        })();

        // ---------------------------------------------------------------------------
        // MMS
        // ---------------------------------------------------------------------------
        function OpenMMSPopUp(mobile, customerID) {
            // Server-side guard: Session["IsMMSAllowed"] used to be dereferenced directly in
            // the markup, so a session without that key rendered a NullReferenceException
            // instead of the page.
            var IsMMSAllowed = <%= IsMMSAllowedJs %>;

            if (!IsMMSAllowed) {
                Swal.fire('MMS disabled', 'MMS is not enabled for this account. Please contact Support.', 'warning');
                return;
            }
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            $("#txtCustMob").val(mobile);
            $("#txtCustId").val(customerID);
            $("#txtMMSBody").val('');
            $("#fuAttachment").val('');
            $("#fileError").text('');
            showBsModal('modalSendMMS');
        }
        function CloseMMSPopup() {
            $("#txtCustMob").val('');
            $("#txtCustId").val('');
            $("#txtMMSBody").val('');
            $("#fuAttachment").val('');
            $("#fileError").text('');
            hideBsModal('modalSendMMS');
        }

        (function () {
            var btn = document.getElementById("btnSendMMS");
            if (!btn) return;   // this getElementById returned null and its TypeError killed
                                // the rest of the script block, including the validator below
            btn.addEventListener("click", function (e) {
                var fileEl = document.getElementById("fuAttachment");
                var bodyEl = document.getElementById("txtMMSBody");
                var attachment = fileEl ? fileEl.value.trim() : "";
                var body = bodyEl ? bodyEl.value.trim() : "";

                if (!attachment || !body) {
                    e.preventDefault();
                    Swal.fire('Validation Error', 'MMS body and file cannot be empty.', 'warning');
                }
            });
        })();

        // Attachment type check for the MMS modal. Never ran before: it is registered after
        // the throw above, and it is a DOMContentLoaded handler in a script block that had
        // already been aborted.
        (function () {
            function bindAttachmentValidation() {
                var fileInput = document.getElementById("fuAttachment");
                var errorLabel = document.getElementById("fileError");
                if (!fileInput || !errorLabel) return;

                fileInput.addEventListener("change", function () {
                    errorLabel.innerText = "";
                    if (fileInput.files.length > 0) {
                        var fileName = fileInput.files[0].name.toLowerCase();
                        var allowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
                        var isValid = allowedExtensions.some(function (ext) {
                            return fileName.endsWith(ext);
                        });
                        if (!isValid) {
                            errorLabel.innerText = "Invalid file type! Only PDF, JPG, JPEG, PNG are allowed.";
                            fileInput.value = "";
                        }
                    }
                });
            }
            if (document.readyState === 'loading') {
                document.addEventListener("DOMContentLoaded", bindAttachmentValidation);
            } else {
                bindAttachmentValidation();
            }
        })();

        // ---------------------------------------------------------------------------
        // History
        // ---------------------------------------------------------------------------
        // OpenSMSHistory() was removed: it navigated to CustomerTextHistory.aspx, which does
        // not exist in TPM. CustomerChatHistory.aspx does, and is what OpenAllHistory uses.
        function OpenAllHistory(mobile, customerName, customerId) {
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            var url = "CustomerChatHistory.aspx?mobile=" + encodeURIComponent(mobile)
                + "&name=" + encodeURIComponent(customerName)
                + "&customerId=" + encodeURIComponent(customerId);

            window.location.href = url;
        }

    </script>

</asp:Content>
