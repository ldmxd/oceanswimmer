-- Migration 002: Add password reset token columns to auth.Users
-- Run this against your production database before deploying the forgot-password feature.

ALTER TABLE auth.Users ADD ResetToken NVARCHAR(100) NULL;
ALTER TABLE auth.Users ADD ResetTokenExpiry DATETIME2 NULL;
