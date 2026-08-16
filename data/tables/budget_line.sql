/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE budget_line_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "00:createTable" } */
CREATE TABLE budget_line (
    budget_line_id INT NOT NULL DEFAULT (nextval('budget_line_id_seq')),
    budget_period_id INT NOT NULL,
    include_in_cashflow BOOLEAN NOT NULL DEFAULT true,
    estimated_date DATE NULL,
    actual_date DATE NULL,
    estimated_amount NUMERIC(12,2) NULL,
    actual_amount NUMERIC(12,2) NULL,
    origin TEXT NOT NULL,
    status TEXT NOT NULL,
    budget_recurring_template_id INT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT pk_budget_line PRIMARY KEY (budget_line_id)
);
