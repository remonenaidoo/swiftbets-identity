-- Rolls back 0008_config_permissions. Staff tokens issued afterwards can no longer see or change settings.
DELETE FROM accounts.RolePermissions WHERE Permission IN ('config.read', 'config.write');
DELETE FROM dbo.SchemaVersions WHERE ScriptName = 'SwiftBets.Identity.Migrator.Migrations.0008_config_permissions.sql';
