IF NOT EXISTS (SELECT 1 FROM accounts.RolePermissions WHERE Role = @Role AND Permission = @Permission)
    INSERT INTO accounts.RolePermissions (Role, Permission) VALUES (@Role, @Permission);
