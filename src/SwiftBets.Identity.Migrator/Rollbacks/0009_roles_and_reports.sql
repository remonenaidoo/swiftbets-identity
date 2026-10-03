-- Rolls back 0009_roles_and_reports. Staff tokens issued afterwards can no longer manage roles or read finance reports.
DELETE FROM accounts.RolePermissions WHERE Permission IN ('identity.roles.read', 'identity.roles.write', 'reports.read');
DELETE FROM dbo.SchemaVersions WHERE ScriptName = 'SwiftBets.Identity.Migrator.Migrations.0009_roles_and_reports.sql';
