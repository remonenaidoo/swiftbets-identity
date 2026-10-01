-- Rolls back 0005_compliance_permissions. Staff tokens issued afterwards no longer reach compliance's staff routes.
DELETE FROM accounts.RolePermissions WHERE Permission IN ('compliance.read', 'compliance.audit.read');
DELETE FROM dbo.SchemaVersions WHERE ScriptName = 'SwiftBets.Identity.Migrator.Migrations.0005_compliance_permissions.sql';
