-- Identity publishes its events through the shared outbox (created by the outbox migrations that run first).
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::outbox TO swiftbets_app;
