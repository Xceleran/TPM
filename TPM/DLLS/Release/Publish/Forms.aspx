<%@ Page Title="Forms" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="Forms.aspx.cs" Inherits="FSM.Forms" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <!-- CSS Variables aligned with main site.css -->
    <link rel="stylesheet" href="Content/forms.css">

    <!-- Page Content -->
    <div class="forms-page-container">
        <div class="forms-header">
            <button type="button" class="btn btn-primary" onclick="openNewTemplateModal()">
                <i class="fa fa-plus"></i> New Template
            </button>
        </div>

        <div class="forms-content">
            <div class="border-b">
                <nav class="tabs-nav" role="tablist">
                    <button class="tabs-nav__btn is-active" 
                            data-tab-target="templates-section" 
                            role="tab" 
                            aria-selected="true" 
                            aria-controls="templates-section">
                        Form Templates
                    </button>
                    <button class="tabs-nav__btn" 
                            data-tab-target="usage-log-section" 
                            role="tab" 
                            aria-selected="false" 
                            aria-controls="usage-log-section">
                        Recent Form Activity
                    </button>
                </nav>
            </div>

            <!-- Form Templates Tab -->
            <div id="templates-section" class="tabs-content is-active" role="tabpanel">
                <div class="templates-section">
                    <div class="table-controls mb-3">
                        <div class="row align-items-center">
                            <div class="col-md-6 col-12">
                                <span id="templateRowCount" class="text-muted">0 rows selected</span>
                            </div>
                            <div class="col-md-6 col-12">
                                <div class="input-group justify-content-end">
                                    <input type="text" id="templateSearch" class="form-control" placeholder="Search templates..." onkeyup="searchTemplates()" style="max-width: 200px;">
                                    <span class="input-group-text">
                                        <i class="fa fa-search"></i>
                                    </span>
                                </div>
                            </div>
                        </div>
                        <div class="row align-items-center mt-2">
                            <div class="col-md-6 col-12">
                                <span>Rows per page: </span>
                                <select id="templatePageSize" onchange="changeTemplatePageSize()" class="form-select d-inline-block" style="width: auto;">
                                    <option value="5">5</option>
                                    <option value="10" selected>10</option>
                                    <option value="25">25</option>
                                    <option value="50">50</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-striped" id="templatesTable">
                            <thead>
                                <tr>
                                    <th>Template Name</th>
                                    <th>Category</th>
                                    <th>Auto-Assign</th>
                                    <th>Signature</th>
                                    <th>Status</th>
                                    <th>Created</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="templatesTableBody">
                                <!-- Templates will be loaded here -->
                            </tbody>
                        </table>
                    </div>
                    <div class="pagination-controls mt-3 d-flex justify-content-between align-items-center">
                        <span class="text-muted" id="templatePageInfo">Loading...</span>
                        <nav aria-label="Templates pagination">
                            <ul class="pagination mb-0">
                                <li class="page-item disabled" id="templatePrevPage">
                                    <a class="page-link" href="#" onclick="goToTemplatePage(templateCurrentPage - 1); return false;">Previous</a>
                                </li>
                                <li class="page-item" id="templateNextPage">
                                    <a class="page-link" href="#" onclick="goToTemplatePage(templateCurrentPage + 1); return false;">Next</a>
                                </li>
                            </ul>
                        </nav>
                    </div>
                </div>
            </div>

            <!-- Usage Log Tab -->
            <div id="usage-log-section" class="tabs-content" role="tabpanel" hidden>
                <div class="usage-log-section mt-4">
                    <div class="table-controls mb-3">
                        <div class="row align-items-center">
                            <div class="col-md-6 col-12">
                                <span id="usageLogRowCount" class="text-muted">0 rows selected</span>
                            </div>
                            <div class="col-md-6 col-12">
                                <div class="input-group justify-content-end">
                                    <input type="text" id="usageLogSearch" class="form-control" placeholder="Search activity..." onkeyup="searchUsageLog()" style="max-width: 200px;">
                                    <span class="input-group-text">
                                        <i class="fa fa-search"></i>
                                    </span>
                                </div>
                            </div>
                        </div>
                        <div class="row align-items-center mt-2">
                            <div class="col-md-6 col-12">
                                <span>Rows per page: </span>
                                <select id="usageLogPageSize" onchange="changeUsageLogPageSize()" class="form-select d-inline-block" style="width: auto;">
                                    <option value="10">10</option>
                                    <option value="25">25</option>
                                    <option value="50">50</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-sm" id="usageLogTable">
                            <thead>
                                <tr>
                                    <th>Date/Time</th>
                                    <th>Template</th>
                                    <th>Appointment</th>
                                    <th>Action</th>
                                    <th>Performed By</th>
                                </tr>
                            </thead>
                            <tbody id="usageLogTableBody">
                                <!-- Usage log will be loaded here -->
                            </tbody>
                        </table>
                    </div>
                    <div class="pagination-controls mt-3 d-flex justify-content-between align-items-center">
                        <span class="text-muted" id="usageLogPageInfo"></span>
                        <nav aria-label="Usage log pagination">
                            <ul class="pagination mb-0">
                                <li class="page-item" id="usageLogPrevPage">
                                    <a class="page-link" href="#" onclick="goToUsageLogPage(usageLogCurrentPage - 1)">Previous</a>
                                </li>
                                <li class="page-item" id="usageLogNextPage">
                                    <a class="page-link" href="#" onclick="goToUsageLogPage(usageLogCurrentPage + 1)">Next</a>
                                </li>
                            </ul>
                        </nav>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- New/Edit Template Modal -->
    <div class="modal fade" id="templateModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="templateModalTitle">New Form Template</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <form id="templateForm">
                        <input type="hidden" id="templateId" name="templateId" value="0" />
                        <div class="row">
                            <div class="col-md-8 col-12">
                                <div class="form-group">
                                    <label for="templateName">Template Name *</label>
                                    <input type="text" class="form-control" id="templateName" name="templateName" required>
                                </div>
                            </div>
                            <div class="col-md-4 col-12">
                                <div class="form-group">
                                    <label for="category">Category</label>
                                    <select class="form-control" id="category" name="category">
                                        <option value="">Select Category</option>
                                        <option value="Maintenance">Maintenance</option>
                                        <option value="Installation">Installation</option>
                                        <option value="Repair">Repair</option>
                                        <option value="Inspection">Inspection</option>
                                        <option value="Other">Other</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="description">Description</label>
                            <textarea class="form-control" id="description" name="description" rows="2"></textarea>
                        </div>
                        <div class="row">
                            <div class="col-md-6 col-12">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="requireSignature" name="requireSignature">
                                    <label class="form-check-label" for="requireSignature">Require Signature</label>
                                </div>
                            </div>
                            <div class="col-md-6 col-12">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="requireTip" name="requireTip">
                                    <label class="form-check-label" for="requireTip">Enable Tip Capture</label>
                                </div>
                            </div>
                        </div>
                        <div class="form-group mt-3">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="isAutoAssignEnabled" name="isAutoAssignEnabled">
                                <label class="form-check-label" for="isAutoAssignEnabled">Auto-assign to appointment types</label>
                            </div>
                        </div>
                        <div id="autoAssignSection" class="form-group" style="display: none;">
                            <label>Service Types for Auto-Assignment</label>
                            <div id="serviceTypeCheckboxes"></div>
                        </div>
                        <div class="form-group">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="isActive" name="isActive" checked>
                                <label class="form-check-label" for="isActive">Active</label>
                            </div>
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="saveTemplate()">Save Template</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Form Builder Modal -->
    <div class="modal fade" id="formBuilderModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Form Builder</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-12 col-md-4 col-lg-3">
                            <h6>Field Types</h6>
                            <div class="field-types">
                                <div class="field-type" draggable="true" data-type="text"><i class="fa fa-font"></i> Text Input</div>
                                <div class="field-type" draggable="true" data-type="textarea"><i class="fa fa-align-left"></i> Text Area</div>
                                <div class="field-type" draggable="true" data-type="number"><i class="fa fa-hashtag"></i> Number</div>
                                <div class="field-type" draggable="true" data-type="date"><i class="fa fa-calendar"></i> Date</div>
                                <div class="field-type" draggable="true" data-type="dropdown"><i class="fa fa-caret-down"></i> Dropdown</div>
                                <div class="field-type" draggable="true" data-type="checkbox"><i class="fa fa-check-square"></i> Checkbox</div>
                                <div class="field-type" draggable="true" data-type="radio"><i class="fa fa-dot-circle"></i> Radio Button</div>
                                <div class="field-type" draggable="true" data-type="signature"><i class="fa fa-pencil"></i> Signature</div>
                            </div>
                        </div>
                        <div class="col-12 col-md-8 col-lg-6">
                            <h6>Form Preview</h6>                         
                            <div id="formBuilder" class="form-builder-area">
                                <div class="drop-zone">Drag fields here to build your form</div>
                            </div>
                        </div>
                        <div class="col-12 col-md-12 col-lg-3">
                            <h6>Field Properties</h6>
                            <div id="fieldProperties" class="field-properties">
                                <p>Select a field to edit its properties</p>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="saveFormStructure()">Save Form Structure</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Include Signature Pad Library -->
    <script src="https://cdn.jsdelivr.net/npm/signature_pad@4.0.0/dist/signature_pad.umd.min.js"></script>
    
    
    <script src="Scripts/forms.js"></script>
    <script src="Scripts/signature-handler.js"></script>
    
    <script>
        // Debug function to test if everything is working
        function testNewTemplate() {
            console.log('Testing New Template functionality...');
            console.log('jQuery available:', typeof $ !== 'undefined');
            console.log('Bootstrap modal available:', typeof $.fn.modal !== 'undefined');
            console.log('Modal element exists:', $('#templateModal').length > 0);
            try {
                openNewTemplateModal();
                console.log('Modal opened successfully');
            } catch (error) {
                console.error('Error opening modal:', error);
            }
        }

        // Test when page loads
        $(document).ready(function () {
            console.log('Forms page loaded successfully');
            console.log('Available functions:', {
                openNewTemplateModal: typeof openNewTemplateModal,
                loadTemplates: typeof loadTemplates,
                showAlert: typeof showAlert
            });
        });
    </script>
</asp:Content>