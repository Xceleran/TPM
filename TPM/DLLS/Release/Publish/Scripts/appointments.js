
const GlobalDateSync = {
    _currentDate: new Date().toISOString().split('T')[0],
    _viewMode: 'day',
    _isSyncing: false,

    setDate: function (newDate) {
        if (this._isSyncing || !newDate) return;
        this._currentDate = new Date(newDate).toISOString().split('T')[0]
        this.synchronize();
    },

    setViewMode: function (newMode) {
        if (this._isSyncing || !newMode) return;
        this._viewMode = newMode;
        this.synchronize();
    },

    synchronize: function () {
        if (this._isSyncing) return;
        this._isSyncing = true;

        $('#dayDatePicker').val(this._currentDate);
        $('#viewSelect').val(this._viewMode);
        if (ListViewManager && typeof ListViewManager.syncWithGlobalDate === 'function') {
            ListViewManager.syncWithGlobalDate();
        }

        const isCustom = this._viewMode === 'custom';
        $('#dateCustomDateRangeContainer').toggleClass('d-none', !isCustom);
        $('#dayDatePicker').toggleClass('d-none', isCustom);

        try {
            const activeTabId = $('#viewTabs .nav-link.active').attr('id');

            if (activeTabId === 'date-tab' && typeof renderDateView === 'function') {              
                renderDateView(this._currentDate);
            } else if (activeTabId === 'resource-tab' && typeof renderResourceView === 'function') {
                renderResourceView(this._currentDate);
            } else if (activeTabId === 'list-tab' && window.ListViewManager) {
                ListViewManager.render();
            } else if (activeTabId === 'map-tab' && typeof renderMapView === 'function') {
                renderMapView();
            }

            if (typeof renderDateNav === 'function') {
                renderDateNav('dateNav', this._currentDate);
                renderDateNav('resourceNav', this._currentDate);
            }
        } catch (e) {
            console.error("Error during synchronization render:", e);
        }

        this._isSyncing = false;
    }
};

let appointments = [];
let currentView = "date";
let currentDate = new Date();
let batchSelectedAppointments = new Set();
let isBatchModeActive = false;
let currentEditId = null;
let customSortDirection = 'asc';
let pendingBatchStatus = null; // Store the selected status before applying

let isDateSyncing = false;
let unscheduledSortOrder = 'asc';

const statusWorkflow = {
    "Pending": "Confirmed",
    "Confirmed": "Dispatched",
    "Dispatched": "In-Route",
    "In-Route": "Arrived",
    "Arrived": "Completed",
    "Completed": "Closed"
};

let globalCurrentDate = new Date().toISOString().split('T')[0];

let resourceViewCurrentPage = 1;
let resourceViewPageSize = 5;
let resourceViewTotalPages = 1;
let resourceViewFilteredAppointments = [];
var GlobalTemplateId = 0;
const technicianGroups = {
    "electricians": ["Jim", "Bob"],
    "plumbers": ["Team1"]
};
const timeSlots = {
    morning: { start: "08:00", end: "12:00" },
    afternoon: { start: "12:00", end: "16:00" },
    emergency: { start: "18:00", end: "22:00" }
};

var allTimeSlots = [];
var resources = [];
var timerequired_Hour = 0;
var timerequired_Minute = 0;
let customDateRange = { from: null, to: null };
let resourceCustomDateRange = { from: null, to: null };

const showAlert = (options) => {
    if (typeof Swal !== 'undefined') {
        return Swal.fire(options);
    } else {
        console.warn(' Facade pattern not found: Swal not loaded, falling back to native alert');
        if (options.showCancelButton) {
            return Promise.resolve({ isConfirmed: confirm(options.text) });
        }
        alert(options.text);
        return Promise.resolve();
    }
};

function getStatusOptions() {
    const statusDropdown = $('#MainContent_StatusTypeFilter_Edit');
    if (statusDropdown.length === 0) return [];
    const options = [];
    statusDropdown.find('option').each(function () {
        const text = $(this).text();
        const value = $(this).val();
        if (value && text.trim() !== '' && !text.toLowerCase().includes('select')) {
            options.push({ value, text });
        }
    });
    return options;
}

let isUpdatingBatchUI = false; // Guard flag to prevent infinite recursion

function updateBatchActionUI() {
    if (isUpdatingBatchUI) return; // Prevent infinite recursion
    isUpdatingBatchUI = true;
    
    try {
        const isResourceView = $('#resource-tab').hasClass('active');
        const statusFilterSelector = isResourceView ? '#MainContent_StatusTypeFilter_Resource' : '#MainContent_StatusTypeFilter';
        const batchContainerSelector = isResourceView ? '#batchActionContainerResource' : '#batchActionContainer';
        const smartButtonSelector = isResourceView ? '#smartBatchButtonResource' : '#smartBatchButton';
        const dropdownMenuSelector = isResourceView ? '#batchStatusDropdownMenuResource' : '#batchStatusDropdownMenu';

        const statusFilter = $(statusFilterSelector);
        const batchContainer = $(batchContainerSelector);

        if (batchContainer.length === 0 || statusFilter.length === 0) {
            isUpdatingBatchUI = false;
            return;
        }

        const smartButton = $(smartButtonSelector);
        const dropdownMenu = $(dropdownMenuSelector);

        const selectedStatusText = statusFilter.find('option:selected').text();
        const selectedStatusValue = statusFilter.val();
        const nextStatusInWorkflow = statusWorkflow[selectedStatusText];

        const isBatchPossible = selectedStatusValue !== '' && selectedStatusValue !== 'all';

        if (isBatchPossible) {
            if (nextStatusInWorkflow) {
                smartButton.text(`Change Status to...`);
                smartButton.data('next-status', nextStatusInWorkflow);
            } else {
                smartButton.text('Change Status to...');
                smartButton.data('next-status', '');
            }

            const allStatuses = getStatusOptions();
            dropdownMenu.empty();
            allStatuses.forEach(status => {
                if (status.text !== selectedStatusText) {
                    const li = `<li><a class="dropdown-item" href="#" data-status-text="${status.text}">${status.text}</a></li>`;
                    dropdownMenu.append(li);
                }
            });

            batchContainer.removeClass('d-none');
            
            // Hide Apply button when batch mode becomes active (user needs to select status first)
            const applyButtonSelector = isResourceView ? '#applyBatchButtonResource' : '#applyBatchButton';
            $(applyButtonSelector).addClass('d-none');
            pendingBatchStatus = null;
        } else {
            batchContainer.addClass('d-none');
            batchSelectedAppointments.clear();
            pendingBatchStatus = null;
            const applyButtonSelector = isResourceView ? '#applyBatchButtonResource' : '#applyBatchButton';
            $(applyButtonSelector).addClass('d-none');
            // Don't call updateSelectionCounter here to avoid recursion - it will be called separately if needed
        }
    } finally {
        isUpdatingBatchUI = false;
    }
}

function handleSingleSelect(checkbox) {
    const appointmentId = $(checkbox).data('id').toString();

    if (checkbox.checked) {
        batchSelectedAppointments.add(appointmentId);
    } else {
        batchSelectedAppointments.delete(appointmentId);
    }

    updateBatchSelectionUI();
    updateBatchActionUI();
}

function handleSelectAll(checkbox) {
    const isChecked = $(checkbox).prop('checked');
    const visibleCheckboxes = $(`.tab-pane.active .appointment-card .batch-select-checkbox`);

    visibleCheckboxes.prop('checked', isChecked);

    batchSelectedAppointments.clear();
    if (isChecked) {
        visibleCheckboxes.each(function () {
            batchSelectedAppointments.add($(this).data('id').toString());
        });
    }

    updateBatchSelectionUI(); 
    updateBatchActionUI();
}

function executeSmartBatchUpdate(nextStatusText) {
    const appointmentIds = Array.from(batchSelectedAppointments)
        .map(id => parseInt(id, 10))
        .filter(id => !isNaN(id));

    if (appointmentIds.length === 0) {
        showAlert({ icon: 'warning', title: 'No Appointments Selected', text: 'Please select at least one valid appointment.' });
        return;
    }

    const statusDropdown = $('#MainContent_StatusTypeFilter_Edit');
    let nextStatusId = null;
    statusDropdown.find('option').each(function () {
        if ($(this).text() === nextStatusText) {
            nextStatusId = $(this).val();
        }
    });

    if (!nextStatusId) {
        showAlert({ icon: 'error', title: 'Workflow Error', text: `Could not find a valid ID for status: "${nextStatusText}".` });
        return;
    }

    const companyId = (appointments.length > 0) ? appointments[0].CompanyID : null;
    if (!companyId) {
        showAlert({ icon: 'error', title: 'Configuration Error', text: 'Cannot determine a valid Company ID.' });
        return;
    }

    showAlert({
        title: 'Confirm Batch Update',
        text: `This will change ${appointmentIds.length} appointment(s) to "${nextStatusText}". Are you sure?`,
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: 'Yes, update them!'
    }).then((result) => {
        if (result.isConfirmed) {
            const payloadWrapper = { payload: { appointmentIds: appointmentIds, newStatusId: parseInt(nextStatusId, 10), companyId: companyId } };
            $.ajax({
                type: "POST",
                url: "Appointments.aspx/BatchUpdateAppointmentStatus",
                data: JSON.stringify(payloadWrapper),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d === true) {
                        showAlert({ icon: 'success', title: 'Success!', text: `${appointmentIds.length} appointment(s) have been updated to "${nextStatusText}".`, timer: 2000, showConfirmButton: false });
                        // Clear selections and reset UI
                        batchSelectedAppointments.clear();
                        pendingBatchStatus = null;
                        const isResourceView = $('#resource-tab').hasClass('active');
                        const applyButtonSelector = isResourceView ? '#applyBatchButtonResource' : '#applyBatchButton';
                        $(applyButtonSelector).addClass('d-none');
                        updateBatchSelectionUI();
                        // Refresh the appointments list
                        getAppoinments("", "", "", globalCurrentDate, () => {
                            const activeView = $('#resource-tab').hasClass('active') ? 'resource' : 'date';
                            renderUnscheduledList(activeView);
                            if (activeView === 'date') {
                                renderDateView(globalCurrentDate);
                            } else {
                                renderResourceView(globalCurrentDate);
                            }
                        });
                    } else {
                        showAlert({ icon: 'error', title: 'Update Failed', text: 'The server rejected the request.' });
                    }
                },
                error: function (xhr) {
                    showAlert({ icon: 'error', title: 'AJAX Error', text: 'A server error occurred.' });
                }
            });
        }
    });
}


$(document).ready(function () {
    // Add event listener for view selectors
    $('#viewSelect').on('change', function () {
        GlobalDateSync.setViewMode($(this).val());
    });

    // Add event listener for date pickers
    $('#dayDatePicker').on('change', function () {
        GlobalDateSync.setDate($(this).val());
    });

    // Reset CSL handlers when modal is closed
    $('#editModal').on('hidden.bs.modal', function () {
        cslHandlersInitialized = false;
        // Clear CSL content divs
        const contentDivs = ['cslBasicInfoContent', 'cslAppointmentsContent', 'cslInvoicesContent', 'cslNotesContent', 'cslEquipmentContent', 'cslPicturesContent', 'cslFilesContent', 'cslAgreementsContent'];
        contentDivs.forEach(divId => {
            const $div = $('#' + divId);
            if ($div.length > 0) {
                $div.html('<div class="text-center p-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');
            }
        });
    });
    
    $('#MainContent_StatusTypeFilter, #MainContent_StatusTypeFilter_Resource').on('change', function () {
        batchSelectedAppointments.clear();
        updateBatchActionUI();
        renderUnscheduledList(isResourceView ? 'resource' : 'date');
    });
    $(document).on('click', '#selectAllBtn, #selectAllBtnResource', function (e) {
        e.preventDefault();
        e.stopPropagation();
        handleSelectAllClick(this);
    });

    $(document).on('change', '.appointment-select-checkbox', function (e) {
        e.stopPropagation();
        handleAppointmentSelection(e);
    });
    
    // Also handle click for better compatibility
    $(document).on('click', '.appointment-select-checkbox', function (e) {
        // Prevent double-firing - let change event handle it
        if (e.target.type === 'checkbox') {
            // The change event will handle the state update
        }
    });
    // Only attach to main view tabs, not modal tabs
    $('#viewTabs button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        const targetTab = $(e.target).attr('href'); 

        setTimeout(() => {
            renderDateNav("dateNav", globalCurrentDate);
            renderDateNav("resourceNav", globalCurrentDate);
            updateBatchActionUI();
        }, 50);
    });
    $(document).on('click', '#selectAllCheckbox', function () { handleSelectAll(this); });
    $(document).on('click', '.appointment-card .batch-select-checkbox', function (e) {
        e.stopPropagation();
        handleSingleSelect(this);
    });
    // smartBatchButton should only open dropdown - don't execute directly
    // Status selection is handled by dropdown item click, Apply button executes
    const cslDrawerElement = document.getElementById('cslDetailsDrawer');
    const cslDrawer = new bootstrap.Offcanvas(cslDrawerElement);

   
    $(document).on('click', '#viewCslDetailsBtn', function () {
        const siteSelector = document.getElementById('siteSelector');
        let siteName = "Primary Location"; // Default text
        if (siteSelector && siteSelector.value !== "0") {
            siteName = siteSelector.options[siteSelector.selectedIndex].text;
        }
        $('#cslSiteName').text(siteName);

        cslDrawer.show();
    });
    // Duplicate handler removed - handled above

    $(document).on('click', '.remove-filter-btn', function () {
        const filterId = $(this).closest('.filter-pill').data('filter-id');
        const $filterElement = $(filterId);

        if ($filterElement.is('select')) {
            $filterElement.val('all');
        } else {
            $filterElement.val('');
        }

        $filterElement.trigger('change');
    });

    $(document).on('click', '#clearAllFiltersBtn', function () {
        const isResourceView = $('#resource-tab').hasClass('active');
        const filterSelectors = [
            isResourceView ? '#ResourceTypeFilter_Resource' : '#ResourceTypeFilter_2',
            isResourceView ? '#MainContent_StatusTypeFilter_Resource' : '#MainContent_StatusTypeFilter',
            isResourceView ? '#MainContent_ServiceTypeFilter_Resource' : '#MainContent_ServiceTypeFilter_2',
            isResourceView ? '#CountryFilterResource' : '#CountryFilter',
            isResourceView ? '#ProvinceFilterResource' : '#ProvinceFilter',
            isResourceView ? '#PostalCodeFilterResource' : '#PostalCodeFilter',
            isResourceView ? '#searchFilterResource' : '#searchFilter'
        ];

        filterSelectors.forEach(selector => {
            const $el = $(selector);
            if ($el.is('select')) {
                $el.val('all');
            } else {
                $el.val('');
            }
        });

        $(filterSelectors[0]).trigger('change');
    });

    $('#ResourceTypeFilter_2, #MainContent_StatusTypeFilter, #MainContent_ServiceTypeFilter_2, #CountryFilter, #ProvinceFilter, #PostalCodeFilter, #searchFilter').on('change', () => renderUnscheduledList('date'));
    $('#ResourceTypeFilter_Resource, #MainContent_StatusTypeFilter_Resource, #MainContent_ServiceTypeFilter_Resource, #CountryFilterResource, #ProvinceFilterResource, #PostalCodeFilterResource, #searchFilterResource').on('change', () => renderUnscheduledList('resource'));
});
function parseDuration(durationString) {
    if (!durationString) return 60; // Default to 60 minutes if empty
    let totalMinutes = 0;

    const normalized = durationString.replace(/\s*:\s*/g, ' ').trim();
    const hourMatch = normalized.match(/(\d+)\s*Hr/i);
    const minuteMatch = normalized.match(/(\d+)\s*Min/i);
    if (hourMatch) totalMinutes += parseInt(hourMatch[1], 10) * 60;
    if (minuteMatch) totalMinutes += parseInt(minuteMatch[1], 10);
    return totalMinutes || 60; // Default to 60 minutes if parsing fails
}


function parseTimeToMinutes(timeStr) {
    if (!timeStr || typeof timeStr !== 'string') return 0;

    const lowerTimeStr = timeStr.toLowerCase();
    if (timeSlots[lowerTimeStr]) {
        timeStr = timeSlots[lowerTimeStr].start;
    } else {

        const matchingSlot = allTimeSlots.find(slot =>
            slot.TimeBlock.toLowerCase() === lowerTimeStr ||
            slot.TimeBlockSchedule.toLowerCase() === lowerTimeStr
        );
        if (matchingSlot) {
            timeStr = matchingSlot.TimeBlockSchedule.split('-')[0].trim();
        }
    }


    let time = timeStr.toUpperCase();
    let hours = 0;
    let minutes = 0;


    const match = time.match(/(\d{1,2}):(\d{2})/);
    if (match) {
        hours = parseInt(match[1], 10);
        minutes = parseInt(match[2], 10);
    } else {

        const singleHourMatch = time.match(/(\d{1,2})/);
        if (singleHourMatch) {
            hours = parseInt(singleHourMatch[1], 10);
        }
    }

    if (time.includes('PM') && hours < 12) {
        hours += 12;
    }

    if (time.includes('AM') && hours === 12) {
        hours = 0;
    }

    if (isNaN(hours) || isNaN(minutes)) {
        console.warn(`Could not parse time: ${timeStr}`);
        return 0;
    }

    return hours * 60 + minutes;
}

function showMainLoader() {
    const activeTabId = $('#viewTabs .nav-link.active').attr('id');
    let loaderSelector = '';

    if (activeTabId === 'date-tab') {
        loaderSelector = '#dateView .loading-overlay';
    } else if (activeTab - id === 'resource-tab') {
        loaderSelector = '#resourceLoading';
    } else if (activeTabId === 'list-tab') {
        loaderSelector = '#listViewLoading';
    }

    if (loaderSelector) {
        $(loaderSelector).show();
    }
}

function hideMainLoader() {
    $('#dateView .loading-overlay').hide();
    $('#resourceLoading').hide();
    $('#listViewLoading').hide();
}

function getAppoinments(searchValue, fromDate, toDate, today, callback, customerId = null, siteId = null) {
    searchValue = searchValue || "";
    fromDate = fromDate || "";
    toDate = toDate || "";
    today = today || "";
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/LoadAppoinments",
        data: JSON.stringify({
            searchValue: searchValue,
            fromDate: fromDate,
            toDate: toDate,
            today: today,
            customerId: customerId, // Pass the customerId
            siteId: siteId          // Pass the siteId
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            appointments = response.d;
            console.log('Appointments loaded:', appointments);
            populatePostalCodeDropdowns();
            callback(appointments);
        },
        error: function (xhr, status, error) {
            console.error('Error loading appointments:', error);
            hideMainLoader();
            callback([]);
        }
    });
}

function populatePostalCodeDropdowns(selectedProvince = 'all') {
    const relevantAppointments = (selectedProvince === 'all')
        ? appointments
        : appointments.filter(app => app.State === selectedProvince);

    const postalCodes = [...new Set(relevantAppointments
        .map(app => app.PostalCode)
        .filter(code => code && code.trim() !== ''))].sort();

    const $postalCodeFilter = $('#PostalCodeFilter');
    const $postalCodeFilterResource = $('#PostalCodeFilterResource');

    const currentVal = $postalCodeFilter.val();

    $postalCodeFilter.empty().append('<option value="all">All Postal Codes</option>');
    $postalCodeFilterResource.empty().append('<option value="all">All Postal Codes</option>');

    postalCodes.forEach(code => {
        const option = `<option value="${code}">${code}</option>`;
        $postalCodeFilter.append(option);
        $postalCodeFilterResource.append(option);
    });

    if (postalCodes.includes(currentVal)) {
        $postalCodeFilter.val(currentVal);
        $postalCodeFilterResource.val(currentVal);
    }
}

function saveAppointments() {
    try {
        localStorage.setItem('appointments', JSON.stringify(appointments));
    } catch (e) {
        console.error("Error saving to localStorage:", e);
    }
}

function hasConflict(appointment, newTimeSlot, newResource, newDate, excludeId = null) {
    if (!newTimeSlot || !newResource || !newDate) return false;

    return appointments.some(a =>
        a.AppoinmentId !== excludeId &&
        a.ResourceName === newResource &&
        a.RequestDate === newDate &&
        a.TimeSlot === newTimeSlot
    );
}

function getEventTimeSlotClass(appointment) {
    return 'service-type-custom'; // Generic class for all service types
}

function getContrastColor(hex) {
    if (!hex) return "#000"; // default black if no color

    hex = hex.replace("#", "");

    if (hex.length === 3) {
        hex = hex.split("").map(c => c + c).join("");
    }

    let r = parseInt(hex.substr(0, 2), 16);
    let g = parseInt(hex.substr(2, 2), 16);
    let b = parseInt(hex.substr(4, 2), 16);

    let luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

    return luminance > 0.5 ? "#000000" : "#FFFFFF";
}

function updateCalendarEventColors() {
    document.querySelectorAll('.calendar-event, .calendar-event-resource').forEach(element => {
        const appointmentId = element.dataset.id;
        const appointment = appointments.find(a => a.AppoinmentId === appointmentId);

        if (appointment && appointment.ServiceColor) {
            let bgColor = appointment.ServiceColor;
            let textColor = getContrastColor(bgColor);

            element.style.backgroundColor = bgColor;
            element.style.color = textColor;
        }
    });
}


function attachAllEventListeners() {
    console.log("Attaching all event listeners now that the app is ready.");




    document.querySelectorAll('#viewTabs button[data-bs-toggle="tab"]').forEach(tab => {
        tab.addEventListener('shown.bs.tab', function (event) {

            currentView = event.target.id.replace('-tab', '');


            syncDatePickers(null, globalCurrentDate);
        });
    });


    $('#viewSelect').on('change', function () {

        syncDatePickers(null, globalCurrentDate);
    });
    
    // Custom date range search handlers
    $('#dateCustomDateSearch').on('click', function() {
        const from = $('#datePickerFrom').val();
        const to = $('#datePickerTo').val();
        if (from && to) {
            customDateRange.from = from;
            customDateRange.to = to;
            renderDateView(from);
        }
    });

    $(document).on('click', '#expandCalendarBtn', function () { toggleCalendarExpansion('dateView'); });
    $(document).on('click', '#expandCalendarBtnResource', function () { toggleCalendarExpansion('resourceView'); });
    $(document).on('click', '#toggleUnscheduledBtn', function () { toggleUnscheduledPanel('dateView'); });
    $(document).on('click', '#toggleUnscheduledBtnResource', function () { toggleUnscheduledPanel('resourceView'); });
    $('#ResourceTypeFilter_2, #MainContent_StatusTypeFilter, #MainContent_ServiceTypeFilter_2, #CountryFilter, #ProvinceFilter, #PostalCodeFilter, #searchFilter').on('change', () => renderUnscheduledList('date'));

    $('#ResourceTypeFilter_Resource, #MainContent_StatusTypeFilter_Resource, #MainContent_ServiceTypeFilter_Resource, #CountryFilterResource, #ProvinceFilterResource, #PostalCodeFilterResource, #searchFilterResource').on('change', () => renderUnscheduledList('resource'));


    // Duplicate handlers removed - already defined above

    $(document).on('click', '#batchStatusDropdownMenu .dropdown-item, #batchStatusDropdownMenuResource .dropdown-item', function (e) {
        e.preventDefault();
        const statusText = $(this).data('status-text');
        if (statusText) {
            // Store the selected status and show Apply button
            pendingBatchStatus = statusText;
            const isResourceView = $('#resource-tab').hasClass('active');
            const applyButtonSelector = isResourceView ? '#applyBatchButtonResource' : '#applyBatchButton';
            $(applyButtonSelector).removeClass('d-none').text(`Apply: ${statusText}`);
            
            // Update the smart button text to show what will be applied
            const smartButtonSelector = isResourceView ? '#smartBatchButtonResource' : '#smartBatchButton';
            $(smartButtonSelector).text(`Change to: ${statusText}`);
        }
    });
    
    // Apply button handlers
    $(document).on('click', '#applyBatchButton, #applyBatchButtonResource', function() {
        if (pendingBatchStatus && batchSelectedAppointments.size > 0) {
            executeSmartBatchUpdate(pendingBatchStatus);
            // Hide Apply button after applying
            $(this).addClass('d-none');
            pendingBatchStatus = null;
        } else {
            showAlert({ 
                icon: 'warning', 
                title: 'Nothing to Apply', 
                text: 'Please select appointments and choose a status first.' 
            });
        }
    });

    $(document).on('click', '#viewCslDetailsBtn', function () {
        const customerId = $('#CustomerID').val();
        const siteId = parseInt($('#siteSelector').val(), 10) || 0;
    
        if (!customerId) {
            showAlert({ icon: 'error', text: 'Cannot load details: Customer ID is missing.' });
            return;
        }
    
        const placeholder = $('#cslAccordionPlaceholder');
        placeholder.html('<div class="text-center p-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');
        cslDrawerInstance.show();
    
    
        $.ajax({
            type: "POST",
            url: "Appointments.aspx/GetCslDrawerData",
            data: JSON.stringify({ customerId: customerId, siteId: siteId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                const data = response.d;
                if (data) {
                    $('#cslSiteName').text(data.SiteInfo.SiteName || 'Details');
    
                    const accordionHtml = populateCslDrawer(data);
                    placeholder.html(accordionHtml);
                } else {
                    placeholder.html('<div class="alert alert-warning">Could not load customer details.</div>');
                }
            },
            error: function () {
                placeholder.html('<div class="alert alert-danger">An error occurred while fetching data.</div>');
            }
        });
    });



}
// Expand/Collapse functionality - FIXED VERSION
function toggleCalendarExpansion(viewType) {
    const calendarContainer = viewType === 'dateView' ?
        $('#dateView .calendar-container') : $('#resourceView .calendar-container');
    const unscheduledPanel = viewType === 'dateView' ?
        $('#dateView .unscheduled-panel') : $('#resourceView .unscheduled-panel');
    const expandBtn = viewType === 'dateView' ?
        $('#expandCalendarBtn') : $('#expandCalendarBtnResource');

    // Toggle expanded state
    calendarContainer.toggleClass('expanded');
    unscheduledPanel.toggleClass('collapsed');

    // Update button icon
    const isExpanded = calendarContainer.hasClass('expanded');
    const icon = expandBtn.find('i');
    icon.toggleClass('fa-expand fa-compress');
    expandBtn.attr('title', isExpanded ? 'Collapse Calendar' : 'Expand Calendar');

    // NO view reload - just adjust layout
    adjustLayoutAfterToggle();
}

function toggleUnscheduledPanel(viewType) {
    const unscheduledPanel = viewType === 'dateView' ?
        $('#dateView .unscheduled-panel') : $('#resourceView .unscheduled-panel');
    const toggleBtn = viewType === 'dateView' ?
        $('#toggleUnscheduledBtn') : $('#toggleUnscheduledBtnResource');

    // Toggle collapsed state
    unscheduledPanel.toggleClass('collapsed');

    // Update button icon
    const isCollapsed = unscheduledPanel.hasClass('collapsed');
    const icon = toggleBtn.find('i');
    icon.toggleClass('fa-chevron-right fa-chevron-left');

    // NO view reload - just adjust layout
    adjustLayoutAfterToggle();
}

// Helper function to adjust layout without reloading views
function adjustLayoutAfterToggle() {
    // Force browser to recalculate layout
    setTimeout(() => {
        $('.calendar-container, .unscheduled-panel').css('display', 'flex');
    }, 10);
}

function loadServiceTypeIndicators() {
   return  $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetServiceTypesWithColors",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const serviceTypes = response.d;
            const container = $(".appt-type-indicators");
            container.empty();

            serviceTypes.forEach(service => {
                const indicatorHtml = `
                    <span class="appt-type-indicator" style="background-color: ${service.CalendarColor}"></span>
                    ${service.ServiceName}
                `;
                container.append(indicatorHtml);
            });
        },
        error: function (xhr, status, error) {
            console.error("Error loading service types:", error);
        }
    });
}

// Helper function to format date consistently as YYYY-MM-DD (for internal use)
function formatDateToISO(date) {
    if (!date) return '';
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d.getTime())) {
        // Try parsing as YYYY-MM-DD string
        if (typeof date === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(date)) {
            return date;
        }
        return '';
    }
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

// Helper function to format date for display in USA format (MM/DD/YYYY) with leading zeros
function formatDateToUSA(date) {
    if (!date) return '';
    let d;
    if (date instanceof Date) {
        d = date;
    } else if (typeof date === 'string') {
        // Try parsing as YYYY-MM-DD string first
        if (/^\d{4}-\d{2}-\d{2}$/.test(date)) {
            const parts = date.split('-');
            // Ensure leading zeros
            const month = parts[1].padStart(2, '0');
            const day = parts[2].padStart(2, '0');
            return `${month}/${day}/${parts[0]}`;
        }
        // Try parsing as date string
        d = new Date(date);
    } else {
        d = new Date(date);
    }
    
    if (isNaN(d.getTime())) {
        return '';
    }
    
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${month}/${day}/${year}`;
}

function renderDateNav(containerId, selectedDate) {
    const container = $(`#${containerId}`);
    const view = $("#viewSelect").val();

    const mainSelectedDate = new Date(selectedDate + 'T00:00:00'); 
    if (isNaN(mainSelectedDate.getTime())) {
        const parts = selectedDate.split('-').map(part => parseInt(part, 10));
        mainSelectedDate = new Date(parts[0], parts[1] - 1, parts[2]);
    }

    const mainSelectedDateStr = selectedDate; 

    let html = `
        <button class="btn btn-primary" onclick="prevPeriod('${containerId}')"><i class="fas fa-chevron-left"></i></button>
    `;

    if (view !== 'month') {
        let daysToShow;
        let startDate;

        if (view === 'custom') {
            const range = customDateRange;
            if (range.from && range.to) {             
                startDate = new Date(range.from + 'T00:00:00');
                const toDate = new Date(range.to + 'T00:00:00');

                daysToShow = Math.ceil((toDate - startDate) / (1000 * 60 * 60 * 24)) + 1;
            } else {
                startDate = mainSelectedDate;
                daysToShow = 1;
            }
        } else if (view === 'week') {
            startDate = new Date(mainSelectedDate);
            daysToShow = 7;
        } else if (view === 'threeDay') {
            startDate = mainSelectedDate;
            daysToShow = 3;
        } else {
            startDate = mainSelectedDate;
            daysToShow = 1;
        }

        html += `<div class="date-boxes">`;
        for (let i = 0; i < daysToShow; i++) {
            const currentDateInLoop = new Date(startDate);
            currentDateInLoop.setDate(startDate.getDate() + i);

            
            const year = currentDateInLoop.getFullYear();
            const month = String(currentDateInLoop.getMonth() + 1).padStart(2, '0');
            const day = String(currentDateInLoop.getDate()).padStart(2, '0');
            const currentDateStr = `${year}-${month}-${day}`;

            const isActive = currentDateStr === mainSelectedDateStr;

            const weekday = currentDateInLoop.toLocaleDateString('en-US', {
                weekday: 'short'
            });

            html += `
                <div class="date-box${isActive ? ' active' : ''}" 
                     data-date="${currentDateStr}" 
                     onclick="selectDate('${currentDateStr}', '${containerId}')">
                    <div class="date-weekday">${weekday}</div>
                    <div class="date-number">${currentDateInLoop.getDate()}</div>
                </div>
            `;
        }
        html += `</div>`;
    }

    html += `
        <button class="btn btn-primary" onclick="nextPeriod('${containerId}')"><i class="fas fa-chevron-right"></i></button>
        <button class="btn btn-primary ms-2" onclick="gotoToday('${containerId}')">Today</button>
    `;

    container.html(html);

    // Only update the date picker if we're rendering dateNav, not resourceNav
    // This prevents triggering change events that could cause infinite recursion
    if (containerId === 'dateNav') {
        const pickerId = "#dayDatePicker";
        // Temporarily remove change handler to prevent recursion
        $(pickerId).off('change').val(mainSelectedDateStr);
        // Re-attach handler immediately
        $(pickerId).on('change', function () {
            console.log(`dayDatePicker change event fired. Value: ${$(this).val()}`); // ADDED LOG
            if (!GlobalDateSync._isSyncing && !isDateSyncing) {
                syncDatePickers(null, $(this).val());
            }
        });
    }
}
function prevPeriod(containerId) {
    const view = $("#viewSelect").val();
    
    // Get current date from the appropriate source based on containerId
    let pickerDate;
    if (containerId === 'resourceNav') {
        // For Resource View, try multiple sources:
        // 1. Active date box in resourceNav (most accurate)
        const activeDateBox = $(`#${containerId} .date-box.active`);
        if (activeDateBox.length > 0) {
            pickerDate = activeDateBox.attr('data-date');
        }
        // 2. Fallback to GlobalDateSync or globalCurrentDate
        if (!pickerDate || !/^\d{4}-\d{2}-\d{2}$/.test(pickerDate)) {
            pickerDate = GlobalDateSync._currentDate || globalCurrentDate || $('#dayDatePicker').val();
        }
    } else {
        // For Date View, use dayDatePicker
        pickerDate = $('#dayDatePicker').val() || GlobalDateSync._currentDate || globalCurrentDate;
    }
    
    // Validate and parse the date
    if (!pickerDate || !/^\d{4}-\d{2}-\d{2}$/.test(pickerDate)) {
        pickerDate = globalCurrentDate || new Date().toISOString().split('T')[0];
    }
    
    // Parse date using local components to avoid timezone issues
    const parts = pickerDate.split('-').map(part => parseInt(part, 10));
    const currentDate = new Date(parts[0], parts[1] - 1, parts[2]);

    if (view === 'month') {
        currentDate.setMonth(currentDate.getMonth() - 1);
    } else {
        const daysToMove = view === 'week' ? 7 : view === 'threeDay' ? 3 : 1;
        currentDate.setDate(currentDate.getDate() - daysToMove);
    }

    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    const day = String(currentDate.getDate()).padStart(2, '0');
    const newDate = `${year}-${month}-${day}`;

    syncDatePickers(null, newDate);
}

function nextPeriod(containerId) {
    const view = $("#viewSelect").val();
    
    // Get current date from the appropriate source based on containerId
    let pickerDate;
    if (containerId === 'resourceNav') {
        // For Resource View, try multiple sources:
        // 1. Active date box in resourceNav (most accurate)
        const activeDateBox = $(`#${containerId} .date-box.active`);
        if (activeDateBox.length > 0) {
            pickerDate = activeDateBox.attr('data-date');
        }
        // 2. Fallback to GlobalDateSync or globalCurrentDate
        if (!pickerDate || !/^\d{4}-\d{2}-\d{2}$/.test(pickerDate)) {
            pickerDate = GlobalDateSync._currentDate || globalCurrentDate || $('#dayDatePicker').val();
        }
    } else {
        // For Date View, use dayDatePicker
        pickerDate = $('#dayDatePicker').val() || GlobalDateSync._currentDate || globalCurrentDate;
    }
    
    // Validate and parse the date
    if (!pickerDate || !/^\d{4}-\d{2}-\d{2}$/.test(pickerDate)) {
        pickerDate = globalCurrentDate || new Date().toISOString().split('T')[0];
    }
    
    // Parse date using local components to avoid timezone issues
    const parts = pickerDate.split('-').map(part => parseInt(part, 10));
    const currentDate = new Date(parts[0], parts[1] - 1, parts[2]);

    if (view === 'month') {
        currentDate.setMonth(currentDate.getMonth() + 1);
    } else {
        const daysToMove = view === 'week' ? 7 : view === 'threeDay' ? 3 : 1;
        currentDate.setDate(currentDate.getDate() + daysToMove);
    }
    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    const day = String(currentDate.getDate()).padStart(2, '0');
    const newDate = `${year}-${month}-${day}`;

    syncDatePickers(null, newDate);
}
function selectDate(dateStr, containerId) {
    syncDatePickers(null, dateStr); 
}

function gotoToday(containerId) {
    const todayStr = new Date().toISOString().split('T')[0];
    syncDatePickers(null, todayStr); 
}
function syncDatePickers(pickerId, newDate) {
    isDateSyncing = true; // Set flag to prevent recursion
    console.log(`syncDatePickers: Called with pickerId=${pickerId}, newDate=${newDate}`);

    if (newDate) {
        console.log(`syncDatePickers: Using newDate parameter: ${newDate}`);
        GlobalDateSync.setDate(newDate);
        localStorage.setItem('lastViewedDate', newDate); // Save the last viewed date
    } else if (localStorage.getItem('lastViewedDate')) {
        const storedDate = localStorage.getItem('lastViewedDate');
        console.log(`syncDatePickers: Using stored localStorage date: ${storedDate}`);
        GlobalDateSync.setDate(storedDate);
    } else {
        const today = new Date().toISOString().split('T')[0];
        console.log(`syncDatePickers: No newDate or stored date, defaulting to today: ${today}`);
        GlobalDateSync.setDate(today); // Default to today
    }
    
    globalCurrentDate = GlobalDateSync._currentDate;
    console.log(`syncDatePickers: globalCurrentDate is now ${globalCurrentDate}`);

    if (pickerId) {
        $(pickerId).val(globalCurrentDate);
        console.log(`syncDatePickers: Set picker ${pickerId} to ${globalCurrentDate}`);
    }
    
    isDateSyncing = false; // Reset flag
}


const calendarDetailsPopup = document.createElement('div');
calendarDetailsPopup.className = 'appointment-details-popup';
document.body.appendChild(calendarDetailsPopup);

const cardDetailsPopup = document.createElement('div');
cardDetailsPopup.className = 'appointment-card-details-popup';
document.body.appendChild(cardDetailsPopup);

const mapDetailsPopup = document.createElement('div');
mapDetailsPopup.className = 'appointment-details-popup map-popup'; // Added 'map-popup' for specific styling
document.body.appendChild(mapDetailsPopup);
function showDetailsPopup(appointment, element, event, popup) {
    if (element.classList.contains('ui-draggable-dragging')) {
        return;
    }


    element.classList.add('expanded');
    popup.innerHTML = `
        <div class="details-title">${appointment.CustomerName || 'N/A'}</div>
        <div class="details-item">
            <span class="details-label">Service Type:</span>
            <span class="details-value">${appointment.ServiceType || 'N/A'}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Date:</span>
            <span class="details-value">${formatToUSDate(appointment.RequestDate)}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Time Slot:</span>
            <span class="details-value">${formatTimeRange(appointment.TimeSlot) || 'N/A'}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Duration:</span>
            <span class="details-value">${appointment.Duration || 'N/A'}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Resource:</span>
            <span class="details-value">${appointment.ResourceName || 'Unassigned'}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Status:</span>
            <span class="details-value">${appointment.AppoinmentStatus || 'N/A'}</span>
        </div>
        <div class="details-item">
            <span class="details-label">Address:</span>
           <span class="details-value">${[appointment.SiteAddress || appointment.Address1, appointment.City, appointment.State, appointment.ZipCode].filter(Boolean).join(', ') || 'N/A'}</span>
        </div>
    `;

    popup.style.display = 'block';
    popup.style.opacity = '0';

    const rect = element.getBoundingClientRect();
    const popupRect = popup.getBoundingClientRect();
    const viewportHeight = window.innerHeight;
    const viewportWidth = window.innerWidth;
    const spaceBelow = viewportHeight - rect.bottom;
    const spaceAbove = rect.top;
    const margin = 10;

    let top, left;

    if (spaceBelow < popupRect.height && spaceAbove > popupRect.height) {
        top = rect.top - popupRect.height - margin;
    } else {
        top = rect.bottom + margin;
    }


    left = rect.right + margin;
    if (left + popupRect.width > viewportWidth) {
        left = rect.left - popupRect.width - margin;
    }

    if (top < margin) {
        top = margin;
    }
    if (left < margin) {
        left = margin;
    }
    if (top + popupRect.height > viewportHeight - margin) {
        top = viewportHeight - popupRect.height - margin;
    }
    popup.style.left = `${left}px`;
    popup.style.top = `${top}px`;
    popup.style.opacity = '1';
    popup.classList.add('show');
}
function syncMapViewFilters() {
    const $sourceServiceTypes = $('#MainContent_ServiceTypeFilter_ListView');
    const $targetServiceType = $('#MainContent_ServiceTypeFilter_MapView');

    const $sourceStatuses = $('#MainContent_StatusTypeFilter_ListView');
    const $targetStatus = $('#MainContent_StatusTypeFilter_MapView');

    const $sourceTicketStatuses = $('#MainContent_TicketStatusFilter_ListView');
    const $targetTicketStatus = $('#MainContent_TicketStatusFilter_MapView');

    if ($sourceServiceTypes.length > 0 && $sourceServiceTypes.find('option').length > 0) {
        $targetServiceType.html($sourceServiceTypes.html());
    } else {
        console.error('Could not find source Service Type dropdown to clone for Map View.');
    }

    if ($sourceStatuses.length > 0 && $sourceStatuses.find('option').length > 0) {
        $targetStatus.html($sourceStatuses.html());
    } else {
        console.error('Could not find source Status dropdown to clone for Map View.');
    }

    if ($sourceTicketStatuses.length > 0 && $sourceTicketStatuses.find('option').length > 0) {
        $targetTicketStatus.html($sourceTicketStatuses.html());
    } else {
        console.error('Could not find source Ticket Status dropdown to clone for Map View.');
    }
}

function hideDetailsPopup(popup) {
    if (popup) {
        popup.classList.remove('show');
        popup.style.display = 'none';
    }
    document.querySelectorAll('.calendar-event.expanded, .calendar-event-resource.expanded, .appointment-card.expanded').forEach(el => {
        el.classList.remove('expanded');
    });
}

function setupHoverEvents() {
    const calendarElements = document.querySelectorAll('.calendar-event, .calendar-event-resource');

    calendarElements.forEach(element => {

        element.addEventListener('mouseenter', function (e) {

            if (this.classList.contains('ui-draggable-dragging')) {
                return;
            }

            const appointmentId = this.dataset.id;
            if (!appointmentId) return;

            const appointment = appointments.find(a => a.AppoinmentId === appointmentId.toString());
            if (appointment) {

                showDetailsPopup(appointment, this, e, calendarDetailsPopup);
            }
        });

        element.addEventListener('mouseleave', function () {
            hideDetailsPopup(calendarDetailsPopup);
        });
    });


}


function renderDateView(date) {
    $('#dateViewLoading').show();
    $("#dayCalendar").html('');
    
    // Ensure date is properly parsed to avoid timezone issues
    let dateStr;
    if (typeof date === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(date)) {
        // Already in YYYY-MM-DD format, use it directly
        dateStr = date;
        // Parse to Date object for calculations, using local time to avoid timezone shift
        const parts = date.split('-').map(part => parseInt(part, 10));
        currentDate = new Date(parts[0], parts[1] - 1, parts[2]);
    } else {
        // Date object or other format, convert properly
        const d = date instanceof Date ? date : new Date(date);
        if (isNaN(d.getTime())) {
            // Fallback to today if invalid
            currentDate = new Date();
            // Use local date components to avoid timezone issues
            const year = currentDate.getFullYear();
            const month = String(currentDate.getMonth() + 1).padStart(2, '0');
            const day = String(currentDate.getDate()).padStart(2, '0');
            dateStr = `${year}-${month}-${day}`;
        } else {
            currentDate = d;
            // Use local date components to avoid timezone issues
            const year = currentDate.getFullYear();
            const month = String(currentDate.getMonth() + 1).padStart(2, '0');
            const day = String(currentDate.getDate()).padStart(2, '0');
            dateStr = `${year}-${month}-${day}`;
        }
    }
    
    const container = $("#dayCalendar").addClass('date-view').removeClass('resource-view');
    const view = $("#viewSelect").val();
    $("#dayDatePicker").toggleClass('d-none', view === 'custom');
    const selectedService = $("#MainContent_ServiceTypeFilter").val();
    const isAllServices = selectedService === 'all' || !selectedService;

    const selectedStatus = $("#MainContent_StatusTypeFilter_DateView").val();
    const isAllStatuses = selectedStatus === 'all' || !selectedStatus;

    const selectedTicketStatus = $("#MainContent_TicketStatusFilter_DateView").val();
    const isAllTicketStatuses = selectedTicketStatus === 'all' || !selectedTicketStatus;

    const selectedGroup = $("#dispatchGroupDateView").val();
    const selectedIndividualResource = $("#individualResourceFilterDateView").val();

    renderDateNav("dateNav", dateStr);

    let fromDate, toDate, todayParam;
    let fromStr, toStr;

    if (view === 'custom' && customDateRange.from && customDateRange.to) {
        fromStr = customDateRange.from;
        toStr = customDateRange.to;
        todayParam = "";
    } else {
        switch (view) {
            case 'month':
                fromDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
                toDate = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
                // Use formatDateToISO to avoid timezone issues
                fromStr = formatDateToISO(fromDate);
                toStr = formatDateToISO(toDate);
                todayParam = "";
                break;
            case 'week':
                fromDate = new Date(currentDate);
                toDate = new Date(currentDate);
                toDate.setDate(currentDate.getDate() + 6);
                // Use formatDateToISO to avoid timezone issues
                fromStr = formatDateToISO(fromDate);
                toStr = formatDateToISO(toDate);
                todayParam = "";
                break;
            case 'threeDay':
                fromDate = new Date(currentDate);
                toDate = new Date(currentDate);
                toDate.setDate(currentDate.getDate() + 2);
                // Use formatDateToISO to avoid timezone issues
                fromStr = formatDateToISO(fromDate);
                toStr = formatDateToISO(toDate);
                todayParam = "";
                break;
            default: // 'day' view
                fromStr = dateStr;
                toStr = dateStr;
                todayParam = dateStr;
                break;
        }
    }

    const slotDurationMinutes = 30;

    getAppoinments('', fromStr, toStr, todayParam, function (fetchedAppointments) {
        $('#dateViewLoading').hide();
        
        const filteredAppointments = fetchedAppointments.filter(a => {
            const serviceMatch = isAllServices || (a.ServiceTypeID != null && String(a.ServiceTypeID) == selectedService);
            const statusMatch = isAllStatuses || (a.AppoinmentStatusID != null && String(a.AppoinmentStatusID) == selectedStatus);
            const ticketStatusMatch = isAllTicketStatuses || (a.TicketStatusID != null && String(a.TicketStatusID) == selectedTicketStatus);
            
            const individualResourceMatch = !selectedIndividualResource || selectedIndividualResource === 'all' || (a.ResourceID != null && String(a.ResourceID) == selectedIndividualResource);
            
            let groupMatch = true;
            if (!selectedIndividualResource || selectedIndividualResource === 'all') {
                if (selectedGroup && selectedGroup !== 'all') {
                    const groupMembers = technicianGroups[selectedGroup] || [];
                    groupMatch = groupMembers.includes(a.ResourceName);
                }
            }

            return serviceMatch && statusMatch && ticketStatusMatch && individualResourceMatch && groupMatch;
        });

        // Start of dynamic time slot extension
        let viewTimeSlots = JSON.parse(JSON.stringify(allTimeSlots)); // Deep copy

        if (viewTimeSlots.length > 0) {
            let maxTimeMinutes = 0;

            const lastOriginalSlot = viewTimeSlots[viewTimeSlots.length - 1];
            if (lastOriginalSlot && lastOriginalSlot.TimeBlockSchedule) {
                const lastOriginalSlotEndTimeStr = lastOriginalSlot.TimeBlockSchedule.split('-')[1];
                if (lastOriginalSlotEndTimeStr) {
                    maxTimeMinutes = parseTimeToMinutes(lastOriginalSlotEndTimeStr.trim());
                }
            }
            
            filteredAppointments.forEach(a => {
                if (a.TimeSlot && a.Duration) {
                    let startTimeMinutes;
                    // Handle both StartDateTime and TimeSlot for start time
                    if (a.StartDateTime) {
                        const startDt = new Date(a.StartDateTime);
                        if (!isNaN(startDt)) {
                           startTimeMinutes = startDt.getHours() * 60 + startDt.getMinutes();
                        }
                    } else {
                        const timeSlotInfo = allTimeSlots.find(slot => slot.TimeBlockSchedule === a.TimeSlot || (slot.TimeBlock && slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase()));
                        if (timeSlotInfo && timeSlotInfo.TimeBlockSchedule) {
                            const startTimeStr = timeSlotInfo.TimeBlockSchedule.split('-')[0].trim();
                            startTimeMinutes = parseTimeToMinutes(startTimeStr);
                        }
                    }
    
                    if (startTimeMinutes !== undefined) {
                        const durationMinutes = parseDuration(a.Duration);
                        if (!isNaN(durationMinutes)) {
                            const endTimeMinutes = startTimeMinutes + durationMinutes;
                            if (endTimeMinutes > maxTimeMinutes) {
                                maxTimeMinutes = endTimeMinutes;
                            }
                        }
                    }
                }
            });

            function formatMinutesToTime(minutes) {
                let h = Math.floor(minutes / 60) % 24;
                let m = minutes % 60;
                const ampm = h >= 12 ? 'PM' : 'AM';
                h = h % 12;
                h = h ? h : 12; // the hour '0' should be '12'
                let m_str = m.toString().padStart(2, '0');
                return `${h}:${m_str} ${ampm}`;
            }

            let lastSlot = viewTimeSlots.length > 0 ? viewTimeSlots[viewTimeSlots.length - 1] : null;
            let lastSlotEndTimeMinutes = 0;
            if(lastSlot && lastSlot.TimeBlockSchedule) {
                const lastSlotEndTimeStr = lastSlot.TimeBlockSchedule.split('-')[1];
                if(lastSlotEndTimeStr) {
                     lastSlotEndTimeMinutes = parseTimeToMinutes(lastSlotEndTimeStr.trim());
                }
            }
            
            // Extend slots in 30-minute increments
            while (lastSlotEndTimeMinutes < maxTimeMinutes) {
                const newSlotStartMinutes = lastSlotEndTimeMinutes;
                const newSlotEndMinutes = newSlotStartMinutes + 30;

                const newSlot = {
                    TimeBlock: `Custom ${viewTimeSlots.length + 1}`,
                    TimeBlockSchedule: `${formatMinutesToTime(newSlotStartMinutes)} - ${formatMinutesToTime(newSlotEndMinutes)}`
                };
                viewTimeSlots.push(newSlot);
                lastSlotEndTimeMinutes = newSlotEndMinutes;
            }
        }
        // End of dynamic time slot extension

        // Format header date in USA format (MM/DD/YYYY) for display, using dateStr to avoid timezone issues
        const headerDateStr = formatDateToUSA(dateStr);
        let html = `<div class="custom-calendar-header d-flex justify-content-center">
                <span>${headerDateStr}</span>
            </div>`;

        if (view === 'month') {
            const firstDay = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
            const lastDay = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
            const startWeek = firstDay.getDay();
            const totalDays = lastDay.getDate();
            let calendarDays = [];
            for (let i = 0; i < startWeek; i++) calendarDays.push(null);
            for (let i = 1; i <= totalDays; i++) calendarDays.push(i);
            while (calendarDays.length % 7 !== 0) calendarDays.push(null);

            html += `<div class="calendar-grid-month">
                ${['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => `<div class="text-center font-weight-medium p-2 calendar-header-cell">${day}</div>`).join('')}`;
            calendarDays.forEach(day => {
                const dayDate = day ? `${currentDate.getFullYear()}-${(currentDate.getMonth() + 1).toString().padStart(2, '0')}-${day.toString().padStart(2, '0')}` : '';
                const dayAppointments = filteredAppointments.filter(a => {
                    const apptDate = a.RequestDate || (a.StartDateTime ? a.StartDateTime.split(' ')[0] : null);
                    return apptDate === dayDate;
                });
                // All appointments displayed, previous limit removed.
                const displayAppointments = dayAppointments;
                // const remainingCount = dayAppointments.length - 5; // No longer needed
                
                html += `<div class="min-h-100px border p-1 drop-target calendar-cell ${dayDate === dateStr ? 'bg-blue-50 border-blue-200' : ''}" data-date="${dayDate}">
                    ${day ? `<div class="text-right fs-7 mb-1">${day}</div>
                        <div class="space-y-1">
                            ${displayAppointments.map(a => `
                                <div class="calendar-event ${getEventTimeSlotClass(a)} fs-7 p-1 cursor-move" data-id="${a.AppoinmentId}" draggable="true">
                                    ${getAppointmentStatusIcon(a.AppoinmentStatus)}
                                    ${getTicketStatusIcon(a.AppoinmentStatus)} 
                                    ${a.CustomerName} 
                                </div>`).join('')}
                            </div>` : ''}
                </div>`;
            });
            html += `</div>`;
        } else if (view === 'week' || view === 'threeDay' || view === 'custom') {
            let dates = [];
            if (view === 'custom' && customDateRange.from && customDateRange.to) {
                const start = new Date(customDateRange.from + 'T00:00:00');
                const end = new Date(customDateRange.to + 'T00:00:00');
                for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
                    // Use formatDateToISO to avoid timezone issues
                    dates.push(formatDateToISO(d));
                }
            } else {
                const days = view === 'week' ? 7 : 3;
                const startDate = new Date(currentDate);
                dates = Array.from({ length: days }, (_, i) => {
                    const d = new Date(startDate);
                    d.setDate(startDate.getDate() + i);
                    // Use formatDateToISO to avoid timezone issues
                    return formatDateToISO(d);
                });
            }

            html += `<div class="border rounded overflow-hidden">
               <div class="calendar-grid" style="grid-template-columns: 80px repeat(${dates.length}, 1fr);">
                    <div class="p-2 border-right bg-gray-50 calendar-header-cell"></div>
                    ${dates.map(day => {
                        // Parse day string to Date object for weekday, use formatDateToUSA for date display
                        let dayDate = new Date(day + 'T00:00:00');
                        if (isNaN(dayDate.getTime())) {
                            const parts = day.split('-').map(part => parseInt(part, 10));
                            dayDate = new Date(parts[0], parts[1] - 1, parts[2]);
                        }
                        const weekday = dayDate.toLocaleDateString('en-US', { weekday: 'short' });
                        const formattedDate = formatDateToUSA(day);
                        return `
                        <div class="p-2 text-center font-weight-medium border-right last-border-right-none bg-gray-50 calendar-header-cell">
                            <div>${weekday}</div>
                            <div>${formattedDate}</div>
                        </div>`;
                    }).join('')}
                </div>
                <div class="calendar-body">`;

            if (!viewTimeSlots || viewTimeSlots.length === 0) {
                ;
            } else {

                const renderedAppointments = {};
                dates.forEach(d => {
                    renderedAppointments[d] = new Set();
                });

                viewTimeSlots.forEach((time, index) => {
                    html += `<div class="calendar-grid" style="grid-template-columns: 80px repeat(${dates.length}, 1fr);">
                        <div class="h-60px border-bottom last-border-bottom-none p-1 fs-7 text-right pr-2 bg-gray-50 calendar-time-cell">
                            ${formatTimeRange(time.TimeBlockSchedule)}
                        </div>`;
                    dates.forEach(dStr => {
                        const cellAppointments = filteredAppointments
                            .filter(a => a.RequestDate === dStr && a.TimeSlot)
                            .map(a => {
                                const timeSlot = viewTimeSlots.find(slot => slot.TimeBlockSchedule === a.TimeSlot || slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase());
                                if (!timeSlot) {
                                    console.warn(`No matching time slot for appointment ${a.AppoinmentId}:`, { appointmentTimeSlot: a.TimeSlot, availableTimeSlots: viewTimeSlots.map(s => s.TimeBlockSchedule) });
                                    return null;
                                }
                                const startIndex = viewTimeSlots.findIndex(slot => slot.TimeBlockSchedule === timeSlot.TimeBlockSchedule);
                                if (startIndex !== -1 && !renderedAppointments[dStr].has(a.AppoinmentId)) {
                                    const durationMinutes = parseDuration(a.Duration);
                                    const startTimeMinutes = parseTimeToMinutes(timeSlot.TimeBlockSchedule.split('-')[0]);
                                    const slotStartTimeMinutes = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[0]);
                                    if (isNaN(startTimeMinutes) || isNaN(slotStartTimeMinutes) || isNaN(durationMinutes) || slotDurationMinutes === 0) {
                                        console.warn(`Invalid data for appointment ${a.AppoinmentId}:`, { startTimeMinutes, slotStartTimeMinutes, durationMinutes, slotDurationMinutes, timeSlot: a.TimeSlot });
                                        return null;
                                    }
                                    const offsetMinutes = startTimeMinutes - slotStartTimeMinutes;
                                    const offsetPx = (offsetMinutes / slotDurationMinutes) * 40;
                                    const heightPx = (durationMinutes / slotDurationMinutes) * 40;
                                    return { appointment: a, offsetPx, heightPx, startIndex };
                                }
                                return null;
                            })
                            .filter(a => a && a.startIndex === index);

                        cellAppointments.sort((a, b) => {
                            const aStart = parseTimeToMinutes(a.appointment.TimeSlot.split('-')[0]);
                            const bStart = parseTimeToMinutes(b.appointment.TimeSlot.split('-')[0]);
                            return aStart - bStart;
                        });

                        const numAppointments = cellAppointments.length;

                        html += `<div class="h-60px border-bottom last-border-bottom-none border-right last-border-right-none p-1 relative drop-target calendar-cell" style="overflow: visible;" data-date="${dStr}" data-time="${time.TimeBlockSchedule}">
                            ${cellAppointments.map((appt, idx) => {
                            renderedAppointments[dStr].add(appt.appointment.AppoinmentId);
                            return `<div class="calendar-event ${getEventTimeSlotClass(appt.appointment)} cursor-move fs-7 truncate"
                                     style="position: absolute; top: ${appt.offsetPx}px; left: calc(${idx} * 100% / ${numAppointments}); height: ${appt.heightPx}px; width: calc(100% / ${numAppointments});"
                                     data-id="${appt.appointment.AppoinmentId}" draggable="true">
                                    <div class="font-weight-medium fs-7">
                                    ${getAppointmentStatusIcon(appt.appointment.AppoinmentStatus)}
                                    ${getTicketStatusIcon(appt.appointment.TicketStatus)}
                                    ${appt.appointment.CustomerName}
                                    </div>
                                    <div class="fs-7 truncate">${appt.appointment.ServiceType} (${appt.appointment.Duration})</div>
                                </div>`;
                        }).join('')}
                        </div>`;
                    });
                    html += `</div>`;
                });
            }
            html += `</div></div>`;
        } else {

            html += `<div class="border rounded overflow-hidden">
                <div class="calendar-grid" style="grid-template-columns: 80px 1fr;">
                    <div class="p-2 border-right bg-gray-50 calendar-header-cell"></div>
                    <div class="p-2 text-center font-weight-medium bg-gray-50 calendar-header-cell">
                       ${formatDateToUSA(dateStr)}
                    </div>
                </div>
                <div class="calendar-body">`;

            if (!viewTimeSlots || viewTimeSlots.length === 0) {
                ;
            } else {
                const renderedAppointments = new Set();

                viewTimeSlots.forEach((time, index) => {
                    html += `<div class="calendar-grid" style="grid-template-columns: 80px 1fr;">
                        <div class="h-60px border-bottom last-border-bottom-none p-1 fs-7 text-left pr-2 bg-gray-50 calendar-time-cell">
                            ${formatTimeRange(time.TimeBlockSchedule)}
                        </div>`;

                    const cellAppointments = filteredAppointments
                        .filter(a => {
                            // Check if appointment is on this date
                            const apptDate = a.RequestDate || (a.StartDateTime ? a.StartDateTime.split(' ')[0] : null);
                            if (apptDate !== dateStr) return false;
                            
                            // If appointment has StartDateTime/EndDateTime, use those for more accurate positioning
                            if (a.StartDateTime && a.EndDateTime) {
                                const startDt = new Date(a.StartDateTime);
                                const endDt = new Date(a.EndDateTime);
                                const slotStart = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[0]);
                                const slotEnd = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[1]);
                                const apptStartMinutes = startDt.getHours() * 60 + startDt.getMinutes();
                                const apptEndMinutes = endDt.getHours() * 60 + endDt.getMinutes();
                                
                                // Show appointment if it overlaps with this time slot
                                return (apptStartMinutes < slotEnd && apptEndMinutes > slotStart);
                            }
                            
                            // Fallback to TimeSlot matching
                            return a.TimeSlot;
                        })
                        .map(a => {
                            let startTimeMinutes, durationMinutes;
                            
                            // Use StartDateTime/EndDateTime if available for more accurate positioning
                            if (a.StartDateTime && a.EndDateTime) {
                                const startDt = new Date(a.StartDateTime);
                                const endDt = new Date(a.EndDateTime);
                                startTimeMinutes = startDt.getHours() * 60 + startDt.getMinutes();
                                const endTimeMinutes = endDt.getHours() * 60 + endDt.getMinutes();
                                durationMinutes = endTimeMinutes - startTimeMinutes;
                            } else {
                                // Fallback to TimeSlot parsing
                                const timeSlot = viewTimeSlots.find(slot => slot.TimeBlockSchedule === a.TimeSlot || slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase());
                                if (!timeSlot) {
                                    console.warn(`No matching time slot for appointment ${a.AppoinmentId}: TimeSlot=${a.TimeSlot}`);
                                    return null;
                                }
                                startTimeMinutes = parseTimeToMinutes(timeSlot.TimeBlockSchedule.split('-')[0]);
                                durationMinutes = parseDuration(a.Duration);
                            }
                            
                            const slotStartTimeMinutes = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[0]);
                            if (isNaN(startTimeMinutes) || isNaN(slotStartTimeMinutes) || isNaN(durationMinutes) || slotDurationMinutes === 0) {
                                console.warn(`Invalid data for appointment ${a.AppoinmentId}:`, { startTimeMinutes, slotStartTimeMinutes, durationMinutes, slotDurationMinutes });
                                return null;
                            }
                            
                            // Only render in the starting slot to avoid duplicates
                            const startIndex = viewTimeSlots.findIndex(slot => {
                                const slotStart = parseTimeToMinutes(slot.TimeBlockSchedule.split('-')[0]);
                                return Math.abs(slotStart - startTimeMinutes) < 30; // Within 30 minutes
                            });
                            
                            if (startIndex === index && !renderedAppointments.has(a.AppoinmentId)) {
                                const offsetMinutes = startTimeMinutes - slotStartTimeMinutes;
                                const offsetPx = Math.max(0, (offsetMinutes / slotDurationMinutes) * 25);
                                const heightPx = Math.max(25, (durationMinutes / slotDurationMinutes) * 25);
                                return { appointment: a, offsetPx, heightPx };
                            }
                            return null;
                        })
                        .filter(a => a);

                    cellAppointments.sort((a, b) => {
                        const aStart = parseTimeToMinutes(a.appointment.TimeSlot.split('-')[0]);
                        const bStart = parseTimeToMinutes(b.appointment.TimeSlot.split('-')[0]);
                        return aStart - bStart;
                    });

                    const appointmentWidth = 150;
                    const maxAppointments = cellAppointments.length;
                    const totalWidth = maxAppointments * appointmentWidth;

                    html += `<div class="h-60px border-bottom last-border-bottom-none border-right last-border-right-none p-1 relative drop-target calendar-cell" style="min-width: ${totalWidth}px; overflow: visible;" data-date="${dateStr}" data-time="${time.TimeBlockSchedule}">
                    ${cellAppointments.map((appt, idx) => {
                        const leftPx = idx * appointmentWidth;
                        renderedAppointments.add(appt.appointment.AppoinmentId);
                        return `<div class="calendar-event ${getEventTimeSlotClass(appt.appointment)} cursor-move fs-7 truncate"
                                 style="position: absolute; top: ${appt.offsetPx}px; left: ${leftPx}px; height: ${appt.heightPx}px; width: ${appointmentWidth}px;"
                                 data-id="${appt.appointment.AppoinmentId}" draggable="true">
                                <div class="font-weight-medium fs-7">
                                    ${getAppointmentStatusIcon(appt.appointment.AppoinmentStatus)} 
                                    ${getTicketStatusIcon(appt.appointment.TicketStatus)} 
                                    ${appt.appointment.CustomerName}
                                </div>
                                <div class="truncate">${appt.appointment.ServiceType} (${appt.appointment.Duration})</div>
                            </div>`;
                    }).join('')}
                </div>`;
                    html += `</div>`;
                });
            }
            html += `</div></div>`;
        }

        container.html(html);
        setupDragAndDrop();
        setupHoverEvents();
        updateCalendarEventColors();

        renderUnscheduledList('date', { from: fromStr, to: toStr });
        hideMainLoader();
        setTimeout(() => {
            $('#dateView .loading-overlay').hide();
        }, 100);
    });
}


$(document).off('change.servicetype', "[id$='ServiceTypeFilter']")
    .on('change.servicetype', "[id$='ServiceTypeFilter']", function () {
        const d = $('#dayDatePicker').val() || new Date().toISOString().slice(0, 10);
        renderDateView(d);
    });

function sendToFA(event, appointmentId) {
    event.preventDefault();
    event.stopPropagation();

    const clickedButton = event.currentTarget;
    const appointmentIndex = appointments.findIndex(a => a.AppoinmentId === appointmentId);

    if (appointmentIndex === -1) {
        console.error('FATAL: Appointment not found for ID:', appointmentId);
        return;
    }
    clickedButton.classList.remove('btn-outline-primary');
    clickedButton.classList.add('btn-success');
    clickedButton.innerHTML = '<i class="fas fa-check me-1"></i>FA-ID Sent';
    clickedButton.disabled = true;

    showAlert({
        icon: 'success',
        title: 'Sent!',
        text: `Appointment #${appointmentId} has been marked as sent to FA.`,
        timer: 2500,
        showConfirmButton: false
    });
    appointments[appointmentIndex].IsSentToFA = true;
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/MarkAsSentToFA",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                console.log(`Server confirmed IsSentToFA for Appointment ${appointmentId}.`);
            } else {
                console.error(`Server failed to set IsSentToFA for Appointment ${appointmentId}.`);
                showAlert({ icon: 'warning', title: 'Sync Failed', text: 'Could not save the change to the server.' });
            }
        },
        error: function () {
            console.error(`Network error setting IsSentToFA for Appointment ${appointmentId}.`);
            showAlert({ icon: 'error', title: 'Network Error', text: 'Could not reach the server.' });
        }
    });
}


function renderActiveFilters(view) {
    const isResourceView = (view === 'resource');

    const containerSelector = isResourceView ? '#activeFiltersContainerResource' : '#activeFiltersContainer';
    const filters = [
        { id: isResourceView ? '#ResourceTypeFilter_Resource' : '#ResourceTypeFilter_2', label: 'Resource', type: 'select' },
        { id: isResourceView ? '#MainContent_StatusTypeFilter_Resource' : '#MainContent_StatusTypeFilter', label: 'Status', type: 'select' },
        { id: isResourceView ? '#MainContent_ServiceTypeFilter_Resource' : '#MainContent_ServiceTypeFilter_2', label: 'Service', type: 'select' },
        { id: isResourceView ? '#CountryFilterResource' : '#CountryFilter', label: 'Country', type: 'select' },
        { id: isResourceView ? '#ProvinceFilterResource' : '#ProvinceFilter', label: 'Province', type: 'select' },
        { id: isResourceView ? '#PostalCodeFilterResource' : '#PostalCodeFilter', label: 'Postal Code', type: 'select' },
        { id: isResourceView ? '#searchFilterResource' : '#searchFilter', label: 'Search', type: 'text' }
    ];

    const $container = $(containerSelector);
    $container.empty();

    let hasActiveFilters = false;

    filters.forEach(filter => {
        const $filterEl = $(filter.id);
        if (!$filterEl.length) return;

        const value = $filterEl.val();
        let text = '';

        if (value && value !== 'all' && value !== '') {
            hasActiveFilters = true;
            if (filter.type === 'select') {
                text = $filterEl.find('option:selected').text();
            } else {
                text = value;
            }


            const pillHtml = `
                <div class="filter-pill" data-filter-id="${filter.id}">
                    <span>${filter.label}: <strong>${text}</strong></span>
                    <button type="button" class="remove-filter-btn" title="Remove this filter">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            `;
            $container.append(pillHtml);
        }
    });

    if (hasActiveFilters) {
        const clearAllHtml = `
            <button type="button" class="btn btn-link btn-sm text-danger" id="clearAllFiltersBtn">Clear All</button>
        `;
        $container.append(clearAllHtml);
    }
}

function renderUnscheduledList(view = 'date') {
    renderActiveFilters(view);
    const isResourceView = view === 'resource';

    const resourceFilterId = isResourceView ? '#ResourceTypeFilter_Resource' : '#ResourceTypeFilter_2';
    const statusFilterId = isResourceView ? '#MainContent_StatusTypeFilter_Resource' : '#MainContent_StatusTypeFilter';
    const serviceFilterId = isResourceView ? '#MainContent_ServiceTypeFilter_Resource' : '#MainContent_ServiceTypeFilter_2';
    const countryFilterId = isResourceView ? '#CountryFilterResource' : '#CountryFilter';
    const provinceFilterId = isResourceView ? '#ProvinceFilterResource' : '#ProvinceFilter';
    const postalCodeFilterId = isResourceView ? '#PostalCodeFilterResource' : '#PostalCodeFilter';
    const searchFilterId = isResourceView ? '#searchFilterResource' : '#searchFilter';
    const listContainerId = isResourceView ? '#unscheduledListResource' : '#unscheduledList';

    const resourceFilterValue = $(resourceFilterId).val() || 'all';
    const statusFilterValue = $(statusFilterId).val() || '';
    const serviceFilterValue = $(serviceFilterId).val() || '';
    const countryFilterValue = $(countryFilterId).val() || 'all';
    const provinceFilterValue = $(provinceFilterId).val() || 'all';
    const postalCodeFilterValue = $(postalCodeFilterId).val() || 'all';
    const searchFilter = ($(searchFilterId).val() || '').toLowerCase().trim();
    const isBatchModeActive = statusFilterValue !== '' && statusFilterValue !== 'all';
    updateDependentFilters(countryFilterValue, provinceFilterId, postalCodeFilterId);

    const statusFilterText = statusFilterValue === '' ? '' : $(`${statusFilterId} option:selected`).text();
    const serviceFilterText = serviceFilterValue === '' ? '' : $(`${serviceFilterId} option:selected`).text();

    const filteredAppointments = appointments.filter(app => {
        const isUnassigned = !(app.ResourceName && app.ResourceName.trim() !== '' && app.ResourceName !== 'Unassigned' && app.ResourceName !== 'none');
        let matchesResource = true;
        if (resourceFilterValue === 'unassigned') {
            matchesResource = isUnassigned;
        } else if (resourceFilterValue === 'assigned') {
            matchesResource = !isUnassigned;
        }

        const matchesStatus = (statusFilterValue === '' || statusFilterValue === 'all') || (app.AppoinmentStatusID != null && String(app.AppoinmentStatusID) == statusFilterValue);
        const matchesService = (serviceFilterValue === '' || serviceFilterValue === 'all') || (app.ServiceTypeID != null && String(app.ServiceTypeID) == serviceFilterValue);
        const matchesCountry = (countryFilterValue === 'all') || (app.Country === countryFilterValue);
        const matchesProvince = (provinceFilterValue === 'all') || (app.State === provinceFilterValue);
        const matchesPostalCode = (postalCodeFilterValue === 'all') || (app.PostalCode === postalCodeFilterValue);
        const matchesSearch = !searchFilter ||
            (app.CustomerName && app.CustomerName.toLowerCase().includes(searchFilter)) ||
            (app.Address1 && app.Address1.toLowerCase().includes(searchFilter));

        return matchesResource && matchesStatus && matchesService && matchesCountry && matchesProvince && matchesPostalCode && matchesSearch;
    });


    const sortedAppointments = filteredAppointments.slice().sort((a, b) => {
        const getDate = (dateStr) => {
            const date = new Date(dateStr);
            return isNaN(date) ? new Date('9999-12-31') : date;
        };
        const dateA = getDate(a.RequestDate);
        const dateB = getDate(b.RequestDate);
        const timeA = parseTimeToMinutes(a.TimeSlot);
        const timeB = parseTimeToMinutes(b.TimeSlot);

        if (unscheduledSortOrder === 'asc') {
            if (dateA < dateB) return -1;
            if (dateA > dateB) return 1;
            if (timeA < timeB) return -1;
            if (timeA > timeB) return 1;
            return (a.CustomerName || '').localeCompare(b.CustomerName || '');
        } else {
            if (dateA > dateB) return -1;
            if (dateA < dateB) return 1;
            if (timeA > timeB) return -1;
            if (timeA < timeB) return 1;
            return (b.CustomerName || '').localeCompare(a.CustomerName || '');
        }
    });

    const $listContainer = $(listContainerId);
    $listContainer.empty();

    $listContainer.toggleClass('batch-mode-active', isBatchModeActive);

    if (sortedAppointments.length === 0) {
        $listContainer.append('<div class="text-center py-4 text-muted">No appointments match the filters.</div>');
        updateBatchActionUI();
        return;
    }

    sortedAppointments.forEach(app => {
        const checkboxHtml = isBatchModeActive
            ? `<input 
         type="checkbox" 
         class="appointment-select-checkbox form-check-input" 
         data-id="${app.AppoinmentId}"
         ${batchSelectedAppointments.has(app.AppoinmentId.toString()) ? 'checked' : ''}
       >`
            : '';

        const displayDate = app.StartDateTime ? app.StartDateTime.split(' ')[0] : app.RequestDate;
        const faIdHtml = `<button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="openFaIdModal(event, '${app.AppoinmentId}')">FA-ID Sent</button>`;

        const card = `
  <div class="appointment-card card mb-3 shadow-sm unscheduled-item" data-id="${app.AppoinmentId}" draggable="true">
    <div class="card-body p-3">
      <div class="d-flex justify-content-between align-items-start">
        <div class="d-flex align-items-center gap-2">
          ${checkboxHtml}
          <h3 class="font-weight-medium fs-6 mb-0">${app.CustomerName || 'Unknown Customer'}</h3>
        </div>
        <span class="fs-7 text-muted"><i class="fa fa-user me-1"></i>${app.ResourceName || 'Unassigned'}</span>
      </div>
              <div class="fs-7 text-muted mt-1 line-clamp-2">${[app.SiteAddress || app.Address1, app.City, app.State, app.ZipCode].filter(Boolean).join(', ') || 'No address'}</div>
              <div class="fs-7 text-muted mt-1">
                <i class="fa fa-calendar me-1"></i>${formatToUSDate(displayDate)}
                &nbsp;&nbsp; 
                <i class="fa fa-clock me-1"></i>${formatTimeRange(app.TimeSlot)}
              </div>
              <div class="d-flex justify-content-between align-items-center mt-2">
                <span class="fs-7">${app.ServiceType || 'Unknown'}</span>
                <div class="d-flex align-items-center gap-2">
                    ${faIdHtml}
                    <span class="fs-7 truncate status status-${(app.AppoinmentStatus || '').toLowerCase().replace(/\s+/g, '-')}">${app.AppoinmentStatus || 'N/A'}</span>
                </div>
              </div>
            </div>
          </div>`;
        $listContainer.append(card);
    });

    setupDragAndDrop();
    updateBatchActionUI();
}
function handleSelectAllClick(selectAllButton) {
    const $button = $(selectAllButton);
    const buttonText = $button.text().trim().toLowerCase();
    const shouldSelect = buttonText === 'select all';
    
    // Find the correct list container based on active tab
    const isResourceView = $('#resource-tab').hasClass('active');
    const listContainerSelector = isResourceView ? '#unscheduledListResource' : '#unscheduledList';
    const $listContainer = $(listContainerSelector);
    
    if ($listContainer.length === 0) {
        console.warn('List container not found:', listContainerSelector);
        // Try alternative selector
        const altSelector = isResourceView ? '.tab-pane#resource-tab #unscheduledListResource' : '.tab-pane#date-tab #unscheduledList';
        const $altContainer = $(altSelector);
        if ($altContainer.length > 0) {
            $listContainer = $altContainer;
        } else {
            return;
        }
    }
    
    const visibleCheckboxes = $listContainer.find('.appointment-select-checkbox');

    if (visibleCheckboxes.length === 0) {
        console.warn('No checkboxes found to select. Container:', listContainerSelector, 'Found elements:', $listContainer.find('input[type="checkbox"]').length);
        return;
    }

    visibleCheckboxes.each(function () {
        const $checkbox = $(this);
        const appointmentId = $checkbox.data('id');
        
        if (!appointmentId) {
            console.warn('Checkbox missing appointment ID', $checkbox);
            return;
        }
        
        const appointmentIdStr = appointmentId.toString();
        $checkbox.prop('checked', shouldSelect);

        if (shouldSelect) {
            batchSelectedAppointments.add(appointmentIdStr);
        } else {
            batchSelectedAppointments.delete(appointmentIdStr);
        }
    });

    updateSelectAllButtonState();
    updateBatchSelectionUI();
    updateBatchActionUI();
}


function handleAppointmentSelection(event) {
    event.stopPropagation();
    const checkbox = event.target;
    const $checkbox = $(checkbox);
    const appointmentId = $checkbox.data('id');
    
    if (!appointmentId) {
        console.warn('No appointment ID found on checkbox', $checkbox);
        return;
    }

    const appointmentIdStr = appointmentId.toString();
    const isChecked = checkbox.checked || $checkbox.prop('checked');

    if (isChecked) {
        batchSelectedAppointments.add(appointmentIdStr);
    } else {
        batchSelectedAppointments.delete(appointmentIdStr);
    }

    updateSelectAllButtonState();
    updateBatchSelectionUI();
    updateBatchActionUI();
}

function updateSelectAllButtonState() {
    updateBatchSelectionUI();
    updateBatchActionUI();
}
function updateSelectionCounter() {
    if (isUpdatingBatchUI) return; // Prevent infinite recursion
    const count = batchSelectedAppointments.size;
    const counterElement = document.getElementById('selectionCounter');
    const selectAllBtn = document.getElementById('selectAllBtn');

    const $listContainer = $('.tab-pane.active .unscheduled-list');
    const totalVisible = $listContainer.find('.appointment-select-checkbox').length;

    if (counterElement) {
        counterElement.textContent = `Selected ${count}`;
    }

    if (selectAllBtn) {
        if (count > 0 && count === totalVisible) {
            $(selectAllBtn).text('Deselect All');
        } else {
            $(selectAllBtn).text('Select All');
        }
    }

    // Only update batch action UI if not already updating to prevent recursion
    if (!isUpdatingBatchUI) {
        updateBatchActionUI();
    }
}


function updateBatchCounter() {
    const count = batchSelectedAppointments.size;
    const counterElement = document.getElementById('batchCounter');
    if (counterElement) {
        if (count > 0) {
            counterElement.textContent = `${count} selected`;
            counterElement.style.display = 'inline';
        } else {
            counterElement.style.display = 'none';
        }
    }
}


function updateDependentFilters(selectedCountry, provinceSelector, postalSelector) {
    const $provinceFilter = $(provinceSelector);
    const $postalFilter = $(postalSelector);

    const currentProv = $provinceFilter.val();
    const currentPostal = $postalFilter.val();

    $provinceFilter.empty().append('<option value="all">All Provinces/States</option>');
    let provinces = [];
    if (selectedCountry === 'Canada') {
        provinces = statesData.Canada;
    } else if (selectedCountry === 'USA') {
        provinces = statesData.USA;
    } else {
        provinces = [...statesData.Canada, ...statesData.USA].sort();
    }
    provinces.forEach(p => $provinceFilter.append(`<option value="${p}">${p}</option>`));

    if (provinces.includes(currentProv)) {
        $provinceFilter.val(currentProv);
    }

    const selectedProvince = $provinceFilter.val();

    let relevantAppointments = appointments;
    if (selectedCountry !== 'all') {
        relevantAppointments = relevantAppointments.filter(app => app.Country === selectedCountry);
    }
    if (selectedProvince !== 'all') {
        relevantAppointments = relevantAppointments.filter(app => app.State === selectedProvince);
    }

    const postalCodes = [...new Set(relevantAppointments.map(app => app.PostalCode).filter(Boolean))].sort();
    $postalFilter.empty().append('<option value="all">All Postal/Zip Codes</option>');
    postalCodes.forEach(code => $postalFilter.append(`<option value="${code}">${code}</option>`));

    if (postalCodes.includes(currentPostal)) {
        $postalFilter.val(currentPostal);
    }
}

function formatToUSDate(dateString) {

    if (!dateString) {
        return 'No date';
    }
    const parts = dateString.split('-');
    if (parts.length !== 3) {
        return dateString;
    }
    const date = new Date(parts[0], parts[1] - 1, parts[2]);

    if (isNaN(date.getTime())) {
        return dateString;
    }

    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const year = date.getFullYear();

    return `${month}/${day}/${year}`;
}



function performSort(view) {

    unscheduledSortOrder = unscheduledSortOrder === 'asc' ? 'desc' : 'asc';
    renderUnscheduledList(view);
}
function syncModalTimes() {
    const modal = document.getElementById('editModal');
    const datePicker = modal.querySelector("[name='date']");
    const timeSlotSelect = modal.querySelector("[name='timeSlot']");
    const startDateInput = modal.querySelector('#txt_StartDate');
    const durationInput = modal.querySelector('#duration');

    const dateValue = datePicker.value;
    const timeSlotValue = timeSlotSelect.value;

    if (!dateValue || !timeSlotValue) return;

    const selectedSlot = allTimeSlots.find(slot => slot.TimeBlock === timeSlotValue);
    if (!selectedSlot) return;

    const timeMatch = selectedSlot.TimeBlockSchedule.match(/(\d{1,2}:\d{2}\s*[AP]M)/);
    if (!timeMatch) return;

    const startTimeStr = timeMatch[0];
    const newStartDateTime = moment(`${dateValue} ${startTimeStr}`, 'YYYY-MM-DD hh:mm A');

    if (newStartDateTime.isValid()) {
        startDateInput.value = newStartDateTime.format('MM/DD/YYYY hh:mm A');
        updateEndDateFromDuration();
    }
}


function setupDragAndDrop() {

    $(".calendar-event, .calendar-event-resource, .appointment-card").draggable({
        revert: "invalid",
        revertDuration: 200,
        zIndex: 1000,
        helper: "clone",
        opacity: 0.7,
        scroll: true,
        scrollSensitivity: 100,
        start: function (event, ui) {
            $(this).addClass("dragging");
            ui.helper.addClass("shadow-sm").css({
                width: $(this).width(),
                transition: "none",
                transform: "translateZ(0)"
            });
            hideDetailsPopup(calendarDetailsPopup);
            hideDetailsPopup(cardDetailsPopup);
        },
        stop: function () {
            $(this).removeClass("dragging");
        }
    });


    $(".calendar-event-resource").resizable({
        handles: "e",
        stop: function (event, ui) {
            const appointmentId = $(this).data("id").toString();
            const appointment = appointments.find(a => a.AppoinmentId === appointmentId);
            if (!appointment) { return; }

            let startDateTime;


            if (appointment.StartDateTime && !isNaN(new Date(appointment.StartDateTime))) {
                startDateTime = new Date(appointment.StartDateTime);
            }

            else {
                const dateStr = appointment.RequestDate;
                const timeSlotStr = appointment.TimeSlot;

                if (dateStr && timeSlotStr) {

                    const timeMatch = timeSlotStr.match(/(\d{1,2}:\d{2}(\s*[AP]M)?)/);


                    if (timeMatch) {
                        startDateTime = new Date(`${dateStr} ${timeMatch[0]}`);
                    }
                }
            }


            if (!startDateTime || isNaN(startDateTime.getTime())) {
                showAlert({
                    icon: 'error',
                    title: 'Cannot Resize Appointment',
                    text: `This appointment (ID: ${appointmentId}) has an invalid date or time slot format that could not be parsed.`
                });
                updateAllViews();
                return;
            }


            const newWidth = ui.size.width;
            const pixelsPerHour = 200;
            const newDurationInMinutes = Math.round((newWidth / pixelsPerHour) * 60);
            const newEndDateTime = new Date(startDateTime.getTime() + newDurationInMinutes * 60000);
            const newHours = Math.floor(newDurationInMinutes / 60);
            const newMinutes = newDurationInMinutes % 60;

            const formatForServer = (dt) => {
                if (isNaN(dt.getTime())) return '';
                const mo = (dt.getMonth() + 1).toString().padStart(2, '0');
                const d = dt.getDate().toString().padStart(2, '0');
                const y = dt.getFullYear();
                let h = dt.getHours();
                const m = dt.getMinutes().toString().padStart(2, '0');
                const ampm = h >= 12 ? 'PM' : 'AM';
                h = h % 12; h = h ? h : 12;
                return `${mo}/${d}/${y} ${h}:${m} ${ampm}`;
            };

            $.ajax({
                type: "POST",
                url: "Appointments.aspx/UpdateAppointmentDuration",
                data: JSON.stringify({
                    AppoinmentId: parseInt(appointment.AppoinmentId),
                    StartDateTime: formatForServer(startDateTime),
                    EndDateTime: formatForServer(newEndDateTime),
                    Hour: newHours,
                    Minute: newMinutes
                }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        showAlert({ icon: 'success', title: 'Duration Updated!', timer: 1500, showConfirmButton: false });

                        appointment.StartDateTime = formatForServer(startDateTime);
                        appointment.EndDateTime = formatForServer(newEndDateTime);
                        saveAppointments();
                        updateAllViews();

                        // --- NEW LOGIC TO REFRESH EDIT MODAL ---
                        const editModalElement = document.getElementById('editModal');
                        const isModalVisible = editModalElement && editModalElement.classList.contains('show'); // Check if Bootstrap modal is visible

                        if (isModalVisible && currentEditId && currentEditId.toString() === appointment.AppoinmentId.toString()) {
                            // If the modal is open and showing the *same* appointment, re-populate it
                            openEditModal(currentEditId);
                        }
                        // --- END NEW LOGIC ---
                    } else {
                        showAlert({ icon: 'error', title: 'Update Failed' });
                        updateAllViews();
                    }
                },
                error: function () {
                    showAlert({ icon: 'error', title: 'Server Error' });
                    updateAllViews();
                }
            });
        }
    });

    $(".drop-target").droppable({
        accept: ".appointment-card, .calendar-event, .calendar-event-resource",
        hoverClass: "drag-over",
        tolerance: "pointer",
        drop: function (event, ui) {
            const draggedAppointmentId = ui.draggable.data("id").toString();
            const newDate = $(this).data("date");
            const newTime = $(this).data("time");
            const newResourceName = $(this).data("resource") || "Unassigned";

            let appointmentIdsToSchedule = [];
            const isBatchMode = batchSelectedAppointments.size > 1 &&
                batchSelectedAppointments.has(draggedAppointmentId);

            if (isBatchMode) {
                appointmentIdsToSchedule = Array.from(batchSelectedAppointments);
            } else {
                appointmentIdsToSchedule = [draggedAppointmentId];
            }

            if (appointmentIdsToSchedule.length === 0) return;

            const isBatch = appointmentIdsToSchedule.length > 1;
            const confirmationText = isBatch
                ? `You are about to schedule ${appointmentIdsToSchedule.length} appointments for ${newResourceName || 'Unassigned'} on ${newDate} in the "${newTime || 'Original'}" slot. Continue?`
                : `Schedule appointment for ${newResourceName || 'Unassigned'} on ${newDate} in the "${newTime || 'Original'}" slot?`;

            const confirmButtonText = isBatch
                ? `Yes, schedule ${appointmentIdsToSchedule.length} appointments!`
                : 'Yes, schedule it!';

            showAlert({
                title: isBatch ? 'Confirm Batch Schedule' : 'Confirm Schedule',
                text: confirmationText,
                icon: 'info',
                showCancelButton: true,
                confirmButtonText: confirmButtonText,
            }).then((result) => {
                if (result.isConfirmed) {
                    processBatchUpdate(appointmentIdsToSchedule, newDate, newTime, newResourceName);
                }
            });
        }
    });

    $("#unscheduledList, #unscheduledListResource").droppable({
        accept: ".calendar-event, .calendar-event-resource, .appointment-card",
        hoverClass: "drag-over",
        tolerance: "pointer",
        drop: function (event, ui) {
            const appointmentId = ui.draggable.data("id").toString();
            const appointment = appointments.find(a => a.AppoinmentId === appointmentId);
            if (!appointment) {
                console.warn(`Appointment not found for ID: ${appointmentId}`);
                return;
            }


            appointment.ResourceID = 0;
            appointment.ResourceName = 'Unassigned';
            appointment.RequestDate = null;
            appointment.TimeSlot = null;
            appointment.Duration = '1 Hr';


            const serverAppointment = {
                AppoinmentId: parseInt(appointment.AppoinmentId),
                CustomerID: parseInt(appointment.CustomerID) || null,
                ServiceType: appointment.ServiceType,
                RequestDate: null,
                TimeSlot: null,
                ResourceID: 0,
                Status: appointment.AppoinmentStatus,
                TicketStatus: appointment.TicketStatus || null,
                Note: appointment.Note || '',
                StartDateTime: null,
                EndDateTime: null
            };

            $.ajax({
                type: "POST",
                url: "Appointments.aspx/UpdateAppointment",
                data: JSON.stringify({ appointment: serverAppointment }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        showAlert({
                            icon: 'success',
                            title: 'Success',
                            text: 'Appointment unscheduled successfully!',
                            confirmButtonText: 'OK',
                            timer: 2000,
                            customClass: {
                                popup: 'swal-custom-popup',
                                title: 'swal-custom-title',
                                content: 'swal-custom-content',
                                confirmButton: 'swal-custom-button'
                            }
                        });
                        saveAppointments();
                        updateAllViews();
                    } else {
                        showAlert({
                            icon: 'error',
                            title: 'Error',
                            text: 'Failed to unschedule appointment.',
                            confirmButtonText: 'OK',
                            customClass: {
                                popup: 'swal-custom-popup',
                                title: 'swal-custom-title',
                                content: 'swal-custom-content',
                                confirmButton: 'swal-custom-button'
                            }
                        });
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error unscheduling appointment:", error);
                    showAlert({
                        icon: 'error',
                        title: 'Error',
                        text: 'Failed to unschedule appointment due to a server error.',
                        confirmButtonText: 'OK',
                        customClass: {
                            popup: 'swal-custom-popup',
                            title: 'swal-custom-title',
                            content: 'swal-custom-content',
                            confirmButton: 'swal-custom-button'
                        }
                    });
                }
            });
        }
    });



    $(document).off('click', '.calendar-event, .calendar-event-resource, .appointment-card')
        .on('click', '.calendar-event, .calendar-event-resource, .appointment-card', function (e) {
            if ($(e.target).hasClass('appointment-select-checkbox')) { // Corrected class name
                return;
            }
            if (!$(this).hasClass('ui-draggable-dragging')) {
                const appointmentId = $(this).data("id").toString();
                openEditModal(appointmentId);
            }
        });
}
function updateBatchSelectionUI() {
    const batchCount = batchSelectedAppointments.size;
    const isResourceView = $('#resource-tab').hasClass('active');
    const batchContainerSelector = isResourceView ? '#batchActionContainerResource' : '#batchActionContainer';
    const counterSelector = isResourceView ? '#selectionCounterResource' : '#selectionCounter';
    const batchContainer = $(batchContainerSelector);

    if (batchCount > 0) {
        batchContainer.removeClass('d-none');
    } else {
        batchContainer.addClass('d-none');
    }

    const counterElement = document.querySelector(counterSelector);
    if (counterElement) {
        if (batchCount === 0) {
            counterElement.textContent = 'Selected 0';
        } else if (batchCount === 1) {
            counterElement.textContent = 'Selected 1';
        } else {
            counterElement.textContent = `Selected ${batchCount}`;
        }
    }

    const $selectAllBtn = isResourceView ? $('#selectAllBtnResource') : $('#selectAllBtn');
    const listContainerSelector = isResourceView ? '#unscheduledListResource' : '#unscheduledList';
    const $listContainer = $(listContainerSelector);
    const totalVisible = $listContainer.find('.appointment-select-checkbox').length;

    if ($selectAllBtn.length > 0) {
        if (totalVisible > 0 && batchCount === totalVisible && batchCount > 0) {
            $selectAllBtn.text('Deselect All');
        } else {
            $selectAllBtn.text('Select All');
        }
    }
}
function processBatchUpdate(appointmentIds, newDate, newTime, newResourceName) {
    const resourceObj = resources.find(r => r.ResourceName === newResourceName);
    const newResourceId = resourceObj ? resourceObj.Id : 0;

    let successCount = 0;
    let conflictCount = 0;
    const totalCount = appointmentIds.length;

    appointmentIds.forEach((id, index) => {
        const appointment = appointments.find(a => a.AppoinmentId === id);
        if (!appointment || appointment.AppoinmentStatus.toLowerCase() === "closed") {
            conflictCount++;
            return;
        }

        if (hasConflict(appointment, newTime || appointment.TimeSlot, newResourceName, newDate, id)) {
            conflictCount++;
            return;
        }

        let newStartDateTime = null;
        let newEndDateTime = null;
        const timeMatch = (newTime || appointment.TimeSlot).match(/(\d{1,2}:\d{2}\s*[AP]M)/);
        if (timeMatch) {
            newStartDateTime = moment(`${newDate} ${timeMatch[0]}`, 'YYYY-MM-DD hh:mm A');
            const durationMinutes = parseDuration(appointment.Duration);
            if (newStartDateTime.isValid() && durationMinutes > 0) {
                newEndDateTime = newStartDateTime.clone().add(durationMinutes, 'minutes');
            }
        }

        const serverAppointment = {
            AppoinmentId: parseInt(appointment.AppoinmentId),
            CustomerID: parseInt(appointment.CustomerID) || null,
            ServiceType: appointment.ServiceTypeID,
            RequestDate: newDate,
            TimeSlot: newTime || appointment.TimeSlot,
            ResourceID: newResourceId,
            Status: appointment.AppoinmentStatus,
            TicketStatus: appointment.TicketStatusID || null,
            Note: appointment.Note || '',
            StartDateTime: newStartDateTime ? newStartDateTime.format('MM/DD/YYYY hh:mm A') : null,
            EndDateTime: newEndDateTime ? newEndDateTime.format('MM/DD/YYYY hh:mm A') : null
        };

        $.ajax({
            type: "POST",
            url: "Appointments.aspx/UpdateAppointment",
            data: JSON.stringify({ appointment: serverAppointment }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                if (response.d) {
                    successCount++;
                    appointment.RequestDate = newDate;
                    appointment.TimeSlot = newTime || appointment.TimeSlot;
                    appointment.ResourceName = newResourceName;
                    appointment.ResourceID = newResourceId;
                    appointment.StartDateTime = serverAppointment.StartDateTime;
                    appointment.EndDateTime = serverAppointment.EndDateTime;
                } else {
                    conflictCount++;
                }
            },
            error: function () {
                conflictCount++;
            },
            complete: function () {
                if ((successCount + conflictCount) === totalCount) {
                    saveAppointments();
                    batchSelectedAppointments.clear();

                    let summaryTitle = 'Batch Complete';
                    let summaryText = `${successCount} of ${totalCount} appointments were confirmed successfully.`;
                    if (conflictCount > 0) {
                        summaryText += `\n${conflictCount} could not be confirmed due to conflicts or errors.`;
                        summaryTitle = 'Batch Partially Complete';
                    }

                    showAlert({ icon: 'success', title: summaryTitle, text: summaryText });

                    updateAllViews();
                }
            }
        });
    });
}

function createAppointment(e) {
    e.preventDefault();
    const form = new FormData(e.target);
    const newAppointment = {
        AppoinmentId: (Math.max(...appointments.map(a => parseInt(a.AppoinmentId)), 0) + 1).toString(),
        CustomerName: form.get("customerName"),
        ServiceType: form.get("serviceType"),
        RequestDate: form.get("date"),
        TimeSlot: form.get("timeSlot"),
        Duration: form.get("duration") || "1 Hr",
        ResourceName: form.get("resource"),
        AppoinmentStatus: form.get("status"),
        location: {
            Address1: form.get("address"),
            lat: 40.7128,
            lng: -74.0060
        },
        priority: "Medium"
    };

    if (newAppointment.RequestDate && newAppointment.TimeSlot && hasConflict(newAppointment, newAppointment.TimeSlot, newAppointment.ResourceName, newAppointment.RequestDate)) {
        showAlert({
            icon: 'error',
            title: 'Scheduling Conflict',
            text: 'A scheduling conflict was detected!',
            confirmButtonText: 'OK',
            customClass: {
                popup: 'swal-custom-popup',
                title: 'swal-custom-title',
                content: 'swal-custom-content',
                confirmButton: 'swal-custom-button'
            }
        });
        return;
    }

    appointments.push(newAppointment);
    saveAppointments();

    if (selectedForms.length > 0 && window.FormsManager) {
        window.FormsManager.createFormInstance(selectedForms[0].id, newAppointment.AppoinmentId, newAppointment.CustomerID || '')
            .then(() => {
                console.log('Form instances created for new appointment');
            })
            .catch(error => {
                console.error('Error creating form instances:', error);
            });
    }

    updateAllViews();
    window.newModalInstance.hide();

    selectedForms = [];
}
function openEditModal(id, date, time, resource, confirm) {
    const a = appointments.find(x => x.AppoinmentId === id.toString());
    if (!a) {
        console.error(`Appointment with ID ${id} not found.`);
        return;
    }


    const viewDetailsBtn = document.getElementById('viewCustomerDetailsBtn');
    if (viewDetailsBtn) {
        if (a.CustomerID && a.SiteId) {
            viewDetailsBtn.href = `CustomerDetails.aspx?custId=${encodeURIComponent(a.CustomerID)}&siteId=${a.SiteId}`;
            viewDetailsBtn.style.display = 'inline-block';
        } else {
            viewDetailsBtn.style.display = 'none';
        }
    }
    if (!confirm) {
        loadCurrentlySelectedForms(id);
    }
    if (a.AppoinmentStatus.toLowerCase() === "closed") {
        showAlert({ icon: 'info', title: 'Cannot Edit', text: 'This appointment is closed and cannot be edited.' });
        return;
    }

    currentEditId = id;
    const form = document.getElementById("editForm");
    if (!form) {
        console.error('Edit form not found in DOM');
        return;
    }

    loadCustomFields(form, a.AppoinmentId);

    form.querySelector("[id='AppoinmentId']").value = parseInt(a.AppoinmentId);
    form.querySelector("[id='CustomerID']").value = parseInt(a.CustomerID) || '';
    form.querySelector("[name='customerName']").value = a.CustomerName || '';

    populateSiteSelector(a);

    const emailToDisplay = (a.SiteId && a.SiteId !== "0" && a.SiteEmail) ? a.SiteEmail : a.Email;
    const emailInput = form.querySelector("[name='email']");
    const sendEmailBtn = document.getElementById('sendEmail');

    emailInput.value = emailToDisplay || '';
    if (emailToDisplay) {
        sendEmailBtn.href = `mailto:${emailToDisplay}`;
        sendEmailBtn.style.display = 'block';
    } else {
        sendEmailBtn.style.display = 'none';
    }

    const phoneInput = form.querySelector("[name='phone']");
    const mobileInput = form.querySelector("[name='mobile']");
    const callPhoneBtn = document.getElementById('callPhone');
    const callMobileBtn = document.getElementById('callMobile');

    phoneInput.value = a.Phone || '';
    if (a.Phone) {
        callPhoneBtn.href = `tel:${a.Phone}`;
        callPhoneBtn.style.display = 'block';
    } else {
        callPhoneBtn.style.display = 'none';
    }

    mobileInput.value = a.Mobile || '';
    if (a.Mobile) {
        callMobileBtn.href = `tel:${a.Mobile}`;
        callMobileBtn.style.display = 'block';
    } else {
        callMobileBtn.style.display = 'none';
    }


    form.querySelector("[name='note']").value = a.Note || '';

    getSelectedId(form.querySelector("[id='MainContent_ServiceTypeFilter_Edit']"), a.ServiceType || "");
    getSelectedId(form.querySelector("[id='MainContent_StatusTypeFilter_Edit']"), a.AppoinmentStatus || "");
    getSelectedId(form.querySelector("[id='MainContent_TicketStatusFilter_Edit']"), a.TicketStatus || "");
    getSelectedId(form.querySelector("[name='resource']"), resource || a.ResourceName || "");

    const startDateInput = form.querySelector("[id='txt_StartDate']");
    const endDateInput = form.querySelector("[id='txt_EndDate']");
    const durationInput = form.querySelector("[name='duration']");
    const datePicker = form.querySelector("[name='date']");
    const timeSlotSelect = form.querySelector("[name='timeSlot']");

    if (a.StartDateTime && moment(a.StartDateTime, 'MM/DD/YYYY hh:mm A').isValid()) {
        datePicker.value = moment(a.StartDateTime, 'MM/DD/YYYY hh:mm A').format('YYYY-MM-DD');
    } else {
        datePicker.value = a.RequestDate || '';
    }

    const timeSlotValue = time || a.TimeSlot || '';
    const matchingSlot = allTimeSlots.find(slot => slot.TimeBlockSchedule === timeSlotValue || slot.TimeBlock === timeSlotValue);
    timeSlotSelect.value = matchingSlot ? matchingSlot.TimeBlock : timeSlotValue;

    durationInput.value = a.Duration || "1 Hr : 0 Min";

    $(datePicker).off('change').on('change', syncModalTimes);
    $(timeSlotSelect).off('change').on('change', syncModalTimes);
    $(durationInput).off('change').on('change', updateEndDateFromDuration);
    $(startDateInput).off('change').on('change', calculateTimeRequired);
    $(endDateInput).off('change').on('change', calculateTimeRequired);

    syncModalTimes();


    if (confirm) {
        $('.confirm-title').removeClass('d-none');
        $('.edit-title').addClass('d-none');
    } else {
        $('.edit-title').removeClass('d-none');
        $('.confirm-title').addClass('d-none');
    }

    const isClosed = a.AppoinmentStatus.toLowerCase() === "closed";
    form.querySelector("[id='MainContent_ServiceTypeFilter_Edit']").disabled = isClosed;
    form.querySelector("[id='MainContent_StatusTypeFilter_Edit']").disabled = isClosed;
    form.querySelector("[id='MainContent_TicketStatusFilter_Edit']").disabled = isClosed;

    // Load appointment-specific links (invoices/estimates and forms)
    loadAppointmentSpecificLinks(id);
    
    // Reset CSL handlers flag when opening new modal
    cslHandlersInitialized = false;
    
    // Load CSL data when modal opens
    loadCslDataForModal(a.CustomerID, a.SiteId || 0);

    try {
        window.editModalInstance.show();
    } catch (error) {
        console.error('Error opening editModal:', error);
    }
}



function loadCustomFields(form, appointmentId) {
    const container = document.getElementById("customFieldsContainer");
    if (!container) {
        console.error("Custom fields container not found");
        return;
    }
    container.innerHTML = '<div class="text-center p-4">Loading custom fields...</div>';

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetActiveCustomFields",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ apptId: appointmentId }),
        dataType: "json",
        success: function (response) {
            container.innerHTML = ''; // Clear loading spinner
            if (response.d && response.d.length > 0) {
                renderCustomFields(response.d, container);
            } else {
                container.innerHTML = '<div class="alert alert-info">No active custom fields found.</div>';
            }
        },
        error: function (xhr, status, error) {
            container.innerHTML = '<div class="alert alert-danger">Failed to load custom fields.</div>';
            console.error("Error loading custom fields:", error);
        }
    });
}


function renderCustomFields(fields, container) {
    fields.forEach(field => {
        const fieldGroup = document.createElement('div');
        fieldGroup.className = 'form-group mt-2 col-md-6'; // Use grid columns

        const label = document.createElement('label');
        label.className = 'form-label';
        label.htmlFor = `custom_${field.FieldId}`;
        label.textContent = field.FieldName;
        fieldGroup.appendChild(label);

        let input;
        const value = field.Value; // Use the value from the server
        const options = field.Options ? JSON.parse(field.Options) : [];

        switch (field.FieldType) {
            case 'text':
            case 'number':
            case 'date':
                input = document.createElement('input');
                input.type = field.FieldType;
                input.id = `custom_${field.FieldId}`;
                input.name = `custom_${field.FieldId}`;
                input.className = 'form-control';
                if (value) input.value = value;
                break;
            case 'dropdown':
                input = document.createElement('select');
                input.id = `custom_${field.FieldId}`;
                input.name = `custom_${field.FieldId}`;
                input.className = 'form-select';
                const defaultOpt = document.createElement('option');
                defaultOpt.value = '';
                defaultOpt.textContent = 'Select an option';
                input.appendChild(defaultOpt);
                options.forEach(opt => {
                    const option = document.createElement('option');
                    option.value = opt;
                    option.textContent = opt;
                    if (opt === value) option.selected = true;
                    input.appendChild(option);
                });
                break;
            case 'checklist':
                input = document.createElement('div');
                const savedValues = value ? JSON.parse(value) : [];
                options.forEach(opt => {
                    const checkDiv = document.createElement('div');
                    checkDiv.className = 'form-check';
                    const chkInput = document.createElement('input');
                    chkInput.type = 'checkbox';
                    chkInput.className = 'form-check-input';
                    chkInput.name = `custom_${field.FieldId}`;
                    chkInput.value = opt;
                    if (savedValues.includes(opt)) chkInput.checked = true;
                    chkInput.id = `custom_${field.FieldId}_${opt.replace(/\s+/g, '_')}`;
                    const chkLabel = document.createElement('label');
                    chkLabel.className = 'form-check-label';
                    chkLabel.htmlFor = chkInput.id;
                    chkLabel.textContent = opt;
                    checkDiv.appendChild(chkInput);
                    checkDiv.appendChild(chkLabel);
                    input.appendChild(checkDiv);
                });
                break;
            default:
                return; // Skip unknown field types
        }

        if (input) fieldGroup.appendChild(input);
        container.appendChild(fieldGroup);
    });
}




function updateAppointment(e) {
    e.preventDefault();
    const form = new FormData(e.target);
    const id = form.get("AppoinmentId");
    const appointment = appointments.find(a => a.AppoinmentId === id);
    if (!appointment) return;

    if (selectedForms.length > 0) {
        updateAttachedForms();

        const newDate = form.get("date");
        const newTimeSlot = form.get("timeSlot");
        const formData = document.getElementById("editForm");
        const select_rs = formData.querySelector("[name='resource']");
        const newResource = select_rs.options[select_rs.selectedIndex].text;

        saveAppoinmentData(e);
    }
    saveAppoinmentData(e);
}

function openConfirmModal(id, date, timeSlot, resource) {
    const a = appointments.find(x => x.AppoinmentId === id.toString());
    if (!a) return;
    const form = document.getElementById("confirmForm");
    form.querySelector("[name='id']").value = a.AppoinmentId;
    form.querySelector("[name='customerName']").value = a.CustomerName;
    form.querySelector("[name='date']").value = date || '';
    form.querySelector("[name='timeSlot']").value = timeSlot || 'morning';
    form.querySelector("[name='duration']").value = a.Duration || "1 Hr";
    form.querySelector("[name='resource']").value = resource || 'Unassigned';
    window.confirmModalInstance.show();
}

function confirmScheduling(e) {
    e.preventDefault();
    const form = new FormData(e.target);
    const id = form.get("id");
    const appointment = appointments.find(a => a.AppoinmentId === id);
    if (!appointment) return;

    const newDate = form.get("date");
    const newTimeSlot = form.get("timeSlot");
    const newResource = form.get("resource");
    const newDuration = form.get("duration") || "1 Hr";

    if (hasConflict(appointment, newTimeSlot, newResource, newDate, id)) {
        showAlert({
            icon: 'error',
            title: 'Scheduling Conflict',
            text: 'A scheduling conflict was detected!',
            confirmButtonText: 'OK',
            customClass: {
                popup: 'swal-custom-popup',
                title: 'swal-custom-title',
                content: 'swal-custom-content',
                confirmButton: 'swal-custom-button'
            }
        });
        return;
    }

    appointment.RequestDate = newDate;
    appointment.TimeSlot = newTimeSlot;
    appointment.ResourceName = newResource;
    appointment.Duration = newDuration;
    saveAppointments();
    updateAllViews();
    window.confirmModalInstance.hide();
}

function deleteAppointment() {
    showAlert({
        icon: 'warning',
        title: 'Confirm Delete',
        text: 'Are you sure you want to delete this appointment?',
        showCancelButton: true,
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel',
        customClass: {
            popup: 'swal-custom-popup',
            title: 'swal-custom-title',
            content: 'swal-custom-content',
            confirmButton: 'swal-custom-button',
            cancelButton: 'swal-custom-cancel-button'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            appointments = appointments.filter(a => a.AppoinmentId !== currentEditId.toString());
            saveAppointments();
            updateAllViews();
            window.editModalInstance.hide();
        }
    });
}

const statesData = {
    USA: ["Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"],
    Canada: ["Alberta", "British Columbia", "Manitoba", "New Brunswick", "Newfoundland and Labrador", "Nova Scotia", "Northwest Territories", "Nunavut", "Ontario", "Prince Edward Island", "Quebec", "Saskatchewan", "Yukon"]
};

function updateAppointmentStateOptions(country, selectedState) {
    const stateDropdown = $('#site_state');
    stateDropdown.empty();
    const options = statesData[country] || [];
    options.forEach(state => {
        stateDropdown.append(new Option(state, state));
    });
    if (selectedState) {
        stateDropdown.val(selectedState);
    }
}

function updateAppointmentZipLabel(country) {
    $('#site_zip_label').text(country === 'Canada' ? 'Postal Code' : 'Zip Code');
}


$(document).on('change', '#site_country', function () {
    const selectedCountry = $(this).val();
    updateAppointmentStateOptions(selectedCountry);
    updateAppointmentZipLabel(selectedCountry);
});


function populateSiteSelector(appointment) {
    const container = $('#siteSelectionContainer');
    container.html('<p class="form-control-plaintext text-muted">Loading sites...</p>');

    if (!appointment || !appointment.CustomerID) {
        container.html('<p class="form-control-plaintext text-danger">No customer assigned.</p>');
        handleSiteChange(null, appointment);
        return;
    }

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetSitesForCustomer",
        data: JSON.stringify({ customerId: appointment.CustomerID }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const sites = response.d;
            if (sites && sites.length > 0) {
                let dropdownHtml = '<select id="siteSelector" class="form-select">';
                dropdownHtml += '<option value="0">-- No Site Selected (Use Customer Address) --</option>';
                sites.forEach(site => {
                    const isSelected = appointment.SiteId && parseInt(appointment.SiteId, 10) === site.Id;
                    dropdownHtml += `<option 
                                        value="${site.Id}" 
                                        data-address="${escapeHTML(site.Address)}"
                                        data-state="${escapeHTML(site.State)}"
                                        data-zip="${escapeHTML(site.Zip)}"
                                        data-country="${escapeHTML(site.Country)}"
                                        ${isSelected ? 'selected' : ''}>
                                        ${escapeHTML(site.SiteName)}
                                    </option>`;
                });
                dropdownHtml += '</select>';
                container.html(dropdownHtml);

                const selector = document.getElementById('siteSelector');
                $(selector).on('change', function () { handleSiteChange(this, appointment); });
                handleSiteChange(selector, appointment);
            } else {
                container.html('<p class="form-control-plaintext">No sites found.</p>');
                handleSiteChange(null, appointment);
            }
        },
        error: function (xhr) {
            container.html('<p class="form-control-plaintext text-danger">Failed to load sites.</p>');
        }
    });
}



function handleSiteChange(selectElement, appointment) {
    const selectedOption = selectElement ? selectElement.options[selectElement.selectedIndex] : null;

    let address, state, zip, country;

    if (selectedOption && selectElement.value !== "0") {
        address = selectedOption.getAttribute('data-address') || '';
        state = selectedOption.getAttribute('data-state') || '';
        zip = selectedOption.getAttribute('data-zip') || '';
        country = selectedOption.getAttribute('data-country') || 'USA';
    }

    else if (appointment) {
        address = appointment.Address1 || '';
        state = appointment.State || '';
        zip = appointment.ZipCode || '';
        country = appointment.Country || 'USA';
    }

    $('#site_address').val(address || '');
    $('#site_country').val(country || 'USA');
    $('#site_zip').val(zip || '');


    updateAppointmentStateOptions(country || 'USA', state || '');
    updateAppointmentZipLabel(country || 'USA');
}


function escapeHTML(str) {
    return String(str ?? '').replace(/[&<>"']/g, s => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[s]));
}

function unscheduleAppointment() {
    const appointment = appointments.find(a => a.AppoinmentId === currentEditId.toString());
    if (!appointment) return;
    appointment.RequestDate = null;
    appointment.TimeSlot = null;
    appointment.Duration = "1 Hr";
    saveAppointments();
    updateAllViews();
    window.editModalInstance.hide();
}

function updateAllViews() {
    if (currentView === "date") {
        renderDateView($("#dayDatePicker").val());
    } else if (currentView === "resource") {
        renderResourceView(GlobalDateSync._currentDate);
    } else if (currentView === "list") {
        if (window.ListViewManager && typeof window.ListViewManager.render === 'function') {
            window.ListViewManager.render();
        }
    } else if (currentView === "map") {
        renderMapView();
    }
}

const getAppointmentStatusIcon = (status) => {
    if (!status) return '<i class="fas fa-question-circle"></i>';

    const lowerStatus = status.toLowerCase();
    switch (lowerStatus) {
        case 'pending':
            return '<i class="fas fa-hourglass-half" title="Pending"></i>'; // Pending icon
        case 'confirmed':
            return '<i class="fas fa-calendar-check" title="Confirmed"></i>'; // Confirmed icon
        case 'dispatched':
            return '<i class="fas fa-truck-fast" title="Dispatched"></i>'; // Dispatched icon
        case 'in-route':
            return '<i class="fas fa-route" title="In-Route"></i>'; // In-Route icon
        case 'fa-id sent':
            return '<i class="fas fa-paper-plane" title="FA-ID Sent"></i>'; // FA-ID Sent icon
        case 'arrived':
            return '<i class="fas fa-location-dot" title="Arrived"></i>'; // Arrived icon
        case 'completed':
            return '<i class="fas fa-check-circle" title="Completed"></i>'; // Completed icon
        case 'closed':
            return '<i class="fas fa-lock" title="Closed"></i>'; // Closed icon
        case 'on-hold':
            return '<i class="fas fa-pause-circle" title="On-Hold"></i>'; // On-Hold icon
        case 'cancelled':
            return '<i class="fas fa-circle-xmark" title="Cancelled"></i>'; // Cancelled icon
        default:
            return '<i class="fas fa-question-circle" title="Undefined Status"></i>'; // Default icon
    }
};

const getTicketStatusIcon = (ticketStatus) => {
    if (ticketStatus == null)
        return '<i class="fas fa-info-circle" title="Unknown Ticket Status"></i>';

    const v = String(ticketStatus).trim().toLowerCase();

    switch (v) {
        case '1':
        case 'on hold':
            return '<i class="fa-solid fa-pause"> title="On Hold"></i>';

        case '2':
        case 'parts on order':
            return '<i class="fas fa-box-open" title="Parts on Order"></i>';

        case '3':
        case 'installation in progress':
            return '<i class="fas fa-tools" title="Installation in Progress"></i>';

        case '4':
        case 'completed':
            return '<i class="fas fa-clipboard-check" title="Completed"></i>';

        case 'pending':
            return '<i class="fas fa-clock" title="Pending"></i>';

        case 'confirmed':
            return '<i class="fas fa-calendar-alt" title="Confirmed"></i>'; // Unique icon

        default:
            return '<i class="fas fa-info-circle" title="Unknown Ticket Status"></i>';
    }
};



function formatLocalDate(d) {
    return d.getFullYear() +
        '-' + String(d.getMonth() + 1).padStart(2, '0') +
        '-' + String(d.getDate()).padStart(2, '0');
}

function renderResourceView(date) {
    $('#resourceLoading').show();
    $("#resourceViewContainer").css('display', 'block');
    $("#resourceViewContainer").html('<div id="resourceLoading" class="loading-overlay"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>');

    const container = $("#resourceViewContainer");
    
    // Ensure date is properly parsed to avoid timezone issues (same as renderDateView)
    let dateStr;
    let currentDateForResource;
    if (typeof date === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(date)) {
        // Already in YYYY-MM-DD format, use it directly
        dateStr = date;
        // Parse to Date object for calculations, using local time to avoid timezone shift
        const parts = date.split('-').map(part => parseInt(part, 10));
        currentDateForResource = new Date(parts[0], parts[1] - 1, parts[2]);
    } else {
        // Date object or other format, convert properly
        const d = date instanceof Date ? date : new Date(date);
        if (isNaN(d.getTime())) {
            // Fallback to today if invalid
            currentDateForResource = new Date();
            // Use local date components to avoid timezone issues
            const year = currentDateForResource.getFullYear();
            const month = String(currentDateForResource.getMonth() + 1).padStart(2, '0');
            const day = String(currentDateForResource.getDate()).padStart(2, '0');
            dateStr = `${year}-${month}-${day}`;
        } else {
            currentDateForResource = d;
            // Use local date components to avoid timezone issues
            const year = currentDateForResource.getFullYear();
            const month = String(currentDateForResource.getMonth() + 1).padStart(2, '0');
            const day = String(currentDateForResource.getDate()).padStart(2, '0');
            dateStr = `${year}-${month}-${day}`;
        }
    }

    renderDateNav("resourceNav", dateStr);
    
    // New filter logic
    const selectedService = $("#MainContent_ServiceTypeFilter_ResourceView").val();
    const isAllServices = selectedService === 'all' || !selectedService;

    const selectedStatus = $("#MainContent_StatusTypeFilter_ResourceView").val();
    const isAllStatuses = selectedStatus === 'all' || !selectedStatus;

    const selectedTicketStatus = $("#MainContent_TicketStatusFilter_ResourceView").val();
    const isAllTicketStatuses = selectedTicketStatus === 'all' || !selectedTicketStatus;

    const selectedGroup = $("#dispatchGroupResourceView").val();
    const selectedIndividualResource = $("#individualResourceFilterResourceView").val();
    const view = $("#viewSelect").val();
    $("#dayDatePicker").toggleClass('d-none', view === 'custom');
    
    // Filter resources based on group and individual selection
    let filteredResources = resources;
    if (selectedIndividualResource && selectedIndividualResource !== 'all') {
        filteredResources = resources.filter(r => String(r.Id) === String(selectedIndividualResource));
    } else if (selectedGroup && selectedGroup !== 'all') {
        const groupMembers = technicianGroups[selectedGroup] || [];
        filteredResources = resources.filter(r => groupMembers.includes(r.ResourceName));
    }
    const slotDurationMinutes = 30;
    const pixelsPerSlot = 100;
    const eventHeight = 35;
    let appointmentWidth = 150; // default for day/week/3-day
    if (view === 'month') {
        appointmentWidth = 80; // shrink appointment blocks specifically for month view
    }

    const paginationControls = document.querySelector('#resourceView .pagination-controls');
    if (paginationControls) {
        paginationControls.style.display = 'flex';
    }


    let dates = [dateStr];
    let fromDate, toDate;
    if (view === 'week') {
        // Use currentDateForResource to avoid timezone issues
        const startDate = new Date(currentDateForResource);
        fromDate = formatDateToISO(startDate);
        const endDate = new Date(startDate);
        endDate.setDate(startDate.getDate() + 6);
        toDate = formatDateToISO(endDate);
        dates = Array.from({ length: 7 }, (_, i) => {
            const d = new Date(startDate);
            d.setDate(startDate.getDate() + i);
            return formatDateToISO(d);
        });
    } else if (view === 'threeDay') {
        // Use currentDateForResource to avoid timezone issues
        const startDate = new Date(currentDateForResource);
        fromDate = formatDateToISO(startDate);
        const endDate = new Date(startDate);
        endDate.setDate(startDate.getDate() + 2);
        toDate = formatDateToISO(endDate);
        dates = Array.from({ length: 3 }, (_, i) => {
            const d = new Date(startDate);
            d.setDate(startDate.getDate() + i);
            return formatDateToISO(d);
        });
    } else if (view === 'custom') {
        const fromCustom = $("#datePickerFrom").val();
        const toCustom = $("#datePickerTo").val();
        if (fromCustom && toCustom) {
            fromDate = fromCustom;
            toDate = toCustom;
            resourceCustomDateRange.from = fromCustom;
            resourceCustomDateRange.to = toCustom;
            const startDate = new Date(fromDate + 'T00:00:00');
            const endDate = new Date(toDate + 'T00:00:00');
            if (startDate <= endDate) {
                dates = [];
                for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
                    dates.push(formatDateToISO(d));
                }
            }
        } else {
            fromDate = dateStr;
            toDate = dateStr;
        }
    } else if (view === 'month') {
        // Use currentDateForResource to avoid timezone issues
        const firstDay = new Date(currentDateForResource.getFullYear(), currentDateForResource.getMonth(), 1);
        const lastDay = new Date(currentDateForResource.getFullYear(), currentDateForResource.getMonth() + 1, 0);
        fromDate = formatDateToISO(firstDay);
        toDate = formatDateToISO(lastDay);
        dates = [];
        for (let d = new Date(firstDay); d <= lastDay; d.setDate(d.getDate() + 1)) {
            dates.push(formatDateToISO(d));
        }
    } else {
        fromDate = dateStr;
        toDate = dateStr;
    }

    const toDateEnd = toDate + " 23:59:59";

    getAppoinments("", fromDate, toDateEnd, view === 'day' ? dateStr : "", function (appointments) {

        $('#resourceLoading').hide();

        const filteredAppointments = appointments.filter(a => {
            const serviceMatch = isAllServices || (a.ServiceTypeID != null && String(a.ServiceTypeID) == selectedService);
            const statusMatch = isAllStatuses || (a.AppoinmentStatusID != null && String(a.AppoinmentStatusID) == selectedStatus);
            const ticketStatusMatch = isAllTicketStatuses || (a.TicketStatusID != null && String(a.TicketStatusID) == selectedTicketStatus);
            return serviceMatch && statusMatch && ticketStatusMatch;
        });

        // Start of dynamic time slot extension
        let viewTimeSlots = JSON.parse(JSON.stringify(allTimeSlots)); // Deep copy
    
        if (viewTimeSlots.length > 0) {
            let maxTimeMinutes = 0;
    
            const lastOriginalSlot = viewTimeSlots[viewTimeSlots.length - 1];
            if (lastOriginalSlot && lastOriginalSlot.TimeBlockSchedule) {
                const lastOriginalSlotEndTimeStr = lastOriginalSlot.TimeBlockSchedule.split('-')[1];
                if (lastOriginalSlotEndTimeStr) {
                    maxTimeMinutes = parseTimeToMinutes(lastOriginalSlotEndTimeStr.trim());
                }
            }
    
            const resourceNames = filteredResources.map(r => r.ResourceName);
            const visibleAppointments = filteredAppointments.filter(a => resourceNames.includes(a.ResourceName));

            visibleAppointments.forEach(a => {
                if (a.TimeSlot && a.Duration) {
                    let startTimeMinutes;
                    if (a.StartDateTime) {
                        const startDt = new Date(a.StartDateTime);
                        if (!isNaN(startDt)) {
                           startTimeMinutes = startDt.getHours() * 60 + startDt.getMinutes();
                        }
                    } else {
                        const timeSlotInfo = allTimeSlots.find(slot => slot.TimeBlockSchedule === a.TimeSlot || (slot.TimeBlock && slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase()));
                        if (timeSlotInfo && timeSlotInfo.TimeBlockSchedule) {
                            const startTimeStr = timeSlotInfo.TimeBlockSchedule.split('-')[0].trim();
                            startTimeMinutes = parseTimeToMinutes(startTimeStr);
                        }
                    }
    
                    if (startTimeMinutes !== undefined) {
                        const durationMinutes = parseDuration(a.Duration);
                        if (!isNaN(durationMinutes)) {
                            const endTimeMinutes = startTimeMinutes + durationMinutes;
                            if (endTimeMinutes > maxTimeMinutes) {
                                maxTimeMinutes = endTimeMinutes;
                            }
                        }
                    }
                }
            });
    
            function formatMinutesToTime(minutes) {
                let h = Math.floor(minutes / 60) % 24;
                let m = minutes % 60;
                const ampm = h >= 12 ? 'PM' : 'AM';
                h = h % 12;
                h = h ? h : 12; // the hour '0' should be '12'
                let m_str = m.toString().padStart(2, '0');
                return `${h}:${m_str} ${ampm}`;
            }
    
            let lastSlot = viewTimeSlots.length > 0 ? viewTimeSlots[viewTimeSlots.length - 1] : null;
            let lastSlotEndTimeMinutes = 0;
            if(lastSlot && lastSlot.TimeBlockSchedule) {
                const lastSlotEndTimeStr = lastSlot.TimeBlockSchedule.split('-')[1];
                if (lastSlotEndTimeStr) {
                    lastSlotEndTimeMinutes = parseTimeToMinutes(lastSlotEndTimeStr.trim());
                }
            }
    
            while (lastSlotEndTimeMinutes < maxTimeMinutes) {
                const newSlotStartMinutes = lastSlotEndTimeMinutes;
                const newSlotEndMinutes = newSlotStartMinutes + 30;
    
                const newSlot = {
                    TimeBlock: `Custom ${viewTimeSlots.length + 1}`,
                    TimeBlockSchedule: `${formatMinutesToTime(newSlotStartMinutes)} - ${formatMinutesToTime(newSlotEndMinutes)}`
                };
                viewTimeSlots.push(newSlot);
                lastSlotEndTimeMinutes = newSlotEndMinutes;
            }
        }
        // End of dynamic time slot extension

        const validTimeSlots = viewTimeSlots.filter(slot =>
            slot && slot.TimeBlockSchedule && !viewTimeSlots.some(other => other !== slot && other.TimeBlockSchedule === slot.TimeBlockSchedule)
        );

        // renderDateNav already called above, don't call again to avoid duplicate rendering

        let html = `
    <div class="border rounded overflow-hidden resizable-container" style="margin: 0; padding: 0; min-width: 100%;">
`;


        if (view === 'day') {
            html += `
                <div class="calendar-grid calendar-header" id="resource-header" style="grid-template-columns: 120px repeat(${validTimeSlots.length}, ${pixelsPerSlot}px);">
                    <div class="p-2 border-right bg-gray-50 calendar-header-cell"></div>
                    ${validTimeSlots.map(time => `
                        <div class="p-2 text-center font-weight-medium border-right last-border-right-none bg-gray-50 calendar-header-cell">
                            ${formatTimeRange(time.TimeBlockSchedule)}
                        </div>
                    `).join('')}
                </div>
            `;
        } else {
            html += `
                <div class="calendar-grid calendar-header" id="resource-header" style="grid-template-columns: 120px repeat(${dates.length}, minmax(80px, 1fr));">
                    <div class="p-2 border-right bg-gray-50 calendar-header-cell"></div>
                    ${dates.map(day => {
                        // Parse day string to Date object for weekday, use formatDateToUSA for date display
                        let dayDate = new Date(day + 'T00:00:00');
                        if (isNaN(dayDate.getTime())) {
                            const parts = day.split('-').map(part => parseInt(part, 10));
                            dayDate = new Date(parts[0], parts[1] - 1, parts[2]);
                        }
                        const weekday = dayDate.toLocaleDateString('en-US', { weekday: 'short' });
                        const formattedDate = formatDateToUSA(day);
                        return `
                        <div class="p-2 text-center font-weight-medium border-right last-border-right-none bg-gray-50 calendar-header-cell">
                            <div>${weekday}</div>
                            <div>${formattedDate}</div>
                        </div>
                    `;
                    }).join('')}
                </div>
            `;
        }

        html += `
    <div class="calendar-body" style="margin: 0; padding: 0;">
`;

        if (!validTimeSlots.length || !resources.length) {
            html += `
                <div class="text-center py-4 text-muted">
                    No resources or time slots available.
                </div>
            `;
        } else {
            filteredResources.forEach((resource, index) => {
                const rowId = `resource-row-${index}`;
                const resourceIcon = '<i class="fas fa-user"></i>';

                if (view === 'day') {
                    html += `
                        <div class="calendar-grid resource-row" id="${rowId}" style="grid-template-columns: 120px repeat(${validTimeSlots.length}, ${pixelsPerSlot}px); margin: 0; padding: 0; max-width: 100%; overflow: hidden; position: relative;">
                            <div class="h-${eventHeight}px border-bottom last-border-bottom-none p-1 fs-7 text-left bg-gray-50 calendar-time-cell resource-name" style="position: sticky; left: 0; z-index: 1; padding: 7px 10px !important;">
                                ${resourceIcon} ${resource.ResourceName}
                            </div>
                    `;

                    const placedAppointments = [];
                    validTimeSlots.forEach((time, timeIndex) => {
                        const cellAppointments = filteredAppointments
                            .filter(a => {
                                // Check resource and date match
                                if (a.ResourceName !== resource.ResourceName) return false;
                                
                                // Check if appointment is on this date
                                const apptDate = a.RequestDate || (a.StartDateTime ? a.StartDateTime.split(' ')[0] : null);
                                if (apptDate !== dateStr) return false;
                                
                                // If appointment has StartDateTime/EndDateTime, use those for more accurate positioning
                                if (a.StartDateTime && a.EndDateTime) {
                                    const startDt = new Date(a.StartDateTime);
                                    const endDt = new Date(a.EndDateTime);
                                    const slotStart = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[0]);
                                    const slotEnd = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[1]);
                                    const apptStartMinutes = startDt.getHours() * 60 + startDt.getMinutes();
                                    const apptEndMinutes = endDt.getHours() * 60 + endDt.getMinutes();
                                    
                                    // Show appointment if it overlaps with this time slot
                                    return (apptStartMinutes < slotEnd && apptEndMinutes > slotStart);
                                }
                                
                                // Fallback to TimeSlot matching
                                return a.TimeSlot;
                            })
                            .map(a => {
                                let durationMinutes, startTimeMinutes;
                                
                                // Use StartDateTime/EndDateTime if available for more accurate positioning
                                if (a.StartDateTime && a.EndDateTime) {
                                    const start = new Date(a.StartDateTime);
                                    const end = new Date(a.EndDateTime);
                                    
                                    if (!isNaN(start) && !isNaN(end)) {
                                        durationMinutes = (end - start) / (1000 * 60);
                                        startTimeMinutes = start.getHours() * 60 + start.getMinutes();
                                    } else {
                                        durationMinutes = parseDuration(a.Duration);
                                        const timeSlot = validTimeSlots.find(slot =>
                                            slot.TimeBlockSchedule === a.TimeSlot ||
                                            slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase()
                                        );
                                        if (!timeSlot) return null;
                                        startTimeMinutes = parseTimeToMinutes(timeSlot.TimeBlockSchedule.split('-')[0]);
                                    }
                                } else {
                                    const timeSlot = validTimeSlots.find(slot =>
                                        slot.TimeBlockSchedule === a.TimeSlot ||
                                        slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase()
                                    );
                                    if (!timeSlot) {
                                        console.warn(`No matching time slot for appointment ${a.AppoinmentId}: TimeSlot=${a.TimeSlot}`);
                                        return null;
                                    }
                                    durationMinutes = parseDuration(a.Duration);
                                    startTimeMinutes = parseTimeToMinutes(timeSlot.TimeBlockSchedule.split('-')[0]);
                                }
                                
                                // Only render in the starting slot to avoid duplicates
                                const startIndex = validTimeSlots.findIndex(slot => {
                                    const slotStart = parseTimeToMinutes(slot.TimeBlockSchedule.split('-')[0]);
                                    return Math.abs(slotStart - startTimeMinutes) < 30; // Within 30 minutes
                                });
                                
                                if (startIndex === timeIndex) {
                                    const totalHours = durationMinutes / 60;
                                    const slotStartTimeMinutes = parseTimeToMinutes(time.TimeBlockSchedule.split('-')[0]);
                                    const offsetMinutes = startTimeMinutes - slotStartTimeMinutes;
                                    const offsetPx = Math.max(0, (offsetMinutes / slotDurationMinutes) * pixelsPerSlot);
                                    const widthPx = (totalHours * (pixelsPerSlot * 2));

                                    // Allow overlapping appointments by stacking them
                                    const overlappingAppointments = placedAppointments.filter(pa =>
                                        (pa.offsetPx < offsetPx + widthPx && pa.offsetPx + pa.widthPx > offsetPx) ||
                                        (offsetPx < pa.offsetPx + pa.widthPx && offsetPx + widthPx > pa.offsetPx)
                                    );
                                    const conflictIndex = overlappingAppointments.length;
                                    const adjustedOffsetPx = offsetPx + (conflictIndex * 5); // Smaller offset for better overlap handling

                                    placedAppointments.push({ appointment: a, offsetPx: adjustedOffsetPx, widthPx });

                                    return { appointment: a, offsetPx: adjustedOffsetPx, widthPx };
                                }
                                return null;
                            })
                            .filter(a => a);

                        html += `
                            <div class="h-${eventHeight}px border-bottom last-border-bottom-none border-right last-border-right-none p-1 relative drop-target calendar-cell"
                                 data-date="${dateStr}" 
                                 data-time="${time.TimeBlockSchedule}" 
                                 data-resource="${resource.ResourceName}"
                                 style="position: relative; margin: 0; padding: 0; max-width: ${pixelsPerSlot}px;">
                                ${cellAppointments.map(({ appointment, offsetPx, widthPx }) => {
                            const statusIcon = getAppointmentStatusIcon(appointment.AppoinmentStatus);
                            const ticketStatusIcon = getTicketStatusIcon(appointment.TicketStatus);
                            return `
                                        <div class="calendar-event-resource ${getEventTimeSlotClass(appointment)}"
                                             style="left:0px; 
                                                    width: ${Math.min(widthPx, pixelsPerSlot * validTimeSlots.length - offsetPx)}px; 
                                                    height: ${eventHeight}px; 
                                                    position: absolute;"
                                             data-id="${appointment.AppoinmentId}" 
                                             draggable="true">
                                            <div class="event-content" style="overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                                                ${statusIcon} ${ticketStatusIcon} ${appointment.CustomerName} (${appointment.ServiceType})
                                            </div>
                                        </div>
                                    `;
                        }).join('')}
                            </div>
                        `;
                    });
                    html += `</div>`;
                } else {
                    html += `
                        <div class="calendar-grid resource-row" id="${rowId}" style="grid-template-columns: 120px repeat(${dates.length}, minmax(80px, 1fr)); margin: 0; padding: 0; max-width: 100%; overflow: hidden; position: relative;">
                            <div class="h-${eventHeight}px border-bottom last-border-bottom-none p-1 fs-7 text-left bg-gray-50 calendar-time-cell resource-name" style="position: sticky; left: 0; z-index: 1; padding: 7px 10px !important;">
                                ${resourceIcon} ${resource.ResourceName}
                            </div>
                    `;

                    dates.forEach((day, dayIndex) => {
                        const cellAppointments = filteredAppointments
                            .filter(a => a.ResourceName === resource.ResourceName &&
                                a.RequestDate === day &&
                                a.TimeSlot)
                            .sort((a, b) => {
                                const aTime = parseTimeToMinutes(a.TimeSlot.split('-')[0]);
                                const bTime = parseTimeToMinutes(b.TimeSlot.split('-')[0]);
                                return aTime - bTime;
                            })
                            .map((a, idx) => {
                                const timeSlot = validTimeSlots.find(slot =>
                                    slot.TimeBlockSchedule === a.TimeSlot ||
                                    slot.TimeBlock.toLowerCase() === a.TimeSlot.toLowerCase()
                                );
                                if (!timeSlot) {
                                    console.warn(`No matching time slot for appointment ${a.AppoinmentId}: TimeSlot=${a.TimeSlot}`);
                                    return null;
                                }
                                const durationMinutes = parseDuration(a.Duration);
                                const totalHours = durationMinutes / 60;
                                const widthPx = (totalHours * (pixelsPerSlot * 2));
                                const offsetPx = idx * eventHeight;
                                return { appointment: a, offsetPx, widthPx };
                            })
                            .filter(a => a);

                        html += `
                            <div class="border-bottom last-border-bottom-none border-right last-border-right-none p-1 relative drop-target calendar-cell"
                                 data-date="${day}" 
                                 data-resource="${resource.ResourceName}"
                                 style="position: relative; margin: 0; padding: 0; min-height: ${cellAppointments.length * eventHeight}px;">
                                ${cellAppointments.map(({ appointment, offsetPx, widthPx }) => {
                            const statusIcon = getAppointmentStatusIcon(appointment.AppoinmentStatus);
                            const ticketStatusIcon = getTicketStatusIcon(appointment.TicketStatus);
                            return `
                                        <div class="calendar-event-resource ${getEventTimeSlotClass(appointment)}"
                                             style="top: ${offsetPx}px; 
                                                    left: 0px; 
                                                    width: ${Math.min(widthPx, appointmentWidth)}px; 
                                                    height: ${eventHeight}px; 
                                                    position: absolute;"
                                             data-id="${appointment.AppoinmentId}" 
                                             draggable="true">
                                            <div class="event-content" style="overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                                                ${statusIcon} ${ticketStatusIcon} ${appointment.CustomerName} (${appointment.ServiceType})
                                            </div>
                                        </div>
                                    `;
                        }).join('')}
                            </div>
                        `;
                    });
                    html += `</div>`;
                }
            });
        }

        html += `</div></div>`;
        container.html(html);
        setTimeout(() => {
            const $viewContainer = $('#resourceViewContainer');
            const $innerContainer = $viewContainer.find('.resizable-container');

            // Calculate needed width for scrolling
            const viewportWidth = $(window).width();
            const calendarWidth = $innerContainer.width();

            // Set minimum width if content is smaller than viewport
            if (calendarWidth < viewportWidth) {
                // Calculate width needed: resource column (120px) + time slots
                const resourceColumnWidth = 120;
                const timeSlotWidth = 100; 
                const timeSlotCount = validTimeSlots ? validTimeSlots.length : 24;
                const totalWidth = resourceColumnWidth + (timeSlotCount * timeSlotWidth);

                $innerContainer.css('min-width', Math.max(totalWidth, viewportWidth + 100) + 'px');
            }


            $viewContainer.on('mousedown touchstart', function (e) {
     
                if ($(e.target).closest('.calendar-event, .calendar-event-resource, button, a, .resource-name, .calendar-header-cell:first-child').length) {
                    return;
                }

                const $this = $(this);
                const startX = e.pageX || (e.originalEvent.touches ? e.originalEvent.touches[0].pageX : 0);
                const startScrollLeft = $this.scrollLeft();

                $this.addClass('dragging');

                function onMove(moveEvent) {
                    if (!$this.hasClass('dragging')) return;

                    const currentX = moveEvent.pageX || (moveEvent.originalEvent.touches ? moveEvent.originalEvent.touches[0].pageX : 0);
                    const deltaX = startX - currentX;
                    const newScrollLeft = startScrollLeft + deltaX;

                    $this.scrollLeft(Math.max(0, newScrollLeft));
                }

                function onEnd() {
                    $this.removeClass('dragging');
                    $(document).off('mousemove touchmove', onMove);
                    $(document).off('mouseup touchend', onEnd);
                }

                $(document).on('mousemove touchmove', onMove);
                $(document).on('mouseup touchend', onEnd);

                return false; 
            });

            $viewContainer.on('selectstart', function (e) {
                if ($(this).hasClass('dragging')) {
                    e.preventDefault();
                    return false;
                }
            });

        }, 100);
        const resizableContainer = container.find('.resizable-container')[0];
        let isResizing = false;
        let startX, startWidth;

        resizableContainer.addEventListener('mousedown', (e) => {
            if (e.offsetX > resizableContainer.offsetWidth - 10) {
                isResizing = true;
                startX = e.pageX;
                startWidth = resizableContainer.offsetWidth;
                resizableContainer.style.cursor = 'ew-resize';
            }
        });

        document.addEventListener('mousemove', (e) => {
            if (isResizing) {
                const width = startWidth + (e.pageX - startX);
                resizableContainer.style.width = `${Math.max(300, width)}px`;
            }
        });

        document.addEventListener('mouseup', () => {
            if (isResizing) {
                isResizing = false;
                resizableContainer.style.cursor = 'default';
            }
        });

        $('#resourceLoading').hide();
        setupHoverEvents();
        setupDragAndDrop();
        updateCalendarEventColors();
        renderUnscheduledList('resource');

        resourceViewFilteredAppointments = filteredResources;
        resourceViewCurrentPage = 1;
        updateResourceViewPagination();



        hideMainLoader();
    });
}
function enableDragToScroll(containerSelector) {
    const container = $(containerSelector);
    if (!container.length) {
        console.warn(`Container ${containerSelector} not found for drag-to-scroll`);
        return;
    }

    let isDragging = false;
    let startX;
    let startScrollLeft;

    const startDragging = (e) => {
        // Only start dragging on the container itself, not on appointment events
        if (e.target.closest('.calendar-event, .calendar-event-resource, button, a, .ui-draggable')) {
            return;
        }

        isDragging = true;
        container.addClass('dragging');
        startX = e.pageX - container.offset().left;
        startScrollLeft = container.scrollLeft();
    };

    const stopDragging = () => {
        isDragging = false;
        container.removeClass('dragging');
    };

    const drag = (e) => {
        if (!isDragging) return;
        e.preventDefault();
        const x = e.pageX - container.offset().left;
        const walk = (x - startX) * 2; // Scroll speed multiplier
        container.scrollLeft(startScrollLeft - walk);
    };

    // Remove old listeners
    container.off('mousedown');
    container.off('mouseleave');
    container.off('mouseup');
    container.off('mousemove');

    // Add new listeners
    container.on('mousedown', startDragging);
    container.on('mouseleave', stopDragging);
    container.on('mouseup', stopDragging);
    container.on('mousemove', drag);

    // Also handle touch events for mobile
    container.on('touchstart', (e) => {
        if (e.target.closest('.calendar-event, .calendar-event-resource, button, a, .ui-draggable')) {
            return;
        }
        isDragging = true;
        container.addClass('dragging');
        startX = e.touches[0].pageX - container.offset().left;
        startScrollLeft = container.scrollLeft();
    });

    container.on('touchend', stopDragging);
    container.on('touchmove', (e) => {
        if (!isDragging) return;
        e.preventDefault();
        const x = e.touches[0].pageX - container.offset().left;
        const walk = (x - startX) * 2;
        container.scrollLeft(startScrollLeft - walk);
    });
}

document.addEventListener('DOMContentLoaded', () => {

    $('#page-loader').show();

    // Initialize date synchronization mechanism
    syncDatePickers(null, null); // This will load from localStorage or default to today

    Promise.all([
        getTimeSlots(),
        getResources(),
        loadServiceTypeIndicators()
    ]).then(() => {
        console.log('All initial data (Slots, Resources, Services) has been loaded.');


        $('#dayDatePicker').val(globalCurrentDate); // Set picker to the synchronized date

        window.newModalInstance = new bootstrap.Modal(document.getElementById("newModal"));
        window.editModalInstance = new bootstrap.Modal(document.getElementById("editModal"));
        window.confirmModalInstance = new bootstrap.Modal(document.getElementById("confirmModal"));

        attachAllEventListeners();

        currentView = "date";
        renderDateView(globalCurrentDate); // Render with the synchronized date

        $('#page-loader').fadeOut();

    }).catch((error) => {

        console.error("A critical error occurred during initial page load:", error);
        $('#page-loader').html('<div class="alert alert-danger">Failed to load essential application data. Please refresh the page.</div>');
    });
});



function calculateDurationInMinutes(startTime, endTime) {
    const parseTime = timeStr => {
        const [time, modifier] = timeStr.split(' ');
        let [hours, minutes] = time.split(':').map(Number);
        if (modifier === 'PM' && hours !== 12) hours += 12;
        if (modifier === 'AM' && hours === 12) hours = 0;
        return hours * 60 + minutes;
    };

    const startMinutes = parseTime(startTime);
    const endMinutes = parseTime(endTime);
    return endMinutes - startMinutes;
}

function getTimeSlots() {
    return $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetTimeSlots",
        data: {},
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).then(function (response) {
        console.log('TimeSlots fetched:', response.d);
        allTimeSlots = response.d;
        renderTimeSlots(response.d);
        populateTimeSlotDropdown(response.d);
    }).catch(function (xhr, status, error) {
        console.error("Error fetching time slots: ", error);
        throw error;
    });
}

function renderTimeSlots(timeSlots) {
    const container = $(".time-slot-indicators");
    container.empty();
    timeSlots.forEach(slot => {
        const fullLabel = slot.TimeBlock;
        const match = fullLabel.match(/^(\w+)/);
        const timeBlockClass = match ? match[1].toLowerCase() : "default";
        const className = `time-block-${timeBlockClass}`;
        const html = `<span class="time-block-indicator ${className}"></span>${slot.TimeBlockSchedule} `;
        container.append(html);
    });
}

function getResources() {
    return $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetResourcess",
        data: {},
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).then(function (response) {
        console.log('Resources fetched:', response.d);
        resources = response.d;
        populateResourceDropdown(response.d);
    }).catch(function (xhr, status, error) {
        console.error("Error fetching resources: ", error);
        throw error;
    });
}

function formatTimeRange(str) {
    if (!str) return 'N/A';
    return str.replace(/[()]/g, '')
        .trim()
        .replace(/\s{2,}/g, ' ')
        .replace(/\s*(AM|PM)\s*/gi, '')
        .trim();
}

function populateResourceDropdown(resources) {
    const $dropdown = $("#resource_list");
    $dropdown.empty();
    $dropdown.append(`<option value="0">Unassigned</option>`);
    resources.forEach(resource => {
        $dropdown.append(`<option value="${resource.Id}">${resource.ResourceName}</option>`);
    });
    
    // Also populate individual resource filters
    const $dateViewFilter = $("#individualResourceFilterDateView");
    const $resourceViewFilter = $("#individualResourceFilterResourceView");
    const $listViewFilter = $("#individualResourceFilterListView");
    
    if ($dateViewFilter.length) {
        $dateViewFilter.empty();
        $dateViewFilter.append(`<option value="all">All Resources</option>`);
        resources.forEach(resource => {
            $dateViewFilter.append(`<option value="${resource.Id}">${resource.ResourceName}</option>`);
        });
    }
    
    if ($resourceViewFilter.length) {
        $resourceViewFilter.empty();
        $resourceViewFilter.append(`<option value="all">All Resources</option>`);
        resources.forEach(resource => {
            $resourceViewFilter.append(`<option value="${resource.Id}">${resource.ResourceName}</option>`);
        });
    }

    if ($listViewFilter.length) {
        $listViewFilter.empty();
        $listViewFilter.append(`<option value="all">All Resources</option>`);
        resources.forEach(resource => {
            $listViewFilter.append(`<option value="${resource.Id}">${resource.ResourceName}</option>`);
        });
    }
}

function getSelectedId(select, targetName) {
    let matched = false;
    for (const option of select.options) {
        if (option.text.trim() === targetName.trim()) {
            select.value = option.value;
            matched = true;
            break;
        }
    }
    if (!matched && select.options.length > 0) {
        select.selectedIndex = 0;
    }
}

function populateTimeSlotDropdown(slots) {
    const $dropdown = $("#time_slot");
    $dropdown.empty();
    slots.forEach(slot => {
        $dropdown.append(`<option value="${slot.TimeBlock}">${slot.TimeBlockSchedule}</option>`);
    });
}

function calculateTimeRequired() {
    const modal = document.getElementById('editModal');
    const startDateInput = modal.querySelector('#txt_StartDate');
    const endDateInput = modal.querySelector('#txt_EndDate');
    const durationInput = modal.querySelector('#duration');
    const errorMsg = modal.querySelector('#customer_EndDate');

    const start = moment(startDateInput.value, 'MM/DD/YYYY hh:mm A');
    const end = moment(endDateInput.value, 'MM/DD/YYYY hh:mm A');

    if (!start.isValid() || !end.isValid()) {
        durationInput.value = '';
        return;
    }

    if (end.isBefore(start)) {
        errorMsg.style.display = 'block';
        endDateInput.style.borderColor = 'red';
        durationInput.value = 'Invalid';
        return;
    }

    errorMsg.style.display = 'none';
    endDateInput.style.borderColor = '';

    const diff = moment.duration(end.diff(start));
    const hours = Math.floor(diff.asHours());
    const minutes = diff.minutes();

    durationInput.value = `${hours} Hr : ${minutes} Min`;
}

function updateEndDateFromDuration() {
    const modal = document.getElementById('editModal');
    const startDateInput = modal.querySelector('#txt_StartDate');
    const endDateInput = modal.querySelector('#txt_EndDate');
    const durationInput = modal.querySelector('#duration');

    const start = moment(startDateInput.value, 'MM/DD/YYYY hh:mm A');
    if (!start.isValid()) return;

    const durationStr = durationInput.value;
    const hourMatch = durationStr.match(/(\d+)\s*Hr/i);
    const minuteMatch = durationStr.match(/(\d+)\s*Min/i);

    const hours = hourMatch ? parseInt(hourMatch[1], 10) : 0;
    const minutes = minuteMatch ? parseInt(minuteMatch[1], 10) : 0;

    if (isNaN(hours) && isNaN(minutes)) return;

    const newEnd = start.clone().add(hours, 'hours').add(minutes, 'minutes');
    endDateInput.value = newEnd.format('MM/DD/YYYY hh:mm A');

    calculateTimeRequired(); // Re-validate after updating
}

function populateStatusDropdown() {
    const statusOptions = [
        { value: 'Pending', text: 'Pending' },
        { value: 'Scheduled', text: 'Confirmed' },
        { value: 'Cancelled', text: 'Cancelled' },
        { value: 'Closed', text: 'Closed' },
        { value: 'Installation In Progress', text: 'Installation In Progress' },
        { value: 'Completed', text: 'Completed' }
    ];
    const dropdowns = ['#MainContent_StatusTypeFilter_List', '#MainContent_StatusTypeFilter_Edit'];
    dropdowns.forEach(selector => {
        const $dropdown = $(selector);
        $dropdown.empty().append('<option value="">Select a Status</option>');
        statusOptions.forEach(opt => {
            $dropdown.append(`<option value="${opt.value}">${opt.text}</option>`);
        });
    });
}

function populateTicketStatusDropdown() {
    const ticketStatusOptions = [
        { value: '1', text: 'On Hold' },
        { value: '2', text: 'Parts on Order' },
        { value: '3', text: 'Installation in Progress' },
        { value: '4', text: 'Completed' },
        { value: 'Pending', text: 'Pending' }
    ];
    const dropdowns = ['#MainContent_TicketStatusFilter_List', '#MainContent_TicketStatusFilter_Edit'];
    dropdowns.forEach(selector => {
        const $dropdown = $(selector);
        $dropdown.empty().append('<option value="">Select a ticket status</option>');
        ticketStatusOptions.forEach(opt => {
            $dropdown.append(`<option value="${opt.value}">${opt.text}</option>`);
        });
    });
}
function saveAppoinmentData(e) {
    e.preventDefault();
    const form = new FormData(e.target);
    const id = form.get("AppoinmentId");


    let isValid = true;
    document.querySelectorAll('#customFieldsContainer [name^="custom_"][required]').forEach(input => {
        if (!input.value) {
            isValid = false;
            input.classList.add('is-invalid');
            if (!input.parentNode.querySelector('.invalid-feedback')) {
                const errorMsg = document.createElement('div');
                errorMsg.className = 'invalid-feedback';
                errorMsg.textContent = 'This field is required.';
                input.parentNode.appendChild(errorMsg);
            }
        } else {
            input.classList.remove('is-invalid');
            const existingError = input.parentNode.querySelector('.invalid-feedback');
            if (existingError) existingError.remove();
        }
    });

    if (!isValid) {
        showAlert({
            icon: 'error',
            title: 'Validation Error',
            text: 'Please fill in all required custom fields.',
        });
        return;
    }


    const customValues = {};
    document.querySelectorAll('#customFieldsContainer [name^="custom_"]').forEach(input => {
        const fieldId = input.name.replace('custom_', '');
        if (input.type === 'checkbox') {
            if (input.checked) {
                if (!customValues[fieldId]) customValues[fieldId] = [];
                customValues[fieldId].push(input.value);
            }
        } else {
            customValues[fieldId] = input.value;
        }
    });


    const appointment = {};
    appointment.AppoinmentId = parseInt(id);
    appointment.CustomerID = parseInt(form.get("CustomerID"));

    const siteSelector = document.getElementById('siteSelector');
    appointment.SiteId = siteSelector ? parseInt(siteSelector.value, 10) : 0;

    appointment.ServiceType = form.get("ctl00$MainContent$ServiceTypeFilter_Edit");
    appointment.RequestDate = form.get("date");
    appointment.TimeSlot = form.get("timeSlot");
    appointment.ResourceID = parseInt(form.get("resource"));
    appointment.Status = $("#MainContent_StatusTypeFilter_Edit").val();
    appointment.TicketStatus = form.get("ctl00$MainContent$TicketStatusFilter_Edit");
    appointment.Note = form.get("note");
    appointment.StartDateTime = form.get("txt_StartDate");
    appointment.EndDateTime = form.get("txt_EndDate");
    appointment.AttachedForms = selectedForms.map(form => form.id);
    appointment.CustomFieldsJson = JSON.stringify(customValues);


    $.ajax({
        type: "POST",
        url: "Appointments.aspx/UpdateAppointment",
        data: JSON.stringify({ appointment: appointment }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                const updatedAppointment = appointments.find(a => a.AppoinmentId === id);
                if (updatedAppointment) {

                    updatedAppointment.CustomFieldsJson = appointment.CustomFieldsJson;
                    updatedAppointment.AttachedForms = selectedForms.map(form => form.id);
                }
                showAlert({
                    icon: 'success',
                    title: 'Success',
                    text: 'Appointment updated successfully!',
                    timer: 2000
                });
                updateAllViews();
                window.editModalInstance.hide();
            } else {
                showAlert({
                    icon: 'error',
                    title: 'Error',
                    text: 'Something went wrong while updating the appointment.'
                });
            }
        },
        error: function (xhr, status, error) {
            console.error("Error updating appointment: ", error);
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to update appointment due to a server error.'
            });
        }
    });
}

function saveAllDataFromModal(e) {
    e.preventDefault();
    const form = document.getElementById("editForm");

    const getStateAbbreviation = (fullName) => {
        const stateEntry = Object.entries(statesData.USA.concat(statesData.Canada)).find(([abbr, name]) => name === fullName);
        return stateEntry ? stateEntry[0] : fullName;
    };

    const viewModel = {
        AppointmentData: {
            AppoinmentId: form.querySelector("#AppoinmentId").value,
            CustomerID: form.querySelector("#CustomerID").value,
            SiteId: parseInt($('#siteSelector').val()) || 0,
            ServiceType: $("#MainContent_ServiceTypeFilter_Edit").val(),
            StatusID: parseInt($("#MainContent_StatusTypeFilter_Edit").val()) || 0,
            TicketStatusID: parseInt($("#MainContent_TicketStatusFilter_Edit").val()) || 0,
            ResourceID: parseInt($("select[name='resource']").val()) || 0,
            RequestDate: $("input[name='date']").val(),
            TimeSlot: $("select[name='timeSlot']").val(),
            Note: $("textarea[name='note']").val(),
            StartDateTime: $("#txt_StartDate").val(),
            EndDateTime: $("#txt_EndDate").val()
        },
        SiteData: {
            Id: parseInt($('#siteSelector').val()) || 0,
            Address: $('#site_address').val(),
            Country: $('#site_country').val(),
            State: LocationHelper.getAbbreviation($('#site_state').val()),
            Zip: $('#site_zip').val()
        }
    };

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/UpdateAppointment",
        data: JSON.stringify({ viewModel: viewModel }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                showAlert({ icon: 'success', title: 'Success!', text: 'Appointment updated successfully.' });
                window.editModalInstance.hide();
                getAppoinments("", "", "", "", function () {
                    updateAllViews();
                });
            } else {
                showAlert({ icon: 'error', title: 'Update Failed', text: 'The server could not save the appointment data.' });
            }
        },
        error: function (xhr) {
            console.error("Error saving appointment: ", xhr.responseText);
            showAlert({ icon: 'error', title: 'Server Error', text: 'A critical error occurred while saving.' });
        }
    });
}


function extractHoursAndMinutes(duration) {
    if (!duration) {
        timerequired_Hour = 0;
        timerequired_Minute = 0;
        return;
    }
    const hourMatch = duration.match(/(\d+)\s*Hr/i);
    const minuteMatch = duration.match(/(\d+)\s*Min/i);
    timerequired_Hour = hourMatch ? parseInt(hourMatch[1], 10) : 0;
    timerequired_Minute = minuteMatch ? parseInt(minuteMatch[1], 10) : 0;
}

function calculateStartEndTime() {
    const form = document.getElementById("editForm");
    const timeSlot = form.querySelector("[name='timeSlot']").value;
    const dateValue = form.querySelector("[name='date']").value;

    if (!timeSlot || !dateValue) {
        form.querySelector("[id='txt_StartDate']").value = '';
        form.querySelector("[id='txt_EndDate']").value = '';
        return;
    }


    const dateParts = dateValue.split('-');
    const year = parseInt(dateParts[0], 10);
    const month = parseInt(dateParts[1], 10) - 1;
    const day = parseInt(dateParts[2], 10);


    const timeMatch = timeSlot.match(/(\d{1,2}:\d{2}\s*[AP]M)/);
    if (!timeMatch) {
        console.warn(`Could not extract start time from timeSlot: ${timeSlot}`);
        return;
    }
    const startTimeStr = timeMatch[0];


    const timeParts = startTimeStr.match(/(\d+):(\d+)\s*([AP]M)/);
    let hours = parseInt(timeParts[1], 10);
    const minutes = parseInt(timeParts[2], 10);
    const modifier = timeParts[3];

    if (modifier === 'PM' && hours < 12) {
        hours += 12;
    }
    if (modifier === 'AM' && hours === 12) {
        hours = 0;
    }

    const startDateTime = new Date(year, month, day, hours, minutes);

    if (isNaN(startDateTime.getTime())) {
        console.warn(`Invalid start date created for: ${dateValue} ${startTimeStr}`);
        return;
    }


    const durationMinutes = (timerequired_Hour * 60) + timerequired_Minute;
    const endDateTime = new Date(startDateTime.getTime() + durationMinutes * 60000);


    const formatToUSDateTime = (dt) => {
        if (isNaN(dt.getTime())) return '';
        const mo = (dt.getMonth() + 1).toString().padStart(2, '0');
        const d = dt.getDate().toString().padStart(2, '0');
        const y = dt.getFullYear();

        let h = dt.getHours();
        const m = dt.getMinutes().toString().padStart(2, '0');
        const ampm = h >= 12 ? 'PM' : 'AM';
        h = h % 12;
        h = h ? h : 12;

        return `${mo}/${d}/${y} ${h}:${m} ${ampm}`;
    };


    form.querySelector("[id='txt_StartDate']").value = formatToUSDateTime(startDateTime);
    form.querySelector("[id='txt_EndDate']").value = formatToUSDateTime(endDateTime);
}


function calculateDate(dateStr) {
    const form = document.getElementById("editForm");
    const date = new Date(dateStr);
    if (isNaN(date)) {
        console.warn(`Invalid date: ${dateStr}`);
        return;
    }
    form.querySelector("[name='date']").value = date.toISOString().split('T')[0];
    calculateStartEndTime();
}




let currentFormsModal = null;
let selectedForms = [];
let currentAppointmentForms = [];
let currentFormInstance = null;

function initializeFormsIntegration() {

    $('select[name="serviceTypeNew"], select[name="serviceTypeEdit"]').on('change', function () {
        const serviceType = $(this).val();
        const isNewForm = $(this).attr('name') === 'serviceTypeNew';

        if (serviceType) {
            loadAutoAssignedForms(serviceType, isNewForm);
        }
    });
}

function openFormsSelectionModal(mode) {
    currentFormsModal = mode;

    if (mode === 'new') {
        selectedForms = [];
    }

    $('#formsSelectionModal').modal('show');
    loadAvailableForms();

    if (mode === 'edit') {

        setTimeout(() => {
            loadCurrentlySelectedForms();
        }, 200);
    }
}

function loadAvailableForms() {
    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAllTemplates",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                populateAvailableFormsList(response.d);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading available forms:', error);
        }
    });
}

function populateAvailableFormsList(forms) {
    const container = $('#availableFormsList');
    container.empty();

    forms.filter(form => form.IsActive).forEach(form => {
        const formItem = $(`
            <div class="form-check mb-2">
                <input class="form-check-input" type="checkbox" id="form_${form.Id}" 
                       value="${form.Id}" onchange="toggleFormSelection(${form.Id}, '${form.TemplateName}', this.checked)">
                <label class="form-check-label" for="form_${form.Id}">
                    <strong>${form.TemplateName}</strong>
                    ${form.Description ? '<br><small class="text-muted">' + form.Description + '</small>' : ''}
                    ${form.RequireSignature ? '<br><small class="text-info"><i class="fa fa-pencil"></i> Signature Required</small>' : ''}
                </label>
            </div>
        `);
        container.append(formItem);
    });
}

function toggleFormSelection(formId, formName, isSelected) {
    if (isSelected) {
        selectedForms.push({ id: formId, name: formName });
    } else {
        selectedForms = selectedForms.filter(form => form.id !== formId);
    }

    updateSelectedFormsList();
}




function updateSelectedFormsFromIds(formIds) {
    console.log('updateSelectedFormsFromIds called with:', formIds);
    if (!formIds || !Array.isArray(formIds) || formIds.length === 0) {
        console.log('No form IDs provided, clearing selection');
        selectedForms = [];
        updateSelectedFormsList();
        return;
    }

    selectedForms = [];

    $('.form-check-input[type="checkbox"]').prop('checked', false);

    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAllTemplates",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                const availableForms = response.d;

                formIds.forEach(formId => {
                    const form = availableForms.find(f => f.Id === formId);
                    if (form) {
                        selectedForms.push({ id: form.Id, name: form.TemplateName });

                        setTimeout(() => {
                            $(`#form_${form.Id}`).prop('checked', true);
                        }, 100);
                    }
                });
                updateSelectedFormsList();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading available forms for updateSelectedFormsFromIds:', error);

            formIds.forEach(formId => {
                selectedForms.push({ id: formId, name: `Form ${formId}` });
            });
            updateSelectedFormsList();
        }
    });
}

function updateSelectedFormsList() {
    const container = $('#selectedFormsList');
    container.empty();

    if (selectedForms.length === 0) {
        container.append('<p class="text-muted">No forms selected</p>');
        return;
    }

    selectedForms.forEach(form => {
        const formItem = $(`
            <div class="selected-form-item p-2 mb-2 border rounded">
                <div class="d-flex justify-content-between align-items-center">
                    <span>${form.name}</span>
                    <button type="button" class="btn btn-sm btn-outline-danger" 
                            onclick="removeSelectedForm(${form.id})">
                        <i class="fa fa-times"></i>
                    </button>
                </div>
            </div>
        `);
        container.append(formItem);
    });
}

function removeSelectedForm(formId) {
    selectedForms = selectedForms.filter(form => form.id !== formId);
    $(`#form_${formId}`).prop('checked', false);
    updateSelectedFormsList();
}

function loadAutoAssignedForms(serviceType, isNewForm) {
    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAutoAssignedForms",
        data: JSON.stringify({ serviceType: serviceType }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const containerId = isNewForm ? 'selectedFormsNew' : 'selectedFormsEdit';
            const container = $(`#${containerId}`);

            if (response.d && response.d.length > 0) {
                container.empty();
                response.d.forEach(form => {
                    const formBadge = $(`
                        <span class="badge badge-primary me-2 mb-2" data-form-id="${form.Id}">
                            ${form.TemplateName}
                            ${form.RequireSignature ? ' <i class="fa fa-pencil"></i>' : ''}
                        </span>
                    `);
                    container.append(formBadge);
                });

                selectedForms = response.d.map(form => ({ id: form.Id, name: form.TemplateName }));
            } else {
                container.html('<small class="text-muted">No auto-assigned forms for this service type</small>');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading auto-assigned forms:', error);
        }
    });
}

function applyFormsSelection() {
    const containerId = currentFormsModal === 'new' ? 'selectedFormsNew' : 'selectedFormsEdit';
    const container = $(`#${containerId}`);

    container.empty();

    if (selectedForms.length === 0) {
        container.html('<small class="text-muted">No forms selected</small>');

    } else {
        selectedForms.forEach(form => {
            const formBadge = $(`
                <span class="badge badge-success me-2 mb-2" data-form-id="${form.id}">
                    ${form.name}
                    <button type="button" class="btn btn-sm btn-link text-dark p-0 ms-1" 
                            onclick="removeFormFromAppointment(${form.id})">
                        <i class="fa fa-times"></i>
                    </button>
                </span>
            `);
            container.append(formBadge);
        });

        if (currentFormsModal === 'edit') {
            $('#formActionsContainer').show();
        }
    }
    $('#formsSelectionModal').modal('hide');
}

function removeFormFromAppointment(formId) {
    selectedForms = selectedForms.filter(form => form.id !== formId);
    $(`.badge[data-form-id="${formId}"]`).remove();

    const container = currentFormsModal === 'new' ? $('#selectedFormsNew') : $('#selectedFormsEdit');
    if (selectedForms.length === 0) {
        container.html('<small class="text-muted">No forms selected</small>');

        if (currentFormsModal === 'edit') {
            $('#formActionsContainer').hide();
        }
    }
}

function openAppointmentFormsModal() {
    $('#formName').empty();
    $('#formViewerContainer').empty();
    $('#editModal').modal('hide');

    const appointmentId = $('#AppoinmentId').val();
    if (!appointmentId) {
        showAlert({
            icon: 'warning',
            title: 'No Appointment Selected',
            text: 'Please select an appointment first.',
            confirmButtonText: 'OK'
        });
        return;
    }

    $('#appointmentFormsModal').modal('show');
    loadAppointmentForms(appointmentId);
}

function loadAppointmentForms(appointmentId) {
    $("#loader").show();
    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAppointmentForms",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                $("#loader").hide();
                currentAppointmentForms = response.d;
                populateAppointmentFormsList(response.d);
            }
        },
        error: function (xhr, status, error) {
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to load appointment forms',
                confirmButtonText: 'OK'
            });
        }
    });
}

function populateAppointmentFormsList(forms) {
    const container = $('#appointmentFormsList');
    container.empty();

    if (!forms || forms.length === 0) {
        container.append('<p class="text-muted">No forms attached to this appointment</p>');
        return;
    }

    forms.forEach(form => {
        const statusClass = getFormStatusClass(form.Status);
        const formItem = $(`
            <div class="form-item p-3 mb-2 border rounded cursor-pointer" 
                 onclick="openFormForFilling(${form.TemplateId})">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${form.TemplateName}</strong>
                        <br><small class="text-muted">Status: <span class="${statusClass}">${form.Status}</span></small>
                        ${form.CompletedDateTime ? '<br><small class="text-muted">Completed: ' + formatDateTime(form.CompletedDateTime) + '</small>' : ''}
                    </div>
                    <div class="form-actions">
                        ${form.RequireSignature ? '<i class="fa fa-pencil text-info" title="Signature Required"></i>' : ''}
                        ${form.RequireTip ? '<i class="fa fa-dollar text-success ms-1" title="Tip Enabled"></i>' : ''}
                    </div>
                </div>
            </div>
        `);
        container.append(formItem);
    });
}
function openFormForFilling(templateId) {
    GlobalTemplateId = templateId;
    $("#loader").show();
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetFormStructure",
        data: JSON.stringify({ templateId: templateId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            try {

                let formStructure = {};
                if (response && response.d !== undefined && response.d !== null) {
                    if (typeof response.d === "string") {

                        try {
                            formStructure = JSON.parse(response.d);
                        } catch (parseErr) {
                            console.error("Failed to JSON.parse(response.d):", parseErr, "response.d:", response.d);
                            $('#formViewerContainer').html('<div class="drop-zone">Invalid form structure received from server</div>');
                            return;
                        }
                    } else {
                        $("#loader").hide();

                        formStructure = response.d;
                    }
                } else {
                    console.warn("Empty response.d:", response);
                }

                console.log("Parsed formStructure:", formStructure);


                var formTemplateData = formStructure.FormStructure;


                if (typeof formTemplateData === "string") {
                    try {
                        formTemplateData = JSON.parse(formTemplateData);
                    } catch (e) {
                        console.error("Failed to parse FormStructure:", e);
                        formTemplateData = {};
                    }
                }
                $('#formViewerContainer').empty();
                $("#formName").text(formStructure.TemplateName);
                if (formTemplateData.fields && formTemplateData.fields.length > 0) {

                    formTemplateData.fields.forEach(function (field) {

                        const fieldHtml = generateFieldFromStructure(field);
                        $('#formViewerContainer').append(fieldHtml);
                    });
                } else {

                    $('#formViewerContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
                }
            } catch (error) {
                console.error('Error parsing form structure:', error);
                $('#formViewerContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading form structure:', error);
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to load form structure',
                confirmButtonText: 'OK'
            });
            $('#formViewerContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
        }
    });
}

function generateDropdownHtml(options) {
    if (!options || options.length === 0) {
        return '<select class="form-control"><option>Option 1</option><option>Option 2</option></select>';
    }

    let html = '<select class="form-control">';
    options.forEach(option => {
        html += `<option value="${option.value || option}">${option.label || option}</option>`;
    });
    html += '</select>';
    return html;
}

function generateRadioHtml(options, fieldId) {
    if (!options || options.length === 0) {
        return `<div class="form-check"><input type="radio" class="form-check-input" name="radio_${fieldId}"><label class="form-check-label">Option 1</label></div>`;
    }

    let html = '';
    options.forEach((option, index) => {
        html += `<div class="form-check">
            <input type="radio" class="form-check-input" name="radio_${fieldId}" value="${option.value || option}">
            <label class="form-check-label">${option.label || option}</label>
        </div>`;
    });
    return html;
}

function generateFieldFromStructure(field) {
    const fieldId = field.id || 'field_' + Date.now();
    const fieldType = field.type || 'text';
    const fieldLabel = field.label || 'Untitled Field';
    const isRequired = field.required || false;

    const fieldConfig = {
        text: { icon: 'fa-font', input: `<input type="text" class="form-control" placeholder="${field.placeholder || 'Enter text'}" ${field.defaultValue ? 'value="' + field.defaultValue + '"' : ''}>` },
        textarea: { icon: 'fa-align-left', input: `<textarea class="form-control" rows="3" placeholder="${field.placeholder || 'Enter text'}">${field.defaultValue || ''}</textarea>` },
        number: { icon: 'fa-hashtag', input: `<input type="number" class="form-control" placeholder="${field.placeholder || 'Enter number'}" ${field.defaultValue ? 'value="' + field.defaultValue + '"' : ''}>` },
        date: { icon: 'fa-calendar', input: `<input type="date" class="form-control" ${field.defaultValue ? 'value="' + field.defaultValue + '"' : ''}>` },
        dropdown: { icon: 'fa-caret-down', input: generateDropdownHtml(field.options) },
        checkbox: { icon: 'fa-check-square', input: `<div class="form-check"><input type="checkbox" class="form-check-input" ${field.defaultValue ? 'checked' : ''}><label class="form-check-label">${field.checkboxLabel || 'Check this option'}</label></div>` },
        radio: { icon: 'fa-dot-circle', input: generateRadioHtml(field.options, fieldId) },
        signature: { icon: 'fa-pencil', input: '<div class="signature-pad" style="border: 1px solid #ddd; height: 150px; display: flex; align-items: center; justify-content: center;">Signature Area</div>' }
    };

    const config = fieldConfig[fieldType] || fieldConfig.text;

    return `
        <div class="form-field" data-field-id="${fieldId}" data-field-type="${fieldType}" onclick="selectField('${fieldId}')">
            <div class="form-group">
                <label><i class="fa ${config.icon}"></i> ${fieldLabel}${isRequired ? ' *' : ''}</label>
                ${config.input}
            </div>
        </div>
    `;
}

function toggleUnscheduledSort(view) {

    unscheduledSortOrder = unscheduledSortOrder === 'asc' ? 'desc' : 'asc';


    const sortBtnId = view === 'resource' ? '#sortUnscheduledBtnResource' : '#sortUnscheduledBtn';
    const $sortBtn = $(sortBtnId);


    if ($sortBtn.length) {
        const $icon = $sortBtn.find('i');

        $icon.removeClass('fa-sort-amount-up fa-sort-amount-down');


        if (unscheduledSortOrder === 'asc') {
            $icon.addClass('fa-sort-amount-up');
        } else {
            $icon.addClass('fa-sort-amount-down');
        }
    }


    renderUnscheduledList(view);
}

function loadAppointmentSpecificLinks(appointmentId) {
    if (!appointmentId) {
        appointmentId = $('#AppoinmentId').val();
    }
    if (!appointmentId) {
        return;
    }

    const container = $('#appointmentSpecificLinks');
    container.html('<div class="text-center p-2"><small class="text-muted">Loading...</small></div>');

    // Load invoices/estimates
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetAppointmentInvoices",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            let html = '';
            const invoices = response.d || [];
            
            if (invoices.length === 0) {
                html = '<small class="text-muted">No invoices or estimates attached</small>';
            } else {
                invoices.forEach(inv => {
                    const typeLabel = inv.InvoiceType === 'Proposal' ? 'Estimate' : 'Invoice';
                    const link = inv.ExternalLink || '#';
                    html += `<div class="mb-1">
                        <a href="${link}" target="_blank" class="btn btn-sm btn-outline-primary me-1">
                            <i class="fa fa-file-invoice"></i> ${typeLabel} #${inv.InvoiceNumber || inv.ID}
                        </a>
                        <small class="text-muted">${inv.InvoiceDate || ''} - $${parseFloat(inv.Total || 0).toFixed(2)}</small>
                    </div>`;
                });
            }

            // Load forms count
            const appointment = appointments.find(a => a.AppoinmentId == appointmentId);
            if (appointment && appointment.AttachedForms) {
                const formCount = Array.isArray(appointment.AttachedForms) ? appointment.AttachedForms.length : 0;
                if (formCount > 0) {
                    html += `<div class="mt-2">
                        <button type="button" class="btn btn-sm btn-outline-info" onclick="openAppointmentFormsModal()">
                            <i class="fa fa-file-alt"></i> View ${formCount} Form(s)
                        </button>
                    </div>`;
                }
            }

            container.html(html || '<small class="text-muted">No items attached to this appointment</small>');
        },
        error: function () {
            container.html('<small class="text-muted">Error loading appointment links</small>');
        }
    });
}

// Track if CSL handlers are already set up
let cslHandlersInitialized = false;

function loadCslDataForModal(customerId, siteId) {
    if (!customerId) return;

    // Only set up handlers once
    if (!cslHandlersInitialized) {
        // Remove any existing handlers first using event delegation on the modal
        $('#editModal').off('shown.bs.tab.cslModal');
        
        // Set up tab click handlers to load content on demand using event delegation
        $('#editModal').on('shown.bs.tab.cslModal', '#csl-basic-tab, #csl-appointments-tab, #csl-invoices-tab, #csl-notes-tab, #csl-equipment-tab, #csl-pictures-tab, #csl-files-tab, #csl-agreements-tab', function (e) {
            e.stopPropagation();
            e.stopImmediatePropagation();
            
            const $tab = $(e.target);
            const targetId = $tab.attr('data-bs-target') || $tab.attr('href');
            
            if (!targetId) {
                console.warn('No target ID found for tab');
                return;
            }
            
            // Map target section IDs to content container IDs
            const contentIdMap = {
                '#csl-basic-section': 'cslBasicInfoContent',
                '#csl-appointments-section': 'cslAppointmentsContent',
                '#csl-invoices-section': 'cslInvoicesContent',
                '#csl-notes-section': 'cslNotesContent',
                '#csl-equipment-section': 'cslEquipmentContent',
                '#csl-pictures-section': 'cslPicturesContent',
                '#csl-files-section': 'cslFilesContent',
                '#csl-agreements-section': 'cslAgreementsContent'
            };
            
            // Get the content container ID from the map
            const contentId = contentIdMap[targetId];
            if (!contentId) {
                console.warn('No content ID mapping found for:', targetId);
                return;
            }
            
            const $content = $('#' + contentId);
            
            if ($content.length === 0) {
                console.warn('Content container not found:', contentId);
                return;
            }
            
            // Check if already loaded (has content other than spinner or empty)
            const currentContent = $content.html().trim();
            if (currentContent && 
                !currentContent.includes('spinner-border') && 
                !currentContent.includes('Loading...') &&
                currentContent !== '' &&
                !currentContent.includes('Click tab to load content')) {
                return; // Already loaded
            }

            // Show loading state
            $content.html('<div class="text-center p-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');

            // Store customerId and siteId in closure
            const modalCustomerId = customerId;
            const modalSiteId = siteId;

            $.ajax({
                type: "POST",
                url: "Appointments.aspx/GetCslDrawerData",
                data: JSON.stringify({ customerId: modalCustomerId, siteId: modalSiteId }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    const data = response.d;
                    if (data) {
                        populateCslModalTab(targetId, data);
                    } else {
                        $content.html('<div class="alert alert-warning">Could not load data.</div>');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error loading CSL data:', error, xhr);
                    $content.html('<div class="alert alert-danger">An error occurred while fetching data.</div>');
                }
            });
        });
        
        cslHandlersInitialized = true;
    }
    
    // Initialize content divs with empty state if needed
    const contentDivs = ['cslBasicInfoContent', 'cslAppointmentsContent', 'cslInvoicesContent', 'cslNotesContent', 'cslEquipmentContent', 'cslPicturesContent', 'cslFilesContent', 'cslAgreementsContent'];
    contentDivs.forEach(divId => {
        const $div = $('#' + divId);
        if ($div.length > 0 && $div.html().trim() === '') {
            $div.html('<div class="text-muted text-center p-3">Click tab to load content</div>');
        }
    });
}

function populateCslModalTab(tabId, data) {
    const safe = (val) => val || 'N/A';
    
    // Helper function to escape HTML (use global if available, otherwise define locally)
    const escapeHTML = (str) => {
        if (typeof window.escapeHTML === 'function') {
            return window.escapeHTML(str);
        }
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    };
    
    // Helper function to format file sizes
    const formatFileSize = (bytes) => {
        if (!bytes || bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    };
    
    // Map target section IDs to content container IDs
    const contentIdMap = {
        '#csl-basic-section': 'cslBasicInfoContent',
        '#csl-appointments-section': 'cslAppointmentsContent',
        '#csl-invoices-section': 'cslInvoicesContent',
        '#csl-notes-section': 'cslNotesContent',
        '#csl-equipment-section': 'cslEquipmentContent',
        '#csl-pictures-section': 'cslPicturesContent',
        '#csl-files-section': 'cslFilesContent',
        '#csl-agreements-section': 'cslAgreementsContent'
    };
    
    const contentId = contentIdMap[tabId];
    if (!contentId) {
        console.error('No content ID mapping found for tab:', tabId);
        return;
    }
    
    if (tabId === '#csl-basic-section') {
        const html = `
            <table class="table table-sm table-bordered">
                <tbody>
                    <tr><th>Customer</th><td>${safe(data.CustomerInfo.FirstName)} ${safe(data.CustomerInfo.LastName)}</td></tr>
                    <tr><th>Contact</th><td>${safe(data.SiteInfo.FirstName)} ${safe(data.SiteInfo.LastName)}</td></tr>
                    <tr><th>Phone</th><td><a href="tel:${safe(data.SiteInfo.PhoneNumber)}">${safe(data.SiteInfo.PhoneNumber)}</a></td></tr>
                    <tr><th>Email</th><td><a href="mailto:${safe(data.SiteInfo.Email)}">${safe(data.SiteInfo.Email)}</a></td></tr>
                    <tr><th>Address</th><td>${safe(data.SiteInfo.Address)}</td></tr>
                    <tr><th>Status</th><td>${data.SiteInfo.IsActive ? 'Active' : 'Inactive'}</td></tr>
                </tbody>
            </table>
        `;
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-appointments-section') {
        let html = '<p>No appointments found.</p>';
        if (data.Appointments && data.Appointments.length > 0) {
            html = '<ul class="list-group">';
            data.Appointments.slice(0, 10).forEach(appt => {
                html += `<li class="list-group-item">
                    ${safe(appt.RequestDate)} - ${safe(appt.ServiceType)}
                    <span class="badge bg-info float-end">${safe(appt.AppoinmentStatus)}</span>
                </li>`;
            });
            if (data.Appointments.length > 10) {
                html += '<li class="list-group-item text-center">...and more</li>';
            }
            html += '</ul>';
        }
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-invoices-section') {
        let html = '<p>No invoices found.</p>';
        if (data.Invoices && data.Invoices.length > 0) {
            html = '<ul class="list-group">';
            data.Invoices.slice(0, 10).forEach(inv => {
                const link = inv.ExternalLink ? `<a href="${inv.ExternalLink}" target="_blank">View</a>` : '';
                html += `<li class="list-group-item">
                    ${safe(inv.InvoiceNumber)} - ${safe(inv.InvoiceType)} - $${parseFloat(safe(inv.Total)).toFixed(2)}
                    ${link}
                    <span class="badge bg-info float-end">${safe(inv.InvoiceStatus)}</span>
                </li>`;
            });
            if (data.Invoices.length > 10) {
                html += '<li class="list-group-item text-center">...and more</li>';
            }
            html += '</ul>';
        }
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-notes-section') {
        // Get customerId and siteId from the modal
        // Store these in data attributes for later use
        const customerId = $('#CustomerID').val();
        const appointmentId = $('#AppoinmentId').val();
        
        // Get siteId from the appointment data or site selector
        let siteId = 0;
        if (appointmentId) {
            const appointment = appointments.find(x => x.AppoinmentId === appointmentId.toString());
            if (appointment && appointment.SiteId) {
                siteId = parseInt(appointment.SiteId) || 0;
            }
        }
        // Fallback to site selector if available
        if (siteId === 0) {
            const siteSelector = $('#siteSelectionContainer select');
            if (siteSelector.length) {
                siteId = parseInt(siteSelector.val()) || 0;
            }
        }
        
        let html = `
            <div class="mb-3">
                <h5 class="mb-3">Create a New Note</h5>
                <form id="cslNoteForm">
                    <div class="mb-3">
                        <label for="noteTagTo" class="form-label">Tag To</label>
                        <select id="noteTagTo" class="form-select" multiple>
                            <option value="Appointment">Appointment</option>
                            <option value="FAPRO">FA-PRO</option>
                            <option value="FSM">FSM</option>
                            <option value="CEC">CEC</option>
                        </select>
                        <small class="form-text text-muted">Click to select tags from the list. Hold Ctrl/Cmd to select multiple.</small>
                    </div>
                    <div class="mb-3">
                        <label for="noteDescription" class="form-label">Note</label>
                        <textarea id="noteDescription" class="form-control" rows="4" placeholder="Enter note text..."></textarea>
                    </div>
                    <button type="submit" id="addNoteButton" class="btn btn-primary">
                        <i class="fas fa-plus me-2"></i>Add Note
                    </button>
                </form>
            </div>
            <hr>
            <h5 class="mb-3">Notes History</h5>
        `;
        
        if (data.Notes && data.Notes.length > 0) {
            html += '<div class="table-responsive"><table class="table table-sm table-hover">';
            html += '<thead><tr><th>Date</th><th>Note</th><th>Tagged From</th><th>Created By</th></tr></thead><tbody>';
            data.Notes.slice(0, 20).forEach(note => {
                const noteText = safe(note.Description);
                const truncatedNote = noteText.length > 100 ? noteText.substring(0, 100) + '...' : noteText;
                html += `<tr>
                    <td>${safe(note.CreatedAt)}</td>
                    <td>${truncatedNote}</td>
                    <td><span class="badge bg-secondary">${safe(note.TaggedFrom || 'FSM')}</span></td>
                    <td>${safe(note.UserId)}</td>
                </tr>`;
            });
            if (data.Notes.length > 20) {
                html += '<tr><td colspan="4" class="text-center">...and ' + (data.Notes.length - 20) + ' more notes</td></tr>';
            }
            html += '</tbody></table></div>';
        } else {
            html += '<p class="text-muted">No notes found.</p>';
        }
        
        $('#' + contentId).html(html);
        
        // Attach form submit handler
        $('#cslNoteForm').off('submit').on('submit', function(e) {
            e.preventDefault();
            saveCslNote(customerId, siteId, appointmentId);
        });
    } else if (tabId === '#csl-equipment-section') {
        let html = '<p>No equipment found.</p>';
        if (data.Equipment && data.Equipment.length > 0) {
            html = '<ul class="list-group">';
            data.Equipment.slice(0, 10).forEach(eq => {
                html += `<li class="list-group-item">${safe(eq.EquipmentName)} - ${safe(eq.Model)}</li>`;
            });
            if (data.Equipment.length > 10) {
                html += '<li class="list-group-item text-center">...and more</li>';
            }
            html += '</ul>';
        }
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-pictures-section') {
        let html = '<p>No pictures found.</p>';
        if (data.Pictures && data.Pictures.length > 0) {
            html = '<div class="row g-2">';
            data.Pictures.forEach((pic, index) => {
                // Handle both object structure and ViewModel structure
                const picUrl = pic.FileUrl || pic.Url || pic.Path || pic.FilePath || '';
                const picName = pic.FileName || pic.Name || `Picture ${index + 1}`;
                const uploadDate = pic.UploadDate || pic.CreatedDate || '';
                html += `<div class="col-md-3 col-sm-4 col-6 mb-2">
                    <div class="card h-100">
                        <img src="${picUrl}" class="card-img-top" alt="${escapeHTML(picName)}" style="height: 150px; object-fit: cover; cursor: pointer;" onclick="window.open('${picUrl}', '_blank')" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'200\' height=\'200\'%3E%3Crect fill=\'%23ddd\' width=\'200\' height=\'200\'/%3E%3Ctext fill=\'%23999\' font-family=\'sans-serif\' font-size=\'14\' dy=\'10.5\' x=\'50%25\' y=\'50%25\' text-anchor=\'middle\'%3ENo Image%3C/text%3E%3C/svg%3E';">
                        <div class="card-body p-2">
                            <p class="card-text small mb-0" title="${escapeHTML(picName)}">${escapeHTML(picName.length > 20 ? picName.substring(0, 20) + '...' : picName)}</p>
                            ${uploadDate ? `<small class="text-muted d-block">${uploadDate}</small>` : ''}
                        </div>
                    </div>
                </div>`;
            });
            html += '</div>';
        }
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-files-section') {
        let html = '<p>No files found.</p>';
        if (data.Files && data.Files.length > 0) {
            // Helper function to get file type icon
            const getFileTypeIcon = (fileType) => {
                if (!fileType) return 'fas fa-file';
                const type = fileType.toLowerCase();
                if (type.includes('pdf')) return 'fas fa-file-pdf text-danger';
                if (type.includes('word') || type.includes('doc')) return 'fas fa-file-word text-primary';
                if (type.includes('excel') || type.includes('xls')) return 'fas fa-file-excel text-success';
                if (type.includes('image')) return 'fas fa-file-image text-info';
                if (type.includes('text')) return 'fas fa-file-alt text-secondary';
                return 'fas fa-file text-muted';
            };
            
            html = '<div class="table-responsive"><table class="table table-sm table-hover">';
            html += '<thead><tr><th>File Name</th><th>Type</th><th>Size</th><th>Upload Date</th><th>Actions</th></tr></thead><tbody>';
            data.Files.forEach(file => {
                // Handle both object structure and ViewModel structure
                const fileName = file.FileName || file.Name || 'Unknown File';
                const fileUrl = file.FileUrl || file.Url || file.Path || file.FilePath || '#';
                const fileType = file.FileType || file.Type || 'Unknown';
                const fileSize = file.FileSize || file.Size || 0;
                const uploadDate = file.UploadDate || file.CreatedDate || '';
                const fileIcon = getFileTypeIcon(fileType);
                html += `<tr>
                    <td>
                        <i class="${fileIcon} me-2"></i>
                        <a href="${fileUrl}" target="_blank" class="text-decoration-none">${escapeHTML(fileName)}</a>
                    </td>
                    <td><small class="text-muted">${escapeHTML(fileType)}</small></td>
                    <td><small class="text-muted">${formatFileSize(fileSize)}</small></td>
                    <td><small class="text-muted">${uploadDate}</small></td>
                    <td>
                        <a href="${fileUrl}" target="_blank" class="btn btn-sm btn-outline-primary">
                            <i class="fas fa-eye"></i> View
                        </a>
                    </td>
                </tr>`;
            });
            html += '</tbody></table></div>';
        }
        $('#' + contentId).html(html);
    } else if (tabId === '#csl-agreements-section') {
        let html = '<p>No maintenance agreements found.</p>';
        if (data.MaintenanceAgreements && data.MaintenanceAgreements.length > 0) {
            html = '<div class="table-responsive"><table class="table table-sm table-hover">';
            html += '<thead><tr><th>Agreement Name</th><th>Start Date</th><th>End Date</th><th>Status</th><th>Actions</th></tr></thead><tbody>';
            data.MaintenanceAgreements.forEach(agreement => {
                // Handle both object structure and ViewModel structure
                const name = safe(agreement.AgreementName || agreement.Name || 'Unnamed Agreement');
                const startDate = safe(agreement.StartDate || agreement.EffectiveDate || '');
                const endDate = safe(agreement.EndDate || agreement.ExpirationDate || '');
                const status = agreement.Status || 'Active';
                const fileUrl = agreement.FileUrl || agreement.Url || agreement.FilePath || '';
                const statusClass = status.toLowerCase() === 'active' ? 'success' : status.toLowerCase() === 'expired' ? 'danger' : 'secondary';
                html += `<tr>
                    <td>${escapeHTML(name)}</td>
                    <td>${startDate}</td>
                    <td>${endDate}</td>
                    <td><span class="badge bg-${statusClass}">${escapeHTML(status)}</span></td>
                    <td>
                        ${fileUrl ? `<a href="${fileUrl}" target="_blank" class="btn btn-sm btn-outline-primary"><i class="fas fa-eye"></i> View</a>` : '<span class="text-muted">No file</span>'}
                    </td>
                </tr>`;
            });
            html += '</tbody></table></div>';
        }
        $('#' + contentId).html(html);
    } else {
        $('#' + contentId).html('<p>Content coming soon.</p>');
    }
}

function saveCslNote(customerId, siteId, appointmentId) {
    const noteDescription = $('#noteDescription').val().trim();
    const tagToValues = $('#noteTagTo').val() || [];
    const tagTo = tagToValues.join(', ');
    
    if (!noteDescription) {
        showAlert({ icon: 'warning', title: 'Validation Error', text: 'Please enter a note description.' });
        return;
    }
    
    if (!customerId) {
        showAlert({ icon: 'error', title: 'Error', text: 'Customer ID is missing.' });
        return;
    }
    
    // Show loading state
    $('#addNoteButton').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Saving...');
    
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/SaveCslNote",
        data: JSON.stringify({
            customerId: customerId,
            siteId: siteId || 0,
            appointmentId: appointmentId || '',
            description: noteDescription,
            taggedTo: tagTo,
            taggedFrom: 'FSM'
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                showAlert({ 
                    icon: 'success', 
                    title: 'Success!', 
                    text: 'Note has been saved successfully.', 
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Clear form
                $('#noteDescription').val('');
                $('#noteTagTo').val([]);
                
                // Reload notes by re-fetching CSL data and updating the notes tab
                const currentCustomerId = $('#CustomerID').val();
                const currentAppointmentId = $('#AppoinmentId').val();
                
                // Get siteId from appointment data or site selector
                let currentSiteId = 0;
                if (currentAppointmentId) {
                    const appointment = appointments.find(x => x.AppoinmentId === currentAppointmentId.toString());
                    if (appointment && appointment.SiteId) {
                        currentSiteId = parseInt(appointment.SiteId) || 0;
                    }
                }
                // Fallback to site selector if available
                if (currentSiteId === 0) {
                    const siteSelector = $('#siteSelectionContainer select');
                    if (siteSelector.length) {
                        currentSiteId = parseInt(siteSelector.val()) || 0;
                    }
                }
                
                if (currentCustomerId) {
                    // Re-fetch CSL data
                    $.ajax({
                        type: "POST",
                        url: "Appointments.aspx/GetCslDrawerData",
                        data: JSON.stringify({ customerId: currentCustomerId, siteId: currentSiteId }),
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (response) {
                            const data = response.d;
                            if (data) {
                                // Update the notes tab content
                                populateCslModalTab('#csl-notes-section', data);
                            }
                        },
                        error: function (xhr, status, error) {
                            console.error('Error reloading notes:', error);
                        }
                    });
                }
            } else {
                showAlert({ icon: 'error', title: 'Error', text: 'Failed to save note. Please try again.' });
            }
        },
        error: function (xhr, status, error) {
            console.error('Error saving note:', error, xhr);
            showAlert({ icon: 'error', title: 'Error', text: 'An error occurred while saving the note. Please try again.' });
        },
        complete: function() {
            $('#addNoteButton').prop('disabled', false).html('<i class="fas fa-plus me-2"></i>Add Note');
        }
    });
}

function loadCurrentlySelectedForms(appointmentId) {
    if (!appointmentId) {
        appointmentId = $('#AppoinmentId').val();
    }
    if (!appointmentId) {
        console.log('No appointment ID found for loadCurrentlySelectedForms');
        return;
    }
    console.log('Loading currently selected forms for appointment:', appointmentId);

    const appointment = appointments.find(a => a.AppoinmentId == appointmentId);
    if (appointment && appointment.AttachedForms) {
        console.log('Found attached forms in local data:', appointment.AttachedForms);

        updateSelectedFormsFromIds(appointment.AttachedForms);
        return;
    }
    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAppointmentForms",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                const container = $('#selectedFormsEdit');
                container.empty();

                if (response.d.length === 0) {
                    container.html('<small class="text-muted">No forms attached to this appointment</small>');
                } else {

                    selectedForms = response.d.map(form => ({
                        id: form.Id,
                        name: form.TemplateName
                    }));

                    response.d.forEach(form => {
                        $(`#form_${form.Id}`).prop('checked', true);
                    });
                    response.d.forEach(form => {
                        const statusClass = getFormStatusClass(form.Status);
                        const formBadge = $(`
                        <div class="form-badge p-2 mb-2 border rounded d-flex justify-content-between align-items-center">
                            <div>
                               <strong>${form.TemplateName}</strong>
                                <br><small class="${statusClass}">Status: ${form.Status}</small>
                            </div>
                            <div>
                                ${form.RequireSignature ? '<i class="fa fa-pencil text-info" title="Signature Required"></i>' : ''}
                                ${form.RequireTip ? '<i class="fa fa-dollar text-success ms-1" title="Tip Enabled"></i>' : ''}
                            </div>
                        </div>
                    `);
                        container.append(formBadge);
                    });

                    if (response.d.length > 0) {
                        $('#formActionsContainer').show();
                    }
                }
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading current forms:', error);
        }
    });
}

function updateAttachedForms() {
    const appointmentId = $('#AppoinmentId').val();
    var customerId = $('#CustomerID').val();

    if (!appointmentId) {
        showAlert({
            icon: 'error',
            title: 'Error',
            text: 'No appointment selected',
            confirmButtonText: 'OK'
        });
        return;
    }
    if (!customerId) {
        showAlert({
            icon: 'error',
            title: 'Error',
            text: 'No customer selected',
            confirmButtonText: 'OK'
        });
        return;
    }









    const formIds = selectedForms.map(form => form.id);
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/UpdateAttachedForms",
        data: JSON.stringify({
            appointmentId: appointmentId,
            customerId: customerId,
            formIds: formIds
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                showAlert({
                    icon: 'success',
                    title: 'Success',
                    text: 'Forms have been attached to the appointment successfully!',
                    timer: 2000
                });

                const appointment = appointments.find(a => a.AppoinmentId == appointmentId);
                if (appointment) {
                    appointment.AttachedForms = formIds;
                }
            } else {
                showAlert({
                    icon: 'error',
                    title: 'Error',
                    text: 'Failed to update attached forms',
                    confirmButtonText: 'OK'
                });
            }
        },
        error: function (xhr, status, error) {
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to update attached forms: ' + error,
                confirmButtonText: 'OK'
            });
        }
    });
}

function sendFormsViaEmail() {
    const appointmentId = $('#AppoinmentId').val();
    if (!appointmentId) {
        showAlert({
            icon: 'error',
            title: 'Error',
            text: 'No appointment selected',
            confirmButtonText: 'OK'
        });
        return;
    }

    if (selectedForms.length === 0) {
        showAlert({
            icon: 'warning',
            title: 'Warning',
            text: 'No forms attached to send',
            confirmButtonText: 'OK'
        });
        return;
    }

    const appointment = appointments.find(a => a.AppoinmentId == appointmentId);
    let customerEmail = appointment?.Email || '';

    if (!customerEmail) {
        customerEmail = prompt('Enter customer email address:');
        if (!customerEmail) return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(customerEmail)) {
        showAlert({
            icon: 'error',
            title: 'Invalid Email',
            text: 'Please enter a valid email address',
            confirmButtonText: 'OK'
        });
        return;
    }
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/SendFormsViaEmail",
        data: JSON.stringify({
            appointmentId: appointmentId,
            customerEmail: customerEmail
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                showAlert({
                    icon: 'success',
                    title: 'Email Sent',
                    text: `Forms have been sent to ${customerEmail} successfully!`,
                    timer: 3000
                });
            } else {
                showAlert({
                    icon: 'error',
                    title: 'Error',
                    text: 'Failed to send email',
                    confirmButtonText: 'OK'
                });
            }
        },
        error: function (xhr, status, error) {
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to send email: ' + error,
                confirmButtonText: 'OK'
            });
        }
    });
}

function sendFormsViaSMS() {
    const appointmentId = $('#AppoinmentId').val();
    if (!appointmentId) {
        showAlert({
            icon: 'error',
            title: 'Error',
            text: 'No appointment selected',
            confirmButtonText: 'OK'
        });
        return;
    }
    if (selectedForms.length === 0) {
        showAlert({
            icon: 'warning',
            title: 'Warning',
            text: 'No forms attached to send',
            confirmButtonText: 'OK'
        });
        return;
    }

    const appointment = appointments.find(a => a.AppoinmentId == appointmentId);
    let customerPhone = appointment?.CustomerPhone || appointment?.Mobile || '';

    if (!customerPhone) {
        customerPhone = prompt('Enter customer phone number:');
        if (!customerPhone) return;
    }

    const phoneRegex = /^[\+]?[1-9][\d]{3,14}$/;
    if (!phoneRegex.test(customerPhone.replace(/[\s\-\(\)]/g, ''))) {
        showAlert({
            icon: 'error',
            title: 'Invalid Phone',
            text: 'Please enter a valid phone number',
            confirmButtonText: 'OK'
        });
        return;
    }
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/SendFormsViaSMS",
        data: JSON.stringify({
            appointmentId: appointmentId,
            customerPhone: customerPhone
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                showAlert({
                    icon: 'success',
                    title: 'SMS Sent',
                    text: `Form notification has been sent to ${customerPhone} successfully!`,
                    timer: 3000
                });
            } else {
                showAlert({
                    icon: 'error',
                    title: 'Error',
                    text: 'Failed to send SMS',
                    confirmButtonText: 'OK'
                });
            }
        },
        error: function (xhr, status, error) {
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to send SMS: ' + error,
                confirmButtonText: 'OK'
            });
        }
    });
}

$(document).on('hidden.bs.modal', '.modal', function () {
    $(this).find('form').trigger('reset');
    $(this).find('.is-invalid').removeClass('is-invalid');
    $(this).find('.invalid-feedback').remove();
});

$(document).on('click', '[data-dismiss="modal"]', function () {
    const modal = $(this).closest('.modal');
    modal.modal('hide');
});

$(document).on('keydown', function (e) {
    if (e.key === 'Escape') {
        $('.modal.show').modal('hide');
    }
});

$('.modal').on('click', function (e) {
    if ($(e.target).hasClass('modal')) {
        $(this).modal('hide');
    }
});

$(document).ready(function () {
    initializeFormsIntegration();
});

function toISODate(d) {
    if (!d) return new Date().toISOString().slice(0, 10);
    const dt = (d instanceof Date) ? d : new Date(d);
    return new Date(dt.getFullYear(), dt.getMonth(), dt.getDate()).toISOString().slice(0, 10);
}

function startOfWeek(iso) {
    const dt = new Date(iso);
    const off = dt.getDay(); // 0..6
    dt.setDate(dt.getDate() - off);
    return toISODate(dt);
}
function endOfWeek(iso) {
    const s = startOfWeek(iso);
    const dt = new Date(s);
    dt.setDate(dt.getDate() + 6);
    return toISODate(dt);
}
function firstOfMonth(iso) {
    const dt = new Date(iso);
    return toISODate(new Date(dt.getFullYear(), dt.getMonth(), 1));
}
function lastOfMonth(iso) {
    const dt = new Date(iso);
    return toISODate(new Date(dt.getFullYear(), dt.getMonth() + 1, 0));
}

function computeRangeByMode(mode, isoDate) {
    switch (mode) {
        case 'day': return { from: isoDate, to: isoDate };
        case 'threeDay': {
            const d = new Date(isoDate);
            const from = new Date(d); from.setDate(d.getDate() - 1);
            const to = new Date(d); to.setDate(d.getDate() + 1);
            return { from: toISODate(from), to: toISODate(to) };
        }
        case 'week': return { from: startOfWeek(isoDate), to: endOfWeek(isoDate) };
        case 'month': return { from: firstOfMonth(isoDate), to: lastOfMonth(isoDate) };
        case 'custom':
        default: return { from: isoDate, to: isoDate };
    }
}

window.computeRangeByMode = computeRangeByMode;
window.toISODate = toISODate;
window.startOfWeek = startOfWeek;
window.endOfWeek = endOfWeek;
window.firstOfMonth = firstOfMonth;
window.lastOfMonth = lastOfMonth;

function getCurrentMode() {
    const dateMode = $("#viewSelect").val();
    const resMode = $("#resourceViewSelect").val();
    return (resMode || dateMode || 'day');
}

function syncDatePickers(changedPickerId, newDate) {
    if (isDateSyncing) return;
    isDateSyncing = true;

    try {
        if (!newDate || isNaN(new Date(newDate))) {
            newDate = globalCurrentDate;
        }

        // Update global state FIRST
        globalCurrentDate = newDate;
        // Also update GlobalDateSync._currentDate so nextPeriod/prevPeriod can read it correctly
        if (!GlobalDateSync._isSyncing) {
            GlobalDateSync._currentDate = newDate;
        }

        // Update date pickers without triggering change events to prevent infinite loops
        $('#dayDatePicker, #resourceDatePicker, #mapDatePicker, #listDatePicker').each(function() {
            if ($(this).val() !== newDate) {
                // Temporarily remove change handler to prevent recursion
                $(this).off('change').val(newDate);
                // Re-attach handler after a short delay
                setTimeout(() => {
                    $(this).on('change', function () {
                        if (!GlobalDateSync._isSyncing && !isDateSyncing) {
                            GlobalDateSync.setDate($(this).val());
                        }
                    });
                }, 50);
            }
        });
        const activeViewFromGlobal = currentView;

        switch (activeViewFromGlobal) {
            case 'date':
                renderDateView(newDate);
                break;
            case 'resource':
                renderResourceView(newDate);
                break;
            case 'map':
                if (window.MapView && typeof window.MapView.reload === 'function') {
                    window.MapView.reload();
                }
                break;
            case 'list':
                if (window.ListViewManager && typeof window.ListViewManager.render === 'function') {
                    window.ListViewManager.render();
                }
                break;
        }

        if (typeof renderDateNav === 'function') {
            renderDateNav('dateNav', newDate);
            renderDateNav('resourceNav', newDate);
        }

    } catch (error) {
        console.error('Error during date synchronization:', error);
    } finally {
        setTimeout(() => {
            isDateSyncing = false;
        }, 100);
    }
}


document.addEventListener('DOMContentLoaded', function () {
    var today = (function () {
        const d = new Date();
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${dd}`;
    })();

    const from = document.getElementById('resourceDatePickerFrom');
    const to = document.getElementById('resourceDatePickerTo');
});

document.addEventListener("DOMContentLoaded", function () {
    const resourceFrom = document.getElementById("resourceDatePickerFrom");
    const resourceTo = document.getElementById("resourceDatePickerTo");
    const listFrom = document.getElementById("listDatePickerFrom");
    const listTo = document.getElementById("listDatePickerTo");
    $('#ProvinceFilter, #ProvinceFilterResource').on('change', function () {
        const selectedProvince = $(this).val();
        populatePostalCodeDropdowns(selectedProvince);
        renderUnscheduledList(this.id === 'ProvinceFilterResource' ? 'resource' : 'date');
    });

    document.querySelectorAll('select').forEach(select => {
        const firstOption = select.querySelector('option');
        if (firstOption && firstOption.value === '' && firstOption.textContent.trim() === '') {
            const label = document.querySelector(`label[for='${select.id}']`) || select.previousElementSibling;
            let placeholder = 'Select ticket status...';
            if (label) {
                placeholder = `Select a ${label.textContent.replace(':', '').trim()}...`;
            }
            firstOption.textContent = placeholder;
        }
    });

    function syncDates(source, target, isFromDate) {
        source.addEventListener("change", () => {

            target.value = source.value;


            if (source === listFrom || source === listTo) {
                const fromDate = $("#listDatePickerFrom").val();
                const toDate = $("#listDatePickerTo").val();

                if (fromDate && toDate) {
                    $("#resourceViewSelect").val('custom');
                    $("#resourceCustomDateRangeContainer").removeClass('d-none');
                    resourceCustomDateRange.from = fromDate;
                    resourceCustomDateRange.to = toDate;


                    if (currentView === "resource") {
                        renderResourceView(fromDate);
                    }
                }
            }
        });
    }

    syncDates(resourceFrom, listFrom, true);
    syncDates(listFrom, resourceFrom, true);
    syncDates(resourceTo, listTo, false);
    syncDates(listTo, resourceTo, false);
});


function populateCustomerDataTab(customerData) {
    const customerDataTab = document.getElementById('customer-data');
    if (!customerDataTab) {
        console.error('Customer data tab not found');
        return;
    }
    const header = customerDataTab.querySelector('h2.h4');
    if (header) {
        if (customerData && customerData.SiteName) {
            header.textContent = `Site Name: ${customerData.SiteName}`;
        } else {
            header.textContent = 'Basic Information';
        }
    }
    const customerNameCell = customerDataTab.querySelector('#customerName');
    const siteContactCell = customerDataTab.querySelector('#siteContact');
    const customerEmailCell = customerDataTab.querySelector('#customerEmail');
    const siteAddressCell = customerDataTab.querySelector('#siteAddress');
    const siteStatusCell = customerDataTab.querySelector('#siteStatus');
    const siteInstructionsCell = customerDataTab.querySelector('#siteInstructions');
    const siteDescriptionCell = customerDataTab.querySelector('#siteDescription');

    if (customerNameCell) {
        customerNameCell.innerHTML = customerData.CustomerName || 'N/A';
    }

    if (siteContactCell) {
        const contactHtml = `
            ${customerData.Contact || 'N/A'}<br />
            <i class="fas fa-phone me-1" style="font-size: 13px;"></i>Phone:
            <a href="${customerData.PhoneLink || '#'}">${customerData.Phone || 'N/A'}</a><br />
            <i class="fas fa-mobile-alt me-1"></i>Mobile:
            <a href="${customerData.MobileLink || '#'}">${customerData.Mobile || 'N/A'}</a>
        `;
        siteContactCell.innerHTML = contactHtml;
    }

    if (customerEmailCell) {
        const emailHtml = `<a href="${customerData.EmailLink || '#'}">${customerData.Email || 'N/A'}</a>`;
        customerEmailCell.innerHTML = emailHtml;
    }

    if (siteAddressCell) {
        siteAddressCell.innerHTML = customerData.Address || 'N/A';
    }

    if (siteStatusCell) {
        siteStatusCell.innerHTML = customerData.Status || 'N/A';
    }

    if (siteInstructionsCell) {
        siteInstructionsCell.innerHTML = customerData.Note || 'N/A';
    }

    if (siteDescriptionCell) {
        siteDescriptionCell.innerHTML = customerData.CreatedOn || 'N/A';
    }
}

function updateResourceViewPagination() {
    const totalItems = resourceViewFilteredAppointments.length;
    resourceViewTotalPages = Math.ceil(totalItems / resourceViewPageSize);

    if (resourceViewCurrentPage > resourceViewTotalPages) {
        resourceViewCurrentPage = resourceViewTotalPages || 1;
    }

    const pageInfo = document.getElementById('resourceViewPageInfo');
    const prevBtn = document.getElementById('resourceViewPrevPage');
    const nextBtn = document.getElementById('resourceViewNextPage');
    const pageSizeSelect = document.getElementById('resourceViewPageSize');

    if (pageInfo) {
        const startItem = (resourceViewCurrentPage - 1) * resourceViewPageSize + 1;
        const endItem = Math.min(resourceViewCurrentPage * resourceViewPageSize, totalItems);
        pageInfo.textContent = `Showing ${startItem}-${endItem} of ${totalItems} resources`;
    }

    if (prevBtn) {
        prevBtn.classList.toggle('disabled', resourceViewCurrentPage <= 1);
    }

    if (nextBtn) {
        nextBtn.classList.toggle('disabled', resourceViewCurrentPage >= resourceViewTotalPages);
    }

    if (pageSizeSelect) {
        pageSizeSelect.value = resourceViewPageSize;
    }
}

function goToResourceViewPage(page) {
    if (page < 1 || page > resourceViewTotalPages) return;

    resourceViewCurrentPage = page;
    renderResourceViewTable();
    updateResourceViewPagination();
}

function changeResourceViewPageSize() {
    const pageSizeSelect = document.getElementById('resourceViewPageSize');
    if (pageSizeSelect) {
        resourceViewPageSize = parseInt(pageSizeSelect.value);
        resourceViewCurrentPage = 1; // Reset to first page
        renderResourceViewTable();
        updateResourceViewPagination();
    }
}

function renderResourceViewTable() {
    const startIndex = (resourceViewCurrentPage - 1) * resourceViewPageSize;
    const endIndex = startIndex + resourceViewPageSize;
    const pageResources = resourceViewFilteredAppointments.slice(startIndex, endIndex);

    renderResourceView($("#resourceDatePicker").val());
}

function openCustomerResponseModal() {
    $('#customerResponseModal').modal('show');
    openFormForFillingForCustomerResponse(GlobalTemplateId);
    $('#appointmentFormsModal').modal('hide');
}

function openFormForFillingForCustomerResponse(templateId) {
    if (!templateId) {
        showAlert({
            icon: 'warning',
            title: 'Form Not Selected.',
            text: 'Please select a form',
            timer: 3000
        });
    }
    GlobalTemplateId = templateId;
    var apptId = $('#AppoinmentId').val();
    var cId = $('#CustomerID').val();
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetCustomerResponseOnForms",
        data: JSON.stringify({
            templateId: templateId,
            appointmentId: apptId,
            customerId: cId
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            try {
                let formStructure = {};
                if (response && response.d !== undefined && response.d !== null) {
                    if (typeof response.d === "string") {

                        try {
                            formStructure = JSON.parse(response.d);
                        } catch (parseErr) {
                            console.error("Failed to JSON.parse(response.d):", parseErr, "response.d:", response.d);
                            $('#formViewerContainer').html('<div class="drop-zone">Invalid form structure received from server</div>');
                            return;
                        }
                    } else {

                        formStructure = response.d;
                    }
                } else {
                    console.warn("Empty response.d:", response);
                }

                console.log("Parsed formStructure:", formStructure);


                var formTemplateData = formStructure;
                $('#customerResponseContainer').empty();
                if (formTemplateData && formTemplateData.length > 0) {
                    formTemplateData.forEach(function (field) {

                        const fieldHtml = generateFieldFromStructure({
                            id: field.fieldId,
                            type: field.type,
                            label: field.label
                        });

                        $('#customerResponseContainer').append(fieldHtml);

                        const fieldWrapper = $(`[data-field-id="${field.fieldId}"]`);

                        switch (field.type) {
                            case 'text':
                            case 'number':
                            case 'date':
                                fieldWrapper.find('input').val(field.value || '');
                                break;

                            case 'textarea':
                                fieldWrapper.find('textarea').val(field.value || '');
                                break;

                            case 'dropdown':
                                fieldWrapper.find('select').val(field.value || '');
                                break;

                            case 'checkbox':
                                fieldWrapper.find('input[type="checkbox"]').prop('checked', field.value === true || field.value === "true");
                                break;

                            case 'radio':
                                fieldWrapper.find(`input[type="radio"][value="${field.value}"]`).prop('checked', true);
                                break;

                            case 'signature':
                                fieldWrapper.find('.signature-pad').text(field.value || 'Signature Area');
                                break;
                        }

                    });
                } else {

                    $('#customerResponseContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
                }
            } catch (error) {
                console.error('Error parsing form structure:', error);
                $('#customerResponseContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading form structure:', error);
            showAlert({
                icon: 'error',
                title: 'Error',
                text: 'Failed to load form structure',
                confirmButtonText: 'OK'
            });
            $('#customerResponseContainer').html('<div class="drop-zone">Drag fields here to build your form</div>');
        }
    });
}

function showAppointmentModalFromResponseClose() {
    $('#appointmentFormsModal').modal('show');
    openFormForFilling(GlobalTemplateId);
}
let currentFaIdButton = null;

function openFaIdModal(event, appointmentId) {
    event.stopPropagation();
    currentEditId = appointmentId;
    currentFaIdButton = event.currentTarget; // Store the button element
    loadFaProfiles();
    var myModal = new bootstrap.Modal(document.getElementById('faIdSentModal'), {});
    myModal.show();
}

function loadFaProfiles() {
    getFaProfiles(function (profiles) {
        const profileList = document.getElementById('faProfileList');
        profileList.innerHTML = ''; // Clear previous content

        if (profiles && profiles.length > 0) {
            const table = document.createElement('table');
            table.className = 'table table-hover';
            table.innerHTML = `
                <thead>
                    <tr>
                        <th scope="col"></th>
                        <th scope="col">Name</th>
                        <th scope="col">Phone</th>
                        <th scope="col">Custom Content</th>
                    </tr>
                </thead>
                <tbody id="faProfileTableBody">
                </tbody>
            `;
            profileList.appendChild(table);

            const tableBody = document.getElementById('faProfileTableBody');

            profiles.forEach(profile => {
                const row = document.createElement('tr');
                row.dataset.profileId = profile.ProfileID;
                row.style.cursor = 'pointer';
                row.innerHTML = `
                    <td><img src="${profile.PictureUrl || 'https://via.placeholder.com/50'}" class="rounded-circle" alt="Agent Picture" width="40" height="40"></td>
                    <td>${profile.FaName}</td>
                    <td>${profile.MobilePhone || ''}</td>
                    <td>${profile.CustomContent || ''}</td>
                `;
                tableBody.appendChild(row);
            });

            tableBody.querySelectorAll('tr').forEach(row => {
                row.addEventListener('click', function () {

                    tableBody.querySelectorAll('tr').forEach(r => r.classList.remove('table-active'));

                    this.classList.add('table-active');
                });
            });

        } else {
            profileList.innerHTML = '<p class="text-muted">No Field Agent profiles found.</p>';
        }
    });
}

function getFaProfiles(callback) {
    $.ajax({
        type: "POST",
        url: "Settings.aspx/GetFaProfiles",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d.success) {
                callback(response.d.data);
            } else {
                console.error('Error loading FA profiles:', response.d.message);
                callback([]);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading FA profiles:', error);
            callback([]);
        }
    });
}

let cslDrawerInstance = null;
document.addEventListener('DOMContentLoaded', () => {
    const cslDrawerElement = document.getElementById('cslDetailsDrawer');
    if (cslDrawerElement) {
        cslDrawerInstance = new bootstrap.Offcanvas(cslDrawerElement);

        cslDrawerElement.addEventListener('hidden.bs.offcanvas', function () {
            $('.offcanvas-backdrop').remove();
        });
    }
});

$(document).on('click', '#viewCslDetailsBtn', function () {
    const customerId = $('#CustomerID').val();
    const siteId = parseInt($('#siteSelector').val(), 10) || 0;

    if (!customerId) {
        showAlert({ icon: 'error', text: 'Cannot load details: Customer ID is missing.' });
        return;
    }

    const placeholder = $('#cslAccordionPlaceholder');
    placeholder.html('<div class="text-center p-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');
    cslDrawerInstance.show();


    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetCslDrawerData",
        data: JSON.stringify({ customerId: customerId, siteId: siteId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const data = response.d;
            if (data) {
                $('#cslSiteName').text(data.SiteInfo.SiteName || 'Details');

                const accordionHtml = populateCslDrawer(data);
                placeholder.html(accordionHtml);
            } else {
                placeholder.html('<div class="alert alert-warning">Could not load customer details.</div>');
            }
        },
        error: function () {
            placeholder.html('<div class="alert alert-danger">An error occurred while fetching data.</div>');
        }
    });
});

function populateCslDrawer(data) {
    const safe = (val) => val || 'N/A';

    const basicInfoHtml = `
        <table class="table table-sm table-bordered">
            <tbody>
                <tr><th>Customer</th><td>${safe(data.CustomerInfo.FirstName)} ${safe(data.CustomerInfo.LastName)}</td></tr>
                <tr><th>Contact</th><td>${safe(data.SiteInfo.FirstName)} ${safe(data.SiteInfo.LastName)}</td></tr>
                <tr><th>Phone</th><td><a href="tel:${safe(data.SiteInfo.PhoneNumber)}">${safe(data.SiteInfo.PhoneNumber)}</a></td></tr>
                <tr><th>Email</th><td><a href="mailto:${safe(data.SiteInfo.Email)}">${safe(data.SiteInfo.Email)}</a></td></tr>
                <tr><th>Address</th><td>${safe(data.SiteInfo.Address)}</td></tr>
                <tr><th>Status</th><td>${data.SiteInfo.IsActive ? 'Active' : 'Inactive'}</td></tr>
            </tbody>
        </table>
    `;

    let appointmentsHtml = '<p>No appointments found.</p>';
    if (data.Appointments && data.Appointments.length > 0) {
        appointmentsHtml = `
            <ul class="list-group">
                ${data.Appointments.slice(0, 5).map(appt => `
                    <li class="list-group-item">
                        ${safe(appt.RequestDate)} - ${safe(appt.ServiceType)}
                        <span class="badge bg-info float-end">${safe(appt.AppoinmentStatus)}</span>
                    </li>
                `).join('')}
                ${data.Appointments.length > 5 ? '<li class="list-group-item text-center">...and more</li>' : ''}
            </ul>`;
    }


    let invoicesHtml = '<p>No invoices found.</p>';
    if (data.Invoices && data.Invoices.length > 0) {
        invoicesHtml = `
             <ul class="list-group">
                ${data.Invoices.slice(0, 5).map(inv => `
                    <li class="list-group-item">
                        #${safe(inv.InvoiceNumber)} - ${safe(inv.InvoiceDate)}
                        <span class="badge bg-secondary float-end">$${safe(inv.Total)}</span>
                    </li>
                `).join('')}
            </ul>`;
    }


    let equipmentHtml = '<p>No equipment found.</p>';
    if (data.Equipment && data.Equipment.length > 0) {
        equipmentHtml = `
             <ul class="list-group">
                ${data.Equipment.slice(0, 5).map(eq => `
                    <li class="list-group-item">
                        ${safe(eq.EquipmentType)}
                        <small class="text-muted d-block">S/N: ${safe(eq.SerialNumber)}</small>
                    </li>
                `).join('')}
            </ul>`;
    }

    return `
        <div class="accordion" id="cslAccordionDynamic">
            ${createAccordionItem('One', 'Basic Information', 'fa-user', basicInfoHtml, true)}
            ${createAccordionItem('Two', 'Appointments', 'fa-calendar-check', appointmentsHtml, false)}
            ${createAccordionItem('Three', 'Invoices/Estimates', 'fa-file-invoice-dollar', invoicesHtml, false)}
            ${createAccordionItem('Four', 'Equipment', 'fa-tools', equipmentHtml, false)}
        </div>
    `;
}

function createAccordionItem(id, title, icon, content, expanded) {
    return `
        <div class="accordion-item">
            <h2 class="accordion-header" id="heading${id}">
                <button class="accordion-button ${expanded ? '' : 'collapsed'}" type="button" data-bs-toggle="collapse" data-bs-target="#collapse${id}" aria-expanded="${expanded}" aria-controls="collapse${id}">
                    <i class="fas ${icon} me-2"></i>${title}
                </button>
            </h2>
            <div id="collapse${id}" class="accordion-collapse collapse ${expanded ? 'show' : ''}" aria-labelledby="heading${id}" data-bs-parent="#cslAccordionDynamic">
                <div class="accordion-body">
                    ${content}
                </div>
            </div>
        </div>
    `;
}

document.getElementById('sendFaIdButton').addEventListener('click', function () {
    const selectedProfileElement = document.querySelector('#faProfileTableBody .table-active');
    if (selectedProfileElement) {
        const profileId = selectedProfileElement.dataset.profileId;
        const appointmentId = currentEditId;
        console.log(`Sending FA-ID for appointment ${appointmentId} to profile ${profileId}`);

        alert(`FA-ID for appointment ${appointmentId} would be sent to profile ${profileId}.`);

        if (currentFaIdButton) {
            currentFaIdButton.classList.remove('btn-outline-primary');
            currentFaIdButton.classList.add('btn-success'); // Green background
            currentFaIdButton.innerHTML = '<i class="fas fa-check me-1"></i>FA-ID Sent'; // Tick mark
            currentFaIdButton.disabled = true; // Optionally disable it
        }

        var myModal = bootstrap.Modal.getInstance(document.getElementById('faIdSentModal'));
        myModal.hide();
    } else {
        alert('Please select a Field Agent profile.');
    }
});

function openAppointmentModal() {
    $('#editModal').modal('show');
}

function getFormStatusClass(status) {
    switch (status?.toLowerCase()) {
        case 'completed': return 'text-success';
        case 'inprogress': return 'text-info';
        case 'submitted': return 'text-primary';
        default: return 'text-warning';
    }
}

