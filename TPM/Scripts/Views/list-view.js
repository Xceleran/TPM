
const ListViewManager = {
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    filteredAppointments: [],
    currentSort: {
        key: 'RequestDate',
        direction: 'asc'
    },
    init: function () {
        this.bindEvents();

        $('#list-tab').on('shown.bs.tab', () => {
            this.render();
        });
    },
    bindEvents: function () {
        const self = this;

        $('#search_term, #dispatchGroupListView, #individualResourceFilterListView, #MainContent_ServiceTypeFilter_ListView, #MainContent_StatusTypeFilter_ListView, #MainContent_TicketStatusFilter_ListView').on('input change', () => {
            self.currentPage = 1;
            self.render();
        });

        $('#clearFilterButton').on('click', (e) => self.clearFilters(e));

        $(document).on('click', '#listView th.sortable', function () {
            const key = $(this).data('key');
            self.applySort(key);
        });

        $('#listViewPrevPage').on('click', (e) => {
            e.preventDefault();
            self.goToPage(self.currentPage - 1);
        });
        $('#listViewNextPage').on('click', (e) => {
            e.preventDefault();
            self.goToPage(self.currentPage + 1);
        });
        $('#listViewPageSize').on('change', () => self.changePageSize());
    },

    render: function () {
        const self = this;
        $('#listViewLoading').show();

        const { fromDate, toDate } = this.getDateRange();
        const filters = this.getFilters();

        getAppoinments(filters.searchTerm, fromDate, toDate, "", function (appointments) {
            self.filteredAppointments = self.filterAppointments(appointments, filters);
            self.sortAppointments();
            self.renderTable();
            self.updatePagination();
            $('#listViewLoading').hide();
        });
    },

    renderTable: function () {
        const tbody = $("#listTableBody");
        const startIndex = (this.currentPage - 1) * this.pageSize;
        const endIndex = startIndex + this.pageSize;
        const pageAppointments = this.filteredAppointments.slice(startIndex, endIndex);

        if (pageAppointments.length === 0) {
            tbody.html('<tr><td colspan="13" class="text-center py-4 text-muted">No appointments match the current filters.</td></tr>');
            return;
        }

        const tableRowsHtml = pageAppointments.map(a => this.createTableRow(a)).join('');
        tbody.html(tableRowsHtml);
    },

    createTableRow: function (appointment) {
        const data = {
            id: appointment.AppoinmentId,
            customerId: appointment.CustomerID,
            customerName: appointment.CustomerName || 'N/A',
            businessName: appointment.BusinessName || 'N/A',
            address: appointment.SiteAddress || appointment.Address1 || 'N/A',
            requestDate: appointment.RequestDate ? formatToUSDate(appointment.RequestDate) : 'N/A',
            timeSlot: appointment.TimeSlot ? formatTimeRange(appointment.TimeSlot) : 'N/A',
            serviceType: appointment.ServiceType || 'N/A',
            email: appointment.Email,
            mobile: appointment.Mobile,
            phone: appointment.Phone,
            status: appointment.AppoinmentStatus || 'N/A',
            resource: appointment.ResourceName || 'Unassigned',
            ticketStatus: appointment.TicketStatus && appointment.TicketStatus !== 'Unknown' ? appointment.TicketStatus : 'N/A'
        };

        const actionMenu = `
            <div class="dropdown">
              <button class="btn btn-sm btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-container=".card-body" aria-expanded="false">
                <i class="fas fa-ellipsis-v"></i>
              </button>
              <ul class="dropdown-menu">
                <li><a class="dropdown-item" href="javascript:void(0);" onclick="openEditModal('${data.id}')"><i class="fas fa-eye me-2"></i>View Details</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item" href="#"><i class="fas fa-file-invoice-dollar me-2"></i>Invoices</a></li>
                <li><a class="dropdown-item" href="#"><i class="fas fa-file-alt me-2"></i>Estimates</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item text-danger" href="javascript:void(0);" onclick="ListViewManager.confirmDeleteAppointment('${data.id}')"><i class="fas fa-trash-alt me-2"></i>Delete</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item" href="javascript:void(0);" onclick="ListViewManager.openReminderEmailModal('${data.id}', '${data.customerId}')">Send Reminder Email</a></li>
                <li><a class="dropdown-item" href="javascript:void(0);" onclick="ListViewManager.openReminderSmsModal('${data.id}', '${data.customerId}', '${data.mobile}')">Send Reminder SMS</a></li>
                <li><a class="dropdown-item" href="javascript:void(0);" onclick="ListViewManager.openFollowUpEmailModal('${data.id}', '${data.customerId}')">Send Follow Up Email</a></li>
              </ul>
            </div>`;

        return `
            <tr data-id="${data.id}">
                <td data-label="Actions">${actionMenu}</td>
                <td data-label="Customer">${data.customerName}</td>
                <td data-label="Business Name">${data.businessName}</td>
                <td data-label="Address">${data.address}</td>
                <td data-label="Request Date">${data.requestDate}</td>
                <td data-label="Time Slot">${data.timeSlot}</td>
                <td data-label="Service Type">${data.serviceType}</td>
                <td data-label="Email">${data.email ? `<a href="mailto:${data.email}" class="custom-link">${data.email}</a>` : 'N/A'}</td>
                <td data-label="Mobile">${data.mobile ? `<a href="tel:${data.mobile}" class="custom-link">${data.mobile}</a>` : 'N/A'}</td>
                <td data-label="Phone">${data.phone ? `<a href="tel:${data.phone}" class="custom-link">${data.phone}</a>` : 'N/A'}</td>
                <td data-label="Status">${data.status}</td>
                <td data-label="Resource">${data.resource}</td>
                <td data-label="Ticket Status">${data.ticketStatus}</td>
            </tr>`;
    },

    getDateRange: function () {
        const viewMode = $('#viewSelect').val();
        const selectedDate = $('#dayDatePicker').val();

        // Get custom dates from shared controls
        const fromCustom = $("#datePickerFrom").val();
        const toCustom = $("#datePickerTo").val();

        if (viewMode === 'custom' && fromCustom && toCustom) {
            return { fromDate: fromCustom, toDate: toCustom };
        }

        const range = computeRangeByMode(viewMode, selectedDate);
        return { fromDate: range.from, toDate: range.to };
    },

    getFilters: function () {
        const norm = s => String(s ?? '').trim().toLowerCase();
        const getFilter = (type) => {
            const $el = $(`#MainContent_${type}Filter_ListView`);
            const val = norm($el.val());
            const text = norm($el.find("option:selected").text());
            const isAll = val === '' || /^all/i.test(val) || /^all/i.test(text) || val === 'all';
            return {
                isAll: isAll,
                id: (!isAll && /^\d+$/.test(val)) ? val : null,
                text: isAll ? '' : text
            };
        };
        return {
            searchTerm: norm($("#search_term").val()),
            serviceType: getFilter('ServiceType'),
            statusType: getFilter('StatusType'),
            ticketStatus: getFilter('TicketStatus'),
            resourceGroup: $("#dispatchGroupListView").val(),
            individualResource: $("#individualResourceFilterListView").val()
        };
    },

    filterAppointments: function (appointments, filters) {
        const norm = s => String(s ?? '').trim().toLowerCase();
        return (appointments || []).filter(item => {
            const f = filters;
            let ok = true;
            if (!f.serviceType.isAll) {
                const itemTypeId = item.ServiceTypeID ?? item.ServiceTypeId ?? null;
                ok = (f.serviceType.id && itemTypeId) ? String(itemTypeId) === f.serviceType.id : norm(item.ServiceType) === f.serviceType.text;
            }
            if (ok && !f.statusType.isAll) {
                const itemStatusId = item.AppoinmentStatusID ?? null;
                ok = (f.statusType.id && itemStatusId) ? String(itemStatusId) === f.statusType.id : norm(item.AppoinmentStatus) === f.statusType.text;
            }
            if (ok && !f.ticketStatus.isAll) {
                const itemTicketId = item.TicketStatusID ?? null;
                ok = (f.ticketStatus.id && itemTicketId) ? String(itemTicketId) === f.ticketStatus.id : norm(item.TicketStatus) === f.ticketStatus.text;
            }
            if (ok && f.searchTerm) {
                const combinedText = [item.CustomerName, item.BusinessName, item.ResourceName, item.Email, item.Mobile, item.Phone, item.Address1].map(norm).join(" ");
                ok = combinedText.includes(f.searchTerm);
            }
            if (ok && f.individualResource && f.individualResource !== 'all') {
                ok = String(item.ResourceID || item.ResourceId || 0) === String(f.individualResource);
            }
            if (ok && f.resourceGroup && f.resourceGroup !== 'all') {
                const groupMembers = technicianGroups[f.resourceGroup] || [];
                ok = groupMembers.includes(item.ResourceName);
            }
            return ok;
        });
    },

    applySort: function (key) {
        if (this.currentSort.key === key) {
            this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            this.currentSort.key = key;
            this.currentSort.direction = 'asc';
        }
        $('#listView th.sortable').removeClass('sort-asc sort-desc');
        $(`#listView th.sortable[data-key="${key}"]`).addClass(this.currentSort.direction === 'asc' ? 'sort-asc' : 'sort-desc');

        this.currentPage = 1;
        this.sortAppointments();
        this.renderTable();
        this.updatePagination();
    },

    sortAppointments: function () {
        const { key, direction } = this.currentSort;
        const dir = direction === 'asc' ? 1 : -1;
        this.filteredAppointments.sort((a, b) => {
            let valA, valB;
            if (key === 'RequestDate') {
                valA = new Date(a.RequestDate).getTime() || 0;
                valB = new Date(b.RequestDate).getTime() || 0;
            } else if (key === 'TimeSlot') {
                valA = parseTimeToMinutes(a.TimeSlot);
                valB = parseTimeToMinutes(b.TimeSlot);
            } else {
                valA = (a[key] || '').toString().toLowerCase();
                valB = (b[key] || '').toString().toLowerCase();
            }
            if (valA < valB) return -1 * dir;
            if (valA > valB) return 1 * dir;
            return 0;
        });
    },

    clearFilters: function (e) {
        e.preventDefault();
        const todayStr = new Date().toISOString().split('T')[0];
        $("#dayDatePicker").val(todayStr);
        $("#datePickerFrom, #datePickerTo, #search_term").val("");
        $("#MainContent_StatusTypeFilter_ListView, #MainContent_ServiceTypeFilter_ListView, #MainContent_TicketStatusFilter_ListView").val("all");
        $("#viewSelect").val("day");
        this.currentPage = 1;
        this.currentSort = { key: 'RequestDate', direction: 'asc' };
        $('#listView th.sortable').removeClass('sort-asc sort-desc');
        $('#listView th.sortable[data-key="RequestDate"]').addClass('sort-asc');

        this.render();
    },

    updatePagination: function () {
        const totalItems = this.filteredAppointments.length;
        this.totalPages = Math.ceil(totalItems / this.pageSize) || 1;
        if (this.currentPage > this.totalPages) this.currentPage = this.totalPages;
        const startItem = totalItems > 0 ? (this.currentPage - 1) * this.pageSize + 1 : 0;
        const endItem = Math.min(this.currentPage * this.pageSize, totalItems);
        $('#listViewPageInfo').text(`Showing ${startItem}-${endItem} of ${totalItems}`);
        $('#listViewPrevPage').toggleClass('disabled', this.currentPage <= 1);
        $('#listViewNextPage').toggleClass('disabled', this.currentPage >= this.totalPages);
        $('#listViewPageSize').val(this.pageSize);
    },

    goToPage: function (page) {
        if (page < 1 || page > this.totalPages) return;
        this.currentPage = page;
        this.renderTable();
        this.updatePagination();
    },

    changePageSize: function () {
        this.pageSize = parseInt($('#listViewPageSize').val(), 10);
        this.currentPage = 1;
        this.renderTable();
        this.updatePagination();
    },

    openReminderEmailModal: function (appointmentId, customerId) {
        $('#reminderEmail_ApptId').val(appointmentId);
        $('#reminderEmail_CustId').val(customerId);
        const modal = new bootstrap.Modal(document.getElementById('sendReminderEmailModal'));
        modal.show();
    },

    openReminderSmsModal: function (appointmentId, customerId, mobile) {
        if (!mobile || String(mobile).trim() === '') {
            showAlert({ icon: 'warning', title: 'Missing Information', text: 'This customer does not have a mobile number on file.' });
            return;
        }
        $('#reminderSms_ApptId').val(appointmentId);
        $('#reminderSms_CustId').val(customerId);
        const modal = new bootstrap.Modal(document.getElementById('sendReminderSmsModal'));
        modal.show();
    },

    openFollowUpEmailModal: function (appointmentId, customerId) {
        $('#followUpEmail_ApptId').val(appointmentId);
        $('#followUpEmail_CustId').val(customerId);
        const modal = new bootstrap.Modal(document.getElementById('sendFollowUpEmailModal'));
        modal.show();
    },

    confirmDeleteAppointment: function (appointmentId) {
        showAlert({
            icon: 'warning',
            title: 'Are you sure?',
            text: "This action will permanently delete the appointment.",
            showCancelButton: true,
            confirmButtonText: 'Yes, delete it!',
            confirmButtonColor: '#d33'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    type: "POST",
                    url: "Appointments.aspx/DeleteAppointment",
                    data: JSON.stringify({ AppoinmentId: appointmentId }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: (response) => {
                        if (response.d) {
                            showAlert({ icon: 'success', title: 'Deleted!', timer: 1500 });
                            this.render();
                        } else {
                            showAlert({ icon: 'error', title: 'Deletion Failed' });
                        }
                    },
                    error: () => showAlert({ icon: 'error', title: 'Server Error' })
                });
            }
        });
    },

    syncWithGlobalDate: function () {
        // Sync list view with global date - trigger render if on list tab
        if ($('#list-tab').hasClass('active')) {
            this.render();
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    ListViewManager.init();

    $(document).on('change', '#dayDatePicker, #resourceDatePicker, #listDatePicker, #mapDatePicker', function () {
        GlobalDateSync.setDate(this.value);
        $('#dayDatePicker, #resourceDatePicker, #listDatePicker, #mapDatePicker').val(this.value);
        if (ListViewManager) {
            ListViewManager.syncWithGlobalDate();
        }
    });

    $(document).on('change', '#viewSelect, #resourceViewSelect, #listViewSelect', function () {
        GlobalDateSync.setViewMode(this.value);
        // Force list view to sync when view mode changes
        if (ListViewManager) {
            ListViewManager.syncWithGlobalDate();
        }
    });

    $('#viewTabs button[data-bs-toggle="tab"]').on('shown.bs.tab', function () {
        GlobalDateSync.synchronize();
    });

    GlobalDateSync.synchronize();
});
