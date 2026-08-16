/* Migration { "title": "00:createTable" } */
CREATE TABLE account_role (
    account_id INT NOT NULL,
    role_id INT NOT NULL,
    scope TEXT NOT NULL,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_by_account_id INT NOT NULL,
    CONSTRAINT pk_account_role PRIMARY KEY (account_id, role_id)
);
