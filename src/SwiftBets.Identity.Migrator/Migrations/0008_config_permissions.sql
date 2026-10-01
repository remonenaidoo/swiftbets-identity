-- Ops and Admin see operational settings; Admin changes them (kill switch, placement mode, limits, flags).
INSERT INTO accounts.RolePermissions (Role, Permission) VALUES
    ('Ops', 'config.read'),
    ('Admin', 'config.read'),
    ('Admin', 'config.write');
