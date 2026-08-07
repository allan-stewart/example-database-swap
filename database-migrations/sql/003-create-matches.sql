CREATE TABLE IF NOT EXISTS matches (
    match_id UUID PRIMARY KEY,
    winner_player_id UUID NOT NULL,
    loser_player_id UUID NOT NULL,
    best_of INT NOT NULL,
    winner_wins INT NOT NULL,
    loser_wins INT NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_matches_winner_player_id ON matches (winner_player_id, recorded_at);
