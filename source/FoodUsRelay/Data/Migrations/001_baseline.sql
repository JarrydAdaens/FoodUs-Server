-- Migration 001: schema-version bookkeeping only.
-- Domain tables (profiles, friend codes, blocks, mailbox) arrive as later numbered
-- migrations beside the stories that own them (Milestone 3, Stories 3-5).
CREATE TABLE IF NOT EXISTS schema_migrations
(
    version    INTEGER PRIMARY KEY,
    name       TEXT NOT NULL,
    applied_at TEXT NOT NULL
);
