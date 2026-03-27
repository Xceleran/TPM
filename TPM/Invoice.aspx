<%@ Page Title="" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="Invoice.aspx.cs" Inherits="TPM.Invoice" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">


    <style type="text/css">
        /* The Modal (background) */
        .modal {
            display: none; /* Hidden by default */
            position: fixed; /* Stay in place */
            z-index: 1; /* Sit on top */
            padding-top: 100px; /* Location of the box */
            left: 0;
            top: 0;
            width: 100%; /* Full width */
            height: 100%; /* Full height */
            overflow: auto; /* Enable scroll if needed */
            background-color: rgb(0,0,0); /* Fallback color */
            background-color: rgba(0,0,0,0.4); /* Black w/ opacity */
        }

        /*#modal,
        .modal {
            z-index: 2000;
        }*/

        /* Modal Content */
        .modal-content {
            background-color: #fefefe;
            margin: auto;
            padding: 20px;
            border: 1px solid #888;
            width: 80%;
        }

        /* The Close Button */
        .close {
            color: #aaaaaa;
            float: right;
            font-size: 28px;
            font-weight: bold;
        }

            .close:hover,
            .close:focus {
                color: #000;
                text-decoration: none;
                cursor: pointer;
            }

        /*===============================*/

        .material-icons {
            cursor: pointer;
            color: #FFF;
        }

        .float {
            width: 40px;
            height: 40px;
            background-color: cadetblue;
            color: #FFF;
            border-radius: 100%;
            text-align: center;
            box-shadow: 0 2.8px 2.2px rgba(0, 0, 0, 0.048), 0 6.7px 5.3px rgba(0, 0, 0, 0.069), 0 12.5px 10px rgba(0, 0, 0, 0.085), 0 22.3px 17.9px rgba(0, 0, 0, 0.101), 0 41.8px 33.4px rgba(0, 0, 0, 0.122), 0 100px 80px rgba(0, 0, 0, 0.17);
        }

            .float:hover {
                background-color: cadetblue;
            }

        .plus {
            margin-top: 10px;
        }

        thead {
            color: white;
            background-color: #CACACA !important;
            height: 50px;
            vertical-align: middle;
        }

            thead th {
                color: black;
                background-color: #CACACA !important;
                font-family: 'poppinsmedium';
                font-weight: normal;
                font-size: 15px;
                vertical-align: middle !important;
            }

        table td {
            vertical-align: middle !important;
        }

        .bgtopin {
            height: 350px;
        }

        .bgbotin {
            height: 250px
        }

        .cnttop {
            margin-top: -350px;
            padding: 25px;
            z-index: 1;
        }

        @media only screen and (max-width : 991px) {
            .bgtopin {
                height: 200px;
            }

            .bgbotin {
                height: 100px
            }

            .cnttop {
                margin-top: -150px;
            }
        }

        @media only screen and (max-width : 767px) {
            .bgtopin {
                height: 200px;
            }

            .bgbotin {
                height: 100px
            }

            .cnttop {
                margin-top: -150px;
            }
        }
    </style>

    <style type="text/css">
        .idSelect {
            width: 250px;
            padding: 5px;
        }

        .srvDate {
            width: 130px;
            padding: 5px;
        }

        .itmDesc {
            width: 300px;
        }

        .itmQty {
            width: 60px;
            padding: 5px;
        }

        .itmRate {
            width: 90px;
            padding: 5px;
        }

        .itmAmount {
            width: 90px;
            padding: 5px;
        }

        .itmTax {
            width: 40px;
            padding: 5px;
        }
    </style>
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

        .content-box {
            border: 1px solid #ccc;
            padding: 20px;
        }
    </style>
    <style>
        .parent > .row {
            display: flex;
            align-items: center;
            height: 100%;
        }

        .col img {
            height: 170px;
            width: 100%;
            cursor: pointer;
            transition: transform 1s;
            object-fit: cover;
            border-radius: 15%;
            overflow: hidden;
        }

        .col label {
            overflow: hidden;
            position: relative;
        }

        .imgbgchk:checked + label > .tick_container {
            opacity: 1;
        }
        /*         aNIMATION */
        .imgbgchk:checked + label > img {
            transform: scale(1.25);
            opacity: 0.3;
        }

        .tick_container {
            transition: .5s ease;
            opacity: 0;
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            -ms-transform: translate(-50%, -50%);
            cursor: pointer;
            text-align: center;
            border-radius: 15%;
            overflow: hidden;
        }

        .tick {
            background-color: #4CAF50;
            color: white;
            font-size: 16px;
            padding: 6px 12px;
            height: 40px;
            width: 40px;
            border-radius: 100%;
            border-radius: 15%;
            overflow: hidden;
        }

        .swal2-container {
            z-index: 1000000000000005 !important;
        }
    </style>

    <%-- Proposal Modal Start --%>
    <div class="modal fade" id="modalCustomerMail" tabindex="-1" role="dialog" aria-labelledby="modalCustomerMail" aria-hidden="true" style="z-index: 1000000000000000">
        <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="exampleModalLongTitle">Confirm/Edit Email/MMS Content</h5>
                    <%-- <button type="button" class="close btn-danger" data-dismiss="modal" aria-label="Close" onclick="ClosePopup()">
                                        <span aria-hidden="true">&times;</span>
                                    </button>--%>
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

                    <asp:HiddenField ID="CustomerID" ClientIDMode="Static"  runat="server" />
                    <asp:HiddenField ID="IsDataSaving" ClientIDMode="Static" Value="0" runat="server" />
                    <asp:HiddenField ID="EnableSaveOnly" ClientIDMode="Static" Value="0" runat="server" />
                    <asp:HiddenField ID="_CC" ClientIDMode="Static" runat="server" />
                    <asp:HiddenField ID="_InvoiceNo" ClientIDMode="Static" runat="server" />
                    <asp:HiddenField ID="hf_CustomerQboID" ClientIDMode="Static" Value="0" runat="server" />
                    <asp:HiddenField ID="hf_CurrentPdfType" ClientIDMode="Static" Value="AsaInvoice" runat="server" />
                    <%--Added to check that loggeed in user is lhg OR NOT --%>
                    <asp:HiddenField ID="isLHG" ClientIDMode="Static" Value="" runat="server" />
                    <asp:HiddenField ID="IsDeposit" ClientIDMode="Static" Value="0" runat="server" />
                    <asp:HiddenField ID="IsPCS" ClientIDMode="Static" Value="False" runat="server" />
                    <asp:HiddenField ID="FromInvList"  runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="FromInvoices" ClientIDMode="Static" runat="server" />
                    <asp:HiddenField ID="FromCustomer" ClientIDMode="Static" runat="server" />
                    <asp:HiddenField ID="FormType" ClientIDMode="Static" runat="server" />

                    <div class="row mt-0">
                        <div class="col-12 mt-0">
                            <label for="_To" class="col-form-label">To</label>
                            <div class="col-lg-12">
                                <input type="text" id="_To" name="_To" ClientIDMode="Static" runat="server" class="form-control" />
                            </div>
                        </div>
                    </div>

                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="_BCC" class="col-form-label">Bcc</label>
                            <div class="col-lg-12">
                                <input type="text" id="_BCC" name="BCC" ClientIDMode="Static" runat="server" class="form-control" />
                            </div>
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="EmailSubject" class="col-form-label">Subject</label>
                            <div class="col-lg-12">
                                <input type="text" id="_EmailSubject" name="EmailSubject" ClientIDMode="Static" runat="server" class="form-control col-6" max="500" />
                            </div>
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <div class="col-lg-12">
                                <div class="custom-file">

                                    <asp:FileUpload ID="file" ClientIDMode="Static"  runat="server" AllowMultiple="true" MaxLength="100" CssClass="custom-file-input" onchange="javascript:updateList()" />

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
                                        Template 1
                                        <img src="images/AsaInvoice.jpg" alt="Invocie 1" id="invImg" ClientIDMode="Static" runat="server">
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
                                        <img src="images/Estimate.jpg" alt="Image 4" id="estImg" ClientIDMode="Static" runat="server">
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
                                <textarea id="EmailBody" name="EmailBody" rows="8" ClientIDMode="Static" runat="server" class="form-control"></textarea>
                                <p for="validationCustom01" class=" text-warning mb-0">Please Save first before you sent mail.</p>
                            </div>
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0" id="IsSendPaymentLinkDIV">
                            <div class="form-check form-check-xl">
                                <input type="checkbox" class="form-check-input" id="IsSendPaymentLink" style="width: 1.5em; height: 1.5em;" ClientIDMode="Static" runat="server">
                                <label class="form-check-label" for="exampleCheckbox" style="font-size: 1.3em;">Include Payment Link</label>
                            </div>
                        </div>
                        <div>
                        </div>
                    </div>
                    <div>
                        <label for="txt_ReqDepo" style="float: left;" class="col-form-label">Requested Deposit/Invoice Payment Amount:</label>
                        <div class="col-sm-12">
                            <input type="text" id="txt_ReqDepo" name="txt_ReqDepo" readonly class="form-control" />
                        </div>
                    </div>
                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-secondary  btn-block ml-1" data-dismiss="modal" onclick="ClosePopup()" style="float: left">Close</button>
                    <asp:Button ID="btn_PrintPdf" ClientIDMode="Static" runat="server" Text="Print" CssClass="btn btn-secondary text-nowrap" OnClick="btn_PrintPdf_Click" />&nbsp;&nbsp;
                    
                    <input type="button" ClientIDMode="Static" runat="server" id="btnMMS" value="Send MMS" class="btn btn-success text-nowrap" onclick="OpenMMSPopup()" />
                    &nbsp;

                    <asp:Button ID="btnSendProposalMail" ClientIDMode="Static" runat="server" Text="Send Email" CssClass="btn btn-success text-nowrap" OnClientClick="return sendProposal();" OnClick="btnSendProposalMail_Click" />
                    <asp:Button ID="btnSendInvoiceMail" ClientIDMode="Static" runat="server" Text="Send Email" CssClass="btn btn-success text-nowrap" OnClientClick="return sendInvoice();" OnClick="btnSendInvoiceMail_Click" />
                </div>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="_Type" ClientIDMode="Static" runat="server" />
    <%--End Proposal Modal  --%>
    <div class="row m-3">
        <div class="container">
            <div class="row">
                <div class="col-12 mb-3">

                    <div class="card card-custom gutter-b card-stretch p-3">
                        <input type="hidden" id="ItemOpts" />
                        <div class="col-12 bgtopin" style="background-image: url(./images/invoice/bgtopin.png); background-position: center center; background-size: 100% 100%; width: 100%;">
                        </div>

                        <div class="col-12 cnttop">

                            <asp:HiddenField ID="hdCompanyID" Value="" ClientIDMode="Static"  runat="server" />
                            <asp:HiddenField ID="hdCompanyName" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="hdCompanyTag" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="PrvTotal" runat="server" Value="" ClientIDMode="Static" />

                            <asp:HiddenField ID="hdCompanyGUID" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="SV_CustomeID" Value="" ClientIDMode="Static" runat="server" />

                            <asp:HiddenField ID="AppointmentID" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="Indicator" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="hd_IsQuickBookEnabled" Value="False" runat="server" />

                            <asp:HiddenField ID="HdInvoiceId" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="HdQboId" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="hf_QboEstimateId" Value="" ClientIDMode="Static" runat="server" />
                            <asp:HiddenField ID="customerPhone" Value="" ClientIDMode="Static" runat="server" />
                            <div class="row">
                                <div class="col-12">
                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <asp:Label ID="lblmessage" ClientIDMode="Static" runat="server" Visible="False" ForeColor="#339966"></asp:Label>
                                        </div>

                                        <div class="col-12">
                                            <h1 class="poppinsbold">
                                                <asp:Label ForeColor="#566A98" ID="PageTitle" ClientIDMode="Static" runat="server"></asp:Label></h1>
                                            <hr />

                                            <div class="row">
                                                <div class="col-lg-2">
                                                    <label for="Date">Invoice Date :</label>
                                                    <asp:TextBox ID="Date" ClientIDMode="Static" runat="server" class="form-control datefield "></asp:TextBox>
                                                </div>
                                                <div class="col-lg-2 " id="div_Expirationdate" ClientIDMode="Static" runat="server">
                                                    <label for="ExpirationDate">Expiration Date :</label>
                                                    <asp:TextBox ID="ExpirationDate" ClientIDMode="Static"  runat="server" class="form-control datefield "></asp:TextBox>
                                                </div>
                                                <div class="col-lg-2">
                                                    <label for="txt_InvocieNumber">Number:</label>
                                                    <asp:TextBox ID="txt_InvocieNumber" ClientIDMode="Static" ReadOnly  runat="server" class="form-control "></asp:TextBox>
                                                </div>
                                                <div class="col-lg-2">
                                                </div>
                                                <div id="divLocation" ClientIDMode="Static" runat="server" class="col-lg-2">
                                                    <label for="qboLocation">Location:</label>
                                                    <select id="qboLocation" ClientIDMode="Static" runat="server" class="form-select"></select>
                                                </div>

                                                <div id="divClass" ClientIDMode="Static" runat="server" class="col-lg-2">
                                                    <label for="qboClass">Class:</label>
                                                    <select id="qboClass" ClientIDMode="Static" runat="server" class="form-select"></select>
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
                                                    <asp:TextBox ID="Number" ClientIDMode="Static"  runat="server" class="form-control  "></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="col-12 mt-4">
                                            <h5 class="poppinsbold">
                                                <asp:Label ForeColor="#566A98" ID="txtFirstName" ClientIDMode="Static" runat="server"></asp:Label></h5>
                                            <asp:Label ID="Address" ClientIDMode="Static" runat="server" Style="font-size: 15px;"></asp:Label>
                                            <br />
                                            <asp:Label ID="Contact" ClientIDMode="Static" runat="server" Style="font-size: 15px;"></asp:Label>
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
                                                                <input type='text' id='invSubTotal' runat="server" ClientIDMode="Static" readonly style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td colspan="2" style="border: 0px"></td>
                                                        </tr>
                                                        <tr style="border: 0px;">
                                                            <td colspan="5" align='right' style="border: 0px;">
                                                                <select id="optDiscount" ClientIDMode="Static" runat="server" style="width: 150px" class="form-select" onchange='CalDiscount()'>
                                                                    <option value="1">Discount(%)</option>
                                                                    <option value="2">Discount($)</option>
                                                                </select>
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='invDiscountRate' name="invDiscountRate" ClientIDMode="Static" runat="server" style="width: 60px; text-align: right;" class="form-control" value='' onchange='CalDiscount()' />
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='invDiscountVal' ClientIDMode="Static" runat="server" style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td colspan="2" style="border: 0px"></td>
                                                        </tr>
                                                        <tr align='right' style="border: 0px">
                                                            <td colspan="5" style="border: 0px">
                                                                <select id="optTaxRate" ClientIDMode="Static" runat="server" style="width: 150px" class="form-select" onchange='FillTaxRate(this.value)'></select></td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='invTaxRate' readonly ClientIDMode="Static" runat="server" style="width: 60px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='invTaxTotal' ClientIDMode="Static" readonly runat="server" style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td colspan="2" style="border: 0px"></td>
                                                        </tr>
                                                        <tr align='right' style="border: 0px;">
                                                            <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                <h6 class="poppinsmed m-0" style="color: black">Total</h6>
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='invTotal' runat="server" ClientIDMode="Static"  readonly style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td colspan="2" style="border: 0px"></td>
                                                        </tr>
                                                        <tr align='right' style="border: 0px;">
                                                            <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                <h6 class="poppinsmed m-0" style="color: black">Payment</h6>
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='txt_Deposit' runat="server" ClientIDMode="Static" readonly style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td style="border: 0px">
                                                                <input class="btn btn-secondary" width="20px;" onclick="ViewDepositList()" type="button" value="..." /></td>
                                                            <td style="border: 0px">&nbsp;</td>
                                                        </tr>
                                                        <tr align='right' style="border: 0px;">
                                                            <td colspan="6" style="border: 0px; vertical-align: middle;">
                                                                <h6 class="poppinsmed m-0" style="color: black">Due</h6>
                                                            </td>
                                                            <td align='center' style="border: 0px">
                                                                <input type='text' id='txt_Due' runat="server"  ClientIDMode="Static" readonly style="width: 100px; text-align: right;" class="form-control" value='' />
                                                            </td>
                                                            <td colspan="2" style="border: 0px"></td>
                                                        </tr>
                                                    </table>
                                                </div>
                                            </div>
                                            <div class="col-12 mt-4">

                                                <label for="lblPONO">PO :</label>
                                                <%--<asp:Label ID="lblPONO"  runat="server" ></asp:Label>--%>
                                                <asp:Label ID="lblPONO" ClientIDMode="Static" runat="server" Style="font-size: 15px;"></asp:Label>
                                            </div>
                                            <div class="col-12 col-sm-12 col-md-8 mt-5">
                                                <h5 class="poppinsmed">
                                                    <label for="dtfrom">Notes :</label>
                                                </h5>
                                                <asp:TextBox ID="Note" TextMode="MultiLine" ClientIDMode="Static" runat="server" class="form-control mb-5" Rows="4"></asp:TextBox>
                                            </div>

                                            <table>
                                                <tr style="border: 0px;">
                                                    <td colspan="5" align='right' style="border: 0px;">
                                                        <select id="ddlReqPer" ClientIDMode="Static" runat="server" style="width: 360px" class="form-select" onchange='CalRequestedDepoAmount()'>
                                                            <option value="1">Requested Deposit/Invoice Payment Amount:(%)</option>
                                                            <option value="2">Requested Deposit/Invoice Payment Amount:($)</option>
                                                        </select>
                                                    </td>
                                                    <td align='center' style="border: 0px">
                                                        <input type='text' id='txtReqPercent' ClientIDMode="Static" runat="server" style="width: 60px; text-align: right;" class="form-control" value='' onchange='CalRequestedDepoAmount()' />
                                                    </td>
                                                    <td align='center' style="border: 0px">
                                                        <asp:TextBox ID="txtDepoReq" ClientIDMode="Static" runat="server" Style="width: 100px; text-align: right;" class="form-control"></asp:TextBox>

                                                    </td>
                                                    <td colspan="2" style="border: 0px"></td>
                                                </tr>
                                            </table>

                                            <div class="col-12 mt-5">
                                                <div id="invPermission" ClientIDMode="Static" runat="server" class="row">
                                                    <div class="col-lg-12 mt-2 text-warning text-lg-start ">
                                                        <h4>You do not have permission to Add/Edit Invoice.</h4>
                                                    </div>
                                                </div>
                                                <div class="row">
                                                    <div class="col-lg-2 mt-2">
                                                        <input type="button" ClientIDMode="Static" runat="server" id="_SubmitInvoice" value="Save" class="btn btn-secondary btn-transparentdark mt-3 w-100" onclick="submitInvoice()" />
                                                    </div>
                                                    <div class="col-lg-2 mt-2">
                                                        <input type="button" ClientIDMode="Static" runat="server" id="SendMail" value="E-Mail/Print/MMS" class="btn btn-success btn-transparentdark mt-3 w-100" onclick="OpenMailPopUp()" />
                                                    </div>

                                                    <div class="col-lg-2 mt-2">
                                                        <asp:Button ID="btnBack" ClientIDMode="Static" runat="server" Text="Back" OnClick="btnBack_Click" class="btn btn-secondary btn-transparentdark mt-3 w-100" />
                                                    </div>

                                                    <%--<div class="col-lg-2 mt-2">
                                                        <div id="paymentButtonArea" runat="server">
                                                        </div>
                                                    </div>--%>

                                                    <div class="col-lg-2 mt-2">
                                                        <p style="display: none" id="ProgressGIF">
                                                            <img id="imgProcess" src="images/Rolling.gif" />
                                                        </p>
                                                    </div>
                                                    <div class="col-lg-2 mt-2" id="CreateOf">
                                                        <asp:Button ID="btn_ConvertToInvocie" ClientIDMode="Static" runat="server" Text="Convert to Invoice" OnClick="btnSave_Click_ConvertToInvoice" class="btn btn-secondary btn-transparentdark mt-3 w-100" />
                                                    </div>
                                                </div>
                                            </div>
                                            <br />
                                            <br />
                                            <div id="wisetackSignup" ClientIDMode="Static" runat="server" class="row content-box">
                                                <img src="images/wisetack_white_logo.png" style="float: left" class="col-1" />
                                                <p style="height: 50%; text-align: left">
                                                    <br />
                                                    Win more jobs by offering low monthly payments send your customer a financing application to see how much they qualify for. Applications take less than 5 minutes to complete.
                                                </p>
                                                <a href="WisetackSignup.aspx" target="_blank" style="text-decoration: none; text-align: right">Signup Now</a>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="col-12 bgbotin" style="background-image: url(./images/invoice/bgbotin.png); background-position: center center; background-size: 100% 100%; width: 100%; margin-top: -100px; z-index: 0 !important;"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="ShowDeposit" tabindex="-1" role="dialog" aria-labelledby="Collect_deposit" aria-hidden="true" style="z-index: 1000000000000000">
        <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Deposit List</h5>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="float-start">
                            <p style="display: none" id="d_ProgressGIF">
                                <img id="imgProcess" src="images/Rolling.gif" />
                                Sync on progress....
                            </p>
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="table-responsive">

                            <table class="table table-bordered">
                                <thead style="font-weight: normal; color: black;">
                                    <tr>
                                        <th>..</th>
                                        <th>Type</th>
                                        <th>Source</th>
                                        <th>Check Name</th>
                                        <th>Check Number</th>
                                        <th>Amount</th>
                                        <th>Date</th>
                                    </tr>
                                </thead>

                                <tbody id="NewRowForDeposit">
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <br />
                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-secondary  btn-block ml-1" data-dismiss="modal" onclick="CloseCollect_DepositListPopup()" style="float: left">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="showMMSBody" tabindex="-1" role="dialog" aria-labelledby="Collect_deposit" aria-hidden="true" style="z-index: 1000000000000000">
        <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Send MMS</h5>
                </div>
                <div class="modal-body">
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="mmsNumber" class="col-form-label"><span>Mobile no:</span></label>
                            <div class="col-lg-12">
                                <input id="mmsPhone" name="EmailBody" ClientIDMode="Static" runat="server" class="form-control" />

                            </div>
                        </div>
                        <div class="col-12 mt-0">
                            <label for="EmailBody" class="col-form-label"><span>MMS Body</span></label>
                            <div class="col-lg-12">
                                <textarea id="txtMMSBody" name="EmailBody" rows="10" ClientIDMode="Static" runat="server" class="form-control"></textarea>

                            </div>
                        </div>
                    </div>

                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-warning  btn-block ml-1" data-dismiss="modal" onclick="CloseMMSPopup()" style="float: left">Close</button>

                    <asp:Button ID="btn_SendMMS" ClientIDMode="Static" runat="server" Text="Send" CssClass="btn btn-success text-nowrap" OnClick="btn_SendMMS_Click" />
                </div>
            </div>
        </div>
    </div>


    <script src="Scripts/InvoiceList.js?v=19" type="text/javascript"></script>
    <script type="text/javascript">
        let isFormChanged = false;

        window.addEventListener('input', function () {
            isFormChanged = true;
        });


        window.addEventListener('beforeunload', function (e) {
            if (isFormChanged) {

                e.preventDefault();
                e.returnValue = '';
            }
        });


        function onFormSubmit() {
            isFormChanged = false;
        }
        $(function () {
            $('#txtDepoReq').attr('disabled', true);

            $("#ddlReqPer").change(function () {
                var depType = $('#ddlReqPer').val();
                if (depType == 1) {
                    $('#txtReqPercent').attr('disabled', false);
                    $('#txtDepoReq').attr('disabled', true);
                    $('#txtDepoReq').val('');
                }
                else {
                    $('#txtReqPercent').val('');
                    $('#txtDepoReq').val('');
                    $('#txtReqPercent').attr('disabled', true);
                    $('#txtDepoReq').attr('disabled', false);
                }

            })
        });

        $(function () {
            $(".datefield").datepicker({
                dateFormat: 'mm/dd/yy',
                changeMonth: true,
                changeYear: true,
                yearRange: '-100y:c+15y'
            });
        });
    </script>


    <script type="text/javascript">      
        var _InvsId = $("#HdInvoiceId").val();
        //Added By Munem if LHG then call the tax value
        var isLHGValue = $("#isLHG").val();
        if (isLHGValue === 'True') {

            if (_InvsId == "") FillTaxRate($('#optTaxRate').val());
        }
        //End
        function ViewDepositList() {

            $("#ShowDeposit").modal('show');
            $("#NewRowForDeposit").html("");
            var prgBar = document.getElementById("d_ProgressGIF");
            prgBar.style.display = "block";

            $.ajax({
                type: "POST",
                url: "Invoices.aspx/GetDepositsById",
                data: JSON.stringify({ 'iId': $("#HdInvoiceId").val() }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
                    prgBar.style.display = "none";
                    $("#NewRowForDeposit").html(Data.d.TableRow);
                }
            });
        }
        function CloseCollect_DepositListPopup() {

            $("#ShowDeposit").modal('hide');
        }
        $(".numbers-only").keypress(function (e) {
            //alert('sdfsd');
        });


  
        if (_InvsId == "")
            FillQItemData();
        else
            ViewInvoice(_InvsId)

        function ViewInvoice(ivId) {
        
            $.ajax({
                type: "POST",
                url: "Invoice.aspx/GetInvoiceDetailsById",
                data: JSON.stringify({ 'iId': ivId }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {

                    $("#tblNewInvoice").html(Data.d.TableRow);

                    document.getElementById("<%=optTaxRate.ClientID%>").value = Data.d.TaxId;
                    document.getElementById("<%=optDiscount.ClientID%>").value = Data.d.DisType;

                    if (!Data.d.TaxId)
                        document.getElementById("<%=optTaxRate.ClientID%>").selectedIndex = 0;

                    $("#invDiscountRate").val(Data.d.DisRate);
                    $("#invDiscountVal").val(Data.d.DisAmount);

                    $("#invTaxRate").val(Data.d.TaxRate);
                    $("#invTaxTotal").val(Data.d.TotalTax);

                    $("#Note").val(Data.d.Notes);

                    $("#txtReqPercent").val(Data.d.ReqDepoPercent);
                    $("#ddlReqPer").val(Data.d.ReqAmtType);

                    // Set QboClassId and QboLocationId dropdowns
                    if (Data.d.QboClassId && Data.d.QboClassId != "0") {
                        $("#<%=qboClass.ClientID%>").val(Data.d.QboClassId);
                    }
                    if (Data.d.QboLocationId && Data.d.QboLocationId != "0") {
                        $("#<%=qboLocation.ClientID%>").val(Data.d.QboLocationId);
                    }

                    CalNetTotal();

                    $('#ItemOpts').val(Data.d.OptionData);
                    CreateNewTR(Data.d.Quantity);
                    var prvtotal = $("#invTotal").val();
                    $("#PrvTotal").val(prvtotal);

                    setTimeout(function () {
                        $('#SendMail').attr('disabled', false);
                        $('#btn_ConvertToInvocie').attr('disabled', false);
                    }, 2000); // 2000 milliseconds = 2 seconds


                }
            });
        }

        function FillQItemData() {
            $.ajax({
                type: "POST",
                url: "Invoice.aspx/GetQboItemData",
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
            $('#tblNewInvoice').append("<tr class='invTR'><td>" + Sl + "</td>"
                + "<td><select id='id_Name" + Sl + "' class='allInputs idSelect form-select' onchange='FillItemsInfo(this)'>" + ItemOpts + "</select></td>"
                + "<td><input id='id_Desc" + Sl + "' type='text' class='idInput form-control itmDesc' /></td>"
                + "<td><input id='id_ServDate" + Sl + "' type='date' class='idInput form-control srvDate' /></td>"
                + "<td><input id='id_Qty" + Sl + "' type='number' oninput=this.value=this.value.replace(/[.,][^.0-9.,]/g,'');  class='idInput form-control itmQty numbers-only' onchange='CalLineTotal(this)' /></td>"
                + "<td><input id='id_Rate" + Sl + "' type='number' onclick='removeZero(this)' onfocusout='FormatDecimal(this)'  class='idInput form-control itmRate numbers-only' onchange='CalLineTotal(this)' /></td>"
                + "<td><input id='id_Amount" + Sl + "' type='number' class='idInput form-control itmAmount' readonly /></td>"
                + "<td><input id='id_Tax" + Sl + "' type='checkbox' class='idInput itmTax' onchange='TaxRecal()' /></td>"
                + "<td><a href='javascript: void(0);' onclick='removeThis(this)' title='Del'>Del</a></td>"
                + "</tr>");

        }
        function removeZero(_this) {
            if ($(_this).val() == "0.00") {
                $(_this).val('')
            }
        }
        function FormatDecimal(_this) {

            var value = parseFloat($(_this).val())
            if ($(_this).val()) {
                $(_this).val(value.toFixed(2));
            }
            else {
                $(_this).val('0.00')
            }
        }

        $("body").on("keyup change", ".allInputs", function (event) {
            var cId = $(this).attr('id').match(/\d+/),
                Sl = parseInt(cId) + 1;
            if ($('#id_Name' + Sl).length == 0) CreateNewTR(Sl);

            $('#SendMail').attr('disabled', true);
            $('#btn_ConvertToInvocie').attr('disabled', true);
        });

        function FillItemsInfo(_this) {

            var cId = $(_this).attr('id').match(/\d+/);
            var pId = $('#id_Name' + cId).val();
            $.ajax({
                type: "POST",
                url: "Invoice.aspx/GetProductById",
                data: JSON.stringify({ 'pId': pId }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
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
        function FillTaxRate(ids) {

            $.ajax({
                type: "POST",
                url: "Invoice.aspx/GetTaxRateById",
                data: JSON.stringify({ 'pId': ids }),
                contentType: "application/json",
                dataType: "json",
                success: function (Data) {
                    $("#invTaxRate").val(Data.d.Rate);
                    CalTax();
                }
            });
        }
        function CalLineTotal(_this) {
            var cId = $(_this).attr('id').match(/\d+/);
            var _qty = parseFloat($('#id_Qty' + cId).val() || '0');
            var _Rate = parseFloat($('#id_Rate' + cId).val() || '0');
            var TotalAmount = _qty * _Rate;
            $('#id_Amount' + cId).val(TotalAmount.toFixed(2));
            CalNetTotal();
        }
        $('input[type=radio][name=imgbackground]').change(function () {
            $("#hf_CurrentPdfType").val($("input[name='imgbackground']:checked").val());
        });
        function CalNetTotal() {
            var vAmount = 0;
            $('#tblNewInvoice tr').each(function () {
                var cId = $(this).find('td:eq(0)').text();
                var _Amount = parseFloat($('#id_Amount' + cId).val() || '0');
                 
                vAmount = vAmount + _Amount;
                
            })
           
            $("#invSubTotal").val(vAmount.toFixed(2));
            $("#invTotal").val(vAmount.toFixed(2));
           
            // CalTax();
            //Modified By Munem
            var taxAmount = $("#invTaxTotal").val();
            var _invTotal = parseFloat($("#invTotal").val());
            var _txt_Deposit = parseFloat($("#txt_Deposit").val())
            var _Due = _invTotal - _txt_Deposit;
         

           
                CalDiscount();
          

            // 
            $("#txt_Due").val(_Due.toFixed(2));

            //setTimeout(function () {
            //    addThousandSeparator();
            //}, 2000); 
        }
        function addThousandSeparator() {
            var invSubTotal = $("#invSubTotal").val();
            var discountTotal = $("#invDiscountVal").val();
            var taxTotal = $("#invTaxTotal").val();
            var total = $("#invTotal").val();
            var payment = $("#txt_Deposit").val();
            var due = $("#txt_Due").val();
            var reqDepoAmt = $("#txtDepoReq").val();

            invSubTotal = invSubTotal.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            discountTotal = discountTotal.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            taxTotal = taxTotal.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            total = total.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            payment = payment.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            due = due.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
            reqDepoAmt = reqDepoAmt.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");

            $("#invSubTotal").val(invSubTotal);
            $("#invDiscountVal").val(discountTotal);
            $("#invTaxTotal").val(taxTotal);
            $("#invTotal").val(total);
            $("#txt_Deposit").val(payment);
            $("#txt_Due").val(due);
            $("#txtDepoReq").val(reqDepoAmt);


        }

        function CalDiscount() {
             
            if ($("#invDiscountRate").val()) {
                var _SubTotal = parseFloat($("#invSubTotal").val())
                if (isNaN(_SubTotal)) _SubTotal = 0;
                var _Rate = parseFloat($("#invDiscountRate").val());
                var TotalAmount = 0;
                const _disRate = document.getElementById("<%=optDiscount.ClientID%>");
                if (_disRate.value == 1) {
                    TotalAmount = _SubTotal * _Rate;
                    TotalAmount = TotalAmount / 100;
                }
                else
                    TotalAmount = _Rate;

                if (TotalAmount < 0) TotalAmount = 0;
                $("#invDiscountVal").val(TotalAmount.toFixed(2));

             
                var _Total = _SubTotal - TotalAmount;
              

                $("#invTotal").val(_Total.toFixed(2));
                
                CalTax();


                //code by mohsin
                setTimeout(function () {
                    CalRequestedDepoAmount();
                }, 1000); // 1000 milliseconds = 2 seconds


            }

        }

        function CalRequestedDepoAmount() {
            var dAmt = parseFloat($("#txtDepoReq").val());
            if (isNaN(dAmt)) dAmt = 0;
            if ($("#txtReqPercent").val()) {
                var _invTotal = parseFloat($("#txt_Due").val())
                if (isNaN(_invTotal)) _invTotal = 0;
                var _Rate = parseFloat($("#txtReqPercent").val())
                var TotalAmount = 0;
                const _disRate = document.getElementById("<%=ddlReqPer.ClientID%>");
                if (_disRate.value == 1) {
                    TotalAmount = _invTotal * _Rate;
                    TotalAmount = TotalAmount / 100;
                }
                else
                    TotalAmount = _Rate;
                $("#txtDepoReq").val(TotalAmount.toFixed(2));

                if (dAmt > 0 && $("#ddlReqPer").val() == "2") {
                    $("#txtDepoReq").val(dAmt.toFixed(2));
                    $('#txtReqPercent').attr('disabled', true);
                }

                $('#SendMail').attr('disabled', true);
                $('#btn_ConvertToInvocie').attr('disabled', true);
            }
        }
        function TaxRecal() {
            CalTax();
            CalNetTotal();

        }
        function CalTax() {

            var vAmount = 0;
            var vTotalAmount = 0;
            if ($("#invTaxRate").val()) {
                $('#tblNewInvoice tr').each(function () {

                    var cId = $(this).find('td:eq(0)').text();
                    var TaxChecked = document.getElementById('id_Tax' + cId)
                    var _Amount = parseFloat($('#id_Amount' + cId).val() || '0');

                    if (TaxChecked.checked)
                        vAmount = vAmount + _Amount;

                    vTotalAmount = vTotalAmount + _Amount;
                })

                var disRate = parseFloat($('#invDiscountRate').val());
                if (isNaN(disRate)) disRate = 0;

                var disTaxableAmt = 0;
                var taxableAmt = 0;
                const _disRate = $('#optDiscount').val();
                if (_disRate == 2) {
                    var fixDiscount = parseFloat($("#invDiscountVal").val());
                    if (isNaN(fixDiscount)) fixDiscount = 0;
                    var discountPercent = (fixDiscount / vTotalAmount) * 100;
                    disTaxableAmt = (discountPercent / 100) * vAmount;

                }
                else {
                    disTaxableAmt = (disRate / 100) * vAmount;
                }

                taxableAmt = vAmount - disTaxableAmt;


                var _disCount = 0;

                if ($("#invDiscountVal").val())
                    _disCount = parseFloat($("#invDiscountVal").val())

                vAmount = vAmount - _disCount;

                var _Rate = parseFloat($("#invTaxRate").val())

                var TotalTax = taxableAmt * _Rate;
                TotalTax = TotalTax / 100;

                if (TotalTax < 0)
                    TotalTax = 0;
                $("#invTaxTotal").val(TotalTax.toFixed(2));

                var _Total = vTotalAmount + TotalTax - _disCount;
                $("#invTotal").val(_Total.toFixed(2));

                var _invTotal = parseFloat($("#invTotal").val())
                var _txt_Deposit = parseFloat($("#txt_Deposit").val())
                var _Due = _invTotal - _txt_Deposit;
                $("#txt_Due").val(_Due.toFixed(2));


                //code by mohsin
                setTimeout(function () {
                    CalRequestedDepoAmount();
                }, 1000); // 1000 milliseconds = 2 seconds

            }

        }

        function removeThis(_this) {
            $(_this).closest('tr').remove();
            CalNetTotal();

            $('#SendMail').attr('disabled', true);
            $('#btn_ConvertToInvocie').attr('disabled', true);
        }

        function removeThis(_this) {
            $(_this).closest('tr').remove();
            CalNetTotal();

            $('#SendMail').attr('disabled', true);
            $('#btn_ConvertToInvocie').attr('disabled', true);
        }

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

            $("#IsDataSaving").val('1');
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
            })

            const _Cust = "";

            var _cId = _Cust.value;

            var _cName = "";

            var invdt = "0";
            var invDuteDt = "0";
            var invNotes = "0";

            var discountOpt = $("#optDiscount").val();
            var disAmnt = "0";
            var disRt = "0";
            var TotalAmount = "0";
            if ($("#invDiscountRate").val()) {
                disRt = $("#invDiscountRate").val();
                disAmnt = $("#invDiscountVal").val();
            }

            var subTotal = $("#invSubTotal").val();
            disAmnt = $("#invDiscountVal").val();
            var TaxTp = $("#optTaxRate").val();
            var taxAmt = $("#invTaxTotal").val();

            TotalAmount = $("#invTotal").val();
            var cID = $("#SV_CustomeID").val();
            var qNum = $("#Number").val();
            //alert(qNum);
            var _ExpirationDate = $("#ExpirationDate").val();
            if (!_ExpirationDate) { _ExpirationDate = 'None' }
            //  alert('_ExpirationDate = ' + _ExpirationDate);
            var _Date = $("#Date").val();
            var _Note = $("#Note").val();
            var _CompanyId = $("#hdCompanyID").val();
            var _Paid = "0";// $("#invPaid").val();
            var _Indicator = $("#Indicator").val();
            var _AppointmentID = $("#AppointmentID").val();
            var InvoiceID = $("#HdInvoiceId").val();
            var _QboId = "0";
            if (_Indicator == "Invoice") {
                _QboId = $("#HdQboId").val();
            }
            else if (_Indicator == "Proposal") {
                _QboId = $("#hf_QboEstimateId").val();
            }
            var reqDepoAmt = $("#txtDepoReq").val();
            var reqDepoPercent = $("#txtReqPercent").val();
            if (!reqDepoPercent) reqDepoPercent = 0;

            var reqDepoAmtType = $("#ddlReqPer").val();

            // Capture QboClassId and QboLocationId from dropdowns
            var qboClassId = $("#<%=qboClass.ClientID%>").val() || "0";
            var qboLocationId = $("#<%=qboLocation.ClientID%>").val() || "0";

            var param = "";

            if (_count == 0) {
                swal.fire("Please add invoice item");
                return;
            }

            // Define proceedWithSave function first
            function proceedWithSave() {
                var isNewTotal = false;
                var prvtotal = $("#PrvTotal").val();
                var newtotal = $("#invTotal").val();
                console.log(prvtotal)
                console.log(newtotal)

                if (prvtotal != null) {
                    if (prvtotal != newtotal) {
                        isNewTotal = true;
                    }
                }
                param = {
                    'pId': AllPId, 'pName': AllPName, 'sDate': AllSDate, 'pQty': AllQty, 'AllTaxable': AllTax, 'pRate': AllRate, 'pDes': AllDes, 'CustomerId': cID, 'CustomerQboID': $("#hf_CustomerQboID").val(), 'qNum': qNum,
                    'subTotal': subTotal, 'discountOpt': discountOpt, 'disRt': disRt, 'disAmt': disAmnt, 'TaxTp': TaxTp, 'taxAmt': taxAmt, 'TotalAmount': TotalAmount, '_ExpirationDate': _ExpirationDate, '_Date': _Date,
                    '_Note': _Note, '_CompanyId': _CompanyId, '_Paid': _Paid, '_Indicator': _Indicator, '_AppointmentID': _AppointmentID, 'InvoiceID': InvoiceID, '_QboId': _QboId, 'isNewTotal': isNewTotal, 'reqDepoAmt': reqDepoAmt, 'reqDepoPercent': reqDepoPercent, 'requestedAmtType': reqDepoAmtType, 'QboClassId': qboClassId, 'QboLocationId': qboLocationId
                }
                console.log(param);
                var prgBar = document.getElementById("ProgressGIF");
                prgBar.style.display = "block";

                $.ajax({
                type: "POST",
                url: "Invoice.aspx/InvoiceSubmit",
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

                    if ($("#EnableSaveOnly").val() == "0") {

                        $.alert({
                            title: 'Xceleran',
                            content: data.d,
                            icon: 'fa fa-info-circle',
                            animation: 'scale',
                            closeAnimation: 'scale',
                            opacity: 0.5,
                            buttons: {
                                okay: {
                                    text: 'okay',
                                    btnClass: 'btn-blue',
                                    action: function () {
                                        isFormChanged = false;
                                        if (_Indicator == "Invoice") {
                                            //if (_AppointmentID == "") {
                                            //    window.location.href = "InvoiceList.aspx?Id=" + cID + "&Type=" + _Indicator;                                              
                                            //}
                                            //else {
                                            //    window.location.href = "appointmentDetail.aspx?m=1" + "&cid=" + cID + "&appt=" + _AppointmentID;
                                            //}
                                            window.location.href = "Invoice.aspx?InvoiceNumber=" + $('#txt_InvocieNumber').val() + "&cId=" + cID + "&FromInvList=" + $('#FromInvList').val() + "&FromInvoices=" + $('#FromInvoices').val() + "&FromCustomer=" + $('#FromCustomer').val() + "&FormType=" + $('#FormType').val() + "&InType=Invoice&AppID=0";
                                        }
                                        else if (_Indicator == "Proposal" || _Indicator == 'Estimate') {
                                            //if (_AppointmentID == "") {
                                            //    window.location.href = "InvoiceList.aspx?Id=" + cID + "&Type=" + _Indicator;
                                            //}
                                            //else {
                                            //    window.location.href = "appointmentDetail.aspx?m=1" + "&cid=" + cID + "&appt=" + _AppointmentID;
                                            //}
                                            window.location.href = "Invoice.aspx?InvoiceNumber=" + $('#txt_InvocieNumber').val() + "&cId=" + cID + "&FromInvList=" + $('#FromInvList').val() + "&FromInvoices=" + $('#FromInvoices').val() + "&FromCustomer=" + $('#FromCustomer').val() + "&FormType=" + $('#FormType').val() + "&InType=Proposal&AppID=0";

                                            //window.location.href = "Invoice.aspx?InvoiceNumber=" + $('#txt_InvocieNumber').val() + "&cId=" + cID + "&InType=Proposal&AppID=0";
                                        }

                                        else {
                                            window.location.href = "InvoiceList.aspx";
                                        }
                                    }
                                }
                            }
                        });

                    }
                    else {
                        $("#EnableSaveOnly").val('0')
                    }

                    //prgBarInv.style.display = "none";

                }
            });
            } // End of proceedWithSave function

            // Check if Class or Location is not selected and show confirmation popup
            // Only show popup if IsPCS session variable is true
            var isPCS = $("#IsPCS").val();
            if ((!qboClassId || qboClassId == "0" || qboClassId == "") && (!qboLocationId || qboLocationId == "0" || qboLocationId == "") && isPCS === "True") {
                // Check if user has chosen to not show this message again
                var dontShowAgain = localStorage.getItem('dontShowClassLocationWarning');
                if (dontShowAgain !== 'true') {
                    // Show confirmation popup with checkbox
                    Swal.fire({
                        title: 'Confirm Save',
                        html: '<p>Are you sure you want to save without class or location?</p>' +
                              '<label style="display: flex; align-items: center; margin-top: 15px; cursor: pointer;">' +
                              '<input type="checkbox" id="dontShowAgainCheckbox" style="margin-right: 8px; cursor: pointer;">' +
                              '<span>Do not show this message again</span>' +
                              '</label>',
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonText: 'Yes, Save',
                        cancelButtonText: 'Cancel',
                        confirmButtonColor: '#3085d6',
                        cancelButtonColor: '#d33',
                        allowOutsideClick: false,
                        didOpen: () => {
                            // Add event listener to checkbox
                            document.getElementById('dontShowAgainCheckbox').addEventListener('change', function(e) {
                                if (e.target.checked) {
                                    localStorage.setItem('dontShowClassLocationWarning', 'true');
                                } else {
                                    localStorage.removeItem('dontShowClassLocationWarning');
                                }
                            });
                        }
                    }).then((result) => {
                        if (result.isConfirmed) {
                            // User confirmed, proceed with save
                            proceedWithSave();
                        } else {
                            // User cancelled, stop the save process
                            var prgBar = document.getElementById("ProgressGIF");
                            if (prgBar) prgBar.style.display = "none";
                            return;
                        }
                    });
                    return; // Exit function, will continue in the then() callback
                }
            }

            // If we reach here, either Class/Location is selected or user has chosen to not show the warning
            proceedWithSave();

        }
        $('#frmUserList').on('change keyup keydown', 'input, textarea, select', function (e) {
            //$(this).addClass('changed-input');
            $("#IsDataSaving").val('1');
        });
        $(window).on('beforeunload', function () {
            alert($("#IsDataSaving").val());
            // if ( $("#IsDataSaving").val() == '1') {
            //    return 'If you leave this page without saving, some changed may be lost.';
            //}
            //if ($('.changed-input').length) {
            //    return 'You haven\'t saved your changes.';
            //}
        });
    </script>
    <script>
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

        function Collect(DepositAmount, Total, Number) {
            $("#txt_depositAmount").val(DepositAmount);
            $("#txt_TotalAmount").val("$" + Total);
            $("#_InvoiceNo").val(Number);
            $("#Collect_deposit").modal('show');
        }
        function ShowCollect_depositPopup() {

            $("#Collect_deposit").modal('show');
        }
        function CloseCollect_depositPopup() {

            $("#Collect_deposit").modal('hide');
        }
        function ShowPopup() {

            $("#modalCustomerMail").modal('show');
        }
        function ClosePopup() {

            $("#modalCustomerMail").modal('hide');
        }
        function OpenMailPopUp() {
            if ($("#_InvoiceNo").val() == '0') {
                $("#EnableSaveOnly").val('1')
                Swal.fire("Click Save before sending email.");
                return
            }

            var CustomerID = $("#SV_CustomeID").val();
            var InvoiceNo = $("#_InvoiceNo").val();

            $("#CustomerID").val(CustomerID);

            FillEmailModal(CustomerID, InvoiceNo);
            /* $("#_To").val(emailTo);*/



        }

        function FillEmailModal(customerid, InvoiceNo) {
            ShowPopup();
            //  alert("caled");

            //var doctype = $("#_Type").val();
            //var defaultTemplate = "AsaInvoice";

            //if (doctype == "Invoice") {
            //    defaultTemplate = "AsaInvoice";
            //}
            //if (doctype == "Proposal/Estimate") {
            //    defaultTemplate = "Estimate"
            //}

            //$("input[name='imgbackground'][value='" + defaultTemplate + "']").prop("checked", true);

            //Added My Munem
            var defaultTemplate = $("#PageTitle").text(); // This could come from server-side too
            var isLHG = $('#isLHG').val();

            if (isLHG === 'True') {
                if (defaultTemplate != "Proposal/Estimate") {
                    defaultTemplate = "AsaInvoice";
                }
                else {
                    $("#hf_CurrentPdfType").val('Estimate');
                }

            }


            var docType = "AsaInvoice";
            if (defaultTemplate == "Proposal/Estimate") {
                docType = "Estimate"
            }

            $("input[name='imgbackground'][value='" + docType + "']").prop("checked", true);
            //End
            //$("#hf_CurrentPdfType").val('AsaInvoice');
            $.ajax({
                url: 'Invoice.aspx/FillEmailModal',
                type: "Post",
                beforeSend: function () {
                    $("#imgSpinner1").show();
                },
                contentType: 'application/json',
                data: "{CustomerID:'" + customerid + "',InvoiceNo:'" + InvoiceNo + "',FromInvDtl:1}",
                dataType: 'json',
                success: function (sR) {
                    $("#imgSpinner1").hide();
                    if (sR.d != "0") {
                        var data = $.parseJSON(sR.d);
                        var doctype = $("#_Type").val();
                        var mailBodyType = $("#PageTitle").text();
                        // Added By Munem
                        //  document.getElementById('fixAttachment').innerHTML = " " + doctype + ".pdf";
                        if (doctype == "Invoice") {
                            $("#_EmailSubject").val(data.InvoiceMailSubject);
                            $("#EmailBody").val(data.InvoiceMailBody);

                        }
                        if (doctype == "Proposal" || mailBodyType == 'Proposal/Estimate') { //modified by Munem

                            $("#_EmailSubject").val(data.ProposalMailSubject);
                            $("#EmailBody").val(data.ProposalMailBody);
                        }
                        $("#txt_ReqDepo").val('$' + data.ReqDepoAmt.toFixed(2));
                        $("#_CC").val(data.EmailCC);
                        $("#_BCC").val(data.EmailBCC);
                        $("#_To").val(data.EmailTo);
                        $("#file").val();
                        $("#fileList").empty();
                        // document.getElementById('fileList').innerHTML='<li><span class="remove-list fa fa-check"></span>Proposal.pdf</li>';
                        $("#_InvoiceNo").val(InvoiceNo);

                    }

                },
                error: function (error) {
                    alert(JSON.stringify(error));
                }
            })

        }
        function sendProposal() {
            isFormChanged = false;
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
                        //window.__doPostBack("<%= btnSendProposalMail.UniqueID %>", "");

                } else if (result.isDenied) {
                    evt.preventDefault();

                }
                //return true;
            })

        }
        function sendInvoice() {
            isFormChanged = false;
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
                        //window.__doPostBack("<%= btnSendProposalMail.UniqueID %>", "");

                } else if (result.isDenied) {
                    evt.preventDefault();

                }
                //return true;
            })

        }

        function OpenMMSPopup() {
            var text = $("#EmailBody").val();
            var phone = $("#customerPhone").val();
            $("#txtMMSBody").val(text);
            $("#mmsPhone").val(phone);
            $('#showMMSBody').modal('show');
        }
        function CloseMMSPopup() {
            $("#txtMMSBody").val('');
            $("#showMMSBody").modal('hide');
        }



        document.getElementById("btn_SendMMS").addEventListener("click", function (e) {
            var sms = document.getElementById("txtMMSBody").value.trim();

            if (!sms) {
                e.preventDefault();

                Swal.fire({
                    title: 'Validation Error',
                    text: 'MMS Body cannot be empty.',
                    icon: 'warning',
                    didOpen: () => {
                        document.querySelector('.swal2-container').style.zIndex = '1000000000000005';
                    }
                });
            }
        });


    </script>

</asp:Content>
