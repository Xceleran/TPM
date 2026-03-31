
let table = null;
var sites = [];
var siteAppointmentsCache = {};
var IsSiteDataLoading = true;

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
            var savedDate = sessionStorage.getItem('siteApptDateFilter');
            var savedStatus = sessionStorage.getItem('siteApptStatusFilter');
            if (savedDate) $('#siteApptDateFilter').val(savedDate);
            if (savedStatus) $('#siteApptStatusFilter').val(savedStatus);
        }
    });
}

function saveSiteFiltersToSession() {
    sessionStorage.setItem('siteApptDateFilter', $('#siteApptDateFilter').val() || '');
    sessionStorage.setItem('siteApptStatusFilter', $('#siteApptStatusFilter').val() || '');
}

function clearSiteFiltersFromSession() {
    sessionStorage.removeItem('siteApptDateFilter');
    sessionStorage.removeItem('siteApptStatusFilter');
}

$(document).ready(function () {

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

    // Site appointment filter - Search button
    $('#siteFilterSearchBtn').on('click', function () {
        saveSiteFiltersToSession();
        if ($.fn.DataTable.isDataTable('#customerSiteTable')) {
            $('#customerSiteTable').DataTable().draw();
        }
    });

    // Site appointment filter - Clear button
    $('#siteFilterClearBtn').on('click', function () {
        $('#siteApptDateFilter').val('');
        $('#siteApptStatusFilter').val('');
        clearSiteFiltersFromSession();
        if ($.fn.DataTable.isDataTable('#customerSiteTable')) {
            $('#customerSiteTable').DataTable().draw();
        }
    });

    $('#statusFilter').on('change', function () {
        if (table) table.draw(false);
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
   $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        filter: true,
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
                    hideNoAppointments: $('#hideNA').is(':checked')
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
            { data: "Email", name: "Email", autoWidth: true },
            {
                data: "StatusName",
                name: "Status",
                autoWidth: true,
                render: function (data) {
                    let statusText = data || 'N/A'; // Changed from const status to let statusText
                    let statusClass = 'status-na';
                    switch (statusText.toLowerCase()) { // Used statusText.toLowerCase()
                        case 'pending': statusClass = 'status-pending'; break;
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
            if (api.rows({ page: 'current' }).count() > 0 && !$('#customerTable tbody tr.selected').length) {
             //   selectFirstVisibleRow(); // Re-added this line
                IsSiteDataLoading = false;
            }
        }
    });

    $('#customerTable tbody').on('click', 'tr', function () {
        if ($(this).hasClass('selected')) return;
        var data = $('#customerTable').DataTable().row(this).data();
        if (data) {
         
            IsSiteDataLoading = false;
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

    const fullAddress = [safe(data.Address1), safe(data.City), safe(data.State), safe(data.ZipCode)].filter(Boolean).join(', ');
    updateItem('customerAddress', fullAddress);

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
                    appointmentStartDate: $('#siteApptDateFilter').val() || '',
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
    if (!$('#resource_list option[value="0"]').length) {
        $('#resource_list').prepend(new Option("Unassigned", "0"));
    }
    populateDropdown("MainContent_StatusTypeFilter_Edit", cslApptStatuses, "StatusID", "StatusName", "Select Status");
    populateDropdown("MainContent_TicketStatusFilter_Edit", cslTicketStatuses, "StatusID", "StatusName", "Select Ticket Status");
    populateTimeSlots();

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
                $('#editCustomerId').val(details.CustomerID);
                $('#editAppointmentForm').data('site-id', siteId);

                // Populate Customer/Site Info (Left Column)
              
                $('#custModal_CustomerName').val(details.CustomerName);
                $('#custModal_SiteName').val(details.SiteName || '');
                    // For email, phone, mobile, country - ensure we use the correct property names from site object
                $('#custModal_Address').val(details.Address || '');
                $('#custModal_City').val(details.City || '');
                $('#custModal_State').val(details.State || '');
                $('#custModal_Zip').val(details.Zip || '');
                $('#custModal_Country').val(details.Country || '');
                $('#custModal_Email').val(details.Email || '');
                $('#custModal_Phone').val(details.PhoneNumber || '');
                $('#custModal_Mobile').val(details.MobileNumber || '');
               

                $('#MainContent_ServiceTypeFilter_Edit').val(details.ServiceTypeID || "");
                $('#resource_list').val(details.ResourceID || "0");

                setDropdownByTextOrValue('MainContent_StatusTypeFilter_Edit', details.Status);
                setDropdownByTextOrValue('MainContent_TicketStatusFilter_Edit', details.TicketStatus);
                setDropdownByTextOrValue('time_slot', details.TimeSlot);


                $('#dateInput').val(details.Date);

                let startMoment = null;
                if (details.Date) {
                    startMoment = moment(`${details.Date} ${details.Hour}:${details.Minute}`, "YYYY-MM-DD H:m");
                    $('#txt_StartDate').val(startMoment.format("MM/DD/YYYY hh:mm A"));
                } else {
                    $('#txt_StartDate').val('');
                }

                $('#duration').val(details.Duration || "");

                if (startMoment && startMoment.isValid() && details.Duration) {
                    const totalMinutes = parseDuration(details.Duration);
                    if (totalMinutes > 0) {
                        const newEndDateTime = startMoment.clone().add(totalMinutes, 'minutes');
                        $('#txt_EndDate').val(newEndDateTime.format('MM/DD/YYYY hh:mm A'));
                    } else {
                        $('#txt_EndDate').val('');
                    }
                } else if (details.EndDateTime) {
                    $('#txt_EndDate').val(moment(details.EndDateTime).format('MM/DD/YYYY hh:mm A'));
                }
                else {
                    $('#txt_EndDate').val('');
                }


                $('#editApptNote').val(details.Note);

                // Reset Tabs
                const tabTrigger = new bootstrap.Tab(document.querySelector('#editAppointmentTabs button[data-bs-target="#appointment-details"]'));
                tabTrigger.show();

                // Load Forms for this appointment (in Forms tab)
                // Use the shared function which calls Appointments.aspx/Method
                // We need to ensure attached forms are loaded. 
                // Since we don't have 'AttachedForms' in 'details' from Customer.aspx (presumably),
                // we might need to fetch them or assume they load when tab is clicked?
                // Actually loadAppointmentSpecificLinks loads them for the side panel. 
                // But for the Forms TAB, we need to populate #selectedFormsEdit
                // We will call a helper function here.
               // loadFormsForModal(details.ApptID);
               // loadCustomFields(null, details.ApptID);

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

function populateTimeSlots() {
    const $timeSlot = $('#time_slot');
    if (!$timeSlot.length) return;
    $timeSlot.empty();
    $timeSlot.append('<option value="">Select Time Slot</option>');

    

    // Add 30-min increments from 8 AM to 8 PM
    for (let h = 8; h < 20; h++) {
        for (let m = 0; m < 60; m += 30) {
            let hh = h % 12 || 12;
            let ampm = h >= 12 ? "PM" : "AM";
            let h_next = m === 30 ? h + 1 : h;
            let m_next = m === 30 ? 0 : 30;
            let hh_next = h_next % 12 || 12;
            let ampm_next = h_next >= 12 ? "PM" : "AM";

            let slotText = `${hh.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')} ${ampm} - ${hh_next.toString().padStart(2, '0')}:${m_next.toString().padStart(2, '0')} ${ampm_next}`;
            $timeSlot.append(new Option(slotText, slotText.toLowerCase()));
        }
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
                    <div class="appt-date">${escapeHTML(item.AppoinmentDate || item.RequestDate || '—')}</div>
                    <div class="appt-type">${escapeHTML(item.ServiceType || '—')}</div>
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
