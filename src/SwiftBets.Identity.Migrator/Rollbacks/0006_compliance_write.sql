-- Rolls back 0006_compliance_write. Staff tokens issued afterwards can no longer change customer restrictions.
DELETE FROM accounts.RolePermissions WHERE Permission = 'compliance.write';
DELETE FROM dbo.SchemaVersions WHERE ScriptName = 'SwiftBets.Identity.Migrator.Migrations.0006_compliance_write.sql';
