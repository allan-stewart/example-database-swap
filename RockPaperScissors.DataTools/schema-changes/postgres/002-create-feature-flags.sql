CREATE TABLE IF NOT EXISTS feature_flags (
    name TEXT PRIMARY KEY,
    enabled BOOLEAN NOT NULL
);
