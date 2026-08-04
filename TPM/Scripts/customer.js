
let table = null;
var sites = [];
var siteAppointmentsCache = {};
var IsSiteDataLoading = true;

// --- Filter state persistence (ported from the FSM CSL board) ---

// The status dropdown is an <asp:DropDownList> inside ContentPlaceHolderID="MainContent",
// so it actually renders as #MainContent_statusFilter. Always resolve it through this
// helper: a bare $('#statusFilter') matches nothing and silently turns the filter into a
// no-op, which is exactly how it behaved before.
function $statusFilterEl() {
    return $('#statusFilter').length ? $('#statusFilter') : $('#MainContent_statusFilter');
}

// Key is TPM-specific on purpose: TPM and FSM both run on http://localhost:62934 during
// development, so a shared key would let one app read the other's saved filters.
var TPM_FILTER_STATE_KEY = 'tpmCustomerFilterState';

function getFilterState() {
    try {
        var saved = sessionStorage.getItem(TPM_FILTER_STATE_KEY);
        return saved ? JSON.parse(saved) : null;
    } catch (e) {
        return null;
    }
}

function saveFilterState() {
    var dt = $.fn.DataTable.isDataTable('#customerTable') ? $('#customerTable').DataTable() : null;
    var state = {
        search: dt ? dt.search() : '',
        page: dt ? dt.page() : 0,
        pageLength: dt ? dt.page.len() : 10,
        statusFilter: $statusFilterEl().val() || '',
        hideNA: $('#hideNA').is(':checked')
    };
    sessionStorage.setItem(TPM_FILTER_STATE_KEY, JSON.stringify(state));
}

function clearFilterState() {
    sessionStorage.removeItem(TPM_FILTER_STATE_KEY);
}

function escapeHTML(str) {
    return String(str ?? '').replace(/[&<>"']/g, s => (
        { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[s]
    ));
}

function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'flex';
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

function getDateRangeFromSelection(value) {
    var today = new Date();
    var formatDate = function (d) {
        var yyyy = d.getFullYear();
        var mm = String(d.getMonth() + 1).padStart(2, '0');
        var dd = String(d.getDate()).padStart(2, '0');
        return yyyy + '-' + mm + '-' + dd;
    };
    switch (value) {
        case 'today':
            var d = formatDate(today);
            return { startDate: d, endDate: d };
        case 'this_week':
            var sun = new Date(today);
            sun.setDate(today.getDate() - today.getDay());
            var sat = new Date(sun);
            sat.setDate(sun.getDate() + 6);
            return { startDate: formatDate(sun), endDate: formatDate(sat) };
        case 'this_month':
            var ms = new Date(today.getFullYear(), today.getMonth(), 1);
            var me = new Date(today.getFullYear(), today.getMonth() + 1, 0);
            return { startDate: formatDate(ms), endDate: formatDate(me) };
        case 'this_year':
            var ys = new Date(today.getFullYear(), 0, 1);
            var ye = new Date(today.getFullYear(), 11, 31);
            return { startDate: formatDate(ys), endDate: formatDate(ye) };
        case 'custom':
            return {
                startDate: $('#siteApptDateFrom').val() || '',
                endDate: $('#siteApptDateTo').val() || ''
            };
        default:
            return { startDate: '', endDate: '' };
    }
}

function loadSiteApptStatuses() {
    $.ajax({
        url: "Customer.aspx/GetAppointmentStatuses",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: "{}",
        success: function (response) {
            var select = $('#siteApptStatusFilter');
            select.find('option:gt(0)').remove();
            var statuses = response.d || [];
            statuses.forEach(function (s) {
                select.append('<option value="' + escapeHTML(s.StatusID) + '">' + escapeHTML(s.StatusName) + '</option>');
            });
            // Restore saved filter values after dropdown is populated
            var savedRange = sessionStorage.getItem('siteApptDateRangeSelect');
            var savedStatus = sessionStorage.getItem('siteApptStatusFilter');
            if (savedRange) {
                $('#siteApptDateRangeSelect').val(savedRange);
                if (savedRange === 'custom') {
                    $('#customDateRange').show();
                    var savedFrom = sessionStorage.getItem('siteApptDateFrom');
                    var savedTo = sessionStorage.getItem('siteApptDateTo');
                    if (savedFrom) $('#siteApptDateFrom').val(savedFrom);
                    if (savedTo) $('#siteApptDateTo').val(savedTo);
                }
            }
            if (savedStatus) $('#siteApptStatusFilter').val(savedStatus);
        }
    });
}

function saveSiteFiltersToSession() {
    sessionStorage.setItem('siteApptDateRangeSelect', $('#siteApptDateRangeSelect').val() || '');
    sessionStorage.setItem('siteApptDateFrom', $('#siteApptDateFrom').val() || '');
    sessionStorage.setItem('siteApptDateTo', $('#siteApptDateTo').val() || '');
    sessionStorage.setItem('siteApptStatusFilter', $('#siteApptStatusFilter').val() || '');
}

function clearSiteFiltersFromSession() {
    sessionStorage.removeItem('siteApptDateRangeSelect');
    sessionStorage.removeItem('siteApptDateFrom');
    sessionStorage.removeItem('siteApptDateTo');
    sessionStorage.removeItem('siteApptStatusFilter');
}

$(document).ready(function () {

    // Restore the dropdown/toggle from the previous visit BEFORE loadCustomers(), so the
    // very first ajax call already carries the saved status filter instead of firing once
    // with the default and again after restore.
    var savedFilterState = getFilterState();
    if (savedFilterState) {
        if (savedFilterState.statusFilter) {
            $statusFilterEl().val(savedFilterState.statusFilter);
        }
        if (typeof savedFilterState.hideNA === 'boolean') {
            $('#hideNA').prop('checked', savedFilterState.hideNA);
        }
    }

    // Persist filters when leaving for the details page, so coming back lands on the same
    // search term, page and status.
    $(document).on('click', 'a[href*="CustomerDetails.aspx"]', function () {
        saveFilterState();
    });

    loadCustomers();
    loadSiteApptStatuses();

    $('.cust-section-toggle').on('click', function () {
        const sectionId = $(this).data('section');
        const content = $('#' + sectionId);
        const isActive = content.is(':visible');

        $('.cust-section-content').slideUp();
        $('.cust-section-toggle').removeClass('active');

        if (!isActive) {
            content.slideDown();
            $(this).addClass('active');
        }
    });
    loadDropdownDataForModal();
    $(document).on('click', '.cust-appt-row', function () {
        const siteId = $(this).data('site-id');
        const apptIndex = $(this).data('appt-index');
        if (siteAppointmentsCache[siteId] && typeof siteAppointmentsCache[siteId][apptIndex] !== 'undefined') {
            const appointment = siteAppointmentsCache[siteId][apptIndex];
            showAppointmentDetailsModal(appointment, siteId);
        } else {
            console.error('Could not find appointment data', siteId, apptIndex, siteAppointmentsCache);
            alert('An error occurred while retrieving appointment details.');
        }
    });

    $('#customerTable tbody').on('click', 'tr', function () {
        $('#contact, #sites').slideDown();
        $('#contactBtn, #sitesBtn').addClass('active');
    });

    // SMS from the appointment modal. FSM opens its own apptSmsModal backed by
    // Customer.aspx/SendCustomerSMS; TPM has neither, but it does have CustomerChatHistory.aspx,
    // which is the same affordance on TPM's own plumbing (and matches the grid's SMS button).
    $(document).on('click', '#custModal_smsMobile', function (e) {
        e.preventDefault();
        const mobile = ($('#custModal_Mobile').val() || '').trim();
        const name = ($('#custModal_CustomerName').val() || '').trim();
        const customerId = ($('#editCustomerId').val() || $('#CustomerID').val() || '').toString().trim();
        OpenCustomerChatHistory(mobile, name, customerId);
    });

    $('#addCustomerBtn').on('click', function () {
        $('#addCustomerForm')[0].reset();
        openModal('addCustomerModal');
    });

    $('#closeAddCustomer, #closeAddCustomerIcon').on('click', function () {
        closeModal('addCustomerModal');
    });


    $('#editCustomerBtn').on('click', function () {
        const customerData = table.row({ selected: true }).data();
        if (customerData) {
            populateAndOpenEditCustomerModal(customerData);
        } else {
            alert('Please select a customer to edit.');
        }
    });


    $('#customerTable tbody').on('click', '.cust-table-edit-btn', function (e) {
        e.stopPropagation();
        const customerData = table.row($(this).closest('tr')).data();
        if (customerData) {
            populateAndOpenEditCustomerModal(customerData);
        }
    });


    $('#closeEditCustomer, #closeEditCustomerIcon').on('click', function () {
        closeModal('editCustomerModal');
    });


    $('#addCustomerForm').on('submit', function (event) {
        event.preventDefault();
        if (validateCustomerForm()) {
            const customer = {
                FirstName: $('input[name="firstName"]').val().trim(),
                LastName: $('input[name="lastName"]').val().trim(),
                Email: $('input[name="email"]').val().trim(),
                Phone: $('input[name="phone"]').val().trim()
            };

            $.ajax({
                type: "POST",
                url: "Customer.aspx/AddCustomer",
                data: JSON.stringify({ customer: customer }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        alert("Customer added successfully!");
                        closeModal('addCustomerModal');
                        table.ajax.reload(null, false);
                    } else {
                        alert("Failed to add customer.");
                    }
                },
                error: function (xhr) {
                    console.error("Error adding customer: ", xhr.responseText);
                    alert("An error occurred while adding the customer.");
                }
            });
        }
    });

    $('#editCustomerForm').on('submit', function (event) {
        event.preventDefault();
        if (validateCustomerForm()) {
            const customer = {
                CustomerID: $(this).data('customerId'),
                CustomerGuid: $(this).data('customerGuid'),
                FirstName: $('#editFirstName').val().trim(),
                LastName: $('#editLastName').val().trim(),
                Email: $('#editEmail').val().trim(),
                Phone: $('#editPhone').val().trim()
            };

            $.ajax({
                type: "POST",
                url: "Customer.aspx/UpdateCustomer",
                data: JSON.stringify({ customer: customer }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        alert("Customer updated successfully!");
                        closeModal('editCustomerModal');
                        table.ajax.reload(null, false);
                    } else {
                        alert("Failed to update customer.");
                    }
                },
                error: function (xhr) {
                    console.error("Error updating customer: ", xhr.responseText);
                    alert("An error occurred while updating the customer.");
                }
            });
        }
    });



    const statesData = {
        USA: ["Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"],
        Canada: ["Alberta", "British Columbia", "Manitoba", "New Brunswick", "Newfoundland and Labrador", "Nova Scotia", "Northwest Territories", "Nunavut", "Ontario", "Prince Edward Island", "Quebec", "Saskatchewan", "Yukon"]
    };

    function updateStates(country, selectedState) {
        const stateDropdown = $('#state');
        stateDropdown.empty().append((statesData[country] || []).map(state => new Option(state, state)));
        if (selectedState) {
            stateDropdown.val(selectedState);
        }
    }

    function updateZipLabel(country) {
        $('#zipLabel').text(country === 'Canada' ? 'Postal Code' : 'Zip Code');
    }


    $('#country').on('change', function () {
        const selectedCountry = $(this).val();
        updateStates(selectedCountry);
        updateZipLabel(selectedCountry);
    });


    $('#sites').on('click', '#addSiteBtn', function () {
        $('#addSiteForm')[0].reset();
        $('.cust-modal-title').text('Add Site');
        $('.cust-modal-submit').text('Save');
        $('#SiteId').val(0);

        const defaultCountry = 'USA';
        $('#country').val(defaultCountry);
        updateStates(defaultCountry);
        updateZipLabel(defaultCountry);

        $('#CustomerID').val($('#CustomerID').val());
        $('#CustomerGuid').val($('#CustomerGuid').val());
        $('#isActive').prop('checked', true);

        openModal('addSiteModal');
        updateIsActiveLabel();
    });
    
    $('#sites').on('click', '.cust-site-Duplicate-btn', function () {
        const siteId = $(this).data('siteid');
     
        const customerId = $(this).attr('data-CustomerID');

        const Sitename = $(this).attr('data-Site-Name'); 

        if ($.fn.DataTable.isDataTable('#DuplicatecustomerSiteTable')) {

            $('#DuplicatecustomerSiteTable').DataTable().destroy();
            $('#DuplicatecustomerSiteTable').empty(); // Manually empty the table's DOM
        }



        $('#DuplicatecustomerSiteTable').on('error.dt', function (e, settings, techNote, message) {
            alert(message)
            console.error('An error has been reported by DataTables: ', message);
        });

        $('#DuplicatecustomerSiteTable').DataTable({
            processing: true,
            serverSide: true,
            filter: true,
            select: {
                style: 'none'
            },
            ajax: {
                url: "Customer.aspx/GetDuplicatecustomerSiteTable",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",

                data: function (d) {
                    // Save current filter values on every request
                   
                    return JSON.stringify({
                        customerId: customerId,
                        siteId: siteId,
                        Sitename: Sitename
                    });
                },

                dataSrc: function (json) {
                    if (json.error) {
                        alert("Error loading customers: " + json.error);
                        return [];
                    }
                    return json.data;
                },
                error: function (err) {
                    console.log(err);
                }
            },
            paging: true,
            pageLength: 10,
            select: { style: 'single' },
            columns: [

                {
                    data: "SiteName",
                    name: "Select Main Site",
                    autoWidth: true,
                    render: function (data, type, row) {
                        return `<input type="radio" name="row-selection" value="${row.id}">`;

                    }
                },
                {
                    data: 'id', // Map to your data property
                    render: function (data, type, row) {
                        // Return a checkbox with the ID as the value
                        return `<input type="checkbox" name="id" value="${row.id}">`;
                    },
                    orderable: false // Prevent sorting on the checkbox column
                },
                { data: 'SiteName' },
                { data: 'Address' },
                { data: 'PhoneNumber'}

            ]
        });
       
        openModal('mdl_CheckDuplicate');
       
    });

    $('#sites').on('click', '.cust-site-edit-btn', function () {
        const siteId = $(this).data('site-id');
        const isDefault = $(this).data('is-default') === true;
        // alert(siteId);
        const site = sites.find(s => String(s.Id) === String(siteId));
        if (!site) {
            alert('Error: Could not find site data.');
            return;
        }

        $('.cust-modal-title').text(isDefault ? 'Edit Default Site (Customer Info)' : 'Edit Site');
        $('.cust-modal-submit').text('Update');

        // For default site, disable Site Name editing or make it read-only
        if (isDefault) {
            $('#siteName').prop('readonly', true).addClass('bg-light');
        } else {
            $('#siteName').prop('readonly', false).removeClass('bg-light');
        }
        
        
        $('#SiteId').val(site.Id);
        $('#CustomerID').val(site.CustomerID);
        $('#CustomerGuid').val(site.CustomerGuid);
        $('#siteName').val(site.SiteName || '');
        $('#firstName').val(site.FirstName || '');
        $('#lastName').val(site.LastName || '');
        $('#phoneNumber').val(site.PhoneNumber || '');
        $('#email').val(site.Email || '');
        $('#address').val(site.Address || '');
        $('#zip').val(site.Zip || '');
        $('#note').val(site.Note || '');
        $('#isActive').prop('checked', !!site.IsActive);

        const country = site.Country || 'USA';
        $('#country').val(country);
        updateStates(country, site.State);
        updateZipLabel(country);

        openModal('addSiteModal');
        updateIsActiveLabel();
    });


    $('#closeAddSite, #closeAddSiteIcon').on('click', function () {
        closeModal('addSiteModal');
    });
    $('#btn_CloseCheckDuplicate, #close_mdl_CheckDuplicate').on('click', function () {
        closeModal('mdl_CheckDuplicate');
    });

    

    // Site appointment filter - Search button
    $('#siteFilterSearchBtn').on('click', function () {
        saveSiteFiltersToSession();
        if ($.fn.DataTable.isDataTable('#customerSiteTable')) {
            $('#customerSiteTable').DataTable().draw();
        }
    });

    // Site appointment filter - Clear button
    $('#siteFilterClearBtn').on('click', function () {
        $('#siteApptDateRangeSelect').val('');
        $('#siteApptDateFrom').val('');
        $('#siteApptDateTo').val('');
        $('#customDateRange').hide();
        $('#siteApptStatusFilter').val('');
        clearSiteFiltersFromSession();
        if ($.fn.DataTable.isDataTable('#customerSiteTable')) {
            $('#customerSiteTable').DataTable().draw();
        }
    });

    // Show/hide custom date range and auto-search on dropdown change
    $('#siteApptDateRangeSelect').on('change', function () {
        if ($(this).val() === 'custom') {
            $('#customDateRange').show();
        } else {
            $('#customDateRange').hide();
            $('#siteApptDateFrom').val('');
            $('#siteApptDateTo').val('');
            saveSiteFiltersToSession();
            if ($.fn.DataTable.isDataTable('#customerSiteTable')) {
                $('#customerSiteTable').DataTable().draw();
            }
        }
    });

    // Delegated + both IDs, because the ASP.NET control renders as #MainContent_statusFilter.
    // This must re-query the server (not table.draw) now that the status filter is applied in
    // SQL - a client-side redraw would only re-filter the 10 rows already on screen.
    $(document).on('change', '#statusFilter, #MainContent_statusFilter', function () {
        if (table) table.ajax.reload();
    });

    $('#cslViewFilter').on('change', function () {
        if (table) {
            table.draw(false);
        }
    });

    $('#hideNA').on('change', function () {
        if (table) {
            table.ajax.reload(null, false);
        }
    });

    $('#customerTable').on('draw.dt.statusFilter', function () {
        applyRowFiltersOnCurrentPage();
        selectFirstVisibleRow();
    });

    $('#sites').on('click', '.cust-site-appts-toggle', function () {
        const siteId = parseInt($(this).data('site-id'), 10);
        const apptsEl = $(`#site-appts-${siteId}`);

        loadSiteAppointments(siteId, apptsEl);

    });


    updateStates($('#country').val());
    updateZipLabel($('#country').val());
});
$('#sites').on('click', '.cust-site-delete-btn', function () {
    const siteId = $(this).data('site-id');
    const isDefault = $(this).data('is-default') === true;
    const site = sites.find(s => String(s.Id) === String(siteId));

    if (!site) {
        alert('Error: Could not find site data to delete.');
        return;
    }

    // Prevent deletion of default site
    if (isDefault || siteId === 0 || siteId === '0') {
        alert('The default site cannot be deleted. It represents the primary customer location.');
        return;
    }

    // Use a confirmation dialog before deleting
    if (confirm(`Are you sure you want to delete the site "${site.SiteName}"? This action cannot be undone.`)) {
        $.ajax({
            type: "POST",
            url: "Customer.aspx/DeleteCustomerSite",
            data: JSON.stringify({ siteId: siteId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                if (response.d) {
                    alert("Site deleted successfully!");
                    loadCustomerSiteData(site.CustomerID);
                } else {
                    alert("Something went wrong while deleting the site.");
                }
            },
            error: function (xhr) {
                console.error("Error deleting site: ", xhr.responseText);
                alert("An error occurred while deleting the site.");
            }
        });
    }
});
function loadCustomers() {
    IsSiteDataLoading = true;

    if ($.fn.DataTable.isDataTable('#customerTable')) {

        $('#customerTable').DataTable().destroy();
        $('#customerTable').empty(); // Manually empty the table's DOM
    }

    const sitesHeaderContainer = $('#sites .sites-header');
    const sitesListContainer = $('#sites .sites-list');

    sitesHeaderContainer.empty();
    sitesListContainer.empty();

    sitesHeaderContainer.append('<h4 class="cust-details-title" >Select a Customer to view sites.</h4>');

    // Restore search text / page from the previous visit (e.g. returning from CustomerDetails).
    var savedState = getFilterState();
    var initSearch = (savedState && savedState.search) ? savedState.search : '';
    var initPage = (savedState && savedState.page) ? savedState.page : 0;

    // Assign to the module-level `table`. Without this it stayed null forever, so every
    // `if (table) table.ajax.reload()` guard (status filter, hideNA toggle, view filter)
    // silently did nothing and the filters looked wired but were inert.
    table = $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        filter: true,
        search: { search: initSearch },
        displayStart: initPage * 10,
        ajax: {
            url: "Customer.aspx/LoadCustomers",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: function (d) {
                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    searchValue: d.search.value,
                    sortColumn: d.columns[d.order[0].column].data,
                    sortDirection: d.order[0].dir,
                    cslViewFilter: 'all', // No longer using $('#cslViewFilter').val()
                    hideNoAppointments: $('#hideNA').is(':checked'),
                    statusFilter: $statusFilterEl().val() || ''
                });
            },
            dataSrc: function (json) {
                if (json.error) {
                    alert("Error loading customers: " + json.error);
                    return [];
                }
                return json.data;
            }
        },
        paging: true,
        pageLength: 10,
        select: { style: 'single' },
        columns: [
            { data: "fullname", name: "TP Name", autoWidth: true },
            {
                data: "Email",
                name: "Email",
                autoWidth: true,
                render: function (data) {
                    if (!data) { return ''; }
                    return `<a href="mailto:${escapeHTML(data)}" class="email-link">${escapeHTML(data)}</a>`;
                }
            },
            {
                data: "StatusName",
                name: "Status",
                autoWidth: true,
                render: function (data, type, row) {
                    // Colour comes from tbl_Status.CalenderColor via LoadCustomers, same as FSM CSL,
                    // so a company's configured status colours carry over instead of a hardcoded
                    // class list that silently falls through on spelling drift (In-Route vs InRoute,
                    // Cancelled vs Canceled).
                    let statusText = data || 'N/A';
                    if (statusText === '0') { statusText = 'Multiple'; }
                    const bgColor = row.StatusColor || '#3b82f6';
                    return `<span class="badge" style="background-color: ${bgColor}; color: #fff; padding: 5px 10px; font-weight: 500; font-size: 0.75rem; border-radius: 4px;">${escapeHTML(statusText)}</span>`;
                }
            },
            {
                data: null,
                orderable: false,
                width: "100px",
                render: function (data, type, row) {
                    const smsBtn = `<button class="cust-action-btn sms-btn" title="Send SMS" onclick="OpenCustomerChatHistory('${escapeHTML(row.Phone)}', '${escapeHTML(row.FirstName + " " + row.LastName)}', '${escapeHTML(row.CustomerID)}')"><i class="fa fa-comment-dots"></i></button>`;
                    const editBtn = `<button class="cust-table-edit-btn" title="Edit Customer"><i class="fa-solid fa-user-pen"></i></button>`;
                    return `<div class="cust-action-btns">${smsBtn}${editBtn}</div>`;
                }
            }
        ],
        drawCallback: function () {
            var api = this.api();
            if (api.rows({ page: 'current' }).count() > 0 && !$('#customerTable tbody tr.selected').length) {
                var savedCustomerId = sessionStorage.getItem('selectedCustomerID');
                if (savedCustomerId) {
                    api.rows({ page: 'current' }).every(function () {
                        var rowData = this.data();
                        if (rowData && rowData.CustomerID == savedCustomerId) {
                            this.select();
                            IsSiteDataLoading = false;
                            generateCustomerDetails(rowData);
                        }
                    });
                } else {
                    IsSiteDataLoading = false;
                }
            }
        },
        initComplete: function () {
            // State has been consumed by search/displayStart above; drop it so a plain
            // reload of the page starts clean instead of resurrecting an old search.
            clearFilterState();

            // Debounce the search box: DataTables fires a server round-trip on every
            // keystroke by default, which on a server-side grid means a query per letter.
            var api = this.api();
            var searchInput = $('div.dataTables_filter input');
            searchInput.off('keyup search input').on('keyup', function () {
                var self = this;
                clearTimeout(self._searchTimer);
                self._searchTimer = setTimeout(function () {
                    if (api.search() !== self.value) {
                        api.search(self.value).draw();
                    }
                }, 400);
            });
        }
    });

    $('#customerTable tbody').on('click', 'tr', function () {
        if ($(this).hasClass('selected')) return;
        var data = $('#customerTable').DataTable().row(this).data();
        if (data) {

            IsSiteDataLoading = false;
            sessionStorage.setItem('selectedCustomerID', data.CustomerID);
            generateCustomerDetails(data);
        }
    });
}
function generateCustomerDetails(data) {
    if (!data) {
        $('#customerName').text('Select a Customer');

        $('.ci-item').addClass('is-empty');
        $('#customerPhone, #customerMobile, #customerEmail, #customerJobTitle').text('-');
        // The address is a set of spans, not one #customerAddress node - clear each of them.
        $('#customerAddress1, #customerAddress2, #customerCityStateZip, #customerCountry').text('');

        $('#sites .sites-header').empty();
        $('#sites .sites-list').empty().html('<p class="text-muted">Select a customer to see their sites.</p>');
        return;
    }
    loadCustomerSiteData(data.CustomerID);
    const safe = (v) => v || '';
    const normPhone = (v) => safe(v).replace(/[^\d+]/g, '');


    $('#customerName').text([safe(data.FirstName), safe(data.LastName)].filter(Boolean).join(' '));


    const updateItem = (id, value, href = null) => {
        const container = $(`#${id}-container`);
        const valueEl = $(`#${id}`);

        if (value && value.trim() !== '') {
            const content = href ? `<a href="${href}" target="_blank">${escapeHTML(value)}</a>` : escapeHTML(value);
            valueEl.html(content);
            container.removeClass('is-empty');
        } else {
            valueEl.text('-');
            container.addClass('is-empty');
        }
    };


    updateItem('customerPhone', data.Phone, `tel:${normPhone(data.Phone)}`);
    updateItem('customerMobile', data.Mobile, `sms:${normPhone(data.Mobile)}`);
    updateItem('customerEmail', data.Email, `mailto:${data.Email}`);

    // Structured address block, mirroring FSM CSL: street on its own line, then
    // "City, State, Zip", then country. The old single-line join lost Address2 entirely.
    $('#customerAddress1').text(safe(data.Address1));
    $('#customerAddress2').text(safe(data.Address2));
    const cityStateZip = [safe(data.City), safe(data.State), safe(data.ZipCode)].filter(Boolean).join(', ');
    $('#customerCityStateZip').text(cityStateZip);
    $('#customerCountry').text(safe(data.Country));

    const customerAddressContainer = $('#customerAddress-container');
    if (safe(data.Address1) || safe(data.Address2) || safe(data.City) || safe(data.State) || safe(data.ZipCode) || safe(data.Country)) {
        customerAddressContainer.removeClass('is-empty');
    } else {
        customerAddressContainer.addClass('is-empty');
    }

    updateItem('customerJobTitle', data.JobTitle);

    $('#CustomerID').val(safe(data.CustomerID));
    $('#CustomerGuid').val(safe(data.CustomerGuid));


}
function showSpinner() {
    $('#loading-spinner').show();
    //  document.getElementById('loading-spinner').hide();
}
hideSpinner();
// Function to hide the spinner
function hideSpinner() {

    $('#loading-spinner').hide();
}
function loadCustomerSiteData(customerId) {
    IsSiteDataLoading = true;

    if ($.fn.DataTable.isDataTable('#customerSiteTable')) {

        $('#customerSiteTable').DataTable().destroy();
        $('#customerSiteTable').empty(); // Manually empty the table's DOM
    }





    $('#customerSiteTable').DataTable({
        processing: true,
        serverSide: true,
        filter: true,
        ajax: {
            url: "Customer.aspx/GetCustomerSiteData",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",

            data: function (d) {
                // Save current filter values on every request
                saveSiteFiltersToSession();
                return JSON.stringify({
                    customerId: customerId,
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    searchValue: d.search.value,
                    sortColumn: d.columns[d.order[0].column].data,
                    sortDirection: d.order[0].dir,
                    appointmentStartDate: getDateRangeFromSelection($('#siteApptDateRangeSelect').val()).startDate,
                    appointmentEndDate: getDateRangeFromSelection($('#siteApptDateRangeSelect').val()).endDate,
                    appointmentStatus: $('#siteApptStatusFilter').val() || ''
                });
            },

            dataSrc: function (json) {
                if (json.error) {
                    alert("Error loading customers: " + json.error);
                    return [];
                }
                return json.data;
            }
        },
        paging: true,
        pageLength: 10,
        select: { style: 'single' },
        columns: [

            {
                data: "SiteName",
                name: "Status",
                autoWidth: true,
                render: function (data, type, row) {
                    // 'data' is the value from the 'myDataField' of the data source
                    // You can append HTML elements as needed
                    var site = row;
                    sites.push(site);
                    const isDefaultSite = site.Id === 0;
                    const statusClass = site.IsActive ? 'active' : 'inactive';
                    const statusTitle = site.IsActive ? 'Active' : 'Inactive';
                    console.log(site)
                    console.log(site.Id)
                    const editButton = `
                        <button class="cust-site-icon-btn cust-site-edit-btn" title="Edit Site" data-site-id="${site.Id}" data-is-default="${isDefaultSite}">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path d="M5.433 13.917l1.262-3.155A4 4 0 017.58 9.42l6.92-6.918a2.121 2.121 0 013 3l-6.92 6.918c-.383.383-.84.685-1.343.886l-3.154 1.262a.5.5 0 01-.65-.65z" /><path d="M3.5 5.75c0-.69.56-1.25 1.25-1.25H10A.75.75 0 0010 3H4.75A2.75 2.75 0 002 5.75v9.5A2.75 2.75 0 004.75 18h9.5A2.75 2.75 0 0017 15.25V10a.75.75 0 00-1.5 0v5.25c0 .69-.56 1.25-1.25 1.25h-9.5c-.69 0-1.25-.56-1.25-1.25v-9.5z" /></svg>
                        </button>`;

                    const deleteButton = `
                        <button class="cust-site-icon-btn delete-btn cust-site-delete-btn" title="${isDefaultSite ? 'Default site cannot be deleted' : 'Delete Site'}" data-site-id="${site.Id}" data-is-default="${isDefaultSite}" ${isDefaultSite ? 'disabled style="opacity:0.5; cursor:not-allowed;"' : ''}>
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.58.22-2.365.468a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193v-.443A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd" /></svg>
                        </button>`;
                    
                 

                    const siteCardHTML = `
                        <div class="cust-site-card" data-site-id="${site.Id}">
                            <div class="cust-site-header">
                                <div class="cust-site-title-group">
                                    <div class="cust-site-status-indicator ${statusClass}" title="${statusTitle}"></div>
                                    <h3 class="cust-site-title">${escapeHTML(site.SiteName)}</h3>
                                </div>
                                <div class="cust-site-actions">
                                    <button class="cust-site-icon-btn cust-site-Duplicate-btn" title="Check Duplicate"  data-Site-Name="${site.SiteName}"  data-siteid="${site.Id}" data-CustomerID="${site.CustomerID}" >
                                   <i class="fa fa-eye"></i> </button>
                                   
                                    ${editButton}
                                    ${deleteButton}
                                    <a href="CustomerDetails.aspx?siteId=${site.Id}&custId=${encodeURIComponent(site.CustomerID)}" class="cust-site-icon-btn ${!site.IsActive ? 'd-none' : ''}" title="View Details">
                                        <i class="fa fa-arrow-right"></i>
                                    </a>
                                </div>
                            </div>
                            <div class="cust-site-body">
                             <p class="cust-site-info">
                             <i class="fas fa-map-marker-alt fa-fw"></i>
                        ${[
                            escapeHTML(site.Address),
                            escapeHTML(site.State),
                            escapeHTML(site.Zip),
                            escapeHTML(site.Country)
                        ].filter(Boolean).join(', ') || '-'}
                               </p>
                                <p class="cust-site-info"> <i class="fas fa-user fa-fw"></i> ${escapeHTML(site.FirstName || '')} ${escapeHTML(site.LastName || '')}</p>
                                <p class="cust-site-info"> <i class="fas fa-envelope fa-fw"></i> ${site.Email ? `<a href="mailto:${site.Email}">${escapeHTML(site.Email)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-phone-alt fa-fw"></i> ${site.PhoneNumber ? `<a href="tel:${site.PhoneNumber}">${escapeHTML(site.PhoneNumber)}</a>` : '-'}</p>
                            </div>
                            <div class="cust-site-footer">
                                <button class="cust-site-appts-toggle" data-site-id="${site.Id}">
                                    Appointments <span class="appointment-count" id="appt-count-${site.Id}">${site.TotalAppointment}</span>
                                </button>
                                <div class="cust-site-appts" id="site-appts-${site.Id}" data-loaded="false" ></div>
                            </div>
                        </div> `;
                    //const sitesListContainer = $('#sites .sites-list');
                    //sitesListContainer.append(siteCardHTML);


                    return siteCardHTML;
                }
            }

        ],
        drawCallback: function () {


            //IsSiteDataLoading = false;
            //hideSpinner();

            siteAppointmentsCache = {};

            const sitesHeaderContainer = $('#sites .sites-header');


            sitesHeaderContainer.empty();

            sitesHeaderContainer.append('<button id="addSiteBtn" type="button">+ Add Site</button>');



            var api = this.api();
            if (api.rows({ page: 'current' }).count() > 0 && !$('#customerTable tbody tr.selected').length) {
                //   selectFirstVisibleRow(); // Re-added this line
                IsSiteDataLoading = false;
            }
        }
    });

}
function loadCustomerSiteData_EX(customerId) {
    if (!customerId) return;
    if (IsSiteDataLoading) return;

    showSpinner();
    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetCustomerSiteData",
        data: JSON.stringify({ customerId: customerId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            IsSiteDataLoading = false;
            hideSpinner();
            sites = response.d || [];
            siteAppointmentsCache = {};

            const sitesHeaderContainer = $('#sites .sites-header');
            const sitesListContainer = $('#sites .sites-list');

            sitesHeaderContainer.empty();
            sitesListContainer.empty();
            sitesHeaderContainer.append('<button id="addSiteBtn" type="button">+ Add Site</button>');

            if (sites.length > 0) {
                sites.forEach(site => {
                    const isDefaultSite = site.Id === 0;
                    const statusClass = site.IsActive ? 'active' : 'inactive';
                    const statusTitle = site.IsActive ? 'Active' : 'Inactive';

                    const editButton = `
                        <button class="cust-site-icon-btn cust-site-edit-btn" title="Edit Site" data-site-id="${site.Id}" data-is-default="${isDefaultSite}">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path d="M5.433 13.917l1.262-3.155A4 4 0 017.58 9.42l6.92-6.918a2.121 2.121 0 013 3l-6.92 6.918c-.383.383-.84.685-1.343.886l-3.154 1.262a.5.5 0 01-.65-.65z" /><path d="M3.5 5.75c0-.69.56-1.25 1.25-1.25H10A.75.75 0 0010 3H4.75A2.75 2.75 0 002 5.75v9.5A2.75 2.75 0 004.75 18h9.5A2.75 2.75 0 0017 15.25V10a.75.75 0 00-1.5 0v5.25c0 .69-.56 1.25-1.25 1.25h-9.5c-.69 0-1.25-.56-1.25-1.25v-9.5z" /></svg>
                        </button>`;

                    const deleteButton = `
                        <button class="cust-site-icon-btn delete-btn cust-site-delete-btn" title="${isDefaultSite ? 'Default site cannot be deleted' : 'Delete Site'}" data-site-id="${site.Id}" data-is-default="${isDefaultSite}" ${isDefaultSite ? 'disabled style="opacity:0.5; cursor:not-allowed;"' : ''}>
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.58.22-2.365.468a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193v-.443A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd" /></svg>
                        </button>`;

                    const siteCardHTML = `
                        <div class="cust-site-card" data-site-id="${site.Id}">
                            <div class="cust-site-header">
                                <div class="cust-site-title-group">
                                    <div class="cust-site-status-indicator ${statusClass}" title="${statusTitle}"></div>
                                    <h3 class="cust-site-title">${escapeHTML(site.SiteName)}</h3>
                                </div>
                                <div class="cust-site-actions">    
                                    ${editButton}
                                    ${deleteButton}
                                    <a href="CustomerDetails.aspx?siteId=${site.Id}&custId=${encodeURIComponent(site.CustomerID)}" class="cust-site-icon-btn ${!site.IsActive ? 'd-none' : ''}" title="View Details">
                                        <i class="fa fa-arrow-right"></i>
                                    </a>
                                </div>
                            </div>
                            <div class="cust-site-body">
                             <p class="cust-site-info">
                             <i class="fas fa-map-marker-alt fa-fw"></i>
                        ${[
                            escapeHTML(site.Address),
                            escapeHTML(site.State),
                            escapeHTML(site.Zip),
                            escapeHTML(site.Country)
                        ].filter(Boolean).join(', ') || '-'}
                               </p>
                                <p class="cust-site-info"> <i class="fas fa-user fa-fw"></i> ${escapeHTML(site.FirstName || '')} ${escapeHTML(site.LastName || '')}</p>
                                <p class="cust-site-info"> <i class="fas fa-envelope fa-fw"></i> ${site.Email ? `<a href="mailto:${site.Email}">${escapeHTML(site.Email)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-phone-alt fa-fw"></i> ${site.PhoneNumber ? `<a href="tel:${site.PhoneNumber}">${escapeHTML(site.PhoneNumber)}</a>` : '-'}</p>
                            </div>
                            <div class="cust-site-footer">
                                <button class="cust-site-appts-toggle" data-site-id="${site.Id}">
                                    Appointments <span class="appointment-count" id="appt-count-${site.Id}">...</span>
                                </button>
                                <div class="cust-site-appts" id="site-appts-${site.Id}" data-loaded="false" style="display:none;"></div>
                            </div>
                        </div>`;
                    sitesListContainer.append(siteCardHTML);
                    // loadAppointmentCount(site.CustomerID, site.Id);
                });
            } else {
                hideSpinner();
                sitesListContainer.append('<p class="text-muted">No sites have been added for this customer.</p>');
            }
        },
        error: function (xhr) {
            hideSpinner();
            console.error("Error loading site data: ", xhr.responseText);
            $('#sites').html('<p class="text-danger">Failed to load site data.</p>');
        }
    });
}

function loadCustomerSiteData_OLD(customerId) {
    if (!customerId) return;
    if (IsSiteDataLoading) return;

    showSpinner();
    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetCustomerSiteData",
        data: JSON.stringify({ customerId: customerId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            IsSiteDataLoading = false;
            hideSpinner();
            sites = response.d || [];
            siteAppointmentsCache = {};

            const sitesHeaderContainer = $('#sites .sites-header');
            const sitesListContainer = $('#sites .sites-list');

            sitesHeaderContainer.empty();
            sitesListContainer.empty();
            sitesHeaderContainer.append('<button id="addSiteBtn" type="button">+ Add Site</button>');

            if (sites.length > 0) {
                sites.forEach(site => {
                    const isDefaultSite = site.Id === 0;
                    const statusClass = site.IsActive ? 'active' : 'inactive';
                    const statusTitle = site.IsActive ? 'Active' : 'Inactive';

                    const editButton = `
                        <button class="cust-site-icon-btn cust-site-edit-btn" title="Edit Site" data-site-id="${site.Id}" data-is-default="${isDefaultSite}">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path d="M5.433 13.917l1.262-3.155A4 4 0 017.58 9.42l6.92-6.918a2.121 2.121 0 013 3l-6.92 6.918c-.383.383-.84.685-1.343.886l-3.154 1.262a.5.5 0 01-.65-.65z" /><path d="M3.5 5.75c0-.69.56-1.25 1.25-1.25H10A.75.75 0 0010 3H4.75A2.75 2.75 0 002 5.75v9.5A2.75 2.75 0 004.75 18h9.5A2.75 2.75 0 0017 15.25V10a.75.75 0 00-1.5 0v5.25c0 .69-.56 1.25-1.25 1.25h-9.5c-.69 0-1.25-.56-1.25-1.25v-9.5z" /></svg>
                        </button>`;

                    const deleteButton = `
                        <button class="cust-site-icon-btn delete-btn cust-site-delete-btn" title="${isDefaultSite ? 'Default site cannot be deleted' : 'Delete Site'}" data-site-id="${site.Id}" data-is-default="${isDefaultSite}" ${isDefaultSite ? 'disabled style="opacity:0.5; cursor:not-allowed;"' : ''}>
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.58.22-2.365.468a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193v-.443A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd" /></svg>
                        </button>`;

                    const siteCardHTML = `
                        <div class="cust-site-card" data-site-id="${site.Id}">
                            <div class="cust-site-header">
                                <div class="cust-site-title-group">
                                    <div class="cust-site-status-indicator ${statusClass}" title="${statusTitle}"></div>
                                    <h3 class="cust-site-title">${escapeHTML(site.SiteName)}</h3>
                                </div>
                                <div class="cust-site-actions">    
                                    ${editButton}
                                    ${deleteButton}
                                    <a href="CustomerDetails.aspx?siteId=${site.Id}&custId=${encodeURIComponent(site.CustomerID)}" class="cust-site-icon-btn ${!site.IsActive ? 'd-none' : ''}" title="View Details">
                                        <i class="fa fa-arrow-right"></i>
                                    </a>
                                </div>
                            </div>
                            <div class="cust-site-body">
                             <p class="cust-site-info">
                             <i class="fas fa-map-marker-alt fa-fw"></i>
                        ${[
                            escapeHTML(site.Address),
                            escapeHTML(site.State),
                            escapeHTML(site.Zip),
                            escapeHTML(site.Country)
                        ].filter(Boolean).join(', ') || '-'}
                               </p>
                                <p class="cust-site-info"> <i class="fas fa-user fa-fw"></i> ${escapeHTML(site.FirstName || '')} ${escapeHTML(site.LastName || '')}</p>
                                <p class="cust-site-info"> <i class="fas fa-envelope fa-fw"></i> ${site.Email ? `<a href="mailto:${site.Email}">${escapeHTML(site.Email)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-phone-alt fa-fw"></i> ${site.PhoneNumber ? `<a href="tel:${site.PhoneNumber}">${escapeHTML(site.PhoneNumber)}</a>` : '-'}</p>
                            </div>
                            <div class="cust-site-footer">
                                <button class="cust-site-appts-toggle" data-site-id="${site.Id}">
                                    Appointments <span class="appointment-count" id="appt-count-${site.Id}">...</span>
                                </button>
                                <div class="cust-site-appts" id="site-appts-${site.Id}" data-loaded="false" style="display:none;"></div>
                            </div>
                        </div>`;
                    sitesListContainer.append(siteCardHTML);
                    // loadAppointmentCount(site.CustomerID, site.Id);
                });
            } else {
                hideSpinner();
                sitesListContainer.append('<p class="text-muted">No sites have been added for this customer.</p>');
            }
        },
        error: function (xhr) {
            hideSpinner();
            console.error("Error loading site data: ", xhr.responseText);
            $('#sites').html('<p class="text-danger">Failed to load site data.</p>');
        }
    });
}


function loadAppointmentCount(customerId, siteId) {
    const countEl = $(`#appt-count-${siteId}`);

    // Check cache first
    if (siteAppointmentsCache[siteId]) {
        countEl.text(siteAppointmentsCache[siteId].length);
        return;
    }

    $.ajax({
        type: 'POST',
        url: 'CustomerDetails.aspx/GetCustomerAppoinmets',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ customerId: customerId, siteId: siteId }),
        success: function (resp) {
            const list = resp && Array.isArray(resp.d) ? resp.d : [];
            siteAppointmentsCache[siteId] = list;
            countEl.text(list.length);
        },
        error: function () {
            countEl.text('!');
        }
    });
}
function updateIsActiveLabel() {
    const isChecked = $('#isActive').is(':checked');
    $('#isActiveText').text(isChecked ? 'Active' : 'Deactivated');
}

$('#addSiteModal').on('change', '#isActive', function () {
    updateIsActiveLabel();
});

function saveSite(event) {
    event.preventDefault();
    if (validateSiteForm()) {
        const siteId = parseInt($('#SiteId').val());
        const isDefaultSite = siteId === 0 && $('.cust-modal-title').text().includes('Default');

        const site = {
            Id: siteId,
            CustomerID: $('#CustomerID').val(),
            CustomerGuid: $('#CustomerGuid').val(),
            SiteName: $('#siteName').val().trim(),
            FirstName: $('#firstName').val().trim(),
            LastName: $('#lastName').val().trim(),
            PhoneNumber: $('#phoneNumber').val().trim(),
            Email: $('#email').val().trim(),
            Address: $('#address').val().trim(),
            Country: $('#country').val(),
            State: $('#state').val(),
            Zip: $('#zip').val().trim(),
            Note: $('#note').val().trim(),
            IsActive: $("#isActive").is(":checked")
        };

        // For default site (Id=0 when editing), update customer record instead
        if (isDefaultSite) {
            $.ajax({
                type: "POST",
                url: "Customer.aspx/UpdateCustomerFromDefaultSite",
                data: JSON.stringify({ site: site }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        alert('Default site (customer information) updated successfully!');
                        closeModal('addSiteModal');
                        location.reload(); // Reload to refresh customer data
                    } else {
                        alert("Something went wrong while updating the customer information.");
                    }
                },
                error: function (xhr) {
                    console.error("Error updating customer: ", xhr.responseText);
                    alert("An error occurred while updating the customer information.");
                }
            });
        } else {
            $.ajax({
                type: "POST",
                url: "Customer.aspx/SaveCustomerSiteData",
                data: JSON.stringify({ site: site }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        alert(`Site ${site.Id > 0 ? 'updated' : 'saved'} successfully!`);
                        closeModal('addSiteModal');
                        loadCustomerSiteData(site.CustomerID);
                    } else {
                        alert("Something went wrong while saving the site.");
                    }
                },
                error: function (xhr) {
                    console.error("Error saving site: ", xhr.responseText);
                    alert("An error occurred while saving the site.");
                }
            });
        }
    }
}

function validateSiteForm() {
    let errorMessage = "";
    if ($("#siteName").val().trim() === "") errorMessage += "Site Name is required.\n";
    if ($("#address").val().trim() === "") errorMessage += "Street Address is required.\n";
    if (errorMessage) {
        alert(errorMessage);
        return false;
    }
    return true;
}

function validateCustomerForm() {
    let errorMessage = "";
    if ($("#editFirstName").val().trim() === "") errorMessage += "First Name is required.\n";
    if ($("#editLastName").val().trim() === "") errorMessage += "Last Name is required.\n";
    if ($("#editEmail").val().trim() === "") {
        errorMessage += "Email is required.\n";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test($("#editEmail").val().trim())) {
        errorMessage += "Invalid email format.\n";
    }
    if (errorMessage) {
        alert(errorMessage);
        return false;
    }
    return true;
}

function populateAndOpenEditCustomerModal(customerData) {
    $('#editFirstName').val(customerData.FirstName || '');
    $('#editLastName').val(customerData.LastName || '');
    $('#editEmail').val(customerData.Email || '');
    $('#editPhone').val(customerData.Phone || '');
    $('#editCustomerForm').data('customerId', customerData.CustomerID);
    $('#editCustomerForm').data('customerGuid', customerData.CustomerGuid);
    openModal('editCustomerModal');
}

function applyRowFiltersOnCurrentPage() {
    if (!table) return;
    // '' (Active appointments) and 'all_inclusive' are not real row statuses - the server
    // already applied them - so treat both as "no client-side status filter".
    const rawStatus = $statusFilterEl().val();
    const wantedStatus = (!rawStatus || rawStatus === 'all_inclusive') ? 'all' : rawStatus.toLowerCase();
    const hideNA = $('#hideNA').is(':checked');

    table.rows({ page: 'current' }).every(function () {
        const data = this.data();
        const status = (data && data.StatusName ? data.StatusName : 'n/a').toLowerCase();
        let visible = true;
        if (wantedStatus !== 'all' && status !== wantedStatus) {
            visible = false;
        }
        $(this.node()).toggle(visible);
    });
}

function selectFirstVisibleRow() {
    if (!table) return;
    const firstVisibleRow = $('#customerTable tbody tr:visible').first();
    if (firstVisibleRow.length) {
        table.rows().deselect();
        const row = table.row(firstVisibleRow);
        row.select();
        generateCustomerDetails(row.data());
    } else {

        generateCustomerDetails(null);
    }
}
function loadSiteAppointments(siteId, containerEl) {
    const customerId = $('#CustomerID').val();
    if (!customerId) {
        containerEl.html('<div class="text-danger small">Missing customer ID.</div>');
        return;
    }
    // $('#ApptListModal').show();

    containerEl.html('<div class="text-muted small">Loading appointments…</div>');

    //if (siteAppointmentsCache[siteId]) {
    //    renderSiteAppointments(siteId, siteAppointmentsCache[siteId], containerEl);
    //    containerEl.data('loaded', true);
    //    return;
    //}

    $.ajax({
        type: 'POST',
        url: 'AppoinementList.aspx/GetCustomerAppoinmetsForView',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ customerId: customerId, siteId: siteId }),
        success: function (resp) {
            const list = resp && Array.isArray(resp.d) ? resp.d : [];
            siteAppointmentsCache[siteId] = list;
            renderSiteAppointments(siteId, list, containerEl);
            containerEl.data('loaded', true);
        },
        error: function (xhr) {
            console.error('GetCustomerAppoinmets failed:', xhr.responseText);
            containerEl.html('<div class="text-danger small">Failed to load appointments.</div>');
        }
    });
}

function showAppointmentDetailsModal(appointment, siteId) {
    if (!appointment) return;

    // Ensure dropdowns are populated
    populateDropdown("MainContent_ServiceTypeFilter_Edit", cslServiceTypes, "ServiceTypeID", "ServiceName", "Select Service Type");
    populateDropdown("resource_list", cslResources, "Id", "Name", "Unassigned");
    // populateDropdown adds its placeholder with an EMPTY value, and the line below wants an
    // explicit "0" for Unassigned - without this drop we end up with two "Unassigned" entries
    // and a selected value of "" rather than "0".
    $('#resource_list option').filter(function () { return this.value === ''; }).remove();
    if (!$('#resource_list option[value="0"]').length) {
        $('#resource_list').prepend(new Option("Unassigned", "0"));
    }
    populateDropdown("MainContent_StatusTypeFilter_Edit", cslApptStatuses, "StatusID", "StatusName", "Select Status");
    populateDropdown("MainContent_TicketStatusFilter_Edit", cslTicketStatuses, "StatusID", "StatusName", "Select Ticket Status");
    populateTimeSlotDropdown(allTimeSlots);

    // Extract raw ID if it's formatted (e.g. APPT-103-5432 -> 5432)
    let apptId = appointment.AppoinmentId;
    if (apptId && typeof apptId === 'string' && apptId.includes('-')) {
        apptId = apptId.split('-').pop();
    }

    // Fetch full details
    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetAppointmentDetails",
        data: JSON.stringify({ appointmentId: apptId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const details = response.d;
            if (details) {
                $('#editApptId').val(details.ApptID);
                // Display-only: stash the CEC-facing UId so any shown.bs.modal listener shows it, not the PK.
                $('#editApptId').attr('data-uid', details.AppoinmentUId || '');
                $('#editCustomerId').val(details.CustomerID);
                $('#editAppointmentForm').data('site-id', siteId);

                // Appointment ID badge - prefer the CEC-facing AppoinmentUId, fall back to the numeric PK.
                var bodyApptIdDisplay = document.getElementById('bodyAppointmentIdDisplay');
                var displayApptId = details.AppoinmentUId || details.ApptID;
                if (bodyApptIdDisplay && displayApptId) {
                    bodyApptIdDisplay.textContent = '#' + displayApptId;
                }

                // Populate Customer/Site Info (Left Column)

                // GetAppointmentDetails returns ContactName/Phone/Mobile - the old code read
                // details.PhoneNumber / details.MobileNumber, which do not exist on this payload,
                // so both boxes were permanently blank.
                $('#custModal_CustomerName').val(details.ContactName || details.CustomerName || '');
                $('#custModal_SiteName').val(details.SiteName || '');
                $('#custModal_Address').val(details.Address || '');
                $('#custModal_City').val(details.City || '');
                $('#custModal_State').val(details.State || '');
                $('#custModal_Zip').val(details.Zip || '');
                $('#custModal_Country').val(details.Country || '');
                $('#custModal_Email').val(details.Email || '');
                $('#custModal_Phone').val(details.Phone || '');
                $('#custModal_Mobile').val(details.Mobile || '');

                $('#MainContent_ServiceTypeFilter_Edit').val(details.ServiceTypeID || "");

                // Stash the stored resource before selecting it. The dropdown only lists CURRENT
                // resources, so an appointment assigned to a since-removed technician selects
                // nothing - and a save would then write ResourceID = NULL and silently drop the
                // assignment. saveAppointmentChanges falls back to this value when the dropdown
                // has no selection at all (an explicit "0"/Unassigned pick is still honoured).
                $('#resource_list').attr('data-orig-resource', details.ResourceID || 0);
                $('#resource_list').val(details.ResourceID || "0");

                setDropdownByTextOrValue('MainContent_StatusTypeFilter_Edit', details.Status);
                // Remember the status the modal opened with, so the save can tell whether it actually
                // changed and only then offer "Send notification?".
                $('#MainContent_StatusTypeFilter_Edit').attr('data-orig-status', $('#MainContent_StatusTypeFilter_Edit').val());
                setDropdownByTextOrValue('MainContent_TicketStatusFilter_Edit', details.TicketStatus);

                // Match the saved TimeSlot text against a configured block, falling back to matching
                // the appointment's start time when the stored label doesn't line up.
                var timeSlotValue = (details.TimeSlot || '').trim();
                var matchingSlot = allTimeSlots.find(function (s) {
                    return (s.TimeBlockSchedule || '').trim() === timeSlotValue || (s.TimeBlock || '').trim() === timeSlotValue;
                });
                if (!matchingSlot && details.StartDateTime) {
                    var extractedStart = moment(details.StartDateTime, 'MM/DD/YYYY hh:mm A').format('hh:mm A');
                    matchingSlot = allTimeSlots.find(function (s) { return s.StartTime === extractedStart; });
                }
                $('#time_slot').val(matchingSlot ? matchingSlot.StartTime : '');

                // Prefer the stored Start/End datetimes over recomputing them from Hour/Minute.
                $('#txt_StartDate').val(details.StartDateTime || '');
                $('#txt_EndDate').val(details.EndDateTime || '');
                $('#duration').val(details.Duration || '');

                if (details.StartDateTime) {
                    var startMom = moment(details.StartDateTime, 'MM/DD/YYYY hh:mm A');
                    if (startMom.isValid()) $('#dateInput').val(startMom.format('YYYY-MM-DD'));
                    else $('#dateInput').val(details.Date || '');
                } else {
                    $('#dateInput').val(details.Date || '');
                }

                // Pull the service type's configured default duration and re-derive the end time.
                var serviceTypeId = parseInt(details.ServiceTypeID) || 0;
                if (serviceTypeId > 0) {
                    $.ajax({
                        url: "Customer.aspx/GetDuration",
                        type: "POST",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        data: JSON.stringify({ serviceTypeID: serviceTypeId }),
                        success: function (resp) {
                            var dur = resp.d || "0";
                            if (dur && dur !== "0") {
                                $('#duration').val(dur);
                                updateEndDateFromDuration();
                            }
                        }
                    });
                }

                $('#editApptNote').val(details.Note);

                // Keep date / slot / duration / start / end in step as the user edits them.
                $('#dateInput').off('change').on('change', syncModalTimes);
                $('#time_slot').off('change').on('change', syncModalTimes);
                $('#duration').off('change').on('change', updateEndDateFromDuration);
                $('#txt_StartDate').off('change').on('change', calculateTimeRequired);
                $('#txt_EndDate').off('change').on('change', calculateTimeRequired);
                $('#MainContent_ServiceTypeFilter_Edit').off('change').on('change', function () {
                    var stId = parseInt($(this).val()) || 0;
                    if (stId <= 0) return;
                    $.ajax({
                        url: "Customer.aspx/GetDuration",
                        type: "POST",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        data: JSON.stringify({ serviceTypeID: stId }),
                        success: function (resp) {
                            var dur = resp.d || "0";
                            if (dur && dur !== "0") {
                                $('#duration').val(dur);
                                updateEndDateFromDuration();
                            }
                        }
                    });
                });

                // Reset Tabs
                const tabTrigger = new bootstrap.Tab(document.querySelector('#editAppointmentTabs button[data-bs-target="#appointment-details"]'));
                tabTrigger.show();

                loadCustomFields(null, details.ApptID);
                loadFormsForModal(details.ApptID);

                // Use Bootstrap modal syntax
                $('#siteAppointmentDetailsModal').modal('show');
            } else {
                alert("Failed to load appointment details.");
            }
        },
        error: function (xhr) {
            console.error("Error fetching details", xhr.responseText);
            alert("Error fetching appointment details.");
        }
    });
}

let cslServiceTypes = [];
let cslResources = [];
let cslApptStatuses = [];
let cslTicketStatuses = [];

function loadDropdownDataForModal() {
    const calls = [
        { url: "Customer.aspx/GetServiceTypes", target: "cslServiceTypes" },
        { url: "Customer.aspx/GetResources", target: "cslResources" },
        { url: "Customer.aspx/GetAppointmentStatuses", target: "cslApptStatuses" },
        { url: "Customer.aspx/GetTicketStatuses", target: "cslTicketStatuses" }
    ];

    calls.forEach(call => {
        $.ajax({
            type: "POST",
            url: call.url,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                if (call.target === "cslServiceTypes") cslServiceTypes = response.d;
                if (call.target === "cslResources") cslResources = response.d;
                if (call.target === "cslApptStatuses") cslApptStatuses = response.d;
                if (call.target === "cslTicketStatuses") cslTicketStatuses = response.d;
            }
        });
    });

    // Fetch the company's configured time blocks for the Time Slot dropdown.
    getTimeSlots();
}

function populateDropdown(elementId, data, valueField, textField, defaultText) {
    const $el = $(`#${elementId}`);
    if (!$el.length) return;
    const currentVal = $el.val();
    $el.empty();
    if (defaultText) {
        $el.append(new Option(defaultText, ""));
    }
    if (data && data.length) {
        data.forEach(item => {
            $el.append(new Option(item[textField], item[valueField]));
        });
    }
}

// Time blocks configured for the company (tbl_TimeBlocks), fetched once per page load.
let allTimeSlots = [];

// Replaces the old populateTimeSlots(), which invented fixed 30-minute 8AM-8PM slots on the
// client. Those matched no configured block, so every save wrote a TimeSlot string the rest of
// the system didn't recognise and a TimeSlotId of 0.
function getTimeSlots() {
    return $.ajax({
        url: "Customer.aspx/GetTimeSlots",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: {}
    }).then(function (response) {
        var slots = response.d || [];
        slots.sort(function (a, b) {
            var parseTime = function (timeStr) {
                var match = (timeStr || '').match(/(\d+):(\d+)\s*(AM|PM)/i);
                if (!match) return 0;
                var hours = parseInt(match[1], 10);
                var mins = parseInt(match[2], 10);
                if (match[3].toUpperCase() === 'PM' && hours !== 12) hours += 12;
                if (match[3].toUpperCase() === 'AM' && hours === 12) hours = 0;
                return hours * 60 + mins;
            };
            return parseTime(a.StartTime) - parseTime(b.StartTime);
        });
        allTimeSlots = slots;
        populateTimeSlotDropdown(slots);
        return slots;
    });
}

function populateTimeSlotDropdown(slots) {
    var $dropdown = $('#time_slot');
    if (!$dropdown.length) return;
    var currentValue = $dropdown.val();
    $dropdown.empty();
    $dropdown.append('<option value="">Select Time Slot</option>');
    (slots || []).forEach(function (slot) {
        var value = slot.StartTime;
        var displayText = slot.TimeBlockSchedule || slot.TimeBlock || slot.StartTime;
        var optionHtml = '<option value="' + value + '" data-id="' + slot.ID + '" data-start="' + slot.StartTime + '" data-end="' + slot.EndTime + '">' + displayText + '</option>';
        $dropdown.append(optionHtml);
    });
    if (currentValue) $dropdown.val(currentValue);
}

// Date + Time Slot -> Start/End datetime, honouring the Time Required box when it has a value
// and falling back to the block's own EndTime otherwise.
function syncModalTimes() {
    var dateValue = $('#dateInput').val();
    var timeSlotValue = $('#time_slot').val();

    if (!dateValue || !timeSlotValue) return;

    var selectedSlot = allTimeSlots.find(function (slot) { return slot.StartTime === timeSlotValue; }) ||
                       allTimeSlots.find(function (slot) { return slot.TimeBlock === timeSlotValue; });
    if (!selectedSlot) return;

    var startTimeStr = selectedSlot.StartTime;
    if (!startTimeStr) {
        var timeMatch = (selectedSlot.TimeBlockSchedule || '').match(/(\d{1,2}:\d{2}\s*[AP]M)/);
        if (!timeMatch) return;
        startTimeStr = timeMatch[0];
    }

    var newStartDateTime = moment(dateValue + ' ' + startTimeStr, 'YYYY-MM-DD hh:mm A');
    if (!newStartDateTime.isValid()) return;

    $('#txt_StartDate').val(newStartDateTime.format('MM/DD/YYYY hh:mm A'));

    var totalDur = parseDuration($('#duration').val() || '');
    if (totalDur > 0) {
        $('#txt_EndDate').val(newStartDateTime.clone().add(totalDur, 'minutes').format('MM/DD/YYYY hh:mm A'));
    } else if (selectedSlot.EndTime) {
        var endMom = moment(dateValue + ' ' + selectedSlot.EndTime, 'YYYY-MM-DD hh:mm A');
        if (endMom.isValid()) {
            $('#txt_EndDate').val(endMom.format('MM/DD/YYYY hh:mm A'));
        }
    }
}

function updateEndDateFromDuration() {
    var start = moment($('#txt_StartDate').val(), 'MM/DD/YYYY hh:mm A');
    if (!start.isValid()) return;

    var totalMinutes = parseDuration($('#duration').val() || '');
    if (totalMinutes <= 0) return;

    $('#txt_EndDate').val(start.clone().add(totalMinutes, 'minutes').format('MM/DD/YYYY hh:mm A'));
    calculateTimeRequired();
}

// Start/End -> "N Hr : N Min", with the inline warning when End precedes Start.
function calculateTimeRequired() {
    const $start = $('#txt_StartDate');
    const $end = $('#txt_EndDate');
    const $duration = $('#duration');
    const $error = $('#customer_EndDate');

    const start = moment($start.val(), 'MM/DD/YYYY hh:mm A');
    const end = moment($end.val(), 'MM/DD/YYYY hh:mm A');

    if (!start.isValid() || !end.isValid()) {
        if ($duration.length) $duration.val('');
        return;
    }

    if (end.isBefore(start)) {
        if ($error.length) $error.show();
        $end.css('border-color', 'red');
        if ($duration.length) $duration.val('Invalid');
        return;
    }

    if ($error.length) $error.hide();
    $end.css('border-color', '');

    const diff = moment.duration(end.diff(start));
    const hours = Math.floor(diff.asHours());
    const minutes = diff.minutes();

    if ($duration.length) $duration.val(`${hours} Hr : ${minutes} Min`);
}

// Moving the Date picker shifts the day of Start/End while keeping their times.
function updateDate(event) {
    const newDate = event.target.value; // YYYY-MM-DD
    if (!newDate) return;

    const $start = $('#txt_StartDate');
    const $end = $('#txt_EndDate');
    const mNewDate = moment(newDate, "YYYY-MM-DD");

    [$start, $end].forEach($el => {
        if ($el.val()) {
            let curMoment = moment($el.val(), "MM/DD/YYYY hh:mm A");
            if (curMoment.isValid()) {
                curMoment.year(mNewDate.year()).month(mNewDate.month()).date(mNewDate.date());
                $el.val(curMoment.format("MM/DD/YYYY hh:mm A"));
            }
        } else {
            let timeStr = $el.attr('id') === 'txt_StartDate' ? "08:00 AM" : "09:00 AM";
            $el.val(mNewDate.format("MM/DD/YYYY") + " " + timeStr);
        }
    });

    calculateTimeRequired();
}

// ---------------------------------------------------------------------------
// Appointment save (ported from FSM CSL)
// ---------------------------------------------------------------------------

// showAlert lives in forms.js, which isn't loaded on this page - so define the same wrapper
// here (SweetAlert when available, native alert fallback). Guarded so forms.js's copy wins if
// it is ever present. Must RETURN a thenable: callers chain .then(r => r.isConfirmed).
window.showAlert = window.showAlert || function (options) {
    if (typeof Swal !== 'undefined') { return Swal.fire(options); }
    if (options && options.showCancelButton) {
        return Promise.resolve({ isConfirmed: window.confirm(options.text || options.title || '') });
    }
    window.alert((options && (options.text || options.title)) || 'Alert');
    return Promise.resolve({ isConfirmed: true });
};

// Blocking overlay over the edit modal while the save round-trips, so the Update button can't
// be double-submitted.
window.showApptUpdateLoading = window.showApptUpdateLoading || function (modalId) {
    var $modal = $('#' + modalId);
    if (!$modal.length || $modal.find('.appt-update-loading').length) return;
    $modal.find('.modal-content').first().append(
        '<div class="appt-update-loading" style="position:absolute;inset:0;background:rgba(255,255,255,.65);' +
        'display:flex;align-items:center;justify-content:center;z-index:1080;">' +
        '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Saving...</span></div></div>'
    );
};
window.hideApptUpdateLoading = window.hideApptUpdateLoading || function (modalId) {
    $('#' + modalId).find('.appt-update-loading').remove();
};

// "Send notification?" confirmation shown before saving a status change. Uses Swal directly so it
// returns a promise; resolves true ("Yes, send") or false ("No, just save") - both proceed with the save.
window.confirmSendNotificationDialog = window.confirmSendNotificationDialog || function () {
    if (typeof Swal === 'undefined') {
        return Promise.resolve(confirm('Send the confirmation email / text message to the customer?'));
    }
    return Swal.fire({
        icon: 'question',
        title: 'Send notification?',
        text: 'Do you want to send the confirmation email / text message to the customer?',
        showCancelButton: true,
        confirmButtonText: 'Yes, send',
        cancelButtonText: 'No, just save',
        reverseButtons: true,
        allowEscapeKey: false,
        allowOutsideClick: false
    }).then(result => !!(result && result.isConfirmed));
};

// FSM narrows the prompt to statuses that actually have a configured template, via
// Appointments.aspx/GetNotificationEnabledStatuses. TPM has no such endpoint (its
// AppointmentStatusCommunicationProcessor has no WillSendNotificationOnStatus), so we take FSM's
// own conservative fallback: prompt on any real status change rather than risk sending silently.
window.statusChangeNeedsNotifyPrompt = window.statusChangeNeedsNotifyPrompt || function (statusId) {
    return true;
};

function saveAppointmentChanges() {
    const apptId = $('#editApptId').val();
    const siteId = $('#editAppointmentForm').data('site-id');

    if (!apptId) return;

    // .val() gives the numeric StatusID (dropdowns carry StatusID as value / StatusName as text);
    // reading .text() would send the visible name and corrupt the column.
    const statusId = parseInt($('#MainContent_StatusTypeFilter_Edit').val(), 10) || 0;
    const ticketStatusId = parseInt($('#MainContent_TicketStatusFilter_Edit').val(), 10) || 0;
    const customerId = $('#editCustomerId').val();

    const startMom = moment($('#txt_StartDate').val(), "MM/DD/YYYY hh:mm A");
    const totalMinutes = parseDuration($('#duration').val() || '');
    const durHours = Math.floor(totalMinutes / 60);
    const durMinutes = totalMinutes % 60;

    // Custom fields, scoped to the modal container so we don't sweep up stray custom_* inputs
    // elsewhere on the page.
    const customFieldValues = [];
    $('#customFieldsContainer [name^="custom_"]').each(function () {
        const fieldId = parseInt($(this).attr('name').split('_')[1]);
        if ($(this).is(':checkbox')) {
            if ($(this).is(':checked')) {
                let entry = customFieldValues.find(f => f.FieldId === fieldId);
                if (!entry) {
                    entry = { FieldId: fieldId, Value: [] };
                    customFieldValues.push(entry);
                }
                const currentVals = typeof entry.Value === 'string' ? JSON.parse(entry.Value) : entry.Value;
                currentVals.push($(this).val());
                entry.Value = JSON.stringify(currentVals);
            }
        } else {
            customFieldValues.push({ FieldId: fieldId, Value: $(this).val() });
        }
    });

    // Only send a time slot when one is genuinely selected. The placeholder option has an
    // empty value but the label "Select Time Slot", and blindly sending option:selected.text()
    // writes that literal string into tbl_Appointment.TimeSlot - Live already has rows
    // polluted exactly that way. Empty string makes the server skip the column entirely.
    const slotValue = $('#time_slot').val() || '';
    const $slotOption = $('#time_slot option:selected');

    // '' means the dropdown never matched the appointment's stored resource (it is not in the
    // current resource list) - keep what is on the row rather than clearing it. An explicit
    // '0' is the user genuinely choosing Unassigned and is passed through.
    const resourceRaw = $('#resource_list').val();
    const resourceId = (resourceRaw === '' || resourceRaw === null || resourceRaw === undefined)
        ? (parseInt($('#resource_list').attr('data-orig-resource'), 10) || 0)
        : (parseInt(resourceRaw, 10) || 0);

    const viewModel = {
        AppointmentData: {
            AppoinmentId: apptId,
            CustomerID: customerId,
            SiteId: parseInt(siteId) || 0,
            ServiceType: $('#MainContent_ServiceTypeFilter_Edit').val(),
            StatusID: statusId,
            TicketStatusID: ticketStatusId,
            ResourceID: resourceId,
            // Send the full datetime, not just the date. A "YYYY-MM-DD" string makes the
            // server's Convert.ToDateTime write ApptDateTime at midnight, silently discarding
            // the appointment's time of day on every save - which is why status/tech emails
            // that read ApptDateTime ended up announcing "12:00 AM".
            RequestDate: startMom.isValid() ? startMom.format("YYYY-MM-DD HH:mm:ss") : $('#dateInput').val(),
            TimeSlot: slotValue ? ($slotOption.text() || '').trim() : '',
            TimeSlotId: slotValue ? ($slotOption.data('id') || 0) : 0,
            Note: $('#editApptNote').val(),
            StartDateTime: $('#txt_StartDate').val(),
            EndDateTime: $('#txt_EndDate').val(),
            Hour: durHours,
            Minute: durMinutes
        },
        SiteData: null,   // this modal doesn't edit the site address; leave it untouched
        CustomFields: customFieldValues
    };

    const origStatusId = parseInt($('#MainContent_StatusTypeFilter_Edit').attr('data-orig-status'), 10) || 0;
    const statusChanged = statusId > 0 && statusId !== origStatusId;

    const performSave = (sendNotification) => {
        showApptUpdateLoading('siteAppointmentDetailsModal');
        $.ajax({
            type: "POST",
            url: "Appointments.aspx/UpdateAppointmentWithViewModel",
            data: JSON.stringify({ viewModel: viewModel, sendNotification: sendNotification }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                if (response.d) {
                    showAlert({ icon: 'success', title: 'Success!', text: 'Appointment updated successfully.' });
                    $('#siteAppointmentDetailsModal').modal('hide');
                    const $container = $(`#site-appts-${siteId}`);
                    if (siteAppointmentsCache[siteId]) delete siteAppointmentsCache[siteId];
                    if ($container.length) {
                        $container.data('loaded', false);
                        loadSiteAppointments(siteId, $container);
                        loadAppointmentCount(customerId, siteId);
                    }
                } else {
                    // Server returns false when a status that requires a resource is set without one.
                    showAlert({ icon: 'error', title: 'Error', text: 'Failed to update appointment. If you changed the status, make sure a resource is assigned.' });
                }
            },
            error: function (xhr) {
                console.error("Update failed", xhr.responseText);
                showAlert({ icon: 'error', title: 'Error', text: 'Failed to update appointment due to a server error.' });
            },
            complete: function () {
                hideApptUpdateLoading('siteAppointmentDetailsModal');
            }
        });
    };

    if (statusChanged && window.statusChangeNeedsNotifyPrompt(statusId)) {
        window.confirmSendNotificationDialog().then(performSave);
    } else {
        performSave(false);
    }
}

function setDropdownByTextOrValue(elementId, textOrVal) {
    if (!textOrVal) return;
    const $el = $(`#${elementId}`);
    if (!$el.length) return;

    // Try by value
    $el.val(textOrVal);
    if ($el.val() === textOrVal || (textOrVal && $el.val())) return;

    // Try by text
    let foundVal = "";
    $el.find('option').each(function () {
        if ($(this).text() === textOrVal) {
            foundVal = $(this).val();
            return false;
        }
    });
    if (foundVal) $el.val(foundVal);
}

function parseDuration(durationString) {
    if (!durationString) return 0;
    let totalMinutes = 0;
    const normalized = durationString.replace(/\s*:\s*/g, ' ').trim();
    const hourMatch = normalized.match(/(\d+)\s*Hr/i);
    const minuteMatch = normalized.match(/(\d+)\s*Min/i);
    if (hourMatch) totalMinutes += parseInt(hourMatch[1], 10) * 60;
    if (minuteMatch) totalMinutes += parseInt(minuteMatch[1], 10);
    return totalMinutes;
}

// FILE: customer.js
// This is the corrected function.

function renderSiteAppointments(siteId, list, containerEl) {
    if (!list || !list.length) {
        containerEl.html('<div class="text-muted small">No appointments for this site.</div>');
        return;
    }

    const customerId = $('#CustomerID').val();

    // Store index in the row for easy retrieval on click
    const rows = list.map((item, index) => {
        const status = item.AppoinmentStatus || 'N/A';
        let statusClass = 'status-na';
        const lowerCaseStatus = status.toLowerCase();

        if (lowerCaseStatus === 'confirmed' || lowerCaseStatus === 'installation in progress') {
            statusClass = 'status-confirmed';
        } else if (lowerCaseStatus === 'pending') {
            statusClass = 'status-pending';
        } else if (lowerCaseStatus === 'closed') {
            statusClass = 'status-closed';
        }

        // Changed from an <a> tag to a clickable div with data attributes
        return `
            <div class="cust-appt-row" data-site-id="${siteId}" data-appt-index="${index}" style="cursor: pointer;">
                <div class="appt-main">
                    <div class="appt-date">ID: ${escapeHTML(item.AppoinmentUId || '—')} Date: ${escapeHTML(item.AppoinmentDate || item.RequestDate || '—')} Type:${escapeHTML(item.ServiceType || '—')}</div>
                    
                </div>
                <div class="appt-status">
                    <span class="badge" style="background-color: ${item.StatusColor || '#3b82f6'} !important;">${escapeHTML(status)}</span>
                    <span class="appt-chevron" aria-hidden="true"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M9 6l6 6-6 6"></path></svg></span>
                </div>
            </div>`;
    }).join('');

    containerEl.html(`<div class="cust-appt-list">${rows}</div>`);
}



function OpenCustomerChatHistory(mobile, name, customerId) {
    if (!mobile || mobile.trim() === "") {
        if (typeof Swal !== 'undefined') {
            Swal.fire('Validation Error', 'Please insert a phone number for this customer.', 'warning');
        } else {
            alert('Please insert a phone number for this customer.');
        }
        return;
    }
    window.open(`CustomerChatHistory.aspx?mobile=${encodeURIComponent(mobile)}&name=${encodeURIComponent(name)}&customerId=${encodeURIComponent(customerId)}`, '_blank');
}
function Open_TPStatusPopup() {
    $("#m_Staus").modal("show");
}

// ---------------------------------------------------------------------------
// Custom Fields - chip UI (ported from FSM CSL / appointments.js)
// ---------------------------------------------------------------------------
// Chip-only by design: the panel never renders option lists or editable values
// inline. Values are captured on the mobile app; the office side only links or
// unlinks a field to the appointment and inspects it via the hover tooltip.
// The save serializer expects the `name="custom_{fieldId}"` hidden-input shape.

function escapeHtmlAttr(s) {
    return String(s === null || s === undefined ? '' : s)
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function loadCustomFields(form, appointmentId) {
    const container = document.getElementById("customFieldsContainer");
    if (!container) return;

    container.innerHTML = '<div class="text-center p-3">Loading custom fields...</div>';

    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetActiveCustomFields",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ apptId: appointmentId }),
        dataType: "json",
        success: function (response) {
            container.innerHTML = '';
            if (response.d && response.d.length > 0) {
                renderCustomFields(response.d, container);
            } else {
                container.innerHTML = '<div class="alert alert-info py-1 px-2 small">No active custom fields.</div>';
            }
        },
        error: function () {
            container.innerHTML = '<div class="text-danger small">Failed to load custom fields.</div>';
        }
    });
}

function renderCustomFields(fields, container) {
    container.innerHTML = `
        <label class="form-label">Custom Fields</label>
        <select id="addCustomFieldDropdown" class="form-select form-select-sm mb-2">
            <option value="">Select an option</option>
        </select>
        <div id="addedCustomFields"></div>
    `;

    const allFields = fields.slice();
    const $dropdown = $('#addCustomFieldDropdown', container);
    const $list = $('#addedCustomFields', container);

    const GROUPS = [
        { key: 'checklist', label: 'Checklist',        types: ['checklist'] },
        { key: 'dropdown',  label: 'Dropdown',         types: ['dropdown'] },
        { key: 'simple',    label: 'Text/Number/Date', types: ['text', 'number', 'date'] }
    ];
    const groupForType = (t) => GROUPS.find(g => g.types.indexOf(t) !== -1);

    GROUPS.forEach(g => {
        const wrap = document.createElement('div');
        wrap.className = 'cf-group mb-2';
        wrap.dataset.groupKey = g.key;
        wrap.style.display = 'none';
        wrap.innerHTML = `
            <small class="text-muted d-block mb-1">${g.label}</small>
            <div class="cf-group-chips d-flex flex-wrap gap-1"></div>
        `;
        $list.append(wrap);
    });

    const refreshDropdown = () => {
        const addedIds = new Set(
            Array.from($list[0].querySelectorAll('[data-field-id]')).map(el => parseInt(el.dataset.fieldId, 10))
        );
        $dropdown.empty().append('<option value="">Select an option</option>');
        allFields.forEach(f => {
            const group = groupForType(f.FieldType);
            if (!addedIds.has(f.FieldId) && group) {
                $dropdown.append(`<option value="${f.FieldId}">${escapeHTML(f.FieldName)} - ${group.label}</option>`);
            }
        });
    };

    const buildHiddenInputs = (field) => {
        const frag = document.createDocumentFragment();
        const v = field.Value;
        if (field.FieldType === 'checklist') {
            let arr = [];
            if (v !== null && v !== undefined && v !== '') {
                try { arr = JSON.parse(v) || []; } catch (e) { arr = []; }
            }
            if (arr.length > 0) {
                arr.forEach(opt => {
                    const inp = document.createElement('input');
                    inp.type = 'checkbox';
                    inp.checked = true;
                    inp.name = `custom_${field.FieldId}`;
                    inp.value = opt;
                    inp.style.display = 'none';
                    frag.appendChild(inp);
                });
            } else {
                const inp = document.createElement('input');
                inp.type = 'hidden';
                inp.name = `custom_${field.FieldId}`;
                inp.value = '[]';
                frag.appendChild(inp);
            }
        } else {
            const inp = document.createElement('input');
            inp.type = 'hidden';
            inp.name = `custom_${field.FieldId}`;
            inp.value = (v === null || v === undefined) ? '' : v;
            frag.appendChild(inp);
        }
        return frag;
    };

    const renderChip = (field) => {
        const group = groupForType(field.FieldType);
        if (!group) return;
        const groupWrap = $list[0].querySelector(`[data-group-key="${group.key}"]`);
        const chipsRow = groupWrap.querySelector('.cf-group-chips');

        const chip = document.createElement('span');
        chip.className = 'badge border bg-light text-dark d-inline-flex align-items-center cf-chip';
        chip.dataset.fieldId = field.FieldId;
        chip.dataset.fieldName = field.FieldName || '';
        chip.dataset.fieldType = field.FieldType || '';
        chip.dataset.fieldOptions = field.Options || '[]';
        chip.dataset.fieldValue = (field.Value === null || field.Value === undefined) ? '' : field.Value;
        chip.innerHTML = `${escapeHTML(field.FieldName)}
            <button type="button" class="btn-close btn-close-sm ms-2 cf-chip-remove" aria-label="Remove" style="font-size:.55em;"></button>`;
        chip.appendChild(buildHiddenInputs(field));
        chip.querySelector('.cf-chip-remove').addEventListener('click', () => {
            chip.remove();
            if (!chipsRow.querySelector('[data-field-id]')) groupWrap.style.display = 'none';
            refreshDropdown();
        });
        chipsRow.appendChild(chip);
        groupWrap.style.display = '';
    };

    fields.forEach(f => {
        if (customFieldHasValue(f)) renderChip(f);
    });
    refreshDropdown();

    $dropdown.on('change', function () {
        const fid = parseInt($(this).val(), 10);
        if (!fid) return;
        const field = allFields.find(f => f.FieldId === fid);
        if (!field) return;
        renderChip(field);
        refreshDropdown();
        $(this).val('');
    });
}

function customFieldHasValue(field) {
    // Linked = server returned a (possibly empty) FieldValue row. Unlinked = null.
    const v = field.Value;
    return v !== null && v !== undefined;
}

// --- Custom-fields chip hover tooltip -------------------------------------
function ensureCfChipTooltip() {
    let tip = document.getElementById('cfChipTooltip');
    if (!tip) {
        tip = document.createElement('div');
        tip.id = 'cfChipTooltip';
        tip.className = 'cf-chip-tooltip';
        document.body.appendChild(tip);
    }
    return tip;
}

function buildCfChipTooltipHtml(chip) {
    const name = chip.dataset.fieldName || 'Custom Field';
    const ftype = chip.dataset.fieldType || '';
    let opts = [];
    try { opts = JSON.parse(chip.dataset.fieldOptions || '[]') || []; } catch (e) { opts = []; }
    if (!Array.isArray(opts)) opts = [];
    const rawValue = chip.dataset.fieldValue || '';

    let body;
    if (ftype === 'text' || ftype === 'number' || ftype === 'date') {
        body = rawValue
            ? `<div class="cf-tt-value">${escapeHTML(rawValue)}</div>`
            : `<div class="cf-tt-empty">No value set</div>`;
    } else {
        let selected = [];
        if (ftype === 'checklist') {
            try { selected = JSON.parse(rawValue) || []; } catch (e) { selected = []; }
            if (!Array.isArray(selected)) selected = [];
        } else if (rawValue) {
            selected = [rawValue];
        }
        if (opts.length === 0) {
            body = `<div class="cf-tt-empty">No options defined</div>`;
        } else {
            body = opts.map(opt => {
                const isSel = selected.indexOf(opt) !== -1;
                const icon = ftype === 'checklist'
                    ? (isSel ? '☑' : '☐')
                    : (isSel ? '●' : '○');
                return `<div class="cf-tt-opt${isSel ? ' cf-tt-opt-sel' : ''}">${icon} ${escapeHTML(opt)}</div>`;
            }).join('');
        }
    }

    const typeLabel = {
        checklist: 'Checklist', dropdown: 'Dropdown',
        text: 'Text', number: 'Number', date: 'Date'
    }[ftype] || 'Custom Field';

    return `<div class="cf-tt-title">${escapeHTML(name)} <span class="cf-tt-type">${typeLabel}</span></div>
            <div class="cf-tt-body">${body}</div>`;
}

function positionCfChipTooltip(tip, chip) {
    const rect = chip.getBoundingClientRect();
    tip.style.top = '0px';
    tip.style.left = '0px';
    tip.style.display = 'block';
    const tipRect = tip.getBoundingClientRect();
    let top = window.scrollY + rect.top - tipRect.height - 8;
    if (top < window.scrollY + 8) top = window.scrollY + rect.bottom + 8;
    let left = window.scrollX + rect.left + (rect.width / 2) - (tipRect.width / 2);
    left = Math.max(window.scrollX + 8,
        Math.min(left, window.scrollX + window.innerWidth - tipRect.width - 8));
    tip.style.top = top + 'px';
    tip.style.left = left + 'px';
}

function showCfChipTooltip(chip) {
    const tip = ensureCfChipTooltip();
    tip.innerHTML = buildCfChipTooltipHtml(chip);
    positionCfChipTooltip(tip, chip);
}

function hideCfChipTooltip() {
    const tip = document.getElementById('cfChipTooltip');
    if (tip) tip.style.display = 'none';
}

$(document)
    .off('mouseenter.cfchip', '#customFieldsContainer .cf-chip')
    .on('mouseenter.cfchip', '#customFieldsContainer .cf-chip', function (e) {
        if (e.target && e.target.classList && e.target.classList.contains('cf-chip-remove')) return;
        showCfChipTooltip(this);
    })
    .off('mouseleave.cfchip', '#customFieldsContainer .cf-chip')
    .on('mouseleave.cfchip', '#customFieldsContainer .cf-chip', function () {
        hideCfChipTooltip();
    });

// ---------------------------------------------------------------------------
// CSL nav buttons in the appointment modal
// ---------------------------------------------------------------------------
// These open CustomerDetails.aspx in a new tab on the requested tab. They are pure
// navigation, NOT Bootstrap tabs - clicking must not switch an inline pane, which is
// why the markup omits data-bs-toggle and this handler stops propagation.
$(document).off('click.cslmodal', '#siteAppointmentDetailsModal button[id^="csl-"]')
    .on('click.cslmodal', '#siteAppointmentDetailsModal button[id^="csl-"]', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const tabId = $(this).attr('id');
        const customerId = $('#editCustomerId').val() || $('#CustomerID').val() || '';
        const siteId = parseInt($('#editAppointmentForm').data('site-id'), 10) || 0;
        if (!customerId) {
            console.warn('CSL tab clicked but customerId missing');
            return;
        }
        const tabMapping = {
            'csl-basic-tab': 'basic',
            'csl-appointments-tab': 'appointments',
            'csl-invoices-tab': 'invoices',
            'csl-notes-tab': 'notes',
            'csl-equipment-tab': 'equipment',
            'csl-pictures-tab': 'pictures',
            'csl-files-tab': 'files',
            'csl-agreements-tab': 'agreements'
        };
        const targetTab = tabMapping[tabId];
        if (targetTab) {
            const url = `CustomerDetails.aspx?custId=${encodeURIComponent(customerId)}&siteId=${siteId}&tab=${targetTab}`;
            window.open(url, '_blank');
        }
    });

// ---------------------------------------------------------------------------
// Forms lane for the SL appointment modal
// ---------------------------------------------------------------------------
// Wired to TPM's OWN forms endpoints, not FSM's. The two products diverged here:
// FSM attaches/detaches per form instance (AttachFormsToAppointment /
// DetachFormFromAppointment / UpdateFormInstanceStatus), whereas TPM replaces the whole
// attached set in one call (Appointments.aspx/UpdateAttachedForms). Rather than port FSM's
// server model into TPM, this reuses the endpoints TPM's own Appointments page already
// drives, so behaviour stays consistent across the two TPM screens.
//
// Deliberately NOT duplicated here: the inline editable form renderer + signature pad from
// appointments.js (~1500 lines). Filling a form stays on the Appointments page; from the SL
// modal you attach forms, see their status, email/SMS them, and read the customer's response.

let cslSelectedForms = [];
let cslCurrentAppointmentForms = [];

// The SL modal keys off #editApptId / #editCustomerId; the Appointments page uses
// #AppoinmentId / #CustomerID. Fall back so these helpers work on either surface.
function _cslFormsApptId() {
    return $('#editApptId').val() || $('#AppoinmentId').val() || '';
}
function _cslFormsCustomerId() {
    return $('#editCustomerId').val() || $('#CustomerID').val() || '';
}
function _cslFormsAlert(opts) {
    if (typeof showAlert === 'function') { return showAlert(opts); }
    alert((opts && (opts.text || opts.title)) || 'Error');
    return Promise.resolve({ isConfirmed: true });
}

function getFormStatusClass(status) {
    switch ((status || '').toLowerCase()) {
        case 'completed': return 'text-success';
        case 'inprogress': return 'text-info';
        case 'submitted': return 'text-primary';
        default: return 'text-warning';
    }
}

// --- Attach / detach ------------------------------------------------------

function openFormsSelectionModal(mode) {
    if (!_cslFormsApptId()) {
        _cslFormsAlert({ icon: 'warning', title: 'No Appointment', text: 'Please open an appointment first.' });
        return;
    }
    $('#formsSelectionModal').modal('show');
    loadAvailableForms();
    // Tick the boxes for what's already attached once the list has rendered.
    setTimeout(loadCurrentlySelectedForms, 250);
}

function loadAvailableForms() {
    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAllTemplates",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) populateAvailableFormsList(response.d);
        },
        error: function (xhr, status, error) {
            console.error('Error loading available forms:', error);
            $('#availableFormsList').html('<p class="text-danger">Failed to load forms.</p>');
        }
    });
}

function populateAvailableFormsList(forms) {
    const container = $('#availableFormsList');
    container.empty();

    const active = (forms || []).filter(f => f.IsActive);
    if (active.length === 0) {
        container.append('<p class="text-muted">No active form templates.</p>');
        return;
    }

    active.forEach(form => {
        const name = escapeHTML(form.TemplateName || '');
        const formItem = $(`
            <div class="form-check mb-2">
                <input class="form-check-input csl-form-cb" type="checkbox" id="form_${form.Id}"
                       value="${form.Id}" data-form-name="${escapeHtmlAttr(form.TemplateName || '')}">
                <label class="form-check-label" for="form_${form.Id}">
                    <strong>${name}</strong>
                    ${form.Description ? '<br><small class="text-muted">' + escapeHTML(form.Description) + '</small>' : ''}
                    ${form.RequireSignature ? '<br><small class="text-info"><i class="fa fa-pencil"></i> Signature Required</small>' : ''}
                </label>
            </div>
        `);
        container.append(formItem);
    });
}

// Delegated so it survives the list being re-rendered; appointments.js uses an inline
// onchange with the template name interpolated straight into the attribute, which breaks
// on any name containing a quote.
$(document).off('change.cslforms', '#availableFormsList .csl-form-cb')
    .on('change.cslforms', '#availableFormsList .csl-form-cb', function () {
        const formId = parseInt($(this).val(), 10);
        const formName = $(this).data('form-name') || '';
        toggleFormSelection(formId, formName, this.checked);
    });

function toggleFormSelection(formId, formName, isSelected) {
    if (isSelected) {
        if (!cslSelectedForms.some(f => f.id === formId)) {
            cslSelectedForms.push({ id: formId, name: formName });
        }
    } else {
        cslSelectedForms = cslSelectedForms.filter(form => form.id !== formId);
    }
    updateSelectedFormsList();
}

function updateSelectedFormsList() {
    const container = $('#selectedFormsList');
    container.empty();

    if (cslSelectedForms.length === 0) {
        container.append('<p class="text-muted">No forms selected</p>');
        return;
    }

    cslSelectedForms.forEach(form => {
        const item = $(`
            <div class="selected-form-item p-2 mb-2 border rounded">
                <div class="d-flex justify-content-between align-items-center">
                    <span>${escapeHTML(form.name)}</span>
                    <button type="button" class="btn btn-sm btn-outline-danger csl-remove-selected"
                            data-form-id="${form.id}">
                        <i class="fa fa-times"></i>
                    </button>
                </div>
            </div>
        `);
        container.append(item);
    });
}

$(document).off('click.cslforms', '.csl-remove-selected')
    .on('click.cslforms', '.csl-remove-selected', function () {
        removeSelectedForm(parseInt($(this).data('form-id'), 10));
    });

function removeSelectedForm(formId) {
    cslSelectedForms = cslSelectedForms.filter(form => form.id !== formId);
    $(`#form_${formId}`).prop('checked', false);
    updateSelectedFormsList();
}

// Reads what's currently attached and mirrors it into both the checkbox list and the
// modal's own chip strip.
function loadCurrentlySelectedForms(appointmentId) {
    if (!appointmentId) appointmentId = _cslFormsApptId();
    if (!appointmentId) return;

    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAppointmentForms",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const forms = response.d || [];
            cslSelectedForms = forms.map(f => ({ id: f.TemplateId || f.Id, name: f.TemplateName }));
            cslSelectedForms.forEach(f => { $(`#form_${f.id}`).prop('checked', true); });
            updateSelectedFormsList();
            populateAttachedFormsTab(forms);
        },
        error: function (xhr, status, error) {
            console.error('Error loading attached forms:', error);
        }
    });
}

// The chip strip inside the modal's Forms tab (#selectedFormsEdit).
function populateAttachedFormsTab(forms) {
    const container = $('#selectedFormsEdit');
    container.empty();

    if (!forms || forms.length === 0) {
        container.html('<small class="text-muted">No forms attached to this appointment</small>');
        return;
    }

    forms.forEach(form => {
        const statusClass = getFormStatusClass(form.Status);
        const badge = $(`
            <div class="form-badge p-2 mb-2 border rounded d-flex justify-content-between align-items-center">
                <div>
                    <strong>${escapeHTML(form.TemplateName || '')}</strong>
                    <br><small class="${statusClass}">Status: ${escapeHTML(form.Status || 'Pending')}</small>
                </div>
                <div>
                    ${form.RequireSignature ? '<i class="fa fa-pencil text-info" title="Signature Required"></i>' : ''}
                    ${form.RequireTip ? '<i class="fa fa-dollar text-success ms-1" title="Tip Enabled"></i>' : ''}
                </div>
            </div>
        `);
        container.append(badge);
    });
}

// Persists the selection. TPM's UpdateAttachedForms replaces the whole set, so sending the
// full id list is the detach path too.
function applyFormsSelection() {
    const appointmentId = _cslFormsApptId();
    const customerId = _cslFormsCustomerId();

    if (!appointmentId) {
        _cslFormsAlert({ icon: 'error', title: 'Error', text: 'No appointment selected' });
        return;
    }
    if (!customerId) {
        _cslFormsAlert({ icon: 'error', title: 'Error', text: 'No customer selected' });
        return;
    }

    const formIds = cslSelectedForms.map(f => f.id);

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/UpdateAttachedForms",
        data: JSON.stringify({ appointmentId: appointmentId, customerId: customerId, formIds: formIds }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                $('#formsSelectionModal').modal('hide');
                _cslFormsAlert({ icon: 'success', title: 'Saved', text: 'Attached forms updated.', timer: 2000 });
                loadFormsForModal(appointmentId);
            } else {
                _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to update attached forms.' });
            }
        },
        error: function (xhr, status, error) {
            _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to update attached forms: ' + error });
        }
    });
}

// --- View attached forms --------------------------------------------------

function loadFormsForModal(appointmentId) {
    if (!appointmentId) appointmentId = _cslFormsApptId();
    if (!appointmentId) return;

    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetAppointmentForms",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const forms = response.d || [];
            cslCurrentAppointmentForms = forms;
            cslSelectedForms = forms.map(f => ({ id: f.TemplateId || f.Id, name: f.TemplateName }));
            populateAttachedFormsTab(forms);
        },
        error: function () {
            $('#selectedFormsEdit').html('<small class="text-danger">Failed to load forms.</small>');
        }
    });
}

function openAppointmentFormsModal() {
    const appointmentId = _cslFormsApptId();
    if (!appointmentId) {
        _cslFormsAlert({ icon: 'warning', title: 'No Appointment Selected', text: 'Please select an appointment first.' });
        return;
    }

    $('#formName').empty();
    $('#formViewerContainer').empty();
    $('#formActionsContainer').hide();
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
            $("#loader").hide();
            cslCurrentAppointmentForms = response.d || [];
            populateAppointmentFormsList(cslCurrentAppointmentForms);
        },
        error: function () {
            $("#loader").hide();
            _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to load appointment forms' });
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
        const item = $(`
            <div class="form-item p-3 mb-2 border rounded csl-form-item" style="cursor:pointer;"
                 data-template-id="${form.TemplateId || form.Id}"
                 data-template-name="${escapeHtmlAttr(form.TemplateName || '')}">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${escapeHTML(form.TemplateName || '')}</strong>
                        <br><small class="text-muted">Status: <span class="${statusClass}">${escapeHTML(form.Status || 'Pending')}</span></small>
                    </div>
                    <div class="form-actions">
                        ${form.RequireSignature ? '<i class="fa fa-pencil text-info" title="Signature Required"></i>' : ''}
                        ${form.RequireTip ? '<i class="fa fa-dollar text-success ms-1" title="Tip Enabled"></i>' : ''}
                    </div>
                </div>
            </div>
        `);
        container.append(item);
    });
}

$(document).off('click.cslforms', '#appointmentFormsList .csl-form-item')
    .on('click.cslforms', '#appointmentFormsList .csl-form-item', function () {
        const templateId = parseInt($(this).data('template-id'), 10);
        $('#formName').text($(this).data('template-name') || '');
        $('#formActionsContainer').show().data('template-id', templateId);
        openCustomerResponseModal(templateId);
    });

// Read-only render of what the customer actually submitted. The editable renderer +
// signature pad stay on the Appointments page - see the note at the top of this section.
function openCustomerResponseModal(templateId) {
    if (!templateId) templateId = parseInt($('#formActionsContainer').data('template-id'), 10);
    const appointmentId = parseInt(_cslFormsApptId(), 10) || 0;
    const customerId = parseInt(_cslFormsCustomerId(), 10) || 0;

    if (!templateId) {
        _cslFormsAlert({ icon: 'warning', title: 'No Form Selected', text: 'Pick a form from the list first.' });
        return;
    }

    $("#loader").show();
    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetCustomerResponseOnForms",
        data: JSON.stringify({ templateId: templateId, appointmentId: appointmentId, customerId: customerId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            $("#loader").hide();
            let structure = null;
            try { structure = response.d ? JSON.parse(response.d) : null; } catch (e) { structure = null; }
            renderCslFormResponse(structure);
        },
        error: function () {
            $("#loader").hide();
            $('#formViewerContainer').html('<div class="alert alert-danger">Failed to load the customer response.</div>');
        }
    });
}

function renderCslFormResponse(structure) {
    const container = $('#formViewerContainer');
    container.empty();

    const fields = (structure && (structure.fields || structure.Fields)) || [];
    if (!structure || !fields.length) {
        container.html('<div class="alert alert-info">No response has been submitted for this form yet.</div>');
        return;
    }

    const rows = fields.map(f => {
        const label = f.label || f.Label || f.name || f.Name || '';
        let value = f.value !== undefined ? f.value : (f.Value !== undefined ? f.Value : '');
        if (Array.isArray(value)) value = value.join(', ');
        if (value === null || value === undefined || value === '') value = '<em class="text-muted">- not answered -</em>';
        else value = escapeHTML(String(value));
        return `<tr><th class="w-35 align-top">${escapeHTML(String(label))}</th><td>${value}</td></tr>`;
    }).join('');

    container.html(`
        <div class="table-responsive">
            <table class="table table-sm table-striped mb-0">
                <tbody>${rows}</tbody>
            </table>
        </div>
    `);
}

// --- Send to customer -----------------------------------------------------

function sendFormsViaEmail() {
    const appointmentId = _cslFormsApptId();
    if (!appointmentId) {
        _cslFormsAlert({ icon: 'error', title: 'Error', text: 'No appointment selected' });
        return;
    }
    if (cslCurrentAppointmentForms.length === 0) {
        _cslFormsAlert({ icon: 'warning', title: 'Warning', text: 'No forms attached to send' });
        return;
    }

    // The modal already shows the resolved site/customer email; fall back to a prompt.
    let customerEmail = ($('#custModal_Email').val() || '').trim();
    if (!customerEmail) {
        customerEmail = prompt('Enter customer email address:');
        if (!customerEmail) return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(customerEmail)) {
        _cslFormsAlert({ icon: 'error', title: 'Invalid Email', text: 'Please enter a valid email address' });
        return;
    }

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/SendFormsViaEmail",
        data: JSON.stringify({ appointmentId: appointmentId, customerEmail: customerEmail }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                _cslFormsAlert({ icon: 'success', title: 'Email Sent', text: `Forms have been sent to ${customerEmail} successfully!`, timer: 3000 });
            } else {
                _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to send email' });
            }
        },
        error: function (xhr, status, error) {
            _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to send email: ' + error });
        }
    });
}

function sendFormsViaSMS() {
    const appointmentId = _cslFormsApptId();
    if (!appointmentId) {
        _cslFormsAlert({ icon: 'error', title: 'Error', text: 'No appointment selected' });
        return;
    }
    if (cslCurrentAppointmentForms.length === 0) {
        _cslFormsAlert({ icon: 'warning', title: 'Warning', text: 'No forms attached to send' });
        return;
    }

    let customerPhone = ($('#custModal_Mobile').val() || $('#custModal_Phone').val() || '').trim();
    if (!customerPhone) {
        customerPhone = prompt('Enter customer mobile number:');
        if (!customerPhone) return;
    }

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/SendFormsViaSMS",
        data: JSON.stringify({ appointmentId: appointmentId, customerPhone: customerPhone }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d === true) {
                _cslFormsAlert({ icon: 'success', title: 'SMS Sent', text: `Forms have been sent to ${customerPhone} successfully!`, timer: 3000 });
            } else {
                _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to send SMS' });
            }
        },
        error: function (xhr, status, error) {
            _cslFormsAlert({ icon: 'error', title: 'Error', text: 'Failed to send SMS: ' + error });
        }
    });
}
