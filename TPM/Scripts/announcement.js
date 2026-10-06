
var tpmQuillTitle = null;
var tpmQuillDesc = null;
var _tpmDeleteID = 0;

function tpmAnnCanManage() {
    return true;
}


function tpmAnnInitQuill() {
    if (typeof Quill === 'undefined') return;
    if (document.getElementById('tpmTitleEditor') && !tpmQuillTitle) {
        tpmQuillTitle = new Quill('#tpmTitleEditor', {
            theme: 'snow',
            placeholder: 'Announcement title…',
            modules: {
                toolbar: [
                    [{ header: [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
                    [{ color: [] }, { background: [] }],
                    ['clean']
                ]
            }
        });
    }
    if (document.getElementById('tpmDescEditor') && !tpmQuillDesc) {
        tpmQuillDesc = new Quill('#tpmDescEditor', {
            theme: 'snow',
            placeholder: 'Write your announcement here…',
            modules: {
                toolbar: [
                    [{ header: [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
                    [{ color: [] }, { background: [] }],
                    ['clean']
                ]
            }
        });
        var wrap = document.getElementById('tpmDescEditorWrap');
        if (wrap) wrap.classList.add('tpm-ann-quill-wrap--desc');
    }
}

function tpmAnnStripHtml(html) {
    if (!html) return '';
    var d = document.createElement('div');
    d.innerHTML = html;
    return (d.textContent || d.innerText || '').trim();
}

function tpmAnnEsc(s) {
    if (s == null) return '';
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

/* ── Load grid (lazy: only when tab shown) ── */
var _tpmAnnLoaded = false;
function initializeAnnouncements() {
    // Add-button visibility is decided server-side (Settings.aspx renders it
    // only for allowed users), so don't hide it here.
    tpmAnnInitQuill();
    var tab = document.getElementById('announcements-tab');
    if (tab && !tab.dataset.bound) {
        tab.dataset.bound = '1';
        tab.addEventListener('shown.bs.tab', loadTpmAnnouncements);
    }
    // If deep-linked / restored active, load immediately
    if (tab && tab.classList.contains('active')) loadTpmAnnouncements();
    // Fallback: grid present but tab system not used
    var tbody = document.getElementById('tpmAnnTbody');
    if (tbody && !tab) loadTpmAnnouncements();
}

function loadTpmAnnouncements() {
    var tbody = document.getElementById('tpmAnnTbody');
    if (!tbody) return;
    tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></td></tr>';
    PageMethods.GetAnnouncements(function (res) {
        _tpmAnnLoaded = true;
        if (!res || !res.success) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-danger py-4">' + tpmAnnEsc((res && res.message) || 'Could not load announcements.') + '</td></tr>';
            return;
        }
        renderTpmAnnouncements(res.data || []);
    }, function (err) {
        console.error('GetAnnouncements error', err);
        tbody.innerHTML = '<tr><td colspan="7" class="text-center text-danger py-4">Server error loading announcements.</td></tr>';
    });
}

function renderTpmAnnouncements(list) {
    var tbody = document.getElementById('tpmAnnTbody');
    if (!list || list.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="tpm-ann-empty"><i class="bi bi-megaphone"></i><br/>No announcements yet. Click <strong>Add Announcement</strong> to create one.</td></tr>';
        return;
    }
    var html = '';
    list.forEach(function (a) {
        var badgeCls, badgeText;
        var now = new Date();
        var end = a.EndDate ? new Date(a.EndDate) : null;
        if (!a.IsActive) { badgeCls = 'tpm-ann-badge--inactive'; badgeText = 'Inactive'; }
        else if (end && end < now) { badgeCls = 'tpm-ann-badge--expired'; badgeText = 'Expired'; }
        else { badgeCls = 'tpm-ann-badge--active'; badgeText = 'Active'; }

        var timeline = '—';
        if (a.StartDate && a.EndDate) {
            var s = new Date(a.StartDate), e = new Date(a.EndDate);
            var days = Math.max(0, Math.ceil((e - s) / 86400000));
            timeline = fmtShortDate(s) + ' → ' + fmtShortDate(e) + '<br/><small>' + days + ' day' + (days === 1 ? '' : 's') + '</small>';
        } else if (a.DurationPreset && a.DurationPreset !== 'custom') {
            timeline = tpmAnnEsc(a.DurationPreset) + ' day' + (a.DurationPreset === '1' ? '' : 's');
        }

        var titleText = tpmAnnStripHtml(a.Title);
        var descText = tpmAnnStripHtml(a.Description);
        if (descText.length > 60) descText = descText.substring(0, 60) + '…';

        html += '<tr>'
            + '<td><span class="tpm-ann-badge ' + badgeCls + '"><i class="bi bi-check-circle"></i> ' + badgeText + '</span></td>'
            + '<td class="tpm-ann-td-title">' + tpmAnnEsc(titleText) + '</td>'
            + '<td class="tpm-ann-td-desc">' + tpmAnnEsc(descText) + '</td>'
            + '<td class="tpm-ann-td-user">' + tpmAnnEsc(a.CreatedBy || '—') + '</td>'
            + '<td>' + tpmAnnEsc(fmtShortDate(a.CreatedOn ? new Date(a.CreatedOn) : null)) + '</td>'
            + '<td class="tpm-ann-td-timeline">' + timeline + '</td>'
            + '<td class="tpm-ann-td-actions">'
            + (tpmAnnCanManage()
                ? '<button type="button" class="tpm-ann-act tpm-ann-act--edit" title="Edit" onclick="editTpmAnnouncement(' + a.AnnouncementID + ')"><i class="bi bi-pencil"></i></button>'
                  + '<button type="button" class="tpm-ann-act tpm-ann-act--toggle" title="' + (a.IsActive ? 'Deactivate' : 'Activate') + '" onclick="toggleTpmAnnouncement(' + a.AnnouncementID + ',' + (!a.IsActive) + ')"><i class="bi bi-' + (a.IsActive ? 'pause-circle' : 'power') + '"></i></button>'
                  + '<button type="button" class="tpm-ann-act tpm-ann-act--delete" title="Delete" onclick="confirmDeleteTpmAnnouncement(' + a.AnnouncementID + ')"><i class="bi bi-trash"></i></button>'
                : '—')
            + '</td></tr>';
    });
    tbody.innerHTML = html;
}

function fmtShortDate(d) {
    if (!d || isNaN(d)) return '—';
    var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[d.getMonth()] + ' ' + d.getDate() + ', ' + d.getFullYear();
}

/* ── Modals (Bootstrap 5) ── */
function tpmAnnModal(id) { return new bootstrap.Modal(document.getElementById(id)); }

function openTpmCreateModal() {
    tpmAnnInitQuill();
    document.getElementById('tpmAnnModalTitle').textContent = 'Add Announcement';
    document.getElementById('hfTpmAnnouncementID').value = '0';
    if (tpmQuillTitle) tpmQuillTitle.setContents([]);
    if (tpmQuillDesc) tpmQuillDesc.setContents([]);
    document.getElementById('tpmSelStatus').value = '1';
    document.getElementById('tpmSelDuration').value = '';
    document.getElementById('tpmCustomRangePanel').style.display = 'none';
    document.getElementById('tpmTxtStartDate').value = '';
    document.getElementById('tpmTxtEndDate').value = '';
    var now = new Date();
    document.getElementById('tpmSpnCreationDate').textContent = fmtLongDate(now);
    document.getElementById('tpmSpnCreationDate').dataset.iso = toIsoDate(now);
    tpmAnnModal('tpmAnnModal').show();
}

function editTpmAnnouncement(id) {
    tpmAnnInitQuill();
    PageMethods.GetAnnouncementByID(id, function (res) {
        if (!res || !res.success || !res.data) {
            Swal.fire('Error', (res && res.message) || 'Could not load announcement.', 'error');
            return;
        }
        var d = res.data;
        document.getElementById('tpmAnnModalTitle').textContent = 'Edit Announcement';
        document.getElementById('hfTpmAnnouncementID').value = d.AnnouncementID;
        if (tpmQuillTitle) { tpmQuillTitle.setContents([]); tpmQuillTitle.clipboard.dangerouslyPasteHTML(d.Title || ''); }
        if (tpmQuillDesc) { tpmQuillDesc.setContents([]); tpmQuillDesc.clipboard.dangerouslyPasteHTML(d.Description || ''); }
        document.getElementById('tpmSelStatus').value = d.IsActive ? '1' : '0';
        document.getElementById('tpmSpnCreationDate').textContent = d.CreatedOnDisplay || '';
        document.getElementById('tpmSpnCreationDate').dataset.iso = d.CreatedOnISO || '';
        var dur = d.DurationPreset || '';
        document.getElementById('tpmSelDuration').value = dur;
        if (dur === 'custom') {
            document.getElementById('tpmCustomRangePanel').style.display = 'block';
            document.getElementById('tpmTxtStartDate').value = d.StartDate || '';
            document.getElementById('tpmTxtEndDate').value = d.EndDate || '';
        } else {
            document.getElementById('tpmCustomRangePanel').style.display = 'none';
        }
        tpmAnnModal('tpmAnnModal').show();
    }, function (err) {
        console.error('GetAnnouncementByID error', err);
        Swal.fire('Error', 'Server error loading announcement.', 'error');
    });
}

function onTpmDurationChange(val) {
    document.getElementById('tpmCustomRangePanel').style.display = (val === 'custom') ? 'block' : 'none';
}

function useTpmCreationDate() {
    var iso = document.getElementById('tpmSpnCreationDate').dataset.iso;
    if (iso) document.getElementById('tpmTxtStartDate').value = iso;
}

function saveTpmAnnouncement() {
    var titleHtml = tpmQuillTitle ? tpmQuillTitle.root.innerHTML.trim() : '';
    var descHtml = tpmQuillDesc ? tpmQuillDesc.root.innerHTML.trim() : '';
    if (!titleHtml || titleHtml === '<p><br></p>') { Swal.fire('Missing', 'Please enter a title.', 'warning'); return; }

    var id = parseInt(document.getElementById('hfTpmAnnouncementID').value, 10) || 0;
    var isActive = document.getElementById('tpmSelStatus').value === '1';
    var dur = document.getElementById('tpmSelDuration').value;
    var sDate = '', eDate = '';
    if (dur === 'custom') {
        sDate = document.getElementById('tpmTxtStartDate').value;
        eDate = document.getElementById('tpmTxtEndDate').value;
        if (!sDate || !eDate) { Swal.fire('Missing', 'Please fill in both Start Date and End Date.', 'warning'); return; }
        if (new Date(eDate) <= new Date(sDate)) { Swal.fire('Invalid', 'End Date must be after Start Date.', 'warning'); return; }
    }

    var btn = document.getElementById('tpmBtnSaveAnnouncement');
    var original = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

    PageMethods.SaveAnnouncement(id, titleHtml, descHtml, isActive, dur, sDate, eDate, function (res) {
        btn.disabled = false; btn.innerHTML = original;
        if (res && res.success) {
            bootstrap.Modal.getInstance(document.getElementById('tpmAnnModal')).hide();
            Swal.fire({ icon: 'success', title: 'Saved', timer: 1400, showConfirmButton: false });
            loadTpmAnnouncements();
        } else {
            Swal.fire('Error', (res && res.message) || 'Failed to save announcement.', 'error');
        }
    }, function (err) {
        btn.disabled = false; btn.innerHTML = original;
        console.error('SaveAnnouncement error', err);
        Swal.fire('Error', 'Server error saving announcement.', 'error');
    });
}

function confirmDeleteTpmAnnouncement(id) {
    _tpmDeleteID = id;
    tpmAnnModal('tpmAnnDeleteModal').show();
}

function deleteTpmAnnouncement() {
    if (!_tpmDeleteID) return;
    PageMethods.DeleteAnnouncement(_tpmDeleteID, function (res) {
        if (res && res.success) {
            bootstrap.Modal.getInstance(document.getElementById('tpmAnnDeleteModal')).hide();
            Swal.fire({ icon: 'success', title: 'Deleted', timer: 1200, showConfirmButton: false });
            loadTpmAnnouncements();
        } else {
            Swal.fire('Error', (res && res.message) || 'Failed to delete announcement.', 'error');
        }
    }, function (err) {
        console.error('DeleteAnnouncement error', err);
        Swal.fire('Error', 'Server error deleting announcement.', 'error');
    });
}

function toggleTpmAnnouncement(id, isActive) {
    PageMethods.ToggleAnnouncementStatus(id, isActive, function (res) {
        if (res && res.success) loadTpmAnnouncements();
        else Swal.fire('Error', (res && res.message) || 'Failed to update status.', 'error');
    }, function (err) {
        console.error('ToggleAnnouncementStatus error', err);
        Swal.fire('Error', 'Server error updating status.', 'error');
    });
}

function fmtLongDate(d) {
    var months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var h = d.getHours(), m = d.getMinutes(), ampm = h >= 12 ? 'PM' : 'AM';
    h = h % 12 || 12;
    return months[d.getMonth()] + ' ' + d.getDate() + ', ' + d.getFullYear() +
        ' at ' + String(h).padStart(2, '0') + ':' + String(m).padStart(2, '0') + ' ' + ampm;
}

function toIsoDate(d) {
    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
}
