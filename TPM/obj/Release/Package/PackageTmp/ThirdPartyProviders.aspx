<%@ Page Title="" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="ThirdPartyProviders.aspx.cs" Inherits="TPM.ThirdPartyProviders" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <%-- jQuery (must be loaded before DataTables ) --%>
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>

    <%-- DataTables CSS --%>
    <link rel="stylesheet" href="https://cdn.datatables.net/1.13.6/css/dataTables.bootstrap5.min.css">
    <%-- Use same version as master page to avoid conflicts --%>
    <link rel="stylesheet" href="https://cdn.datatables.net/responsive/3.0.2/css/responsive.dataTables.min.css">

    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.14.0-beta3/css/bootstrap-select.min.css">

    <%-- Bootstrap JS (must be loaded before bootstrap-select) --%>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/js/bootstrap.bundle.min.js"></script>
    
    <%-- DataTables JS and Responsive extension --%>
    <script src="https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js"></script>
    <script src="https://cdn.datatables.net/1.13.6/js/dataTables.bootstrap5.min.js"></script>
    <%-- Use same version as master page to avoid conflicts --%>
    <script src="https://cdn.datatables.net/responsive/3.0.2/js/dataTables.responsive.min.js"></script>
    <!-- Add these for the multi-select dropdown -->
    <%-- Dropdown --%>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.14.0-beta3/js/bootstrap-select.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/js/bootstrap.bundle.min.js" integrity="sha384-MrcW6ZMFYlzcLA8Nl+NtUVF0sA7MsXsP1UyJoMp4YLEuNSfAP+JcXn/tWtIaxVXM" crossorigin="anonymous"></script>

    <style>
        #categoryFilter option {
            background: #ffffffcc !important;
            color: black;
        }

        select#itemType option {
            color: black;
        }

        .bill-container {
            width: 100%;
            margin-top: 25px;
            padding: 0 15px;
        }

        .bill-title {
            font-size: 32px;
            font-weight: bold;
            color: var(--text-orange-700);
        }

        [data-theme="dark"] .bill-title {
            color: var(--bg-orange-200);
        }

        [data-theme="dark"] .form-check {
            color: white;
        }

        .bill-table {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0;
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            border-radius: 8px;
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
        }

        [data-theme="dark"] .bill-table {
            background: rgba(255, 255, 255, 0.25);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        .bill-table th,
        .bill-table td {
            text-align: left;
            border-bottom: 1px solid var(--bg-gray-300);
            color: var(--text-gray-800);
        }

        [data-theme="dark"] .bill-table th,
        [data-theme="dark"] .bill-table td {
            color: var(--text-gray-700);
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        .bill-table th {
            font-size: 12px;
            text-transform: uppercase;
            background: var(--bg-gray-100);
            font-weight: 600;
        }

        .form-select {
            background-image: url('data:image/svg+xml;utf8,<svg fill="gray" height="20" viewBox="0 0 24 24" width="20" xmlns="http://www.w3.org/2000/svg"><path d="M7 10l5 5 5-5z"/></svg>' ) !important;
            background-repeat: no-repeat !important;
            background-position: right 0.5rem center !important;
            background-size: 1rem !important;
            -webkit-appearance: none;
            -moz-appearance: none;
            appearance: none;
            padding-right: 2rem !important;
        }

        [data-theme="dark"] .bill-table th {
            background: transparent;
            border-right: none;
            border-left: none;
        }

        .bill-table tbody tr:hover {
            background: var(--bg-orange-200);
        }

        [data-theme="dark"] .bill-table tbody tr:hover {
            background: rgba(255, 255, 255, 0.2);
        }

        .d-flex.align-items-center.gap-2 > span {
            color: var(--text-gray-800);
            font-size: 14px;
        }

        [data-theme="dark"] .d-flex.align-items-center.gap-2 > span {
            color: var(--text-gray-700);
        }

        .bill-edit-btn,
        .bill-delete-btn {
            font-weight: 600;
            background: none;
            border: none;
            cursor: pointer;
            padding: 0 8px;
        }

        .bill-edit-btn {
            color: var(--text-orange-700);
        }

            .bill-edit-btn:hover {
                color: var(--text-orange-500);
            }

        [data-theme="dark"] .bill-edit-btn {
            color: var(--bg-orange-200);
        }

            [data-theme="dark"] .bill-edit-btn:hover {
                color: rgb(176, 205, 235);
            }

        .bill-delete-btn {
            color: #dc2626;
        }

            .bill-delete-btn:hover {
                color: #b91c1c;
            }

        [data-theme="dark"] .bill-delete-btn {
            color: #ff4d4f;
        }

            [data-theme="dark"] .bill-delete-btn:hover {
                color: #ff7875;
            }

        .bill-image-view {
            display: none;
        }

        .bill-image-card {
            text-align: center;
            background: var(--bg-white);
            border: 1px solid var(--bg-gray-300);
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 15px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
        }

        [data-theme="dark"] .bill-image-card {
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
        }

        .bill-image {
            width: 128px;
            height: 128px;
            object-fit: cover;
            border-radius: 8px;
            margin-bottom: 8px;
            border: 1px solid var(--bg-gray-300);
        }

        [data-theme="dark"] .bill-image {
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        .bill-image-title {
            font-size: 18px;
            font-weight: 600;
            color: var(--text-gray-800);
        }

        [data-theme="dark"] .bill-image-title {
            color: var(--text-gray-700);
        }

        .bill-image-text {
            font-size: 14px;
            color: var(--text-gray-600);
        }

        [data-theme="dark"] .bill-image-text {
            color: var(--text-gray-700);
        }

        .bill-modal-image-preview {
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            display: none;
            border: 1px solid var(--bg-gray-300);
        }

        [data-theme="dark"] .bill-modal-image-preview {
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        .bill-modal-error {
            color: #dc2626;
            font-size: 14px;
            margin-top: 4px;
            display: none;
        }

        [data-theme="dark"] .bill-modal-error {
            color: #ff4d4f;
        }

        .form-select,
        .form-control {
            background: var(--bg-white);
            color: var(--text-gray-800);
            border: 1px solid var(--bg-gray-300);
            border-radius: 6px;
            padding: 8px;
            font-size: 14px;
        }

        [data-theme="dark"] .form-select,
        [data-theme="dark"] .form-control {
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            color: var(--text-gray-700);
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        .form-label {
            color: var(--text-gray-800);
            font-size: 14px;
        }

        [data-theme="dark"] .form-label {
            color: var(--text-gray-700);
        }

        .modal-content {
            background: var(--bg-white);
            border-radius: 8px;
            border: 1px solid var(--bg-gray-300);
            box-shadow: var(--shadow-lg);
        }

        [data-theme="dark"] .modal-content {
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
        }

        .modal-header {
            border-bottom: 1px solid var(--bg-gray-300);
        }

        [data-theme="dark"] .modal-header {
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        .modal-title {
            color: var(--text-gray-800);
        }

        [data-theme="dark"] .modal-title {
            color: var(--text-gray-700);
        }

        .modal-footer {
            border-top: 1px solid var(--bg-gray-300);
        }

        [data-theme="dark"] .modal-footer {
            border-top: 1px solid rgba(255, 255, 255, 0.1);
        }

        .edit-btn {
            font-size: 16px !important;
            color: #fff !important;
            cursor: pointer;
            border: none !important;
            background-size: 300% 100% !important;
            border-radius: 5px !important;
            transition: all .4s ease-in-out !important;
            background-image: linear-gradient(to right, #617dfc, #406fff, #3d6dd6, #0e407d);
            padding: 7px 15px;
        }

            .edit-btn:hover {
                background-position: 100% 0 !important;
                transition: all .4s ease-in-out !important;
            }

            .edit-btn:focus {
                outline: none !important;
            }

        .btn-secondary {
            background: #6b7280;
            border: none;
            color: #fff;
            border-radius: 6px;
            padding: 8px 16px;
            font-size: 14px;
        }

            .btn-secondary:hover {
                background: #4b5563;
            }

        .btn-primary {
            background: #5e7cfd;
        }

            .btn-primary:hover {
                background: #3d53b4;
            }

        .btn-outline-secondary {
            color: var(--text-gray-800);
            border-color: var(--bg-gray-300);
        }

        [data-theme="dark"] .btn-outline-secondary {
            color: var(--text-gray-700);
            border-color: rgba(255, 255, 255, 0.1);
        }

        .btn-outline-secondary:hover {
            background: var(--bg-orange-200);
            color: var(--text-gray-800);
        }

        [data-theme="dark"] .btn-outline-secondary:hover {
            background: rgba(255, 255, 255, 0.2);
            color: var(--text-gray-700);
        }

        tr td:nth-child(8) {
            text-align: center;
        }

        .card {
            border: none;
            border-radius: 8px;
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
        }

        [data-theme="dark"] .card {
            background: none;
            border: none;
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
        }

        .text-muted {
            color: var(--text-gray-600);
        }

        [data-theme="dark"] .text-muted {
            color: var(--text-gray-700);
        }

        @media (max-width: 576px) {
            .bill-table {
                font-size: 14px;
            }

                .bill-table th,
                .bill-table td {
                    padding: 8px;
                }

            .bill-image {
                width: 100px;
                height: 100px;
            }

            .bill-title {
                font-size: 18px;
            }

            .form-select,
            .form-control {
                font-size: 12px;
                padding: 6px;
            }

            .btn-primary,
            .btn-secondary,
            .btn-outline-secondary {
                font-size: 12px;
                padding: 6px;
            }

            .bill-image-title {
                font-size: 16px;
            }

            .bill-image-text {
                font-size: 12px;
            }
        }

        .custom-select-wrapper {
            position: relative;
            display: inline-block;
        }

            .custom-select-wrapper select {
                appearance: none;
                -webkit-appearance: none;
                -moz-appearance: none;
                padding-right: 2rem;
                background: white url('data:image/svg+xml;utf8,<svg fill="gray" height="20" viewBox="0 0 24 24" width="20" xmlns="http://www.w3.org/2000/svg"><path d="M7 10l5 5 5-5z"/></svg>') no-repeat right 0.5rem center !important;
                background-size: 1rem;
                color: var(--text-gray-800);
            }

        select#entriesPerPage option {
            color: black;
        }

        #categoryFilter option {
            background: #ffffffcc !important;
            color: black;
        }

        [data-theme="dark"] span#pageInfo {
            color: white;
        }

        [data-theme="dark"] #categoryFilter option {
            background: rgba(255, 255, 255, 0.12) !important;
            color: black;
        }

        [data-theme="dark"] .custom-select-wrapper select {
            background: rgba(255, 255, 255, 0.12) url('data:image/svg+xml;utf8,<svg fill="rgb(239,242,247)" height="20" viewBox="0 0 24 24" width="20" xmlns="http://www.w3.org/2000/svg"><path d="M7 10l5 5 5-5z"/></svg>') no-repeat right 0.5rem center !important;
            background-size: 1rem;
            color: var(--text-gray-700);
        }

        #loadingOverlay img {
            width: 60px;
            margin-bottom: 10px;
        }

        .category-card {
            border: 2px solid #c7c7c7;
            border-radius: 8px;
            padding: 10px;
            text-align: center;
            cursor: pointer;
            transition: all 0.3s ease;
            width: 120px;
            background: #ffffff;
        }

            .category-card:hover {
                border-color: #5e7cfd;
                background-color: rgba(94, 124, 253, 0.1);
            }

            .category-card.active {
                border-color: #5e7cfd;
                background-color: rgba(94, 124, 253, 0.2);
                font-weight: bold;
            }

        .category-image {
            width: 64px;
            height: 64px;
            object-fit: cover;
            border-radius: 50%;
            margin-bottom: 8px;
        }

        .category-name {
            font-size: 14px;
        }

        .manage-types-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            border-style: dashed;
        }

        .type-view-section {
            margin-bottom: 20px;
        }

        .type-group {
            margin-bottom: 30px;
        }

            .type-group h3 {
                font-size: 24px;
                color: var(--text-gray-800);
                margin-bottom: 15px;
                display: flex;
                align-items: center;
                gap: 10px;
            }

        [data-theme="dark"] .type-group h3 {
            color: var(--text-gray-700);
        }

        .type-group img {
            width: 32px;
            height: 32px;
            object-fit: cover;
            border-radius: 50%;
        }

        .type-item-card {
            background: var(--bg-white);
            border: 1px solid var(--bg-gray-300);
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 15px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        [data-theme="dark"] .type-item-card {
            background: rgba(255, 255, 255, 0.12);
            border: 1px solid rgba(255, 255, 255, 0.1);
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
        }

        .type-item-details {
            flex-grow: 1;
        }

            .type-item-details h5 {
                font-size: 18px;
                color: var(--text-gray-800);
                margin-bottom: 5px;
            }

        [data-theme="dark"] .type-item-details h5 {
            color: var(--text-gray-700);
        }

        .type-item-details p {
            font-size: 14px;
            color: var(--text-gray-600);
            margin: 0;
        }

        [data-theme="dark"] .type-item-details p {
            color: var(--text-gray-700);
        }

        .type-item-actions {
            display: flex;
            gap: 10px;
        }

        .categoryContainer {
            display: flex;
            margin: 10px auto;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 10px;
        }

        .btn-primary.qb {
            background: #0fd46c;
            border: none;
            color: #0d333f;
            font-weight: 600;
        }

        /* --- Add these styles for the 3-dot dropdown --- */
        .actions-dropdown .dropdown-toggle::after {
            display: none;
        }

        .actions-dropdown .btn-light {
            background-color: transparent;
            border: none;
        }

        .actions-dropdown .dropdown-menu {
            border-radius: 8px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            border: 1px solid #dee2e6;
            z-index: 1050;
            min-width: 150px;
        }

        [data-theme="dark"] .actions-dropdown .dropdown-menu {
            background-color: #343a40;
            border-color: rgba(255,255,255,0.15);
        }

        [data-theme="dark"] .actions-dropdown .dropdown-item {
            color: #f8f9fa;
        }

            [data-theme="dark"] .actions-dropdown .dropdown-item:hover {
                background-color: rgba(255, 255, 255, 0.1);
            }

        .dropdown-item {
            font-size: 14px;
            padding: .5rem 1rem;
            cursor: pointer;
        }

            .dropdown-item i {
                margin-right: 10px;
                width: 16px;
                text-align: center;
            }
    </style>

    <!-- SweetAlert2 CSS & JS -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css">
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>

    <input type="hidden" id="companyId" value="" runat="server" />

    <div class="bill-container">
        <header class="mb-4">
            <div class="row align-items-center">
            </div>
        </header>
        <section class="mb-4">
         
            <div class="row align-items-center">
                <div id="loadingOverlay" style="display: none; position: fixed; z-index: 9999; top: 0; left: 0; width: 100%; height: 100%; background-color: rgba(255, 255, 255, 0.8); text-align: center;">
                    <div style="position: relative; top: 40%;">
                        <div class="spinner-border text-success" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        
                        
                    </div>
                </div>
                <div class="col-md-6">
                     </div>
                <div class="col-md-6">
                    
                </div>
            </div>
        </section>
        <section class="mb-4">
            <div id="categoryImageContainer" class="categoryContainer">
                <!-- Category images will be dynamically inserted here by JavaScript -->
            </div>
            <div class="row align-items-center">
                <div class="col-md-6 mb-3 mb-md-0">
                    <div class="d-flex align-items-center gap-2">
                        <span>Show</span>
                        <div class="custom-select-wrapper">
                            <select id="entriesPerPage" class="form-select w-auto">
                                <option value="10">10</option>
                                <option value="25">25</option>
                                <option value="50">50</option>
                                <option value="100">100</option>
                            </select>
                        </div>
                        <span>entries</span>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="d-flex justify-content-md-end gap-2 flex-wrap">
                        <div class="custom-select-wrapper">
                           
                        </div>

                        <input type="hidden" id="selectedCategory" value="all" />
                        <input type="text" id="searchBar" class="form-control w-auto" placeholder="Search..." />
                        <button id="clearSearchBtn" class="btn btn-outline-secondary" type="button">Clear</button>
                    </div>
                </div>
            </div>
        </section>
        <%--<div class="btn-group">
  <button type="button" class="btn btn-danger">Action</button>
  <button type="button" class="btn btn-danger dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown" aria-expanded="false">
    <span class="visually-hidden">Toggle Dropdown</span>
  </button>
  <ul class="dropdown-menu">
    <li><a class="dropdown-item" href="#">Action</a></li>
    <li><a class="dropdown-item" href="#">Another action</a></li>
    <li><a class="dropdown-item" href="#">Something else here</a></li>
    <li><hr class="dropdown-divider"></li>
    <li><a class="dropdown-item" href="#">Separated link</a></li>
  </ul>
</div>--%>
        <section id="listView" class="card mb-4">
            <div class="card-body p-0"> 
                <div class="text-center ajax-loader">
                  <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                  </div>
                </div>
                <div class="table-responsive">
                    <table class="bill-table table table-bordered">
                        <thead class="table-light">
                            <tr>
                                <th style="width: 50px;">#</th>
                                <th style="width: 200px;">Name</th>
                                <th style="width: 250px;">Address</th>
                                <th style="width: 120px;">City</th>
                                <th style="width: 100px;">State</th>
                                <th style="width: 120px;">Zip</th>
                                <th style="width: 100px;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="itemList"></tbody>
                    </table>
                </div>
            </div>
        </section>

        <section id="typeView" class="type-view-section" style="display: none;">
            <div id="typeViewContent">
                <!-- Items grouped by type will be rendered here -->
            </div>
        </section>

        <footer class="d-flex justify-content-center align-items-center gap-3">
            <button class="btn btn-secondary" id="prevPage">Previous</button>
            <span id="pageInfo" class="fw-medium"></span>
            <button class="btn btn-primary" id="nextPage">Next</button>
            <p id="itemSummary" class="mt-3 text-muted"></p>
        </footer>

        <div class="modal fade" id="itemModal" tabindex="-1" aria-labelledby="modalTitle" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h2 class="modal-title fs-5" id="modalTitle">Add New Item</h2>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <form id="itemForm" class="modal-body">
                        <input type="text" id="Id" hidden="hidden" />
                        <input type="text" id="QboId" hidden="hidden" />
                        <div class="row p-3">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-medium">Item Group</label>
                                <select id="itemType" class="form-select"></select>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-medium">QBO Type</label>
                                <select id="qboType" class="form-select">
                                    <option value="0">Select QBO Type</option>
                                    <option value="1">Service</option>
                                    <option value="2">Non-Inventory</option>
                                    <option value="3">Inventory</option>
                                    <option value="4">Other Charge</option>
                                    <option value="5">Payment</option>
                                    <option value="6">Discount</option>
                                    <option value="7">Sales Tax</option>
                                    <option value="8">Bundle</option>
                                </select>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-medium">Item Name</label>
                                <input name="itemName" type="text" id="itemName" class="form-control" required />
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-medium">Location</label>
                                <input name="location" type="text" id="location" class="form-control" />
                            </div>


                            <div class="col-md-2 mb-3">
                                <label class="form-label fw-medium">Quantity</label>
                                <input name="quantity" type="number" id="quantity" class="form-control" step="1" />
                            </div>
                            <div class="col-md-3 mb-3">
                                <label class="form-label fw-medium">Price</label>
                                <input name="price" type="number" id="price" class="form-control" step="0.01" required />
                            </div>

                            <div id="newCategorySection" class="row" style="display: none;">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label fw-medium">New Category Name</label>
                                    <input type="text" id="newCategoryName" class="form-control" />
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label fw-medium">Category Image URL</label>
                                    <input type="text" id="newCategoryImageUrl" class="form-control" />
                                </div>
                            </div>

                            <div id="bundleImageSection" class="col-12 mb-3" style="display: none;">
                                <label class="form-label fw-medium">Bundle Image (for Field App)</label>
                                <div class="d-flex align-items-center gap-3">
                                    <div id="bundleImagePreview" style="display: none;">
                                        <img id="bundleImagePreviewImg" src="" alt="Bundle Image" 
                                             style="width: 128px; height: 128px; object-fit: cover; border-radius: 8px; border: 1px solid #ddd;" />
                                    </div>
                                    <div class="flex-grow-1">
                                        <input type="file" id="bundleImageUpload" accept="image/*" class="form-control" />
                                        <small class="text-muted">Image will be displayed in Field App for bundle selection</small>
                                    </div>
                                </div>
                            </div>

                            <div id="groupItemsSection" class="col-12 mb-3" style="display: none;">
                                <label class="form-label fw-medium">Items included in the bundle</label>
                                <div class="form-check mb-2">
                                    <input class="form-check-input" type="checkbox" id="displayBundleItems">
                                    <label class="form-check-label" for="displayBundleItems">
                                        Display bundle items when printing or sending transactions
                                    </label>
                                </div>
                                <table class="table table-bordered table-sm" id="bundleItemsTable">
                                    <thead class="table-light">
                                        <tr>
                                            <th style="width: 10px;"></th>
                                            <th>ITEM NAME</th>
                                            <th style="width: 100px;">QTY</th>
                                            <th style="width: 80px;">Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody id="bundleItemsTableBody">
                                        <!-- Bundle items will be populated here -->
                                    </tbody>
                                </table>
                                <button type="button" class="btn btn-sm btn-outline-primary mt-2" id="addBundleItemBtn">
                                    <i class="fa fa-plus"></i> Add Item
                                </button>
                            </div>
                            <div class="col-md-5 mb-3">
                                <label class="form-label fw-medium">Sku</label>
                                <input name="Sku" type="text" id="Sku" class="form-control" />
                            </div>
                            <div class="col-md-2 mb-3">
                                <label class="form-label fw-medium">Taxable</label>
                                <div class="d-flex gap-3">
                                    <div class="form-check">
                                        <input type="radio" name="taxable" value="1" id="taxYes" class="form-check-input" />
                                        <label class="form-check-label" for="taxYes">Yes</label>
                                    </div>
                                    <div class="form-check">
                                        <input type="radio" name="taxable" value="0" id="taxNo" class="form-check-input" checked />
                                        <label class="form-check-label" for="taxNo">No</label>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-12 mb-3">
                                <label class="form-label fw-medium">Description</label>
                                <textarea name="description" id="description" class="form-control"></textarea>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" id="cancelBtn" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" onclick="updateItem(event)" class="btn btn-primary" id="submitBtn">Add Item</button>
                        </div>
                    </form>
                </div>
            </div>
        </div>

      
    </div>

    <script>

        $(document).ready(function () {
             $(".dropdown-toggle").dropdown();
            let itemData = [];
            let filteredData = [];
            let currentPage = 1;
            let pageSize = 10;
            let itemTypes = [];

            function initialize() {
                $('#listView').show();
                $('#typeView').hide();
                $('#categoryImageContainer').show();
            }

            function loadItems() {
                console.log("Attempting to load items via AJAX...");
                 $('.ajax-loader').css("visibility", "visible");
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/GetBillableItems',
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: {},
                    dataType: 'json',
                    success: function (rs) {
                        console.log("Full response:", rs);
                        console.log("Successfully received items data:", rs.d);
                        itemData = rs.d || [];
                        console.log("Item data count:", itemData.length);
                        if (itemData.length === 0) {
                            console.warn("No items returned from server. Check server logs for CompanyID and query execution.");
                        }
                        currentPage = 1;
                        applyFilters();
                       
                    },
                    error: function (xhr, status, error) {
                        console.error("Error loading items:", error);
                        console.error("XHR Status:", xhr.status);
                        console.error("XHR Response:", xhr.responseText);
                        console.error("Full XHR:", xhr);
                          $('.ajax-loader').css("visibility", "hidden");
                    }
                });
            }
            

            function applyFilters() {
                const searchTerm = $('#searchBar').val().trim().toLowerCase();
                const selectedType = $('#selectedCategory').val().toLowerCase();
                const viewType = $('#viewToggle').val(); // all, items, groups, bundles

                filteredData = itemData.filter(item => {
                    const typeName = (itemTypes.find(t => t.Id === item.ItemTypeId)?.Name || "").toLowerCase();
                    const matchesType = selectedType === 'all' || selectedType === "" || typeName === selectedType;

                    // Filter by view type (All, Items, Groups, Bundles)
                    let matchesView = true;
                    if (viewType === 'items') {
                        matchesView = item.QboType !== 8 && !item.IsGroup; // Not bundle, not group
                    } else if (viewType === 'groups') {
                        matchesView = item.IsGroup === true; // Is a group
                    } else if (viewType === 'bundles') {
                        matchesView = item.QboType === 8; // Is a bundle
                    }
                    // viewType === 'all' means show everything

                    const combinedText = [item.ItemName, item.typeName, item.Description, item.Sku, item.Quantity, item.Price, item.Taxable].join(' ').toLowerCase();
                    const matchesSearch = combinedText.includes(searchTerm);
                    return matchesType && matchesSearch && matchesView;
                });
                currentPage = 1;
                renderItems();
                updatePagination();
            }

            // REPLACE your old renderItems function with this one
            function renderItems() {
                const startIndex = (currentPage - 1) * pageSize;
                const pageData = filteredData.slice(startIndex, startIndex + pageSize);
                const tbody = $('#itemList');
                tbody.empty();

                if (pageData.length === 0) {
                    tbody.append('<tr><td colspan="10" class="text-center">No items found.</td></tr>');
                    updatePagination();
                    return;
                }

                // Load bundle items for all bundles in current page
                const bundleIds = pageData.filter(item => item.QboType === 8).map(item => item.Id);
                const bundleItemsMap = {};

                // Function to render items rows
                function renderItemsRows() {
                    pageData.forEach((item, index) => {
                        const serialNumber = startIndex + index + 1;
                        const typeName = itemTypes.find(t => t.Id === item.ItemTypeId)?.Name || "";
                        const bundleItems = bundleItemsMap[item.Id] || '';

                        var rowHtml = `
            <tr>
                <td>` + `<div class='dropdown'>
                                  <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action${serialNumber}' data-bs-toggle='dropdown' aria-expanded='false'>
                                    <i class='fas fa-align-justify'></i>
                                  </button>
                                  <ul class='dropdown-menu' aria-labelledby='Action${serialNumber}'> 
                                      <li><a class='dropdown-item' href='#'>Access Portal</a></li>
                                      <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Create Invoice</a></li>
                                      <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>View Invoices</a></li>
                                       <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>View Files</a></li>
                                        <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Send Email</a></li>
                                        <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Email History</a></li>
                                        <li><hr class='dropdown-divider'></li>
                                        <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Send Text</a></li>
                                        <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Send SMS</a></li>
                                        <li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId='>Text History</a></li>
                                        <li><hr class='dropdown-divider'></li>
                                      
                    </ul></div></td>` ;

                        

                            if (item.IsEnable) {
                                                        rowHtml += `<td><a class='' href='BusinessContact.aspx?WGID=${item.WarrantyCompanyGuID}'>${item.CompanyName || ''}</a></td>`;
                                              
                                                    }
                                    else {
                                        rowHtml += `<td>${item.CompanyName || ''}</td>`;
                                                
                                       
                }


                rowHtml +=`<td>${item.Address || ''}</td>
                <td>${item.City || ''}</td>
                <td>${item.State || ''}</td>
                <td>${(item.Zip)}</td>`;
                if (item.IsEnable) {
                            const rowHtmlAdd = `<td class="text-center">
                    <div class="dropdown actions-dropdown">
                        <a class="btn btn-primary" href="#" data-id="${item.Id}">
                                    <i class="fa fa-check" aria-hidden="true"></i>
                                </a>
                    </div>
                </td>
            </tr>
        `;
                     tbody.append(rowHtml + rowHtmlAdd);
                        }
                else {
                    const rowHtmlAdd = `<td class="text-center">
                    <div class="dropdown actions-dropdown">
                        <a class="btn-secondary btn-AssignCompany" href="#" data-id="${item.Id}">
                                    <i class="fa fa-arrow-right" aria-hidden="true"></i>

                                </a>
                    </div>
                </td>
            </tr>
        `;
                     tbody.append(rowHtml + rowHtmlAdd);
                }
                
                       
                    });
                }
                  $('.ajax-loader').css("visibility", "hidden");
                // Fetch bundle items asynchronously
                if (bundleIds.length > 0) {


                        renderItemsRows();


                }
                else {
                    renderItemsRows();
                }

                const endIndex = Math.min(startIndex + pageSize, filteredData.length);
                $('#itemSummary').text(`Showing ${startIndex + 1}–${endIndex} of ${filteredData.length} items`);
                updatePagination();
            }
             

            function updatePagination() {
                const totalPages = Math.ceil(filteredData.length / pageSize);
                $('#pageInfo').text(`Page ${currentPage} of ${totalPages || 1}`);
                $('#prevPage').prop('disabled', currentPage <= 1);
                $('#nextPage').prop('disabled', currentPage >= totalPages);
            }
            

            let bundleItems = []; // Array of {SubItemId, Quantity, ItemName}

        
           

            function resetTypeForm() {
                $('#itemTypeId').val('0');
                $('#itemTypeForm')[0].reset();
                $('#itemTypeImagePreview').attr('src', '#').hide();
            }
            
            
               $('#itemList').on('click', '.btn-AssignCompany', function (e) {
                e.preventDefault();
                e.stopPropagation(); // Prevent event bubbling
                const WarrentyCompanyID = $(this).data('id');

                if (WarrentyCompanyID) {
                    $.ajax({
                        url: 'ThirdPartyProviders.aspx/AssignWarrentyCompany',
                        type: "POST",
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({ WarrentyCompanyID: WarrentyCompanyID }),
                        dataType: 'json',
                        success: function (rs) {
                            const data = rs.d;
                            alert('Customer Assigned successfully!');
                           loadItems();
                         
                           
                        },
                        error: function (error) {
                            console.error("Error fetching item:", error);
                        }
                    });
                }
            });

            $('#prevPage').click(function () {
                if (currentPage > 1) {
                    currentPage--;
                    renderItems();
                    updatePagination();
                }
            });

            $('#nextPage').click(function () {
                const totalPages = Math.ceil(filteredData.length / pageSize);
                if (currentPage < totalPages) {
                    currentPage++;
                    renderItems();
                    updatePagination();
                }
            });

            $('#searchBar').on('input', function () { applyFilters(); });

            $('#clearSearchBtn').click(function () {
                $('#searchBar').val('');
                $('#selectedCategory').val('all');
                $('#categoryImageContainer .category-card').removeClass('active');
                $('#categoryImageContainer .category-card[data-category-id="all"]').addClass('active');
                applyFilters();
            });

            $('#entriesPerPage').on('change', function () {
                pageSize = parseInt($(this).val());
                currentPage = 1;
                renderItems();
                updatePagination();
            });

            $('#itemType').on('change', function () {
                if ($(this).val() === 'add_new') {
                    $('#newCategorySection').show();
                } else {
                    $('#newCategorySection').hide();
                }
            });

       

        
     

            $('#clearTypeFormBtn').on('click', resetTypeForm);

       


         
            

            $('#searchItemsForGroup').on('input', function () {
                const searchTerm = $(this).val().toLowerCase();
                $('#availableItemsList label').each(function () {
                    const text = $(this).text().toLowerCase();
                    $(this).toggle(text.includes(searchTerm));
                });
            });

           
         

         

            console.log("jQuery is ready. Initializing script...");
           
            
            loadItems();

        });
</script>

</asp:Content>