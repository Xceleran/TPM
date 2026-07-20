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

    <!-- Portal configuration modal -->
    <div class="modal fade" id="portalConfigModal" tabindex="-1" aria-labelledby="portalConfigModalLabel" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="portalConfigModalLabel">Configure TP Portal</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" id="cfgThirdPartyId" />
                    <input type="hidden" id="cfgWarrantyId" />
                    <div class="mb-3">
                        <label class="form-label">Provider</label>
                        <input type="text" id="cfgProviderName" class="form-control" readonly />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Portal URL</label>
                        <input type="url" id="cfgPortalUrl" class="form-control" placeholder="https://portal.example.com" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">API Endpoint (optional)</label>
                        <input type="url" id="cfgApiEndpoint" class="form-control" placeholder="https://api.example.com/status" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Submission Method</label>
                        <select id="cfgSubmissionMethod" class="form-select">
                            <option value="Manual">Manual — opens portal link (no queue)</option>
                            <option value="API">API — sends to API Endpoint (failed calls go to queue)</option>
                            <option value="RPA">RPA — queues for Process Status Queue</option>
                        </select>
                        <div id="cfgMethodHelp" class="form-text text-muted mt-1"></div>
                    </div>
                    <div class="form-check mb-3">
                        <input class="form-check-input" type="checkbox" id="cfgIsEnabled" checked />
                        <label class="form-check-label" for="cfgIsEnabled">Enable portal integration</label>
                    </div>
                    <div id="cfgQueueTestSection" class="border rounded p-2 bg-light" style="display:none;">
                        <small class="text-muted d-block mb-2">After saving API/RPA config, queue a test status (requires at least one work order):</small>
                        <button type="button" class="btn btn-sm btn-outline-success" id="btnQueueTestStatus">
                            <i class="fas fa-plus-circle"></i> Queue test status
                        </button>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" id="btnSavePortalConfig">Save</button>
                </div>
            </div>
        </div>
    </div>

                <div class="col-md-6">
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
            <div class="card-body p-3">
                <div class="alert alert-info mb-3 py-2">
                    <strong><i class="fas fa-link me-1"></i> Portal setup:</strong>
                    Assign provider → <strong>⚙ Configure Portal</strong> → set method <strong>API</strong> or <strong>RPA</strong> → <strong>Queue test status</strong> → <strong>Process Status Queue</strong>.
                    <button type="button" class="btn btn-sm btn-outline-secondary ms-2" id="btnProcessStatusQueue" title="Process pending API/RPA status queue">
                        <i class="fas fa-sync"></i> Process Status Queue <span id="queuePendingBadge" class="badge bg-warning text-dark ms-1">0</span>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-primary ms-1" id="btnRefreshQueue">
                        <i class="fas fa-redo"></i> Refresh
                    </button>
                </div>
                <div id="statusQueuePanel" class="card mb-3" style="display:none;">
                    <div class="card-header py-2 d-flex justify-content-between align-items-center">
                        <span><i class="fas fa-list-alt me-1"></i> Status Queue</span>
                        <small class="text-muted">
                            Pending: <strong id="sqPending">0</strong> |
                            Processed: <strong id="sqProcessed">0</strong> |
                            Failed: <strong id="sqFailed">0</strong>
                        </small>
                    </div>
                    <div class="card-body p-0">
                        <div class="table-responsive">
                            <table class="table table-sm table-striped mb-0">
                                <thead><tr>
                                    <th>ID</th><th>Provider</th><th>Work Order</th><th>Status</th><th>Method</th><th>Queue</th><th>When</th>
                                </tr></thead>
                                <tbody id="statusQueueBody"></tbody>
                            </table>
                        </div>
                        <p id="statusQueueEmpty" class="text-muted text-center py-3 mb-0" style="display:none;">
                            No queue items yet. Configure a provider as API/RPA and click <strong>Queue test status</strong> in Configure Portal.
                        </p>
                    </div>
                </div>
            <div class="p-0"> 
                <div class="text-center ajax-loader">
                  <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                  </div>
                </div>
                <div class="table-responsive">
                    <table class="bill-table table table-bordered">
                        <thead class="table-light">
                            <tr>
                                <th style="width: 60px;">Actions</th>
                                <th style="width: 200px;">Name</th>
                                <th style="width: 250px;">Address</th>
                                <th style="width: 120px;">City</th>
                                <th style="width: 100px;">State</th>
                                <th style="width: 120px;">Zip</th>
                                <th style="width: 140px;">Assign / Portal</th>
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
            

            function isProviderAssigned(item) {
                return !!(item && (item.IsEnable === true || item.isEnable === true || item.IsAssigned === true || item.isAssigned === true));
            }

            function applyFilters() {
                const searchTerm = $('#searchBar').val().trim().toLowerCase();

                filteredData = itemData.filter(item => {
                    const combinedText = [
                        item.CompanyName, item.Address, item.City, item.State, item.Zip,
                        item.ShortName, item.CustomerID
                    ].join(' ').toLowerCase();
                    const matchesSearch = !searchTerm || combinedText.includes(searchTerm);
                    return matchesSearch;
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

                        const cid = (item.CustomerID || item.customerID || '').toString();
                        const cguid = (item.CustomerGuid || item.customerGuid || '').toString();
                        const cmobile = (item.Mobile || item.mobile || item.Phone || item.phone || '').toString();
                        const cname = (item.CompanyName || item.companyName || '').replace(/"/g, '');

                        var rowHtml = `
            <tr>
                <td>` + `<div class='dropdown'>
                                  <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action${serialNumber}' data-bs-toggle='dropdown' aria-expanded='false'>
                                    <i class='fas fa-align-justify'></i>
                                  </button>
                                  <ul class='dropdown-menu' aria-labelledby='Action${serialNumber}'> 
                                      <li><a class='dropdown-item portal-access-link' href='#' data-tp-id="${item.ThirdPartyId || 0}" data-portal-url="${item.PortalUrl || ''}"><i class="fas fa-external-link-alt"></i> Access Portal</a></li>
                                      <li><a class='dropdown-item configure-portal-link' href='#' data-tp-id="${item.ThirdPartyId || 0}" data-warranty-id="${item.Id}" data-company-name="${(item.CompanyName || '').replace(/'/g, '')}"><i class="fas fa-cog"></i> Configure Portal</a></li>
                                      <li><a class='dropdown-item create-invoice-link' href='#' data-warranty-id="${item.Id}" data-customer-guid="${cguid}" data-customer-id="${cid}">Create Invoice</a></li>
                                      <li><a class='dropdown-item push-portal-status' href='#' data-tp-id="${item.ThirdPartyId || 0}">Push Status to Portal</a></li>
                                      <li><a class='dropdown-item tp-view-invoices' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}">View Invoices</a></li>
                                      <li><a class='dropdown-item tp-view-files' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}">View Files</a></li>
                                      <li><a class='dropdown-item tp-send-email' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}">Send Email</a></li>
                                      <li><a class='dropdown-item tp-email-history' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}">Email History</a></li>
                                      <li><hr class='dropdown-divider'></li>
                                      <li><a class='dropdown-item tp-send-sms' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}" data-mobile="${cmobile}">Send Text</a></li>
                                      <li><a class='dropdown-item tp-send-sms' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}" data-mobile="${cmobile}">Send SMS</a></li>
                                      <li><a class='dropdown-item tp-text-history' href='#' data-warranty-id="${item.Id}" data-customer-id="${cid}" data-mobile="${cmobile}" data-name="${cname}">Text History</a></li>
                                        <li><hr class='dropdown-divider'></li>
                                      
                    </ul></div></td>` ;

                        

                            if (isProviderAssigned(item)) {
                                                        rowHtml += `<td><a class='' href='BusinessContact.aspx?WGID=${item.WarrantyCompanyGuID}'>${item.CompanyName || ''}</a></td>`;
                                              
                                                    }
                                    else {
                                        rowHtml += `<td>${item.CompanyName || ''}</td>`;
                                                
                                       
                }


                rowHtml +=`<td>${item.Address || ''}</td>
                <td>${item.City || ''}</td>
                <td>${item.State || ''}</td>
                <td>${(item.Zip)}</td>`;
                if (isProviderAssigned(item)) {
                            const rowHtmlAdd = `<td class="text-center">
                    <a class="btn btn-sm btn-outline-primary configure-portal-link me-1" href="#" title="Configure Portal"
                       data-tp-id="${item.ThirdPartyId || 0}" data-warranty-id="${item.Id}" data-company-name="${(item.CompanyName || '').replace(/'/g, '')}">
                        <i class="fas fa-cog"></i>
                    </a>
                    <span class="badge bg-success px-2 py-2" title="Assigned to your company">
                        <i class="fas fa-check me-1"></i> Assigned
                    </span>
                </td>
            </tr>
        `;
                     tbody.append(rowHtml + rowHtmlAdd);
                        }
                else {
                    const rowHtmlAdd = `<td class="text-center">
                    <a class="btn btn-sm btn-outline-primary configure-portal-link me-1" href="#" title="Configure Portal"
                       data-tp-id="${item.ThirdPartyId || 0}" data-warranty-id="${item.Id}" data-company-name="${(item.CompanyName || '').replace(/'/g, '')}">
                        <i class="fas fa-cog"></i>
                    </a>
                    <a class="btn btn-success btn-sm btn-AssignCompany" href="#" data-id="${item.Id}" title="Assign provider to your company">
                        <i class="fas fa-arrow-right" aria-hidden="true"></i> Assign
                    </a>
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
            
            
            function updateCfgMethodHelp() {
                var m = $('#cfgSubmissionMethod').val();
                var help = '';
                if (m === 'Manual') {
                    help = 'Opens the portal URL in your browser. Does not use the status queue.';
                    $('#cfgQueueTestSection').hide();
                } else if (m === 'API') {
                    help = 'Posts status to API Endpoint. If the call fails, items are added to the queue for retry.';
                    $('#cfgQueueTestSection').show();
                } else if (m === 'RPA') {
                    help = 'Queues status updates for Process Status Queue (external RPA worker or manual portal step).';
                    $('#cfgQueueTestSection').show();
                }
                $('#cfgMethodHelp').text(help);
            }

            function loadStatusQueue() {
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/GetStatusQueue',
                    type: 'POST', contentType: 'application/json', data: '{}',
                    success: function (rs) {
                        var d = rs.d || {};
                        if (!d.success) return;
                        var summary = d.summary || {};
                        var pending = d.pendingCount || summary.Pending || 0;
                        $('#queuePendingBadge').text(pending);
                        $('#sqPending').text(summary.Pending || 0);
                        $('#sqProcessed').text(summary.Processed || 0);
                        $('#sqFailed').text(summary.Failed || 0);

                        var tbody = $('#statusQueueBody');
                        tbody.empty();
                        var items = d.items || [];
                        if (items.length === 0) {
                            $('#statusQueuePanel').show();
                            $('#statusQueueEmpty').show();
                            return;
                        }
                        $('#statusQueuePanel').show();
                        $('#statusQueueEmpty').hide();
                        items.forEach(function (row) {
                            var when = row.createdDate ? new Date(parseInt(String(row.createdDate).replace(/\/Date\((\d+)\)\//, '$1'))).toLocaleString() : '';
                            var statusClass = row.status === 'Pending' ? 'warning' : (row.status === 'Processed' ? 'success' : 'danger');
                            tbody.append('<tr><td>' + row.id + '</td><td>' + (row.thirdPartyName || '-') + '</td><td>' + (row.workOrderNumber || '-') +
                                '</td><td>' + (row.statusCode || '-') + '</td><td>' + (row.submissionMethod || '-') +
                                '</td><td><span class="badge bg-' + statusClass + '">' + (row.status || '') + '</span></td><td><small>' + when + '</small></td></tr>');
                        });
                    }
                });
            }

            function showQueueProcessResults(d) {
                var html = '<p>' + (d.message || '') + '</p>';
                if (d.results && d.results.length) {
                    html += '<ul class="mb-0">';
                    d.results.forEach(function (r) {
                        html += '<li>' + (r.success ? '✓' : '✗') + ' #' + r.queueId + ' ' + (r.thirdPartyName || '') +
                            ' (' + (r.method || '') + '): ' + (r.message || '') + '</li>';
                    });
                    html += '</ul>';
                }
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ title: 'Status Queue', html: html, icon: d.processed > 0 ? 'success' : 'info' });
                } else {
                    alert(d.message + (d.results ? '\n' + d.results.map(function(r){ return r.message; }).join('\n') : ''));
                }
            }

            $('#cfgSubmissionMethod').on('change', updateCfgMethodHelp);

            $('#btnRefreshQueue').on('click', loadStatusQueue);

            $('#btnProcessStatusQueue').on('click', function () {
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/ProcessStatusQueue',
                    type: 'POST', contentType: 'application/json', data: '{}',
                    success: function (rs) {
                        var d = rs.d || {};
                        showQueueProcessResults(d);
                        loadStatusQueue();
                    },
                    error: function () { alert('Failed to process status queue.'); }
                });
            });

            $('#btnQueueTestStatus').on('click', function () {
                var tpId = parseInt($('#cfgThirdPartyId').val(), 10) || 0;
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/EnqueueTestPortalStatus',
                    type: 'POST', contentType: 'application/json',
                    data: JSON.stringify({ thirdPartyId: tpId, status: 'Acknowledged' }),
                    success: function (rs) {
                        var d = rs.d || {};
                        alert(d.message || (d.success ? 'Queued.' : 'Failed.'));
                        if (d.success) loadStatusQueue();
                    },
                    error: function () { alert('Failed to queue test status.'); }
                });
            });

            $('#itemList').on('click', '.portal-access-link', function (e) {
                e.preventDefault();
                var url = $(this).data('portal-url');
                if (url) window.open(url, '_blank');
                else alert('Portal URL not configured. Use Actions → Configure Portal to set it.');
            });

            $('#itemList').on('click', '.configure-portal-link', function (e) {
                e.preventDefault();
                var tpId = parseInt($(this).data('tp-id'), 10) || 0;
                var warrantyId = parseInt($(this).data('warranty-id'), 10) || 0;
                var name = $(this).data('company-name') || '';
                $('#cfgThirdPartyId').val(tpId);
                $('#cfgWarrantyId').val(warrantyId);
                $('#cfgProviderName').val(name);
                $('#cfgPortalUrl, #cfgApiEndpoint').val('');
                $('#cfgSubmissionMethod').val('Manual');
                $('#cfgIsEnabled').prop('checked', true);
                updateCfgMethodHelp();

                if (tpId > 0) {
                    $.ajax({
                        url: 'ThirdPartyProviders.aspx/GetApiConfig',
                        type: 'POST', contentType: 'application/json',
                        data: JSON.stringify({ thirdPartyId: tpId }),
                        success: function (rs) {
                            var d = rs.d || {};
                            if (d.portalUrl) $('#cfgPortalUrl').val(d.portalUrl);
                            if (d.apiEndpoint) $('#cfgApiEndpoint').val(d.apiEndpoint);
                            if (d.submissionMethod) $('#cfgSubmissionMethod').val(d.submissionMethod);
                            $('#cfgIsEnabled').prop('checked', d.isEnabled !== false);
                            updateCfgMethodHelp();
                        },
                        complete: function () {
                            new bootstrap.Modal(document.getElementById('portalConfigModal')).show();
                        }
                    });
                } else {
                    updateCfgMethodHelp();
                    new bootstrap.Modal(document.getElementById('portalConfigModal')).show();
                }
            });

            $('#btnSavePortalConfig').on('click', function () {
                var tpId = parseInt($('#cfgThirdPartyId').val(), 10) || 0;
                var warrantyId = parseInt($('#cfgWarrantyId').val(), 10) || 0;
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/SaveApiConfig',
                    type: 'POST', contentType: 'application/json',
                    data: JSON.stringify({
                        thirdPartyId: tpId,
                        warrantyCompanyId: warrantyId,
                        portalUrl: $('#cfgPortalUrl').val(),
                        apiEndpoint: $('#cfgApiEndpoint').val(),
                        submissionMethod: $('#cfgSubmissionMethod').val(),
                        isEnabled: $('#cfgIsEnabled').is(':checked')
                    }),
                    success: function (rs) {
                        var d = rs.d || {};
                        if (d.thirdPartyId) $('#cfgThirdPartyId').val(d.thirdPartyId);
                        bootstrap.Modal.getInstance(document.getElementById('portalConfigModal')).hide();
                        alert('Portal settings saved. ' + ($('#cfgSubmissionMethod').val() !== 'Manual'
                            ? 'Use "Queue test status" then "Process Status Queue".' : 'Use Access Portal for manual updates.'));
                        loadItems();
                        loadStatusQueue();
                    },
                    error: function () { alert('Failed to save portal settings.'); }
                });
            });

            function findProviderItem(warrantyId) {
                if (!warrantyId) return null;
                return itemData.find(function (x) {
                    return String(x.Id || x.id || '') === String(warrantyId);
                }) || null;
            }

            function resolveProviderContext(el) {
                var $el = $(el);
                var warrantyId = $el.attr('data-warranty-id') || '';
                var item = findProviderItem(warrantyId);
                var customerId = ($el.attr('data-customer-id') || '').trim();
                var customerGuid = ($el.attr('data-customer-guid') || '').trim();
                var mobile = ($el.attr('data-mobile') || '').trim();
                var name = ($el.attr('data-name') || '').trim();

                if (item) {
                    if (!customerId) customerId = String(item.CustomerID || item.customerID || '').trim();
                    if (!customerGuid) customerGuid = String(item.CustomerGuid || item.customerGuid || '').trim();
                    if (!mobile) mobile = String(item.Mobile || item.mobile || item.Phone || item.phone || '').trim();
                    if (!name) name = String(item.CompanyName || item.companyName || '').trim();
                }

                return {
                    warrantyId: warrantyId,
                    customerId: customerId,
                    customerGuid: customerGuid,
                    mobile: mobile,
                    name: name,
                    assigned: item ? isProviderAssigned(item) : false
                };
            }

            function requireAssignedCustomer(customerId, customerGuid, actionLabel, isAssigned) {
                if (customerId || customerGuid) return true;
                if (isAssigned) {
                    alert('This provider is assigned but the customer link is missing. Click Assign again or contact support. (' + (actionLabel || 'action') + ')');
                    return false;
                }
                alert('Assign this warranty company first (green "Assigned" badge in last column), then use ' + (actionLabel || 'this action') + '.');
                return false;
            }

            function tpCustomerDetailsUrl(customerId, tab, openAction, extraParams) {
                var url = 'CustomerDetails.aspx?custId=' + encodeURIComponent(customerId) + '&siteId=0';
                if (tab) url += '&tab=' + encodeURIComponent(tab);
                if (openAction) url += '&openAction=' + encodeURIComponent(openAction);
                if (extraParams) {
                    for (var key in extraParams) {
                        if (extraParams[key]) url += '&' + encodeURIComponent(key) + '=' + encodeURIComponent(extraParams[key]);
                    }
                }
                return url;
            }

            $('#itemList').on('click', '.tp-view-invoices', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'View Invoices', ctx.assigned)) return;
                window.location.href = tpCustomerDetailsUrl(ctx.customerId, 'invoices');
            });

            $('#itemList').on('click', '.tp-view-files', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'View Files', ctx.assigned)) return;
                window.location.href = tpCustomerDetailsUrl(ctx.customerId, 'files');
            });

            $('#itemList').on('click', '.tp-send-email', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'Send Email', ctx.assigned)) return;
                window.location.href = tpCustomerDetailsUrl(ctx.customerId, null, 'sendEmail');
            });

            $('#itemList').on('click', '.tp-email-history', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'Email History', ctx.assigned)) return;
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/GetEmailHistoryUrl',
                    type: 'POST', contentType: 'application/json',
                    data: JSON.stringify({ customerId: String(ctx.customerId) }),
                    success: function (rs) {
                        var d = rs.d || {};
                        if (d.success && d.url) window.open(d.url, '_blank');
                        else alert(d.message || 'Could not open email history.');
                    },
                    error: function () { alert('Failed to open email history.'); }
                });
            });

            $('#itemList').on('click', '.tp-send-sms', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'Send SMS', ctx.assigned)) return;
                if (!ctx.mobile) {
                    alert('No mobile/phone number on file for this provider. Update the contact in Business Contact first.');
                    return;
                }
                window.location.href = tpCustomerDetailsUrl(ctx.customerId, null, 'sendSms', { mobile: ctx.mobile });
            });

            $('#itemList').on('click', '.tp-text-history', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'Text History', ctx.assigned)) return;
                if (!ctx.mobile) {
                    alert('No mobile/phone number on file for this provider.');
                    return;
                }
                window.open('CustomerChatHistory.aspx?mobile=' + encodeURIComponent(ctx.mobile)
                    + '&name=' + encodeURIComponent(ctx.name)
                    + '&customerId=' + encodeURIComponent(ctx.customerId), '_blank');
            });

            $('#itemList').on('click', '.create-invoice-link', function (e) {
                e.preventDefault();
                var ctx = resolveProviderContext(this);
                if (!requireAssignedCustomer(ctx.customerId, ctx.customerGuid, 'Create Invoice', ctx.assigned)) return;
                if (!ctx.customerGuid) {
                    alert('Customer GUID missing for this provider. Try clicking Assign again.');
                    return;
                }
                window.location.href = 'InvoiceCreate.aspx?cId=' + encodeURIComponent(ctx.customerGuid) + '&InType=Invoice&InvNum=0';
            });

            $('#itemList').on('click', '.push-portal-status', function (e) {
                e.preventDefault();
                var woId = prompt('Enter Work Order ID to push status:');
                if (!woId) return;
                var parsed = parseInt(woId, 10);
                if (!parsed || parsed <= 0) {
                    alert('Enter a valid numeric work order ID.');
                    return;
                }
                $.ajax({
                    url: 'ThirdPartyProviders.aspx/PushStatusToPortal',
                    type: 'POST', contentType: 'application/json',
                    data: JSON.stringify({ workOrderId: parsed, status: 'Acknowledged' }),
                    success: function (rs) {
                        var d = rs.d || {};
                        if (d.requiresManual && d.manualUrl) window.open(d.manualUrl, '_blank');
                        alert(d.message || (d.success ? 'Status pushed.' : 'Failed to push status.'));
                    },
                    error: function () { alert('Failed to push status. Check work order ID and portal settings.'); }
                });
            });

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
                            const data = rs.d || {};
                            if (data.success) {
                                alert(data.message || 'Provider assigned successfully!');
                                loadItems();
                            } else {
                                alert(data.message || 'Assign failed. Check browser console and server logs.');
                            }
                        },
                        error: function (xhr, status, error) {
                            console.error("Assign error:", error, xhr.responseText);
                            alert('Assign request failed: ' + (error || status));
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
            loadStatusQueue();

        });
</script>

</asp:Content>