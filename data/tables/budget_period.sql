/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE budget_period_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "00:createTable" } */
CREATE TABLE budget_period (
     budget_period_id INT NOT NULL DEFAULT (nextval('budget_period_id_seq')),
     account_id INT NOT NULL,
     year SMALLINT NOT NULL,
     month SMALLINT NOT NULL,
     starting_balance NUMERIC(12,2),
     created_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
     updated_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
     CONSTRAINT pk_budget_period PRIMARY KEY (budget_period_id),
     CONSTRAINT uq_budget_period UNIQUE (account_id, year, month),
     CONSTRAINT chk_budget_period_month CHECK (month BETWEEN 1 AND 12)
);
