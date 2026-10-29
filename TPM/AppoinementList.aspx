<%@ Page Title="TPM Appointments" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="AppoinementList.aspx.cs" Inherits="TPM.AppoinementList" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <link rel="stylesheet" href="https://cdn.datatables.net/2.2.2/css/dataTables.dataTables.min.css">
    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/select/3.0.0/css/select.dataTables.min.css">

    <!-- Local Styles and Scripts -->
    <link rel="stylesheet" href="Content/customer.css?v=6">

    <style>
        .cust-action-btns {
            display: flex;
            gap: 10px;
        }

        .cust-action-btn {
            background: none;
            border: none;
            cursor: pointer;
            color: #444;
            font-size: 22px;
            padding: 6px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
        }

            .cust-action-btn:hover {
                background: #f0f0f0;
                color: #007bff;
            }

        .sms-btn {
            color: green;
            font-size: 22px;
        }

        .edit-btn svg {
            width: 22px;
            height: 22px;
        }

        /* Work order details card (rendered by AppointmentList.js) */
        .ad-card,
        .ahs-modal-content,
        .dup-modal-content {
            --ad-border: #e2e8f0;
            --ad-panel: #ffffff;
            --ad-muted: #64748b;
            --ad-text: #1e293b;
            --ad-accent: #007bff;
            background: #f8fafc;
            border-color: var(--ad-border);
            border-radius: 10px;
            padding: 14px 16px;
            gap: 14px;
            color: var(--ad-text);
            font-size: 13.5px;
        }

        [data-theme="dark"] .ad-card,
        [data-theme="dark"] .ahs-modal-content,
        [data-theme="dark"] .dup-modal-content {
            --ad-border: rgba(255, 255, 255, 0.14);
            --ad-panel: rgba(255, 255, 255, 0.04);
            --ad-muted: #94a3b8;
            --ad-text: #e2e8f0;
            --ad-accent: #60a5fa;
        }

        .ad-header {
            align-items: center;
            flex-wrap: nowrap;
            padding-bottom: 10px;
            border-bottom: 1px solid var(--ad-border);
        }

            .ad-header .cust-site-title-group {
                align-items: center;
                flex: 1;
                min-width: 0;
            }

            /* Long site names cut off with "..." instead of running into the badge or icons. */
            .ad-header .cust-site-title {
                font-size: 17px;
                line-height: 1.2;
                letter-spacing: 0.2px;
                min-width: 0;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }

        .ad-badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            flex-shrink: 0;
            font-size: 11px;
            font-weight: 600;
            line-height: 1;
            padding: 4px 10px 4px 8px;
            border-radius: 99px;
            text-transform: uppercase;
            letter-spacing: 0.4px;
        }

        .ad-header .cust-site-actions {
            flex-shrink: 0;
        }

        /* Dispatch ID stands out from the grey/green header bits on purpose. */
        .ad-dispatch {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            flex-shrink: 0;
            padding: 5px 10px;
            border: 1px solid #858585;
            border-radius: 99px;
            background: #d3d3d3;
            color: #0c0000;
            font-size: 12px;
            font-weight: 500;
            line-height: 1;
            white-space: nowrap;
        }

            .ad-dispatch strong {
                font-weight: 700;
                letter-spacing: 0.3px;
            }

            .ad-dispatch i {
                font-size: 10px;
            }

        [data-theme="dark"] .ad-dispatch {
            border-color: rgba(252, 211, 77, 0.35);
            background: rgba(245, 158, 11, 0.16);
            color: #fcd34d;
        }

        .ad-badge-dot {
            width: 7px;
            height: 7px;
            border-radius: 50%;
            background: currentColor;
        }

        .ad-badge.active .ad-badge-dot {
            background: #22c55e;
        }

        /* Header address: one line, cut off with "..." so it never runs into the name. */
        .cust-details-header div#contact .ci-value {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .cust-details-header div#contact .ci-item {
            min-width: 0;
            gap: 8px;
        }

        .cust-details-header div#contact .ci-tag {
            flex-shrink: 0;
            padding: 3px 8px;
            border-radius: 99px;
            background: #eef2f7;
            color: #64748b;
            font-size: 10.5px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.4px;
        }

        [data-theme="dark"] .cust-details-header div#contact .ci-tag {
            background: rgba(255, 255, 255, 0.08);
            color: #94a3b8;
        }

        .ad-badge.active {
            background: #dcfce7;
            color: #166534;
        }

        .ad-badge.inactive {
            background: #e2e8f0;
            color: #475569;
        }

        [data-theme="dark"] .ad-badge.active {
            background: rgba(34, 197, 94, 0.18);
            color: #86efac;
        }

        [data-theme="dark"] .ad-badge.inactive {
            background: rgba(148, 163, 184, 0.18);
            color: #cbd5e1;
        }

        .ad-toolbar {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-end;
            gap: 12px 20px;
            padding: 12px 14px;
            background: var(--ad-panel);
            border: 1px solid var(--ad-border);
            border-radius: 8px;
        }

        .ad-field {
            display: flex;
            flex-direction: column;
            gap: 4px;
            margin: 0;
            min-width: 160px;
        }

        .ad-field-label {
            font-size: 11px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: var(--ad-muted);
        }

        .ad-field .form-select {
            min-width: 160px;
        }

        .ad-field-static {
            display: flex;
            align-items: center;
            gap: 6px;
            min-height: 31px;
            font-weight: 500;
        }

            .ad-field-static i {
                color: var(--ad-muted);
            }

        /* Name and customer address share one line in the details header. */
        .cust-details-container .cust-details-header {
            flex-direction: row;
            align-items: center;
            gap: 12px;
        }

        .cust-details-container .cust-details-left {
            display: flex;
            align-items: center;
            gap: 14px;
            flex: 1;
            min-width: 0;
        }

            .cust-details-container .cust-details-left .cust-details-title {
                flex-shrink: 0;
                max-width: 50%;
                margin: 0;
                line-height: 1.2;
            }

        /* Rows whose items are all empty are hidden, so they don't push the address off center. */
        .cust-details-header div#contact .ci-row {
            margin: 0;
        }

            .cust-details-header div#contact .ci-row:not(:has(.ci-item:not(.is-empty))) {
                display: none;
            }

        .cust-details-header div#contact .ci-value {
            line-height: 1.2;
            font-size: 16px;
        }

        .cust-details-container .cust-details-header div#contact {
            flex: 1;
            min-width: 0;
            margin: 0;
            padding: 0;
            background: none;
            border: none;
            box-shadow: none;
        }

        /* Loading overlay for the details panel, styled like the table's own "Loading..." dots. */
        .cust-details-container { position: relative; }

        .details-loading {
            position: absolute;
            inset: 0;
            z-index: 20;
            display: none;
            align-items: flex-start;
            justify-content: center;
            padding-top: 120px;
            background: rgba(255, 255, 255, 0.75);
            border-radius: inherit;
        }

            .details-loading.is-active { display: flex; }

        [data-theme="dark"] .details-loading { background: rgba(15, 23, 42, 0.7); }

        .details-loading-box {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
            padding: 14px 26px;
            border-radius: 8px;
            background: #fff;
            box-shadow: 0 4px 16px rgba(15, 23, 42, 0.12);
        }

        [data-theme="dark"] .details-loading-box { background: #1e293b; }

        .details-loading-dots { display: flex; gap: 6px; }

            .details-loading-dots span {
                width: 9px;
                height: 9px;
                border-radius: 50%;
                background: #0d6efd;
                animation: details-loading-bounce 1s infinite ease-in-out;
            }

                .details-loading-dots span:nth-child(2) { animation-delay: 0.15s; }
                .details-loading-dots span:nth-child(3) { animation-delay: 0.3s; }

        .details-loading-text {
            font-size: 12.5px;
            color: #64748b;
        }

        @keyframes details-loading-bounce {
            0%, 80%, 100% { transform: scale(0.6); opacity: 0.5; }
            40% { transform: scale(1); opacity: 1; }
        }

        /* Appointment fields and site address side by side. */
        .ad-top {
            display: grid;
            grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
            gap: 14px;
            align-items: stretch;
        }

            .ad-top .ad-toolbar {
                flex-direction: column;
                flex-wrap: nowrap;
                align-items: stretch;
                gap: 10px;
            }

            .ad-top .ad-field {
                flex-direction: row;
                align-items: center;
                justify-content: space-between;
                gap: 10px;
                min-width: 0;
            }

                .ad-top .ad-field .form-select {
                    min-width: 0;
                    width: 150px;
                }

        .ad-address .ad-panel-title {
            justify-content: space-between;
            margin-bottom: 6px;
        }

            .ad-address .ad-panel-title .ad-link {
                margin: 0;
                text-transform: none;
                letter-spacing: 0;
                font-weight: 500;
            }

        .ad-address-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            column-gap: 16px;
        }

            .ad-address-grid .ad-address-wide {
                grid-column: 1 / -1;
            }

            .ad-address-grid .ad-row {
                grid-template-columns: 18px 46px 1fr;
            }

                .ad-address-grid .ad-row:last-child {
                    border-bottom: 1px dashed var(--ad-border);
                }

        .ad-address-warn {
            margin-top: 8px;
            padding: 6px 10px;
            border-left: 3px solid #f59e0b;
            border-radius: 4px;
            background: #fffbeb;
            color: #92400e;
            font-size: 12.5px;
            font-weight: 500;
        }

        [data-theme="dark"] .ad-address-warn {
            background: rgba(245, 158, 11, 0.12);
            color: #fcd34d;
        }

        @media (max-width: 1100px) {
            .ad-top {
                grid-template-columns: 1fr;
            }
        }

        @media (max-width: 700px) {
            .cust-details-container .cust-details-header,
            .cust-details-container .cust-details-left {
                flex-wrap: wrap;
            }
        }

        .ahs-modal-content {
            max-width: 560px;
            width: 100%;
            color: var(--ad-text);
        }

        .ahs-subtitle {
            margin: -4px 0 16px;
            color: var(--ad-muted);
            font-size: 13px;
        }

            .ahs-subtitle strong {
                color: var(--ad-text);
            }

        .ahs-list {
            border: 1px solid var(--ad-border);
            border-radius: 12px;
            max-height: 60vh;
            overflow-y: auto;
        }

        .ahs-row {
            display: flex;
            align-items: center;
            gap: 14px;
            min-height: 58px;
            padding: 10px 16px;
            border-bottom: 1px solid var(--ad-border);
            transition: background 0.15s;
        }

            .ahs-row:last-child {
                border-bottom: none;
            }

            .ahs-row:hover {
                background: rgba(100, 116, 139, 0.06);
            }

        .ahs-row-text {
            flex: 1;
            min-width: 0;
        }

        .ahs-row-title {
            font-size: 14px;
            font-weight: 600;
            line-height: 1.3;
        }

        .ahs-row-meta {
            margin-top: 2px;
            font-size: 12px;
            color: var(--ad-muted);
        }

            .ahs-row-meta.is-saved {
                color: #16a34a;
            }

        .ahs-date {
            width: 150px;
            flex-shrink: 0;
            border-radius: 8px;
            font-size: 13px;
        }

        .ahs-submit-btn {
            flex-shrink: 0;
            min-width: 84px;
            height: 34px;
            padding: 0 14px;
            border: none;
            border-radius: 8px;
            background: var(--ad-accent);
            color: #fff;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
        }

            .ahs-submit-btn:hover:not(:disabled) {
                filter: brightness(1.08);
            }

            .ahs-submit-btn:disabled {
                opacity: 0.6;
                cursor: wait;
            }

        .ahs-row-danger .ahs-row-title {
            color: #dc2626;
        }

        .ahs-row-danger .ahs-submit-btn {
            background: #dc2626;
        }

        .ahs-footer {
            display: flex;
            justify-content: flex-end;
            margin-top: 18px;
        }

        .ahs-btn {
            height: 38px;
            padding: 0 18px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
        }

        .ahs-btn-secondary {
            border: 1px solid var(--ad-border);
            background: transparent;
            color: var(--ad-text);
        }


        /* Original message modal: tinted header, scrolling body */
        .msg-modal-content {
            display: flex;
            flex-direction: column;
            padding: 0;
            overflow: hidden;
        }

        .msg-header {
            display: flex;
            align-items: center;
            gap: 14px;
            padding: 16px 20px;
            background: #eef4ff;
            border-bottom: 1px solid #dbe5f5;
        }

        [data-theme="dark"] .msg-header {
            background: rgba(96, 165, 250, 0.1);
            border-bottom-color: rgba(255, 255, 255, 0.1);
        }

        .msg-header-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
            width: 40px;
            height: 40px;
            border-radius: 10px;
            background: #fff;
            color: #007bff;
            font-size: 17px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
        }

        [data-theme="dark"] .msg-header-icon {
            background: rgba(255, 255, 255, 0.08);
            color: #60a5fa;
        }

        .msg-header-text {
            flex: 1;
            min-width: 0;
        }

        .MSg-modal-title {
            margin: 0;
            font-size: 18px;
            font-weight: 700;
            line-height: 1.3;
        }

        .msg-subject {
            margin: 2px 0 0;
            font-size: 13px;
            color: #64748b;
            overflow-wrap: anywhere;
        }

            .msg-subject:empty {
                display: none;
            }

        [data-theme="dark"] .msg-subject {
            color: #94a3b8;
        }

        .msg-close {
            position: static;
            align-self: flex-start;
            flex-shrink: 0;
            width: 32px;
            height: 32px;
            border-radius: 8px;
            font-size: 22px;
        }

            .msg-close:hover {
                background: rgba(0, 0, 0, 0.06);
            }

        .msg-body {
            flex: 1;
            min-height: 120px;
            padding: 20px;
            overflow: auto;
        }

        .msg-footer {
            display: flex;
            justify-content: flex-end;
            padding: 12px 20px;
            border-top: 1px solid #e2e8f0;
        }

        [data-theme="dark"] .msg-footer {
            border-top-color: rgba(255, 255, 255, 0.1);
        }

        /* The email HTML brings its own fixed widths; keep it inside the popup. */
        .msg-body img,
        .msg-body table {
            max-width: 100%;
        }

        /* Duplicate customer check modal */
        .dup-modal-content {
            width: 1200px;
            max-width: 95%;
            color: var(--ad-text);
        }

        /* Two lanes: search on the left, results on the right. */
        .dup-layout {
            display: grid;
            grid-template-columns: 280px minmax(0, 1fr);
            gap: 18px;
            align-items: start;
        }

        .dup-side {
            padding: 14px;
            border: 1px solid var(--ad-border);
            border-radius: 10px;
            background: #f8fafc;
        }

        [data-theme="dark"] .dup-side {
            background: rgba(255, 255, 255, 0.04);
        }

        .dup-side-title {
            display: flex;
            align-items: center;
            gap: 8px;
            margin: 0 0 12px;
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.6px;
            color: var(--ad-muted);
        }

            .dup-side-title i {
                color: var(--ad-accent);
            }

        .dup-search {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .dup-field {
            display: flex;
            flex-direction: column;
            gap: 4px;
            margin: 0;
        }

            .dup-field > span {
                font-size: 11px;
                font-weight: 600;
                text-transform: uppercase;
                letter-spacing: 0.5px;
                color: var(--ad-muted);
            }

        .dup-search-btn,
        .dup-merge-btn {
            height: 34px;
            padding: 0 16px;
            border: none;
            border-radius: 8px;
            color: #fff;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
        }

        .dup-search-btn {
            margin-top: 4px;
            background: var(--ad-accent);
        }

        .dup-merge-btn {
            background: #16a34a;
        }

            .dup-search-btn:hover:not(:disabled),
            .dup-merge-btn:hover:not(:disabled) {
                filter: brightness(1.08);
            }

            .dup-search-btn:disabled,
            .dup-merge-btn:disabled {
                opacity: 0.6;
                cursor: wait;
            }

        .dup-help {
            margin: 14px 0 0;
            font-size: 12.5px;
            line-height: 1.5;
            color: var(--ad-muted);
        }

            .dup-help strong {
                color: var(--ad-text);
            }

        .dup-table-wrap {
            max-height: 60vh;
            min-height: 200px;
            overflow: auto;
            border: 1px solid var(--ad-border);
            border-radius: 10px;
        }

        .dup-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
        }

            .dup-table th {
                position: sticky;
                top: 0;
                z-index: 1;
                padding: 9px 10px;
                background: #f8fafc;
                border-bottom: 1px solid var(--ad-border);
                color: var(--ad-muted);
                font-size: 11.5px;
                font-weight: 600;
                text-transform: uppercase;
                letter-spacing: 0.4px;
                text-align: left;
                white-space: nowrap;
            }

            .dup-table td {
                padding: 8px 10px;
                border-bottom: 1px solid var(--ad-border);
                word-break: break-word;
            }

            .dup-table tbody tr:last-child td {
                border-bottom: none;
            }

            .dup-table tbody tr:hover td {
                background: rgba(100, 116, 139, 0.06);
            }

            .dup-table tr.is-primary td {
                background: rgba(0, 123, 255, 0.07);
            }

            .dup-table tr.is-duplicate td {
                background: rgba(245, 158, 11, 0.08);
            }

            .dup-table .dup-pick {
                width: 84px;
                text-align: center;
            }

                .dup-table .dup-pick input {
                    width: 16px;
                    height: 16px;
                    accent-color: var(--ad-accent);
                    cursor: pointer;
                }

            .dup-table .dup-message td {
                padding: 24px 10px;
                text-align: center;
                color: var(--ad-muted);
            }

        [data-theme="dark"] .dup-table th {
            background: rgba(255, 255, 255, 0.06);
        }

        .dup-current-tag {
            margin-left: 6px;
            padding: 1px 7px;
            border-radius: 99px;
            background: #eff6ff;
            color: #1d4ed8;
            font-size: 10.5px;
            font-weight: 600;
            white-space: nowrap;
        }

        .dup-footer {
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 12px;
            margin-top: 16px;
        }

        .dup-delete {
            position: relative;
            display: flex;
            align-items: center;
            gap: 10px;
            margin: 0;
            font-size: 13.5px;
            font-weight: 500;
            cursor: pointer;
            user-select: none;
        }

            /* The real checkbox stays for the form/JS; the switch is drawn by .dup-switch. */
            .dup-delete input {
                position: absolute;
                opacity: 0;
                width: 0;
                height: 0;
            }

        .dup-switch {
            position: relative;
            flex-shrink: 0;
            width: 40px;
            height: 22px;
            border-radius: 99px;
            background: #cbd5e1;
            transition: background 0.2s;
        }

            .dup-switch::before {
                content: "";
                position: absolute;
                top: 3px;
                left: 3px;
                width: 16px;
                height: 16px;
                border-radius: 50%;
                background: #fff;
                box-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
                transition: transform 0.2s;
            }

        .dup-delete input:checked + .dup-switch {
            background: #dc2626;
        }

            .dup-delete input:checked + .dup-switch::before {
                transform: translateX(18px);
            }

        .dup-delete input:focus-visible + .dup-switch {
            box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.3);
        }

        [data-theme="dark"] .dup-switch {
            background: #475569;
        }

        .dup-footer-btns {
            display: flex;
            gap: 8px;
        }

        .ad-panels {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 14px;
        }

        .ad-panel {
            background: var(--ad-panel);
            border: 1px solid var(--ad-border);
            border-radius: 8px;
            padding: 12px 14px;
        }

        .ad-panel-title {
            display: flex;
            align-items: center;
            gap: 8px;
            margin: 0 0 10px;
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.6px;
            color: var(--ad-muted);
        }

            .ad-panel-title i {
                color: var(--ad-accent);
            }

        .ad-link {
            display: inline-block;
            margin-top: 8px;
            font-size: 12.5px;
            color: var(--ad-accent);
            text-decoration: none;
        }

            .ad-link:hover {
                text-decoration: underline;
            }

        .ad-row {
            display: grid;
            grid-template-columns: 20px 70px 1fr;
            align-items: baseline;
            gap: 6px;
            padding: 5px 0;
            border-bottom: 1px dashed var(--ad-border);
        }

            .ad-row:last-child {
                border-bottom: none;
            }

        .ad-row-icon {
            color: var(--ad-muted);
        }

        .ad-row-label {
            color: var(--ad-muted);
            font-size: 12.5px;
        }

        .ad-row-value {
            word-break: break-word;
            font-weight: 500;
        }

            .ad-row-value a {
                color: var(--ad-accent);
                text-decoration: none;
            }

                .ad-row-value a:hover {
                    text-decoration: underline;
                }

        .ad-empty {
            color: var(--ad-muted);
            font-style: italic;
            font-weight: 400;
        }

        .ad-notes-body {
            max-height: 460px;
            overflow-y: auto;
            padding-right: 6px;
            line-height: 1.55;
        }

        .ad-notes-line {
            margin: 0;
        }

        .ad-notes-important {
            margin: 10px 0;
            padding: 8px 12px;
            display: flex;
            flex-direction: column;
            gap: 4px;
            background: #f1f5f9;
            border-left: 3px solid var(--ad-accent);
            border-radius: 4px;
            font-size: 12.5px;
            font-weight: 600;
            letter-spacing: 0.2px;
            line-height: 1.5;
        }

        [data-theme="dark"] .ad-notes-important {
            background: rgba(255, 255, 255, 0.06);
        }

        .ad-notes-gap {
            height: 10px;
        }

        .ad-notes-heading {
            margin: 10px 0 6px;
            font-size: 13px;
            font-weight: 700;
            color: var(--ad-text);
        }

        .ad-notes-list {
            margin: 8px 0 6px;
            padding: 0;
            list-style: none;
            /* Columns rather than a grid, so a wrapped item doesn't pad out its whole row. */
            columns: 240px;
            column-gap: 10px;
        }

            .ad-notes-list li {
                /* No wrapping: long text wraps inside its own span, next to the check. */
                display: flex;
                align-items: flex-start;
                gap: 8px;
                margin-bottom: 8px;
                padding: 7px 10px;
                background: var(--ad-panel);
                border: 1px solid var(--ad-border);
                border-radius: 6px;
                font-size: 13.5px;
                font-weight: 500;
                line-height: 1.35;
                color: var(--ad-text);
                break-inside: avoid;
            }

                .ad-notes-list li > i {
                    flex-shrink: 0;
                    margin-top: 3px;
                    color: #16a34a;
                    font-size: 13px;
                }

                .ad-notes-list li > span:not(.ad-notes-tag) {
                    flex: 1;
                    min-width: 0;
                }

        [data-theme="dark"] .ad-notes-list li > i {
            color: #4ade80;
        }

        .ad-notes-tag {
            flex-shrink: 0;
            align-self: center;
            padding: 1px 8px;
            border-radius: 99px;
            background: #eff6ff;
            color: #1d4ed8;
            font-size: 11px;
            font-weight: 600;
            white-space: nowrap;
        }

        [data-theme="dark"] .ad-notes-tag {
            background: rgba(96, 165, 250, 0.15);
            color: #93c5fd;
        }

        /* Fixed label column so every block of pairs in the notes lines up. */
        .ad-notes-pairs {
            display: grid;
            grid-template-columns: 130px minmax(0, 1fr);
            gap: 3px 14px;
            margin: 0;
        }

            .ad-notes-pairs dd.ad-notes-cont {
                margin-top: -2px;
                color: var(--ad-muted);
            }

            .ad-notes-pairs dt {
                color: var(--ad-muted);
                font-weight: 500;
            }

            .ad-notes-pairs dd {
                margin: 0;
                font-weight: 500;
                word-break: break-word;
            }

        .ad-notes-callout {
            margin: 4px 0;
            padding: 6px 10px;
            border-left: 3px solid #f59e0b;
            background: #fffbeb;
            color: #92400e;
            border-radius: 4px;
            font-weight: 500;
        }

        [data-theme="dark"] .ad-notes-callout {
            background: rgba(245, 158, 11, 0.12);
            color: #fcd34d;
        }

        .ad-notes-divider {
            display: flex;
            align-items: center;
            gap: 10px;
            margin: 8px 0;
            color: var(--ad-muted);
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.6px;
        }

            .ad-notes-divider::before,
            .ad-notes-divider::after {
                content: "";
                flex: 1;
                height: 1px;
                background: var(--ad-border);
            }

        /* Stack the duplicate popups' lanes once the table would get too narrow. */
        @media (max-width: 900px) {
            .dup-layout {
                grid-template-columns: 1fr;
            }

            .dup-table-wrap {
                min-height: 0;
            }
        }

        @media (max-width: 640px) {
            .ad-field, .ad-field .form-select {
                min-width: 0;
                width: 100%;
            }

            .dup-footer-btns {
                width: 100%;
            }

                .dup-footer-btns > * {
                    flex: 1;
                }

            .ahs-row {
                flex-wrap: wrap;
            }

            .ahs-date {
                order: 3;
                width: 100%;
            }

            .ahs-date {
                flex: 1;
                width: auto;
            }

            .ahs-footer .ahs-btn {
                width: 100%;
            }

            .ad-notes-pairs {
                grid-template-columns: 1fr;
            }

                .ad-notes-pairs dd {
                    margin-bottom: 4px;
                }

                .ad-notes-pairs dt:empty {
                    display: none;
                }
        }
    </style>


    <div class="cust-page-container">
        <!-- Page Header -->
        <header class="cust-header mt-0 mb-0">
            <div class="cec-btn">
                <a id="LaunchCecButton" runat="server" href="#" class="custom-launch-btn" role="button" target="_blank">
                    <span>
                        <span>CEC Customer</span>
                        <span aria-hidden="true">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 19.5l15-15m0 0H8.25m11.25 0v11.25" />
                            </svg>
                        </span>
                    </span>
                </a>
            </div>
        </header>


        <!-- Customer Section -->
        <section class="cust-section">
            <!-- Customer List -->
            <div class="cust-list-container">
                <div class="cust-list-toolbar d-flex justify-content-between align-items-center flex-wrap gap-2">
                    <div class="d-flex align-items-center gap-3">
                        <div class="cust-list-filter">
                            <label for="statusFilter" class="form-label"><i class="fas fa-filter"></i>Status</label>
                            <%-- Sp_GetAppointmnetData only branches on 'accept' and 'pending'; an
                                 appointment's status here is IsApproved, so there is no Cancel to filter by. --%>
                            <asp:DropDownList ID="statusFilter" ClientIDMode="Static" runat="server" CssClass="form-select w-auto">
                                <asp:ListItem Text="All Statuses" Value="ALL"></asp:ListItem>
                                <asp:ListItem Text="Accept" Value="Accept"></asp:ListItem>
                                <asp:ListItem Text="Pending" Value="Pending"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <%-- <div class="toggle-switch">
                        <label for="hideNA" class="form-label" style="font-size: 0.8rem;">Hide Customer with No Appointments:</label>
                        <input type="checkbox" id="hideNA" checked />
                    </div>--%>
                </div>

                <table id="AppointmentListTable" class="display" style="width: 100%">
                    <thead>
                        <tr>
                            <th>Source</th>
                            <th>Site Name</th>
                            <th>Date</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                </table>
            </div>
            <!-- Customer Details -->
            <div class="cust-details-container">
                <div class="cust-details-header">
                    <div class="cust-details-left">

                        <h2 class="cust-details-title" id="customerName">Select a Customer</h2>
                        <!-- Customer address, beside the name -->
                        <div id="contact">
                            <!-- Row 1: Address -->
                            <div class="ci-row">
                                <div class="ci-item" id="customerAddress-container" title="Customer's address on file (the job site is in the card below)">
                                    <i class="ci-icon fas fa-map-marker-alt"></i>
                                    <span class="ci-tag">Customer</span>
                                    <span class="ci-value" id="customerAddress">-</span>
                                </div>
                            </div>
                            <!-- Row 2: Phone and Mobile -->
                            <div class="ci-row">
                                <!-- Phone -->
                                <div class="ci-item" id="customerPhone-container">
                                    <i class="ci-icon fas fa-phone-alt"></i>
                                    <span class="ci-value" id="customerPhone">-</span>
                                </div>
                                <!-- Mobile -->
                                <div class="ci-item" id="customerMobile-container">
                                    <i class="ci-icon fas fa-mobile-alt"></i>
                                    <span class="ci-value" id="customerMobile">-</span>
                                </div>
                            </div>
                            <!-- Row 3: Email & Job Title -->
                            <div class="ci-row">
                                <div class="ci-item" id="customerEmail-container">
                                    <i class="ci-icon fas fa-envelope"></i>
                                    <span class="ci-value" id="customerEmail">-</span>
                                </div>
                                <div class="ci-item" id="customerJobTitle-container">
                                    <i class="ci-icon fas fa-briefcase"></i>
                                    <span class="ci-value" id="customerJobTitle">-</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="cust-details-actions">
                        <button type="button" class="cust-table-edit-btn" title="Edit Site" id="editCustomerBtn">
                            <i class="fa-solid fa-user-pen"></i>
                        </button>
                    </div>
                </div>

                <%-- Shown over the details panel while a clicked row's data loads. --%>
                <div class="details-loading" id="detailsLoading" aria-hidden="true">
                    <div class="details-loading-box">
                        <div class="details-loading-dots"><span></span><span></span><span></span></div>
                        <div class="details-loading-text">Loading...</div>
                    </div>
                </div>

                <div class="cust-details-content">
                    <div id="sites">
                        <div class="sites-header">
                        </div>
                        <div class="sites-list">
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>

    <%-- Duplicate customer check, same flow as Jobs-Scheduler's customerDetail.aspx:
         search, pick the Primary to keep, tick Duplicates, optionally a Secondary
         contact, then merge. Front end only: it calls FindDuplicateCustomers and
         ReassignCustomerCompany on this page, which the backend still has to add.
         No HTML5 "required" on the inputs: the modal sits inside the page's server form. --%>
    <div class="cust-modal" id="mdl_CheckDuplicate">
        <div class="cust-modal-content dup-modal-content">
            <button type="button" class="cust-modal-close" id="close_mdl_CheckDuplicate">×</button>
            <h2 class="cust-modal-title">Check for Duplicate Customers</h2>

            <div class="dup-layout">
                <aside class="dup-side">
                    <h4 class="dup-side-title"><i class="fas fa-user"></i>Customer</h4>
                    <div class="dup-search">
                        <label class="dup-field"><span>First Name</span><input type="text" id="dupFirstName" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Last Name</span><input type="text" id="dupLastName" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Address</span><input type="text" id="dupAddress" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Phone</span><input type="text" id="dupPhone" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Mobile</span><input type="text" id="dupMobile" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>E-Mail</span><input type="text" id="dupEmail" class="form-control form-control-sm" /></label>
                        <button type="button" class="dup-search-btn" id="btn_SearchDuplicates"><i class="fas fa-search"></i>Search</button>
                    </div>

                    <p class="dup-help">
                        Pick the customer to <strong>keep (Primary)</strong>, then tick the <strong>Duplicate</strong> records to merge into it.
                Their appointments and invoices move to the Primary. <strong>Secondary</strong> (optional) copies that person in as the Primary's second contact.
           
                    </p>
                </aside>

                <div class="dup-table-wrap">
                    <table class="dup-table" id="DuplicateCustomerTable">
                        <thead>
                            <tr>
                                <th class="dup-pick">Primary</th>
                                <th class="dup-pick">Duplicate</th>
                                <th class="dup-pick">Secondary</th>
                                <th>First Name</th>
                                <th>Last Name</th>
                                <th>Address</th>
                                <th>City</th>
                                <th>Phone</th>
                                <th>Email</th>
                                <th>Mobile</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>

            <div class="dup-footer">
                <label class="dup-delete">
                    <input type="checkbox" id="chk_DeleteMerged" role="switch" checked />
                    <span class="dup-switch" aria-hidden="true"></span>
                    <span>Delete the old (merged) accounts</span>
                </label>
                <div class="dup-footer-btns">
                    <button type="button" class="cust-modal-cancel" id="clossadaseAddSite">Cancel</button>
                    <button type="button" class="dup-merge-btn" id="btn_MergeDuplicates">Merge Selected Duplicates</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Duplicate site check: same flow as the customer check above, for sites.
         Front end only: it calls FindDuplicateSites and MergeCustomerSites on this
         page, which the backend still has to add. --%>
    <div class="cust-modal" id="mdl_CheckSiteDuplicate">
        <div class="cust-modal-content dup-modal-content">
            <button type="button" class="cust-modal-close" id="close_mdl_CheckSiteDuplicate">×</button>
            <h2 class="cust-modal-title">Check for Duplicate Sites</h2>

            <div class="dup-layout">
                <aside class="dup-side">
                    <h4 class="dup-side-title"><i class="fas fa-map-marker-alt"></i>Site</h4>
                    <div class="dup-search">
                        <label class="dup-field"><span>Site Name</span><input type="text" id="dupSiteName" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Address</span><input type="text" id="dupSiteAddress" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>City</span><input type="text" id="dupSiteCity" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Zip</span><input type="text" id="dupSiteZip" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>Phone</span><input type="text" id="dupSitePhone" class="form-control form-control-sm" /></label>
                        <label class="dup-field"><span>E-Mail</span><input type="text" id="dupSiteEmail" class="form-control form-control-sm" /></label>
                        <button type="button" class="dup-search-btn" id="btn_SearchSiteDuplicates"><i class="fas fa-search"></i>Search</button>
                    </div>

                    <p class="dup-help">
                        Pick the site to <strong>keep (Primary)</strong>, then tick the <strong>Duplicate</strong> sites to merge into it.
                Their appointments move to the Primary site.
           
                    </p>
                </aside>

                <div class="dup-table-wrap">
                    <table class="dup-table" id="DuplicateSiteTable">
                        <thead>
                            <tr>
                                <th class="dup-pick">Primary</th>
                                <th class="dup-pick">Duplicate</th>
                                <th>Site Name</th>
                                <th>Contact</th>
                                <th>Address</th>
                                <th>City</th>
                                <th>State</th>
                                <th>Zip</th>
                                <th>Phone</th>
                                <th>Email</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>

            <div class="dup-footer">
                <label class="dup-delete">
                    <input type="checkbox" id="chk_DeleteMergedSites" role="switch" checked />
                    <span class="dup-switch" aria-hidden="true"></span>
                    <span>Delete the old (merged) sites</span>
                </label>
                <div class="dup-footer-btns">
                    <button type="button" class="cust-modal-cancel" id="btn_CloseSiteDuplicate">Cancel</button>
                    <button type="button" class="dup-merge-btn" id="btn_MergeSiteDuplicates">Merge Selected Sites</button>
                </div>
            </div>
        </div>
    </div>

    <%-- AHS Status: one row per AHS status with a date and its own Submit, which
         asks Yes/No and then calls the backend. Rows are built by openAhsStatus()
         in AppointmentList.js. --%>
    <div class="cust-modal" id="mdl_AhsStatus">
        <div class="cust-modal-content ahs-modal-content">
            <button type="button" class="cust-modal-close" id="close_mdl_AhsStatus">×</button>
            <h2 class="cust-modal-title">AHS Status</h2>
            <p class="ahs-subtitle" id="ahsStatusSubtitle"></p>

            <div class="ahs-list" id="AhsStatusList"></div>

            <div class="ahs-footer">
                <button type="button" class="ahs-btn ahs-btn-secondary" id="btn_CloseAhsStatus">Close</button>
            </div>
        </div>
    </div>

    <div class="cust-modal" id="MsgViewModal">
        <div class="cust-modal-content msg-modal-content">
            <div class="msg-header">
                <div class="msg-header-icon"><i class="fa fa-envelope-open-text"></i></div>
                <div class="msg-header-text">
                    <h2 class="MSg-modal-title">Message</h2>
                    <p class="msg-subject" id="msgSubject"></p>
                </div>
                <button type="button" class="cust-modal-close msg-close" id="closeMsgView" aria-label="Close">×</button>
            </div>
            <div class="msg-body">
                <div class="text-center ajax-loader">
                    <div class="spinner-border" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
                <div id="div_Msg"></div>
            </div>
            <div class="msg-footer">
                <button type="button" class="cust-modal-cancel" id="btn_CloseMsgView">Close</button>
            </div>
        </div>
    </div>

    <%-- The Add/Edit Customer modals were removed: nothing on this page opened them,
         and the grid carries CustomerName rather than FirstName/LastName, so saving
         from here posted blank names to UpdateCustomer. Customer records are edited
         on Customer.aspx / CustomerDetails.aspx. --%>
    <!-- SMS Modal -->
    <div class="modal fade" id="modalSendSMS" tabindex="-1" role="dialog" aria-labelledby="modalSendSMS"
        aria-hidden="true">
        <div class="modal-dialog modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="smsModalLongTitle">Send SMS</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="txtMobile" class="form-label mb-0">Phone/Mobile Number</label>
                            <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" />
                            <asp:HiddenField ID="txtCustomerId" runat="server" />
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="txtSMS" class="form-label mb-0">Text</label>
                            <asp:TextBox ID="txtSMS" runat="server" TextMode="MultiLine" Rows="5"
                                CssClass="form-control" />
                        </div>
                    </div>
                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-info" onclick="OpenSMSHistoryFromModal()"
                        title="View SMS History">
                        <i class="fas fa-history me-1"></i>View History
                       
                    </button>
                    <div>
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                            onclick="CloseSMSPopup()">
                            Close</button>
                        <asp:Button ID="btnSendSms" ClientIDMode="Static" runat="server" Text="Send SMS"
                            CssClass="btn btn-success" OnClick="btnSendSMS_Click" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- MMS Modal -->
    <div class="modal fade" id="modalSendMMS" tabindex="-1" role="dialog" aria-labelledby="modalSendMMS"
        aria-hidden="true">
        <div class="modal-dialog modal-dialog-scrollable" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="mmsModalLongTitle">Send MMS</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="txtCustMob" class="form-label mb-0">Phone/Mobile Number</label>
                            <asp:TextBox ID="txtCustMob" runat="server" CssClass="form-control" />
                            <asp:HiddenField ID="txtCustId" runat="server" />
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-12 mt-0">
                            <label for="txtMMSBody" class="form-label mb-0">MMS Body</label>
                            <asp:TextBox ID="txtMMSBody" runat="server" TextMode="MultiLine" Rows="4"
                                CssClass="form-control" />
                        </div>
                    </div>
                    <div class="row mt-3">
                        <div class="col-12 mt-0">
                            <label for="fuAttachment" class="form-label mb-0">Attachment</label>
                            <asp:FileUpload ID="fuAttachment" runat="server" CssClass="form-control" />
                            <p id="fileHint" class="text-muted small mb-0">
                                Allowed file types: .pdf, .jpg, .jpeg,
                                    .png (Max size: 20MB)
                            </p>
                            <span id="fileError" class="text-danger small"></span>
                        </div>
                    </div>
                </div>
                <div class="modal-footer justify-content-between">
                    <button type="button" class="btn btn-info" onclick="OpenSMSHistoryFromModal()"
                        title="View SMS History">
                        <i class="fas fa-history me-1"></i>View History
                       
                    </button>
                    <div>
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"
                            onclick="CloseMMSPopup()">
                            Close</button>
                        <asp:Button ID="btnSendMMS" ClientIDMode="Static" runat="server" Text="Send MMS"
                            CssClass="btn btn-success" OnClick="btnSendMMS_Click" />
                    </div>
                </div>
            </div>
        </div>
    </div>
    <div class="cust-modal" id="addSiteModal">
        <div class="cust-modal-content">
            <button class="cust-modal-close" id="closeAddSiteIcon">×</button>
            <h2 class="cust-modal-title">Add New Site</h2>

            <!-- Hidden fields for IDs -->
            <input type="number" id="SiteId" value="0" hidden />
            <input type="text" id="CustomerID" hidden />
            <input type="text" id="CustomerGuid" hidden />

            <form id="addSiteForm" class="cust-modal-form">

                <!-- Site Name -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="siteName" class="cust-modal-label">Site Name</label>
                        <input type="text" id="siteName" name="siteName" class="cust-modal-input" required />
                    </div>
                </div>

                <!-- Contact Person -->
                <div class="form-row">
                    <div class="cust-modal-field half-width">
                        <label for="firstName" class="cust-modal-label">First Name</label>
                        <input type="text" id="firstName" name="firstName" class="cust-modal-input" />
                    </div>
                    <div class="cust-modal-field half-width">
                        <label for="lastName" class="cust-modal-label">Last Name</label>
                        <input type="text" id="lastName" name="lastName" class="cust-modal-input" />
                    </div>
                </div>

                <!-- Phone + Email -->
                <div class="form-row">
                    <div class="cust-modal-field half-width">
                        <label for="phoneNumber" class="cust-modal-label">Phone Number</label>
                        <input type="tel" id="phoneNumber" name="phoneNumber" class="cust-modal-input" />
                    </div>
                    <div class="cust-modal-field half-width">
                        <label for="email" class="cust-modal-label">Email</label>
                        <input type="email" id="email" name="email" class="cust-modal-input" />
                    </div>
                </div>

                <hr class="form-divider" />

                <!-- Address -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="address" class="cust-modal-label">Street Address</label>
                        <input type="text" id="address" name="address" class="cust-modal-input" required />
                    </div>
                </div>

                <!-- Country / State / Zip -->
                <div class="form-row">
                    <div class="cust-modal-field third-width">
                        <label for="country" class="cust-modal-label">Country</label>
                        <select id="country" name="country" class="cust-modal-input">
                            <option value="USA">USA</option>
                            <option value="Canada">Canada</option>
                        </select>
                    </div>
                    <div class="cust-modal-field third-width">
                        <label for="state" class="cust-modal-label">State / Province</label>
                        <select id="state" name="state" class="cust-modal-input"></select>
                    </div>
                    <div class="cust-modal-field third-width">
                        <label for="zip" id="zipLabel" class="cust-modal-label">Zip Code</label>
                        <input type="text" id="zip" name="zip" class="cust-modal-input" />
                    </div>
                </div>

                <hr class="form-divider" />

                <!-- Note -->
                <div class="form-row">
                    <div class="cust-modal-field full-width">
                        <label for="note" class="cust-modal-label">Note</label>
                        <textarea id="note" name="note" class="cust-modal-input" rows="2"></textarea>
                    </div>
                </div>

                <!-- Active Switch -->
                <div class="form-row">
                    <div class="cust-modal-field cust-toggle-switch-container">
                        <label id="isActiveText" class="cust-modal-label">Active</label>
                        <div class="cust-toggle-switch">
                            <input type="checkbox" id="isActive" name="isActive" checked />
                            <label for="isActive"></label>
                        </div>
                    </div>
                </div>

                <!-- Buttons -->
                <div class="cust-modal-btns">
                    <button type="button" class="cust-modal-cancel" id="closeAddSite">Cancel</button>
                    <button type="button" onclick="saveSite(event )" class="cust-modal-submit">Add Site</button>
                </div>
            </form>
        </div>
    </div>



    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.datatables.net/2.2.2/js/dataTables.min.js"></script>
    <script type="text/javascript" src="https://cdn.datatables.net/select/3.0.0/js/dataTables.select.min.js"></script>

    <script>

        function OpenCustomerChatHistory(mobile, name, customerId) {
            if (!mobile || mobile.trim() === "") {
                Swal.fire('Validation Error', 'Please insert phone number for this customer.', 'warning');
                return;
            }

            window.open('CustomerChatHistory.aspx?mobile=' + encodeURIComponent(mobile) +
                '&name=' + encodeURIComponent(name) +
                '&customerId=' + encodeURIComponent(customerId), '_blank');
        }

        function OpenSMSPopUp(mobile, customerID) {
            $('#<%= txtMobile.ClientID %>').val(mobile);
            $('#<%= txtCustomerId.ClientID %>').val(customerID);
            $('#modalSendSMS').modal('show');
        }

        function CloseSMSPopup() {
            $('#<%= txtMobile.ClientID %>').val('');
            $('#<%= txtCustomerId.ClientID %>').val('');
            $('#<%= txtSMS.ClientID %>').val('');
            $('#modalSendSMS').modal('hide');
        }

        function OpenMMSPopUp(mobile, customerID) {
            $('#<%= txtCustMob.ClientID %>').val(mobile);
            $('#<%= txtCustId.ClientID %>').val(customerID);
            $('#modalSendMMS').modal('show');
        }

        function CloseMMSPopup() {
            $('#<%= txtCustMob.ClientID %>').val('');
            $('#<%= txtCustId.ClientID %>').val('');
            $('#<%= txtMMSBody.ClientID %>').val('');
            $('#<%= fuAttachment.ClientID %>').val('');
            $("#fileError").text('');
            $("#fileError").removeClass('text-success').addClass('text-danger');
            $('#modalSendMMS').modal('hide');
        }

        function OpenSMSHistoryFromModal() {
            var mobile = $('#<%= txtMobile.ClientID %>').val().trim() || $('#<%= txtCustMob.ClientID %>').val().trim();
            var customerId = $('#<%= txtCustomerId.ClientID %>').val().trim() || $('#<%= txtCustId.ClientID %>').val().trim();
            var customerName = $('#customerName').text().trim();

            if (!mobile) {
                Swal.fire('Validation Error', 'Mobile number is required to view history.', 'warning');
                return;
            }

            $('#modalSendSMS').modal('hide');
            $('#modalSendMMS').modal('hide');

            window.open('CustomerChatHistory.aspx?mobile=' + encodeURIComponent(mobile) +
                '&name=' + encodeURIComponent(customerName) +
                '&customerId=' + encodeURIComponent(customerId), '_blank');
        }

    </script>
    <script src="Scripts/AppointmentList.js?v=22"></script>
</asp:Content>
