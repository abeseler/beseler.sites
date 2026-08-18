/* Migration { "title": "00:createTable" } */
CREATE TABLE app_setting (
    key TEXT NOT NULL,
    value TEXT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by_account_id INT NULL,
    CONSTRAINT pk_app_setting PRIMARY KEY (key)
);
