-- SQL Schema for Transactional Outbox Pattern
-- This schema ensures reliable message delivery by storing events
-- before they are published to the message bus.

CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(500) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Headers NVARCHAR(MAX),
    CreatedAt DATETIMEOFFSET NOT NULL,
    ProcessedAt DATETIMEOFFSET,
    Error NVARCHAR(MAX),
    RetryCount INT DEFAULT 0
);

-- Index for efficient querying of unprocessed messages
CREATE INDEX IX_Outbox_Unprocessed ON OutboxMessages (ProcessedAt) WHERE ProcessedAt IS NULL;

-- Index for querying by creation time
CREATE INDEX IX_Outbox_CreatedAt ON OutboxMessages (CreatedAt);
