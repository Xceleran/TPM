document.addEventListener('DOMContentLoaded', () => {


    let appointmentData = [];
    let invoiceData = [];
    let equipmentData = [];
    let notesData = [];
    let site = { serviceAgreements: [] };
    let siteFiles = [];
    let sitePictures = [];
    let siteFilesData = [];
    let invSortColumn = 'InvoiceDate';
    let invSortDirection = 'desc';
    let eqpSortColumn = 'InstallDate';
    let eqpSortDirection = 'desc';
    let notesSortColumn = 'Date/Time';
    let notesSortDirection = 'desc';
    let apptSortColumn = 'RequestDate';
    let apptSortDirection = 'desc';
    let currentPageAppt = 1, pageSizeAppt = 10;
    let currentPageInv = 1, pageSizeInv = 10;
    let currentPageEqp = 1, pageSizeEqp = 10;
    let currentPageNotes = 1, pageSizeNotes = 10;
    let invStartDate = null;
    let invEndDate = null;
    let apptStartDate = null;
    let apptEndDate = null;

    // Get customer data from server controls - try multiple selectors
    let customerId = '';
    let siteId = 0;
    let customerGuid = '';

    // Try to get from MainContent prefixed IDs first
    const customerIdEl = document.getElementById('MainContent_lblCustomerId') || document.querySelector('[id$="lblCustomerId"]');
    const siteIdEl = document.getElementById('MainContent_lblSiteId') || document.querySelector('[id$="lblSiteId"]');
    const customerGuidEl = document.getElementById('MainContent_lblCustomerGuid') || document.querySelector('[id$="lblCustomerGuid"]');

    if (customerIdEl) {
        customerId = customerIdEl.innerText || customerIdEl.textContent || '';
    }
    if (siteIdEl) {
        const siteIdText = siteIdEl.innerText || siteIdEl.textContent || '0';
        siteId = parseInt(siteIdText, 10) || 0;
    }
    if (customerGuidEl) {
        customerGuid = customerGuidEl.innerText || customerGuidEl.textContent || '';
    }

    // Debug logging
    console.log('Customer Details - Loaded IDs:', { customerId, siteId, customerGuid });

    if (!customerId) {
        console.error('Customer ID is missing! Cannot load data.');
        showToast('Error: Customer ID is missing. Cannot load data.');
    }

    initializeEventListeners();

    // Only load data if we have a customer ID
    if (customerId) {
        loadAllData();
    }

    initializeTabFromURL();
    updateDateFilterUI();

    // Helper function to format file sizes
    function formatFileSize(bytes) {
        if (!bytes || bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }

    function initializeEventListeners() {
        // Handle clicks on CSL links in the appointments table to switch tabs
        $(document).on('click', '#apptTableBody .csl-link', function (e) {
            e.preventDefault();
            const targetTabId = $(this).data('tab'); // e.g., "notes", "invoices"
            const tabButtonSelector = `#custdetTabs button[data-bs-target="#${targetTabId}"]`;
            const tabButton = document.querySelector(tabButtonSelector);

            if (tabButton && window.bootstrap && bootstrap.Tab) {
                const tabInstance = new bootstrap.Tab(tabButton);
                tabInstance.show();
            } else {
                console.warn('Could not find tab button or Bootstrap Tab API for:', targetTabId);
            }
        });

        // Handle clicks on CSL links in the appointments table to switch tabs and highlight content
        $(document).on('click', '#apptTableBody .csl-link', function (e) {
            e.preventDefault();
            const targetTabId = $(this).data('tab'); // e.g., "notes", "invoices"
            const appointmentId = $(this).data('appointment-id'); // Get the appointment ID

            const tabButtonSelector = `#custdetTabs button[data-bs-target="#${targetTabId}"]`;
            const tabButton = document.querySelector(tabButtonSelector);

            if (tabButton && window.bootstrap && bootstrap.Tab) {
                const tabInstance = new bootstrap.Tab(tabButton);
                tabInstance.show();

                // After the tab is shown, trigger highlighting
                // Use a short delay to ensure the tab content is rendered
                setTimeout(() => {
                    switch (targetTabId) {
                        case 'notes': highlightNotes(appointmentId); break;
                        case 'invoices': highlightInvoices(appointmentId); break;
                        case 'forms': highlightForms(appointmentId); break;
                        case 'pictures': highlightPictures(appointmentId); break;
                        case 'files': highlightFiles(appointmentId); break;
                        case 'agreements': highlightAgreements(appointmentId); break;
                    }
                }, 300); // 300ms delay to allow tab content to become visible
            } else {
                console.warn('Could not find tab button or Bootstrap Tab API for:', targetTabId);
            }
        });

        $('#apptTableBody').on('click', '.history-icon', function () {
            const appointmentId = $(this).data('appid');
            openStatusHistoryModal(appointmentId);
        });

    // Generic highlight function
    function applyHighlightAndScroll(selector, appointmentId, matchAttr = 'data-appointment-id', scrollContainer = null) {
        const highlightClass = 'table-highlight';
        const fadeOutClass = 'table-highlight-fade';
        const $elementsToHighlight = $(`${selector}[${matchAttr}="${appointmentId}"]`);
        
        // Remove existing highlights first from ALL TRs in the table
        $(selector).removeClass(`${highlightClass} ${fadeOutClass}`);

        if ($elementsToHighlight.length > 0) {
            $elementsToHighlight.addClass(highlightClass);
            
            // Scroll to the first highlighted element
            const $firstElement = $elementsToHighlight.first();
            if ($firstElement.length > 0) {
                const offset = $firstElement.offset().top - 150; // Adjust for header/padding
                $('html, body').animate({
                    scrollTop: offset
                }, 500);
            }

            // Remove highlight after a few seconds with a fade-out effect
            setTimeout(() => {
                $elementsToHighlight.removeClass(highlightClass).addClass(fadeOutClass);
                // After the fade-out, remove the fade-out class
                setTimeout(() => {
                    $elementsToHighlight.removeClass(fadeOutClass);
                }, 2000); // Duration of table-highlight-fade transition
            }, 3000); // Highlight visible for 3 seconds before fading
        }
    }

    // Specific highlighting functions for each tab
    function highlightNotes(appointmentId) {
        applyHighlightAndScroll('#notesTableBody tr', appointmentId, 'data-appointment-id');
    }

    function highlightInvoices(appointmentId) {
        applyHighlightAndScroll('#invTableBody tr', appointmentId, 'data-appointment-id');
    }

    function highlightForms(appointmentId) {
        // Forms table might be more complex, needing to find by appointment ID within form data
        // For now, let's assume rows have data-appointment-id
        applyHighlightAndScroll('#formsTableBody tr', appointmentId, 'data-appointment-id');
    }
    
    // highlightEquipment is removed as it is not present in buildCslLinksForAppointment
    // function highlightEquipment(appointmentId) {
    //     console.log(`Highlighting equipment for appointment ${appointmentId} - implementation needed.`);
    //     applyHighlightAndScroll('#equipTableBody tr', appointmentId, 'data-appointment-id');
    // }

    function highlightPictures(appointmentId) {
        applyHighlightAndScroll('#picturesTableBody tr', appointmentId, 'data-appointment-id');
    }

    function highlightFiles(appointmentId) {
        applyHighlightAndScroll('#filesTableBody tr', appointmentId, 'data-appointment-id');
    }

    function highlightAgreements(appointmentId) {
        applyHighlightAndScroll('#agreementTableBody tr', appointmentId, 'data-appointment-id');
    }

    // Add a CSS class for highlighting
    const style = document.createElement('style');
    style.innerHTML = `
        .table-highlight {
            background-color: #ffc107 !important; /* Vivid yellow (Bootstrap's warning color) */
            transition: background-color 0.5s ease-in-out;
        }
        .table-highlight-fade {
            transition: background-color 2s ease-out;
            background-color: transparent !important;
        }
    `;
    document.head.appendChild(style);
        $('#apptTableBody').on('click', '.view-notes-btn', function () {
            switchToNotesTab();
        });
        // Create Invoice/Estimate buttons removed per requirements
        // SMS/MMS and Communication History buttons
        $('#btnSms').on('click', function (e) {
            e.preventDefault();
            const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
            const siteId = $('#MainContent_lblSiteId').text() || $('[id$="lblSiteId"]').text() || '0';
            if (customerId) {
                window.open(`/Communications.aspx?type=sms&custId=${customerId}&siteId=${siteId}`, '_blank');
            }
        });
        $('#btnMms').on('click', function (e) {
            e.preventDefault();
            const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
            const siteId = $('#MainContent_lblSiteId').text() || $('[id$="lblSiteId"]').text() || '0';
            if (customerId) {
                window.open(`/Communications.aspx?type=mms&custId=${customerId}&siteId=${siteId}`, '_blank');
            }
        });
        $('#btnEmailHistory').on('click', function (e) {
            e.preventDefault();
            const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
            const siteId = $('#MainContent_lblSiteId').text() || $('[id$="lblSiteId"]').text() || '0';
            if (customerId) {
                window.open(`/Communications.aspx?type=email&custId=${customerId}&siteId=${siteId}`, '_blank');
            }
        });
        $('#btnTextHistory').on('click', function (e) {
            e.preventDefault();
            const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
            const siteId = $('#MainContent_lblSiteId').text() || $('[id$="lblSiteId"]').text() || '0';
            if (customerId) {
                window.open(`/Communications.aspx?type=text&custId=${customerId}&siteId=${siteId}`, '_blank');
            }
        });

        // Picture upload button
        $('#uploadPictureBtn').on('click', function (e) {
            e.preventDefault();
            $('#pictureUploadInput').click();
        });

        // Picture file input change handler
        $('#pictureUploadInput').on('change', function (e) {
            const files = e.target.files;
            if (files && files.length > 0) {
                handlePictureUpload(files);
            }
        });

        // File upload button (if exists)
        $('#uploadFileBtn').on('click', function (e) {
            e.preventDefault();
            $('#fileUploadInput').click();
        });

        // File input change handler
        $('#fileUploadInput').on('change', function (e) {
            const files = e.target.files;
            if (files && files.length > 0) {
                handleFileUpload(files);
            }
        });
        // Add this to your initializeEventListeners function
        $('#addBtn').on('click', function (e) {
            e.preventDefault();
            saveNote();
        });

        function saveNote() {
            const description = $('#noteField').val().trim();
            const taggedTo = $('#hiddenTagSelect').val() ? $('#hiddenTagSelect').val().join(', ') : '';
            const taggedFrom = 'FSM'; // Default value

            if (!description) {
                showToast('Please enter a note description.');
                return;
            }

            if (!customerId) {
                showToast('Error: Customer ID is missing.');
                return;
            }

            $.ajax({
                type: "POST",
                url: "CustomerDetails.aspx/SaveCustomerNote",
                data: JSON.stringify({
                    customerId: customerId,
                    siteId: siteId,
                    description: description,
                    taggedTo: taggedTo,
                    taggedFrom: taggedFrom
                }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response && response.d === true) {
                        showToast('Note saved successfully!');
                        $('#noteModal').modal('hide');
                        $('#noteField').val('');
                        $('#hiddenTagSelect').val('');

                        // Reload notes
                        if (cslDataLoaded) {
                            loadAllData(); // Reload all data
                        } else {
                            loadNotes(); // Just reload notes
                        }
                    } else {
                        showToast('Error saving note. Please try again.');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error saving note:', error, xhr);
                    showToast('Error saving note. Please check console for details.');
                }
            });
        }
        // Agreement upload button (if exists)
        $('#uploadAgreementBtn').on('click', function (e) {
            e.preventDefault();
            $('#agreementUploadInput').click();
        });

        // Agreement input change handler
        $('#agreementUploadInput').on('change', function (e) {
            const files = e.target.files;
            if (files && files.length > 0) {
                handleAgreementUpload(files);
            }
        });

        // Modal "Save Agreement" button handler
        $('#agreeSave').on('click', function (e) {
            e.preventDefault();
            const agreeFileInput = $('#agreeFile')[0];
            if (!agreeFileInput || !agreeFileInput.files || agreeFileInput.files.length === 0) {
                showToast('Please select a file to upload.');
                return;
            }

            const files = agreeFileInput.files;
            handleAgreementUpload(files);

            // Close modal after starting upload
            $('#agreeModal').modal('hide');
            // Clear the file input
            $('#agreeFile').val('');
        });
    }

    function showToast(message) {
        // Use SweetAlert2 if available, otherwise use alert
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 3000,
                icon: 'info',
                title: message
            });
        } else {
            alert(message);
        }
    }

    function parseMDY(dateString) {
        if (!dateString) return null;
        // Try to parse MM/DD/YYYY format
        const parts = dateString.split('/');
        if (parts.length === 3) {
            const month = parseInt(parts[0], 10) - 1; // Month is 0-indexed
            const day = parseInt(parts[1], 10);
            const year = parseInt(parts[2], 10);
            return moment([year, month, day]);
        }
        // Fallback to moment's parsing
        return moment(dateString);
    }

    function updateDateFilterUI() {
        // Initialize date range picker for appointments if it exists
        if ($('#apptDateRangePicker').length) {
            $('#apptDateRangePicker').daterangepicker({
                opens: 'left',
                locale: {
                    format: 'MM/DD/YYYY'
                }
            }, function (start, end, label) {
                apptStartDate = start.format('MM/DD/YYYY');
                apptEndDate = end.format('MM/DD/YYYY');
                applyFiltersAppt();
            });
        }
    }

    // Tab switching handlers - render using already loaded cslData
    // Note: Using button elements, not anchor tags
    $('#custdetTabs button[data-bs-toggle="tab"], #custdetTabs a[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        const targetTab = $(e.target).attr('data-bs-target') || $(e.target).closest('[data-bs-target]').attr('data-bs-target');
        console.log('Tab switched to:', targetTab);

        // If cslData is loaded, use it to render. Otherwise, load individually
        if (cslDataLoaded && cslData) {
            // Use already loaded data
            if (targetTab === '#appointments') {
                applyFiltersAppt();
            } else if (targetTab === '#invoices') {
                applyFiltersInv();
            } else if (targetTab === '#equipment') {
                renderEquipments();
            } else if (targetTab === '#notes') {
                renderNotes();
            } else if (targetTab === '#forms') {
                loadForms(); // Forms depend on appointments
            } else if (targetTab === '#pictures') {
                renderPictures();
            } else if (targetTab === '#files') {
                renderFiles();
            } else if (targetTab === '#agreements') {
                renderAgreements();
            }
        } else {
            // Fallback to individual loads if cslData not available
            if (targetTab === '#appointments') {
                if (appointmentData.length === 0) {
                    loadAppointments();
                } else {
                    applyFiltersAppt();
                }
            } else if (targetTab === '#invoices') {
                if (invoiceData.length === 0) {
                    loadInvoices();
                } else {
                    applyFiltersInv();
                }
            } else if (targetTab === '#equipment') {
                if (equipmentData.length === 0) {
                    loadEquipment();
                } else {
                    renderEquipments();
                }
            } else if (targetTab === '#notes') {
                if (notesData.length === 0) {
                    loadNotes();
                } else {
                    renderNotes();
                }
            } else if (targetTab === '#forms') {
                loadForms();
            } else if (targetTab === '#pictures') {
                if (sitePictures.length === 0) {
                    loadPictures();
                } else {
                    renderPictures();
                }
            } else if (targetTab === '#files') {
                if (siteFilesData.length === 0) {
                    loadFiles();
                } else {
                    renderFiles();
                }
            } else if (targetTab === '#agreements') {
                if (siteAgreementsData.length === 0) {
                    loadAgreements();
                } else {
                    renderAgreements();
                }
            }
        }
    });

    function switchToNotesTab() {
        const notesTabTrigger = document.querySelector('#notes-tab');
        if (notesTabTrigger) {
            const tab = new bootstrap.Tab(notesTabTrigger);
            tab.show();
        } else {
            showToast("Error: Customer or Site ID is missing. Cannot load data.");
            console.error(`Customer ID or Site ID is invalid. CustomerID: ${customerId}, SiteID: ${siteId}`);
        }
    }

    // Store all CSL data in one place (like Appointments.aspx)
    let cslData = null;
    let cslDataLoaded = false;

    function loadAllData() {
        if (customerId && siteId >= 0) {
            console.log('Loading all CSL data for customerId:', customerId, 'siteId:', siteId);

            // Use single WebMethod call like Appointments.aspx
            $.ajax({
                type: "POST",
                url: "CustomerDetails.aspx/GetCslDrawerData",
                data: JSON.stringify({ customerId: customerId.trim(), siteId: siteId }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    console.log('CSL Drawer Data response:', response);
                    const data = response.d;
                    if (data) {
                        cslData = data;
                        cslDataLoaded = true;

                        // Populate all data arrays from the single response
                        appointmentData = data.Appointments || [];
                        invoiceData = data.Invoices || [];
                        equipmentData = data.Equipment || [];
                        notesData = data.Notes || [];
                        sitePictures = data.Pictures || [];
                        siteFilesData = data.Files || [];
                        siteAgreementsData = data.MaintenanceAgreements || [];

                        console.log('CSL Data loaded:', {
                            appointments: appointmentData.length,
                            invoices: invoiceData.length,
                            equipment: equipmentData.length,
                            notes: notesData.length,
                            pictures: sitePictures.length,
                            files: siteFilesData.length,
                            agreements: siteAgreementsData.length
                        });

                        // Data is loaded and stored in global arrays
                        // Basic tab is server-side rendered, so no need to render it
                        // Other tabs will render when clicked via the tab switching handler
                        console.log('CSL data loaded successfully. Tabs will render when clicked.');

                        // Get the currently active tab
                        const activeTab = $('#custdetTabs .nav-link.active').attr('data-bs-target');
                        console.log('Active tab after data load:', activeTab);

                        // Re-render the active tab if it's not basic (to show updated data)
                        if (activeTab && activeTab !== '#basic') {
                            // Render the active tab to show updated data
                            console.log('Re-rendering active tab:', activeTab);
                            if (activeTab === '#appointments') {
                                applyFiltersAppt();
                            } else if (activeTab === '#invoices') {
                                applyFiltersInv();
                            } else if (activeTab === '#equipment') {
                                renderEquipments();
                            } else if (activeTab === '#notes') {
                                renderNotes();
                            } else if (activeTab === '#forms') {
                                loadForms();
                            } else if (activeTab === '#pictures') {
                                renderPictures();
                            } else if (activeTab === '#files') {
                                renderFiles();
                            } else if (activeTab === '#agreements') {
                                console.log('Re-rendering agreements tab with', siteAgreementsData.length, 'agreements');
                                renderAgreements();
                            }
                        } else {
                            console.log('Active tab is basic or not found, skipping re-render');
                        }
                    } else {
                        console.error('No data returned from GetCslDrawerData');
                        showToast("Error: Could not load customer data.");
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error loading CSL data:', error, xhr);
                    console.error('XHR Response:', xhr.responseText);
                    showToast("Error loading customer data. Please check console for details.");

                    // Fallback to individual calls if single call fails
                    console.log('Falling back to individual data loads...');
                    loadAppointments();
                    loadInvoices();
                    loadEquipment();
                    loadNotes();
                    loadForms();
                    loadPictures();
                    loadFiles();
                    loadAgreements();
                }
            });
        } else {
            showToast("Error: Customer or Site ID is missing. Cannot load data.");
            console.error("Customer ID or Site ID is missing from the page. customerId:", customerId, "siteId:", siteId);
        }
    }

    function renderAllTabs() {
        // Render the currently active tab only (like Appointments.aspx)
        // Note: Basic tab doesn't need rendering - it's server-side populated
        const activeTab = $('#custdetTabs .nav-link.active').attr('data-bs-target');
        console.log('Rendering active tab:', activeTab);

        // Basic tab is just static HTML, no need to render
        if (activeTab === '#basic') {
            // Basic tab is already populated server-side, just ensure other tabs are ready
            // Don't render anything for basic tab
            console.log('Basic tab is active - no rendering needed');
            return;
        } else if (activeTab === '#appointments') {
            applyFiltersAppt();
        } else if (activeTab === '#invoices') {
            applyFiltersInv();
        } else if (activeTab === '#equipment') {
            renderEquipments();
        } else if (activeTab === '#notes') {
            renderNotes();
        } else if (activeTab === '#forms') {
            loadForms(); // Forms depend on appointments
        } else if (activeTab === '#pictures') {
            renderPictures();
        } else if (activeTab === '#files') {
            renderFiles();
        } else if (activeTab === '#agreements') {
            renderAgreements();
        } else {
            // Default to appointments tab if no active tab or unknown tab
            console.log('No active tab found or unknown tab, defaulting to appointments');
            applyFiltersAppt();
        }
    }

    function loadNotes() {
        if (!customerId) {
            console.error('Cannot load notes: customerId is missing');
            $('#notesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error: Customer ID is missing. Cannot load notes.</td></tr>');
            return;
        }

        console.log('Loading notes for customerId:', customerId, 'siteId:', siteId);

        $.ajax({
            url: 'CustomerDetails.aspx/GetCustomerNotes',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ customerId: customerId, siteId: siteId }),
            dataType: 'json',
            success: (rs) => {
                console.log('Notes response:', rs);

                // Handle both direct array and wrapped response
                if (rs && rs.d !== undefined) {
                    notesData = rs.d || [];
                } else if (Array.isArray(rs)) {
                    notesData = rs;
                } else {
                    notesData = [];
                }

                console.log('Notes data:', notesData);
                console.log('Notes count:', notesData.length);

                if (notesData.length === 0) {
                    $('#notesTableBody').html('<tr><td colspan="6" class="text-center text-muted">No notes found for this customer/site.</td></tr>');
                } else {
                    console.log('Rendering notes, count:', notesData.length);
                    renderNotes();
                }
            },
            error: (xhr, status, error) => {
                console.error('Error loading notes:', error);
                console.error('XHR Status:', xhr.status);
                console.error('XHR Response:', xhr.responseText);
                // If method doesn't exist yet, initialize with empty array
                notesData = [];
                $('#notesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error loading notes. Please check console for details.</td></tr>');
            }
        });
    }


    function initializeTabFromURL() {
        const params = new URLSearchParams(location.search);
        const tab = params.get('tab');
        if (tab) {
            const btn = document.querySelector(`#custdetTabs .nav-link[data-bs-target="#${tab}"]`);
            if (btn && window.bootstrap && bootstrap.Tab) {
                new bootstrap.Tab(btn).show();
            }
        }
    }


    function loadAppointments() {
        if (!customerId) {
            console.error('Cannot load appointments: customerId is missing');
            console.error('customerId element:', document.getElementById('MainContent_lblCustomerId') || document.querySelector('[id$="lblCustomerId"]'));
            $('#apptTableBody').html('<tr><td colspan="7" class="text-center text-danger">Error: Customer ID is missing. Cannot load appointments.</td></tr>');
            return;
        }

        console.log('Loading appointments for customerId:', customerId, 'siteId:', siteId);
        console.log('customerId type:', typeof customerId, 'value:', JSON.stringify(customerId));

        // Show loading state
        $('#apptTableBody').html('<tr><td colspan="7" class="text-center"><i class="fas fa-spinner fa-spin me-2"></i>Loading appointments...</td></tr>');

        const requestData = { customerId: customerId.trim(), siteId: siteId };
        console.log('Sending AJAX request with data:', requestData);

        $.ajax({
            url: 'CustomerDetails.aspx/GetCustomerAppoinmets',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(requestData),
            dataType: 'json',
            success: (rs) => {
                console.log('Appointments response received:', rs);
                console.log('Response type:', typeof rs);
                console.log('Response.d:', rs?.d);
                console.log('Is array:', Array.isArray(rs));

                // Handle both direct array and wrapped response
                if (rs && rs.d !== undefined) {
                    appointmentData = rs.d || [];
                    console.log('Using rs.d, count:', appointmentData.length);
                } else if (Array.isArray(rs)) {
                    appointmentData = rs;
                    console.log('Using direct array, count:', appointmentData.length);
                } else {
                    appointmentData = [];
                    console.warn('Unexpected response format:', rs);
                }

                console.log('Appointments data:', appointmentData);
                console.log('Appointments count:', appointmentData.length);

                if (appointmentData.length === 0) {
                    console.warn('No appointments found for customerId:', customerId);
                    $('#apptTableBody').html('<tr><td colspan="7" class="text-center text-muted">No appointments found for this customer.</td></tr>');
                } else {
                    console.log('Rendering appointments...');
                    applyFiltersAppt();
                }
            },
            error: (xhr, status, error) => {
                console.error('Error loading appointments:', error);
                console.error('XHR Status:', xhr.status);
                console.error('XHR Response:', xhr.responseText);
                console.error('XHR Status Text:', xhr.statusText);
                console.error('Full XHR:', xhr);

                let errorMsg = "Failed to load appointments.";
                if (xhr.responseText) {
                    try {
                        const errorObj = JSON.parse(xhr.responseText);
                        errorMsg += " " + (errorObj.Message || errorObj.message || xhr.responseText);
                        console.error('Parsed error object:', errorObj);
                    } catch (e) {
                        errorMsg += " " + error;
                        console.error('Could not parse error response:', e);
                    }
                }

                showToast(errorMsg);
                $('#apptTableBody').html('<tr><td colspan="7" class="text-center text-danger">Error loading appointments. Please check the console for details.</td></tr>');
            }
        });
    }

    function loadInvoices() {
        if (!customerId) {
            console.error('Cannot load invoices: customerId is missing');
            $('#invTableBody').html('<tr><td colspan="11" class="text-center text-danger">Error: Customer ID is missing. Cannot load invoices.</td></tr>');
            return;
        }

        console.log('Loading invoices for customerId:', customerId);

        $.ajax({
            url: 'CustomerDetails.aspx/GetCustomerInvoices',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ customerId: customerId }),
            dataType: 'json',
            success: (rs) => {
                console.log('Invoices response:', rs);

                // Handle both direct array and wrapped response
                if (rs && rs.d !== undefined) {
                    invoiceData = rs.d || [];
                } else if (Array.isArray(rs)) {
                    invoiceData = rs;
                } else {
                    invoiceData = [];
                }

                console.log('Invoices data:', invoiceData);
                console.log('Invoices count:', invoiceData.length);

                if (invoiceData.length === 0) {
                    $('#invTableBody').html('<tr><td colspan="11" class="text-center text-muted">No invoices found for this customer.</td></tr>');
                } else {
                    console.log('Rendering invoices...');
                    applyFiltersInv();
                }
            },
            error: (xhr, status, error) => {
                console.error('Error loading invoices:', error);
                console.error('XHR Status:', xhr.status);
                console.error('XHR Response:', xhr.responseText);
                invoiceData = [];
                $('#invTableBody').html('<tr><td colspan="11" class="text-center text-danger">Error loading invoices. Please check console for details.</td></tr>');
            }
        });
    }

    function loadEquipment() {
        if (!customerGuid) {
            console.error('Cannot load equipment: customerGuid is missing');
            $('#equipTableBody').html('<tr><td colspan="12" class="text-center text-danger">Error: Customer GUID is missing. Cannot load equipment.</td></tr>');
            return;
        }

        console.log('Loading equipment for siteId:', siteId, 'customerGuid:', customerGuid);

        $.ajax({
            url: 'CustomerDetails.aspx/GetSiteEquipmentData',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ siteId: siteId, customerGuid: customerGuid }),
            dataType: 'json',
            success: (rs) => {
                console.log('Equipment response:', rs);

                // Handle both direct array and wrapped response
                if (rs && rs.d !== undefined) {
                    equipmentData = rs.d || [];
                } else if (Array.isArray(rs)) {
                    equipmentData = rs;
                } else {
                    equipmentData = [];
                }

                console.log('Equipment data:', equipmentData);
                console.log('Equipment count:', equipmentData.length);

                if (equipmentData.length === 0) {
                    $('#equipTableBody').html('<tr><td colspan="12" class="text-center text-muted">No equipment found for this customer/site.</td></tr>');
                } else {
                    console.log('Rendering equipment...');
                    renderEquipments();
                }
            },
            error: (xhr, status, error) => {
                console.error('Error loading equipment:', error);
                console.error('XHR Status:', xhr.status);
                console.error('XHR Response:', xhr.responseText);
                equipmentData = [];
                $('#equipTableBody').html('<tr><td colspan="12" class="text-center text-danger">Error loading equipment. Please check console for details.</td></tr>');
            }
        });
    }

    // Equipment delete handler
    window.equipmentDelete = function (equipmentId) {
        if (confirm('Are you sure you want to delete this equipment?')) {
            $.ajax({
                url: 'CustomerDetails.aspx/DeleteEquipment',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ equipmentId: equipmentId }),
                dataType: 'json',
                success: (rs) => {
                    if (rs && rs.d === true) {
                        showToast('Equipment deleted successfully!');
                        loadEquipment();
                    } else {
                        showToast('Error deleting equipment.');
                    }
                },
                error: () => showToast('A server error occurred while deleting.')
            });
        }
    };

    function equipmentSave(event) {
        event.preventDefault();
        const formData = {
            Id: $('#equipmentId').val() ? parseInt($('#equipmentId').val()) : 0,
            CustomerGuid: customerGuid,
            CustomerID: customerId,
            Make: $('#equipmentMake').val(),
            Model: $('#equipmentModel').val(),
            SerialNumber: $('#equipmentSerialNumber').val(),
            Barcode: $('#equipmentBarcode').val(),
            EquipmentType: $('#equipmentType').val(),
            Notes: $('#equipmentNotes').val(),
            InstallDate: $('#equipmentInstallDate').val(),
            WarrantyStart: $('#equipmentWarrantyStart').val(),
            WarrantyEnd: $('#equipmentWarrantyEnd').val(),
            LaborWarrantyStart: $('#equipmentLaborWarrantyStart').val(),
            LaborWarrantyEnd: $('#equipmentLaborWarrantyEnd').val(),
            SiteId: siteId
        };

        $.ajax({
            url: 'CustomerDetails.aspx/SaveEquipmentData',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ equipment: formData }),
            dataType: 'json',
            success: (rs) => {
                if (rs && rs.d === true) {
                    showToast("Equipment saved successfully!");
                    $('#equipmentModal').modal('hide');
                    loadEquipment();
                } else {
                    showToast("Something went wrong!");
                }
            },
            error: () => showToast("Error saving equipment details.")
        });
    }

// --- Note Tagging Functionality ---
function initializeNoteTagging() {
    const tagInputContainer = $('#tagInputContainer');
    const tagDropdownMenu = $('#tagDropdownMenu');
    const hiddenTagSelect = $('#hiddenTagSelect');
    const tagSearchInput = $('#tagSearch');
    let selectedTags = new Set(); // To keep track of selected tags

    // Load existing tags from the hidden select (if any were pre-selected or on edit)
    hiddenTagSelect.find('option:selected').each(function() {
        selectedTags.add($(this).val());
    });
    updateTagInputVisual();

    // Toggle dropdown visibility
    tagInputContainer.on('click', function (e) {
        e.stopPropagation(); // Prevent modal from closing immediately
        tagDropdownMenu.toggleClass('show');
        tagSearchInput.focus();
    });

    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!tagInputContainer.is(e.target) && tagInputContainer.has(e.target).length === 0 && !tagDropdownMenu.is(e.target) && tagDropdownMenu.has(e.target).length === 0) {
            tagDropdownMenu.removeClass('show');
        }
    });

    // Handle tag item click
    tagDropdownMenu.on('click', 'a.dropdown-item', function (e) {
        e.preventDefault();
        e.stopPropagation(); // Keep dropdown open

        const tagValue = $(this).data('value');
        if (selectedTags.has(tagValue)) {
            selectedTags.delete(tagValue);
            $(this).removeClass('active');
        } else {
            selectedTags.add(tagValue);
            $(this).addClass('active');
        }
        updateHiddenTagSelect();
        updateTagInputVisual();
    });

    // Filter dropdown items based on search input
    tagSearchInput.on('keyup', function () {
        const searchText = $(this).val().toLowerCase();
        tagDropdownMenu.find('a.dropdown-item').each(function () {
            const tagText = $(this).text().toLowerCase();
            if (tagText.includes(searchText)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Function to update the hidden <select> element
    function updateHiddenTagSelect() {
        hiddenTagSelect.empty();
        selectedTags.forEach(tag => {
            hiddenTagSelect.append($('<option>', { value: tag, text: tag, selected: true }));
        });
    }

    // Function to update the visual representation in the input container
    function updateTagInputVisual() {
        const visualTagsHtml = Array.from(selectedTags).map(tag => `
            <span class="badge bg-primary me-1 tag-pill">
                ${tag} <span class="tag-remove" data-tag="${tag}">&times;</span>
            </span>
        `).join('');

        // Ensure tagSearchInput remains usable for typing
        tagInputContainer.find('.tag-pill').remove(); // Clear existing pills
        tagInputContainer.prepend(visualTagsHtml); // Add new pills before the input

        // Adjust input width if needed (optional)
        //tagSearchInput.width(tagInputContainer.width() - tagInputContainer.find('.tag-pill').toArray().reduce((sum, el) => sum + $(el).outerWidth(true), 0) - 20);

        // Re-attach listener for removing tags
        tagInputContainer.find('.tag-remove').on('click', function(e) {
            e.stopPropagation();
            const tagToRemove = $(this).data('tag');
            selectedTags.delete(tagToRemove);
            tagDropdownMenu.find(`a.dropdown-item[data-value="${tagToRemove}"]`).removeClass('active');
            updateHiddenTagSelect();
            updateTagInputVisual();
        });

        // If no tags selected, show placeholder
        if (selectedTags.size === 0) {
            tagSearchInput.attr('placeholder', 'Select tags...');
        } else {
            tagSearchInput.attr('placeholder', ''); // Clear placeholder if tags are selected
        }
        tagDropdownMenu.find('a.dropdown-item').each(function() {
            const tagValue = $(this).data('value');
            if (selectedTags.has(tagValue)) {
                $(this).addClass('active');
            } else {
                $(this).removeClass('active');
            }
        });
    }

    // Clear button in modal
    $('#clearBtn').on('click', function () {
        $('#noteField').val('');
        selectedTags.clear();
        updateHiddenTagSelect();
        updateTagInputVisual();
        tagDropdownMenu.find('a.dropdown-item').removeClass('active');
    });

    // Initialize on modal show
    $('#noteModal').on('show.bs.modal', function() {
        selectedTags.clear();
        tagDropdownMenu.find('a.dropdown-item').removeClass('active');
        updateHiddenTagSelect();
        updateTagInputVisual();
        tagSearchInput.val('');
        tagSearchInput.attr('placeholder', 'Select tags...');
    });
}

    function showToast(message) {
        // Use SweetAlert2 if available, otherwise use alert
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 3000,
                icon: 'info',
                title: message
            });
        } else {
            alert(message);
        }
    }

    // Rendering functions
    function renderAppointments() {
        applyFiltersAppt();
    }

    function renderInvoices() {
        applyFiltersInv();
    }

    function renderEquipments() {
        console.log('Rendering equipment, data count:', equipmentData.length);
        applyFiltersEqp();
    }

    function renderNotes() {
        console.log('Rendering notes, data count:', notesData.length);
        applyFiltersNotes();
    }

    // Filter and sort functions
    function applyFiltersAppt() {
        let filtered = getFilteredAppointments();
        renderAppointmentsTable(filtered);
    }

    function applyFiltersInv() {
        let filtered = getFilteredInvoices();
        renderInvoicesTable(filtered);
    }

    function applyFiltersEqp() {
        let filtered = getFilteredEquipment();
        renderEquipmentTable(filtered);
    }

    function applyFiltersNotes() {
        let filtered = getFilteredNotes();
        renderNotesTable(filtered);
    }

    function getFilteredAppointments() {
        let filtered = [...appointmentData];

        // Apply date filter if set
        if (apptStartDate && apptEndDate) {
            filtered = filtered.filter(apt => {
                const aptDate = parseMDY(apt.RequestDate || apt.AppoinmentDate);
                if (!aptDate) return false;
                const start = parseMDY(apptStartDate);
                const end = parseMDY(apptEndDate);
                return aptDate.isSameOrAfter(start, 'day') && aptDate.isSameOrBefore(end, 'day');
            });
        }

        // Apply sorting
        filtered.sort((a, b) => {
            let aVal, bVal;
            switch (apptSortColumn) {
                case 'RequestDate':
                    aVal = parseMDY(a.RequestDate || a.AppoinmentDate) || moment(0);
                    bVal = parseMDY(b.RequestDate || b.AppoinmentDate) || moment(0);
                    break;
                case 'ServiceType':
                    aVal = (a.ServiceType || '').toLowerCase();
                    bVal = (b.ServiceType || '').toLowerCase();
                    break;
                case 'Status':
                    aVal = (a.AppoinmentStatus || '').toLowerCase();
                    bVal = (b.AppoinmentStatus || '').toLowerCase();
                    break;
                default:
                    aVal = a[apptSortColumn] || '';
                    bVal = b[apptSortColumn] || '';
            }

            if (apptSortDirection === 'asc') {
                return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
            } else {
                return aVal < bVal ? 1 : aVal > bVal ? -1 : 0;
            }
        });

        return filtered;
    }

    function getFilteredInvoices() {
        let filtered = [...invoiceData];

        // Apply date filter if set
        if (invStartDate && invEndDate) {
            filtered = filtered.filter(inv => {
                const invDate = parseMDY(inv.InvoiceDate);
                if (!invDate) return false;
                const start = parseMDY(invStartDate);
                const end = parseMDY(invEndDate);
                return invDate.isSameOrAfter(start, 'day') && invDate.isSameOrBefore(end, 'day');
            });
        }

        // Apply sorting
        filtered.sort((a, b) => {
            let aVal, bVal;
            switch (invSortColumn) {
                case 'InvoiceDate':
                    aVal = parseMDY(a.InvoiceDate) || moment(0);
                    bVal = parseMDY(b.InvoiceDate) || moment(0);
                    break;
                case 'InvoiceNumber':
                    aVal = (a.InvoiceNumber || '').toLowerCase();
                    bVal = (b.InvoiceNumber || '').toLowerCase();
                    break;
                case 'Total':
                    aVal = parseFloat(a.Total || 0);
                    bVal = parseFloat(b.Total || 0);
                    break;
                default:
                    aVal = a[invSortColumn] || '';
                    bVal = b[invSortColumn] || '';
            }

            if (invSortDirection === 'asc') {
                return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
            } else {
                return aVal < bVal ? 1 : aVal > bVal ? -1 : 0;
            }
        });

        return filtered;
    }

    function getFilteredEquipment() {
        let filtered = [...equipmentData];

        // Apply sorting
        filtered.sort((a, b) => {
            let aVal, bVal;
            switch (eqpSortColumn) {
                case 'InstallDate':
                    aVal = parseMDY(a.InstallDate) || moment(0);
                    bVal = parseMDY(b.InstallDate) || moment(0);
                    break;
                case 'EquipmentType':
                    aVal = (a.EquipmentType || '').toLowerCase();
                    bVal = (b.EquipmentType || '').toLowerCase();
                    break;
                default:
                    aVal = a[eqpSortColumn] || '';
                    bVal = b[eqpSortColumn] || '';
            }

            if (eqpSortDirection === 'asc') {
                return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
            } else {
                return aVal < bVal ? 1 : aVal > bVal ? -1 : 0;
            }
        });

        return filtered;
    }

    function getFilteredNotes() {
        let filtered = [...notesData];

        // Apply sorting
        filtered.sort((a, b) => {
            let aVal, bVal;
            switch (notesSortColumn) {
                case 'Date/Time':
                    aVal = moment(a.CreatedAt) || moment(0);
                    bVal = moment(b.CreatedAt) || moment(0);
                    break;
                case 'Tagged From':
                    aVal = (a.TaggedFrom || '').toLowerCase();
                    bVal = (b.TaggedFrom || '').toLowerCase();
                    break;
                default:
                    aVal = a[notesSortColumn] || '';
                    bVal = b[notesSortColumn] || '';
            }

            if (notesSortDirection === 'asc') {
                return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
            } else {
                return aVal < bVal ? 1 : aVal > bVal ? -1 : 0;
            }
        });

        return filtered;
    }

    // Table rendering functions
    function renderAppointmentsTable(appointments) {
        if (!appointments || appointments.length === 0) {
            $('#apptTableBody').html('<tr><td colspan="7" class="text-center text-muted">No appointments found.</td></tr>');
            return;
        }

        let html = '';
        appointments.forEach(apt => {
            const cslLinks = buildCslLinksForAppointment(apt.AppoinmentId);
            html += `
                <tr>
                    <td>${apt.RequestDate || apt.AppoinmentDate || '-'}</td>
                    <td>${apt.TimeSlot || '-'}</td>
                    <td>${apt.ServiceType || '-'}</td>
                    <td>
                        <span class="badge bg-info">${apt.AppoinmentStatus || '-'}</span>
                        <i class="fas fa-history history-icon" data-appid="${apt.AppoinmentId}" style="cursor: pointer;" title="View Status History"></i>
                    </td>
                    <td>${apt.ResourceName || '-'}</td>
                    <td><span class="badge bg-secondary">${apt.TicketStatus || '-'}</span></td>
                    <td>${cslLinks}</td>
                </tr>
            `;
        });
        $('#apptTableBody').html(html);
    }

    function buildCslLinksForAppointment(appointmentId) {
        let links = [];

        // Check for notes
        const notesForAppt = notesData.filter(n => n.AppointmentId === appointmentId.toString());
        if (notesForAppt.length > 0) {
            links.push(`<a href="#notes" class="csl-link" data-tab="notes" data-appointment-id="${appointmentId}">Notes (${notesForAppt.length})</a>`);
        }

        // Check for invoices
        const invoicesForAppt = invoiceData.filter(inv => inv.AppointmentId === appointmentId.toString());
        if (invoicesForAppt.length > 0) {
            links.push(`<a href="#invoices" class="csl-link" data-tab="invoices" data-appointment-id="${appointmentId}">Invoices (${invoicesForAppt.length})</a>`);
        }

        // Check for pictures
        const picturesForAppt = sitePictures.filter(p => p.AppointmentId && p.AppointmentId.toString() === appointmentId.toString());
        if (picturesForAppt.length > 0) {
            links.push(`<a href="#pictures" class="csl-link" data-tab="pictures" data-appointment-id="${appointmentId}">Pictures (${picturesForAppt.length})</a>`);
        }

        // Check for files
        const filesForAppt = siteFilesData.filter(f => f.AppointmentId && f.AppointmentId.toString() === appointmentId.toString());
        if (filesForAppt.length > 0) {
            links.push(`<a href="#files" class="csl-link" data-tab="files" data-appointment-id="${appointmentId}">Files (${filesForAppt.length})</a>`);
        }

        // Check for forms (forms are associated with appointments)
        links.push(`<a href="#forms" class="csl-link" data-tab="forms" data-appointment-id="${appointmentId}">Forms</a>`);

        return links.length > 0 ? links.join(' | ') : '-';
    }

    function renderInvoicesTable(invoices) {
        if (!invoices || invoices.length === 0) {
            $('#invTableBody').html('<tr><td colspan="11" class="text-center text-muted">No invoices found.</td></tr>');
            return;
        }

        let html = '';
        invoices.forEach(inv => {
            html += `
                <tr ${inv.AppointmentId ? `data-appointment-id="${inv.AppointmentId}"` : ''}>
                    <td>${inv.InvoiceNumber || '-'}</td>
                    <td>${inv.InvoiceDate || '-'}</td>
                    <td>${inv.InvoiceType || '-'}</td>
                    <td>$${parseFloat(inv.Total || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.Due || 0).toFixed(2)}</td>
                    <td>${inv.TaggedFrom || 'FSM'}</td>
                    <td>${inv.AppointmentId ? '#' + inv.AppointmentId : '-'}</td>
                    <td><a href="${inv.ExternalLink || '#'}" target="_blank">View</a></td>
                </tr>
            `;
        });
        $('#invTableBody').html(html);
    }

    function renderEquipmentTable(equipment) {
        if (!equipment || equipment.length === 0) {
            $('#equipTableBody').html('<tr><td colspan="12" class="text-center text-muted">No equipment found.</td></tr>');
            return;
        }

        let html = '';
        equipment.forEach(eq => {
            html += `
                <tr>
                    <td>${eq.EquipmentType || '-'}</td>
                    <td>${eq.Make || '-'}</td>
                    <td>${eq.Model || '-'}</td>
                    <td>${eq.SerialNumber || '-'}</td>
                    <td>${eq.Barcode || '-'}</td>
                    <td>${eq.InstallDate || '-'}</td>
                    <td>${eq.WarrantyStart || '-'}</td>
                    <td>${eq.WarrantyEnd || '-'}</td>
                    <td>${eq.LaborWarrantyStart || '-'}</td>
                    <td>${eq.LaborWarrantyEnd || '-'}</td>
                    <td>${eq.Notes || '-'}</td>
                    <td>
                        <button class="btn btn-sm btn-primary" onclick="editEquipment(${eq.Id})">Edit</button>
                        <button class="btn btn-sm btn-danger" onclick="equipmentDelete(${eq.Id})">Delete</button>
                    </td>
                </tr>
            `;
        });
        $('#equipTableBody').html(html);
    }

    function renderNotesTable(notes) {
        if (!notes || notes.length === 0) {
            $('#notesTableBody').html('<tr><td colspan="6" class="text-center text-muted">No notes found.</td></tr>');
            return;
        }

        let html = '';
        notes.forEach(note => {
            const noteText = note.Description || '';
            const truncatedNote = noteText.length > 100 ? noteText.substring(0, 100) + '...' : noteText;
            const showReadMore = noteText.length > 100;

            html += `
                <tr ${note.AppointmentId ? `data-appointment-id="${note.AppointmentId}"` : ''}>
                    <td>${note.CreatedAt || '-'}</td>
                    <td>${note.UserId || '-'}</td>
                    <td>${note.TaggedFrom || 'FSM'}</td>
                    <td>${note.TaggedTo || '-'}</td>
                    <td>
                        ${truncatedNote}
                        ${showReadMore ? `<button class="btn btn-sm btn-link read-more-btn" data-note-id="${note.Id}">Read More</button>` : ''}
                    </td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary edit-note-btn" data-note-id="${note.Id}"><i class="fas fa-edit"></i></button>
                        <button class="btn btn-sm btn-outline-danger delete-note-btn" data-note-id="${note.Id}"><i class="fas fa-trash-alt"></i></button>
                    </td>
                </tr>
            `;
        });
        $('#notesTableBody').html(html);
    }

    // Forms, Pictures, Files, Agreements loading and rendering
    function loadForms() {
        // Get all appointments for this customer/site to find associated forms
        if (!appointmentData || appointmentData.length === 0) {
            // Wait for appointments to load
            setTimeout(loadForms, 500);
            return;
        }

        // Collect all appointment IDs
        const appointmentIds = appointmentData.map(apt => apt.AppoinmentId).filter(id => id);

        if (appointmentIds.length === 0) {
            $('#formsTableBody').html('<tr><td colspan="6" class="text-center text-muted">No appointments found. Forms are associated with appointments.</td></tr>');
            return;
        }

        // Sort appointments by date (most recent first)
        const sortedAppointments = [...appointmentData].sort((a, b) => {
            const dateA = parseMDY(a.RequestDate || a.AppoinmentDate || a.StartDateTime) || moment(0);
            const dateB = parseMDY(b.RequestDate || b.AppoinmentDate || b.StartDateTime) || moment(0);
            return dateB.diff(dateA); // Most recent first
        });

        // For now, show a message that forms are loaded per appointment
        let formsHtml = '';
        sortedAppointments.forEach(appointment => {
            const apptId = appointment.AppoinmentId;
            const appointmentDate = appointment.RequestDate || appointment.AppoinmentDate || '-';
            const taggedFrom = 'Appointment'; // Forms are typically created with appointments
            const taggedTo = '-';
            const formName = `Forms for Appointment #${apptId}`;

                    formsHtml += `<tr ${apptId ? `data-appointment-id="${apptId}"` : ''}>
                        <td>#${apptId}</td>
                        <td>${appointmentDate}</td>
                        <td>${taggedFrom}</td>
                        <td>${taggedTo}</td>
                        <td>
                            <span class="form-preview" title="${formName}">
                                ${formName}
                            </span>
                        </td>
                        <td>
                            <div class="btn-group btn-group-sm">
                                <button class="btn btn-outline-primary view-forms-btn" data-appt-id="${apptId}" title="View Forms">
                                    <i class="fas fa-eye"></i>
                                </button>
                            </div>
                        </td>
                    </tr>`;        });

        if (formsHtml === '') {
            formsHtml = '<tr><td colspan="6" class="text-center text-muted">No forms found.</td></tr>';
        }

        $('#formsTableBody').html(formsHtml);
    }

    function loadPictures() {
        if (!customerId) {
            console.error('Cannot load pictures: customerId is missing');
            $('#picturesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error: Customer ID is missing. Cannot load pictures.</td></tr>');
            return;
        }

        console.log('Loading pictures for customerId:', customerId, 'siteId:', siteId);

        $.ajax({
            type: "POST",
            url: "CustomerDetails.aspx/GetSitePictures",
            data: JSON.stringify({ customerId: customerId, siteId: siteId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log('Pictures loaded:', response);
                // Handle both direct array and wrapped response
                let pictures = [];
                if (response && response.d !== undefined) {
                    pictures = response.d || [];
                } else if (Array.isArray(response)) {
                    pictures = response;
                }
                console.log('Pictures data:', pictures, 'Count:', pictures.length);
                sitePictures = pictures; // Store for CSL links
                renderPictures();
            },
            error: function (xhr, status, error) {
                console.error('Error loading pictures:', error, xhr);
                $('#picturesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error loading pictures.</td></tr>');
            }
        });
    }

    function renderPictures() {
        // Always render, even if empty
        if (!sitePictures || sitePictures.length === 0) {
            $('#picturesTableBody').html('<tr><td colspan="6" class="text-center text-muted">No pictures found.</td></tr>');
            return;
        }

        console.log('Rendering pictures, count:', sitePictures.length);

        let html = '';
        sitePictures.forEach(pic => {
            html += `
                <tr ${pic.AppointmentId ? `data-appointment-id="${pic.AppointmentId}"` : ''}>
                    <td>${pic.AppointmentId ? '#' + pic.AppointmentId : '-'}</td>
                    <td>${pic.UploadDate || '-'}</td>
                    <td>${pic.TaggedFrom || 'FSM'}</td>
                    <td>${pic.TaggedTo || '-'}</td>
                    <td>
                        <img src="${pic.FileUrl}" style="max-width: 100px; max-height: 100px; cursor: pointer;" onclick="window.open('${pic.FileUrl}', '_blank')" />
                        <div>${pic.FileName || '-'}</div>
                    </td>
                    <td>
                        <a href="${pic.FileUrl}" target="_blank" class="btn btn-sm btn-primary">View</a>
                    </td>
                </tr>
            `;
        });
        $('#picturesTableBody').html(html);
    }

    function loadFiles() {
        if (!customerId) {
            console.error('Cannot load files: customerId is missing');
            $('#filesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error: Customer ID is missing. Cannot load files.</td></tr>');
            return;
        }

        console.log('Loading files for customerId:', customerId, 'siteId:', siteId);

        $.ajax({
            type: "POST",
            url: "CustomerDetails.aspx/GetSiteFiles",
            data: JSON.stringify({ customerId: customerId, siteId: siteId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log('Files loaded:', response);
                // Handle both direct array and wrapped response
                let files = [];
                if (response && response.d !== undefined) {
                    files = response.d || [];
                } else if (Array.isArray(response)) {
                    files = response;
                }
                console.log('Files data:', files, 'Count:', files.length);
                siteFilesData = files; // Store for CSL links
                renderFiles();
            },
            error: function (xhr, status, error) {
                console.error('Error loading files:', error, xhr);
                $('#filesTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error loading files.</td></tr>');
            }
        });
    }

    function renderFiles() {
        // Always render, even if empty
        console.log('renderFiles called, siteFilesData:', siteFilesData);
        console.log('siteFilesData length:', siteFilesData ? siteFilesData.length : 0);

        if (!siteFilesData || siteFilesData.length === 0) {
            $('#filesTableBody').html('<tr><td colspan="6" class="text-center text-muted">No files found.</td></tr>');
            return;
        }

        console.log('Rendering files, count:', siteFilesData.length);

        let html = '';
        siteFilesData.forEach(file => {
            const fileUrl = file.FileUrl || `/CustomerDetails.aspx?type=file&id=${file.Id}`;
            html += `
                <tr ${file.AppointmentId ? `data-appointment-id="${file.AppointmentId}"` : ''}>
                    <td>${file.AppointmentId ? '#' + file.AppointmentId : '-'}</td>
                    <td>${file.UploadDate || '-'}</td>
                    <td>${file.TaggedFrom || 'FSM'}</td>
                    <td>${file.TaggedTo || '-'}</td>
                    <td>
                        <a href="${fileUrl}" target="_blank">${escapeHTML(file.FileName || '-')}</a>
                        <div><small>${escapeHTML(file.FileType || '-')} - ${formatFileSize(file.FileSize || 0)}</small></div>
                    </td>
                    <td>
                        <a href="${fileUrl}" target="_blank" class="btn btn-sm btn-primary">View</a>
                    </td>
                </tr>
            `;
        });
        $('#filesTableBody').html(html);
    }

    function loadAgreements() {
        if (!customerId) {
            console.error('Cannot load agreements: customerId is missing');
            $('#agreementTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error: Customer ID is missing. Cannot load agreements.</td></tr>');
            return;
        }

        console.log('Loading agreements for customerId:', customerId, 'siteId:', siteId);

        $.ajax({
            type: "POST",
            url: "CustomerDetails.aspx/GetMaintenanceAgreements",
            data: JSON.stringify({ customerId: customerId, siteId: siteId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log('Agreements loaded:', response);
                // Handle both direct array and wrapped response
                let agreements = [];
                if (response && response.d !== undefined) {
                    agreements = response.d || [];
                } else if (Array.isArray(response)) {
                    agreements = response;
                }
                console.log('Agreements data:', agreements, 'Count:', agreements.length);
                siteAgreementsData = agreements;
                renderAgreements();
            },
            error: function (xhr, status, error) {
                console.error('Error loading agreements:', error, xhr);
                $('#agreementTableBody').html('<tr><td colspan="6" class="text-center text-danger">Error loading agreements.</td></tr>');
            }
        });
    }

    function renderAgreements() {
        // Always render, even if empty
        console.log('renderAgreements called, siteAgreementsData:', siteAgreementsData);
        console.log('siteAgreementsData length:', siteAgreementsData ? siteAgreementsData.length : 0);

        if (!siteAgreementsData || siteAgreementsData.length === 0) {
            $('#agreementTableBody').html('<tr><td colspan="6" class="text-center text-muted">No maintenance agreements found.</td></tr>');
            return;
        }

        console.log('Rendering agreements, count:', siteAgreementsData.length);

        let html = '';
        siteAgreementsData.forEach(agreement => {
            const fileUrl = agreement.FileUrl || `/CustomerDetails.aspx?type=agreement&id=${agreement.Id}`;
            html += `
                <tr ${agreement.AppointmentId ? `data-appointment-id="${agreement.AppointmentId}"` : ''}>
                    <td>${agreement.AppointmentId ? '#' + agreement.AppointmentId : '-'}</td>
                    <td>${agreement.UploadDate || '-'}</td>
                    <td>${agreement.TaggedFrom || 'FSM'}</td>
                    <td>${agreement.TaggedTo || '-'}</td>
                    <td>
                        <a href="${fileUrl}" target="_blank">${escapeHTML(agreement.FileName || agreement.Name || '-')}</a>
                    </td>
                    <td>
                        <a href="${fileUrl}" target="_blank" class="btn btn-sm btn-primary">View</a>
                    </td>
                </tr>
            `;
        });
        console.log('Setting HTML to agreementTableBody, HTML length:', html.length);
        $('#agreementTableBody').html(html);
        console.log('HTML set to agreementTableBody');
    }

    // Status history modal
    function openStatusHistoryModal(appointmentId) {
        $.ajax({
            url: 'CustomerDetails.aspx/GetAppointmentStatusHistory',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ appointmentId: appointmentId }),
            dataType: 'json',
            success: (rs) => {
                const history = rs.d || [];
                let html = '<table class="table"><thead><tr><th>Status</th><th>Changed By</th><th>Date/Time</th></tr></thead><tbody>';
                history.forEach(h => {
                    html += `<tr><td>${h.StatusName || '-'}</td><td>${h.ChangedBy || '-'}</td><td>${h.Timestamp || '-'}</td></tr>`;
                });
                html += '</tbody></table>';
                $('#statusHistoryModalBody').html(html);
                $('#statusHistoryModal').modal('show');
            },
            error: () => {
                showToast('Error loading status history.');
            }
        });
    }

    // Note management
    $(document).on('click', '.edit-note-btn', function () {
        const noteId = $(this).data('note-id');
        const note = notesData.find(n => n.Id === noteId);
        if (note) {
            $('#noteId').val(note.Id);
            $('#noteDescription').val(note.Description);
            $('#noteTaggedTo').val(note.TaggedTo);
            $('#noteTaggedFrom').val(note.TaggedFrom);
            $('#noteModal').modal('show');
        }
    });

    $(document).on('click', '.delete-note-btn', function () {
        const noteId = $(this).data('note-id');
        if (confirm('Are you sure you want to delete this note?')) {
            $.ajax({
                url: 'CustomerDetails.aspx/DeleteCustomerNote',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ noteId: noteId }),
                dataType: 'json',
                success: (rs) => {
                    if (response && response.d === true) {
                        showToast('Note deleted successfully!');
                        loadNotes();
                    } else {
                        showToast('Error deleting note.');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error deleting note:', error, xhr);
                    showToast('Error deleting note. Please try again.');
                }
            });
        }
    });

    // Print notes to PDF
    function printNotesToPDF() {
        const filteredNotes = getFilteredNotes();
        let htmlContent = `
            <!DOCTYPE html>
            <html>
            <head>
                <title>Customer Notes</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; }
                    table { width: 100%; border-collapse: collapse; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                </style>
            </head>
            <body>
                <h1>Customer Notes</h1>
                <table>
                    <thead>
                        <tr>
                            <th>Date/Time</th>
                            <th>Tagged From</th>
                            <th>Tagged To</th>
                            <th>Note</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        filteredNotes.forEach(item => {
            const noteText = item.Description || '';
            htmlContent += `
                <tr>
                    <td>${item.CreatedAt || '-'}</td>
                    <td>${item.TaggedFrom || item.Source || 'FSM'}</td>
                    <td>${item.TaggedTo || item.TaggedToName || ''}</td>
                    <td>${escapeHTML(noteText)}</td>
                </tr>
            `;
        });

        htmlContent += `
                    </tbody>
                </table>
            </body>
            </html>
        `;

        // Open print window
        const printWindow = window.open('', '_blank');
        printWindow.document.write(htmlContent);
        printWindow.document.close();
        printWindow.onload = function () {
            printWindow.print();
        };
    }

    function escapeHTML(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // Picture upload handler
    function handlePictureUpload(files) {
        if (!customerId) {
            showToast('Error: Customer ID is missing. Cannot upload pictures.');
            return;
        }

        const fileArray = Array.from(files);
        let uploadCount = 0;
        let errorCount = 0;

        fileArray.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const base64Content = e.target.result.split(',')[1]; // Remove data:image/...;base64, prefix

                $.ajax({
                    type: "POST",
                    url: "CustomerDetails.aspx/SaveSitePicture",
                    data: JSON.stringify({
                        customerId: customerId,
                        siteId: siteId,
                        fileName: file.name,
                        fileContent: base64Content
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        uploadCount++;
                        if (response && response.d === true) {
                            console.log(`Picture ${file.name} uploaded successfully`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Successfully uploaded ${uploadCount} picture(s)!`);
                                $('#pictureUploadInput').val(''); // Clear input
                                loadPictures(); // Reload pictures list
                            }
                        } else {
                            errorCount++;
                            console.error(`Failed to upload ${file.name}`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Uploaded ${uploadCount - errorCount} picture(s), ${errorCount} failed.`);
                                $('#pictureUploadInput').val('');
                                loadPictures();
                            }
                        }
                    },
                    error: function (xhr, status, error) {
                        errorCount++;
                        uploadCount++;
                        console.error(`Error uploading ${file.name}:`, error);
                        if (uploadCount === fileArray.length) {
                            showToast(`Uploaded ${uploadCount - errorCount} picture(s), ${errorCount} failed.`);
                            $('#pictureUploadInput').val('');
                            loadPictures();
                        }
                    }
                });
            };
            reader.onerror = function () {
                errorCount++;
                uploadCount++;
                console.error(`Error reading file ${file.name}`);
                if (uploadCount === fileArray.length) {
                    showToast(`Error reading some files. Uploaded ${uploadCount - errorCount} picture(s).`);
                    $('#pictureUploadInput').val('');
                }
            };
            reader.readAsDataURL(file);
        });
    }

    // File upload handler
    function handleFileUpload(files) {
        if (!customerId) {
            showToast('Error: Customer ID is missing. Cannot upload files.');
            return;
        }

        const fileArray = Array.from(files);
        let uploadCount = 0;
        let errorCount = 0;

        fileArray.forEach((file) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const base64Content = e.target.result.split(',')[1];

                $.ajax({
                    type: "POST",
                    url: "CustomerDetails.aspx/SaveSiteFile",
                    data: JSON.stringify({
                        customerId: customerId,
                        siteId: siteId,
                        fileName: file.name,
                        fileType: file.type || 'application/octet-stream',
                        fileSize: file.size,
                        fileContent: base64Content
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        uploadCount++;
                        if (response && response.d === true) {
                            console.log(`File ${file.name} uploaded successfully`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Successfully uploaded ${uploadCount} file(s)!`);
                                $('#fileUploadInput').val('');
                                // Reload files data
                                if (cslDataLoaded) {
                                    // Reload all CSL data to get updated files
                                    loadAllData();
                                } else {
                                    // Just reload files
                                    loadFiles();
                                }
                            }
                        } else {
                            errorCount++;
                            console.error(`Failed to upload ${file.name}: Server returned false`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Uploaded ${uploadCount - errorCount} file(s), ${errorCount} failed.`);
                                $('#fileUploadInput').val('');
                                // Reload files data even if some failed
                                if (cslDataLoaded) {
                                    loadAllData();
                                } else {
                                    loadFiles();
                                }
                            }
                        }
                    },
                    error: function (xhr, status, error) {
                        errorCount++;
                        uploadCount++;
                        console.error(`Error uploading ${file.name}:`, error, xhr);
                        if (uploadCount === fileArray.length) {
                            showToast(`Uploaded ${uploadCount - errorCount} file(s), ${errorCount} failed.`);
                            $('#fileUploadInput').val('');
                            // Reload files data even if some failed
                            if (cslDataLoaded) {
                                loadAllData();
                            } else {
                                loadFiles();
                            }
                        }
                    }
                });
            };
            reader.onerror = function () {
                errorCount++;
                uploadCount++;
                console.error(`Error reading file ${file.name}`);
                if (uploadCount === fileArray.length) {
                    showToast(`Error reading some files. Uploaded ${uploadCount - errorCount} file(s).`);
                    $('#fileUploadInput').val('');
                }
            };
            reader.readAsDataURL(file);
        });
    }

    // Agreement upload handler
    function handleAgreementUpload(files) {
        if (!customerId) {
            showToast('Error: Customer ID is missing. Cannot upload agreements.');
            return;
        }

        const fileArray = Array.from(files);
        let uploadCount = 0;
        let errorCount = 0;

        fileArray.forEach((file) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const base64Content = e.target.result.split(',')[1];

                $.ajax({
                    type: "POST",
                    url: "CustomerDetails.aspx/SaveMaintenanceAgreement",
                    data: JSON.stringify({
                        customerId: customerId,
                        siteId: siteId,
                        fileName: file.name,
                        fileContent: base64Content
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        uploadCount++;
                        if (response && response.d === true) {
                            console.log(`Agreement ${file.name} uploaded successfully`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Successfully uploaded ${uploadCount} agreement(s)!`);
                                $('#agreementUploadInput').val('');
                                $('#agreeFile').val('');
                                // Reload agreements data
                                if (cslDataLoaded) {
                                    // Reload all CSL data to get updated agreements
                                    loadAllData();
                                } else {
                                    // Just reload agreements
                                    loadAgreements();
                                }
                            }
                        } else {
                            errorCount++;
                            console.error(`Failed to upload ${file.name}: Server returned false`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Uploaded ${uploadCount - errorCount} agreement(s), ${errorCount} failed.`);
                                $('#agreementUploadInput').val('');
                                $('#agreeFile').val('');
                                // Reload agreements data even if some failed
                                if (cslDataLoaded) {
                                    loadAllData();
                                } else {
                                    loadAgreements();
                                }
                            }
                        }
                    },
                    error: function (xhr, status, error) {
                        errorCount++;
                        uploadCount++;
                        console.error(`Error uploading ${file.name}:`, error, xhr);
                        if (uploadCount === fileArray.length) {
                            showToast(`Uploaded ${uploadCount - errorCount} agreement(s), ${errorCount} failed.`);
                            $('#agreementUploadInput').val('');
                            $('#agreeFile').val('');
                            // Reload agreements data even if some failed
                            if (cslDataLoaded) {
                                loadAllData();
                            } else {
                                loadAgreements();
                            }
                        }
                    }
                });
            };
            reader.onerror = function () {
                errorCount++;
                uploadCount++;
                console.error(`Error reading file ${file.name}`);
                if (uploadCount === fileArray.length) {
                    showToast(`Error reading some files. Uploaded ${uploadCount - errorCount} agreement(s).`);
                    $('#agreementUploadInput').val('');
                }
            };
            reader.readAsDataURL(file);
        });
    }

}); // End of DOMContentLoaded