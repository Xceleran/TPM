
let table = null;
var sites = [];
var siteAppointmentsCache = {};


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

    $('#AppointmentListTable tbody').on('click', 'tr', function () {
        $('#contact, #sites').slideDown();
        $('#contactBtn, #sitesBtn').addClass('active');
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


    $('#AppointmentListTable tbody').on('click', '.cust-table-edit-btn', function (e) {
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
                url: "AppoinementList.aspx/AddCustomer",
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
                url: "AppoinementList.aspx/UpdateCustomer",
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

    $('#sites').on('click', '.cust-site-edit-btn', function () {
        const siteId = $(this).data('site-id');
        const isDefault = $(this).data('is-default') === true;
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

    $('#sites').on('click', '.cust-site-msgview-btn', function () {
        $('#div_Msg').html('');
        openModal('MsgViewModal');
        $('.ajax-loader').css("visibility", "visible");
        const ApptID = $(this).data('site-id');
        $.ajax({
            type: "POST",
            url: "AppoinementList.aspx/Get_Message",
            data: JSON.stringify({ ApptID: ApptID }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response.d);
                if (response.d) {
                   
                    var sites = response.d || [];
                    $('#div_Msg').html(sites[0].Note);
                } else {
                   
                }
                $('.ajax-loader').css("visibility", "hidden");
            },
            error: function (xhr) {
                console.error("Error deleting site: ", xhr.responseText);
                $('.ajax-loader').css("visibility", "hidden");
               
            }
        });
       
      
    });
    $('#closeMsgView').on('click', function () {
        closeModal('MsgViewModal');
    });

    $('#closeAddSite, #closeAddSiteIcon').on('click', function () {
        closeModal('addSiteModal');
    });

    $('#statusFilter').on('change', function () {
        LoadAppointments();
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

    $('#AppointmentListTable').on('draw.dt.statusFilter', function () {
        applyRowFiltersOnCurrentPage();
        selectFirstVisibleRow();
    });

    $('#sites').on('click', '.cust-site-appts-toggle', function () {
        const siteId = parseInt($(this).data('site-id'), 10);
        const apptsEl = $(`#site-appts-${siteId}`);
        if (!apptsEl.length) return;

        const isVisible = apptsEl.is(':visible');
        if (isVisible) {
            apptsEl.slideUp();
            $(this).text('Show Appointments');
        } else {
            $(this).text('Hide Appointments');
            apptsEl.slideDown();
            if (apptsEl.data('loaded') !== true) {
                loadSiteAppointments(siteId, apptsEl);
            }
        }
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
            url: "AppoinementList.aspx/DeleteCustomerSite",
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
function LoadAppointments() {
    table = null;

  //  $('#sites').html('<p class="text-primary">Please select a item to view details.</p>');

    const sitesHeaderContainer = $('#sites .sites-header');
    const sitesListContainer = $('#sites .sites-list');

    sitesHeaderContainer.empty();
    sitesListContainer.empty();


    if ($.fn.DataTable.isDataTable('#AppointmentListTable')) {
      
        $('#AppointmentListTable').DataTable().destroy();
        $('#AppointmentListTable').empty(); // Manually empty the table's DOM
    }


    const wantedStatus = ($('#statusFilter').val() || 'all').toLowerCase();
    $('#AppointmentListTable').DataTable({
        processing: true,
        destroy: true,
        filter: true,
        ajax: {
            url: "AppoinementList.aspx/LoadAppointments",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: function (d) {
                return JSON.stringify({
                    SearchBy: "",
                    SearchFor: "",
                    SearchFrom: "",
                    SearchTo: "",
                    SearchCriteria: "",
                    SearchByWarrantyCompany: "",
                    wantedStatus: wantedStatus
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
        order: [[2, 'desc']],
        select: { style: 'single' },
        columns: [
            { data: "CustomerName", name: "CustomerName", autoWidth: true },
            { data: "SiteName", name: "First Name", autoWidth: true },
            { data: "ApptDateTime", name: "Last Name", autoWidth: true },
            {
                data: "Apptstatus",
                name: "Status",
                autoWidth: true,
                render: function (data) {
                    let statusText = data || 'N/A'; // Changed from const status to let statusText
                    let statusClass = 'status-na';
                    switch (statusText.toLowerCase()) { // Used statusText.toLowerCase()
                        case 'pending': statusClass = 'status-pending'; break;
                        case 'Approved': statusClass = 'status-confirmed'; break;
                        case 'dispatched': statusClass = 'status-dispatched'; break;
                        case 'in-route': statusClass = 'status-in-route'; break;
                        case 'fa-id sent': statusClass = 'status-fa-id-sent'; break;
                        case 'arrived': statusClass = 'status-arrived'; break;
                        case 'completed': statusClass = 'status-completed'; break;
                        case 'closed': statusClass = 'status-closed'; break;
                        case 'on-hold': statusClass = 'status-on-hold'; break;
                        case '0':
                            statusClass = 'status-default';
                            statusText = 'Multiple'; // Changed displayed text
                            break;
                        case 'cancelled': statusClass = 'status-cancelled'; break;
                    }
                    return `<span class="badge ${statusClass}">${statusText}</span>`; // Used statusText
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
            if (api.rows({ page: 'current' }).count() > 0 && !$('#AppointmentListTable tbody tr.selected').length) {
               // selectFirstVisibleRow(); // Re-added this line
            }
        }
    });
   
    $('#AppointmentListTable tbody').on('click', 'tr', function () {
        if ($(this).hasClass('selected')) return;
        var data = $('#AppointmentListTable').DataTable().row(this).data();
        if (data) {
            generateCustomerDetails(data);
        }
    });
}
function generateCustomerDetails(data) {
    if (!data) {
        $('#customerName').text('Select a Customer');

        $('.ci-item').addClass('is-empty');
        $('#customerPhone, #customerMobile, #customerEmail, #customerAddress, #customerJobTitle').text('-');

        $('#sites .sites-header').empty();
        $('#sites .sites-list').empty().html('<p class="text-muted">Select a customer to see their sites.</p>');
        return;
    }

    const safe = (v) => v || '';
    const normPhone = (v) => safe(v).replace(/[^\d+]/g, '');


    $('#customerName').text([safe(data.CustomerName)]);


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

    const fullAddress = [safe(data.Address1), safe(data.City), safe(data.State), safe(data.ZipCode)].filter(Boolean).join(', ');
    updateItem('customerAddress', fullAddress);

    updateItem('customerJobTitle', data.JobTitle);

    $('#CustomerID').val(safe(data.CustomerID));
    $('#CustomerGuid').val(safe(data.CustomerGuid));

    loadCustomerSiteData(data.CustomerID, data.Notes, data.ApptID, data.SchedulingCal, data.IsApproved, data.SiteID);
}

function loadCustomerSiteData(customerId, notes, ApptID, SchedulingCal, IsApproved, SiteId) {
  
    if (!customerId) return;

    $.ajax({
        type: "POST",
        url: "AppoinementList.aspx/GetCustomerSiteData",
        data: JSON.stringify({ customerId: customerId, SiteId: SiteId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            sites = response.d || [];
            siteAppointmentsCache = {};

            const sitesHeaderContainer = $('#sites .sites-header');
            const sitesListContainer = $('#sites .sites-list');

            sitesHeaderContainer.empty();
            sitesListContainer.empty();
          //  sitesHeaderContainer.append('<button id="addSiteBtn" type="button">+ Add Site</button>');
            console.log(sites);
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
                                    
                                 <button class="cust-site-icon-btn cust-site-msgview-btn" title="View Original Message" data-site-id="${ApptID}" data-is-default="${isDefaultSite}">
                                   <i class="fa fa-eye"></i> </button>
                                    ${editButton}
                                   
                                    <a href="CustomerDetails.aspx?siteId=${site.Id}&custId=${encodeURIComponent(site.CustomerID)}" class="cust-site-icon-btn ${!site.IsActive ? 'd-none' : ''}" title="View Details">
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
                                <p class="cust-site-info"> <i class="fas fa-envelope fa-fw"></i> ${site.Email ? `<a href="mailto:${site.Email}" class="site-email-link" data-customer-id="${site.CustomerID}">${escapeHTML(site.Email)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-phone-alt fa-fw"></i> ${site.PhoneNumber ? `<a href="tel:${site.PhoneNumber}">${escapeHTML(site.PhoneNumber)}</a>` : '-'}</p>
                                <p class="cust-site-info"><i class="fas fa-mobile-alt fa-fw"></i> ${site.MobileNumber ? `<a href="tel:${site.MobileNumber}">${escapeHTML(site.MobileNumber)}</a>` : '-'}</p>
                            </div>
                            <div class="cust-site-footer">
                                <button class="cust-site-appts-toggle btn-primary" data-site-id="${site.Id}">
                                     Duplicate Site Check
                                </button>
                               <div class="container">
                                  <div class="row justify-content-start">
                                    <div class="col-3">
                                      Appointments Status :<select onchange="ApptStatusChanged_Event(event,${ApptID},${customerId},${site.Id})" aria-controls="Appt_Status" class="form-select form-select-sm " style="width:150px;" id="dt-length-0"><option value="0">Select</option><option ${IsApproved ? 'selected' : ''} value="Accept">Accept</option><option value="Confirm">Confirm</option><option value="Cancel">Cancel</option></select>

                                    </div>
                                    <div class="col-3">
                                     Scheduling Calendar :<select onchange="SchedulingCalendarChanged_Event(event,${ApptID})" aria-controls="Appt_Status" class="form-select form-select-sm " style="width:150px;" id="dt-length-0"><option value="0">Select</option><option ${SchedulingCal == 'FSM' ? 'selected' : ''}  value="FSM">FSM</option><option ${SchedulingCal == 'CEC' ? 'selected' : ''} value="CEC">CEC</option></select>

                                    </div>
                                  </div>
                                </div>
                                 Notes :<br>"${notes}"
                               
                            </div>
                        </div>`;
                    sitesListContainer.append(siteCardHTML);
                   // alert(SchedulingCal);
                   // loadAppointmentCount(site.CustomerID, site.Id);
                });
            } else {
                sitesListContainer.append('<p class="text-muted">No sites have been added for this customer.</p>');
            }
        },
        error: function (xhr) {
            console.error("Error loading site data: ", xhr.responseText);
            $('#sites').html('<p class="text-danger">Failed to load site data.</p>');
        }
    });
}
function ApptStatusChanged_Event(event, ApptID, customerID, siteID) {

    var selectElement = event.target;
    var value = selectElement.value;
    if (value == '0') return;

    $.ajax({
        type: 'POST',
        url: 'AppoinementList.aspx/ApptStatusChanged_Event',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ ApptID: ApptID, ApptStatus: value, CustomerID: customerID, SiteID: siteID }),
        success: function (resp) {
            if (resp.d) {
                alert('Status updated successfully!');
            }
            else {
                alert('Status updated Failed!');
            }
            LoadAppointments();
        },
        error: function () {
            countEl.text('!');
        }
    });


  
}
function SchedulingCalendarChanged_Event(event, ApptID) {

    var selectElement = event.target;
    var value = selectElement.value;
    if (value == '0') return;

    $.ajax({
        type: 'POST',
        url: 'AppoinementList.aspx/SchedulingCalendarChanged_Event',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({ ApptID: ApptID, SchedulingEvent: value }),
        success: function (resp) {
            if (resp.d) {
                alert('Scheduling updated successfully!');
            }
            else {
                alert('Scheduling updated Failed!');
            }
            LoadAppointments();
        },
        error: function () {
            countEl.text('!');
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
        data: JSON.stringify({ customerId: customerId, SiteId: siteId }),
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
                url: "AppoinementList.aspx/UpdateCustomerFromDefaultSite",
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
                url: "AppoinementList.aspx/SaveCustomerSiteData",
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
    const wantedStatus = ($('#statusFilter').val() || 'all').toLowerCase();
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
    const firstVisibleRow = $('#AppointmentListTable tbody tr:visible').first();
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

    containerEl.html('<div class="text-muted small">Loading appointments…</div>');

    if (siteAppointmentsCache[siteId]) {
        renderSiteAppointments(siteId, siteAppointmentsCache[siteId], containerEl);
        containerEl.data('loaded', true);
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
            renderSiteAppointments(siteId, list, containerEl);
            containerEl.data('loaded', true);
        },
        error: function (xhr) {
            console.error('GetCustomerAppoinmets failed:', xhr.responseText);
            containerEl.html('<div class="text-danger small">Failed to load appointments.</div>');
        }
    });
}

// FILE: customer.js
// This is the corrected function.

function renderSiteAppointments(siteId, list, containerEl) {
    if (!list || !list.length) {
        containerEl.html('<div class="text-muted small">No appointments for this site.</div>');
        return;
    }

    const customerId = $('#CustomerID').val();

    const rows = list.map(item => {
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

        const detailsUrl = `CustomerDetails.aspx?siteId=${siteId}&custId=${encodeURIComponent(customerId)}&tab=appointments`;

        return `
            <div class="cust-appt-row">
                <div class="appt-main">
                    <div class="appt-date">${escapeHTML(item.AppoinmentDate || item.RequestDate || '—')}</div>
                    <div class="appt-type">${escapeHTML(item.ServiceType || '—')}</div>
                </div>
                <a href="${detailsUrl}" class="appt-status">
                    <span class="badge ${statusClass}">${escapeHTML(status)}</span>
                    <span class="appt-chevron" aria-hidden="true"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M9 6l6 6-6 6"></path></svg></span>
                </a>
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
