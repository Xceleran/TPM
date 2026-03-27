<%@ Page Title="ChatHistory" Language="C#" MasterPageFile="~/FSM.Master" AutoEventWireup="true" CodeBehind="CustomerChatHistory.aspx.cs" Inherits="FSM.CustomerChatHistory" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- jQuery must come first -->
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>

    <!-- DataTables core -->
    <link rel="stylesheet" href="https://cdn.datatables.net/1.13.6/css/jquery.dataTables.min.css" />
    <script src="https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js"></script>

    <!-- DataTables Responsive -->
    <link rel="stylesheet" href="https://cdn.datatables.net/responsive/2.5.0/css/responsive.dataTables.min.css" />
    <script src="https://cdn.datatables.net/responsive/2.5.0/js/dataTables.responsive.min.js"></script>

    <style>
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

        .custom-file-input-test {
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

        #example td:nth-child(2) {
            white-space: normal !important;
            word-wrap: break-word;
            /*overflow: hidden;
            text-overflow: ellipsis;
            -webkit-line-clamp: 3;*/ /* show max 3 lines */
            /*-webkit-box-orient: vertical;*/
        }

        table#example.dataTable tbody tr.row-delivered > td {
            background-color: #e8fbe8 !important; /* soft green */
        }

        table#example.dataTable tbody tr.row-received > td {
            background-color: #e8f0fb !important; /* soft blue */
        }

        /* Optional: if Bootstrap .table-striped still shows through on odd rows, neutralize it */
        table#example.table.table-striped > tbody > tr.row-delivered:nth-of-type(odd) > td,
        table#example.table.table-striped > tbody > tr.row-received:nth-of-type(odd) > td {
            background-color: inherit !important;
        }
    </style>

    <br />
    <div class="d-flex flex-column-fluid home-1stsec">
        <div class="container">
            <div class="row">
                <div class="col-12 mb-3">
                    <div class="card card-custom gutter-b card-stretch p-0">
                        <div class="card-header bg-light">
                            <div class="card-title d-flex justify-content-between align-items-center w-100">
                                <div class="float-start d-flex align-items-center">
                                    <h5 id="lblHeader" runat="server" class="mr-3"><i class="fas fa-sms" style="color: #5cb85c; font-size: 28px;"></i>Text Message History</h5> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                    <button type="button" class="btn btn-success mr-2" data-dismiss="modal" onclick="OpenSendSMSPopUp()"> Send SMS</button>
                                    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                    <button type="button" class="btn btn-success" data-dismiss="modal" onclick="OpenMMSPopUp()">Send MMS</button>
                                </div>


                                <asp:HiddenField ID="txtMobile" runat="server" />
                                <asp:HiddenField ID="txtCustomerId" runat="server" />
                                <asp:HiddenField ID="txtCustomerName" runat="server" />
                            </div>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-lg-12 mt-3">
                                    <div class="col-12 table-responsive">
                                        <table id="example" class="display table table-striped table-bordered nowrap" style="width: 100%">
                                            <thead>
                                                <tr>
                                                    <th>Send Date Time</th>
                                                    <th>Message Body</th>
                                                    <th>File</th>
                                                    <th>Status</th>
                                                </tr>
                                            </thead>
                                        </table>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <hr>
                    <div class="row">
                        <div class="col-sm-12">
                            <button type="button" class="btn btn-warning " onclick="BackToCustomer()" style="float: left">Back</button>
                        </div>
                    </div>
                </div>

                <!-- Send SMS Modal -->
                <div class="modal fade" id="modalSendSMS" tabindex="-1" role="dialog" aria-labelledby="modalSendSMS" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-scrollable" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="smsModalLongTitle">Send SMS</h5>
                            </div>
                            <div class="modal-body">
                                <div class="row mt-2">
                                    <div class="col-12 mt-0">
                                        <label class="col-sm-2 col-form-label"><span>Text</span></label>
                                        <div class="col-sm-12">
                                            <textarea id="txtSMS" name="SMSBody" rows="5" runat="server" class="form-control"></textarea>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer justify-content-between">
                                <button type="button" class="btn btn-warning" data-dismiss="modal" onclick="CloseSMSPopup()">Close</button>
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
                                <asp:Button ID="btnSendMMS" ClientIDMode="Static" runat="server" Text="Send MMS" CssClass="btn btn-success text-nowrap" OnClick="btnSendMMS_Click" />
                            </div>
                        </div>
                    </div>
                </div>




            </div>
        </div>
    </div>

    <script>
        $(document).ready(function () {
            var mob = $("#MainContent_txtMobile").val();
            var custId = $("#MainContent_txtCustomerId").val();
            debugger;
            $('#example').DataTable({
                "processing": true,
                "serverSide": false,
                "order": [[0, "desc"]],
                "responsive": false,
                "autoWidth": false,
                "ajax": {
                    "url": "CustomerChatHistory.aspx/GetMessages",
                    "type": "POST",
                    "contentType": "application/json; charset=utf-8",
                    "dataType": "json",
                    "data": function () {
                        return JSON.stringify({
                            mobile: mob,
                            customerId: custId
                        });
                    },
                    "dataSrc": function (json) {
                        console.log("Raw response:", json);
                        return json.d.data;
                    }
                },
                "columns": [
                    { "data": "SendDateTime" },
                    {
                        "data": "Body",
                        "render": function (data) {
                            return "<div class='wrap-text'>" + (data || "") + "</div>";
                        }
                    },
                    {
                        "data": "File",
                        "render": function (data) {
                            return data ? "<a href='" + data + "' target='_blank'><i class='fa fa-eye' style='color:#007bff; font-size:20px;'></i></a>" : "";
                        }
                    },
                    { "data": "Status" }
                    //{
                    //    "data": null,
                    //    "render": function (data, type, row) {
                    //        return "<button type='button' class='btn btn-success' onclick='OpenSendSMSPopUp(\"" + row.Mobile + "\", \"" + row.CustomerId + "\")'><i class='fa fa-comment-dots'></i></button>";
                    //    }
                    //}
                ],
                rowCallback: function (row, data) {
                    const status = (data.Status || '').toString().trim().toLowerCase();
                    row.classList.remove('row-delivered', 'row-received');
                    if (status === 'delivered') row.classList.add('row-delivered');
                    if (status === 'received') row.classList.add('row-received');
                }
            });
        });

        function BackToCustomer() {
            window.location.replace('Customer.aspx');
        }
        function OpenSendSMSPopUp() {
            var mobile = $("#MainContent_txtMobile").val();
           
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
                e.preventDefault(); // Stop form submission
            }
        });


        function OpenMMSPopUp() {
            var mobile = $("#MainContent_txtMobile").val();
            if (!mobile) {
                Swal.fire('Validation Error', 'Please add phone number for this customer.', 'warning');
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



    </script>
</asp:Content>

