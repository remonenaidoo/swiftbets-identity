-- Ops and Admin see deposits, withdrawals and reconciliation runs, and approve or reject held withdrawals (payments).
INSERT INTO accounts.RolePermissions (Role, Permission) VALUES
    ('Ops', 'payments.read'),
    ('Ops', 'payments.approve'),
    ('Admin', 'payments.read'),
    ('Admin', 'payments.approve');
