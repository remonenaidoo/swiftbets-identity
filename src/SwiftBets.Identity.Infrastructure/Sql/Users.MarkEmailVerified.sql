UPDATE accounts.Users SET EmailVerifiedAt = @Now, UpdatedAt = @Now WHERE UserId = @UserId AND EmailVerifiedAt IS NULL;
