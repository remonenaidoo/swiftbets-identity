-- Staff who handle customers see responsible-gambling settings and the audit trail (E2).
INSERT INTO accounts.RolePermissions (Role, Permission) VALUES
    ('Ops', 'compliance.read'),
    ('Ops', 'compliance.audit.read'),
    ('Admin', 'compliance.read'),
    ('Admin', 'compliance.audit.read');
