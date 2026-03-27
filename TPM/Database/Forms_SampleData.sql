-- Sample Data for Forms Management System
-- Execute this script after creating the Forms schema

USE [myServiceJobs]
GO

-- Clear existing data (optional - remove if you want to keep existing data)
-- DELETE FROM FormUsageLog;
-- DELETE FROM FormInstanceFields;
-- DELETE FROM FormInstances;
-- DELETE FROM AppointmentFormMapping;
-- DELETE FROM FormTemplates;

-- Sample Form Templates
INSERT INTO FormTemplates (CompanyID, TemplateName, Description, Category, IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure, RequireSignature, RequireTip, IsActive, CreatedBy, CreatedDateTime) VALUES
('COMP001', 'HVAC Maintenance Checklist', 'Standard maintenance checklist for HVAC systems', 'Maintenance', 1, '["HVAC Maintenance", "AC Service"]', 
'{"fields":[{"id":"field_1","name":"system_type","type":"dropdown","label":"System Type","required":true,"options":["Central Air","Window Unit","Heat Pump","Furnace"]},{"id":"field_2","name":"filter_condition","type":"radio","label":"Filter Condition","required":true,"options":["Clean","Dirty","Needs Replacement"]},{"id":"field_3","name":"temperature_reading","type":"number","label":"Temperature Reading (°F)","required":true},{"id":"field_4","name":"notes","type":"textarea","label":"Additional Notes","required":false},{"id":"field_5","name":"tech_signature","type":"signature","label":"Technician Signature","required":true}]}', 
1, 0, 1, 'admin', GETDATE()),

('COMP001', 'Plumbing Inspection Form', 'Comprehensive plumbing system inspection', 'Inspection', 1, '["Plumbing", "Pipe Repair"]', 
'{"fields":[{"id":"field_1","name":"water_pressure","type":"dropdown","label":"Water Pressure","required":true,"options":["Low","Normal","High"]},{"id":"field_2","name":"leak_detected","type":"radio","label":"Leaks Detected","required":true,"options":["Yes","No"]},{"id":"field_3","name":"leak_location","type":"text","label":"Leak Location (if any)","required":false},{"id":"field_4","name":"recommendations","type":"textarea","label":"Recommendations","required":true},{"id":"field_5","name":"customer_signature","type":"signature","label":"Customer Signature","required":true}]}', 
1, 1, 1, 'admin', GETDATE()),

('COMP001', 'Electrical Safety Check', 'Basic electrical safety inspection form', 'Safety', 0, '[]', 
'{"fields":[{"id":"field_1","name":"panel_condition","type":"dropdown","label":"Panel Condition","required":true,"options":["Good","Fair","Poor","Unsafe"]},{"id":"field_2","name":"gfci_tested","type":"checkbox","label":"GFCI Outlets Tested","required":true},{"id":"field_3","name":"issues_found","type":"textarea","label":"Issues Found","required":false},{"id":"field_4","name":"safety_rating","type":"radio","label":"Overall Safety Rating","required":true,"options":["Safe","Minor Issues","Major Concerns","Unsafe"]},{"id":"field_5","name":"tech_signature","type":"signature","label":"Technician Signature","required":true}]}', 
1, 0, 1, 'admin', GETDATE()),

('COMP001', 'Customer Satisfaction Survey', 'Post-service customer feedback form', 'Survey', 0, '[]', 
'{"fields":[{"id":"field_1","name":"service_rating","type":"radio","label":"Service Quality Rating","required":true,"options":["Excellent","Good","Fair","Poor"]},{"id":"field_2","name":"technician_rating","type":"radio","label":"Technician Rating","required":true,"options":["Excellent","Good","Fair","Poor"]},{"id":"field_3","name":"would_recommend","type":"radio","label":"Would you recommend us?","required":true,"options":["Yes","No","Maybe"]},{"id":"field_4","name":"comments","type":"textarea","label":"Additional Comments","required":false},{"id":"field_5","name":"customer_signature","type":"signature","label":"Customer Signature","required":true}]}', 
0, 1, 1, 'admin', GETDATE()),

('COMP001', 'Equipment Installation Form', 'New equipment installation documentation', 'Installation', 1, '["Installation", "New Equipment"]', 
'{"fields":[{"id":"field_1","name":"equipment_type","type":"text","label":"Equipment Type","required":true},{"id":"field_2","name":"model_number","type":"text","label":"Model Number","required":true},{"id":"field_3","name":"serial_number","type":"text","label":"Serial Number","required":true},{"id":"field_4","name":"installation_date","type":"date","label":"Installation Date","required":true},{"id":"field_5","name":"warranty_info","type":"textarea","label":"Warranty Information","required":true},{"id":"field_6","name":"customer_signature","type":"signature","label":"Customer Signature","required":true}]}', 
1, 0, 1, 'admin', GETDATE());

-- Sample Auto-Assignment Mappings
INSERT INTO AppointmentFormMapping (CompanyID, TemplateId, ServiceType, IsAutoAssign, Priority, IsActive, CreatedBy, CreatedDateTime) VALUES
('COMP001', 1, 'HVAC Maintenance', 1, 1, 1, 'admin', GETDATE()),
('COMP001', 1, 'AC Service', 1, 1, 1, 'admin', GETDATE()),
('COMP001', 2, 'Plumbing', 1, 1, 1, 'admin', GETDATE()),
('COMP001', 2, 'Pipe Repair', 1, 2, 1, 'admin', GETDATE()),
('COMP001', 5, 'Installation', 1, 1, 1, 'admin', GETDATE()),
('COMP001', 5, 'New Equipment', 1, 1, 1, 'admin', GETDATE());

-- Sample Form Instances (representing filled forms)
INSERT INTO FormInstances (CompanyID, TemplateId, AppointmentId, CustomerID, FormData, Status, TipAmount, TipNotes, FilledBy, StartedDateTime, CompletedDateTime, SubmittedDateTime, IsSynced, StoredFilePath, StoredFileName) VALUES
('COMP001', 1, 'APT001', 'CUST001', 
'{"field_1":"Central Air","field_2":"Dirty","field_3":"72","field_4":"Filter needs replacement soon","field_5":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}', 
'Submitted', 0, '', 'tech001', DATEADD(hour, -2, GETDATE()), DATEADD(hour, -1, GETDATE()), DATEADD(hour, -1, GETDATE()), 1, '/forms/2024/01/hvac_maintenance_APT001.pdf', 'hvac_maintenance_APT001.pdf'),

('COMP001', 2, 'APT002', 'CUST002', 
'{"field_1":"Low","field_2":"Yes","field_3":"Kitchen sink faucet","field_4":"Replace faucet cartridge and check supply lines","field_5":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}', 
'Submitted', 15.00, 'Great service!', 'tech002', DATEADD(hour, -3, GETDATE()), DATEADD(hour, -2, GETDATE()), DATEADD(hour, -2, GETDATE()), 1, '/forms/2024/01/plumbing_inspection_APT002.pdf', 'plumbing_inspection_APT002.pdf'),

('COMP001', 4, 'APT003', 'CUST003', 
'{"field_1":"Excellent","field_2":"Excellent","field_3":"Yes","field_4":"Very professional and thorough technician","field_5":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}', 
'Submitted', 20.00, 'Excellent service, will use again', 'tech001', DATEADD(hour, -4, GETDATE()), DATEADD(hour, -3, GETDATE()), DATEADD(hour, -3, GETDATE()), 1, '/forms/2024/01/customer_survey_APT003.pdf', 'customer_survey_APT003.pdf'),

('COMP001', 1, 'APT004', 'CUST004', 
'{"field_1":"Heat Pump","field_2":"Clean","field_3":"68","field_4":"System running efficiently","field_5":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}', 
'Pending', 0, '', 'tech003', DATEADD(minute, -30, GETDATE()), NULL, NULL, 0, NULL, NULL);

-- Sample Form Instance Fields (for more detailed field tracking if needed)
INSERT INTO FormInstanceFields (FormInstanceId, FieldName, FieldValue, FieldType) VALUES
(1, 'system_type', 'Central Air', 'dropdown'),
(1, 'filter_condition', 'Dirty', 'radio'),
(1, 'temperature_reading', '72', 'number'),
(1, 'notes', 'Filter needs replacement soon', 'textarea'),
(1, 'tech_signature', 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'signature'),

(2, 'water_pressure', 'Low', 'dropdown'),
(2, 'leak_detected', 'Yes', 'radio'),
(2, 'leak_location', 'Kitchen sink faucet', 'text'),
(2, 'recommendations', 'Replace faucet cartridge and check supply lines', 'textarea'),
(2, 'customer_signature', 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'signature');

-- Sample Usage Log
INSERT INTO FormUsageLog (CompanyID, FormInstanceId, TemplateId, AppointmentId, Action, PerformedBy, ActionDateTime, Notes) VALUES
('COMP001', 1, 1, 'APT001', 'Created', 'tech001', DATEADD(hour, -2, GETDATE()), 'Auto-assigned form for HVAC maintenance'),
('COMP001', 1, 1, 'APT001', 'Submitted', 'tech001', DATEADD(hour, -1, GETDATE()), 'Form completed and submitted'),
('COMP001', 1, 1, 'APT001', 'Synced', 'system', DATEADD(minute, -50, GETDATE()), 'Form synced to file storage'),

('COMP001', 2, 2, 'APT002', 'Created', 'tech002', DATEADD(hour, -3, GETDATE()), 'Auto-assigned form for plumbing service'),
('COMP001', 2, 2, 'APT002', 'Submitted', 'tech002', DATEADD(hour, -2, GETDATE()), 'Form completed with tip'),
('COMP001', 2, 2, 'APT002', 'Synced', 'system', DATEADD(hour, -2, GETDATE()), 'Form synced to file storage'),

('COMP001', 3, 4, 'APT003', 'Created', 'tech001', DATEADD(hour, -4, GETDATE()), 'Customer satisfaction survey'),
('COMP001', 3, 4, 'APT003', 'Submitted', 'tech001', DATEADD(hour, -3, GETDATE()), 'Customer provided excellent feedback'),

('COMP001', 4, 1, 'APT004', 'Created', 'tech003', DATEADD(minute, -30, GETDATE()), 'Auto-assigned HVAC maintenance form'),
('COMP001', 4, 1, 'APT004', 'InProgress', 'tech003', DATEADD(minute, -25, GETDATE()), 'Technician started filling form');

-- Display summary of inserted data
SELECT 'Form Templates' as TableName, COUNT(*) as RecordCount FROM FormTemplates WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Auto-Assignment Mappings', COUNT(*) FROM AppointmentFormMapping WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Form Instances', COUNT(*) FROM FormInstances WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Form Fields', COUNT(*) FROM FormInstanceFields
UNION ALL
SELECT 'Usage Log Entries', COUNT(*) FROM FormUsageLog WHERE CompanyID = 'COMP001';

PRINT 'Sample data inserted successfully!'
PRINT 'Templates: 5 form templates with different categories'
PRINT 'Mappings: 6 auto-assignment rules for different service types'
PRINT 'Instances: 4 form instances (3 completed, 1 in progress)'
PRINT 'Fields: Individual field data for detailed tracking'
PRINT 'Usage Log: 10 activity log entries showing form lifecycle'