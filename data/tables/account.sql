/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE account_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "01:createTable" } */
CREATE TABLE account (
    account_id INT NOT NULL DEFAULT (nextval('account_id_seq')),
    version BIGINT NOT NULL DEFAULT (0),
    type TEXT NOT NULL,
    username TEXT NOT NULL,
    email TEXT NULL,
    email_verified_at TIMESTAMPTZ NULL,
    secret_hash TEXT NOT NULL,
    secret_hashed_at TIMESTAMPTZ NOT NULL,
    given_name TEXT NULL,
    family_name TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    disabled_at TIMESTAMPTZ NULL,
    locked_at TIMESTAMPTZ NULL,
    last_logon TIMESTAMPTZ NULL,
    failed_login_attempts INT NOT NULL DEFAULT (0),
    invited_at TIMESTAMPTZ NULL,
    CONSTRAINT pk_account PRIMARY KEY (account_id),
    CONSTRAINT uq_account_username UNIQUE (username),
    CONSTRAINT uq_account_email UNIQUE (email),
    CONSTRAINT chk_account_type CHECK (type IN ('User', 'Service'))
);
