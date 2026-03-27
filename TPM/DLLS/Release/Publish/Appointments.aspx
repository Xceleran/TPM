<%@ Page Title="Appointments" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true"
    CodeBehind="Appointments.aspx.cs" Inherits="TPM.Appointments" %>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <!-- External Libraries -->
    <script src="Scripts/moment.js"></script>
    <!-- Local Styles and Scripts -->
    <link rel="stylesheet" href="Content/appointments.css">

    <div id="page-loader"
        style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.8); z-index: 9999; display: flex; align-items: center; justify-content: center;">
        <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
    <!-- Page Content -->
    <div class="container-fluid" data-google-maps-api-key="<%= this.GoogleMapsApiKey %>">
        <header class="">
            <div class="row align-items-center">
                <div class="d-flex justify-content-between align-items-center flex-wrap col-6 gap-2 mt-2 mb-2">
                    <div class="d-flex flex-wrap gap-2 align-items-center">
                        <select id="viewSelect" class="form-select w-120px">
                            <option value="day">Day</option>
                            <option value="week">Week</option>
                            <option value="threeDay">Three-Day</option>
                            <option value="month">Month</option>
                            <option value="custom">Custom</option>
                        </select>
                        <div id="dateCustomDateRangeContainer"
                            class="custom-date-range-container d-none d-flex align-items-center gap-2">
                            <div class="d-flex align-items-center gap-1">
                                <label for="datePickerFrom" class="form-label mb-0"
                                    style="font-size: 12px;">
                                    From:</label>
                                <input type="date" id="datePickerFrom" class="form-control form-control-sm" />
                            </div>
                            <div class="d-flex align-items-center gap-1">
                                <label for="datePickerTo" class="form-label mb-0"
                                    style="font-size: 12px;">
                                    To:</label>
                                <input type="date" id="datePickerTo" class="form-control form-control-sm" />
                            </div>
                            <button type="button" id="dateCustomDateSearch"
                                class="btn btn-primary btn-sm">
                                Search</button>
                        </div>
                        <input type="date" id="dayDatePicker" class="form-control w-200px">
                    </div>
                </div>
                <div class="col-6">
                    <div class="cec-btn">
                        <ul class="nav nav-tabs gap-1" id="viewTabs" role="tablist">
                            <li class="nav-item">
                                <button class="nav-link active" id="date-tab" data-bs-toggle="tab"
                                    data-bs-target="#dateView" type="button" role="tab">
                                    Date View</button>
                            </li>
                            <li class="nav-item">
                                <button class="nav-link" id="resource-tab" data-bs-toggle="tab"
                                    data-bs-target="#resourceView" type="button" role="tab">
                                    Resource View</button>
                            </li>
                            <li class="nav-item">
                                <button class="nav-link" id="list-tab" data-bs-toggle="tab"
                                    data-bs-target="#listView" type="button" role="tab">
                                    List View</button>
                            </li>
                            <li class="nav-item">
                                <button class="nav-link" id="map-tab" data-bs-toggle="tab" data-bs-target="#mapView"
                                    type="button" role="tab">
                                    Map View</button>
                            </li>
                        </ul>
                        <asp:HyperLink ID="cecAppointmentsLink" runat="server" CssClass="custom-launch-btn"
                            Role="button" Target="_blank">
                                <span>
                                    <span>CEC Appointments</span>
                                    <span aria-hidden="true">
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"
                                            stroke-width="1.5" stroke="currentColor">
                                            <path stroke-linecap="round" stroke-linejoin="round"
                                                d="M4.5 19.5l15-15m0 0H8.25m11.25 0v11.25" />
                                        </svg>
                                    </span>
                                </span>
                        </asp:HyperLink>
                    </div>
                </div>
            </div>

        </header>
        <div class="tab-content">
            <div class="tab-pane fade show active" id="dateView" role="tabpanel">
                <div class="date-view-container">
                    <div class="card calendar-container date-view">
                        <div class="card-header">
                            <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                                <div class="d-flex flex-wrap gap-2 align-items-center">
                                    <button type="button" id="expandCalendarBtn"
                                        class="btn btn-outline-secondary me-2" data-bs-toggle="tooltip"
                                        title="Expand/Collapse Calendar">
                                        <i class="fas fa-expand"></i>
                                    </button>
                                    <div>
                                        <select id="dispatchGroupDateView" class="form-select w-auto"
                                            onchange="renderDateView($('#dayDatePicker').val())">
                                            <option value="all">All resource group</option>
                                        </select>
                                    </div>
                                    <div>
                                        <select id="individualResourceFilterDateView" class="form-select w-auto"
                                            onchange="renderDateView($('#dayDatePicker').val())">
                                            <option value="all">All individual resources</option>
                                        </select>
                                    </div>
                                    <div>
                                        <select name="ServiceTypeFilter" id="ServiceTypeFilter" class="form-select"
                                            runat="server" onchange="renderDateView($('#dayDatePicker').val())">
                                        </select>
                                    </div>
                                    <div>
                                        <select name="StatusTypeFilter_DateView" id="StatusTypeFilter_DateView"
                                            class="form-select" runat="server"
                                            onchange="renderDateView($('#dayDatePicker').val())">
                                            <option value="all">Select a Status</option>
                                        </select>
                                    </div>
                                    <div>
                                        <select name="TicketStatusFilter_DateView" id="TicketStatusFilter_DateView"
                                            class="form-select" runat="server"
                                            onchange="renderDateView($('#dayDatePicker').val())">
                                            <option value="all">Select a Ticket Status</option>
                                        </select>
                                    </div>
                                </div>

                                <button id="toggleUnscheduledBtn" class="btn btn-sm">
                                    <i
                                        class="fas fa-chevron-right"></i>
                                </button>
                            </div>
                            <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                                <div class="appt-type-indicators">
                                </div>
                                <div class="color-toggle-container d-flex align-items-center mt-2 gap-2">
                                    <label class="mb-0 small">Color by:</label>
                                    <div class="btn-group" role="group" id="colorToggleGroup">
                                        <input type="radio" class="btn-check" name="colorToggle" id="colorByType"
                                            value="type" checked>
                                        <label class="btn btn-sm btn-outline-primary" for="colorByType">
                                            Appointment
                                                Type</label>

                                        <input type="radio" class="btn-check" name="colorToggle" id="colorByStatus"
                                            value="status">
                                        <label class="btn btn-sm btn-outline-primary"
                                            for="colorByStatus">
                                            Status</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="card-body">
                            <div class="datepicker">
                                <div class="date-nav" id="dateNav"></div>
                            </div>
                            <div id="dateViewContainer">
                                <div id="dateViewLoading" class="loading-overlay" style="display: none;">
                                    <div class="spinner-border text-primary" role="status">
                                        <span class="visually-hidden">Loading...</span>
                                    </div>
                                </div>
                                <!-- The calendar will be rendered inside here -->
                                <div id="dayCalendar"></div>
                            </div>
                        </div>
                    </div>
                    <div class="card unscheduled-panel">
                        <div class="card-header">
                            <div class="d-flex justify-content-between align-items-center">
                                <h3 class="card-title mb-0">Appointment List</h3>
                                <%-- <button type="button" id="sortUnscheduledBtn"
                                        class="btn btn-sm btn-outline-secondary" onclick="performSort('date')"
                                        title="Sort by date">
                                        <i class="fas fa-sort-amount-up"></i>
                                        </button>--%>
                            </div>
                        </div>
                        <div class="card-body">
                            <!--Dropdowns Under One Filter-->
                            <div class="d-flex flex-wrap align-items-center gap-3 mb-3">
                                <!-- Filter Dropdown -->
                                <div class="dropdown">
                                    <button class="btn filter-btn dropdown-toggle d-flex align-items-center gap-2"
                                        type="button" id="unscheduledFilterBtn" data-bs-toggle="dropdown"
                                        aria-expanded="false">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
                                            xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                                            <path
                                                d="M21 6H19M21 12H16M21 18H16M7 20V13.5612C7 13.3532 7 13.2492 6.97958 13.1497C6.96147 13.0615 6.93151 12.9761 6.89052 12.8958C6.84431 12.8054 6.77934 12.7242 6.64939 12.5617L3.35061 8.43826C3.22066 8.27583 3.15569 8.19461 3.10948 8.10417C3.06849 8.02393 3.03853 7.93852 3.02042 7.85026C3 7.75078 3 7.64677 3 7.43875V5.6C3 5.03995 3 4.75992 3.10899 4.54601C3.20487 4.35785 3.35785 4.20487 3.54601 4.10899C3.75992 4 4.03995 4 4.6 4H13.4C13.9601 4 14.2401 4 14.454 4.10899C14.6422 4.20487 14.7951 4.35785 14.891 4.54601C15 4.75992 15 5.03995 15 5.6V7.43875C15 7.64677 15 7.75078 14.9796 7.85026C14.9615 7.93852 14.9315 8.02393 14.8905 8.10417C14.8443 8.19461 14.7793 8.27583 14.6494 8.43826L11.3506 12.5617C11.2207 12.7242 11.1557 12.8054 11.1095 12.8958C11.0685 12.9761 11.0385 13.0615 11.0204 13.1497C11 13.2492 11 13.3532 11 13.5612V17L7 20Z"
                                                stroke="currentColor" stroke-width="1.5" stroke-linecap="round"
                                                stroke-linejoin="round">
                                            </path>
                                        </svg>
                                        Filter

                                    </button>
                                    <div class="dropdown-menu p-3" aria-labelledby="unscheduledFilterBtn"
                                        style="min-width: 320px;">
                                        <select id="ResourceTypeFilter_2" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">All Resource Status</option>
                                            <option value="unassigned">Unassigned</option>
                                            <option value="assigned">Assigned</option>
                                        </select>
                                        <select runat="server" id="StatusTypeFilter" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">Select a Status</option>
                                        </select>
                                        <select runat="server" id="ServiceTypeFilter_2" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">Select a Service</option>
                                            <option value="IT Support">IT Support</option>
                                            <option value="1 Hour">1 Hour</option>
                                            <option value="2 Hour">2 Hour</option>
                                        </select>
                                        <select id="CountryFilter" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">All Countries</option>
                                            <option value="Canada">Canada</option>
                                            <option value="USA">USA</option>
                                        </select>
                                        <select id="ProvinceFilter" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">All Provinces/Territories</option>
                                            <!-- Options will be populated by JavaScript -->
                                        </select>
                                        <select id="PostalCodeFilter" class="form-select mb-3"
                                            onchange="renderUnscheduledList()">
                                            <option value="all">All Postal Codes</option>
                                        </select>
                                    </div>
                                </div>
                                <!-- Search Box -->
                                <div class="ms-auto">
                                    <div class="input-group" style="width: 250px;">
                                        <input type="text" id="searchFilter" class="form-control"
                                            placeholder="Search..." onkeyup="renderUnscheduledList()">
                                        <span class="input-group-text">
                                            <i class="fas fa-search"></i>
                                        </span>
                                    </div>
                                </div>
                            </div>
                            <div id="activeFiltersContainer" class="active-filters-container mb-3">
                                <!-- Filter pills will be dynamically added here -->
                            </div>
                            <div id="batchActionContainer" class="mt-3 p-2 d-none">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div class="d-flex align-items-center gap-3">
                                        <strong id="selectionCounter" style="font-size: 12px;">Selected 1</strong>
                                        <button type="button" id="selectAllBtn"
                                            class="btn btn-outline-secondary btn-sm">
                                            Select All</button>
                                    </div>
                                    <div class="d-flex align-items-center gap-2">
                                        <div class="btn-group">
                                            <button type="button" id="smartBatchButton"
                                                class="btn btn-success btn-sm">
                                                Change to...</button>
                                            <button type="button"
                                                class="btn btn-success btn-sm dropdown-toggle dropdown-toggle-split"
                                                data-bs-toggle="dropdown" aria-expanded="false">
                                                <span class="visually-hidden">Toggle Dropdown</span>
                                            </button>
                                            <ul class="dropdown-menu" id="batchStatusDropdownMenu">
                                                <!-- Populated by JS -->
                                            </ul>
                                        </div>
                                        <button type="button" id="applyBatchButton"
                                            class="btn btn-primary btn-sm d-none">
                                            Apply</button>
                                    </div>
                                </div>
                            </div>
                            <div id="unscheduledList" class="unscheduled-list"></div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="tab-pane fade" id="resourceView" role="tabpanel">
                <div class="date-view-container">
                    <div class="card calendar-container resource-view">
                        <div class="card-header">
                            <div class="d-flex align-items-center flex-wrap gap-2">
                                <!-- Expand/Collapse Button -->
                                <button type="button" id="expandCalendarBtnResource"
                                    class="btn btn-outline-secondary" data-bs-toggle="tooltip"
                                    title="Expand/Collapse Calendar">
                                    <i class="fas fa-expand"></i>
                                </button>
                                <!-- Skills Filter -->
                                <div>
                                    <select id="dispatchGroupResourceView" class="form-select w-auto"
                                        onchange="renderResourceView($('#dayDatePicker').val())">
                                        <option value="all">All resource group</option>
                                    </select>
                                </div>
                                <!-- Individual Resource Filter -->
                                <div>
                                    <select id="individualResourceFilterResourceView" class="form-select w-auto"
                                        onchange="renderResourceView($('#dayDatePicker').val())">
                                        <option value="all">All resources</option>
                                    </select>
                                </div>
                                <!-- Service Type filter -->
                                <div>
                                    <select name="ServiceTypeFilter_ResourceView"
                                        id="ServiceTypeFilter_ResourceView" class="form-select" runat="server"
                                        onchange="renderResourceView($('#dayDatePicker').val())">
                                    </select>
                                </div>
                                <div>
                                    <select name="StatusTypeFilter_ResourceView" id="StatusTypeFilter_ResourceView"
                                        class="form-select" runat="server"
                                        onchange="renderResourceView($('#dayDatePicker').val())">
                                        <option value="all">Select a Status</option>
                                    </select>
                                </div>
                                <div>
                                    <select name="TicketStatusFilter_ResourceView"
                                        id="TicketStatusFilter_ResourceView" class="form-select" runat="server"
                                        onchange="renderResourceView($('#dayDatePicker').val())">
                                        <option value="all">Select a Ticket Status</option>
                                    </select>
                                </div>
                                <!-- Toggle Unscheduled Button -->
                                <button id="toggleUnscheduledBtnResource" class="btn btn-sm">
                                    <i
                                        class="fas fa-chevron-right"></i>
                                </button>
                            </div>
                            <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                                <div class="appt-type-indicators">
                                </div>
                                <div class="color-toggle-container d-flex align-items-center gap-2">
                                    <label class="mb-0 small">Color by:</label>
                                    <div class="btn-group" role="group" id="colorToggleGroupResource">
                                        <input type="radio" class="btn-check" name="colorToggleResource"
                                            id="colorByTypeResource" value="type" checked>
                                        <label class="btn btn-sm btn-outline-primary"
                                            for="colorByTypeResource">
                                            Appointment Type</label>

                                        <input type="radio" class="btn-check" name="colorToggleResource"
                                            id="colorByStatusResource" value="status">
                                        <label class="btn btn-sm btn-outline-primary"
                                            for="colorByStatusResource">
                                            Status</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="card-body">
                            <div class="datepicker">
                                <div class="date-nav" id="resourceNav"></div>
                            </div>
                            <div id="resourceViewContainer">
                                <div id="resourceLoading" class="loading-overlay" style="display: none;">
                                    <div class="spinner-border text-primary" role="status">
                                        <span class="visually-hidden">Loading...</span>
                                    </div>
                                </div>

                            </div>

                        </div>
                    </div>
                    <div class="card unscheduled-panel">
                        <div class="card-header">
                            <div class="d-flex justify-content-between align-items-center">
                                <h3 class="card-title mb-0">Appointment List</h3>
                                <%-- <button type="button" id="sortUnscheduledBtnResource"
                                        class="btn btn-sm btn-outline-secondary" onclick="performSort('resource')"
                                        title="Sort by date">
                                        <i class="fas fa-sort-amount-up"></i>
                                        </button>--%>
                            </div>
                        </div>
                        <div class="card-body">
                            <!--Dropdowns Under One Filter-->
                            <div class="d-flex flex-wrap align-items-center gap-3 mb-3">
                                <!-- Filter Dropdown -->
                                <div class="dropdown">
                                    <button class="btn filter-btn dropdown-toggle d-flex align-items-center gap-2"
                                        type="button" id="unscheduledFilterBtnResource" data-bs-toggle="dropdown"
                                        aria-expanded="false">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
                                            xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                                            <path
                                                d="M21 6H19M21 12H16M21 18H16M7 20V13.5612C7 13.3532 7 13.2492 6.97958 13.1497C6.96147 13.0615 6.93151 12.9761 6.89052 12.8958C6.84431 12.8054 6.77934 12.7242 6.64939 12.5617L3.35061 8.43826C3.22066 8.27583 3.15569 8.19461 3.10948 8.10417C3.06849 8.02393 3.03853 7.93852 3.02042 7.85026C3 7.75078 3 7.64677 3 7.43875V5.6C3 5.03995 3 4.75992 3.10899 4.54601C3.20487 4.35785 3.35785 4.20487 3.54601 4.10899C3.75992 4 4.03995 4 4.6 4H13.4C13.9601 4 14.2401 4 14.454 4.10899C14.6422 4.20487 14.7951 4.35785 14.891 4.54601C15 4.75992 15 5.03995 15 5.6V7.43875C15 7.64677 15 7.75078 14.9796 7.85026C14.9615 7.93852 14.9315 8.02393 14.8905 8.10417C14.8443 8.19461 14.7793 8.27583 14.6494 8.43826L11.3506 12.5617C11.2207 12.7242 11.1557 12.8054 11.1095 12.8958C11.0685 12.9761 11.0385 13.0615 11.0204 13.1497C11 13.2492 11 13.3532 11 13.5612V17L7 20Z"
                                                stroke="currentColor" stroke-width="1.5" stroke-linecap="round"
                                                stroke-linejoin="round">
                                            </path>
                                        </svg>
                                        Filter

                                    </button>
                                    <div class="dropdown-menu p-3" aria-labelledby="unscheduledFilterBtnResource"
                                        style="max-width: 215px;">
                                        <select id="ResourceTypeFilter_Resource" class="form-select mb-3"
                                            onchange="renderUnscheduledList('resource')">
                                            <option value="all">All Resource Status</option>

                                            <option value="unassigned">Unassigned</option>
                                            <option value="assigned">Assigned</option>
                                        </select>
                                        <select runat="server" id="StatusTypeFilter_Resource"
                                            class="form-select mb-3" onchange="renderUnscheduledList('resource')">
                                            <option value="all">Select a Status</option>
                                        </select>
                                        <select runat="server" id="ServiceTypeFilter_Resource"
                                            class="form-select mb-3" onchange="renderUnscheduledList('resource')">
                                            <option value="all">Select a Service</option>
                                            <option value="IT Support">IT Support</option>
                                            <option value="1 Hour">1 Hour</option>
                                            <option value="2 Hour">2 Hour</option>
                                        </select>
                                        <select id="CountryFilterResource" class="form-select mb-3"
                                            onchange="renderUnscheduledList('resource')">
                                            <option value="all">All Countries</option>
                                            <option value="Canada">Canada</option>
                                            <option value="USA">USA</option>
                                        </select>

                                        <select id="ProvinceFilterResource" class="form-select mb-3"
                                            onchange="renderUnscheduledList('resource')">
                                            <option value="all">All Provinces/Territories</option>
                                        </select>
                                        <select id="PostalCodeFilterResource" class="form-select mb-3"
                                            onchange="renderUnscheduledList('resource')">
                                            <option value="all">All Postal Codes</option>
                                        </select>
                                    </div>
                                </div>
                                <!-- Search Box -->
                                <div class="ms-auto">
                                    <div class="input-group" style="width: 250px;">
                                        <input type="text" id="searchFilterResource" class="form-control"
                                            placeholder="Search..." onkeyup="renderUnscheduledList('resource')">
                                        <span class="input-group-text">
                                            <i class="fas fa-search"></i>
                                        </span>
                                    </div>
                                </div>
                            </div>
                            <div id="activeFiltersContainerResource" class="active-filters-container mb-3">
                                <!-- Filter pills will be dynamically added here -->
                            </div>
                            <div id="batchActionContainerResource" class="mt-3 p-2 d-none">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div class="d-flex align-items-center gap-3">
                                        <strong id="selectionCounterResource">Selected 0</strong>
                                        <button type="button" id="selectAllBtnResource"
                                            class="btn btn-outline-secondary btn-sm">
                                            Select All</button>
                                    </div>
                                    <div class="d-flex align-items-center gap-2">
                                        <div class="btn-group">
                                            <button type="button" id="smartBatchButtonResource"
                                                class="btn btn-success btn-sm">
                                                Change to...</button>
                                            <button type="button"
                                                class="btn btn-success btn-sm dropdown-toggle dropdown-toggle-split"
                                                data-bs-toggle="dropdown" aria-expanded="false">
                                                <span class="visually-hidden">Toggle Dropdown</span>
                                            </button>
                                            <ul class="dropdown-menu" id="batchStatusDropdownMenuResource">
                                                <!-- Populated by JS -->
                                            </ul>
                                        </div>
                                        <button type="button" id="applyBatchButtonResource"
                                            class="btn btn-primary btn-sm d-none">
                                            Apply</button>
                                    </div>
                                </div>
                            </div>
                            <div id="unscheduledListResource" class="unscheduled-list"></div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="tab-pane fade" id="listView" role="tabpanel">
                <div class="card calendar-container">
                    <div class="card-header">
                        <div class="d-flex flex-wrap gap-2 align-items-end">
                            <div>
                                <input placeholder="Search anything" type="text" id="search_term"
                                    class="form-control w-200px">
                            </div>
                            <div>
                                <select id="dispatchGroupListView" class="form-select w-auto">
                                    <option value="all">All resource group</option>
                                </select>
                            </div>
                            <div>
                                <select id="individualResourceFilterListView" class="form-select w-auto">
                                    <option value="all">All individual resources</option>
                                </select>
                            </div>
                            <div>
                                <select name="ServiceTypeFilter_ListView" id="ServiceTypeFilter_ListView"
                                    class="form-select" runat="server">
                                    <option value="all">Select Appointment Type</option>
                                </select>
                            </div>
                            <div>
                                <select name="StatusTypeFilter_ListView" id="StatusTypeFilter_ListView"
                                    class="form-select" runat="server">
                                    <option value="all">Select a Status</option>
                                </select>
                            </div>
                            <div>
                                <select name="TicketStatusFilter_ListView" id="TicketStatusFilter_ListView"
                                    class="form-select" runat="server">
                                    <option value="all">Select a Ticket Status</option>
                                </select>
                            </div>

                            <div>
                                <button type="button" class="btn btn-secondary ms-2"
                                    id="clearFilterButton">
                                    Clear</button>
                            </div>
                        </div>
                    </div>
                    <div class="card-body">
                        <div class="list-view-table-container">
                            <div id="listViewLoading" class="loading-overlay" style="display: none;">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Loading...</span>
                                </div>
                            </div>
                            <table class="table list-view-table">
                                <thead>
                                    <tr>
                                        <th>View</th>
                                        <th data-key="CustomerName" class="sortable">Customer</th>
                                        <th data-key="BusinessName" class="sortable">Business Name</th>
                                        <th data-key="Address1" class="sortable">Address</th>
                                        <th data-key="RequestDate" class="sortable">Request Date</th>
                                        <th data-key="TimeSlot" class="sortable">Time Slot</th>
                                        <th data-key="ServiceType" class="sortable">Service Type</th>
                                        <th data-key="Email" class="sortable">Email</th>
                                        <th data-key="Mobile" class="sortable">Mobile</th>
                                        <th data-key="Phone" class="sortable">Phone</th>
                                        <th data-key="AppoinmentStatus" class="sortable">Appointment Status</th>
                                        <th data-key="ResourceName" class="sortable">Resource</th>
                                        <th data-key="TicketStatus" class="sortable">Ticket Status</th>
                                    </tr>
                                </thead>
                                <tbody id="listTableBody">
                                    <!-- Populated by renderListView -->
                                </tbody>
                            </table>
                        </div>

                        <!-- Pagination Controls -->
                        <div class="pagination-controls mt-3 d-flex justify-content-between align-items-center">
                            <div class="d-flex align-items-center gap-3">
                                <div>
                                    <span>Rows per page: </span>
                                    <select id="listViewPageSize" onchange="changeListViewPageSize()"
                                        class="form-select d-inline-block" style="width: auto;">
                                        <option value="5" selected>5</option>
                                        <option value="10">10</option>
                                        <option value="25">25</option>
                                        <option value="50">50</option>
                                    </select>
                                </div>
                            </div>
                            <div class="d-flex align-items-center gap-3">
                                <span class="text-muted" id="listViewPageInfo">Loading...</span>
                                <nav aria-label="List view pagination">
                                    <ul class="pagination mb-0">
                                        <li class="page-item" id="listViewPrevPage">
                                            <a class="page-link" href="#"
                                                onclick="goToListViewPage(listViewCurrentPage - 1); return false;">Previous</a>
                                        </li>
                                        <li class="page-item" id="listViewNextPage">
                                            <a class="page-link" href="#"
                                                onclick="goToListViewPage(listViewCurrentPage + 1); return false;">Next</a>
                                        </li>
                                    </ul>
                                </nav>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!--  Map View section -->
            <div class="tab-pane fade" id="mapView" role="tabpanel">
                <div class="card calendar-container map-view-container">
                    <div class="card-header">
                        <div class="d-flex flex-wrap gap-2 align-items-center justify-content-between">
                            <div class="d-flex flex-wrap gap-2 align-items-center">
                                <div class="workorder-filters-row d-flex flex-wrap gap-2 align-items-end">

                                    <!-- Resource Group Filter for Map View -->
                                    <div class="flex-grow-1">
                                        <asp:DropDownList runat="server" ID="dispatchGroupMapView"
                                            CssClass="form-select w-100">
                                        </asp:DropDownList>
                                    </div>

                                    <!-- Individual Resource Filter for Map View -->
                                    <div class="flex-grow-1">
                                        <asp:DropDownList runat="server" ID="individualResourceFilterMapView"
                                            CssClass="form-select w-100">
                                        </asp:DropDownList>
                                    </div>
                                    <!-- Service Type Filter -->
                                    <div class="flex-grow-1">
                                        <asp:DropDownList runat="server" ID="ServiceTypeFilter_MapView"
                                            CssClass="form-select w-100">
                                        </asp:DropDownList>
                                    </div>
                                    <!-- Status Filter -->
                                    <div class="flex-grow-1">
                                        <asp:DropDownList runat="server" ID="StatusTypeFilter_MapView"
                                            CssClass="form-select w-100">
                                        </asp:DropDownList>
                                    </div>
                                    <!-- Ticket Status Filter -->
                                    <div class="flex-grow-1">
                                        <asp:DropDownList runat="server" ID="TicketStatusFilter_MapView"
                                            CssClass="form-select w-100">
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <button type="button" id="mapReloadBtn" class="btn btn-outline-secondary ms-2">
                                    <i
                                        class="fas fa-rotate-right"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class="card-body p-0">
                        <div id="mapViewContainer" style="height: 600px; width: 100%;"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- FA-ID Sent Modal (aria-hidden set by JS when shown/hidden to avoid focus warning) -->
    <div class="modal fade" id="faIdSentModal" tabindex="-1" aria-labelledby="faIdSentModalLabel" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="faIdSentModalLabel">Send FA-ID to Field Agent</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <p class="mb-1">Select one or more Field Agents using the checkboxes, then click <strong>Send</strong> to send the appointment details (SMS and email) to them.</p>
                    <p class="small text-muted mb-2">Message content uses the template from <strong>Settings > Automated Message Customization > Field Agent ID</strong>.</p>
                    <div id="faProfileList" class="list-group">
                        <style type="text/css">
                            /* Force checkboxes visible: avoid Bootstrap form-check-input which can hide in tables */
                            #faIdSentModal input[type="checkbox"].fa-id-select-cb {
                                display: inline-block !important;
                                width: 18px !important;
                                height: 18px !important;
                                min-width: 18px !important;
                                min-height: 18px !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                opacity: 1 !important;
                                visibility: visible !important;
                                cursor: pointer !important;
                                flex-shrink: 0;
                                vertical-align: middle;
                            }
                        </style>
                        <table class="table table-hover table-sm" role="grid">
                            <thead>
                                <tr>
                                    <th scope="col" style="width:3.5rem;text-align:center;vertical-align:middle;">
                                        <input type="checkbox" id="faProfileSelectAll" class="fa-id-select-cb" title="Select all" aria-label="Select all">
                                        <br><small class="text-muted">Select</small>
                                    </th>
                                    <th scope="col"></th>
                                    <th scope="col">Name</th>
                                    <th scope="col">Phone</th>
                                    <th scope="col">Email</th>
                                    <th scope="col">Resource</th>
                                    <th scope="col">Custom Content</th>
                                </tr>
                            </thead>
                            <tbody id="faProfileTableBody">
                                <tr><td colspan="7" class="text-center text-muted py-3">Loading Field Agents...</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" id="sendFaIdButton">Send</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Modals -->
    <div class="modal fade" id="newModal" tabindex="-1" aria-labelledby="newModalLabel">
        <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="newModalLabel">Create Appointment</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <form id="newForm" onsubmit="createAppointment(event)" novalidate>
                    <div class="modal-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label class="form-label">Customer Name</label>
                                <input type="text" name="customerName" class="form-control">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Service Type</label>
                                <select name="serviceTypeNew" class="form-select">
                                    <option value="IT Support">IT Support</option>
                                    <option value="1 Hour">1 Hour</option>
                                    <option value="2 Hour">2 Hour</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Date</label>
                                <input type="date" name="date" class="form-control">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Resource</label>
                                <select name="resource" class="form-select">
                                    <option value="Unassigned">Unassigned</option>
                                    <option value="Jim">Jim</option>
                                    <option value="Bob">Bob</option>
                                    <option value="Team1">Team 1</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Time Slot</label>
                                <select name="timeSlot" class="form-select">
                                    <option value="morning">Morning</option>
                                    <option value="afternoon">Afternoon</option>
                                    <option value="emergency">Emergency</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Duration (hours)</label>
                                <select name="duration" class="form-select">
                                    <option value="1">1 Hour Package</option>
                                    <option value="2">2 Hour Package</option>
                                    <option value="3">3 Hour Package</option>
                                    <option value="4">4 Hour Package</option>
                                    <option value="5">5 Hour Package</option>
                                    <option value="6">6 Hour Package</option>
                                    <option value="7">7 Hour Package</option>
                                    <option value="8">8 Hour Package</option>
                                </select>
                            </div>
                            <div class="col-12">
                                <label class="form-label">Address</label>
                                <input type="text" name="address" class="form-control">
                            </div>
                            <div class="col-12">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <label class="form-label mb-0">Forms</label>
                                    <button type="button" class="btn btn-sm btn-outline-primary"
                                        onclick="openFormsSelectionModal('new')">
                                        <i class="fa fa-plus"></i>Select Forms

                                    </button>
                                </div>
                                <div id="selectedFormsNew" class="selected-forms-container"
                                    style="min-height: 40px; border: 1px solid #dee2e6; border-radius: 0.375rem; padding: 8px;">
                                    <small class="text-muted">Auto-assigned forms will appear here based on service
                                            type</small>
                                </div>
                            </div>
                            <div class="col-12">
                                <label class="form-label">Status</label>
                                <select name="status" class="form-select">
                                    <option value="pending">Pending</option>
                                    <option value="confirmed">Confirmed</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="submit" class="btn btn-primary">Create</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <div class="modal fade" id="editModal" tabindex="-1" aria-labelledby="editModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-fullscreen">
            <div class="modal-content">
                <div class="modal-header flex justify-between items-center bg-white shadow-md rounded-t-lg">
                    <!-- Tab Navigation with CSL Tabs -->
                    <div class="d-flex flex-wrap align-items-center gap-3 pt-3 px-3 bg-gray-50 rounded-lg">
                        <ul class="nav nav-tabs-modal flex gap-3 mb-0" id="editAppointmentTabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <button
                                    class="nav-link-modal active px-4 py-2 text-sm font-semibold text-gray-700 bg-white rounded-md shadow-sm transition-all duration-200 hover:bg-blue-100 hover:text-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                    id="appointment-tab" data-bs-toggle="tab" data-bs-target="#appointment-details"
                                    type="button" role="tab" aria-controls="appointment-details"
                                    aria-selected="true">
                                    Appointment Details <span id="editAppointmentIdDisplay"
                                        class="ms-2 badge bg-light text-dark font-monospace"
                                        style="font-size: 0.8rem; border: 1px solid #dee2e6;"></span>
                                </button>
                            </li>
                            <li class="nav-item" role="presentation">
                                <button
                                    class="nav-link-modal px-4 py-2 text-sm font-semibold text-gray-700 bg-white rounded-md shadow-sm transition-all duration-200 hover:bg-blue-100 hover:text-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                    id="forms-tab" data-bs-toggle="tab" data-bs-target="#forms-section"
                                    type="button" role="tab" aria-controls="forms-section"
                                    aria-selected="false">
                                    Forms</button>
                            </li>
                        </ul>
                        <div class="d-flex flex-wrap gap-2">
                            <button class="btn btn-sm btn-outline-secondary" id="csl-basic-tab" data-bs-toggle="tab"
                                data-bs-target="#csl-basic-section" type="button" role="tab"
                                aria-controls="csl-basic-section" aria-selected="false">
                                Basic Info</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-appointments-tab"
                                data-bs-toggle="tab" data-bs-target="#csl-appointments-section" type="button"
                                role="tab" aria-controls="csl-appointments-section"
                                aria-selected="false">
                                Appointments</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-invoices-tab"
                                data-bs-toggle="tab" data-bs-target="#csl-invoices-section" type="button" role="tab"
                                aria-controls="csl-invoices-section"
                                aria-selected="false">
                                Invoices/Estimates</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-notes-tab" data-bs-toggle="tab"
                                data-bs-target="#csl-notes-section" type="button" role="tab"
                                aria-controls="csl-notes-section" aria-selected="false">
                                Notes</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-equipment-tab"
                                data-bs-toggle="tab" data-bs-target="#csl-equipment-section" type="button"
                                role="tab" aria-controls="csl-equipment-section"
                                aria-selected="false">
                                Equipment</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-pictures-tab"
                                data-bs-toggle="tab" data-bs-target="#csl-pictures-section" type="button" role="tab"
                                aria-controls="csl-pictures-section" aria-selected="false">
                                Pictures</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-files-tab" data-bs-toggle="tab"
                                data-bs-target="#csl-files-section" type="button" role="tab"
                                aria-controls="csl-files-section" aria-selected="false">
                                Files</button>
                            <button class="btn btn-sm btn-outline-secondary" id="csl-agreements-tab"
                                data-bs-toggle="tab" data-bs-target="#csl-agreements-section" type="button"
                                role="tab" aria-controls="csl-agreements-section" aria-selected="false">
                                Maintenance
                                    Agreements</button>
                        </div>
                    </div>

                    <button type="button"
                        class="btn-close text-gray-500 opacity-80 hover:opacity-100 transition-opacity duration-200"
                        data-bs-dismiss="modal" aria-label="Close modal">
                    </button>
                </div>

                <form id="editForm">
                    <div class="modal-body">
                        <input type="hidden" id="AppoinmentId" name="AppoinmentId">
                        <input type="hidden" id="CustomerID" name="CustomerID">
                        <input type="hidden" id="timerequired_Hour" name="timerequired_Hour">
                        <input type="hidden" id="timerequired_Minute" name="timerequired_Minute">

                        <!-- Tab Content -->
                        <div class="tab-content" id="editAppointmentTabsContent">
                            <!-- Appointment Details Tab -->
                            <div class="tab-pane fade show active" id="appointment-details" role="tabpanel"
                                aria-labelledby="appointment-tab">
                                <div class="row g-4">

                                    <!-- Customer / Site Info -->
                                    <div class="col-md-4">
                                        <div class="d-flex justify-content-between align-items-center mb-3">
                                            <h5 class="mb-0">Customer / Site Info</h5>
                                        </div>
                                        <div class="row col-12">
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Customer Name</label>
                                                <input type="text" name="customerName" class="form-control"
                                                    readonly>
                                            </div>
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Email</label>
                                                <div class="input-group">
                                                    <input type="email" name="email" class="form-control" readonly>
                                                    <a id="sendEmail" href="#" class="btn btn-outline-secondary"
                                                        style="display: none;">
                                                        <i class="fas fa-envelope"></i>
                                                    </a>
                                                </div>
                                            </div>
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Phone</label>
                                                <div class="input-group">
                                                    <input type="text" name="phone" class="form-control" readonly>
                                                    <a id="callPhone" href="#" class="btn btn-outline-secondary"
                                                        style="display: none;">
                                                        <i class="fas fa-phone"></i>
                                                    </a>
                                                </div>
                                            </div>

                                            <div class="mb-1 col-6">
                                                <label class="form-label">Mobile</label>
                                                <div class="input-group">
                                                    <input type="text" name="mobile" class="form-control" readonly>
                                                    <a id="callMobile" href="#" class="btn btn-outline-secondary"
                                                        style="display: none;">
                                                        <i class="fas fa-phone"></i>
                                                    </a>
                                                </div>
                                            </div>

                                            <!--  New Address Block -->
                                            <div class="col-12 mb-1">
                                                <label class="form-label">Service Location (Site)</label>
                                                <div id="siteSelectionContainer">
                                                    <!-- The site dropdown will be loaded here by JavaScript -->
                                                </div>
                                            </div>
                                            <!-- New Editable Address Fields -->
                                            <div class="col-md-12 mb-1">
                                                <label for="site_address" class="form-label">Street Address</label>
                                                <input type="text" id="site_address" name="site_address"
                                                    class="form-control" placeholder="e.g., 123 Main St" readonly>
                                            </div>

                                            <div class="col-md-6 mb-1">
                                                <label for="site_city" class="form-label">City</label>
                                                <input type="text" id="site_city" name="site_city"
                                                    class="form-control" placeholder="e.g., Los Angeles" readonly>
                                            </div>



                                            <div class="col-md-6 mb-1">
                                                <label for="site_state" class="form-label">State / Province</label>
                                                <select id="site_state" name="site_state" class="form-select"
                                                    disabled>
                                                    <!-- Options will be loaded by JavaScript based on country -->
                                                </select>
                                            </div>
                                            <div class="col-md-6 mb-1">
                                                <label for="site_country" class="form-label">Country</label>
                                                <select id="site_country" name="site_country" class="form-select"
                                                    disabled>
                                                    <option value="USA">USA</option>
                                                    <option value="Canada">Canada</option>
                                                </select>
                                            </div>
                                            <div class="col-md-6 mb-1">
                                                <label for="site_zip" id="site_zip_label" class="form-label">
                                                    Zip
                                                        Code</label>
                                                <input type="text" id="site_zip" name="site_zip"
                                                    class="form-control" placeholder="e.g., 90210" readonly>
                                            </div>
                                            <!-- END: New Address Block -->


                                        </div>
                                    </div>

                                    <!-- Appointment Info -->
                                    <div class="col-md-4">
                                        <h5 class="mb-3">Appointment Info</h5>
                                        <div class="row col-12">
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Service Type</label>
                                                <select runat="server" id="ServiceTypeFilter_Edit"
                                                    name="serviceTypeEdit" class="form-select"
                                                    onchange="calculateTimeRequired(event)" required>
                                                    <%-- Options dynamically populated --%>
                                                </select>
                                            </div>
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Resource</label>
                                                <select id="resource_list" name="resource" class="form-select">
                                                    <option value="0">Unassigned</option>
                                                </select>
                                            </div>


                                            <div class="mb-1 col-6">
                                                <label class="form-label">Date</label>
                                                <input type="date" name="date" class="form-control" id="dateInput"
                                                    required onchange="updateDate(event)">
                                            </div>
                                            <div class="mb-1 col-6">
                                                <label class="form-label">Time Required</label>
                                                <input type="text" id="duration" name="duration"
                                                    class="form-control" placeholder="e.g., 1 Hr : 30 Min" />
                                            </div>

                                            <div class="mb-3">
                                                <label class="form-label">Time Slot</label>
                                                <select id="time_slot" name="timeSlot" class="form-select" required
                                                    onchange="calculateTimeRequired(event)">
                                                    <option value="morning">Morning</option>
                                                    <option value="afternoon">Afternoon</option>
                                                    <option value="emergency">Emergency</option>
                                                </select>
                                            </div>



                                            <div class="mb-1 col-6">
                                                <label class="form-label">Appointment Start Date</label>
                                                <input type="text" name="txt_StartDate" class="form-control"
                                                    id="txt_StartDate" placeholder="MM/DD/YYYY hh:mm AM/PM">
                                            </div>

                                            <div class="mb-1 col-6">
                                                <label class="form-label">Appointment End Date</label>
                                                <input type="text" name="txt_EndDate" class="form-control"
                                                    id="txt_EndDate" placeholder="MM/DD/YYYY hh:mm AM/PM">
                                                <small id="customer_EndDate" style="display: none;"
                                                    class="text-warning">End date time can’t be smaller than start
                                                        date time.</small>
                                            </div>


                                            <div class="mb-1 col-6">
                                                <label class="form-label">Appointment Status</label>
                                                <select runat="server" id="StatusTypeFilter_Edit" name="status"
                                                    class="form-select" required>
                                                    <option value="all">Select</option>
                                                </select>
                                            </div>

                                            <div class="mb-1 col-6">
                                                <label class="form-label">Ticket Status</label>
                                                <select runat="server" id="TicketStatusFilter_Edit" name="status"
                                                    class="form-select">
                                                    <option value="all">Select</option>
                                                </select>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- Custom Fields & Notes -->
                                    <div class="col-md-4">
                                        <h5 class="mb-3">Custom Fields & Notes</h5>

                                        <div id="customFieldsContainer" class="mb-3">
                                            <%-- Custom fields will be loaded here dynamically --%>
                                        </div>

                                        <div class="mb-3">
                                            <label class="form-label">Any details</label>
                                            <textarea name="note" class="form-control" rows="6"></textarea>
                                        </div>

                                        <!-- Appointment-Specific Links -->
                                        <div class="mb-3">
                                            <h6 class="mb-2">Appointment-Specific Items</h6>
                                            <div id="appointmentSpecificLinks" class="border rounded p-2"
                                                style="min-height: 60px;">
                                                <small class="text-muted">No items attached to this
                                                        appointment</small>
                                            </div>
                                        </div>
                                    </div>

                                </div>
                            </div>

                            <!-- Forms Tab -->
                            <div class="tab-pane fade" id="forms-section" role="tabpanel"
                                aria-labelledby="forms-tab">
                                <div class="d-flex justify-content-between align-items-center mb-3">
                                    <h6>Attached Forms</h6>
                                    <div>
                                        <button type="button" class="btn btn-sm btn-outline-primary"
                                            onclick="openFormsSelectionModal('edit')">
                                            <i class="fa fa-plus"></i>Add Forms

                                        </button>
                                        <button type="button" class="btn btn-sm btn-outline-info"
                                            onclick="openAppointmentFormsModal()">
                                            <i class="fa fa-list"></i>View Forms

                                        </button>
                                    </div>
                                </div>
                                <div id="selectedFormsEdit" class="selected-forms-container"
                                    style="min-height: 60px; border: 1px solid #dee2e6; border-radius: 0.375rem; padding: 8px;">
                                    <small class="text-muted">No forms attached to this appointment</small>
                                </div>
                            </div>

                            <!-- CSL Basic Info Tab -->
                            <div class="tab-pane fade" id="csl-basic-section" role="tabpanel"
                                aria-labelledby="csl-basic-tab">
                                <div id="cslBasicInfoContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Appointments Tab -->
                            <div class="tab-pane fade" id="csl-appointments-section" role="tabpanel"
                                aria-labelledby="csl-appointments-tab">
                                <div id="cslAppointmentsContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Invoices/Estimates Tab -->
                            <div class="tab-pane fade" id="csl-invoices-section" role="tabpanel"
                                aria-labelledby="csl-invoices-tab">
                                <div id="cslInvoicesContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Notes Tab -->
                            <div class="tab-pane fade" id="csl-notes-section" role="tabpanel"
                                aria-labelledby="csl-notes-tab">
                                <div id="cslNotesContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Equipment Tab -->
                            <div class="tab-pane fade" id="csl-equipment-section" role="tabpanel"
                                aria-labelledby="csl-equipment-tab">
                                <div id="cslEquipmentContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Pictures Tab -->
                            <div class="tab-pane fade" id="csl-pictures-section" role="tabpanel"
                                aria-labelledby="csl-pictures-tab">
                                <div id="cslPicturesContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Files Tab -->
                            <div class="tab-pane fade" id="csl-files-section" role="tabpanel"
                                aria-labelledby="csl-files-tab">
                                <div id="cslFilesContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- CSL Maintenance Agreements Tab -->
                            <div class="tab-pane fade" id="csl-agreements-section" role="tabpanel"
                                aria-labelledby="csl-agreements-tab">
                                <div id="cslAgreementsContent" class="p-3">
                                    <div class="text-center p-5">
                                        <div class="spinner-border" role="status">
                                            <span class="visually-hidden">Loading...</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-danger d-none"
                            onclick="deleteAppointment()">
                            Delete</button>
                        <button type="button" class="btn btn-secondary d-none"
                            onclick="unscheduleAppointment()">
                            openEditModalUnschedule</button>
                        <button type="button" class="btn btn-secondary edit_close"
                            data-bs-dismiss="modal">
                            Cancel</button>
                        <button type="button" class="btn btn-primary"
                            onclick="saveAllDataFromModal(event)">
                            Update</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <div class="modal fade" id="confirmModal" tabindex="-1" aria-labelledby="confirmModalLabel">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">

                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <form id="confirmForm" onsubmit="confirmScheduling(event)">
                    <div class="modal-body">
                        <input type="hidden" name="id">
                        <div class="row g-3">
                            <div class="col-12">
                                <label class="form-label">Customer Name</label>
                                <input type="text" name="customerName" class="form-control" readonly>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Date</label>
                                <input type="date" name="date" class="form-control" required>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Phone</label>
                                <input type="text" name="phone" class="form-control" readonly>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Mobile</label>
                                <input type="text" name="mobile" class="form-control" readonly>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Time Slot</label>
                                <select name="timeSlot" class="form-select" required>
                                    <option value="morning">Morning</option>
                                    <option value="afternoon">Afternoon</option>
                                    <option value="emergency">Emergency</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Duration (hours)</label>
                                <select name="duration" class="form-select" required>
                                    <option value="1">1 Hour Package</option>
                                    <option value="2">2 Hour Package</option>
                                    <option value="3">3 Hour Package</option>
                                    <option value="4">4 Hour Package</option>
                                    <option value="5">5 Hour Package</option>
                                    <option value="6">6 Hour Package</option>
                                    <option value="7">7 Hour Package</option>
                                    <option value="8">8 Hour Package</option>
                                </select>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Resource</label>
                                <select name="resource" class="form-select" required>
                                    <option value="Unassigned">Unassigned</option>
                                    <option value="Jim">Jim</option>
                                    <option value="Bob">Bob</option>
                                    <option value="Team1">Team 1</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="submit" class="btn btn-primary">Confirm</button>
                    </div>
                </form>
            </div>
        </div>
    </div>
    <div class="offcanvas offcanvas-end" tabindex="-1" id="cslDetailsDrawer"
        aria-labelledby="cslDetailsDrawerLabel">
        <div class="offcanvas-header">
            <div>
                <h5 class="offcanvas-title" id="cslDetailsDrawerLabel">Customer Service Location</h5>
                <small id="cslSiteName" class="text-muted">Site Name Here</small>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Close"></button>
        </div>

        <div class="offcanvas-body">
            <div id="cslAccordionPlaceholder">

                <div class="text-center p-5">
                    <div class="spinner-border" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            </div>
        </div>

    </div>
    <script>
        document.addEventListener("DOMContentLoaded", function () {
            // Listen to Bootstrap's tab show event
            const viewTabs = document.querySelectorAll('#viewTabs .nav-link');
            viewTabs.forEach(tab => {
                tab.addEventListener('shown.bs.tab', function (event) {
                    const customLaunchBtn = document.querySelector(".custom-launch-btn");
                    if (customLaunchBtn) {
                        // Show/hide ONLY the CEC button based on active tab
                        if (event.target.id === 'map-tab') {
                            customLaunchBtn.style.display = "none";
                        } else {
                            customLaunchBtn.style.display = "flex";
                        }
                    }
                });
            });
        });
    </script>




    <script src="Scripts/appointments.js?v=faid-select" defer></script>
    <script src="Scripts/Views/list-view.js" defer></script>
    <script src="Scripts/Views/map-view.js" defer></script>
    <script src="Scripts/signature-handler.js" defer></script>

    <!-- Forms Selection Modal -->
    <div class="modal fade" id="formsSelectionModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Select Forms for Appointment</h5>
                    <button type="button" class="close" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-6">
                            <h6>Available Forms</h6>
                            <div class="form-check">
                                <input type="checkbox" class="form-check-input" id="autoAssignForms" checked>
                                <label class="form-check-label" for="autoAssignForms">
                                    Auto-assign forms based on service type

                                </label>
                            </div>
                            <hr>
                            <div id="availableFormsList" class="available-forms-list">
                                <!-- Available forms will be loaded here -->
                            </div>
                        </div>
                        <div class="col-md-6">
                            <h6>Selected Forms</h6>
                            <div id="selectedFormsList" class="selected-forms-list">
                                <!-- Selected forms will appear here -->
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="applyFormsSelection()">
                        Apply
                            Selection</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Appointment Forms Management Modal -->
    <div class="modal fade" id="appointmentFormsModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Appointment Forms</h5>
                    <button type="button" class="close" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-4">
                            <h6>Forms List</h6>
                            <div id="appointmentFormsList" class="appointment-forms-list">
                                <!-- Appointment forms will be loaded here -->
                            </div>

                        </div>
                        <div class="col-md-8">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <h3 id="formName" class="mb-0"></h3>

                                <!-- Form Actions -->
                                <div class="form-actions mt-2" id="formActionsContainer" style="display: none;">
                                    <div class="btn-group" role="group">
                                        <button type="button" class="btn btn-sm btn-primary"
                                            onclick="openCustomerResponseModal()"
                                            title="Save forms to this appointment">
                                            <i class="fa fa-eye"></i>Respone

                                        </button>
                                        <button type="button" class="btn btn-sm btn-info"
                                            onclick="sendFormsViaEmail()" title="Send forms to customer email">
                                            <i class="fa fa-envelope"></i>Email

                                        </button>
                                        <button type="button" class="btn btn-sm btn-warning"
                                            onclick="sendFormsViaSMS()" title="Send forms to customer phone">
                                            <i class="fa fa-mobile"></i>SMS

                                        </button>
                                    </div>
                                </div>
                            </div>

                            <div id="loader">
                                <img src="content/GearLoder.gif" alt="Loading..." />
                            </div>

                            <div id="formViewerContainer">
                                <div class="form-viewer-placeholder text-center p-5">
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">

                    <button type="button" class="btn btn-secondary" data-dismiss="modal"
                        onclick="openAppointmentModal()">
                        Close</button>
                    <button type="button" class="btn btn-success d-none" id="saveFormBtn"
                        onclick="saveCurrentForm()">
                        Save Form</button>
                    <button type="button" class="btn btn-primary d-none" id="submitFormBtn"
                        onclick="submitCurrentForm()">
                        Submit Form</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Signature Capture Modal -->
    <div class="modal fade" id="signatureModal" tabindex="-1" role="dialog">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Capture Signature</h5>
                    <button type="button" class="close" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body text-center">
                    <p id="signaturePrompt">Please sign below:</p>
                    <canvas id="signaturePad" width="400" height="200" style="border: 1px solid #ccc;"></canvas>
                    <div class="mt-3">
                        <button type="button" class="btn btn-sm btn-outline-secondary"
                            onclick="clearSignature()">
                            Clear</button>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="saveSignature()">Save Signature</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="customerResponseModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-xs" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Customer Response</h5>

                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-12">
                            <div id="customerResponseContainer">
                                <div class="form-viewer-placeholder text-center p-5">
                                    <i class="fa fa-file-text-o fa-3x text-muted mb-3"></i>
                                    <p class="text-muted">Select a form to view or fill</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">

                    <button type="button" class="btn btn-secondary" data-dismiss="modal"
                        onclick="showAppointmentModalFromResponseClose()">
                        Close</button>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
