DELETE FROM accounts.UserRoles WHERE UserId = @UserId AND Role IN ('Trader', 'Ops', 'Admin', 'Operator');
