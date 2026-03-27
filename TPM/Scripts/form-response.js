

$(document).ready(function () {
    const urlParams = new URLSearchParams(window.location.search);
    const companyId = urlParams.get('companyId');
    const templateId = urlParams.get('templateId');
    loadForm(templateId, companyId)
});

function loadForm(templateId, companyId) {

    $.ajax({
        type: "POST",
        url: "Forms.aspx/GetFormStructure",
        data: JSON.stringify({  }), 
        data: JSON.stringify({
            templateId: templateId,
            companyId: companyId 
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            try {
                const formStructure = response.d ? JSON.parse(response.d) : {};
                console.log('Loaded form structure:', formStructure);

                // Clear the form builder
                $('#formViewerContainer').empty();

                if (formStructure.fields && formStructure.fields.length > 0) {
                    // Load existing fields
                    formStructure.fields.forEach(function (field) {
                        const fieldHtml = generateFieldFromStructure(field);
                        $('#formViewerContainer').append(fieldHtml);
                    });
                } else {
                    // Show empty state
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
// Generate dropdown HTML from options
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

// Generate radio button HTML from options
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
               <label style="margin-bottom: 7px;"><i class="fa ${config.icon}"></i> ${fieldLabel}${isRequired ? ' *' : ''}</label>
                ${config.input}
            </div>
        </div>
    `;
}
function selectField(fieldId) {
    document.querySelectorAll('.form-field').forEach(el => el.classList.remove('selected'));
    const field = document.querySelector(`[data-field-id="${fieldId}"]`);
    if (field) {
        field.classList.add('selected');
    }
}

function submitResponse() {
    const urlParams = new URLSearchParams(window.location.search);

    const templateId = urlParams.get('templateId');
    const companyId = urlParams.get('companyId');
    const apptId = urlParams.get('apptId');
    const cId = urlParams.get('cId');
    let formData = [];

    $('#formViewerContainer .form-field').each(function () {
        const fieldId = $(this).data('field-id');
        const fieldType = $(this).data('field-type');
        const label = $(this).find('label').text().replace('*', '').trim();
        let value = null;

        switch (fieldType) {
            case 'text':
            case 'number':
            case 'date':
                value = $(this).find('input').val();
                break;
            case 'textarea':
                value = $(this).find('textarea').val();
                break;
            case 'dropdown':
                value = $(this).find('select').val();
                break;
            case 'checkbox':
                value = $(this).find('input[type="checkbox"]').is(':checked');
                break;
            case 'radio':
                value = $(this).find('input[type="radio"]:checked').val();
                break;
            case 'signature':
                // For now just placeholder text — integrate your signature pad later
                value = "Signature Captured";
                break;
            default:
                value = $(this).find('input').val();
        }

        formData.push({
            fieldId: fieldId,
            label: label,
            type: fieldType,
            value: value
        });
    });

    console.log("Collected Form Data:", formData);

    let formDataString = JSON.stringify(formData);
    // Base64-encode to avoid any request validation or special char issues
    try {
        formDataString = btoa(unescape(encodeURIComponent(formDataString)));
    } catch (e) {
        console.warn('Failed to base64-encode payload, sending raw JSON');
    }
    $.ajax({
        type: "POST",
        url: "Forms.aspx/SaveFormResponse",
        data: JSON.stringify({
            responses: formDataString,
            templateId: parseInt(templateId) || 0,
            companyId: companyId,
            apptId: parseInt(apptId) || 0,
            cId: parseInt(cId) || 0
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            const r = response && response.d;
            if (r && (r.success === true || r === "Success")) {
                alert("Thank You For Your Response");
            } else {
                const msg = (r && (r.message || r)) || "Error";
                console.error("SaveFormResponse failed:", r);
                alert(msg);
            }
        },
        error: function (xhr, status, error) {
            console.error('AJAX Error:', { status, error, responseText: xhr.responseText });
            alert("Server error: " + error);
        }
    });
}