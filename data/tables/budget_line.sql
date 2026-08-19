/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE budget_line_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "01:createTable" } */
CREATE TABLE budget_line (
    budget_line_id INT NOT NULL DEFAULT (nextval('budget_line_id_seq')),
    budget_period_id INT NOT NULL,
    name TEXT NOT NULL,
    section TEXT NOT NULL,
    include_in_cashflow BOOLEAN NOT NULL DEFAULT true,
    estimated_date DATE NULL,
    actual_date DATE NULL,
    estimated_amount NUMERIC(12,2) NULL,
    actual_amount NUMERIC(12,2) NULL,
    origin TEXT NOT NULL,
    status TEXT NOT NULL,
    budget_recurring_template_id INT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_budget_line PRIMARY KEY (budget_line_id),
    CONSTRAINT chk_budget_line_section CHECK (section IN ('income', 'expense', 'savings'))
);

/* Migration { "title": "02:createIndexes" } */
CREATE INDEX idx_budget_line_period ON budget_line (budget_period_id);
CREATE UNIQUE INDEX uq_budget_line_period_template
    ON budget_line (budget_period_id, budget_recurring_template_id)
    WHERE budget_recurring_template_id IS NOT NULL;

/* Migration { "title": "03:dropUniquePeriodTemplate" } */
DROP INDEX uq_budget_line_period_template;

/* Migration { "title": "04:indexPeriodTemplate" } */
CREATE INDEX idx_budget_line_period_template
    ON budget_line (budget_period_id, budget_recurring_template_id)
    WHERE budget_recurring_template_id IS NOT NULL;

/* Migration { "title": "05:reshapeColumns" } */
ALTER TABLE budget_line
    DROP COLUMN include_in_cashflow,
    DROP COLUMN estimated_date,
    DROP COLUMN actual_date,
    DROP COLUMN estimated_amount,
    DROP COLUMN actual_amount,
    DROP COLUMN origin,
    DROP COLUMN status,
    ADD COLUMN amount NUMERIC(12,2) NULL,
    ADD COLUMN on_date DATE NULL,
    ADD COLUMN committed BOOLEAN NOT NULL DEFAULT false;
