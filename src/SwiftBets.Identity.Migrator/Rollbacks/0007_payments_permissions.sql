-- Rolls back 0007_payments_permissions. Staff tokens issued afterwards can no longer see or approve payments.
DELETE FROM accounts.RolePermissions WHERE Permission IN ('payments.read', 'payments.approve');
DELETE FROM dbo.SchemaVersions WHERE ScriptName = 'SwiftBets.Identity.Migrator.Migrations.0007_payments_permissions.sql';
