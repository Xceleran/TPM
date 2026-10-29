<%@ Page Title="Policy Holder Portal" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="PolicyHolderInquiry.aspx.cs" Inherits="TPM.PolicyHolderInquiry" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <link rel="stylesheet" href="Content/inquiry-portal.css" />

    <div class="container-fluid py-4">
        <div class="inquiry-page">
            <div class="inquiry-page-header">
                <h2 class="inquiry-page-title">Policy Holder Portal</h2>
                <p class="inquiry-page-subtitle">Ask about your appointment status, technician arrival, or service updates.</p>
            </div>

            <div class="inquiry-card">
                <div class="inquiry-card-header">
                    <span class="inquiry-card-icon">
                        <i class="fas fa-shield-alt"></i>
                    </span>
                    <div>
                        <h5>Service Inquiry Chat</h5>
                        <small class="text-muted">AI-assisted responses with policy-safe information</small>
                    </div>
                </div>
                <div class="inquiry-card-body">
                    <asp:HiddenField ID="hdnToken" runat="server" />
                    <asp:HiddenField ID="hdnHasContext" runat="server" Value="0" />

                    <div id="noContextBanner" class="alert alert-warning py-2 mb-3" style="display:none;">
                        <i class="fas fa-exclamation-triangle me-1"></i>
                        This chat is not linked to an appointment, so answers will be limited.
                        For real status updates, open with <code>?apptId=123</code> in the URL (from your service notification).
                    </div>

                    <div id="chatBox" class="inquiry-chat-box">
                        <div id="emptyState" class="inquiry-empty-state">
                            <i class="fas fa-comments"></i>
                            <p>Start a conversation by sending a message below.</p>
                        </div>
                    </div>

                    <div class="input-group inquiry-input-row">
                        <input type="text" id="txtMessage" class="form-control" placeholder="Ask about your appointment, status, or technician..." />
                        <button type="button" id="btnSend" class="btn btn-primary">
                            <i class="fas fa-paper-plane me-1"></i> Send
                        </button>
                    </div>

                    <p class="inquiry-hint">
                        <i class="fas fa-info-circle"></i>
                        Responses are scoped to your work order and do not expose internal staff notes.
                    </p>
                </div>
            </div>
        </div>
    </div>

    <script>
        $(function () {
            var token = $('#<%= hdnToken.ClientID %>').val();
            if ($('#<%= hdnHasContext.ClientID %>').val() !== '1') {
                $('#noContextBanner').show();
            }

            function escapeHtml(text) {
                return $('<div/>').text(text || '').html();
            }

            function formatTime(value) {
                if (!value) return '';
                var m = moment(value);
                return m.isValid() ? m.format('MMM D, h:mm A') : '';
            }

            function showEmptyState(show) {
                $('#emptyState').toggle(show);
            }

            function loadMessages() {
                if (!token) {
                    showEmptyState(true);
                    return;
                }

                $.ajax({
                    type: 'POST',
                    url: 'PolicyHolderInquiry.aspx/GetMessages',
                    contentType: 'application/json',
                    data: JSON.stringify({ token: token }),
                    success: function (r) {
                        var messages = (r.d && r.d.messages) ? r.d.messages : [];
                        $('#chatBox').empty();

                        if (!messages.length) {
                            showEmptyState(true);
                            $('#chatBox').append($('#emptyState'));
                            return;
                        }

                        showEmptyState(false);
                        messages.forEach(function (m) {
                            var isInbound = m.direction === 'Inbound';
                            var cls = isInbound ? 'inbound' : 'outbound';
                            var label = isInbound ? 'You' : 'Assistant';
                            var time = formatTime(m.createdDate);
                            $('#chatBox').append(
                                '<div class="inquiry-msg ' + cls + '">' +
                                    '<div class="inquiry-msg-bubble">' +
                                        escapeHtml(m.message) +
                                        '<span class="inquiry-msg-meta">' + label + (time ? ' &middot; ' + time : '') + '</span>' +
                                    '</div>' +
                                '</div>'
                            );
                        });

                        $('#chatBox').scrollTop($('#chatBox')[0].scrollHeight);
                    }
                });
            }

            $('#btnSend').on('click', function () {
                var msg = $.trim($('#txtMessage').val());
                if (!msg || !token) return;

                $('#btnSend').prop('disabled', true);
                $.ajax({
                    type: 'POST',
                    url: 'PolicyHolderInquiry.aspx/SendMessage',
                    contentType: 'application/json',
                    data: JSON.stringify({ token: token, message: msg }),
                    complete: function () { $('#btnSend').prop('disabled', false); },
                    success: function () {
                        $('#txtMessage').val('');
                        loadMessages();
                    }
                });
            });

            $('#txtMessage').on('keypress', function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    $('#btnSend').click();
                }
            });

            loadMessages();
        });
    </script>

</asp:Content>
