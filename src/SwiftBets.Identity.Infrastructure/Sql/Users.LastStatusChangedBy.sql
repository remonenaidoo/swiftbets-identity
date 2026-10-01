SELECT TOP (1) ChangedBy FROM accounts.StatusHistory WHERE UserId = @UserId ORDER BY StatusChangeId DESC;
