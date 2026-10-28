
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

    // Delegated from the table itself: DataTables replaces <tbody> on every draw,
    // so a handler bound to the tbody stops firing after the first reload.
    $('#AppointmentListTable').on('click', 'tbody tr', function () {
        if (!table) return;
        const data = table.row(this).data();
        if (!data) return; // the "No data available" placeholder row

        $('#contact').slideDown();

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

        $('#addSiteModal .cust-modal-title').text(isDefault ? 'Edit Default Site (Customer Info)' : 'Edit Site');
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

    // Card header icons: AHS Status, Customer Duplicates and Site Duplicates.
    $('#sites').on('click', '.ahs-status-btn', function () {
        openAhsStatus(currentRow);
    });

    $('#close_mdl_AhsStatus, #btn_CloseAhsStatus').on('click', function () {
        closeModal('mdl_AhsStatus');
    });

    // One click event per AHS Submit button; each calls SubmitAhsStatus().
    $('#mdl_AhsStatus')
        .on('click', '#btn_AhsAppointmentSet', AppointmentSet_Click)
        .on('click', '#btn_AhsWaitingOnCallBack', WaitingOnCallBack_Click)
        .on('click', '#btn_AhsUnableToContact', UnableToContact_Click)
        .on('click', '#btn_AhsPartsOnOrder', PartsOnOrder_Click)
        .on('click', '#btn_AhsComplete', Complete_Click)
        .on('click', '#btn_AhsMissedAppointment', MissedAppointment_Click)
        .on('click', '#btn_AhsNeedToReplace', NeedToReplace_Click)
        .on('click', '#btn_AhsPartsEquipmentStatus', PartsEquipmentStatus_Click)
        .on('click', '#btn_AhsCancelDispatch', CancelDispatch_Click);

    $('#sites').on('click', '.site-dup-btn', function () {
        openSiteDuplicateCheck({
            customerId: $(this).attr('data-customerid'),
            siteId: $(this).attr('data-siteid'),
            siteName: $(this).attr('data-site-name')
        });
    });

    $('#close_mdl_CheckSiteDuplicate, #btn_CloseSiteDuplicate').on('click', function () {
        closeModal('mdl_CheckSiteDuplicate');
    });

    $('#btn_SearchSiteDuplicates').on('click', SearchSiteDuplicates);
    $('#btn_MergeSiteDuplicates').on('click', MergeSiteDuplicates);

    $('#dupSiteName').on('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            SearchSiteDuplicates();
        }
    });

    $('#sites').on('click', '.cust-site-Duplicate-btn', function () {
        openDuplicateCheck({
            customerId: $(this).attr('data-customerid'),
            siteId: $(this).attr('data-siteid'),
            siteName: $(this).attr('data-site-name')
        });
    });

    $('#close_mdl_CheckDuplicate, #clossadaseAddSite').on('click', function () {
        closeModal('mdl_CheckDuplicate');
    });

    $('#btn_SearchDuplicates').on('click', SearchDuplicates);
    $('#btn_MergeDuplicates').on('click', MergeDuplicates);

    // Enter in a search box searches instead of submitting the page's server form.
    $('#mdl_CheckDuplicate .dup-search').on('keydown', 'input', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            SearchDuplicates();
        }
    });

    // Tint the rows so the Primary and the ticked Duplicates stand out.
    $('#DuplicateCustomerTable, #DuplicateSiteTable').on('change', 'input', function () {
        $(this).closest('tbody').find('tr').each(function () {
            $(this).toggleClass('is-primary', $(this).find('.dup-primary').is(':checked'));
            $(this).toggleClass('is-duplicate', $(this).find('.dup-target').is(':checked'));
        });
    });

    $('#sites').on('click', '.cust-site-msgview-btn', function () {
        $('#div_Msg').html('');
        $('#msgSubject').text('');
        openModal('MsgViewModal');
        $('#MsgViewModal .ajax-loader').show();
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
                    // The note starts "Subject: ... Body:" ahead of the email HTML;
                    // the subject goes in the header and the rest is the body.
                    let body = messages[0].Note;
                    const m = /^\s*Subject:\s*([\s\S]*?)\s*Body:\s*/i.exec(body);
                    if (m) {
                        $('#msgSubject').text($('<div>').html(m[1]).text());
                        body = body.slice(m[0].length);
                    }
                    $('#div_Msg').html(body);
                } else {
                    $('#div_Msg').html('<p class="text-muted">No message found.</p>');
                }
                $('#MsgViewModal .ajax-loader').hide();
            },
            error: function (xhr) {
                console.error("Error loading message: ", xhr.responseText);
                $('#div_Msg').html('<p class="text-danger">Failed to load the original message.</p>');
                $('#MsgViewModal .ajax-loader').hide();
            }
        });
    });

    $('#closeMsgView, #btn_CloseMsgView').on('click', function () {
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

// Notes arrive as plain text from the dispatch feed. Escape first, then give
// the common shapes some structure: "Key: value" lines, "- " bullets,
// "*** ... ***" callouts, "-----LABEL-----" dividers and blank-line breaks.
function formatNotes(notes) {
    // The feed usually opens with its own "Notes :" line, which the panel title already covers.
    const text = String(notes ?? '').trim().replace(/^notes\s*:\s*(\r?\n|\r)+/i, '');
    if (!text) return '<p class="ad-empty">No notes for this appointment.</p>';

    const out = [];
    let list = null;
    let pairs = null;
    let loud = null;

    const flush = () => {
        if (list) { out.push(`<ul class="ad-notes-list">${list.join('')}</ul>`); list = null; }
        if (pairs) { out.push(`<dl class="ad-notes-pairs">${pairs.join('')}</dl>`); pairs = null; }
        if (loud) { out.push(`<div class="ad-notes-important">${loud.join('')}</div>`); loud = null; }
    };
    const isLoud = (line) => line.length > 3 && line === line.toUpperCase() && /[A-Z]{3}/.test(line);

    text.split(/\r?\n|\r/).forEach(raw => {
        const line = raw.trim();
        let m;

        if (!line) {
            flush();
            out.push('<div class="ad-notes-gap"></div>');
        } else if ((m = line.match(/^-{3,}\s*([^-]*?)\s*-*$/))) {
            flush();
            out.push(`<div class="ad-notes-divider">${m[1] ? `<span>${escapeHTML(m[1])}</span>` : ''}</div>`);
        } else if ((m = line.match(/^[-\u2022]\s+(.*)$/))) {
            if (pairs) flush();
            // "Plumbing - Additional Coverage": lead with the item, show the qualifier as a tag.
            const [item, ...rest] = m[1].split(' - ');
            const tag = rest.join(' - ').trim();
            (list = list || []).push(`<li><i class="fas fa-check-circle"></i><span>${escapeHTML(item.trim())}</span>${tag ? `<span class="ad-notes-tag">${escapeHTML(tag)}</span>` : ''}</li>`);
        } else if ((m = line.match(/^\*{2,}\s*(.*?)\s*\**$/))) {
            flush();
            out.push(`<div class="ad-notes-callout"><i class="fas fa-info-circle"></i> ${escapeHTML(m[1])}</div>`);
        } else if (pairs && line.length <= 60 && !line.includes(':')) {
            // A short line right under a "Key: value" is that value's second line
            // (the phone under the customer, the city under the street).
            pairs.push(`<dt></dt><dd class="ad-notes-cont">${escapeHTML(line)}</dd>`);
        } else if (isLoud(line)) {
            // All-caps warranty rules ("REMINDER: ...") read better together than as key/value pairs.
            if (list || pairs) flush();
            (loud = loud || []).push(`<div>${escapeHTML(line)}</div>`);
        } else if ((m = line.match(/^([A-Za-z][\w #\/().'&-]{0,40}?)\s*:\s*(.*)$/)) && !m[2].startsWith('//')) {
            if (list || loud) flush();
            if (!m[2]) {
                flush();
                // A bare "Notes:" would just repeat the panel title.
                out.push(/^notes$/i.test(m[1])
                    ? '<div class="ad-notes-gap"></div>'
                    : `<h5 class="ad-notes-heading">${escapeHTML(m[1])}</h5>`);
            } else {
                // The feed leaves blank fields as a bare trailing label ("Effective Date : Expires :"),
                // so drop that label rather than show it as the value.
                const value = m[2].replace(/\s*[A-Za-z][\w ]{0,30}:\s*$/, '').trim();
                (pairs = pairs || []).push(`<dt>${escapeHTML(m[1])}</dt><dd>${value ? escapeHTML(value) : '<span class="ad-empty">Not provided</span>'}</dd>`);
            }
        } else {
            flush();
            out.push(`<p class="ad-notes-line">${escapeHTML(line)}</p>`);
        }
    });
    flush();

    // Collapse runs of blank-line gaps and drop leading/trailing ones.
    return out.join('')
        .replace(/(<div class="ad-notes-gap"><\/div>)+/g, '<div class="ad-notes-gap"></div>')
        .replace(/^(<div class="ad-notes-gap"><\/div>)|(<div class="ad-notes-gap"><\/div>)$/g, '');
}

// Bumped on every details load; lets a response tell whether it is still the latest.
let siteLoadSeq = 0;

function loadCustomerSiteData(customerId, notes, ApptID, SchedulingCal, IsApproved, SiteId, ApptDateTime) {

    if (!customerId) return;
    const seq = ++siteLoadSeq;

    $.ajax({
        type: "POST",
        url: "AppoinementList.aspx/GetCustomerSiteData",
        data: JSON.stringify({ customerId: customerId, SiteId: SiteId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        beforeSend: function () {
            $('#detailsLoading').addClass('is-active');
        },
        complete: function () {
            // Only the latest request clears the overlay, so a quick second click keeps it up.
            if (seq === siteLoadSeq) $('#detailsLoading').removeClass('is-active');
        },
        success: function (response) {
            // A slower earlier request finishing late must not overwrite the newer row's details.
            if (seq !== siteLoadSeq) return;
            sites = response.d || [];
            // The AHS feed writes the Dispatch ID into the note ("DispatchID: 95879269" or
            // "Dispatch ID 95879269"); use that until the backend sends DispatchId on its own.
            const noteDispatch = String(notes || '').match(/dispatch\s*id\s*[:#]?\s*([A-Za-z0-9-]+)/i);
            const dispatchId = (currentRow && currentRow.DispatchId) || (noteDispatch ? noteDispatch[1] : '');

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
                    const mapQuery = [site.Address, site.City, site.State, site.Zip].filter(Boolean).join(', ');
                    const detailRow = (icon, label, value) => `
                        <div class="ad-row">
                            <i class="${icon} fa-fw ad-row-icon"></i>
                            <span class="ad-row-label">${label}</span>
                            <span class="ad-row-value">${value || '<span class="ad-empty">Not provided</span>'}</span>
                        </div>`;

                    const siteCardHTML = `
                        <div class="cust-site-card ad-card" data-site-id="${escapeHTML(site.Id)}">
                            <div class="cust-site-header ad-header">
                                <div class="cust-site-title-group">
                                    <h3 class="cust-site-title" title="${escapeHTML(site.SiteName)}">${escapeHTML(site.SiteName)}</h3>
                                    <span class="ad-badge ${statusClass}"><span class="ad-badge-dot"></span>${statusTitle}</span>
                                    ${dispatchId ? `<span class="ad-dispatch" title="Dispatch ID"><i class="fas fa-hashtag"></i> Dispatch ID: <strong>${escapeHTML(dispatchId)}</strong></span>` : ''}
                                </div>
                                <div class="cust-site-actions">
                                    <button type="button" class="cust-site-icon-btn ahs-status-btn" title="AHS Status">
                                        <i class="fas fa-clipboard-list"></i></button>
                                    <button type="button" class="cust-site-icon-btn cust-site-Duplicate-btn" title="Customer Duplicates" data-Site-Name="${escapeHTML(site.SiteName)}" data-siteid="${escapeHTML(site.Id)}" data-CustomerID="${escapeHTML(site.CustomerID)}">
                                        <i class="fas fa-users"></i></button>
                                    <button type="button" class="cust-site-icon-btn site-dup-btn" title="Site Duplicates" data-Site-Name="${escapeHTML(site.SiteName)}" data-siteid="${escapeHTML(site.Id)}" data-CustomerID="${escapeHTML(site.CustomerID)}">
                                        <i class="fas fa-map-marker-alt"></i></button>
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

                            <div class="ad-top">
                            <section class="ad-panel ad-address">
                                <h4 class="ad-panel-title">
                                    <span><i class="fas fa-map-marker-alt"></i> Site address</span>
                                    ${mapQuery ? `<a class="ad-link" href="https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(mapQuery)}" target="_blank" rel="noopener"><i class="fas fa-external-link-alt"></i> Maps</a>` : ''}
                                </h4>
                                <div class="ad-address-grid">
                                    <div class="ad-address-wide">${detailRow('fas fa-road', 'Street', escapeHTML(site.Address))}</div>
                                    ${detailRow('fas fa-city', 'City', escapeHTML(site.City))}
                                    ${detailRow('fas fa-flag', 'State', escapeHTML(site.State))}
                                    ${detailRow('fas fa-mail-bulk', 'Zip', escapeHTML(site.Zip))}
                                    ${detailRow('fas fa-globe-americas', 'Country', escapeHTML(site.Country))}
                                </div>
                                ${isDefaultSite ? `<div class="ad-address-warn"><i class="fas fa-exclamation-triangle"></i> No site is linked to this appointment, so this is the customer's address, not the job site.</div>` : ''}
                            </section>
                            <div class="ad-toolbar">
                                <label class="ad-field">
                                    <span class="ad-field-label">Appointment status</span>
                                    <select class="form-select form-select-sm appt-status-select" aria-label="Appointment status" data-appt-id="${escapeHTML(ApptID)}" data-customer-id="${escapeHTML(customerId)}" data-site-id="${escapeHTML(site.Id)}"><option value="0">Select</option><option ${IsApproved ? 'selected' : ''} value="Accept">Accept</option><option ${!IsApproved ? 'selected' : ''} value="Pending">New Appt</option></select>
                                </label>
                                <label class="ad-field">
                                    <span class="ad-field-label">Scheduling calendar</span>
                                    <select class="form-select form-select-sm appt-calendar-select" aria-label="Scheduling calendar" data-appt-id="${escapeHTML(ApptID)}"><option value="0">Select</option><option ${SchedulingCal == 'FSM' ? 'selected' : ''} value="FSM">FSM</option><option ${SchedulingCal == 'CEC' ? 'selected' : ''} value="CEC">CEC</option></select>
                                </label>
                                <div class="ad-field">
                                    <span class="ad-field-label">Requested date</span>
                                    <span class="ad-field-static"><i class="far fa-calendar-alt"></i> ${escapeHTML(ApptDateTime) || '-'}</span>
                                </div>
                            </div>
                            </div>

                            <section class="ad-panel ad-notes">
                                <h4 class="ad-panel-title"><i class="fas fa-sticky-note"></i> Notes</h4>
                                <div class="ad-notes-body">${formatNotes(notes)}</div>
                            </section>
                        </div>`;
                    sitesListContainer.append(siteCardHTML);
                });
            } else {
                sitesListContainer.append('<p class="text-muted">No sites have been added for this customer.</p>');
            }
        },
        error: function (xhr) {
            if (seq !== siteLoadSeq) return;
            console.error("Error loading site data: ", xhr.responseText);
            $('#sites .sites-list').html('<p class="text-danger">Failed to load site data.</p>');
        }
    });
}

// The AHS statuses offered in the AHS Status modal, in the order shown, with
// the id of each row's Submit button.
const AHS_STATUSES = [
    { id: 'btn_AhsAppointmentSet', status: 'Appointment Set' },
    { id: 'btn_AhsWaitingOnCallBack', status: 'Waiting on Customer Call Back' },
    { id: 'btn_AhsUnableToContact', status: 'Unable To Contact' },
    { id: 'btn_AhsPartsOnOrder', status: 'In Progress with Parts on Order' },
    { id: 'btn_AhsComplete', status: 'Complete (AHS)' },
    { id: 'btn_AhsMissedAppointment', status: 'Customer Missed Appointment' },
    { id: 'btn_AhsNeedToReplace', status: 'In Progress With Need To Replace' },
    { id: 'btn_AhsPartsEquipmentStatus', status: 'Parts/Equipment Status' },
    { id: 'btn_AhsCancelDispatch', status: 'Cancel Dispatch' }
];

// Click events for the AHS Submit buttons, one per status. They all call the
// same SubmitAhsStatus() so the confirm and the API call live in one place.
function AppointmentSet_Click() { SubmitAhsStatus($(this), 'Appointment Set'); }
function WaitingOnCallBack_Click() { SubmitAhsStatus($(this), 'Waiting on Customer Call Back'); }
function UnableToContact_Click() { SubmitAhsStatus($(this), 'Unable To Contact'); }
function PartsOnOrder_Click() { SubmitAhsStatus($(this), 'In Progress with Parts on Order'); }
function Complete_Click() { SubmitAhsStatus($(this), 'Complete (AHS)'); }
function MissedAppointment_Click() { SubmitAhsStatus($(this), 'Customer Missed Appointment'); }
function NeedToReplace_Click() { SubmitAhsStatus($(this), 'In Progress With Need To Replace'); }
function PartsEquipmentStatus_Click() { SubmitAhsStatus($(this), 'Parts/Equipment Status'); }
function CancelDispatch_Click() { SubmitAhsStatus($(this), 'Cancel Dispatch'); }

// Asks Yes/No, then calls the AHS backend method.
function SubmitAhsStatus(btn, status) {
    const row = btn.closest('.ahs-row');
    const date = row.find('.ahs-date').val();
    const apptId = $('#mdl_AhsStatus').data('appt-id');

    if (!date) {
        notify('warning', `Pick a date for "${status}".`);
        return;
    }

    confirmYesNo(`Submit "${status}"?`, `Date: ${formatIsoDate(date)}`).then(yes => {
        if (!yes) return;

        btn.prop('disabled', true).text('Saving…');
        $.ajax({
            type: "POST",
            url: AHS_STATUS_API_URL,
            data: JSON.stringify(AHS_STATUS_API_PARAMS),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function () {
                (ahsStatusUpdates[apptId] = ahsStatusUpdates[apptId] || {})[status] = date;
                row.find('.ahs-row-meta').addClass('is-saved')
                    .html(`<i class="fas fa-check-circle"></i> Submitted ${escapeHTML(formatIsoDate(date))}`);
                notify('success', `${status} submitted`);
            },
            error: function (xhr) {
                console.error("Error submitting AHS status: ", xhr.responseText);
                notify('error', 'Could not submit the status.', 'Please try again.');
            },
            complete: function () {
                btn.prop('disabled', false).text('Submit');
            }
        });
    });
}

// Every Submit calls this one backend method. Its two parameters are static
// placeholders for now; the backend will tell us what to send in them.
const AHS_STATUS_API_URL = "AppoinementList.aspx/UpdateAhsStatus";
const AHS_STATUS_API_PARAMS = { Param1: "1", Param2: "1" };

// Remembers each successful submit per appointment until the page reloads,
// so the row can show "Submitted <date>".
const ahsStatusUpdates = {};

function todayIso() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function formatIsoDate(iso) {
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso || '');
    return m ? `${m[2]}/${m[3]}/${m[1]}` : iso;
}

function openAhsStatus(row) {
    if (!row) {
        notify('warning', 'Select a work order first.');
        return;
    }

    const saved = ahsStatusUpdates[row.ApptID] || {};
    $('#mdl_AhsStatus').data('appt-id', row.ApptID);
    $('#ahsStatusSubtitle').html(row.CustomerName ? `Work order for <strong>${escapeHTML(row.CustomerName)}</strong>` : '');

    $('#AhsStatusList').html(AHS_STATUSES.map(({ id, status }) => {
        const date = saved[status];
        const danger = status === 'Cancel Dispatch' ? ' ahs-row-danger' : '';
        return `
            <div class="ahs-row${danger}" data-status="${escapeHTML(status)}">
                <div class="ahs-row-text">
                    <div class="ahs-row-title">${escapeHTML(status)}</div>
                    <div class="ahs-row-meta${date ? ' is-saved' : ''}">${date ? `<i class="fas fa-check-circle"></i> Submitted ${escapeHTML(formatIsoDate(date))}` : 'Not submitted'}</div>
                </div>
                <input type="date" class="form-control ahs-date" aria-label="${escapeHTML(status)} date" value="${escapeHTML(date || todayIso())}" />
                <button type="button" class="ahs-submit-btn" id="${id}">Submit</button>
            </div>`;
    }).join(''));

    openModal('mdl_AhsStatus');
}

// Yes/No confirm; Swal comes from the master page, confirm() is the fallback.
function confirmYesNo(title, text) {
    if (typeof Swal !== 'undefined') {
        return Swal.fire({
            icon: 'question',
            title: title,
            text: text || '',
            showCancelButton: true,
            confirmButtonText: 'Yes',
            cancelButtonText: 'No',
            reverseButtons: true
        }).then(result => result.isConfirmed);
    }
    return Promise.resolve(confirm(title + (text ? '\n' + text : '')));
}

// Duplicate customer check, ported from Jobs-Scheduler's customerDetail.aspx.
// Front end only: both methods below still have to be added to
// AppoinementList.aspx.cs. Jobs-Scheduler's versions can be copied as they are
// (same parameter names, same DupCustomer fields).
const DUP_SEARCH_URL = "AppoinementList.aspx/FindDuplicateCustomers";
const DUP_MERGE_URL = "AppoinementList.aspx/ReassignCustomerCompany";

// The customer the popup was opened from; listed first and preselected as Primary.
let dupCurrentCustomerId = null;

function dupMessage(html, table) {
    $(table || '#DuplicateCustomerTable').find('tbody').html(`<tr class="dup-message"><td colspan="10">${html}</td></tr>`);
}

// Opens the popup with the search prefilled from the clicked site's contact.
function openDuplicateCheck(opts) {
    const site = sites.find(s => String(s.Id) === String(opts.siteId)) || {};
    dupCurrentCustomerId = opts.customerId || site.CustomerID || null;

    $('#dupFirstName').val(site.FirstName || '');
    $('#dupLastName').val(site.LastName || '');
    $('#dupAddress').val(site.Address || '');
    $('#dupPhone').val(site.PhoneNumber || '');
    $('#dupMobile').val(site.MobileNumber || '');
    $('#dupEmail').val(site.Email || '');

    openModal('mdl_CheckDuplicate');
    SearchDuplicates();
}

function SearchDuplicates() {
    const payload = {
        firstName: $.trim($('#dupFirstName').val()),
        lastName: $.trim($('#dupLastName').val()),
        address: $.trim($('#dupAddress').val()),
        phone: $.trim($('#dupPhone').val()),
        mobile: $.trim($('#dupMobile').val()),
        email: $.trim($('#dupEmail').val())
    };

    if (!Object.values(payload).some(Boolean)) {
        dupMessage('Fill in at least one search box.');
        return;
    }

    dupMessage('<span class="spinner-border spinner-border-sm"></span> Searching…');
    $('#btn_SearchDuplicates').prop('disabled', true);

    $.ajax({
        type: "POST",
        url: DUP_SEARCH_URL,
        data: JSON.stringify(payload),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const list = response.d || [];
            if (!list.length) {
                dupMessage('No matching customers found. Try adjusting the search.');
                return;
            }

            // The current customer goes first and starts out as the Primary.
            const isCurrent = c => String(c.CustomerID) === String(dupCurrentCustomerId);
            const ordered = list.filter(isCurrent).concat(list.filter(c => !isCurrent(c)));

            $('#DuplicateCustomerTable tbody').html(ordered.map(c => {
                const id = escapeHTML(c.CustomerID);
                const company = escapeHTML(c.CompanyID);
                const current = isCurrent(c);
                return `
                    <tr class="${current ? 'is-primary' : ''}">
                        <td class="dup-pick"><input type="radio" name="dupPrimary" class="dup-primary" value="${id}" data-company="${company}" aria-label="Keep as primary" ${current ? 'checked' : ''} /></td>
                        <td class="dup-pick"><input type="checkbox" class="dup-target" value="${id}" aria-label="Mark as duplicate" /></td>
                        <td class="dup-pick"><input type="radio" name="dupSecondary" class="dup-secondary" value="${id}" aria-label="Use as secondary contact" /></td>
                        <td>${escapeHTML(c.FirstName)}${current ? '<span class="dup-current-tag">current</span>' : ''}</td>
                        <td>${escapeHTML(c.LastName)}</td>
                        <td>${escapeHTML(c.Address1)}</td>
                        <td>${escapeHTML(c.City)}</td>
                        <td>${escapeHTML(c.Phone)}</td>
                        <td>${escapeHTML(c.Email)}</td>
                        <td>${escapeHTML(c.Mobile)}</td>
                    </tr>`;
            }).join(''));
        },
        error: function (xhr) {
            console.error("Error searching duplicate customers: ", xhr.responseText);
            dupMessage('<span class="text-danger">Something went wrong while searching. Please try again.</span>');
        },
        complete: function () {
            $('#btn_SearchDuplicates').prop('disabled', false);
        }
    });
}

function MergeDuplicates() {
    const $primary = $('#DuplicateCustomerTable .dup-primary:checked');
    if (!$primary.length) {
        notify('warning', 'Select a customer', 'Please choose the customer you want to keep.');
        return;
    }
    const primaryId = String($primary.val());

    const targetIds = $('#DuplicateCustomerTable .dup-target:checked').map(function () {
        return $(this).val();
    }).get().filter(id => id !== primaryId).map(id => parseInt(id, 10));

    if (!targetIds.length) {
        notify('warning', 'Select duplicates', 'Please tick at least one duplicate record to merge.');
        return;
    }

    const secondaryId = parseInt($('#DuplicateCustomerTable .dup-secondary:checked').val(), 10) || 0;
    const deleteMerged = $('#chk_DeleteMerged').is(':checked');

    confirmYesNo('Merge duplicates?',
        `The ${targetIds.length} selected duplicate(s) will be merged into the customer you chose to keep, ` +
        `and their appointments and invoices will be moved to it.` +
        (deleteMerged ? ' The old accounts will be removed.' : '')
    ).then(yes => {
        if (!yes) return;

        $('#btn_MergeDuplicates').prop('disabled', true).text('Merging…');
        $.ajax({
            type: "POST",
            url: DUP_MERGE_URL,
            data: JSON.stringify({
                targetCompanyId: String($primary.data('company') ?? ''),
                primaryCustomerId: parseInt(primaryId, 10),
                customerIds: targetIds,
                Isdelete: deleteMerged,
                secondaryCustomerId: secondaryId
            }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                closeModal('mdl_CheckDuplicate');
                // The server answers with a message ("Done! ..." or what went wrong).
                const msg = response.d || 'Duplicates merged.';
                const ok = /^done/i.test(msg);
                // Reload once the message is dismissed, like Jobs-Scheduler does.
                Promise.resolve(notify(ok ? 'info' : 'error', ok ? 'Merged' : 'Merge failed', msg))
                    .then(() => { if (ok) location.reload(); });
            },
            error: function (xhr) {
                console.error("Error merging duplicate customers: ", xhr.responseText);
                notify('error', 'Merge failed', 'Something went wrong while merging. Please try again.');
            },
            complete: function () {
                $('#btn_MergeDuplicates').prop('disabled', false).text('Merge Selected Duplicates');
            }
        });
    });
}

// Duplicate site check, same flow as the customer check. Front end only: the
// backend still has to add both methods to AppoinementList.aspx.cs:
//   FindDuplicateSites(siteName, address, city, zip, phone, email) -> list of
//     { Id, SiteName, FirstName, LastName, Address, City, State, Zip, PhoneNumber, Email },
//     matching ANY filled-in box (like FindDuplicateCustomers)
//   MergeCustomerSites(primarySiteId, siteIds, Isdelete) -> "Done! ..." message
const SITE_DUP_SEARCH_URL = "AppoinementList.aspx/FindDuplicateSites";
const SITE_DUP_MERGE_URL = "AppoinementList.aspx/MergeCustomerSites";

// The site the popup was opened from; listed first and preselected as Primary.
let siteDupContext = {};

// Opens the popup with the search prefilled from the clicked site.
function openSiteDuplicateCheck(opts) {
    siteDupContext = opts;
    const site = sites.find(s => String(s.Id) === String(opts.siteId)) || {};

    $('#dupSiteName').val(site.SiteName || opts.siteName || '');
    $('#dupSiteAddress').val(site.Address || '');
    $('#dupSiteCity').val(site.City || '');
    $('#dupSiteZip').val(site.Zip || '');
    $('#dupSitePhone').val(site.PhoneNumber || '');
    $('#dupSiteEmail').val(site.Email || '');

    openModal('mdl_CheckSiteDuplicate');
    SearchSiteDuplicates();
}

function SearchSiteDuplicates() {
    const payload = {
        siteName: $.trim($('#dupSiteName').val()),
        address: $.trim($('#dupSiteAddress').val()),
        city: $.trim($('#dupSiteCity').val()),
        zip: $.trim($('#dupSiteZip').val()),
        phone: $.trim($('#dupSitePhone').val()),
        email: $.trim($('#dupSiteEmail').val())
    };

    if (!Object.values(payload).some(Boolean)) {
        dupMessage('Fill in at least one search box.', '#DuplicateSiteTable');
        return;
    }

    dupMessage('<span class="spinner-border spinner-border-sm"></span> Searching…', '#DuplicateSiteTable');
    $('#btn_SearchSiteDuplicates').prop('disabled', true);

    $.ajax({
        type: "POST",
        url: SITE_DUP_SEARCH_URL,
        data: JSON.stringify(payload),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const list = response.d || [];
            if (!list.length) {
                dupMessage('No matching sites found. Try adjusting the search.', '#DuplicateSiteTable');
                return;
            }

            const isCurrent = s => String(s.Id) === String(siteDupContext.siteId);
            const ordered = list.filter(isCurrent).concat(list.filter(s => !isCurrent(s)));

            $('#DuplicateSiteTable tbody').html(ordered.map(s => {
                const id = escapeHTML(s.Id);
                const current = isCurrent(s);
                const contact = [s.FirstName, s.LastName].filter(Boolean).join(' ');
                return `
                    <tr class="${current ? 'is-primary' : ''}">
                        <td class="dup-pick"><input type="radio" name="dupSitePrimary" class="dup-primary" value="${id}" aria-label="Keep as primary" ${current ? 'checked' : ''} /></td>
                        <td class="dup-pick"><input type="checkbox" class="dup-target" value="${id}" aria-label="Mark as duplicate" /></td>
                        <td>${escapeHTML(s.SiteName)}${current ? '<span class="dup-current-tag">current</span>' : ''}</td>
                        <td>${escapeHTML(contact)}</td>
                        <td>${escapeHTML(s.Address)}</td>
                        <td>${escapeHTML(s.City)}</td>
                        <td>${escapeHTML(s.State)}</td>
                        <td>${escapeHTML(s.Zip)}</td>
                        <td>${escapeHTML(s.PhoneNumber)}</td>
                        <td>${escapeHTML(s.Email)}</td>
                    </tr>`;
            }).join(''));
        },
        error: function (xhr) {
            console.error("Error searching duplicate sites: ", xhr.responseText);
            dupMessage('<span class="text-danger">Something went wrong while searching. Please try again.</span>', '#DuplicateSiteTable');
        },
        complete: function () {
            $('#btn_SearchSiteDuplicates').prop('disabled', false);
        }
    });
}

function MergeSiteDuplicates() {
    const $primary = $('#DuplicateSiteTable .dup-primary:checked');
    if (!$primary.length) {
        notify('warning', 'Select a site', 'Please choose the site you want to keep.');
        return;
    }
    const primaryId = String($primary.val());

    const siteIds = $('#DuplicateSiteTable .dup-target:checked').map(function () {
        return $(this).val();
    }).get().filter(id => id !== primaryId).map(id => parseInt(id, 10));

    if (!siteIds.length) {
        notify('warning', 'Select duplicates', 'Please tick at least one duplicate site to merge.');
        return;
    }

    const deleteMerged = $('#chk_DeleteMergedSites').is(':checked');

    confirmYesNo('Merge duplicate sites?',
        `The ${siteIds.length} selected site(s) will be merged into the site you chose to keep, ` +
        `and their appointments will be moved to it.` +
        (deleteMerged ? ' The old sites will be removed.' : '')
    ).then(yes => {
        if (!yes) return;

        $('#btn_MergeSiteDuplicates').prop('disabled', true).text('Merging…');
        $.ajax({
            type: "POST",
            url: SITE_DUP_MERGE_URL,
            data: JSON.stringify({
                primarySiteId: parseInt(primaryId, 10),
                siteIds: siteIds,
                Isdelete: deleteMerged
            }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                closeModal('mdl_CheckSiteDuplicate');
                const msg = response.d || 'Sites merged.';
                const ok = /^done/i.test(msg);
                Promise.resolve(notify(ok ? 'info' : 'error', ok ? 'Merged' : 'Merge failed', msg))
                    .then(() => { if (ok) location.reload(); });
            },
            error: function (xhr) {
                console.error("Error merging duplicate sites: ", xhr.responseText);
                notify('error', 'Merge failed', 'Something went wrong while merging. Please try again.');
            },
            complete: function () {
                $('#btn_MergeSiteDuplicates').prop('disabled', false).text('Merge Selected Sites');
            }
        });
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
        const isDefaultSite = siteId === 0 && $('#addSiteModal .cust-modal-title').text().includes('Default');

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
