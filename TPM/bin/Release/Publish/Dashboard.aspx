<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="FSM.Dashboard" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="Content/dashboard.css?v=1">
    <link rel="stylesheet" href="Content/announcement.css">

    <div class="dash-wrap">

        <!-- Greeting -->
        <div class="d-flex justify-content-between align-items-start flex-wrap gap-2 mt-3 mb-1">
            <div>
                <div class="dash-greeting"><asp:Label ID="lblGreeting" runat="server" Text="Good evening" />, <asp:Label ID="lblUserName" runat="server" Text="Demo" /></div>
                <div class="dash-sub">Here's what's happening right now &mdash; <asp:Label ID="lblDateLine" runat="server" Text="" /></div>
            </div>
            <div class="d-flex align-items-center gap-2">
                <span class="last-updated"><asp:Label ID="lblUpdated" runat="server" Text="" /></span>
                <asp:LinkButton ID="btnRefresh" runat="server" CssClass="btn-refresh" OnClick="btnRefresh_Click">
                    <i class="bi bi-arrow-clockwise" id="refreshIcon"></i>Refresh
                </asp:LinkButton>
            </div>
        </div>

        <!-- ── SECTION 1: Operational Counts ── -->
        <div class="section-hdr mt-3"><i class="bi bi-bar-chart-fill"></i>Operational Overview</div>
        <div class="row g-3 mb-4">

            <!-- 1. Total Third Party Providers — blue -->
            <div class="col-12 col-md-6 col-xl-3">
                <a href="TpList.aspx" class="stat-card blue" id="card-providers">
                    <button class="stat-info-btn" type="button" tabindex="-1" onclick="return false;"><i class="bi bi-info-circle"></i></button>
                    <span class="stat-info-tip">Providers linked to your company (IsBusinessContact with a warranty company)</span>
                    <div class="stat-icon">
                        <img src="images/icons/thirdparty.svg" alt="Total Third Party Providers" />
                    </div>
                    <div>
                        <div class="stat-num" id="val-providers"><asp:Label ID="lblTotalProviders" runat="server" Text="0" /></div>
                        <div class="stat-label">Total Third Party Providers</div>
                    </div>
                    <span class="stat-go-link">View Providers <i class="bi bi-arrow-right"></i></span>
                </a>
            </div>

            <!-- 2. New Work Order Requests — lime -->
            <div class="col-12 col-md-6 col-xl-3">
                <a href="AppoinementList.aspx" class="stat-card lime" id="card-requests">
                    <button class="stat-info-btn" type="button" tabindex="-1" onclick="return false;"><i class="bi bi-info-circle"></i></button>
                    <span class="stat-info-tip">TPM work-order requests waiting for approval (IsApproved = 0)</span>
                    <div class="stat-icon">
                        <img src="images/icons/workorder.svg" alt="New Work Order Requests" />
                    </div>
                    <div>
                        <div class="stat-num" id="val-requests"><asp:Label ID="lblNewWorkOrders" runat="server" Text="0" /></div>
                        <div class="stat-label">New Work Order Requests</div>
                    </div>
                    <span class="stat-go-link">View Requests <i class="bi bi-arrow-right"></i></span>
                </a>
            </div>

            <!-- 3. Active Appointments — purple -->
            <div class="col-12 col-md-6 col-xl-3">
                <a href="Appointments.aspx" class="stat-card purple" id="card-active">
                    <button class="stat-info-btn" type="button" tabindex="-1" onclick="return false;"><i class="bi bi-info-circle"></i></button>
                    <span class="stat-info-tip">Approved appointments not closed, cancelled or deleted</span>
                    <div class="stat-icon">
                        <img src="images/icons/activeappointments.svg" alt="Active Appointments" />
                    </div>
                    <div>
                        <div class="stat-num" id="val-active"><asp:Label ID="lblActiveAppointments" runat="server" Text="0" /></div>
                        <div class="stat-label">Active Appointments</div>
                    </div>
                    <span class="stat-go-link">View Appointments <i class="bi bi-arrow-right"></i></span>
                </a>
            </div>

            <!-- 4. Active Assignments — orange -->
            <div class="col-12 col-md-6 col-xl-3">
                <a href="Dispatch.aspx" class="stat-card orange" id="card-dispatched">
                    <button class="stat-info-btn" type="button" tabindex="-1" onclick="return false;"><i class="bi bi-info-circle"></i></button>
                    <span class="stat-info-tip">Assignments whose appointment is still open</span>
                    <div class="stat-icon">
                        <img src="images/icons/assignment.svg" alt="Active Assignments" />
                    </div>
                    <div>
                        <div class="stat-num" id="val-dispatched"><asp:Label ID="lblActiveAssignments" runat="server" Text="0" /></div>
                        <div class="stat-label">Active Assignments</div>
                    </div>
                    <span class="stat-go-link">View Dispatch <i class="bi bi-arrow-right"></i></span>
                </a>
            </div>

        </div>

        <!-- ── SECTION 2: Announcements + Recent Activity ── -->
        <div class="row g-4 mb-4 alerts-row">

            <!-- Announcements (left) -->
            <div class="col-12 col-lg-6">
                <div class="section-hdr"><i class="bi bi-megaphone-fill" style="color:#6366f1;"></i>Announcements</div>
                <div class="announcements-panel" id="announcementsPanel">
                    <asp:Repeater ID="rptAnnouncements" runat="server">
                        <ItemTemplate>
                            <div class="ann-item">
                                <div class="ann-item-header">
                                    <span class="ann-item-dot"></span>
                                    <span class="ann-item-title"><%# Eval("Title") %></span>
                                </div>
                                <div class="ann-item-body tpm-ann-rich"><%# Eval("Announcement") %></div>
                                <div class="ann-item-date"><%# Eval("WhenText") %></div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoAnnouncements" runat="server" Visible="false" CssClass="ann-item ann-empty">
                        <i class="bi bi-megaphone me-2"></i>No announcements right now.
                    </asp:Panel>
                </div>
            </div>

            <!-- Recent Activity (right) -->
            <div class="col-12 col-lg-6">
                <div class="section-hdr d-flex justify-content-between align-items-center">
                    <span><i class="bi bi-clock-history" style="margin-right: 7px;"></i>Recent Activity</span>
                    <a href="Appointments.aspx" class="text-decoration-none" style="font-size: .78rem; color: #6366f1; font-weight: 600; text-transform: none; letter-spacing: 0;">
                        View all <i class="bi bi-arrow-right"></i>
                    </a>
                </div>
                <div class="activity-feed" id="activityFeed">
                    <asp:Repeater ID="rptRecent" runat="server">
                        <ItemTemplate>
                            <div class="activity-item">
                                <div class="activity-dot" style="background:#6366f1;"></div>
                                <div class="activity-text">
                                    <%# Eval("Headline") %>
                                </div>
                                <div class="activity-time"><%# Eval("WhenText") %></div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoRecent" runat="server" Visible="false" CssClass="activity-empty">
                        <i class="bi bi-calendar2-x me-2"></i>No recent activity today.
                    </asp:Panel>
                </div>
            </div>

        </div>

    </div>
</asp:Content>
