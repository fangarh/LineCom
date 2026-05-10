UPDATE requests SET status = 'in_progress' WHERE status = 'quoted';

UPDATE request_history
SET old_status = CASE WHEN old_status = 'quoted' THEN 'in_progress' ELSE old_status END,
    new_status = CASE WHEN new_status = 'quoted' THEN 'in_progress' ELSE new_status END
WHERE old_status = 'quoted' OR new_status = 'quoted';

ALTER TABLE requests DROP CONSTRAINT IF EXISTS ck_requests_status;
ALTER TABLE request_history DROP CONSTRAINT IF EXISTS ck_request_history_old_status;
ALTER TABLE request_history DROP CONSTRAINT IF EXISTS ck_request_history_new_status;

ALTER TABLE requests
    ADD CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'));

ALTER TABLE request_history
    ADD CONSTRAINT ck_request_history_old_status CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'));

ALTER TABLE request_history
    ADD CONSTRAINT ck_request_history_new_status CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'));
