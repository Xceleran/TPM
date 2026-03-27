<%--<%@ Page Title="Form Response" Language="C#" MasterPageFile="~/FSM.Master" AutoEventWireup="true" CodeBehind="FormResponse.aspx.cs" Inherits="FSM.FormResponse" %>--%>

<%--<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">--%>

  <!-- External Libraries -->
  <script src="Scripts/moment.js"></script>

  <!-- Leaflet Map Library -->
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
  <!-- Local Styles and Scripts -->
  <link rel="stylesheet" href="Content/appointments.css">
  <link rel="stylesheet" href="Content/form-response.css">

  <!-- Inline CSS for Expand/Collapse -->
  <style>
      .workorder-filters-row {
          display: flex;
          flex-direction: row;
          gap: 18px; /* space between the filters */
          align-items: flex-end;
      }

      /* Optional: Make them stack on mobile */
      @media (max-width: 600px) {
          .workorder-filters-row {
              flex-direction: column;
              gap: 10px;
          }
      }
  </style>

   <!-- Inline CSS for Expand/Collapse -->
   <style>
       .workorder-filters-row {
           display: flex;
           flex-direction: row;
           gap: 18px; /* space between the filters */
           align-items: flex-end;
       }

       /* Optional: Make them stack on mobile */
       @media (max-width: 600px) {
           .workorder-filters-row {
               flex-direction: column;
               gap: 10px;
           }
       }
   </style>
  <div class="col-md-8">
      <div id="formViewerContainer">
        
      </div>
       <div class="form-viewer-placeholder text-center p-5">
       <i class="fa fa-file-text-o fa-3x text-muted mb-3"></i>
       <p class="text-muted">Select a form to view or fill</p>
         <button type="button" class="btn btn-primary submit" onclick="submitResponse()">Submit</button>
    </div>
  </div>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.7.1/jquery.min.js" integrity="sha512-v2CJ7UaYy4JwqLDIrZUI/4hqeoQieOmAZNXBeQyjo21dadnwR+8ZaIJVT8EE2iyI61OV8e6M8PP2/4hpQINQ/g==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
  <script src="Scripts/form-response.js" defer></script>

<%--</asp:Content>--%>