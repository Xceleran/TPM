<%@ Page Title="Settings" Language="C#" MasterPageFile="~/FSM.Master" AutoEventWireup="true" CodeBehind="SMSSettings.aspx.cs" Inherits="FSM.SMSSettings" %>



<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <br />  <br />
    <div class="d-flex flex-column-fluid home-1stsec">
        <div class="container">
            <div class="row">
                <div class="col-12 mb-3">
                    <div class="card card-custom gutter-b card-stretch p-0">


                        <div class="card-header bg-light">
                            <div class="card-title d-flex justify-content-between align-items-center w-100">

                                <div class="float-start">
                                    <h3 class="card-label">Check the types of communications you want to send to your customers</h3>
                                </div>


                            </div>
                        </div>

                        <input type="hidden" id="hdCompanyID" name="hdCompanyID" runat="server" />


                        <div class="card-body">
                            <div class="row mt-3">
                                <div class="col-12 col-sm-12 col-md-6">


                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Pending</b></legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="PendingYN" runat="server" />
                                                    </td>
                                                    <td align="left">
                                                        <label for="PendingYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtPending" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>

                                    <br />

                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Cancelled</b> </legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="CancelledYN" runat="server" /></td>
                                                    <td align="left">
                                                        <label for="CancelledYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtCancelled" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>

                                    <br />

                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Installation In Progress</b> </legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="ProgressYN" runat="server" /></td>
                                                    <td align="left">
                                                        <label for="ProgressYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtProgress" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>




                                </div>

                                <div class="col-12 col-sm-12 col-md-6">

                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Confirmed</b></legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="ScheduledYN" runat="server" />

                                                    </td>
                                                    <td align="left">
                                                        <label for="ScheduledYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtScheduled" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>

                                    <br />
                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Closed</b></legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="ClosedYN" runat="server" />

                                                    </td>
                                                    <td align="left">
                                                        <label for="ClosedYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtClosed" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>

                                    <br />
                                    <div class="row">
                                        <div class="col-12 mt-0">
                                            <legend>Appointment Status : <b>Completed</b></legend>

                                            <table style="text-align: center">
                                                <tr>
                                                    <td>
                                                        <asp:CheckBox ID="CompletedYN" runat="server" />

                                                    </td>
                                                    <td align="left">
                                                        <label for="CompletedYN">&nbsp; Add Yes/No Option</label></td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div class="col-12 mt-0">
                                            <table style="width: 100%;">
                                                <tr>
                                                    <td>Message Body</td>
                                                    <td align="right" style="font-size: 11px;"></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2">
                                                        <textarea id="txtCompleted" rows="5" runat="server" class="form-control"></textarea>

                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </div>



                                </div>


                            </div>

                            <div class="row">
                                <div class="col-6 col-sm-6 col-md-3">
                                    <asp:Button ID="SubmitData" runat="server" Text="Save" CssClass="btn btn-success btn-bluelight mt-3 w-100"  OnClick="SubmitData_Click"/>

                                </div>
                            </div>
                        </div>

                        <hr />
                        <div class="col-12 col-sm-12 col-md-12">
                            <div class="row" style="margin-left: 10px; margin-bottom: 10px;">
                                <div class="col-lg-4 mt-4">


                                    <small class="form-text text-black-50">[First Name] = Customer First Name</small><br />
                                    <small class="form-text text-black-50">[Last Name] = Customer last Name</small><br />
                                    <small class="form-text text-black-50">[Full Name] = Customer Full Name</small><br />
                                    <small class="form-text text-black-50">[Title] = Customer Title</small><br />



                                </div>
                                <div class="col-lg-4 mt-4">


                                    <small class="form-text text-black-50">[Job Title] = Customer Job Title</small><br />
                                    <small class="form-text text-black-50">[Company Name] = Company full name</small><br />
                                    <small class="form-text text-black-50">[Time] = Appointment Time</small><br />
                                    <small class="form-text text-black-50">[Date] = Appointment Date</small><br />



                                </div>


                            </div>

                        </div>


                    </div>
                </div>
            </div>
        </div>
    </div>



    <script>



</script>

</asp:Content>
