-- Admin manages roles and their permissions from the console; finance reports are for Admin and Ops, not traders.
INSERT INTO accounts.RolePermissions (Role, Permission) VALUES
    ('Admin', 'identity.roles.read'),
    ('Admin', 'identity.roles.write'),
    ('Admin', 'reports.read'),
    ('Ops', 'reports.read');
