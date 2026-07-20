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

    // Global event listener for picture deletion
    $(document).on('click', '.delete-picture-btn', function () {
        const pictureId = $(this).data('picture-id');
        const fileName = $(this).data('file-name');
        console.log('Global delete picture clicked:', pictureId, fileName);
        deletePicture(pictureId, fileName);
    });

    // Get customer data from server controls - try multiple selectors
    let customerId = '';
    let siteId = 0;
    let customerGuid = '';
    let appointmentId = '';

    // Try to get from MainContent prefixed IDs first
    const customerIdEl = document.getElementById('MainContent_lblCustomerId') || document.querySelector('[id$="lblCustomerId"]');
    const siteIdEl = document.getElementById('MainContent_lblSiteId') || document.querySelector('[id$="lblSiteId"]');
    const customerGuidEl = document.getElementById('MainContent_lblCustomerGuid') || document.querySelector('[id$="lblCustomerGuid"]');
    const appointmentIdEl = document.getElementById('MainContent_lblAppointmentId') || document.querySelector('[id$="lblAppointmentId"]');

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
    if (appointmentIdEl) {
        appointmentId = appointmentIdEl.innerText || appointmentIdEl.textContent || '';
    }

    // Debug logging
    console.log('Customer Details - Loaded IDs:', { customerId, siteId, customerGuid, appointmentId });

    if (!customerId) {
        console.error('Customer ID is missing! Cannot load data.');
        showToast('Error: Customer ID is missing. Cannot load data.');
    }

    initializeEventListeners();

    // Only load data if we have a customer ID
    if (customerId) {
        loadAllData();
        // Pre-load dropdown data for modals
        if (typeof window.loadDropdownDataForModal === 'function') {
            window.loadDropdownDataForModal();
        }
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
        $('#invFilterType').on('change', function () {
            applyFiltersInv();
        });

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
        //$('#btnSms').on('click', function (e) {
        //    e.preventDefault();
        //    const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
        //    const siteId = $('#MainContent_lblSiteId').text() || $('[id$="lblSiteId"]').text() || '0';
        //    if (customerId) {
        //        window.open(`/Communications.aspx?type=sms&custId=${customerId}&siteId=${siteId}`, '_blank');
        //    }
        //});
        // SMS/MMS button handlers moved to inline script in CustomerDetails.aspx to open modals instead of redirecting
        // Commented out to prevent redirect to CEC CustomerTextHistory page
        /*
        $('#btnSms').on('click', function (e) {
            e.preventDefault();
            getRedirectionURL();
        });
        $('#btnMms').on('click', function (e) {
            e.preventDefault();
            getRedirectionURL();
        });
        */
        //$('#btnTextHistory').on('click', function (e) {
        //    e.preventDefault();
        //    getRedirectionURL();
        //});
        $('#btnApptClearDate').on('click', function () {
            apptStartDate = null;
            apptEndDate = null;
            $('#apptDateRangePicker').val('');
            applyFiltersAppt();
        });

        // Sortable header click handler for Appointments table
        $(document).on('click', '.sortable-header', function () {
            const sortColumn = $(this).data('sort');
            if (apptSortColumn === sortColumn) {
                apptSortDirection = apptSortDirection === 'asc' ? 'desc' : 'asc';
            } else {
                apptSortColumn = sortColumn;
                apptSortDirection = 'asc';
            }
            // Update sort icons
            $('.sortable-header i').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');
            const icon = $(this).find('i');
            icon.removeClass('fa-sort').addClass(apptSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
            applyFiltersAppt();
        });

        function getRedirectionURL() {
            const customerId = $('#MainContent_lblCustomerId').text() || $('[id$="lblCustomerId"]').text();
            const customerName = $('#MainContent_lblCustomerName').text();
            const customerPhone = $('#MainContent_hlPhone').text();

            $.ajax({
                type: 'POST',
                url: 'CustomerDetails.aspx/GetAuthVerifyUrl',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({
                    customerId: customerId,
                    customerName: customerName,
                    customerPhone: customerPhone
                }),
                success: function (response) {
                    window.open(response.d, '_blank');
                },
                error: function (err) {
                    console.error(err);
                    alert('Something went wrong');
                }
            });

        };
        // Picture upload button
        // Picture upload button triggers modal
        $('#uploadPictureBtn').on('click', function (e) {
            e.preventDefault();
            $('#pictureUploadReference').val('');
            $('#pictureUploadInputModal').val('');
            $('#pictureUploadModal').modal('show');
        });

        // Save Picture from modal
        $(document).on('click', '#savePictureBtn', function () {
            const files = $('#pictureUploadInputModal')[0].files;
            const reference = $('#pictureUploadReference').val();
            if (files && files.length > 0) {
                handlePictureUpload(files, reference);
                forceHideModal('#pictureUploadModal');
            } else {
                showToast('Please select at least one picture.');
            }
        });

        // File upload button triggers modal
        $('#uploadFileBtn').on('click', function (e) {
            e.preventDefault();
            $('#fileUploadReference').val('');
            $('#fileUploadInputModal').val('');
            $('#fileUploadModal').modal('show');
        });

        // Save File from modal
        $(document).on('click', '#saveFileBtn', function () {
            const files = $('#fileUploadInputModal')[0].files;
            const reference = $('#fileUploadReference').val();
            if (files && files.length > 0) {
                handleFileUpload(files, reference);
                forceHideModal('#fileUploadModal');
            } else {
                showToast('Please select at least one file.');
            }
        });
        // Add this to your initializeEventListeners function
        $('#addBtn').on('click', function (e) {
            e.preventDefault();
            saveNote();
        });

        // Event listener for the "Send Email" button to open the modal and pre-fill email
        $('#btnSendEmail').on('click', function (e) {
            e.preventDefault();
            const customerEmail = $('#MainContent_hlEmail').text() || $('[id$="hlEmail"]').text();
            if (customerEmail) {
                $('#emailTo').val(customerEmail);
            }
            // Clear other fields and attachment
            $('#emailCC').val('');
            $('#emailBCC').val('');
            $('#emailSubject').val('');
            $('#emailBody').val('');
            $('#emailAttachment').val(''); // Clear file input
            $('#emailCustomerID').val(customerId); // Set customer ID
            $('#sendEmailModal').modal('show');
        });

        // Event listener for the email form submission
        $('#sendEmailForm').on('submit', function (e) {
            e.preventDefault(); // Prevent default form submission

            const emailTo = $('#emailTo').val();
            const emailCC = $('#emailCC').val();
            const emailBCC = $('#emailBCC').val();
            const subject = $('#emailSubject').val();
            const body = $('#emailBody').val();
            const fileInput = $('#emailAttachment')[0];
            const currentCustomerId = $('#emailCustomerID').val();

            if (!emailTo || !subject || !body) {
                showToast('Please fill in all required email fields (To, Subject, Body).');
                return;
            }

            let attachmentFileName = '';
            let attachmentFileContent = '';
            let attachmentFileType = '';

            const sendEmailAjax = (fileName = '', fileContent = '', fileType = '') => {
                // Prioritize passed data, then pre-attached hidden data
                const finalFileName = fileName || $('#attachedFileName').val();
                const finalFileContent = fileContent || $('#attachedFileContent').val();
                const finalFileType = fileType || $('#attachedFileType').val();

                $.ajax({
                    type: "POST",
                    url: "CustomerDetails.aspx/SendCustomerEmail",
                    data: JSON.stringify({
                        customerId: currentCustomerId,
                        emailTo: emailTo,
                        emailCC: emailCC,
                        emailBCC: emailBCC,
                        subject: subject,
                        body: body,
                        attachmentFileName: finalFileName,
                        attachmentFileContent: finalFileContent,
                        attachmentFileType: finalFileType
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    beforeSend: function () {
                        // Optionally show a loading indicator
                        $('#sendEmailModal button[type="submit"]').prop('disabled', true).text('Sending...');
                    },
                    success: function (response) {
                        const result = response.d;
                        if (result === "Sent") {
                            showToast('Email sent successfully!');
                            $('#sendEmailModal').modal('hide');
                            $('#sendEmailForm')[0].reset(); // Reset the form
                            // Optionally reload email history if needed
                            // window.location.reload(); 
                        } else {
                            showToast('Error sending email: ' + result);
                        }
                    },
                    error: function (xhr, status, error) {
                        console.error('AJAX Error:', error, xhr);
                        let errorMessage = 'An unexpected error occurred.';
                        if (xhr.responseJSON && xhr.responseJSON.Message) {
                            errorMessage = xhr.responseJSON.Message;
                        } else if (xhr.responseText) {
                            errorMessage = xhr.responseText; // Fallback to raw response
                        }
                        showToast('Error sending email: ' + errorMessage);
                    },
                    complete: function () {
                        // Re-enable button
                        $('#sendEmailModal button[type="submit"]').prop('disabled', false).text('Send');
                    }
                });
            };

            if (fileInput.files && fileInput.files.length > 0) {
                const file = fileInput.files[0];
                const reader = new FileReader();
                reader.onload = function (e) {
                    attachmentFileName = file.name;
                    attachmentFileContent = e.target.result.split(',')[1]; // Get base64 content
                    attachmentFileType = file.type || 'application/octet-stream';
                    sendEmailAjax(attachmentFileName, attachmentFileContent, attachmentFileType);
                };
                reader.onerror = function (error) {
                    showToast('Error reading attachment file: ' + error);
                };
                reader.readAsDataURL(file);
            } else {
                sendEmailAjax(); // Send without attachment
            }
        });

        // Clear button in modal
        $('#closeSendEmail').on('click', function () {
            clearEmailModal();
        });

        // Clear icon in modal header
        $('#closeSendEmailIcon').on('click', function () {
            clearEmailModal();
        });

        function clearEmailModal() {
            $('#sendEmailForm')[0].reset();
            $('#preAttachedFile').hide();
            $('#preAttachedFileName').text('');
            $('#attachedFileName').val('');
            $('#attachedFileContent').val('');
            $('#attachedFileType').val('');
            $('#fileInputContainer').show();
            $('#sendEmailModal').modal('hide');
        }

        // Notes Export Button
        $('#notesExport').on('click', function () {
            if (!notesData || notesData.length === 0) {
                showToast('No notes to export.');
                return;
            }
            exportNotesToCSV(notesData);
        });

        function exportNotesToCSV(data) {
            const headers = ['Date/Time', 'User ID', 'Note'];
            const csvRows = [];

            // Add Header
            csvRows.push(headers.join(','));

            // Add Data
            data.forEach(note => {
                const row = [
                    `"${(note.CreatedAt || '').replace(/"/g, '""')}"`,
                    `"${(note.UserId || '').replace(/"/g, '""')}"`,
                    `"${(note.Description || '').replace(/"/g, '""')}"`
                ];
                csvRows.push(row.join(','));
            });

            const csvString = csvRows.join('\n');
            const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.setAttribute('href', url);
            link.setAttribute('download', `Notes_${customerId || 'Export'}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }


        function saveNote() {
            const description = $('#noteField').val().trim();
            // Tagging removed
            const taggedTo = '';
            const taggedFrom = '';

            if (!description) {
                showToast('Please enter a note description.');
                return;
            }

            if (!customerId) {
                showToast('Error: Customer ID is missing.');
                return;
            }

            const noteId = $('#noteId').val() || 0;
            const reference = $('#noteReference').val() || '';

            $.ajax({
                type: "POST",
                url: "CustomerDetails.aspx/SaveCustomerNote",
                data: JSON.stringify({
                    noteId: parseInt(noteId),
                    customerId: customerId,
                    siteId: siteId,
                    description: description,
                    reference: reference,
                    taggedTo: taggedTo,
                    taggedFrom: taggedFrom
                }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response && response.d === true) {
                        showToast(noteId > 0 ? 'Note updated successfully!' : 'Note saved successfully!');
                        forceHideModal('#noteModal');
                        $('#noteField').val('');
                        $('#noteReference').val('');
                        $('#noteId').val('0'); // Reset ID

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
            forceHideModal('#agreeModal');
            // Clear the file input
            $('#agreeFile').val('');
        });

        // Equipment: Open modal for adding new equipment
        $('#equipAdd').on('click', function () {
            $('#equipId').val('0');
            $('#equipForm')[0].reset();
            $('#equipModalLabel').text('Add Equipment');
            $('#equipSave').html('Save');
            loadEquipmentTypes();
        });

        // Equipment: Save button click handler
        $('#equipSave').on('click', function (e) {
            e.preventDefault();
            equipmentSave(e);
        });
    }

    // Load equipment types for dropdown when modal is shown
    $('#equipModal').on('show.bs.modal', function (event) {
        console.log('equipModal show event triggered');
        const $dropdown = $('#equipType');

        // Check if already loaded
        if ($dropdown.find('option').length > 1) {
            console.log('Equipment types already loaded, skipping');
            return;
        }

        console.log('Fetching equipment types from server...');
        $.ajax({
            url: 'CustomerDetails.aspx/GetEquipmentTypes',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({}),
            dataType: 'json',
            success: (rs) => {
                console.log('GetEquipmentTypes response:', rs);
                let equipmentTypes = rs && rs.d ? rs.d : (Array.isArray(rs) ? rs : []);
                console.log('Equipment types parsed:', equipmentTypes);
                $dropdown.empty();
                $dropdown.append('<option value="">Select Type</option>');
                equipmentTypes.forEach(type => {
                    $dropdown.append(`<option value="${type.Id}">${type.TypeName}</option>`);
                });
                console.log('Dropdown populated, options count:', $dropdown.find('option').length);
            },
            error: (xhr, status, error) => {
                console.error('Error loading equipment types:', error);
                console.error('XHR Status:', xhr.status);
                console.error('XHR Response:', xhr.responseText);
            }
        });
    });

    // Edit Equipment function - loads equipment data into modal for editing
    window.editEquipment = function (equipmentId) {
        const equipment = equipmentData.find(eq => eq.Id === equipmentId);
        if (equipment) {
            $('#equipId').val(equipment.Id);
            $('#SerialNumber').val(equipment.SerialNumber || '');
            $('#equipType').val(equipment.EquipmentTypeID || '');
            $('#Make').val(equipment.Make || '');
            $('#Model').val(equipment.Model || '');
            $('#Barcode').val(equipment.Barcode || '');
            $('#instruction').val(equipment.Notes || '');
            $('#equipInstallDate').val(equipment.InstallDate || '');
            $('#WarrantyStart').val(equipment.WarrantyStart || '');
            $('#WarrantyEnd').val(equipment.WarrantyEnd || '');
            $('#LaborWarrantyStart').val(equipment.LaborWarrantyStart || '');
            $('#LaborWarrantyEnd').val(equipment.LaborWarrantyEnd || '');

            $('#equipModalLabel').text('Edit Equipment');
            $('#equipSave').html('Update <i class="bi bi-check-circle"></i>');
            $('#equipModal').modal('show');
        } else {
            showToast('Equipment not found.');
        }
    };

    function forceHideModal(modalId) {
        const $modal = $(modalId);
        $modal.modal('hide');

        // Ensure backdrop is removed even if hide event fails
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open');
        $('body').css('overflow', '');
        $('body').css('padding-right', '');
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
        const invoiceType = params.get('invoiceType');
        if (tab) {
            const btn = document.querySelector(`#custdetTabs .nav-link[data-bs-target="#${tab}"]`);
            if (btn && window.bootstrap && bootstrap.Tab) {
                new bootstrap.Tab(btn).show();
            }
        }
        if (invoiceType) {
            setTimeout(function () {
                const typeFilter = document.getElementById('invFilterType');
                if (!typeFilter) return;
                if (invoiceType === 'Invoice') typeFilter.value = 'invoice';
                else if (invoiceType === 'Proposal') typeFilter.value = 'estimate';
                applyFiltersInv();
            }, 600);
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
            Id: $('#equipId').val() ? parseInt($('#equipId').val()) : 0,
            CustomerGuid: customerGuid,
            CustomerID: customerId,
            Make: $('#Make').val(),
            Model: $('#Model').val(),
            SerialNumber: $('#SerialNumber').val().trim(),
            Barcode: $('#Barcode').val(),
            EquipmentTypeID: $('#equipType').val() ? parseInt($('#equipType').val()) : 0,
            EquipmentType: $('#equipType').find('option:selected').text(),
            Notes: $('#instruction').val(),
            InstallDate: $('#equipInstallDate').val(),
            WarrantyStart: $('#WarrantyStart').val(),
            WarrantyEnd: $('#WarrantyEnd').val(),
            LaborWarrantyStart: $('#LaborWarrantyStart').val(),
            LaborWarrantyEnd: $('#LaborWarrantyEnd').val(),
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
                    $('#equipModal').modal('hide');
                    // Force remove backdrop after modal is hidden
                    setTimeout(() => {
                        $('.modal-backdrop').remove();
                        $('body').removeClass('modal-open');
                        $('body').css('overflow', '');
                        $('body').css('padding-right', '');
                    }, 300);
                    loadEquipment();
                } else {
                    showToast("Something went wrong!");
                }
            },
            error: () => showToast("Error saving equipment details.")
        });
    }

    // --- Note Tagging Functionality ---
    // --- Note Tagging Functionality ---
    function initializeNoteTagging() {
        // Functionality removed
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
                case 'AppoinmentStatus':
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

        const typeFilter = ($('#invFilterType').val() || 'all').toLowerCase();
        if (typeFilter === 'invoice') {
            filtered = filtered.filter(inv => (inv.InvoiceType || '').toLowerCase() === 'invoice');
        } else if (typeFilter === 'estimate') {
            filtered = filtered.filter(inv => (inv.InvoiceType || '').toLowerCase() === 'proposal');
        }

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
                // case 'Tagged From' removed
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
            $('#apptTableBody').html('<tr><td colspan="8" class="text-center text-muted">No appointments found.</td></tr>');
            return;
        }

        let html = '';
        appointments.forEach(apt => {
            const cslLinks = buildCslLinksForAppointment(apt.AppoinmentId);

            // Highlight the appointment that was redirected from
            const isHighlighted = appointmentId && (apt.AppoinmentId === appointmentId || apt.AppoinmentId.endsWith('-' + appointmentId));
            const highlightClass = isHighlighted ? 'table-active fw-bold border border-primary' : '';
            const highlightIcon = isHighlighted ? ' <i class="fas fa-arrow-circle-right text-primary" title="Selected Appointment"></i>' : '';

            html += `
                <tr class="${highlightClass}" style="cursor: default;">
                    <td>
                        <button type="button" class="btn btn-sm btn-outline-primary" onclick="showAppointmentDetailsModal('${apt.AppoinmentId}')">
                            ${apt.AppoinmentUId || '-'}${highlightIcon}
                        </button>
                    </td>
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

        // Scroll to highlighted appointment if exists
        if (appointmentId) {
            const highlightedRow = document.querySelector('#apptTableBody tr.table-active');
            if (highlightedRow) {
                highlightedRow.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }

    function buildCslLinksForAppointment(appointmentId) {
        let links = [];
        // Extract raw ID if formatted (e.g. APPT-101-505 -> 505)
        let rawId = appointmentId ? appointmentId.toString() : '';
        if (rawId.includes('-')) {
            const parts = rawId.split('-');
            rawId = parts[parts.length - 1];
        }

        // Check for notes
        const notesForAppt = notesData.filter(n => n.AppointmentId === rawId || n.AppointmentId === appointmentId.toString());
        if (notesForAppt.length > 0) {
            links.push(`<a href="#notes" class="csl-link" data-tab="notes" data-appointment-id="${appointmentId}">Notes (${notesForAppt.length})</a>`);
        }

        // Check for invoices/estimates
        const invoicesForAppt = invoiceData.filter(inv => {
            const expectedInvoiceApptId = `APPT${inv.AppointmentId}`;
            return expectedInvoiceApptId === appointmentId.toString();
        });

        if (invoicesForAppt.length > 0) {
            links.push(`<a href="#invoices" class="csl-link" data-tab="invoices" data-appointment-id="${appointmentId}">Invoices (${invoicesForAppt.length})</a>`);
        }



        // Check for pictures
        const picturesForAppt = sitePictures.filter(p => p.AppointmentId && (p.AppointmentId.toString() === rawId || p.AppointmentId.toString() === appointmentId.toString()));
        if (picturesForAppt.length > 0) {
            links.push(`<a href="#pictures" class="csl-link" data-tab="pictures" data-appointment-id="${appointmentId}">Pictures (${picturesForAppt.length})</a>`);
        }

        // Check for files
        const filesForAppt = siteFilesData.filter(f => f.AppointmentId && (f.AppointmentId.toString() === rawId || f.AppointmentId.toString() === appointmentId.toString()));
        if (filesForAppt.length > 0) {
            links.push(`<a href="#files" class="csl-link" data-tab="files" data-appointment-id="${appointmentId}">Files (${filesForAppt.length})</a>`);
        }

        // Check for forms (forms are associated with appointments)
        links.push(`<a href="#forms" class="csl-link" data-tab="forms" data-appointment-id="${appointmentId}">Forms</a>`);

        return links.length > 0 ? links.join(' | ') : '-';
    }

    function renderInvoicesTable(invoices) {
        if (!invoices || invoices.length === 0) {
            $('#invTableBody').html('<tr><td colspan="12" class="text-center text-muted">No invoices found.</td></tr>');
            return;
        }

        let html = '';
        invoices.forEach(inv => {
            const statusClass = (inv.InvoiceStatus === 'Paid') ? 'bg-success' : 'bg-warning text-dark';
            html += `
                <tr ${inv.AppointmentId ? `data-appointment-id="${inv.AppointmentId}"` : ''}>
                    <td>${inv.AppointmentId || '-'}</td>
                    <td>${inv.InvoiceNumber || '-'}</td>
                    <td>${inv.InvoiceType || '-'}</td>
                    <td>${inv.InvoiceDate || '-'}</td>
                    <td>$${parseFloat(inv.Subtotal || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.Discount || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.Tax || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.Total || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.Due || 0).toFixed(2)}</td>
                    <td>$${parseFloat(inv.DepositAmount || 0).toFixed(2)}</td>
                    <td><span class="badge ${statusClass}">${inv.InvoiceStatus || '-'}</span></td>
                    <td>
                        <div class="d-flex gap-2">
                            <a href="${inv.ExternalLink || '#'}" target="_blank" class="btn btn-sm btn-outline-primary" title="View"><i class="fas fa-eye"></i></a>
                        </div>
                    </td>
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
                        <div class="d-flex gap-2">
                            <button class="btn btn-sm btn-outline-primary" onclick="editEquipment(${eq.Id})" title="Edit"><i class="fas fa-edit"></i></button>
                            <button class="btn btn-sm btn-outline-danger" onclick="equipmentDelete(${eq.Id})" title="Delete"><i class="fas fa-trash-alt"></i></button>
                        </div>
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
                    <td>${note.AppointmentId ? `<a href="javascript:void(0);" onclick="showAppointmentDetailsModal('${note.AppointmentId}')">${note.AppointmentId}</a>` : '-'}</td>
                    <td class="note-content-cell">
                        <div class="note-text truncated" data-full-text="${escapeHTML(noteText)}">
                            ${escapeHTML(truncatedNote)}
                        </div>
                        ${showReadMore ? `<button class="btn btn-sm btn-link read-more-btn p-0" style="text-decoration: none;">Read More</button>` : ''}
                    </td>
                    <td>${note.CreatedAt || '-'}</td>
                    <td>${escapeHTML(note.Reference || '-')}</td>
                    <td>${note.UserId || '-'}</td>
                    <td>
                        <div class="d-flex gap-2">                        
                            <button type="button" class="btn btn-sm btn-outline-primary edit-note-btn" data-note-id="${note.Id}" title="Edit"><i class="fas fa-edit"></i></button>
                              <button type="button" class="btn btn-sm btn-outline-secondary email-note-btn" data-note-id="${note.Id}" title="Email"><i class="fas fa-envelope"></i></button>
                            <button type="button" class="btn btn-sm btn-outline-danger delete-note-btn" data-note-id="${note.Id}" title="Delete"><i class="fas fa-trash-alt"></i></button>
                        </div>
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

        // Fetch forms for all appointments
        let allForms = [];
        let completedRequests = 0;

        appointmentIds.forEach(apptId => {
            $.ajax({
                type: "POST",
                url: "Forms.aspx/GetAppointmentForms",
                data: JSON.stringify({ appointmentId: apptId }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    const forms = response.d || [];
                    if (forms.length > 0) {
                        forms.forEach(form => {
                            form.AppointmentId = apptId; // Ensure appointment ID is set
                            allForms.push(form);
                        });
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error loading forms for appointment ' + apptId + ':', error);
                },
                complete: function () {
                    completedRequests++;
                    if (completedRequests === appointmentIds.length) {
                        renderForms(allForms);
                    }
                }
            });
        });
    }

    function renderForms(forms) {
        if (!forms || forms.length === 0) {
            $('#formsTableBody').html('<tr><td colspan="6" class="text-center text-muted">No forms found.</td></tr>');
            return;
        }

        let html = '';
        forms.forEach(form => {
            const apptId = form.AppointmentId || '-';
            const formattedApptId = appointmentData.find(apt => apt.AppoinmentId === apptId)?.FormattedAppointmentId || apptId;
            const dateAdded = form.StartedDateTime || form.CreatedDateTime || '-';
            const formName = form.TemplateName || `Form #${form.TemplateId}`;
            const status = form.Status || 'Pending';

            html += `<tr ${apptId ? `data-appointment-id="${apptId}"` : ''}>
                        <td>${formattedApptId}</td>
                        <td>
                            <span class="form-preview" title="${formName}">
                                ${formName}
                            </span>
                            <div><small class="text-muted">Status: ${status}</small></div>
                        </td>
                        <td>${dateAdded}</td>
                        <td>-</td>
                        <td>${form.FilledBy || '-'}</td>
                        <td>
                            <div class="d-flex gap-2">
                                <button class="btn btn-sm btn-outline-primary view-form-btn" data-form-id="${form.Id}" data-appt-id="${apptId}" title="View Form">
                                    <i class="fas fa-eye"></i>
                                </button>
                            </div>
                        </td>
                    </tr>`;
        });

        if (html === '') {
            html = '<tr><td colspan="6" class="text-center text-muted">No forms found.</td></tr>';
        }

        $('#formsTableBody').html(html);
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
                    <td>${pic.AppointmentId ? `<a href="javascript:void(0);" onclick="showAppointmentDetailsModal(${pic.AppointmentId})">${pic.AppointmentId}</a>` : '-'}</td>
                    <td>
                        <img src="${pic.FileUrl}" style="max-width: 100px; max-height: 100px; cursor: pointer;" onclick="window.open('${pic.FileUrl}', '_blank')" />
                        <div>${pic.FileName || '-'}</div>
                    </td>
                    <td>${pic.UploadDate || '-'}</td>
                    <td>${escapeHTML(pic.Reference || '-')}</td>
                    <td>${pic.UploadedBy || '-'}</td>
                    <td>
                        <div class="d-flex gap-2">
                            <a href="${pic.FileUrl}" target="_blank" class="btn btn-sm btn-outline-primary" title="View"><i class="fas fa-eye"></i></a>
                            <button type="button" class="btn btn-sm btn-outline-danger delete-picture-btn" data-picture-id="${pic.Id}" data-file-name="${escapeHTML(pic.FileName || '')}" title="Delete"><i class="fas fa-trash-alt"></i></button>
                        </div>
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
            const fileUrl = file.FileUrl || `/fsm/CustomerDetails.aspx?type=file&id=${file.Id}`;
            html += `
                <tr ${file.AppointmentId ? `data-appointment-id="${file.AppointmentId}"` : ''} class="file-row">
                    <td>${file.AppointmentId ? `<a href="javascript:void(0);" onclick="showAppointmentDetailsModal(${file.AppointmentId})">${file.AppointmentId}</a>` : '-'}</td>
                    <td>
                        <a href="${fileUrl}" target="_blank" class="file-preview-trigger" data-file-url="${fileUrl}" data-file-type="${file.FileType || ''}">${escapeHTML(file.FileName || '-')}</a>
                        <div><small>${escapeHTML(file.FileType || '-')} - ${formatFileSize(file.FileSize || 0)}</small></div>
                    </td>
                    <td>${file.UploadDate || '-'}</td>
                    <td>${escapeHTML(file.Reference || '-')}</td>
                    <td>${file.UploadedBy || '-'}</td>
                    <td>
                        <div class="d-flex gap-2">
                            <a href="${fileUrl}" target="_blank" class="btn btn-sm btn-outline-primary" title="View"><i class="fas fa-eye"></i></a>
                            <button type="button" class="btn btn-sm btn-outline-secondary email-file-btn" data-file-id="${file.Id}" data-file-name="${escapeHTML(file.FileName || '')}" title="Email"><i class="fas fa-envelope"></i></button>
                            <button type="button" class="btn btn-sm btn-outline-danger delete-file-btn" data-file-id="${file.Id}" data-file-name="${escapeHTML(file.FileName || '')}" title="Delete"><i class="fas fa-trash-alt"></i></button>
                        </div>
                    </td>
                </tr>
            `;
        });
        $('#filesTableBody').html(html);

        // Add tooltip element if it doesn't exist
        if ($('#filePreviewTooltip').length === 0) {
            $('body').append('<div id="filePreviewTooltip" class="file-preview-tooltip"></div>');
        }

        // Attach hover events to the TRIGGER (the link) now, not the whole row
        $('.file-preview-trigger').hover(function (e) {
            const fileUrl = $(this).data('file-url');
            const fileType = $(this).data('file-type') || '';
            const isImage = fileType.toLowerCase().includes('image');
            const isPdf = fileType.toLowerCase().includes('pdf');

            let content = '';
            if (isImage) {
                content = `<img src="${fileUrl}" alt="Preview" />`;
            } else if (isPdf) {
                content = `<div class="preview-placeholder"><i class="fas fa-file-pdf"></i><span>PDF Document</span></div>`;
            } else {
                content = `<div class="preview-placeholder"><i class="fas fa-file"></i><span>${fileType || 'File'}</span></div>`;
            }

            $('#filePreviewTooltip').html(content).show();
        }, function () {
            $('#filePreviewTooltip').hide();
        }).mousemove(function (e) {
            const tooltip = $('#filePreviewTooltip');
            const x = e.clientX + 20;
            const y = e.clientY + 20;

            // Boundary checks
            const winWidth = $(window).width();
            const winHeight = $(window).height();
            const toolWidth = tooltip.outerWidth();
            const toolHeight = tooltip.outerHeight();

            let posX = x;
            let posY = y;

            if (x + toolWidth > winWidth) posX = e.clientX - toolWidth - 20;
            if (y + toolHeight > winHeight) posY = e.clientY - toolHeight - 20;

            tooltip.css({ left: posX, top: posY });
        });

        // Attach delete events
        $('.delete-file-btn').off('click').on('click', function () {
            const fileId = $(this).data('file-id');
            const fileName = $(this).data('file-name');
            deleteFile(fileId, fileName);
        });

        // Attach delete events for pictures
        $('.delete-picture-btn').on('click', function () {
            const pictureId = $(this).data('picture-id');
            const fileName = $(this).data('file-name');
            deletePicture(pictureId, fileName);
        });

        // Attach e-mail events for files - using existing email modal
        $('.email-file-btn').on('click', function () {
            const fileId = $(this).data('file-id');
            const fileName = $(this).data('file-name');

            // Show loading or just open modal
            $('#emailSubject').val(`Shared File: ${fileName}`);
            $('#emailBody').val(`Hello,\n\nPlease find the attached file: ${fileName}.\n\nRegards,`);
            $('#emailCustomerID').val(customerId);

            // Fetch file content to pre-attach
            $.ajax({
                type: "POST",
                url: "CustomerDetails.aspx/GetFileContent",
                data: JSON.stringify({ fileId: fileId, type: 'file' }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (response.d && response.d.status === "success") {
                        $('#attachedFileName').val(response.d.fileName);
                        $('#attachedFileContent').val(response.d.content);
                        $('#attachedFileType').val(response.d.contentType);
                        $('#preAttachedFileName').text(response.d.fileName);
                        $('#preAttachedFile').show();
                        $('#fileInputContainer').hide();
                        $('#sendEmailModal').modal('show');
                    } else {
                        showToast('Error fetching file for attachment: ' + (response.d ? response.d.message : 'Unknown error'));
                    }
                },
                error: function () {
                    showToast('Error fetching file content from server.');
                }
            });
        });

    }

    function deleteFile(fileId, fileName) {
        if (!confirm(`Are you sure you want to delete "${fileName}"?`)) return;

        $.ajax({
            type: "POST",
            url: "CustomerDetails.aspx/DeleteSiteFile",
            data: JSON.stringify({ fileId: fileId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                if (response.d) {
                    if (typeof showToast === 'function') {
                        showToast('File deleted successfully', 'success');
                    } else if (typeof toastr !== 'undefined') {
                        toastr.success('File deleted successfully');
                    } else {
                        alert('File deleted successfully');
                    }
                    loadFiles(); // Reload files
                } else {
                    alert('Failed to delete file');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting file:', error);
                alert('An error occurred during deletion');
            }
        });
    }

    function deletePicture(pictureId, fileName) {
        if (!confirm(`Are you sure you want to delete "${fileName}"?`)) return;

        console.log('Attempting to delete picture:');
        console.log('Picture ID:', pictureId);
        console.log('File Name:', fileName);

        $.ajax({
            type: "POST",
            url: "CustomerDetails.aspx/DeleteSitePicture", // Assuming this WebMethod exists or will be created
            data: JSON.stringify({ pictureId: pictureId }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log('DeleteSitePicture success response:', response);
                if (response.d) {
                    if (typeof showToast === 'function') {
                        showToast('Picture deleted successfully', 'success');
                    } else if (typeof toastr !== 'undefined') {
                        toastr.success('Picture deleted successfully');
                    } else {
                        alert('Picture deleted successfully');
                    }
                    loadPictures(); // Reload pictures
                } else {
                    alert('Failed to delete picture');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting picture:', error);
                console.error('XHR Response:', xhr.responseText);
                alert('An error occurred during deletion');
            }
        });
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
            const fileUrl = agreement.FileUrl || `/fsm/CustomerDetails.aspx?type=agreement&id=${agreement.Id}`;
            html += `
                    <tr ${agreement.AppointmentId ? `data-appointment-id="${agreement.AppointmentId}"` : ''}>
                        <td>${agreement.UploadDate || '-'}</td>
                        <td>
                            <a href="${fileUrl}" target="_blank">${escapeHTML(agreement.FileName || agreement.Name || '-')}</a>
                        </td>
                        <td>${agreement.ExpirationDate || '-'}</td>
                        <td>${agreement.AlarmDate || '-'}</td>
                        <td>
                            ${agreement.AlarmSet ?
                    `<span class="badge bg-success" title="Alarm Set"><i class="fas fa-bell"></i> Set</span>` :
                    `<span class="badge bg-secondary" title="No Alarm"><i class="fas fa-bell-slash"></i> No</span>`}
                            ${agreement.AlarmTriggered ? `<span class="badge bg-danger ms-1" title="Triggered"><i class="fas fa-exclamation-triangle"></i></span>` : ''}
                        </td>
                        <td>
                            <div class="d-flex gap-2">
                                <a href="${fileUrl}" target="_blank" class="btn btn-sm btn-outline-primary" title="View"><i class="fas fa-eye"></i></a>
                                <a href="${fileUrl}&download=1" class="btn btn-sm btn-outline-success" title="Download"><i class="fas fa-download"></i></a>
                                <button type="button" class="btn btn-sm btn-outline-info edit-agreement-btn" data-agreement-id="${agreement.Id}" title="Edit"><i class="fas fa-edit"></i></button>
                                <button type="button" class="btn btn-sm btn-outline-danger delete-agreement-btn" data-agreement-id="${agreement.Id}" data-filename="${escapeHTML(agreement.FileName || agreement.Name || '-')}" title="Delete"><i class="fas fa-trash-alt"></i></button>
                            </div>
                        </td>
                    </tr>
                `;
        });
        console.log('Setting HTML to agreementTableBody, HTML length:', html.length);
        $('#agreementTableBody').html(html);
        console.log('HTML set to agreementTableBody');
    }

    // Delete Agreement Handler
    $(document).on('click', '.delete-agreement-btn', function () {
        const agreementId = $(this).data('agreement-id');
        const fileName = $(this).data('filename');
        if (confirm(`Are you sure you want to delete agreement "${fileName}"?`)) {
            $.ajax({
                url: 'CustomerDetails.aspx/DeleteMaintenanceAgreement',
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify({ agreementId: agreementId }),
                dataType: 'json',
                success: (response) => {
                    if (response && response.d === true) {
                        showToast('Agreement deleted successfully!');
                        // Check if cslDataLoaded is defined/true, otherwise just loadAgreements
                        if (typeof cslDataLoaded !== 'undefined' && cslDataLoaded) {
                            loadAllData();
                        } else {
                            loadAgreements();
                        }
                    } else {
                        showToast('Error deleting agreement.');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error deleting agreement:', error, xhr);
                    showToast('Error deleting agreement. Please try again.');
                }
            });
        }
    });

    // Edit Agreement Handler
    $(document).on('click', '.edit-agreement-btn', function () {
        const agreementId = $(this).data('agreement-id');
        const agreement = siteAgreementsData.find(a => a.Id === agreementId);
        if (agreement) {
            $('#agreeId').val(agreement.Id);
            $('#agreeExpirationDate').val(agreement.ExpirationDate ? parseDateToISO(agreement.ExpirationDate) : '');
            $('#agreeAlarmDate').val(agreement.AlarmDate ? parseDateTimeToISO(agreement.AlarmDate) : '');
            $('#agreeAlarmSet').prop('checked', agreement.AlarmSet);

            $('#agreeFileUploadSection').hide();
            $('#agreeFile').prop('required', false);
            $('#agreeModalLabel').text('Edit Maintenance Agreement');
            $('#btnSaveAgreement').html('Update Agreement <i class="bi bi-check-circle"></i>');
            $('#agreeModal').modal('show');
        }
    });

    function parseDateToISO(dateStr) {
        if (!dateStr || dateStr === '-') return '';
        const m = moment(dateStr, ["MM/DD/YYYY", "YYYY-MM-DD"]);
        return m.isValid() ? m.format("YYYY-MM-DD") : '';
    }

    function parseDateTimeToISO(dateStr) {
        if (!dateStr || dateStr === '-') return '';
        const m = moment(dateStr, ["MM/DD/YYYY hh:mm A", "MM/DD/YYYY HH:mm", "YYYY-MM-DDTHH:mm"]);
        return m.isValid() ? m.format("YYYY-MM-DDTHH:mm") : '';
    }

    // Reset modal when opening for new upload
    $('#addAgreementBtn').on('click', function () {
        $('#agreeId').val('0');
        $('#agreeExpirationDate').val('');
        $('#agreeAlarmDate').val('');
        $('#agreeAlarmSet').prop('checked', false);
        $('#agreeFileUploadSection').show();
        $('#agreeFile').prop('required', true);
        $('#agreeModalLabel').text('Add Maintenance Agreement');
        $('#btnSaveAgreement').html('Upload Agreement <i class="bi bi-upload"></i>');
    });

    $('#btnSaveAgreement').on('click', function () {
        const agreementId = $('#agreeId').val();
        if (agreementId && agreementId !== '0') {
            updateAgreement(agreementId);
        } else {
            handleAgreementUpload(document.getElementById('agreeFile').files);
        }
    });

    function updateAgreement(agreementId) {
        const expirationDate = $('#agreeExpirationDate').val();
        const alarmDate = $('#agreeAlarmDate').val();
        const alarmSet = $('#agreeAlarmSet').is(':checked');

        $.ajax({
            url: 'CustomerDetails.aspx/UpdateMaintenanceAgreement',
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({
                agreementId: parseInt(agreementId),
                expirationDate: expirationDate,
                alarmDate: alarmDate,
                alarmSet: alarmSet
            }),
            dataType: 'json',
            success: (rs) => {
                if (rs && rs.d === true) {
                    showToast('Agreement updated successfully!');
                    $('#agreeModal').modal('hide');
                    loadAgreements();
                } else {
                    showToast('Error updating agreement.');
                }
            },
            error: () => showToast('Error calling update service.')
        });
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
                let html = '<table class="table"><thead><tr><th>Previous Status</th><th>New Status</th><th>Changed By</th><th>Date/Time</th></tr></thead><tbody>';
                history.forEach(h => {
                    html += `<tr><td>${h.StatusFromName || '-'}</td><td>${h.StatusName || '-'}</td><td>${h.ChangedBy || '-'}</td><td>${h.Timestamp || '-'}</td></tr>`;
                });
                html += '</tbody></table>';
                $('#statusHistoryModal .modal-body').html(html);
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
            $('#noteField').val(note.Description); // Modal in ASPX has id="noteField"
            $('#noteReference').val(note.Reference || '');
            $('#noteModalLabel').text('Edit Note');
            $('#addBtn').html('Update Note <i class="bi bi-check-circle"></i>');
            $('#noteModal').modal('show');
        }
    });

    // Reset modal when clicking "Create Note" button
    $('#addNoteBtn').on('click', function () {
        $('#noteId').val('0');
        $('#noteField').val('');
        $('#noteReference').val('');
        $('#noteModalLabel').text('Create a New Note');
        $('#addBtn').html('Add Note <i class="bi bi-plus-circle"></i>');
    });

    // Handle Read More expansion
    $(document).on('click', '.read-more-btn', function (e) {
        e.preventDefault();
        const $btn = $(this);
        const $content = $btn.siblings('.note-text');
        const fullText = $content.data('full-text');

        if ($content.hasClass('truncated')) {
            $content.html(fullText).removeClass('truncated');
            $btn.text('Show Less');
        } else {
            const truncated = fullText.substring(0, 100) + '...';
            $content.html(truncated).addClass('truncated');
            $btn.text('Read More');
        }
    });

    // Clear button functionality
    $('#clearBtn').on('click', function () {
        $('#noteField').val('');
    });

    // Handle Email Note button
    $(document).on('click', '.email-note-btn', function () {
        const noteId = $(this).data('note-id');
        const note = notesData.find(n => n.Id === noteId);
        if (note) {
            $('#emailSubject').val('Customer Note');
            $('#emailBody').val(note.Description);
            $('#emailCustomerID').val(customerId);
            $('#preAttachedFile').hide();
            $('#fileInputContainer').show();
            $('#sendEmailModal').modal('show');
        }
    });

    $(document).on('click', '#removePreAttached', function () {
        $('#preAttachedFile').hide();
        $('#preAttachedFileName').text('');
        $('#attachedFileName').val('');
        $('#attachedFileContent').val('');
        $('#attachedFileType').val('');
        $('#fileInputContainer').show();
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
                    if (rs && rs.d === true) {
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
    function handlePictureUpload(files, reference) {
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
                        fileContent: base64Content,
                        reference: reference || ''
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        uploadCount++;
                        if (response && response.d === true) {
                            console.log(`Picture ${file.name} uploaded successfully`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Successfully uploaded ${uploadCount} picture(s)!`);
                                $('#pictureUploadInputModal').val(''); // Clear input
                                loadPictures(); // Reload pictures list
                            }
                        } else {
                            errorCount++;
                            console.error(`Failed to upload ${file.name}`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Uploaded ${uploadCount - errorCount} picture(s), ${errorCount} failed.`);
                                $('#pictureUploadInputModal').val('');
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
                            $('#pictureUploadInputModal').val('');
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
    function handleFileUpload(files, reference) {
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
                        fileContent: base64Content,
                        reference: reference || ''
                    }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        uploadCount++;
                        if (response && response.d === true) {
                            console.log(`File ${file.name} uploaded successfully`);
                            if (uploadCount === fileArray.length) {
                                showToast(`Successfully uploaded ${uploadCount} file(s)!`);
                                $('#fileUploadInputModal').val('');
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
                                $('#fileUploadInputModal').val('');
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
                            $('#fileUploadInputModal').val('');
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

        const expirationDate = $('#agreeExpirationDate').val();
        const alarmDate = $('#agreeAlarmDate').val();
        const alarmSet = $('#agreeAlarmSet').is(':checked');

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
                        fileContent: base64Content,
                        expirationDate: expirationDate,
                        alarmDate: alarmDate,
                        alarmSet: alarmSet
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
                                $('#agreeModal').modal('hide');
                                $('body').removeClass('modal-open');
                                $('.modal-backdrop').remove();
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
});

function openModal(modalId) {
    $(`#${modalId}`).modal('show');
}

function closeModal(modalId) {
    const id = modalId.startsWith('#') ? modalId : '#' + modalId;
    $(id).modal('hide');
    $('.modal-backdrop').remove();
    $('body').removeClass('modal-open');
    $('body').css('overflow', '');
    $('body').css('padding-right', '');
}

$(document).on('click', '.details-email-link', function (e) {
    e.preventDefault();
    const email = $(this).text();
    const customerId = $('#MainContent_lblCustomerId').text();
    $('#emailTo').val(email);
    $('#emailCustomerID').val(customerId);
    openModal('sendEmailModal');
});

$('#closeSendEmail, #closeSendEmailIcon').on('click', function () {
    closeModal('sendEmailModal');
});

/* ==========================================
   APPOINTMENT DETAILS MODAL LOGIC (Ported)
   ========================================== */

var cslServiceTypes = [];
var cslResources = [];
var cslApptStatuses = [];
var cslTicketStatuses = [];
var allTimeSlotsCD = [];

window.dropdownDataPromise = null;

window.loadDropdownDataForModal = function () {
    if (window.dropdownDataPromise) return window.dropdownDataPromise;

    const p1 = new Promise((resolve, reject) => {
        if (cslServiceTypes.length > 0) return resolve();
        $.ajax({
            type: "POST",
            url: "Customer.aspx/GetServiceTypes",
            data: '{}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                cslServiceTypes = response.d || [];
                resolve();
            },
            error: function (e) { console.error("Error fetching ServiceTypes", e); resolve(); } // Resolve anyway to avoid blocking
        });
    });

    const p2 = new Promise((resolve, reject) => {
        if (cslResources.length > 0) return resolve();
        $.ajax({
            type: "POST",
            url: "Customer.aspx/GetResources",
            data: '{}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                cslResources = response.d || [];
                resolve();
            },
            error: function (e) { console.error("Error fetching Resources", e); resolve(); }
        });
    });

    const p3 = new Promise((resolve, reject) => {
        if (cslApptStatuses.length > 0) return resolve();
        $.ajax({
            type: "POST",
            url: "Customer.aspx/GetAppointmentStatuses",
            data: '{}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                cslApptStatuses = response.d || [];
                resolve();
            },
            error: function (e) { console.error("Error fetching AppointmentStatuses", e); resolve(); }
        });
    });

    const p4 = new Promise((resolve, reject) => {
        if (cslTicketStatuses.length > 0) return resolve();
        $.ajax({
            type: "POST",
            url: "Customer.aspx/GetTicketStatuses",
            data: '{}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                cslTicketStatuses = response.d || [];
                resolve();
            },
            error: function (e) {
                console.warn('GetTicketStatuses failed or not found', e);
                resolve();
            }
        });
    });

    const p5 = new Promise((resolve) => {
        if (allTimeSlotsCD.length > 0) return resolve();
        getTimeSlotsForCustomerDetails().then(() => resolve()).catch(() => resolve());
    });

    window.dropdownDataPromise = Promise.all([p1, p2, p3, p4, p5]);
    return window.dropdownDataPromise;
};

function getTimeSlotsForCustomerDetails() {
    return $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetTimeSlots",
        data: '{}',
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).then(function (response) {
        let slots = response.d || [];
        slots.sort(function (a, b) {
            var parseTime = function (timeStr) {
                if (!timeStr) return 0;
                var match = timeStr.match(/(\d+):(\d+)\s*(AM|PM)/i);
                if (!match) return 0;
                var hours = parseInt(match[1], 10);
                var mins = parseInt(match[2], 10);
                if (match[3].toUpperCase() === 'PM' && hours !== 12) hours += 12;
                if (match[3].toUpperCase() === 'AM' && hours === 12) hours = 0;
                return hours * 60 + mins;
            };
            return parseTime(a.StartTime || a.TimeBlockSchedule) - parseTime(b.StartTime || b.TimeBlockSchedule);
        });
        allTimeSlotsCD = slots;
        return slots;
    }).catch(function (xhr, status, error) {
        console.error("Error fetching time slots for CustomerDetails:", error);
        return [];
    });
}

function populateDropdown(elementId, data, valueField, textField, defaultText) {
    const $el = $(`#${elementId}`);
    if (!$el.length) return;
    $el.empty();
    if (defaultText) $el.append(new Option(defaultText, ""));

    if (data && Array.isArray(data)) {
        data.forEach(item => {
            $el.append(new Option(item[textField], item[valueField]));
        });
    }
}

function populateTimeSlots(slots) {
    const $timeSlot = $('#time_slot');
    if (!$timeSlot.length) return;
    $timeSlot.empty();
    $timeSlot.append('<option value="">Select Time Slot</option>');

    if (!slots || slots.length === 0) {
        console.warn('populateTimeSlots: No time slots provided');
        return;
    }

    slots.forEach(function (slot) {
        var value = slot.StartTime;
        var displayText = slot.TimeBlockSchedule || slot.TimeBlock || slot.StartTime;
        var option = $('<option></option>')
            .val(value)
            .text(displayText)
            .attr('data-id', slot.ID)
            .attr('data-start', slot.StartTime)
            .attr('data-end', slot.EndTime);
        $timeSlot.append(option);
    });
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

window.showAppointmentDetailsModal = async function (appointmentId) {
    let cleanApptId = appointmentId; // Declare variable
    if (typeof cleanApptId === 'string' && cleanApptId.includes('-')) {
        cleanApptId = cleanApptId.split('-').pop();
    }
    const intApptId = parseInt(cleanApptId, 10);

    // Wait for dropdown data to be ready
    if (typeof window.loadDropdownDataForModal === 'function') {
        await window.loadDropdownDataForModal();
    }

    // Ensure dropdowns are populated
    populateDropdown("MainContent_ServiceTypeFilter_Edit", cslServiceTypes, "ServiceTypeID", "ServiceName", "Select Service Type");
    populateDropdown("resource_list", cslResources, "Id", "Name", "Unassigned");
    if (!$('#resource_list option[value="0"]').length) {
        $('#resource_list').prepend(new Option("Unassigned", "0"));
    }
    populateDropdown("MainContent_StatusTypeFilter_Edit", cslApptStatuses, "StatusID", "StatusName", "Select Status");
    populateDropdown("MainContent_TicketStatusFilter_Edit", cslTicketStatuses, "StatusID", "StatusName", "Select Ticket Status");
    populateTimeSlots(allTimeSlotsCD);

    // Fetch full details
    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetAppointmentDetails",
        data: JSON.stringify({ appointmentId: intApptId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const details = response.d;
            if (details) {
                $('#editApptId').val(details.ApptID);
                $('#editCustomerId').val(details.CustomerID);
                // Note: details might not have SiteID if simpler model. 
                // But we can try to use it if available.
                if (details.SiteID) $('#editAppointmentForm').data('site-id', details.SiteID);

                // Scrape Contact & Site Info from the Basic Info tab on THIS page
                const contactName = $('#MainContent_lblContact').text().trim() || '';
                const phone = $('#MainContent_hlPhone').text().trim() || '';
                const mobile = $('#MainContent_hlMobile').text().trim() || '';
                const email = $('#MainContent_hlEmail').text().trim() || '';
                const address = $('#MainContent_lblAddress').text().trim() || '';
                const siteName = $('#MainContent_lblSiteNameTable').text().trim() || '';

                // Populate Contact & Site Info from the page (scraped from Basic Info tab)
                $('#custModal_CustomerName').val(contactName);
                $('#custModal_SiteName').val(siteName);
                $('#custModal_Phone').val(phone);
                $('#custModal_Mobile').val(mobile);
                $('#custModal_Email').val(email);

                // Use reliable hidden labels instead of scraping lblAddress
                $('#custModal_Address').val($('#MainContent_lblStreetAddress').text().trim() || $('#lblStreetAddress').text().trim() || '');
                $('#custModal_City').val($('#MainContent_lblCity').text().trim() || $('#lblCity').text().trim() || '');
                $('#custModal_State').val($('#MainContent_lblState').text().trim() || $('#lblState').text().trim() || '');
                $('#custModal_Zip').val($('#MainContent_lblZip').text().trim() || $('#lblZip').text().trim() || '');
                $('#custModal_Country').val($('#MainContent_lblCountry').text().trim() || $('#lblCountry').text().trim() || '');


                $('#MainContent_ServiceTypeFilter_Edit').val(details.ServiceTypeID || "");
                $('#resource_list').val(details.ResourceID || "0");

                setDropdownByTextOrValue('MainContent_StatusTypeFilter_Edit', details.Status);
                setDropdownByTextOrValue('MainContent_TicketStatusFilter_Edit', details.TicketStatus);
                // Match time slot using TimeBlockSchedule, TimeBlock, or StartTime fallback
                var timeSlotValue = (details.TimeSlot || '').trim();
                var matchingSlot = allTimeSlotsCD.find(function (slot) {
                    return (slot.TimeBlockSchedule || '').trim() === timeSlotValue ||
                           (slot.TimeBlock || '').trim() === timeSlotValue;
                });
                // Fallback: match by StartTime from appointment's Hour/Minute
                if (!matchingSlot && details.Hour !== undefined && details.Minute !== undefined) {
                    var h = parseInt(details.Hour, 10);
                    var m = parseInt(details.Minute, 10);
                    if (!isNaN(h)) {
                        var ampm = h >= 12 ? 'PM' : 'AM';
                        var hh = h % 12 || 12;
                        var startTime = hh.toString().padStart(2, '0') + ':' + (m || 0).toString().padStart(2, '0') + ' ' + ampm;
                        matchingSlot = allTimeSlotsCD.find(function (slot) {
                            return slot.StartTime === startTime;
                        });
                    }
                }
                $('#time_slot').val(matchingSlot ? matchingSlot.StartTime : '');

                $('#dateInput').val(details.Date);

                // Set duration from DB Hour/Minute first
                $('#duration').val(details.Duration || "");

                // Recalculate Start/End from Time Slot + Date + Duration
                var selectedSlotOption = $('#time_slot option:selected');
                var slotStart = selectedSlotOption.attr('data-start');
                if (!slotStart) {
                    var slotText = selectedSlotOption.text() || '';
                    var slotMatches = slotText.match(/(\d{1,2}:\d{2}\s*[AP]M)/gi);
                    if (slotMatches && slotMatches.length >= 1) slotStart = slotMatches[0].replace(/([AP]M)/i, ' $1').trim();
                }
                var loadDateVal = $('#dateInput').val();

                if (slotStart && loadDateVal) {
                    var calcStart = moment(loadDateVal + ' ' + slotStart, 'YYYY-MM-DD hh:mm A');
                    if (calcStart.isValid()) {
                        $('#txt_StartDate').val(calcStart.format('MM/DD/YYYY hh:mm A'));
                        var durMinutes = parseDuration($('#duration').val());
                        if (durMinutes > 0) {
                            var calcEnd = calcStart.clone().add(durMinutes, 'minutes');
                            $('#txt_EndDate').val(calcEnd.format('MM/DD/YYYY hh:mm A'));
                        } else {
                            // Fallback: use slot end time
                            var slotEnd = selectedSlotOption.attr('data-end');
                            if (!slotEnd && slotMatches && slotMatches.length >= 2) slotEnd = slotMatches[1].replace(/([AP]M)/i, ' $1').trim();
                            if (slotEnd) {
                                var calcEnd = moment(loadDateVal + ' ' + slotEnd, 'YYYY-MM-DD hh:mm A');
                                if (calcEnd.isValid()) $('#txt_EndDate').val(calcEnd.format('MM/DD/YYYY hh:mm A'));
                            }
                        }
                    }
                } else {
                    // No time slot available — fall back to DB StartDateTime/EndDateTime
                    var startMoment = details.StartDateTime ? moment(details.StartDateTime, 'MM/DD/YYYY hh:mm A') : null;
                    var endMoment = details.EndDateTime ? moment(details.EndDateTime, 'MM/DD/YYYY hh:mm A') : null;
                    if (startMoment && startMoment.isValid()) {
                        $('#txt_StartDate').val(startMoment.format("MM/DD/YYYY hh:mm A"));
                    } else {
                        $('#txt_StartDate').val('');
                    }
                    if (endMoment && endMoment.isValid()) {
                        $('#txt_EndDate').val(endMoment.format('MM/DD/YYYY hh:mm A'));
                    } else {
                        $('#txt_EndDate').val('');
                    }
                }

                // Fetch correct Time Required from DB based on service type and recalculate End
                var stId = parseInt(details.ServiceTypeID) || 0;
                if (stId > 0) {
                    $.ajax({
                        url: "Appointments.aspx/GetDuration",
                        type: "POST",
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        data: JSON.stringify({ serviceTypeID: stId }),
                        success: function (response) {
                            var duration = response.d || "0";
                            if (duration && duration !== "0") {
                                $('#duration').val(duration);
                                // Recalculate End from Start + new Duration
                                var curStart = moment($('#txt_StartDate').val(), 'MM/DD/YYYY hh:mm A');
                                var newDur = parseDuration(duration);
                                if (curStart.isValid() && newDur > 0) {
                                    var newEnd = curStart.clone().add(newDur, 'minutes');
                                    $('#txt_EndDate').val(newEnd.format('MM/DD/YYYY hh:mm A'));
                                }
                            }
                        }
                    });
                }

                $('#editApptNote').val(details.Note);

                // Reset Tabs
                const tabTrigger = new bootstrap.Tab(document.querySelector('#editAppointmentTabs button[data-bs-target="#appointment-details"]'));
                tabTrigger.show();

                loadFormsForModal(details.ApptID);
                loadCustomFields(null, details.ApptID);

                $('#siteAppointmentDetailsModal_PopUP').modal('show');
            } else {
                alert("Failed to load appointment details.");
            }
        },
        error: function (xhr) {
            console.error("Error fetching details", xhr.responseText);
            alert("Error fetching appointment details.");
        }
    });
};

window.calculateTimeRequired = function (event) {
    const $start = $('#txt_StartDate');
    const $end = $('#txt_EndDate');
    const $duration = $('#duration');
    const $error = $('#customer_EndDate');
    const $dateInput = $('#dateInput');
    const $timeSlot = $('#time_slot');

    // If triggered by time slot change, sync Start/End dates and return
    if (event && event.target && event.target.id === 'time_slot') {
        var selectedOption = $timeSlot.find('option:selected');
        var slotStartTime = selectedOption.attr('data-start');
        var slotEndTime = selectedOption.attr('data-end');
        var dateVal = $dateInput.val();

        // Also try extracting times from the display text as fallback
        if (!slotStartTime || !slotEndTime) {
            var displayText = selectedOption.text() || '';
            var timeMatches = displayText.match(/(\d{1,2}:\d{2}\s*[AP]M)/gi);
            if (timeMatches && timeMatches.length >= 1 && !slotStartTime) slotStartTime = timeMatches[0].replace(/([AP]M)/i, ' $1').trim();
            if (timeMatches && timeMatches.length >= 2 && !slotEndTime) slotEndTime = timeMatches[1].replace(/([AP]M)/i, ' $1').trim();
        }

        if (slotStartTime && dateVal) {
            var newStart = moment(dateVal + ' ' + slotStartTime, 'YYYY-MM-DD hh:mm A');
            if (newStart.isValid()) {
                $start.val(newStart.format('MM/DD/YYYY hh:mm A'));

                // End = Start + Time Required (if available), otherwise use slot end time
                var durationMinutes = parseDuration($duration.val());
                if (durationMinutes > 0) {
                    var newEnd = newStart.clone().add(durationMinutes, 'minutes');
                    $end.val(newEnd.format('MM/DD/YYYY hh:mm A'));
                } else if (slotEndTime) {
                    var newEnd = moment(dateVal + ' ' + slotEndTime, 'YYYY-MM-DD hh:mm A');
                    if (newEnd.isValid()) {
                        $end.val(newEnd.format('MM/DD/YYYY hh:mm A'));
                    }
                }
            }
        }
        return; // Don't recalculate duration — it should stay as the original Time Required
    }

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
};

window.updateEndDateFromDuration = function () {
    var startVal = $('#txt_StartDate').val();
    var durationVal = $('#duration').val();
    if (!startVal || !durationVal) return;

    var start = moment(startVal, 'MM/DD/YYYY hh:mm A');
    if (!start.isValid()) return;

    var durationMinutes = parseDuration(durationVal);
    if (durationMinutes <= 0) return;

    var newEnd = start.clone().add(durationMinutes, 'minutes');
    $('#txt_EndDate').val(newEnd.format('MM/DD/YYYY hh:mm A'));
};

// Update Time Required from DB when Service Type changes
$(document).on('change', '[id$="ServiceTypeFilter_Edit"]', function () {
    var serviceTypeId = parseInt($(this).val()) || 0;
    if (serviceTypeId > 0) {
        $.ajax({
            url: "CustomerDetails.aspx/GetDuration",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify({ serviceTypeID: serviceTypeId }),
            success: function (response) {
                var duration = response.d || "0";
                if (duration && duration !== "0") {
                    $('#duration').val(duration);
                    updateEndDateFromDuration();
                }
            },
            error: function (xhr, status, error) {
                console.error("Error fetching duration:", error);
            }
        });
    }
});

window.updateDate = function (event) {
    const newDate = event.target.value;
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
};

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

window.saveAppointmentChanges = function () {
    const apptId = $('#editApptId').val();
    if (!apptId) return;

    try {
    const startMom = moment($('#txt_StartDate').val(), "MM/DD/YYYY hh:mm A");
    const endMom = moment($('#txt_EndDate').val(), "MM/DD/YYYY hh:mm A");

    // Get status text, filtering out placeholder options
    var statusText = ($('#MainContent_StatusTypeFilter_Edit option:selected').val() || '0').trim();
    if (!statusText || statusText.toLowerCase().indexOf('select') === 0) statusText = '0';
    var ticketStatusText = ($('#MainContent_TicketStatusFilter_Edit option:selected').val() || '0').trim();
    if (!ticketStatusText || ticketStatusText.toLowerCase().indexOf('select') === 0) ticketStatusText = '0';

     
    var formSiteId = parseInt($('#editAppointmentForm').data('site-id'));
    if (isNaN(formSiteId)) formSiteId = 0;
    var pageSiteId = (typeof siteId !== 'undefined') ? siteId : 0;

    const appointmentData = {
        AppoinmentId: apptId,
        CustomerID: $('#editCustomerId').val(),
        ServiceType: $('#MainContent_ServiceTypeFilter_Edit').val(),
        ResourceID: parseInt($('#resource_list').val()) || 0,
        Status: statusText || '0',
        TicketStatus: ticketStatusText,
        RequestDate: startMom.isValid() ? startMom.format("YYYY-MM-DD") : $('#dateInput').val(),
        StartDateTime: startMom.isValid() ? startMom.format("MM/DD/YYYY hh:mm A") : '',
        EndDateTime: endMom.isValid() ? endMom.format("MM/DD/YYYY hh:mm A") : '',
        TimeSlot: ($('#time_slot option:selected').text() || '').trim() || '',
        Hour: (function() { var d = parseDuration($('#duration').val()); return Math.floor(d / 60); })(),
        Minute: (function() { var d = parseDuration($('#duration').val()); return d % 60; })(),
        Note: $('#editApptNote').val(),
        SiteId: formSiteId || pageSiteId || 0
    };

    // Collect Custom Fields
    const customFieldValues = [];
    $('[name^="custom_"]').each(function () {
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

    const payload = {
        appointment: appointmentData,
        customFieldValues: customFieldValues
    };

    $.ajax({
        type: "POST",
        url: "Customer.aspx/UpdateAppointmentWithCustomFields",
        data: JSON.stringify(payload),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                alert("Appointment updated successfully!");
                $('#siteAppointmentDetailsModal_PopUP').modal('hide');
                if (typeof loadAppointments === 'function') {
                    loadAppointments();
                } else {
                    location.reload();
                }
            } else {
                console.error("Update returned false. Full response:", JSON.stringify(response));
                alert("Failed to update appointment.");
            }
        },
        error: function (xhr, status, error) {
            console.error("Update AJAX error:", status, error, xhr.responseText);
            alert("An error occurred while updating: " + (error || status));
        }
    });
    } catch (e) {
        console.error('saveAppointmentChanges error:', e.message);
        alert('Save error: ' + e.message);
    }
};

function loadCustomFields(serviceTypeId, appointmentId) {
    const container = $('#customFieldsContainer');
    container.empty();

    $.ajax({
        type: "POST",
        url: "Customer.aspx/GetActiveCustomFields",
        data: JSON.stringify({ apptId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                renderCustomFields(response.d, container);
            }
        },
        error: function (xhr) {
            console.error("Failed to load custom fields", xhr.responseText);
        }
    });
}

function renderCustomFields(fields, container) {
    if (!fields || fields.length === 0) return;

    fields.forEach(field => {
        let fieldHtml = `<div class="mb-2"><label class="form-label">${field.FieldName}</label>`;
        if (field.FieldType === 'Text') {
            fieldHtml += `<input type="text" class="form-control" name="custom_${field.FieldId}" value="${field.Value || ''}">`;
        } else if (field.FieldType === 'Dropdown') {
            fieldHtml += `<select class="form-select" name="custom_${field.FieldId}">`;

            let options = [];
            if (field.Options) {
                try {
                    options = typeof field.Options === 'string' ? JSON.parse(field.Options) : field.Options;
                } catch (e) {
                    console.error("Error parsing options for field " + field.FieldName, e);
                }
            }

            if (Array.isArray(options)) {
                options.forEach(opt => {
                    fieldHtml += `<option value="${opt}" ${opt === field.Value ? 'selected' : ''}>${opt}</option>`;
                });
            }
            fieldHtml += `</select>`;
        }
        fieldHtml += `</div>`;
        container.append(fieldHtml);
    });
}

function loadFormsForModal(appointmentId) {
    const container = $('#selectedFormsEdit');
    container.html('<small class="text-muted">Loading...</small>');

    $.ajax({
        type: "POST",
        url: "Appointments.aspx/GetAppointmentForms",
        data: JSON.stringify({ appointmentId: appointmentId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.d) {
                const forms = response.d;
                populateAttachedFormsTab(forms);
            }
        },
        error: function () {
            container.html('<small class="text-error">Error loading forms</small>');
        }
    });
}

function populateAttachedFormsTab(forms) {
    const container = $('#selectedFormsEdit');
    container.empty();
    if (!forms || forms.length === 0) {
        container.html('<small class="text-muted">No forms attached to this appointment</small>');
        return;
    }
    forms.forEach(form => {
        container.append(`<span class="badge bg-success me-2 mb-2">${form.TemplateName}</span>`);
    });
}
