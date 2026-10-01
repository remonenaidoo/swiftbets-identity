-- Ops and Admin place and lift customer restrictions and write notes; lifting needs a second operator (compliance).
INSERT INTO accounts.RolePermissions (Role, Permission) VALUES
    ('Ops', 'compliance.write'),
    ('Admin', 'compliance.write');
