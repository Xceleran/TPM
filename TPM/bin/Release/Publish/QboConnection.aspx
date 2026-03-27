<%@ Page Title="Qbo Connection" Language="C#" AutoEventWireup="true" MasterPageFile="~/FSM.Master" CodeBehind="QboConnection.aspx.cs" Inherits="FSM.QboConnection" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <style type="text/css">
        .QboButtonStyle {
            background-image: url("./images/C2QB_green_btn_tall_default.png");
            width: 270px;
            height: 50px;
        }
    </style>

    <% 
        if (HttpContext.Current.Session["accessToken"] != null)
        {
            if (Request.QueryString.Count == 0)
            {
                Response.Write("<script> window.opener.location.reload();window.close(); </script>");
                HttpContext.Current.Session.Remove("ConnectClick");
            }

        }
    %>

    <div class="d-flex flex-column-fluid home-1stsec">
        <div class="container">
            <div class="row">
               
                    <asp:HiddenField ID="hdCompanyID" runat="server" />
                    <asp:HiddenField ID="hdCompanyName" runat="server" />
                    <asp:HiddenField ID="hdCompanyTag" runat="server" />

                    <asp:HiddenField ID="hdCompanyGUID" runat="server" />
                    <div class="col-12 mb-3">
                        <div class="card card-custom gutter-b card-stretch p-0">

                            <div id="dvBeforeConnect" runat="server" style="margin: 20px">
                                <div class="d-flex">
                                    <h3>You are not connected to QuickBooks Online</h3>
                                </div>
                                <div>
                                    <asp:Button ID="btnConnect" CssClass="QboButtonStyle" runat="server" class="btn btn-primary" OnClick="btnConnect_Click"></asp:Button>
                                </div>
                            </div>
                            <div id="dvAfterConnect" runat="server" style="margin: 20px">
                                <div class="d-flex">
                                    <h5>You are now connected to QuickBooks Online.</h5>
                                </div>
                               
                                 <div class="d-flex">
                                    <%-- <asp:Button ID="btnDisconnect" runat="server" Text="Disconnect QuickBooks Online" class="btn btn-primary w100pr mt-3" OnClick="btnDisconnect_Click"></asp:Button>--%>
                                    <a id="syncCustomer" class="btn btn-primary w100pr mt-3" href="javascript:CallDisconnect();" style="margin-left: 20px">Disconnect QuickBooks Online</a>
                                </div>
                                 <div class="d-flex">
                                    <h6 id="h_FileName" runat="server" >QuickBooks File :</h6>
                                </div>
                                <div class="float-start" style="margin-left:10px;margin-top:10px">
                                    <p style="display: none" id="ProgressGIF">
                                        <img id="imgProcess" src="images/Rolling.gif" />
                                    </p>
                                </div>
                            </div>

                            <div class="card-body">

                                <div class="row">
                                    <div class="col-12">
                                        <div class="row">


                                            <asp:HiddenField ID="HiddenField1" runat="server" />
                                            <asp:HiddenField ID="HiddenField2" runat="server" />
                                            <asp:HiddenField ID="HiddenField3" runat="server" />

                                            <asp:HiddenField ID="HiddenField4" runat="server" />

                                            <div class="row">
                                                <div class="col-12">
                                                    <div class="contentWrapper">
                                                        <div class="row">


                                                            <div class="col-12 table-responsive" style="overflow-y: hidden;">

                                                                <asp:PlaceHolder ID="ListTable" runat="server"></asp:PlaceHolder>

                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>

                                            </div>


                                        </div>
                                    </div>
                                </div>

                            </div>
                        </div>
                    </div>

             

            </div>

        </div>

    </div>
   
    <script>

        function CallDisconnect() {

            if (confirm("Do you want to disconnect from QuickBooks Online?")) {
                var prgBarDisc = document.getElementById("ProgressGIF");
                prgBarDisc.style.display = "block";

                $.ajax({
                    type: "POST",
                    url: "QboConnection.aspx/QBODisconnect",
                    contentType: "application/json",
                    dataType: "json",
                    success: function (msg) {
                        prgBarDisc.style.display = "none";
                        alert(msg.d);
                        location.href = "QboConnection.aspx";
                    }
                });
            } else {
                prgBarDisc.style.display = "none";
            }
        }

    </script>

</asp:Content>
