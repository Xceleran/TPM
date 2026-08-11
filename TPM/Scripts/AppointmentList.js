
let table = null;
var sites = [];
var currentRow = null;

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

// Swal comes from the master page; fall back to alert() if it ever fails to load.
function notify(icon, title, text) {
    if (typeof Swal !== 'undefined') {
        return Swal.fire({
            icon: icon,
            title: title,
            text: text || '',
            timer: icon === 'success' ? 1600 : undefined,
            showConfirmButton: icon !== 'success'
        });
    }
    alert(title + (text ? '\n' + text : ''));
}

// Sp_GetAppointmnetData returns ApptDateTime as CONVERT(VARCHAR(10), .., 101) -> "MM/dd/yyyy".
// Sorting that as a string puts December 2025 above September 2026, so the Date
// column sorts on a numeric yyyymmdd key instead.
function dateSortKey(value) {
    const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(String(value || '').trim());
    if (!m) return 0;
    return Number(m[3] + m[1].padStart(2, '0') + m[2].padStart(2, '0'));
}

function currentStatusFilter() {
    return ($('#statusFilter').val() || 'ALL').toLowerCase();
}

$(document).ready(function () {

    LoadAppointments();

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

    // Delegated from the table itself: DataTables replaces <tbody> on every draw,
    // so a handler bound to the tbody stops firing after the first reload.
    $('#AppointmentListTable').on('click', 'tbody tr', function () {
        if (!table) return;
        const data = table.row(this).data();
        if (!data) return; // the "No data available" placeholder row

        $('#contact, #sites').slideDown();
        $('#contactBtn, #sitesBtn').addClass('active');

        if (currentRow && currentRow.ApptID === data.ApptID) return;
        generateCustomerDetails(data);
    });

    // The right-hand pane describes the selected work order's site, and the site
    // edit modal is the one wired to a working save endpoint.
    $('#editCustomerBtn').on('click', function () {
        const site = sites && sites.length ? sites[0] : null;
        if (!site) {
            notify('warning', 'Select a work order first.');
            return;
        }
        openSiteEditModal(site);
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

    window.openSiteEditModal = function (site, isDefault) {
        isDefault = isDefault === undefined ? site.Id === 0 : isDefault;

        $('.cust-modal-title').text(isDefault ? 'Edit Default Site (Customer Info)' : 'Edit Site');
        $('#addSiteModal .cust-modal-submit').text('Update');

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
    };

    $('#country').on('change', function () {
        const selectedCountry = $(this).val();
        updateStates(selectedCountry);
        updateZipLabel(selectedCountry);
    });

    $('#sites').on('click', '.cust-site-edit-btn', function () {
        const siteId = $(this).data('site-id');
        const isDefault = $(this).data('is-default') === true;
        const site = sites.find(s => String(s.Id) === String(siteId));
        if (!site) {
            notify('error', 'Could not find site data.');
            return;
        }
        openSiteEditModal(site, isDefault);
    });

    // Both the eye icon in the header and the button in the card footer open the
    // duplicate-site check.
    $('#sites').on('click', '.cust-site-Duplicate-btn, .cust-site-appts-toggle', function () {
        openDuplicateCheck({
            customerId: $(this).attr('data-customerid'),
            siteId: $(this).attr('data-siteid'),
            siteName: $(this).attr('data-site-name')
        });
    });

    $('#close_mdl_CheckDuplicate, #clossadaseAddSite').on('click', function () {
        closeModal('mdl_CheckDuplicate');
    });

    $('#sites').on('click', '.cust-site-msgview-btn', function () {
        $('#div_Msg').html('');
        openModal('MsgViewModal');
        $('#MsgViewModal .ajax-loader').css("visibility", "visible");
        const ApptID = $(this).attr('data-site-id');
        $.ajax({
            type: "POST",
            url: "AppoinementList.aspx/Get_Message",
            data: JSON.stringify({ ApptID: ApptID }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                var messages = response.d || [];
                if (messages.length > 0 && messages[0].Note) {
                    $('#div_Msg').html(messages[0].Note);
                } else {
                    $('#div_Msg').html('<p class="text-muted">No message found.</p>');
                }
                $('#MsgViewModal .ajax-loader').css("visibility", "hidden");
            },
            error: function (xhr) {
                console.error("Error loading message: ", xhr.responseText);
                $('#div_Msg').html('<p class="text-danger">Failed to load the original message.</p>');
                $('#MsgViewModal .ajax-loader').css("visibility", "hidden");
            }
        });
    });

    $('#closeMsgView').on('click', function () {
        closeModal('MsgViewModal');
    });

    $('#closeAddSite, #closeAddSiteIcon').on('click', function () {
        closeModal('addSiteModal');
    });

    // SMS button click handler in site cards
    $('#sites').on('click', '.cust-site-SMS-btn', function () {
        var mobile = $(this).attr('data-mobilenumber-id');
        var customerId = $(this).attr('data-customer-id');
        OpenSMSPopUp(mobile, customerId);
    });

    // MMS button click handler in site cards
    $('#sites').on('click', '.cust-site-MMS-btn', function () {
        var mobile = $(this).attr('data-mobilenumber-id');
        var customerId = $(this).attr('data-customer-id');
        OpenMMSPopUp(mobile, customerId);
    });

    $('#sites').on('change', '.appt-status-select', function () {
        ApptStatusChanged_Event(this);
    });

    $('#sites').on('change', '.appt-calendar-select', function () {
        SchedulingCalendarChanged_Event(this);
    });

    $('#statusFilter').on('change', function () {
        LoadAppointments();
    });

    $('#addSiteModal').on('change', '#isActive', function () {
        updateIsActiveLabel();
    });

    updateStates($('#country').val());
    updateZipLabel($('#country').val());
});

function LoadAppointments(selectApptId) {
    // Re-initialising the grid used to destroy() and .empty() the table, which
    // strips the <thead> cells, so every filter change left the user with an
    // unlabelled grid. Build it once and refetch afterwards.
    if (table) {
        table.ajax.reload(function () { selectRowAfterLoad(selectApptId); }, true);
        return;
    }

    clearDetailsPanel();

    table = $('#AppointmentListTable').DataTable({
        processing: true,
        filter: true,
        ajax: {
            url: "AppoinementList.aspx/LoadAppointments",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: function () {
                // Read the filter here, not at init time, so a reload sends the
                // status the user has currently selected.
                return JSON.stringify({
                    SearchBy: "",
                    SearchFor: "",
                    SearchFrom: "",
                    SearchTo: "",
                    SearchCriteria: "",
                    SearchByWarrantyCompany: "",
                    wantedStatus: currentStatusFilter()
                });
            },
            dataSrc: function (json) {
                if (json.error) {
                    notify('error', 'Error loading work orders', json.error);
                    return [];
                }
                return json.data || [];
            },
            error: function (xhr) {
                console.error("LoadAppointments failed: ", xhr.responseText);
                notify('error', 'Could not load work orders', 'The server returned an error.');
            }
        },
        paging: true,
        pageLength: 10,
        order: [[2, 'desc']],
        select: { style: 'single' },
        // DataTables 2 cycles asc -> desc -> unsorted by default, and that third
        // click looks to the user like the sort has gone random.
        columnDefs: [{ targets: '_all', orderSequence: ['asc', 'desc'] }],
        columns: [
            {
                data: "CustomerName", title: "Source", autoWidth: true,
                render: function (data, type) {
                    return type === 'display' ? escapeHTML(data) : (data || '');
                }
            },
            {
                data: "SiteName", title: "Site Name", autoWidth: true,
                render: function (data, type) {
                    return type === 'display' ? escapeHTML(data) : (data || '');
                }
            },
            {
                data: "ApptDateTime", title: "Date", autoWidth: true,
                render: function (data, type) {
                    if (type === 'sort' || type === 'type') return dateSortKey(data);
                    return escapeHTML(data);
                }
            },
            {
                data: "Apptstatus", title: "Status", autoWidth: true,
                render: function (data, type) {
                    let statusText = data || 'N/A';
                    // Search and sort on the text, not on the badge markup.
                    if (type !== 'display') return statusText;

                    let statusClass = 'status-na';
                    switch (statusText.toLowerCase()) {
                        case 'pending': statusClass = 'status-pending'; break;
                        case 'accept':
                        case 'approved':
                        case 'confirmed': statusClass = 'status-confirmed'; break;
                        case 'dispatched': statusClass = 'status-dispatched'; break;
                        case 'in-route': statusClass = 'status-in-route'; break;
                        case 'fa-id sent': statusClass = 'status-fa-id-sent'; break;
                        case 'arrived': statusClass = 'status-arrived'; break;
                        case 'completed': statusClass = 'status-completed'; break;
                        case 'closed': statusClass = 'status-closed'; break;
                        case 'on-hold': statusClass = 'status-on-hold'; break;
                        case '0':
                            statusClass = 'status-default';
                            statusText = 'Multiple';
                            break;
                        case 'cancelled': statusClass = 'status-cancelled'; break;
                    }
                    return `<span class="badge ${statusClass}">${escapeHTML(statusText)}</span>`;
                }
            }
        ],
        initComplete: function () {
            selectRowAfterLoad(selectApptId);
        }
    });
}

// Keep the details pane in step with the grid: after a load, show the row the
// caller asked for, else the first one, else nothing.
function selectRowAfterLoad(apptId) {
    if (!table) return;

    const indexes = table.rows({ page: 'current' }).indexes().toArray();
    if (!indexes.length) {
        currentRow = null;
        clearDetailsPanel();
        return;
    }

    let target = indexes[0];
    if (apptId) {
        const match = indexes.find(i => String(table.row(i).data().ApptID) === String(apptId));
        if (match !== undefined) target = match;
    }

    const row = table.row(target);
    table.rows({ selected: true }).deselect();
    row.select();
    currentRow = null;
    generateCustomerDetails(row.data());
}

function clearDetailsPanel() {
    currentRow = null;
    $('#customerName').text('Select a Customer');
    $('.ci-item').addClass('is-empty');
    $('#customerPhone, #customerMobile, #customerEmail, #customerAddress, #customerJobTitle').text('-');
    $('#sites .sites-header').empty();
    $('#sites .sites-list').empty().html('<p class="text-muted">Select a work order to see its details.</p>');
}

function generateCustomerDetails(data) {
    if (!data) {
        clearDetailsPanel();
        return;
    }

    currentRow = data;

    const safe = (v) => v || '';
    const normPhone = (v) => safe(v).replace(/[^\d+]/g, '');

    $('#customerName').text(safe(data.CustomerName));

    const updateItem = (id, value, href = null) => {
        const container = $(`#${id}-container`);
        const valueEl = $(`#${id}`);

        if (value && value.trim() !== '') {
            const content = href ? `<a href="${escapeHTML(href)}" target="_blank">${escapeHTML(value)}</a>` : escapeHTML(value);
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

    const fullAddress = [safe(data.Address1), safe(data.City), safe(data.State), safe(data.ZipCode)].filter(Boolean).join(', ');
    updateItem('customerAddress', fullAddress);

    updateItem('customerJobTitle', data.JobTitle);

    $('#CustomerID').val(safe(data.CustomerID));
    $('#CustomerGuid').val(safe(data.CustomerGuid));

    loadCustomerSiteData(data.CustomerID, data.Notes, data.ApptID, data.SchedulingCal, data.IsApproved, data.SiteID, data.ApptDateTime);
}

function loadCustomerSiteData(customerId, notes, ApptID, SchedulingCal, IsApproved, SiteId, ApptDateTime) {

    if (!customerId) return;

    $.ajax({
        type: "POST",
        url: "AppoinementList.aspx/GetCustomerSiteData",
        data: JSON.stringify({ customerId: customerId, SiteId: SiteId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            sites = response.d || [];

            const sitesHeaderContainer = $('#sites .sites-header');
            const sitesListContainer = $('#sites .sites-list');

            sitesHeaderContainer.empty();
            sitesListContainer.empty();

            if (sites.length > 0) {
                sites.forEach(site => {
                    const isDefaultSite = site.Id === 0;
                    const statusClass = site.IsActive ? 'active' : 'inactive';
                    const statusTitle = site.IsActive ? 'Active' : 'Inactive';

                    const editButton = `
                        <button class="cust-site-icon-btn cust-site-edit-btn" title="Edit Site" data-site-id="${escapeHTML(site.Id)}" data-is-default="${isDefaultSite}">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path d="M5.433 13.917l1.262-3.155A4 4 0 017.58 9.42l6.92-6.918a2.121 2.121 0 013 3l-6.92 6.918c-.383.383-.84.685-1.343.886l-3.154 1.262a.5.5 0 01-.65-.65z" /><path d="M3.5 5.75c0-.69.56-1.25 1.25-1.25H10A.75.75 0 0010 3H4.75A2.75 2.75 0 002 5.75v9.5A2.75 2.75 0 004.75 18h9.5A2.75 2.75 0 0017 15.25V10a.75.75 0 00-1.5 0v5.25c0 .69-.56 1.25-1.25 1.25h-9.5c-.69 0-1.25-.56-1.25-1.25v-9.5z" /></svg>
                        </button>`;

                    const smsButton = `
                        <button class="cust-site-icon-btn cust-site-SMS-btn" title="Send SMS" data-site-id="${escapeHTML(site.Id)}" data-customer-id="${escapeHTML(site.CustomerID)}" data-MobileNumber-id="${escapeHTML(site.MobileNumber)}" data-is-default="${isDefaultSite}">
                            <i class="fa-solid fa-message"></i></button>`;
                    const mmsButton = `
                        <button class="cust-site-icon-btn cust-site-MMS-btn" title="Send MMS" data-site-id="${escapeHTML(site.Id)}" data-customer-id="${escapeHTML(site.CustomerID)}" data-MobileNumber-id="${escapeHTML(site.MobileNumber)}" data-is-default="${isDefaultSite}">
                            <i class="fa-solid fa-photo-film"></i></button>`;

                    const siteCardHTML = `
                        <div class="cust-site-card" data-site-id="${escapeHTML(site.Id)}">
                            <div class="cust-site-header">
                                <div class="cust-site-title-group">
                                    <div class="cust-site-status-indicator ${statusClass}" title="${statusTitle}"></div>
                                    <h3 class="cust-site-title">${escapeHTML(site.SiteName)}</h3>
                                </div>
                                <div class="cust-site-actions">
                                    <button class="cust-site-icon-btn cust-site-Duplicate-btn" title="Check for duplicate sites" data-Site-Name="${escapeHTML(site.SiteName)}" data-siteid="${escapeHTML(site.Id)}" data-CustomerID="${escapeHTML(site.CustomerID)}">
                                        <i class="fa fa-clone"></i></button>
                                    <button class="cust-site-icon-btn cust-site-msgview-btn" title="View Original Message" data-site-id="${escapeHTML(ApptID)}" data-is-default="${isDefaultSite}">
                                        <i class="fa fa-envelope-open-text"></i></button>
                                    ${editButton}
                                    ${smsButton}
                                    ${mmsButton}
                                    <a href="CustomerDetails.aspx?siteId=${encodeURIComponent(site.Id)}&custId=${encodeURIComponent(site.CustomerID)}" class="cust-site-icon-btn ${!site.IsActive ? 'd-none' : ''}" title="View Details">
                                        <i class="fa fa-arrow-right"></i>
                                    </a>
                                </div>
                            </div>
                            <div class="cust-site-body">
                            <div class="cust-site-address-group">
                                <p class="cust-site-info">
                                    <i class="fas fa-map-marker-alt fa-fw"></i> <strong>Street:</strong> ${escapeHTML(site.Address) || '-'}
                                </p>
                                <p class="cust-site-info">
                                    <i class="fas fa-city fa-fw"></i> <strong>City:</strong> ${escapeHTML(site.City) || '-'}
                                </p>
                                <p class="cust-site-info">
                                    <i class="fas fa-flag fa-fw"></i> <strong>State:</strong> ${escapeHTML(site.State) || '-'}
                                </p>
                                <p class="cust-site-info">
                                    <i class="fas fa-mail-bulk fa-fw"></i> <strong>Zip:</strong> ${escapeHTML(site.Zip) || '-'}
                                </p>
                                <p class="cust-site-info">
                                    <i class="fas fa-globe-americas fa-fw"></i> <strong>Country:</strong> ${escapeHTML(site.Country) || '-'}
                                </p>
                            </div>
                                <p class="cust-site-info"> <i class="fas fa-user fa-fw"></i> ${escapeHTML(site.FirstName || '')} ${escapeHTML(site.LastName || '')}</p>
                                <p class="cust-site-info"> <i class="fas fa-envelope fa-fw"></i> ${site.Email ? `<a href="mailto:${encodeURIComponent(site.Email)}" class="site-email-link" data-customer-id="${escapeHTML(site.CustomerID)}">${escapeHTML(site.Email)}</a>` : '-'}<br>Requested Date:- ${escapeHTML(ApptDateTime) || '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-phone-alt fa-fw"></i> ${site.PhoneNumber ? `<a href="tel:${escapeHTML(site.PhoneNumber)}">${escapeHTML(site.PhoneNumber)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-mobile-alt fa-fw"></i> ${site.MobileNumber ? `<a href="tel:${escapeHTML(site.MobileNumber)}">${escapeHTML(site.MobileNumber)}</a>` : '-'}</p>
                            </div>
                            <div class="cust-site-footer">
                                <button class="cust-site-appts-toggle btn-primary" data-Site-Name="${escapeHTML(site.SiteName)}" data-siteid="${escapeHTML(site.Id)}" data-CustomerID="${escapeHTML(site.CustomerID)}">
                                     Duplicate Site Check
                                </button>
                               <div class="container">
                                  <div class="row justify-content-start">
                                    <div class="col-3">
                                      Appointments Status :<select class="form-select form-select-sm appt-status-select" aria-label="Appointment status" style="width:150px;" data-appt-id="${escapeHTML(ApptID)}" data-customer-id="${escapeHTML(customerId)}" data-site-id="${escapeHTML(site.Id)}"><option value="0">Select</option><option ${IsApproved ? 'selected' : ''} value="Accept">Accept</option><option value="Confirm">Confirm</option><option ${!IsApproved ? 'selected' : ''} value="Pending">Pending</option><option value="Cancel">Cancel</option></select>
                                    </div>
                                    <div class="col-3">
                                     Scheduling Calendar :<select class="form-select form-select-sm appt-calendar-select" aria-label="Scheduling calendar" style="width:150px;" data-appt-id="${escapeHTML(ApptID)}"><option value="0">Select</option><option ${SchedulingCal == 'FSM' ? 'selected' : ''} value="FSM">FSM</option><option ${SchedulingCal == 'CEC' ? 'selected' : ''} value="CEC">CEC</option></select>
                                    </div>
                                  </div>
                                </div>
                                 Notes :<br>"${escapeHTML(notes)}"
                            </div>
                        </div>`;
                    sitesListContainer.append(siteCardHTML);
                });
            } else {
                sitesListContainer.append('<p class="text-muted">No sites have been added for this customer.</p>');
            }
        },
        error: function (xhr) {
            console.error("Error loading site data: ", xhr.responseText);
            $('#sites .sites-list').html('<p class="text-danger">Failed to load site data.</p>');
        }
    });
}

// TPM has no site-merge endpoint, so this lists the sites that share a name and
// leaves the resolving to the user.
function openDuplicateCheck(opts) {
    const tbody = $('#DuplicatecustomerSiteTable').find('tbody');
    const body = tbody.length ? tbody : $('<tbody></tbody>').appendTo('#DuplicatecustomerSiteTable');

    body.html('<tr><td colspan="2" class="text-muted">Loading…</td></tr>');
    openModal('mdl_CheckDuplicate');

    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetDuplicatecustomerSiteTable",
        data: JSON.stringify({
            customerId: opts.customerId,
            siteId: opts.siteId,
            Sitename: opts.siteName
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            let payload = response.d;
            if (typeof payload === 'string') {
                try { payload = JSON.parse(payload); } catch (e) { payload = null; }
            }
            const rows = (payload && payload.data) || [];

            if (!rows.length) {
                body.html('<tr><td colspan="2" class="text-muted">No other site matches this name.</td></tr>');
                return;
            }

            body.html(rows.map(site => {
                const address = [site.Address, site.City, site.State, site.Zip].filter(Boolean).join(', ');
                const isCurrent = String(site.Id) === String(opts.siteId);
                return `<tr>
                            <td>${escapeHTML(site.SiteName)}${isCurrent ? ' <span class="badge status-confirmed">this site</span>' : ''}</td>
                            <td>${escapeHTML(address) || '-'}</td>
                        </tr>`;
            }).join(''));
        },
        error: function (xhr) {
            console.error("Error loading duplicate sites: ", xhr.responseText);
            body.html('<tr><td colspan="2" class="text-danger">Failed to load duplicate sites.</td></tr>');
        }
    });
}

function ApptStatusChanged_Event(selectElement) {
    const $select = $(selectElement);
    const value = $select.val();
    if (value === '0') return;

    const previous = $select.data('previous') || '0';
    const ApptID = $select.data('appt-id');
    const customerID = $select.data('customer-id');
    const siteID = $select.data('site-id');

    $select.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: 'AppoinementList.aspx/ApptStatusChanged_Event',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({
            ApptID: String(ApptID ?? ''),
            ApptStatus: value,
            CustomerID: String(customerID ?? ''),
            SiteID: String(siteID ?? '')
        }),
        success: function (resp) {
            $select.prop('disabled', false);
            if (resp.d) {
                $select.data('previous', value);
                notify('success', 'Status updated');
                // The grid's Status column is derived from IsApproved, so refetch
                // it and keep this work order selected.
                LoadAppointments(ApptID);
            } else {
                $select.val(previous);
                notify('error', 'Status update failed', 'The server rejected the change.');
            }
        },
        error: function (xhr) {
            console.error('ApptStatusChanged_Event failed: ', xhr.responseText);
            $select.prop('disabled', false).val(previous);
            notify('error', 'Status update failed', 'The server returned an error.');
        }
    });
}

function SchedulingCalendarChanged_Event(selectElement) {
    const $select = $(selectElement);
    const value = $select.val();
    if (value === '0') return;

    const previous = $select.data('previous') || '0';
    const ApptID = $select.data('appt-id');

    $select.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: 'AppoinementList.aspx/SchedulingCalendarChanged_Event',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ ApptID: String(ApptID ?? ''), SchedulingEvent: value }),
        success: function (resp) {
            $select.prop('disabled', false);
            if (resp.d) {
                $select.data('previous', value);
                notify('success', 'Scheduling calendar updated');
                LoadAppointments(ApptID);
            } else {
                $select.val(previous);
                notify('error', 'Scheduling update failed', 'The server rejected the change.');
            }
        },
        error: function (xhr) {
            console.error('SchedulingCalendarChanged_Event failed: ', xhr.responseText);
            $select.prop('disabled', false).val(previous);
            notify('error', 'Scheduling update failed', 'The server returned an error.');
        }
    });
}

function updateIsActiveLabel() {
    const isChecked = $('#isActive').is(':checked');
    $('#isActiveText').text(isChecked ? 'Active' : 'Deactivated');
}

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
                url: "AppoinementList.aspx/UpdateCustomerFromDefaultSite",
                data: JSON.stringify({ site: site }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        closeModal('addSiteModal');
                        notify('success', 'Customer information updated');
                        if (currentRow) generateCustomerDetails(currentRow);
                    } else {
                        notify('error', 'Update failed', 'Something went wrong while updating the customer information.');
                    }
                },
                error: function (xhr) {
                    console.error("Error updating customer: ", xhr.responseText);
                    notify('error', 'Update failed', 'An error occurred while updating the customer information.');
                }
            });
        } else {
            $.ajax({
                type: "POST",
                url: "AppoinementList.aspx/SaveCustomerSiteData",
                data: JSON.stringify({ site: site }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d) {
                        closeModal('addSiteModal');
                        notify('success', `Site ${site.Id > 0 ? 'updated' : 'saved'}`);
                        if (currentRow) generateCustomerDetails(currentRow);
                    } else {
                        notify('error', 'Save failed', 'Something went wrong while saving the site.');
                    }
                },
                error: function (xhr) {
                    console.error("Error saving site: ", xhr.responseText);
                    notify('error', 'Save failed', 'An error occurred while saving the site.');
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
        notify('warning', 'Missing information', errorMessage);
        return false;
    }
    return true;
}

function OpenCustomerChatHistory(mobile, name, customerId) {
    if (!mobile || mobile.trim() === "") {
        notify('warning', 'Validation Error', 'Please insert a phone number for this customer.');
        return;
    }
    window.open(`CustomerChatHistory.aspx?mobile=${encodeURIComponent(mobile)}&name=${encodeURIComponent(name)}&customerId=${encodeURIComponent(customerId)}`, '_blank');
}
