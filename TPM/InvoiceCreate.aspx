<%@ Page Title="Invoices" Language="C#" MasterPageFile="~/FSM.Master" AutoEventWireup="true" CodeBehind="InvoiceCreate.aspx.cs" Inherits="FSM.InvoiceCreate" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css" integrity="sha512-1ycn6IcaQQ40/MKBW2W4Rhis/DbILU74C1vSrLJxCq57o941Ym01SwNsOMqvEBFlcgUa6xLiPY/NS5R+E6ztJQ==" crossorigin="anonymous" referrerpolicy="no-referrer" />
    <style>
        /* Main Invoice Container */
        .invoice-container {
            padding: 20px;
            margin-top: 50px;
        }
        /* Card Container */
        .card.card-custom {
            overflow: hidden;
            margin-bottom: 20px;
            border-radius: 8px;
            box-shadow: var(--shadow-lg);
            background: #f1f1f1;
        }

        [data-theme="dark"] .card.card-custom {
            background: var(--bg-card);
            box-shadow: rgb(0 0 0) 3px 3px 6px 0px inset, rgb(7 44 77 / 50%) -3px -3px 6px 1px inset;
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        /* Row containing buttons */
        .save-button-row {
            display: flex;
            justify-content: flex-end;
            align-items: center;
            gap: 15px;
            margin-top: 20px;
            padding: 0 10px;
        }

        /* Save button container */
        .save-button-container {
            display: flex;
            justify-content: flex-end;
            align-items: center;
            padding: 0;
            margin: 0;
            gap: 10px;
        }

        /* Save button styling */
        #MainContent__SubmitInvoice {
            width: 100%;
            max-width: 120px;
            padding: 8px 16px;
            font-size: 14px;
            font-weight: 600;
            color: #fff;
            background-color: rgb(94 124 253);
            border: none;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin: 0 !important;
        }

            #MainContent__SubmitInvoice:hover {
                background-color: #4c62c4;
                border: none;
                box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
            }

            #MainContent__SubmitInvoice:active {
                background-color: #4caf50;
                transform: translateY(1px);
            }

        /* Table container adjustments */
        .table-responsive {
            margin-bottom: 20px;
            overflow-x: auto;
        }

        /* Ensure content stays within card */
        .d-flex.flex-column-fluid.home-1stsec .container {
            max-width: 100%;
            padding: 0 15px;
            width: 100%;
        }

        /* Fix for potential overflow from parent styles */
        .col-12.bgtopin, .col-12.bgbotin {
            position: absolute;
            width: 100%;
            z-index: 0;
        }

        .col-12.cnttop {
            position: relative;
            z-index: 1;
        }

        /* Page Title */
        .page-title {
            font-size: 32px;
            font-weight: bold;
            color: #265598;
            padding-left: 18px;
        }

        [data-theme="dark"] .page-title {
            color: rgb(255, 255, 255);
        }

        /* Delete Icon */
        .delete-icon {
            color: #dc3545;
            font-size: 18px;
            text-decoration: none;
            transition: color 0.2s ease;
            display: inline-block;
        }

            .delete-icon:hover {
                color: #a71d2a;
            }

        [data-theme="dark"] .delete-icon {
            color: #ff6666;
        }

            [data-theme="dark"] .delete-icon:hover {
                color: #ff9999;
            }

        /* General Text Colors */
        .poppinsbold, .poppinsmed, label, h5, h6 {
            color: var(--text-primary);
        }



        [data-theme="dark"] .form-control,
        [data-theme="dark"] .form-select,
        [data-theme="dark"] textarea {
            background: var(--input-bg);
            border: 1px solid var(--input-border);
            color: var(--input-text);
        }

        /* Adjust Select Dropdown Arrow for Dark Mode */
        .form-select {
            appearance: none;
            background-image: url('data:image/svg+xml;utf8,<svg fill="gray" height="20" viewBox="0 0 24 24" width="20" xmlns="http://www.w3.org/2000/svg"><path d="M7 10l5 5 5-5z"/></svg>') !important;
            background-repeat: no-repeat !important;
            background-position: right 0.5rem center !important;
            background-size: 1rem !important;
        }

        [data-theme="dark"] .form-select {
            background-image: url('data:image/svg+xml;utf8,<svg fill="rgb(239,242,247)" height="20" viewBox="0 0 24 24" width="20" xmlns="http://www.w3.org/2000/svg"><path d="M7 10l5 5 5-5z"/></svg>') !important;
            background: #414e64;
        }

        [data-theme="dark"] h6.poppinsmed.m-0 {
            color: #cacaca !important;
        }

        .tax-toggle-container {
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100%;
        }

        .toggle-switch {
            position: relative;
            display: inline-block;
            width: 50px;
            height: 28px;
        }

            .toggle-switch input {
                opacity: 0;
                width: 0;
                height: 0;
            }

        .slider-tax {
            position: absolute;
            cursor: pointer;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: #ccc;
            transition: .4s;
            border-radius: 28px;
        }

            .slider-tax:before {
                position: absolute;
                content: "";
                height: 20px;
                width: 20px;
                left: 4px;
                bottom: 4px;
                background-color: white;
                transition: .4s;
                border-radius: 50%;
            }

        input:checked + .slider-tax {
            background-color: #5e7cfd !important;
        }

            input:checked + .slider-tax:before {
                transform: translateX(22px);
            }

        .form-control, .form-select, textarea, input[type='text'], input[type='number'], input[type='date'] {
            background: #ffffff;
            border: 1px solid #a9a9a9;
            color: var(--input-text);
            border-radius: 6px;
            padding: 8px;
        }

        .card.card-custom {
            position: relative;
            overflow: hidden;
            margin-bottom: 20px;
            border-radius: 8px;
            box-shadow: var(--shadow-lg);
        }

        .col-12.bgtopin, .col-12.bgbotin {
            position: absolute;
            width: 100%;
            left: 0; /* Ensure it aligns to the card's edge */
            z-index: 0;
            margin-top: 0; /* Remove negative margin */
        }

        .col-12.bgtopin {
            top: 0;
        }

        .col-12.bgbotin {
            bottom: 0;
            margin-top: 0; /* Remove negative margin */
        }

        .table {
            background: var(--table-bg);
            color: var(--table-text);
            border: 1px solid var(--table-border);
            border-radius: 8px; /* Optional: adds rounded corners */
        }


            .table thead th {
                color: var(--table-text);
            }

        /* Labels and Messages */
        .text-warning {
            color: #ffc107 !important;
        }

        [data-theme="dark"] .text-warning {
            color: #ffdb58 !important;
        }

        /* Modal Styling */
        .modal-content {
            background: var(--bg-white);
            color: var(--text-primary);
        }

        [data-theme="dark"] .modal-content {
            background: rgba(255, 255, 255, 0.12);
            border: 1px solid rgba(255, 255, 255, 0.1);
            color: var(--text-gray-700);
        }

        /* Buttons in Modal */
        .btn-secondary {
            background: var(--bg-gray-300);
            color: var(--text-gray-800);
        }

        [data-theme="dark"] .btn-secondary {
            background: rgba(255, 255, 255, 0.12);
            border: 1px solid rgba(255, 255, 255, 0.1);
            color: var(--text-gray-700);
        }

        [data-theme="dark"] .aie {
            color: #b3b3b3 !important;
            font-weight: 500;
        }
        /* Specific Elements */
        #MainContent_lblmessage {
            color: #339966;
        }

        [data-theme="dark"] #MainContent_lblmessage {
            color: #66cc99;
        }

        #MainContent_txtFirstName,
        #MainContent_PageTitle {
            color: #566A98;
        }

        [data-theme="dark"] #MainContent_txtFirstName,
        [data-theme="dark"] #MainContent_PageTitle {
            color: var(--text-gray-700) !important;
        }

        .deposit-switch-container {
            display: flex;
            align-items: center;
            gap: 10px;
            background-color: #ffffff;
            border-radius: 20px;
            width: fit-content;
            position: relative;
            border: 1px solid rgb(94 124 253);
        }

        [data-theme="dark"] .deposit-switch-container {
            background-color: #2b384e;
        }

        .deposit-switch-option {
            padding: 6px 16px;
            cursor: pointer;
            font-weight: 500;
            color: #495057;
            z-index: 2;
            transition: color 0.3s ease;
            border-radius: 18px;
        }

        [data-theme="dark"] .deposit-switch-option {
            color: #adb5bd;
        }

        .deposit-switch-option.active {
            color: #fff;
        }

        [data-theme="dark"] .deposit-switch-option.active {
            color: #e9ecef;
        }

        .deposit-switch-slider {
            position: absolute;
            top: 0px;
            left: 4px;
            height: calc(100% - 0px );
            background-color: rgb(94 124 253);
            border-radius: 18px;
            transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
            z-index: 1;
        }

        .request-deposit {
            display: flex;
            gap: 10px;
            justify-content: end;
            align-items: baseline;
        }
        .table>:not(caption)>*>*{
            background: #f1f1f1 !important;
        }
    </style>

    <div class="invoice-container">
        <header class="mb-4">
            <div class="row align-items-center">
                <div class="col-md-6">
                    <h1 class="page-title mb-3 mb-md-0">Invoices/Estimates</h1>
                </div>
            </div>
        </header>

        <%-- Proposal Modal Start --%>
        <div class="modal fade" id="modalCustomerMail" tabindex="-1" role="dialog" aria-labelledby="modalCustomerMail" aria-hidden="true" style="z-index: 1000000000000000">
            <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="exampleModalLongTitle">Send Email</h5>
                    </div>
                    <div class="modal-body">
                        <div id="imgSpinner1" class="row">
                            <div class="float-start">
                                <p>
                                    <img id="imgProcess" src="images/Rolling.gif" />
                                    Loading....
                                </p>
                            </div>
                        </div>
                        <asp:HiddenField ID="CustomerID" runat="server" />
                        <asp:HiddenField ID="IsDataSaving" Value="0" runat="server" />
                        <asp:HiddenField ID="EnableSaveOnly" Value="0" runat="server" />
                        <asp:HiddenField ID="_CC" runat="server" />
                        <asp:HiddenField ID="_InvoiceNo" runat="server" />
                        <asp:HiddenField ID="hf_CustomerQboID" Value="0" runat="server" />
                        <asp:HiddenField ID="hf_CurrentPdfType" ClientIDMode="Static" Value="AsaInvoice" runat="server" />
                        <div class="row mt-0">
                            <div class="col-12 mt-0">
                                <label for="_To" class="col-form-label">To</label>
                                <div class="col-lg-12">
                                    <input type="text" id="_To" name="_To" runat="server" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="_BCC" class="col-form-label">Bcc</label>
                                <div class="col-lg-12">
                                    <input type="text" id="_BCC" name="BCC" runat="server" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="EmailSubject" class="col-form-label">Subject</label>
                                <div class="col-lg-12">
                                    <input type="text" id="_EmailSubject" name="EmailSubject" runat="server" class="form-control col-6" max="500" />
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <div class="col-lg-12">
                                    <div class="custom-file">
                                        <asp:FileUpload ID="file" ClientIDMode="Static" runat="server" AllowMultiple="true" MaxLength="100" CssClass="custom-file-input" onchange="javascript:updateList()" />
                                        <label class="custom-file-label" for="file">
                                            <span class="fa fa-paperclip"></span>Attach Files</label><br />
                                        <span class="remove-list fa fa-check"></span><span id="fixAttachment"></span>
                                        <ul id="fileList" class="file-list">
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="container parent">
                                <div class="row">
                                    <div class='col text-center'>
                                        <input type="radio" checked="checked" name="imgbackground" id="img1" class="d-none imgbgchk" value="AsaInvoice">
                                        <label for="img1">
                                            Template 1<img src="images/AsaInvoice.jpg" alt="Invocie 1">
                                            <div class="tick_container">
                                                <div class="tick"><i class="fa fa-check"></i></div>
                                            </div>
                                        </label>
                                    </div>
                                    <div class='col text-center'>
                                        <input type="radio" name="imgbackground" id="img2" class="d-none imgbgchk" value="AsaQuote">
                                        <label for="img2">
                                            Template 2
                                                            <img src="images/AsaQuote.jpg" alt="Estimate 1">
                                            <div class="tick_container">
                                                <div class="tick"><i class="fa fa-check"></i></div>
                                            </div>
                                        </label>
                                    </div>
                                    <div class='col text-center'>
                                        <input type="radio" name="imgbackground" id="img3" class="d-none imgbgchk" value="Invoice">
                                        <label for="img3">
                                            Template 3
                                                            <img src="images/Invoice.jpg" alt="Image 3">
                                            <div class="tick_container">
                                                <div class="tick"><i class="fa fa-check"></i></div>
                                            </div>
                                        </label>
                                    </div>
                                    <div class='col text-center'>
                                        <input type="radio" name="imgbackground" id="img5" class="d-none imgbgchk" value="InvoiceWithoutDiscount">
                                        <label for="img5">
                                            Template 4
                                                            <img src="images/InvWithoutDis.PNG" alt="Image 3">
                                            <div class="tick_container">
                                                <div class="tick"><i class="fa fa-check"></i></div>
                                            </div>
                                        </label>
                                    </div>
                                    <div class='col text-center'>
                                        <input type="radio" title="Template 5" name="imgbackground" id="img4" class="d-none imgbgchk" value="Estimate">
                                        <label for="img4">
                                            Template 5
                                                              <img src="images/Estimate.jpg" alt="Image 4">
                                            <div class="tick_container">
                                                <div class="tick"><i class="fa fa-check"></i></div>
                                            </div>
                                        </label>
                                    </div>
                                </div>
                            </div>
                        </div>


                        <div class="row mt-2">
                            <div class="col-12 mt-0">
                                <label for="EmailBody" class="col-form-label"><span>Email Body</span></label>
                                <div class="col-lg-12">
                                    <textarea id="EmailBody" name="EmailBody" rows="8" runat="server" class="form-control"></textarea>
                                    <p for="validationCustom01" class=" text-warning mb-0">Please Save first before you sent mail.</p>
                                </div>
                            </div>

                        </div>
                        <div class="row mt-2">
                            <div class="col-12 mt-0" id="IsSendPaymentLinkDIV">
                                <div class="form-check form-check-xl">
                                    <input type="checkbox" class="form-check-input" id="IsSendPaymentLink" style="width: 1.5em; height: 1.5em;" runat="server">
                                    <label class="form-check-label" for="exampleCheckbox" style="font-size: 1.3em;">Include Payment Link</label>
                                </div>

                            </div>
                        </div>

                    </div>
                    <div class="modal-footer justify-content-between">
                        <button type="button" class="btn btn-secondary  btn-block ml-1" data-dismiss="modal" onclick="ClosePopup()" style="float: left">Close</button>
                        <%--                        <asp:Button ID="btn_PrintPdf" runat="server" Text="Print" CssClass="btn btn-secondary text-nowrap" OnClick="btn_PrintPdf_Click" />&nbsp;&nbsp;
                                    <asp:Button ID="btnSendProposalMail" runat="server" Text="Send" CssClass="btn btn-secondary text-nowrap" OnClientClick="return sendProposal();" OnClick="btnSendProposalMail_Click" />
                        <asp:Button ID="btnSendInvoiceMail" runat="server" Text="Send" CssClass="btn btn-secondary text-nowrap" OnClientClick="return sendInvoice();" OnClick="btnSendInvoiceMail_Click" />--%>
                    </div>
                </div>
            </div>
        </div>

        <asp:HiddenField ID="_Type" runat="server" />
        <%--End Proposal Modal  --%>
        <div class="d-flex flex-column-fluid home-1stsec">
            <div class="container">
                <div class="row">
                    <div class="col-12 mb-3">
                        <div class="card card-custom gutter-b card-stretch">
                            <input type="hidden" id="ItemOpts" />
                            <div class="col-12 bgtopin" style="background-image: url(./crv_sched/img/invoice/bgtopin.png); background-position: center center; background-size: 100% 100%; width: 100%;">
                            </div>
                            <div class="col-12 cnttop">
                                <asp:HiddenField ID="hdCompanyID" runat="server" />
                                <asp:HiddenField ID="hdCompanyName" runat="server" />
                                <asp:HiddenField ID="hdCompanyTag" runat="server" />
                                <asp:HiddenField ID="PrvTotal" runat="server" ClientIDMode="Static" />

                                <asp:HiddenField ID="hdCompanyGUID" runat="server" />
                                <asp:HiddenField ID="SV_CustomeID" runat="server" />
                                <asp:HiddenField ID="hfCustomerIdInt" runat="server" />

                                <asp:HiddenField ID="hfSiteId" runat="server" ClientIDMode="Static" />
                                <asp:HiddenField ID="AppointmentID" runat="server" />
                                <asp:HiddenField ID="Indicator" runat="server" />
                                <asp:HiddenField ID="hd_IsQuickBookEnabled" Value="False" runat="server" />

                                <asp:HiddenField ID="HdInvoiceId" runat="server" />
                                <asp:HiddenField ID="HdQboId" runat="server" />
                                <asp:HiddenField ID="hf_QboEstimateId" runat="server" />
                                <div class="row">
                                    <div class="col-12 px-4 py-2">
                                        <div class="row">
                                            <div class="col-12 mt-0">
                                                <asp:Label ID="lblmessage" runat="server" Visible="False" ForeColor="#339966"></asp:Label>
                                            </div>
                                            <div class="col-12">
                                                <h1 class="poppinsbold">
                                                    <asp:Label ForeColor="#566A98" ID="PageTitle" runat="server"></asp:Label></h1>
                                                <hr />
                                                <div class="row">
                                                    <div class="col-lg-2">
                                                        <label for="Date">Invoice Date :</label>
                                                        <asp:TextBox ID="Date" ClientIDMode="Static" runat="server" class="form-control datefield "></asp:TextBox>
                                                    </div>
                                                    <div class="col-lg-2 " id="div_Expirationdate" runat="server">
                                                        <label for="ExpirationDate">Expiration Date :</label>
                                                        <asp:TextBox ID="ExpirationDate" ClientIDMode="Static" runat="server" class="form-control datefield "></asp:TextBox>
                                                    </div>
                                                    <div class="col-lg-2">
                                                        <label for="txt_InvocieNumber">Number:</label>
                                                        <asp:TextBox ID="txt_InvocieNumber" ClientIDMode="Static" ReadOnly runat="server" class="form-control "></asp:TextBox>
                                                    </div>
                                                </div>
                                                <br />
                                                <div class="row g-2  align-items-center">
                                                </div>
                                                <br />
                                                <div class="row g-3 align-items-center" style="display: none;">
                                                    <div class="col-auto">
                                                        <label for="validationCustom01" class="form-label mb-0">Quantitation Number</label>
                                                    </div>
                                                    <div class="col-auto">
                                                        <asp:TextBox ID="Number" runat="server" class="form-control  "></asp:TextBox>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-12 mt-4 aie">
                                                <h5 class="poppinsbold">
                                                    <asp:Label ForeColor="#566A98" ID="txtFirstName" runat="server"></asp:Label></h5>
                                                <asp:Label ID="Address" runat="server" Style="font-size: 15px;"></asp:Label>
                                                <br />
                                                <asp:Label ID="Contact" runat="server" Style="font-size: 15px;"></asp:Label>
                                            </div>
                                            <div class="col-12 mt-0">
                                                <%-- New Grid Start--%>
                                                <div class="col-12 mt-5">
                                                    <div class="table-responsive">

                                                        <table class="table table-bordered">
                                                            <thead style="font-weight: normal; color: black;">
                                                                <tr>
                                                                    <th>#</th>
                                                                    <th>Product/Service</th>
                                                                    <th>Description</th>
                                                                    <th>Service Date</th>
                                                                    <th>Qty</th>
                                                                    <th>Rate</th>
                                                                    <th>Amount</th>
                                                                    <th>Tax</th>
                                                                    <th></th>
                                                                </tr>
                                                            </thead>
                                                            <tbody id="tblNewInvoice">
                                                            </tbody>
                                                            <tr align='right' style="border: 0px">
                                                                <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                    <h6 class="poppinsmed m-0" style="color: #41464b;">Sub Total</h6>
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invSubTotal' runat="server" readonly style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td colspan="2" style="border: 0px"></td>
                                                            </tr>
                                                            <tr style="border: 0px;">
                                                                <td colspan="5" align='right' style="border: 0px;">
                                                                    <select id="optDiscount" runat="server" style="width: 150px" class="form-select" onchange='CalDiscount()'>
                                                                        <option value="1">Discount(%)</option>
                                                                        <option value="2">Discount($)</option>
                                                                    </select>
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invDiscountRate' runat="server" style="width: 60px" class="form-control" value='' onchange='CalDiscount()' />
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invDiscountVal' runat="server" style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td colspan="2" style="border: 0px"></td>
                                                            </tr>
                                                            <tr align='right' style="border: 0px">
                                                                <td colspan="5" style="border: 0px">
                                                                    <select id="optTaxRate" runat="server" style="width: 150px" class="form-select" onchange='FillTaxRate(this.value)'></select></td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invTaxRate' readonly runat="server" style="width: 60px" class="form-control" value='' />
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invTaxTotal' readonly runat="server" style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td colspan="2" style="border: 0px"></td>
                                                            </tr>
                                                            <tr align='right' style="border: 0px;">
                                                                <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                    <h6 class="poppinsmed m-0" style="color: black">Total</h6>
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='invTotal' runat="server" readonly style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td colspan="2" style="border: 0px"></td>
                                                            </tr>
                                                            <tr align='right' style="border: 0px;">
                                                                <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                    <h6 class="poppinsmed m-0" style="color: black">Payment</h6>
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='txt_Deposit' runat="server" readonly style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td style="border: 0px; text-align: left; padding-left: 5px;">
                                                                    <%-- Upgraded Button --%>
                                                                    <button id="btn_viewDeposite" onclick="ViewDepositList()" type="button" class="btn btn-sm btn-outline-secondary">
                                                                        <i class="fas fa-list-ul" style="padding: 6px; font-size: 18px;"></i>
                                                                    </button>
                                                                </td>
                                                                <td style="border: 0px">&nbsp;</td>
                                                            </tr>
                                                            <tr align='right' style="border: 0px;">
                                                                <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                    <h6 class="poppinsmed m-0" style="color: black">Due</h6>
                                                                </td>
                                                                <td align='center' style="border: 0px">
                                                                    <input type='text' id='txt_Due' runat="server" readonly style="width: 100px" class="form-control" value='' />
                                                                </td>
                                                                <td colspan="2" style="border: 0px"></td>
                                                            </tr>
                                                        </table>

                                                        <div class="request-deposit">
                                                            <div class="col-auto">
                                                                <h6 class="poppinsmed m-0" style="color: #41464b;">Requested Deposit Amount</h6>
                                                            </div>
                                                            <div class="col-auto">
                                                                <div class="deposit-switch-container">
                                                                    <div class="deposit-switch-slider"></div>
                                                                    <div class="deposit-switch-option active" data-value="1">%</div>
                                                                    <div class="deposit-switch-option" data-value="2">$</div>
                                                                </div>
                                                            </div>
                                                            <div class="col-auto" style="padding-left: 0;">
                                                                <input type='text' id='txtReqAmount' runat="server" style="width: 100px;" class="form-control" value='' onchange='CalRequestedDepoAmount()' />
                                                            </div>
                                                            <asp:HiddenField ID="hfRequestedAmtType" runat="server" ClientIDMode="Static" Value="1" />
                                                        </div>

                                                    </div>
                                                </div>
                                                <div class="col-12 mt-4">

                                                    <label for="lblPONO">PO :</label>
                                                    <%--   <asp:Label ID="lblPONO"  runat="server" ></asp:Label>--%>
                                                    <asp:Label ID="lblPONO" runat="server" Style="font-size: 15px;"></asp:Label>
                                                </div>
                                                <div class="col-12 col-sm-12 col-md-8 mt-5">
                                                    <h5 class="poppinsmed">
                                                        <label for="dtfrom">Notes :</label></h5>
                                                    <asp:TextBox ID="Note" TextMode="MultiLine" runat="server" class="form-control mb-5" Rows="4"></asp:TextBox>
                                                </div>

                                                <div class="col-12">
                                                    <div id="invPermission" runat="server" class="row">
                                                        <div class="col-lg-12 mt-2 text-warning text-lg-start ">
                                                            <h4>You do not have permission to Add/Edit Invoice.</h4>
                                                        </div>
                                                    </div>
                                                    <div class="col-lg-12 mt-2 save-button-container">
                                                        <div class="">
                                                            <a id="btnBack" href="#" class="btn btn-outline-secondary w-100">Back</a>
                                                        </div>
                                                        <input type="button" runat="server" id="_SubmitInvoice" value="Save" class="btn btn-secondary btn-transparentdark mt-3 w-100" onclick="submitInvoice()" />
                                                    </div>
                                                    <div class="row justify-content-start save-button-row">

                                                        <div class="col-lg-4 mt-2">
                                                            <input type="button" runat="server" id="SendMail" value="E-Mail/Print" class="btn btn-secondary btn-transparentdark mt-3 w-100 d-none" onclick="OpenMailPopUp()" />
                                                        </div>
                                                        <div class="col-lg-4 mt-2">
                                                            <p style="display: none" id="ProgressGIF">
                                                                <img id="imgProcess" src="./images/Rolling.gif" />
                                                            </p>
                                                        </div>
                                                    </div>
                                                </div>
                                                <br />
                                                <br />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-12 bgbotin" style="background-image: url(./crv_sched/img/invoice/bgbotin.png); background-position: center center; background-size: 100% 100%; width: 100%; margin-top: -100px; z-index: 0 !important;">
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade" id="ShowDeposit" tabindex="-1" role="dialog" aria-labelledby="depositListModalLabel" aria-hidden="true" style="z-index: 1000000000000000">
            <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="depositListModalLabel">Deposit & Payment List</h5>
                        <%-- Added Close Icon Button --%>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="float-start">
                                <p style="display: none" id="d_ProgressGIF">
                                    <img src="images/Rolling.gif" />
                                    Loading...
                                </p>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="table-responsive">
                                <table class="table table-bordered">
                                    <thead style="font-weight: normal; color: black;">
                                        <tr>
                                            <th>Type</th>
                                            <th>Source</th>
                                            <th>Check Name</th>
                                            <th>Check Number</th>
                                            <th>Amount</th>
                                            <th>Date</th>
                                        </tr>
                                    </thead>
                                    <tbody id="NewRowForDeposit">
                                        <%-- Data will be loaded here by JavaScript --%>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <%-- Corrected Close Button --%>
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>
    </div>


    <script type="text/javascript">

        //$(function () {
        //    $(".datefield").datepicker({
        //        dateFormat: 'mm/dd/yy',
        //        changeMonth: true,
        //        changeYear: true,
        //        yearRange: '-100y:c+15y'
        //    });
        //});

        $("body").on("keyup change", ".allInputs", function (event) {
            var cId = $(this).attr('id').match(/\d+/),
                Sl = parseInt(cId) + 1;
            if ($('#id_Name' + Sl).length == 0) CreateNewTR(Sl);
        });

        var _InvsId = $("#MainContent_HdInvoiceId").val();

        if (_InvsId === undefined || _InvsId == "") {
            $('#btn_viewDeposite').hide();
            FillQItemData();
        }
        else
            ViewInvoice(_InvsId);

        function ViewInvoice(ivId) {
            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/GetInvoiceDetailsById",
                data: JSON.stringify({ 'iId': ivId }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {

                    $("#tblNewInvoice").html(Data.d.TableRow);

                    document.getElementById("<%=optTaxRate.ClientID%>").value = Data.d.TaxId;
                    document.getElementById("<%=optDiscount.ClientID%>").value = Data.d.DisType;

                    if (!Data.d.TaxId)
                        document.getElementById("<%=optTaxRate.ClientID%>").selectedIndex = 0;

                    $("#MainContent_invDiscountRate").val(Data.d.DisRate);
                    $("#MainContent_invDiscountVal").val(Data.d.DisAmount);

                    $("#MainContent_invTaxRate").val(Data.d.TaxRate);
                    $("#MainContent_invTaxTotal").val(Data.d.TotalTax);

                    $("#MainContent_Note").val(Data.d.Notes);

                    CalNetTotal();

                    $('#ItemOpts').val(Data.d.OptionData);
                    CreateNewTR(Data.d.Quantity);
                    var prvtotal = $("#MainContent_invTotal").val();
                    $("#PrvTotal").val(prvtotal);
                }
            });
        }

        function FillQItemData() {
            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/GetQboItemData",
                data: JSON.stringify({}),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
                    $('#ItemOpts').val(Data.d);
                    CreateNewTR(1);
                }
            });
        }

        function CreateNewTR(Sl) {
            var ItemOpts = $('#ItemOpts').val();
            var newRow = `
        <tr class='invTR'>
            <td>${Sl}</td>
            <td><select id='id_Name${Sl}' class='allInputs idSelect form-select' onchange='FillItemsInfo(this)'>${ItemOpts}</select></td>
            <td><input id='id_Desc${Sl}' type='text' class='idInput form-control itmDesc' /></td>
            <td><input id='id_ServDate${Sl}' type='date' class='idInput form-control srvDate' /></td>
            <td><input id='id_Qty${Sl}' type='number' oninput="this.value=this.value.replace(/[^0-9.]/g,'');" class='idInput form-control itmQty numbers-only' onchange='CalLineTotal(this)' /></td>
            <td><input id='id_Rate${Sl}' type='number' onclick='removeZero(this)' onfocusout='FormatDecimal(this)' class='idInput form-control itmRate numbers-only' onchange='CalLineTotal(this)' /></td>
            <td><input id='id_Amount${Sl}' type='number' class='idInput form-control itmAmount' readonly /></td>
            <td>
                <div class="tax-toggle-container">
                    <label class="toggle-switch">
                        <input id='id_Tax${Sl}' type='checkbox' class='idInput itmTax' onchange='TaxRecal()'>
                        <span class="slider-tax"></span>
                    </label>
                </div>
            </td>
            <td><a href='javascript: void(0);' onclick='removeThis(this)' title='Delete' class='delete-icon'><i class='fa fa-trash'></i></a></td>
        </tr>`;
            $('#tblNewInvoice').append(newRow);
        }

        function removeThis(_this) {
            /*$(_this).closest('tr').remove();*/
            const $row = $(_this).closest("tr");
            const index = $(".invTR").index($row);

            if (index === 0) {
                alert("You can't delete the first row!");
                return;
            }

            $row.remove();
            CalNetTotal();
        }

        function removeZero(_this) {
            if ($(_this).val() == "0.00") {
                $(_this).val('')
            }
        }

        function CalNetTotal() {
            var vAmount = 0;
            $('#tblNewInvoice tr').each(function () {
                var cId = $(this).find('td:eq(0)').text();
                var _Amount = parseFloat($('#id_Amount' + cId).val() || '0');
                vAmount = vAmount + _Amount;
            })
            $("#MainContent_invSubTotal").val(vAmount.toFixed(2));
            $("#MainContent_invTotal").val(vAmount.toFixed(2));
            CalDiscount();
            CalTax();
            //Modified By Munem
            var taxAmount = $("#MainContent_invTaxTotal").val();
            var _invTotal = parseFloat($("#MainContent_invTotal").val());
            var _txt_Deposit = parseFloat($("#MainContent_txt_Deposit").val())
            var _Due = _invTotal - _txt_Deposit;
            //  $("#invTotal").val(_invTotal);
            $("#MainContent_txt_Due").val(_Due.toFixed(2));
        }

        function FillItemsInfo(_this) {
            var cId = $(_this).attr('id').match(/\d+/);
            var pId = $('#id_Name' + cId).val();
            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/GetProductById",
                data: JSON.stringify({ 'pId': pId }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
                    console.log(Data);
                    $("#id_Desc" + cId).val(Data.d.Description);
                    $("#id_Qty" + cId).val(Data.d.Quantity);
                    $("#id_Rate" + cId).val(Data.d.Rate);
                    $("#id_Amount" + cId).val(Data.d.Amount);

                    //document.getElementById('id_Tax' + cId).checked = true;
                    //var checkboxes = document.getElementsByTagName('id_Tax');
                    if (Data.d.Taxable == "True")
                        document.getElementById('id_Tax' + cId).checked = true;
                    else
                        document.getElementById('id_Tax' + cId).checked = false;
                    CalNetTotal();
                }
            });
        }

        function FormatDecimal(_this) {
            var value = parseFloat($(_this).val())
            if ($(_this).val()) {
                $(_this).val(value.toFixed(2))
            }
            else {
                $(_this).val('0.00')
            }
        }

        function CalLineTotal(_this) {
            var cId = $(_this).attr('id').match(/\d+/);
            var _qty = parseFloat($('#id_Qty' + cId).val() || '0');
            var _Rate = parseFloat($('#id_Rate' + cId).val() || '0');
            var TotalAmount = _qty * _Rate;
            $('#id_Amount' + cId).val(TotalAmount.toFixed(2));
            CalNetTotal();
        }

        function FillTaxRate(ids) {
            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/GetTaxRateById",
                data: JSON.stringify({ 'pId': ids }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
                    $("#MainContent_invTaxRate").val(Data.d.Rate);
                    CalTax();
                }
            });
        }

        function CalDiscount() {
            if ($("#MainContent_invDiscountRate").val()) {
                var _SubTotal = parseFloat($("#MainContent_invSubTotal").val())
                var _Rate = parseFloat($("#MainContent_invDiscountRate").val())
                var TotalAmount = 0;
                const _disRate = document.getElementById("<%=optDiscount.ClientID%>");
                if (_disRate.value == 1) {
                    TotalAmount = _SubTotal * _Rate;
                    TotalAmount = TotalAmount / 100;
                }
                else
                    TotalAmount = _Rate;
                $("#MainContent_invDiscountVal").val(TotalAmount.toFixed(2));

                var _Total = _SubTotal - TotalAmount;
                $("#MainContent_invTotal").val(_Total.toFixed(2));
                CalTax();
            }
        }

        function CalTax() {
            var vAmount = 0;
            var vTotalAmount = 0;
            if ($("#MainContent_invTaxRate").val()) {
                $('#tblNewInvoice tr').each(function () {
                    var cId = $(this).find('td:eq(0)').text();
                    var TaxChecked = document.getElementById('id_Tax' + cId)
                    var _Amount = parseFloat($('#id_Amount' + cId).val() || '0');
                    if (TaxChecked.checked)
                        vAmount = vAmount + _Amount;
                    vTotalAmount = vTotalAmount + _Amount;
                })
                var _disCount = 0;
                if ($("#MainContent_invDiscountVal").val())
                    _disCount = parseFloat($("#MainContent_invDiscountVal").val())

                vAmount = vAmount - _disCount;

                var _Rate = parseFloat($("#MainContent_invTaxRate").val());

                var TotalTax = vAmount * _Rate;
                TotalTax = TotalTax / 100;

                if (TotalTax < 0)
                    TotalTax = 0;
                $("#MainContent_invTaxTotal").val(TotalTax.toFixed(2));
                var _Total = vTotalAmount + TotalTax - _disCount;
                $("#MainContent_invTotal").val(_Total.toFixed(2));
                var _invTotal = parseFloat($("#MainContent_invTotal").val())
                var _txt_Deposit = parseFloat($("#MainContent_txt_Deposit").val())
                var _Due = _invTotal - _txt_Deposit;
                $("#MainContent_txt_Due").val(_Due.toFixed(2));
            }
        }
        function TaxRecal() {
            CalTax();
            CalNetTotal();
        }


        // save invoice
        function submitInvoice() {
            var AllPId = "";
            var AllPName = "";
            var AllQty = "";
            var AllRate = "";
            var AllTaxable = "";
            var AllDes = "";
            var AllSDate = "";
            var AllTax = "";
            var _count = 0;

            $("#IsDataSaving").val('0');
            $('#tblNewInvoice tr').each(function () {
                var vSL = $(this).find('td:eq(0)').text();
                var vPID = $('#id_Name' + vSL).val();
                if (vPID) {
                    var vPname = $('#id_Name' + vSL + ' option:selected').text();
                    var vDesc = $('#id_Desc' + vSL).val();
                    var vSDate = $('#id_ServDate' + vSL).val();
                    var vQty = $('#id_Qty' + vSL).val();
                    var vRate = $('#id_Rate' + vSL).val();
                    var pTax = "";

                    if (vRate == "") {
                        vRate = "0.00";
                    }
                    var TaxChecked = document.getElementById('id_Tax' + vSL)
                    if (TaxChecked.checked)
                        pTax = "TAX";
                    else
                        pTax = "NON";

                    AllPId += vPID;
                    AllPId += "@";

                    AllPName += vPname;
                    AllPName += "@";

                    AllDes += vDesc;
                    AllDes += "@";

                    AllSDate += vSDate;
                    AllSDate += "@";

                    AllQty += vQty;
                    AllQty += "@";

                    AllRate += vRate;
                    AllRate += "@";


                    AllTax += pTax;
                    AllTax += "@";

                    _count = _count + 1;
                }
            });
            const _Cust = "";
            var _cId = _Cust.value;

            var _cName = "";
            var invdt = "0";
            var invDuteDt = "0";
            var invNotes = "0";

            var discountOpt = $("#MainContent_optDiscount").val();
            var disAmnt = "0";
            var disRt = "0";
            var TotalAmount = "0";
            if ($("#MainContent_invDiscountRate").val()) {
                disRt = $("#MainContent_invDiscountRate").val();
                disAmnt = $("#MainContent_invDiscountVal").val();
            }

            var subTotal = $("#MainContent_invSubTotal").val();
            disAmnt = $("#MainContent_invDiscountVal").val();
            var TaxTp = $("#MainContent_optTaxRate").val();
            var taxAmt = $("#MainContent_invTaxTotal").val();

            TotalAmount = $("#MainContent_invTotal").val();
            var cID = $("#MainContent_SV_CustomeID").val();
            var qNum = $("#MainContent_Number").val();
            //alert(qNum);
            var _ExpirationDate = $("#ExpirationDate").val();
            if (!_ExpirationDate) { _ExpirationDate = '' }
            //  alert('_ExpirationDate = ' + _ExpirationDate);
            var _Date = $("#Date").val();
            var _Note = $("#MainContent_Note").val();
            var _CompanyId = $("#MainContent_hdCompanyID").val();
            var _Paid = "0";// $("#invPaid").val();
            var _Indicator = $("#MainContent_Indicator").val();
            var _AppointmentID = $("#MainContent_AppointmentID").val();
            var InvoiceID = $("#MainContent_HdInvoiceId").val();
            var _QboId = "0";
            if (_Indicator == "Invoice") {
                _QboId = $("#MainContent_HdQboId").val();
            }
            else if (_Indicator == "Proposal") {
                _QboId = $("#MainContent_hf_QboEstimateId").val();
            }
            var param = "";

            if (_count == 0) {
                alert("Please add invoice item");
                return;
            }
            var isNewTotal = false;
            var prvtotal = $("#PrvTotal").val();
            var newtotal = $("#MainContent_invTotal").val();
            console.log(prvtotal)
            console.log(newtotal)

            if (prvtotal != null) {
                if (prvtotal != newtotal) {
                    isNewTotal = true;
                }
            }
            param = {
                'pId': AllPId, 'pName': AllPName, 'sDate': AllSDate, 'pQty': AllQty, 'AllTaxable': AllTax, 'pRate': AllRate, 'pDes': AllDes, 'CustomerId': cID, 'CustomerQboID': $("#MainContent_hf_CustomerQboID").val(), 'qNum': qNum,
                'subTotal': subTotal, 'discountOpt': discountOpt, 'disRt': disRt, 'disAmt': disAmnt, 'TaxTp': TaxTp, 'taxAmt': taxAmt, 'TotalAmount': TotalAmount, '_ExpirationDate': _ExpirationDate, '_Date': _Date,
                '_Note': _Note, '_CompanyId': _CompanyId, '_Paid': _Paid, '_Indicator': _Indicator, '_AppointmentID': _AppointmentID, 'InvoiceID': InvoiceID, '_QboId': _QboId, 'isNewTotal': isNewTotal
            }

            console.log(param);

            var prgBar = document.getElementById("ProgressGIF");
            prgBar.style.display = "block";

            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/InvoiceSubmit",
                data: JSON.stringify(param),
                contentType: "application/json",
                dataType: "json",
                success: function (data) {
                    prgBar.style.display = "none";

                    //if (_Indicator == "Invoice") {
                    //   ShowCustomAlert("Invoice has been saved.");
                    //}
                    //else if (_Indicator == "Proposal") {
                    //    ShowCustomAlert("Proposal/Estimate has been saved.");
                    //}
                    //else {
                    //    ShowCustomAlert(data.d);
                    //}
                    if ($("#MainContent_EnableSaveOnly").val() == "0") {
                        alert(data.d);
                        if (_Indicator == "Invoice") {
                            if (_AppointmentID == "") {
                                window.location.href = "Invoice.aspx";
                            }
                            else {
                                window.location.href = "appointmentDetail.aspx?m=1" + "&cid=" + cID + "&appt=" + _AppointmentID;
                            }
                        }
                        else if (_Indicator == "Proposal") {
                            if (_AppointmentID == "") {
                                window.location.href = "Invoice.aspx";
                            }
                            else {
                                window.location.href = "appointmentDetail.aspx?m=1" + "&cid=" + cID + "&appt=" + _AppointmentID;
                            }
                        }
                        else {
                            window.location.href = "Invoice.aspx";
                        }
                    }
                    else {
                        $("#MainContent_EnableSaveOnly").val('0')
                    }

                }
            });
        }


        function ViewDepositList() {
            var depositModal = new bootstrap.Modal(document.getElementById('ShowDeposit'));

            var invoiceId = $("#MainContent_HdInvoiceId").val();
            if (!invoiceId) {
                alert("Please save the invoice before viewing payments.");
                return;
            }

            depositModal.show();

            $("#NewRowForDeposit").html("");
            var prgBar = document.getElementById("d_ProgressGIF");
            prgBar.style.display = "block";

            $.ajax({
                type: "POST",
                url: "InvoiceCreate.aspx/GetDepositsById",
                data: JSON.stringify({ 'iId': invoiceId }),
                contentType: "application/json",
                dataType: "json",
                success: function (response) {
                    prgBar.style.display = "none";
                    $("#NewRowForDeposit").html(response.d.TableRow);
                },
                error: function (err) {
                    prgBar.style.display = "none";
                    $("#NewRowForDeposit").html("<tr><td colspan='6' class='text-center text-danger'>Failed to load payment data.</td></tr>");
                    console.error("Error fetching deposit list:", err);
                }
            });
        }

    </script>
    <script type="text/javascript">
        document.addEventListener('DOMContentLoaded', function () {
            const backButton = document.getElementById('btnBack');

            if (backButton) {
                backButton.addEventListener('click', function (e) {
                    e.preventDefault();

                    const currentUrl = new URL(window.location.href);
                    const customerIdInt = currentUrl.searchParams.get("custId_int");
                    const siteId = currentUrl.searchParams.get("siteId");
                    if (!customerIdInt || !siteId) {
                        alert("Error: Cannot determine the return page. The required IDs were not found in the URL. Returning to the main customer list.");
                        window.location.href = 'Customer.aspx';
                        return;
                    }
                    const returnUrl = `CustomerDetails.aspx?custId=${customerIdInt}&siteId=${siteId}&tab=invoices`;
                    window.location.href = returnUrl;
                });
            }
        });
    </script>
    <script type="text/javascript">

        document.addEventListener('DOMContentLoaded', function () {
            const options = document.querySelectorAll('.deposit-switch-option');
            const slider = document.querySelector('.deposit-switch-slider');
            const hfRequestedAmtType = document.getElementById('hfRequestedAmtType');
            const txtReqAmount = document.getElementById('<%= txtReqAmount.ClientID %>');

            function updateSlider(activeIndex) {
                const activeOption = options[activeIndex];
                slider.style.width = activeOption.offsetWidth + 'px';
                slider.style.transform = `translateX(${activeOption.offsetLeft - 4}px)`;
            }

            options.forEach((option, index) => {
                option.addEventListener('click', function () {
                    options.forEach(opt => opt.classList.remove('active'));
                    this.classList.add('active');
                    hfRequestedAmtType.value = this.getAttribute('data-value');
                    updateSlider(index);
                    CalRequestedDepoAmount();
                });
            });

            const initialActiveIndex = hfRequestedAmtType.value === '2' ? 1 : 0;
            options[initialActiveIndex].classList.add('active');
            options[initialActiveIndex === 0 ? 1 : 0].classList.remove('active');
            setTimeout(() => updateSlider(initialActiveIndex), 100);
        });

        function CalRequestedDepoAmount() {
            const reqAmtType = document.getElementById('hfRequestedAmtType').value;
            const reqAmountInput = document.getElementById('<%= txtReqAmount.ClientID %>');
            const dueAmount = parseFloat(document.getElementById('<%= txt_Due.ClientID %>').value) || 0;
            let reqValue = parseFloat(reqAmountInput.value) || 0;
            let finalDepositAmount = 0;

            if (reqAmtType === '1') { // Percentage
                finalDepositAmount = (dueAmount * reqValue) / 100;
            } else { // Dollar
                finalDepositAmount = reqValue;
            }
            console.log("Calculated Deposit: $" + finalDepositAmount.toFixed(2));
        }
    </script>

</asp:Content>
