/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE budget_recurring_template_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "00:createTable" } */
CREATE TABLE budget_recurring_template (     
    budget_recurring_template_id INT NOT NULL DEFAULT (nextval('budget_recurring_template_id_seq')),
    account_id INT NOT NULL, 
    name TEXT NOT NULL,
    section TEXT NOT NULL,
    estimated_amount NUMERIC(12,2) NULL,
    include_in_cashflow BOOLEAN NOT NULL DEFAULT true,
    --- scheduling ---
    schedule_type TEXT NULL,
    day_of_month SMALLINT NULL,
    weekday SMALLINT NULL,
    week_of_month SMALLINT NULL,
    anchor_date DATE,
    interval_days SMALLINT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT pk_budget_recurring_template PRIMARY KEY (budget_recurring_template_id),
    CONSTRAINT chk_budget_recurring_template_day_of_month CHECK (day_of_month IS NULL OR (day_of_month BETWEEN 1 AND 31)),
    CONSTRAINT chk_budget_recurring_template_weekday CHECK (weekday IS NULL OR (weekday BETWEEN 0 AND 6)),
    CONSTRAINT chk_budget_recurring_template_week_of_month CHECK (week_of_month IS NULL OR (week_of_month BETWEEN 1 AND 5))
);
