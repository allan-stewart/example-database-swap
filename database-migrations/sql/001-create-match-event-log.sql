CREATE TABLE IF NOT EXISTS match_event_log (
    match_event_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    match_id UUID NOT NULL,
    player_id UUID NOT NULL,
    throw_index INT NOT NULL,
    thrown VARCHAR(10) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_match_event_log_match_id ON match_event_log (match_id);
