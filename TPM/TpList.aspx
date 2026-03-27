<%@ Page Title="" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="TpList.aspx.cs" Inherits="TPM.TpList" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
      <%-- new for datable print --%>
    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/css/bootstrap-select.css" />
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.1.1/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.1/js/bootstrap-select.min.js"></script>
     <link rel="stylesheet" type="text/css" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.0.1/css/bootstrap.min.css" />
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/1.11.3/css/jquery.dataTables.min.css" />
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/buttons/2.0.1/css/buttons.dataTables.min.css" />

    <script src="https://cdn.datatables.net/buttons/2.0.1/js/dataTables.buttons.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jszip/3.1.3/jszip.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/pdfmake.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/vfs_fonts.js"></script>
    <script src="https://cdn.datatables.net/buttons/2.0.1/js/buttons.html5.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/2.0.1/js/buttons.print.min.js"></script>
    <script src="https://cdn.datatables.net/buttons/2.0.1/js/buttons.colVis.min.js"></script>
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
		.dataTables_scrollBody{
	        min-height: 400px;
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
                                    <asp:DropDownList ID="ddl_SearchFor" runat="server" class="form-select">
                                        <asp:ListItem Selected="True" Value="Customer">Customer</asp:ListItem>
                                        <asp:ListItem Value="Contact"> Contact</asp:ListItem>
                                        <asp:ListItem Value="Business"> Business</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="col-lg-2 mt-0">
                                    <h6>Search By</h6>
                                    <asp:DropDownList ID="SearchBy" runat="server" class="form-select">
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
                                    <select id="ddlTag" runat="server" title="Select Tags" style="width: 100%" class="form-control selectpicker" multiple="true" data-live-search="true">
                                    </select>
                                </div>
                                <div class="col-lg-4 mt-0">
                                    <h6>Search Value</h6>

                                    <asp:TextBox ID="SearchValue" ClientIDMode="Static" runat="server" class="form-control"></asp:TextBox>

                                </div>

                                <div class="col-lg-2 mt-0">
                                    <h6>&nbsp;</h6>
                                    <asp:Button ID="Search" ClientIDMode="Static" runat="server" OnClick="Search_Click" Text="Search" Width="20px" class="btn btn-secondary w-100" />

                                </div>


                                <div class="col-12" style="margin-top: 10px;display:none;">
                                    <div class="row">
                                        <div class="float-start">
                                            <p style="display: none" id="ProgressGIF">
                                                <img id="imgProcess" src="images/Rolling.gif" />
                                                Sync on progress....
                                            </p>
                                        </div>


                                    </div>
                                    <div style="float: left">
                                        <span class="btn btn-primary" title="QuickBooks Online Sync" runat="server" id="SyncQuickBook" onclick="SyncQuickBook()">QuickBooks Online Sync</span>
                                        <span style="display: none" class="btn btn-primary" title="AM Manager Sync" id="spn_AiremasterSync" runat="server" onclick="AMManagerSync()">Aire-Master Customer Sync</span>
                                    </div>
                                    <%--<div style="float:right">
                                         <asp:Button ID="btnConnect" runat="server" class="btn btn-primary" Text="Connect QuickBooks Online" OnClick="ConQuickBook_Click">
                                        </asp:Button>
                                     </div>--%>
                                </div>
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
                                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
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
                                        <asp:TextBox ID="txt_EmailSubject" Rows="2" TextMode="MultiLine" runat="server" class="form-control"></asp:TextBox>

                                    </div>
                                </div>
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label for="validationCustom01" class="form-label mb-0">Email Body :</label>
                                        <asp:TextBox ID="txt_EmailBody" Rows="5" TextMode="MultiLine" runat="server" class="form-control"></asp:TextBox>

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
                                <button type="button" class="btn btn-secondary" onclick="hideModal();" data-dismiss="modal">Close</button>
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

                                <asp:HiddenField ID="CustomerID" runat="server" />

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
                            <input type="text" id="txtMobile" class="form-control" runat="server" readonly />
                            <input runat="server" type="text" id="txtCustomerId" hidden />

                        </div>
                    </div>

                    <div class="row mt-2">
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


                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="txtCustMob" class="form-label mb-0">Mobile Number</label>
                            <input type="text" id="txtCustMob" class="form-control" runat="server" readonly />
                            <input runat="server" type="text" id="txtCustId" hidden />

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






    <script>
        $("#SearchValue").on("keypress", function(e) {
            if (e.keyCode == 13) {
                $("#Search").click();

            //alert("Enter pressed");
            //return false; // prevent the button click from happening
        }
});

         function AMManagerSync() {
            var prgBar = document.getElementById("ProgressGIF");
            prgBar.style.display = "block";
            $.ajax({
                type: "POST",
                url: "CustomerList.aspx/SyncCustomerAPItoDB",
                contentType: "application/json",
                dataType: "json",
                success: function (msg) {
                    alert(msg.d);
                    prgBar.style.display = "none";
                    window.location.href = "CustomerList.aspx?m=2&Type=Business";
                }
            });
        }

         function SyncQuickBook() {
            var prgBar = document.getElementById("ProgressGIF");
            prgBar.style.display = "block";
            $.ajax({
                type: "POST",
                url: "CustomerList.aspx/SyncCustomerToQBO",
                contentType: "application/json",
                dataType: "json",
                success: function (msg) {
                    alert(msg.d);
                    prgBar.style.display = "none";
                    window.location.href = "CustomerList.aspx";
                }
            });
        }

        $('#file').on('change', function () {
            var fd = new FormData();
            var filename = this.value;
            var lastIndex = filename.lastIndexOf("\\");
            var count = 0;
            if (lastIndex >= 0) {
                filename = filename.substring(lastIndex + 1);
            }
            var files = $('#file')[0].files;
            for (var i = 0; i < files.length; i++) {
                if (files[i].size < 4242880) {
                    $("#custom-file").append('<span>' + '<div class="filenameupload">' + files[i].name + ' abc</div>' + '<p class="close" >X</p></span>');
                    fd.append("file-" + (count++), files[i])
                }
            }
            var request = new XMLHttpRequest();
            request.open("POST", "/path/to/server", true);
            request.send(fd);
            fileCount = Array.from(fd.keys()).length;
            showFileCount();
        });
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



        function OpenSurveyMailPopUp(emailto, customerID) {
            $("#txt_EmailTO").val(emailto);
            $("#txt_CustID").val(customerID);

            showModal();
        }
        function showModal() {

            $('#SurveyMail').modal('show');
        }
        function hideModal() {
            $('#SurveyMail').modal('hide');
            $('.modal-backdrop').remove();

        }
        function FillSurveyEmailBody() {
            var id = $("#optSurvey").val();
            var customerid = $("#txt_CustID").val();


            $.ajax({
                url: 'CustomerList.aspx/optSurvey_Changed',
                type: "POST",
                contentType: 'application/json',
                data: "{SurveyID:'" + id + "',CustomerID:'" + customerid + "'}",
                dataType: 'json',
                success: function (sR) {
                    console.log(sR)
                    $("#txt_EmailSubject").val(sR.d[0]);
                    $("#txt_EmailBody").val(sR.d[1]);

                    //  alert($("#_From").val())

                },
                error: function (error) {
                    alert(error);
                }
            })

        }
        function Add_new_Customer() {
            window.location.href = "customerDetail.aspx?m=0&cid=0&Mode=Add";
        }

        function ShowPopup() {

            $("#modalCustomerMail").modal('show');
        }

        function ClosePopup() {

            $("#modalCustomerMail").modal('hide');
        }
        function OpenMailPopUp(CustomerID) {
            FillEmailBody(CustomerID);
            $("#CustomerID").val(CustomerID);
            // $("#_To").val(emailTo);



        }

        function FillEmailBody(customerid) {
            //  alert("caled");
            $.ajax({
                url: 'CustomerList.aspx/FillEmailModal',
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

                } else if (result.isDenied) {
                    evt.preventDefault();

                }
                //return true;
            })

        });

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

        function initializeSelectPicker() {
            try {
                var $ddlTag = $('#<%= ddlTag.ClientID %>');
                if ($ddlTag.length > 0) {
                    console.log('Initializing selectpicker for ddlTag, options count: ' + $ddlTag.find('option').length);
                    
                    // Destroy existing selectpicker if it exists
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
            
            ClosePopup()
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
                    columns: [1, 2, 3, 4, 5, 6]
                }
                    },
                    {
                        extend: 'print',
                        exportOptions: {
                            columns: ':visible'
                        },
                        className: 'datatableButton',
                         exportOptions: {
                    columns: [1, 2, 3, 4, 5, 6]
                }
                    }


                ],


            });

            if ($("#SearchBy").val() == "All") {
                $("#SearchValue").val("")
                $("#SearchValue").prop('disabled', true);
            }
        })
        
       $("#ddl_SearchFor").change(function () {
            if ($("#ddl_SearchFor").val() == "Business") {
              
            } else {
                $("#SearchValue").prop('disabled', false);
            }
        });

        $("#SearchBy").change(function () {
            if ($("#SearchBy").val() == "All") {
                $("#SearchValue").val("")
                $("#SearchValue").prop('disabled', true);
            } else {
                $("#SearchValue").prop('disabled', false);
            }
        });
         function OpenSMSPopUp(mobile, customerID) {
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            $("#txtMobile").val(mobile);
            $("#txtCustomerId").val(customerID);
            $('#modalSendSMS').modal('show');
        }
        function CloseSMSPopup() {
            $("#txtMobile").val('');
            $("#txtCustomerId").val('');
            $("#txtSMS").val('');
            $("#modalSendSMS").modal('hide');
        }

        document.getElementById("btnSendSms").addEventListener("click", function (e) {
            var mobile = document.getElementById("txtMobile").value.trim();
            var sms = document.getElementById("txtSMS").value.trim();

            if (!mobile || !sms) {
                Swal.fire('Validation Error', 'Mobile number and SMS text cannot be empty.', 'warning');
                e.preventDefault(); // Stop form submission
            }
        });


        function OpenMMSPopUp(mobile, customerID) {
            
             var IsMMSAllowed = <%= Session["IsMMSAllowed"].ToString().ToLower() %>;
        
            if (!IsMMSAllowed) {
                ShowCustomAlert('\MS disabled.Please contact Support');
                return;
            }

            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            $("#txtCustMob").val(mobile);
            $("#txtCustId").val(customerID);
            $('#modalSendMMS').modal('show');
        }
        function CloseMMSPopup() {
            $("#txtCustMob").val('');
            $("#txtCustId").val('');
            $("#txtMMSBody").val('');
            $("#fuAttachment").val('');
            $("#modalSendMMS").modal('hide');
        }

        document.getElementById("btnSendMMS").addEventListener("click", function (e) {
            var mobile = document.getElementById("fuAttachment").value.trim();
            var sms = document.getElementById("txtMMSBody").value.trim();

            if (!mobile || !sms) {
                Swal.fire('Validation Error', 'MMS body and file cannot be empty.', 'warning');
                e.preventDefault(); // Stop form submission
            }
        });

        document.addEventListener("DOMContentLoaded", function () {
            var fileInput = document.getElementById("<%= fuAttachment.ClientID %>");
            var errorLabel = document.getElementById("fileError");

            fileInput.addEventListener("change", function () {
                errorLabel.innerText = ""; // clear previous error

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


        function OpenSMSHistory(mobile, customerName, customerId) {
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add mobile number for this customer.', 'warning');
                return;
            }
            var url = "CustomerTextHistory.aspx?mobile=" + encodeURIComponent(mobile)
                + "&name=" + encodeURIComponent(customerName)
                + "&customerId=" + encodeURIComponent(customerId);

            window.location.href = url;
        }


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

