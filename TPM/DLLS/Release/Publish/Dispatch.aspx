<%@ Page Title="Dispatch" Language="C#" MasterPageFile="~/FSM.Master" AutoEventWireup="true" CodeBehind="Dispatch.aspx.cs" Inherits="FSM.Dispatch" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* General Styles */
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
        }

        /* Header Styles */
        header {
            margin-bottom: 2rem;
            padding: 1rem;
        }

        .header-container {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            gap: 1rem;
        }

        h1 {
            font-size: 1.5rem;
            font-weight: bold;
            color: #f97316; /* orange-500 */
            margin: 0;
        }

        .search-create-container {
            display: flex;
            flex-direction: column;
            gap: 1rem;
            width: 100%;
        }

        #jobSearch {
            padding: 0.5rem 1rem;
            border: 1px solid #d1d5db; /* gray-300 */
            border-radius: 0.375rem;
            outline: none;
            width: 100%;
            background-color: #fff;
            box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
        }

        #jobSearch:focus {
            border-color: #3b82f6; /* blue-500 */
            box-shadow: 0 0 0 2px #3b82f6;
        }

        #createJobBtn {
            padding: 0.5rem 1rem;
            background-color: #2563eb; /* blue-600 */
            color: #fff;
            border: none;
            border-radius: 0.375rem;
            cursor: pointer;
            width: 100%;
        }

        #createJobBtn:hover {
            background-color: #1d4ed8; /* blue-700 */
        }

        #createJobBtn:focus {
            outline: none;
            box-shadow: 0 0 0 2px #3b82f6;
        }

        /* Section Styles */
        .dispatch-section {
            padding: 1rem;
            display: grid;
            grid-template-columns: 1fr;
            gap: 1.5rem;
        }

        .card {
            background-color: #fff;
            padding: 1rem;
            border-radius: 0.375rem;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        /* Jobs List */
        .jobs-list-container {
            order: 1;
        }

        .filter-container {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            margin-bottom: 1rem;
            gap: 1rem;
        }

        #statusFilter, #dateFilter {
            padding: 0.5rem;
            border: 1px solid #d1d5db;
            border-radius: 0.375rem;
            background-color: #fff;
            width: 100%;
            font-size: 0.875rem;
        }

        .jobs-header {
            display: none;
            grid-template-columns: repeat(5, 1fr);
            font-size: 0.75rem;
            text-transform: uppercase;
            color: #6b7280; /* gray-500 */
            font-weight: 500;
            margin-bottom: 0.5rem;
            padding: 0.5rem;
            background-color: #f3f4f6; /* gray-100 */
        }

        #jobsList {
            max-height: 50vh;
            overflow-y: auto;
        }

        .job-item {
            display: block;
            padding: 0.75rem;
            cursor: pointer;
            border-bottom: 1px solid #e5e7eb; /* gray-200 */
        }

        .job-item:hover {
            background-color: #f9fafb; /* gray-50 */
        }

        .status-tag {
            display: inline-block;
            padding: 0.25rem 0.5rem;
            border-radius: 9999px;
            font-size: 0.75rem;
            font-weight: 500;
        }

        .status-tag.open { background-color: #d1fae5; color: #065f46; }
        .status-tag.in-progress { background-color: #fef3c7; color: #92400e; }
        .status-tag.completed { background-color: #d1d5db; color: #374151; }

        /* Job Details */
        .job-details-container {
            order: 2;
        }

        .job-details-header {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            margin-bottom: 1rem;
            gap: 1rem;
        }

        h2 {
            font-size: 1.25rem;
            font-weight: 600;
            color: #1e40af; /* blue-800 */
            margin: 0;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .action-buttons {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
        }

        .action-btn {
            padding: 0.25rem 0.75rem;
            background-color: #e5e7eb; /* gray-200 */
            color: #4b5563; /* gray-700 */
            border: none;
            border-radius: 0.375rem;
            cursor: pointer;
            width: 100%;
        }

        .action-btn:hover {
            background-color: #d1d5db; /* gray-300 */
        }

        .action-btn:focus {
            outline: none;
            box-shadow: 0 0 0 2px #6b7280; /* gray-500 */
        }

        .details-list {
            font-size: 0.875rem;
        }

        .details-list div {
            display: flex;
            flex-direction: column;
            margin-bottom: 0.5rem;
        }

        .details-list label {
            width: 100%;
            color: #6b7280; /* gray-600 */
            font-weight: 500;
        }

        .details-list span {
            color: #1f2937; /* gray-800 */
        }

        /* Calendars */
        .calendars-container {
            display: grid;
            grid-template-columns: 1fr;
            gap: 1.5rem;
            order: 3;
        }

        .calendar-card h2 {
            font-size: 1.125rem;
            font-weight: 600;
            color: #1e40af; /* blue-800 */
            margin-bottom: 1rem;
        }

        #calendar, #optimizedCalendar {
            min-height: 400px;
        }

        .optimized-filter {
            display: flex;
            flex-direction: column;
            gap: 1rem;
            margin-bottom: 1rem;
        }

        #typeFilter, #geoFilter {
            padding: 0.5rem;
            border: 1px solid #d1d5db;
            border-radius: 0.375rem;
            background-color: #fff;
            width: 100%;
            font-size: 0.875rem;
        }

        /* Modal */
        #createJobModal {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.5);
            display: none;
            align-items: center;
            justify-content: center;
            z-index: 50;
            padding: 1rem;
        }

        #createJobModal.flex {
            display: flex;
        }

        .modal-content {
            background-color: #fff;
            padding: 1.5rem;
            border-radius: 0.375rem;
            width: 100%;
            max-width: 28rem;
        }

        .modal-content h2 {
            font-size: 1.25rem;
            font-weight: 600;
            color: #1f2937; /* gray-800 */
            margin-bottom: 1rem;
        }

        #createJobForm div {
            margin-bottom: 1rem;
        }

        #createJobForm label {
            display: block;
            color: #374151; /* gray-700 */
            font-weight: 500;
            margin-bottom: 0.25rem;
        }

        #createJobForm select, #createJobForm input {
            padding: 0.5rem;
            border: 1px solid #d1d5db;
            border-radius: 0.375rem;
            width: 100%;
            outline: none;
        }

        #createJobForm select:focus, #createJobForm input:focus {
            border-color: #3b82f6;
            box-shadow: 0 0 0 2px #3b82f6;
        }

        .modal-buttons {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
        }

        #createJobForm button[type="submit"] {
            padding: 0.5rem 1rem;
            background-color: #2563eb;
            color: #fff;
            border: none;
            border-radius: 0.375rem;
            cursor: pointer;
        }

        #createJobForm button[type="submit"]:hover {
            background-color: #1d4ed8;
        }

        #closeCreateJob {
            padding: 0.5rem 1rem;
            background-color: #e5e7eb;
            color: #4b5563;
            border: none;
            border-radius: 0.375rem;
            cursor: pointer;
        }

        #closeCreateJob:hover {
            background-color: #d1d5db;
        }

        /* Responsive Adjustments */
        @media (min-width: 640px) {
            .header-container {
                flex-direction: row;
                align-items: center;
            }

            h1 {
                font-size: 1.875rem;
            }

            .search-create-container {
                flex-direction: row;
                align-items: center;
                width: auto;
            }

            #jobSearch {
                width: 16rem;
            }

            #createJobBtn {
                width: auto;
            }

            .dispatch-section {
                padding: 1.5rem;
            }

            .card {
                padding: 1.5rem;
            }

            .filter-container {
                flex-direction: row;
                align-items: center;
            }

            #statusFilter, #dateFilter {
                width: 10rem;
            }

            .jobs-header {
                display: grid;
            }

            #jobsList {
                max-height: calc(100vh - 300px);
            }

            .job-item {
                display: grid;
                grid-template-columns: repeat(5, 1fr);
            }

            .job-details-header {
                flex-direction: row;
                align-items: center;
            }

            h2 {
                font-size: 1.5rem;
            }

            .action-btn {
                width: auto;
            }

            .details-list div {
                flex-direction: row;
            }

            .details-list label {
                width: 33.33%;
            }

            .optimized-filter {
                flex-direction: row;
            }

            #typeFilter, #geoFilter {
                width: 10rem;
            }
        }

        @media (min-width: 1024px) {
            .calendars-container {
                grid-template-columns: 1fr 1fr;
            }
        }
    </style>

    <!-- Main Content -->
    <header>
        <div class="header-container">
            <h1>Dispatch</h1>
            <div class="search-create-container">
                <input type="text" placeholder="Search jobs..." id="jobSearch" />
                <button id="createJobBtn">+ Create Job</button>
            </div>
        </div>
    </header>

    <!-- Dispatch Section -->
    <section class="dispatch-section">
        <!-- Jobs List -->
        <div class="card jobs-list-container">
            <div class="filter-container">
                <select id="statusFilter">
                    <option value="">All Statuses</option>
                    <option value="open">Open</option>
                    <option value="in-progress">In Progress</option>
                    <option value="completed">Completed</option>
                </select>
                <input type="date" id="dateFilter" />
            </div>
            <div class="jobs-header">
                <span>ID</span>
                <span>Customer</span>
                <span>Site</span>
                <span>Status</span>
                <span>Date</span>
            </div>
            <div class="jobs-list" id="jobsList">
                <!-- Populated via JS -->
            </div>
        </div>

        <!-- Job Details -->
        <div class="card job-details-container">
            <div class="job-details-header">
                <h2 id="jobId">Select a Job</h2>
                <div class="action-buttons">
                    <button class="action-btn" id="reviewBtn">Review</button>
                    <button class="action-btn" id="invoiceBtn">Invoice</button>
                    <button class="action-btn" id="sendPaymentBtn">Send Payment Link</button>
                    <button class="action-btn" id="openVTBtn">Open VT</button>
                </div>
            </div>
            <div class="details-list">
                <div><label>Customer:</label><span id="detailCustomer">-</span></div>
                <div><label>Site:</label><span id="detailSite">-</span></div>
                <div><label>Status:</label><span id="detailStatus">-</span></div>
                <div><label>Date:</label><span id="detailDate">-</span></div>
                <div><label>Description:</label><span id="detailDescription">-</span></div>
                <div><label>Technician:</label><span id="detailTech">-</span></div>
                <div><label>Billable:</label><span id="detailBillable">-</span></div>
                <div><label>Geo-Location:</label><span id="detailGeo">-</span></div>
                <div><label>Job Type:</label><span id="detailType">-</span></div>
            </div>
        </div>

        <!-- Calendars Container -->
        <div class="calendars-container">
            <!-- Dispatch Schedule Calendar -->
            <div class="card calendar-card">
                <h2>Dispatch Schedule</h2>
                <div id="calendar"></div>
            </div>

            <!-- Optimized Dispatch Calendar -->
            <div class="card calendar-card">
                <h2>Optimized Dispatch (Geo & Load)</h2>
                <div class="optimized-filter">
                    <select id="typeFilter">
                        <option value="">All Job Types</option>
                        <option value="maintenance">Maintenance</option>
                        <option value="repair">Repair</option>
                        <option value="installation">Installation</option>
                    </select>
                    <select id="geoFilter">
                        <option value="">All Locations</option>
                        <option value="north">North</option>
                        <option value="south">South</option>
                        <option value="east">East</option>
                        <option value="west">West</option>
                    </select>
                </div>
                <div id="optimizedCalendar"></div>
            </div>
        </div>
    </section>

    <!-- Create Job Modal -->
    <div id="createJobModal">
        <div class="modal-content">
            <h2>Create New Job</h2>
            <form id="createJobForm">
                <div>
                    <label for="createCustomer">Customer</label>
                    <select name="customer" id="createCustomer" required>
                        <option value="">Select Customer</option>
                        <option value="John Doe">John Doe</option>
                        <option value="ABC Property Mgmt">ABC Property Mgmt</option>
                    </select>
                </div>
                <div>
                    <label for="createSite">Site</label>
                    <select name="site" id="createSite" required>
                        <option value="">Select Site</option>
                    </select>
                </div>
                <div>
                    <label for="createDescription">Description</label>
                    <input type="text" name="description" id="createDescription" required />
                </div>
                <div>
                    <label for="createDate">Date</label>
                    <input type="date" name="date" id="createDate" required />
                </div>
                <div>
                    <label for="createTech">Technician</label>
                    <select name="tech" id="createTech">
                        <option value="">Unassigned</option>
                        <option value="Jane Doe">Jane Doe</option>
                        <option value="Mark Smith">Mark Smith</option>
                    </select>
                </div>
                <div>
                    <label for="createGeo">Geo-Location</label>
                    <select name="geo" id="createGeo" required>
                        <option value="">Select Location</option>
                        <option value="north">North</option>
                        <option value="south">South</option>
                        <option value="east">East</option>
                        <option value="west">West</option>
                    </select>
                </div>
                <div>
                    <label for="createType">Job Type</label>
                    <select name="type" id="createType" required>
                        <option value="">Select Type</option>
                        <option value="maintenance">Maintenance</option>
                        <option value="repair">Repair</option>
                        <option value="installation">Installation</option>
                    </select>
                </div>
                <div class="modal-buttons">
                    <button type="submit">Create</button>
                    <button type="button" id="closeCreateJob">Cancel</button>
                </div>
            </form>
        </div>
    </div>

    <!-- Scripts -->
    <script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.15/index.global.min.js"></script>

    <script>
        document.addEventListener('DOMContentLoaded', () => {
            // Sample Data with Geo-Location and Job Type
            const jobs = [
                { id: '2001', customer: 'John Doe', site: 'Main Residence', status: 'open', date: '2025-03-15', start: '2025-03-15T09:00:00', end: '2025-03-15T10:00:00', description: 'HVAC Maintenance', tech: 'Jane Doe', billable: 'HVAC Service - $450', geo: 'north', type: 'maintenance', load: 2 },
                { id: '2002', customer: 'ABC Property Mgmt', site: 'Property A', status: 'in-progress', date: '2025-03-16', start: '2025-03-16T10:15:00', end: '2025-03-16T11:45:00', description: 'Plumbing Repair', tech: 'Mark Smith', billable: 'Plumbing - $300', geo: 'south', type: 'repair', load: 3 }
            ];

            // Modal Handlers
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

            // Create Job
            const createJobBtn = document.getElementById('createJobBtn');
            if (createJobBtn) {
                createJobBtn.addEventListener('click', () => {
                    openModal('createJobModal');
                    updateSiteOptions();
                });
            }

            const createCustomerSelect = document.getElementById('createCustomer');
            if (createCustomerSelect) {
                createCustomerSelect.addEventListener('change', updateSiteOptions);
            }

            function updateSiteOptions() {
                const customer = document.getElementById('createCustomer').value;
                const siteSelect = document.getElementById('createSite');
                if (siteSelect) {
                    siteSelect.innerHTML = '<option value="">Select Site</option>';
                    if (customer === 'John Doe') {
                        siteSelect.innerHTML += '<option value="Main Residence">Main Residence</option><option value="Vacation Home">Vacation Home</option>';
                    } else if (customer === 'ABC Property Mgmt') {
                        siteSelect.innerHTML += '<option value="Property A">Property A</option><option value="Property B">Property B</option>';
                    }
                }
            }

            const closeCreateJobBtn = document.getElementById('closeCreateJob');
            if (closeCreateJobBtn) {
                closeCreateJobBtn.addEventListener('click', () => closeModal('createJobModal'));
            }

            const createJobForm = document.getElementById('createJobForm');
            if (createJobForm) {
                createJobForm.addEventListener('submit', (e) => {
                    e.preventDefault();
                    const formData = new FormData(e.target);
                    const jobDate = formData.get('date');
                    const job = {
                        id: `20${Math.floor(Math.random() * 1000)}`,
                        customer: formData.get('customer'),
                        site: formData.get('site'),
                        status: 'open',
                        date: jobDate,
                        start: `${jobDate}T09:00:00`,
                        end: `${jobDate}T10:00:00`,
                        description: formData.get('description'),
                        tech: formData.get('tech') || 'Unassigned',
                        billable: `${formData.get('description')} - $TBD`,
                        geo: formData.get('geo'),
                        type: formData.get('type'),
                        load: Math.floor(Math.random() * 3) + 1 // Random load between 1-3 for demo
                    };
                    jobs.push(job);
                    addJobToList(job);
                    calendar.refetchEvents();
                    optimizedCalendar.refetchEvents();
                    closeModal('createJobModal');
                    e.target.reset();
                });
            }

            function addJobToList(job) {
                const list = document.getElementById('jobsList');
                if (list) {
                    const item = document.createElement('div');
                    item.className = 'job-item';
                    item.dataset.id = job.id;
                    item.innerHTML = `
                <div><span class="mobile-label">ID: </span>#${job.id}</div>
                <div><span class="mobile-label">Customer: </span>${job.customer}</div>
                <div><span class="mobile-label">Site: </span>${job.site}</div>
                <div><span class="mobile-label">Status: </span><span class="status-tag ${job.status}">${job.status.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())}</span></div>
                <div><span class="mobile-label">Date: </span>${job.date}</div>
            `;
                    list.appendChild(item);
                    item.addEventListener('click', () => selectJob(job.id));
                }
            }

            // Select Job
            function selectJob(id) {
                const items = document.querySelectorAll('.job-item');
                if (items) {
                    items.forEach(i => i.style.backgroundColor = '');
                    const item = document.querySelector(`.job-item[data-id="${id}"]`);
                    if (item) {
                        item.style.backgroundColor = '#dbeafe'; // Light blue for selection
                        const job = jobs.find(j => j.id === id);
                        document.getElementById('jobId').textContent = `Job #${job.id}`;
                        document.getElementById('detailCustomer').textContent = job.customer;
                        document.getElementById('detailSite').textContent = job.site;
                        document.getElementById('detailStatus').textContent = job.status.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase());
                        document.getElementById('detailDate').textContent = job.date;
                        document.getElementById('detailDescription').textContent = job.description;
                        document.getElementById('detailTech').textContent = job.tech;
                        document.getElementById('detailBillable').textContent = job.billable;
                        document.getElementById('detailGeo').textContent = job.geo ? job.geo.charAt(0).toUpperCase() + job.geo.slice(1) : '-';
                        document.getElementById('detailType').textContent = job.type ? job.type.charAt(0).toUpperCase() + job.type.slice(1) : '-';
                    }
                }
            }

            // Job Actions
            const reviewBtn = document.getElementById('reviewBtn');
            if (reviewBtn) {
                reviewBtn.addEventListener('click', () => {
                    const id = document.querySelector('.job-item[style*="background-color"]')?.dataset.id;
                    if (id) {
                        const job = jobs.find(j => j.id === id);
                        alert(`Reviewing Job #${id}\nDescription: ${job.description}\nStatus: ${job.status}`);
                    }
                });
            }

            const invoiceBtn = document.getElementById('invoiceBtn');
            if (invoiceBtn) {
                invoiceBtn.addEventListener('click', () => {
                    const id = document.querySelector('.job-item[style*="background-color"]')?.dataset.id;
                    if (id) {
                        const job = jobs.find(j => j.id === id);
                        job.status = 'completed';
                        updateJobStatus(id);
                        calendar.refetchEvents();
                        optimizedCalendar.refetchEvents();
                        alert(`Invoiced Job #${id}: ${job.billable}`);
                    }
                });
            }

            const sendPaymentBtn = document.getElementById('sendPaymentBtn');
            if (sendPaymentBtn) {
                sendPaymentBtn.addEventListener('click', () => {
                    const id = document.querySelector('.job-item[style*="background-color"]')?.dataset.id;
                    if (id) alert(`Payment link sent for Job #${id}`);
                });
            }

            const openVTBtn = document.getElementById('openVTBtn');
            if (openVTBtn) {
                openVTBtn.addEventListener('click', () => {
                    const id = document.querySelector('.job-item[style*="background-color"]')?.dataset.id;
                    if (id) alert(`Opening Virtual Terminal for Job #${id}`);
                });
            }

            function updateJobStatus(id) {
                const job = jobs.find(j => j.id === id);
                const item = document.querySelector(`.job-item[data-id="${id}"]`);
                if (item) {
                    const statusSpan = item.querySelector('.status-tag');
                    statusSpan.className = `status-tag ${job.status}`;
                    statusSpan.textContent = job.status.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase());
                    document.getElementById('detailStatus').textContent = job.status.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase());
                }
            }

            // Filter Jobs
            const statusFilter = document.getElementById('statusFilter');
            const dateFilter = document.getElementById('dateFilter');
            const jobSearch = document.getElementById('jobSearch');
            const typeFilter = document.getElementById('typeFilter');
            const geoFilter = document.getElementById('geoFilter');
            if (statusFilter && dateFilter && jobSearch && typeFilter && geoFilter) {
                statusFilter.addEventListener('change', filterJobs);
                dateFilter.addEventListener('change', filterJobs);
                jobSearch.addEventListener('input', filterJobs);
                typeFilter.addEventListener('change', filterOptimizedJobs);
                geoFilter.addEventListener('change', filterOptimizedJobs);
            }

            function filterJobs() {
                const status = statusFilter.value;
                const date = dateFilter.value;
                const query = jobSearch.value.toLowerCase();
                const items = document.querySelectorAll('.job-item');
                if (items) {
                    items.forEach(item => {
                        const job = jobs.find(j => j.id === item.dataset.id);
                        const matchesStatus = !status || job.status === status;
                        const matchesDate = !date || job.date === date;
                        const matchesQuery = !query ||
                            job.id.includes(query) ||
                            job.customer.toLowerCase().includes(query) ||
                            job.site.toLowerCase().includes(query);
                        item.style.display = matchesStatus && matchesDate && matchesQuery ? '' : 'none';
                    });
                    calendar.refetchEvents();
                }
            }

            function filterOptimizedJobs() {
                optimizedCalendar.refetchEvents();
            }

            // FullCalendar Setup - Dispatch Schedule
            const calendarEl = document.getElementById('calendar');
            let calendar;
            if (calendarEl) {
                calendar = new FullCalendar.Calendar(calendarEl, {
                    initialView: 'timeGridDay',
                    initialDate: '2025-03-21', // Updated to current date (March 21, 2025)
                    headerToolbar: {
                        left: 'prev,next today',
                        center: 'title',
                        right: 'timeGridDay,dayGridMonth'
                    },
                    events: function (fetchInfo, successCallback) {
                        const filteredJobs = jobs.filter(job => {
                            const status = statusFilter.value;
                            const date = dateFilter.value;
                            const query = jobSearch.value.toLowerCase();
                            const matchesStatus = !status || job.status === status;
                            const matchesDate = !date || job.date === date;
                            const matchesQuery = !query ||
                                job.id.includes(query) ||
                                job.customer.toLowerCase().includes(query) ||
                                job.site.toLowerCase().includes(query);
                            return matchesStatus && matchesDate && matchesQuery;
                        });
                        successCallback(filteredJobs.map(job => ({
                            id: job.id,
                            title: `#${job.id} - ${job.description} (${job.tech})`,
                            start: job.start,
                            end: job.end,
                            status: job.status,
                            classNames: [job.status]
                        })));
                    },
                    eventClick: function (info) {
                        const job = jobs.find(j => j.id === info.event.id);
                        selectJob(job.id);
                        alert(`Job Details:\nID: ${job.id}\nCustomer: ${job.customer}\nSite: ${job.site}\nStatus: ${job.status}\nTech: ${job.tech}`);
                    },
                    slotMinTime: '08:00:00',
                    slotMaxTime: '18:00:00',
                    height: 'auto'
                });
                calendar.render();
            }

            // FullCalendar Setup - Optimized Dispatch
            const optimizedCalendarEl = document.getElementById('optimizedCalendar');
            let optimizedCalendar;
            if (optimizedCalendarEl) {
                optimizedCalendar = new FullCalendar.Calendar(optimizedCalendarEl, {
                    initialView: 'timeGridDay',
                    initialDate: '2025-03-21', // Updated to current date (March 21, 2025)
                    headerToolbar: {
                        left: 'prev,next today',
                        center: 'title',
                        right: 'timeGridDay,dayGridMonth'
                    },
                    events: function (fetchInfo, successCallback) {
                        const type = typeFilter.value;
                        const geo = geoFilter.value;
                        const filteredJobs = jobs.filter(job => {
                            const matchesType = !type || job.type === type;
                            const matchesGeo = !geo || job.geo === geo;
                            return matchesType && matchesGeo && job.status !== 'completed'; // Exclude completed jobs
                        });
                        // Basic load balancing: Sort by technician load (simplified)
                        const techLoads = {};
                        filteredJobs.forEach(job => {
                            techLoads[job.tech] = (techLoads[job.tech] || 0) + job.load;
                        });
                        const balancedJobs = filteredJobs.sort((a, b) => {
                            const loadA = techLoads[a.tech] || 0;
                            const loadB = techLoads[b.tech] || 0;
                            return loadA - loadB; // Prioritize lower load
                        });
                        successCallback(balancedJobs.map(job => ({
                            id: job.id,
                            title: `#${job.id} - ${job.type} (${job.tech}) [${job.geo}]`,
                            start: job.start,
                            end: job.end,
                            classNames: [job.status, job.type],
                            backgroundColor: getTypeColor(job.type),
                            borderColor: getTypeColor(job.type)
                        })));
                    },
                    eventClick: function (info) {
                        const job = jobs.find(j => j.id === info.event.id);
                        selectJob(job.id);
                        alert(`Optimized Job Details:\nID: ${job.id}\nType: ${job.type}\nGeo: ${job.geo}\nTech: ${job.tech}\nLoad: ${job.load}`);
                    },
                    slotMinTime: '08:00:00',
                    slotMaxTime: '18:00:00',
                    height: 'auto'
                });
                optimizedCalendar.render();
            }

            // Helper: Get color based on job type
            function getTypeColor(type) {
                const colors = {
                    maintenance: '#4CAF50', // Green
                    repair: '#F44336',      // Red
                    installation: '#2196F3' // Blue
                };
                return colors[type] || '#9E9E9E'; // Gray fallback
            }

            // Initial Setup
            jobs.forEach(addJobToList);
            selectJob('2001');
        });

</script>
</asp:Content>