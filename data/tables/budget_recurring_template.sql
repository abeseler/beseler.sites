/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE budget_recurring_template_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "01:createTable" } */
CREATE TABLE budget_recurring_template (
    budget_recurring_template_id INT NOT NULL DEFAULT (nextval('budget_recurring_template_id_seq')),
    account_id INT NOT NULL,
    name TEXT NOT NULL,
    section TEXT NOT NULL,
    estimated_amount NUMERIC(12,2) NULL,
    include_in_cashflow BOOLEAN NOT NULL DEFAULT true,
    schedule_type TEXT NOT NULL,
    day_of_month SMALLINT NULL,
    weekday SMALLINT NULL,
    week_of_month SMALLINT NULL,
    anchor_date DATE NULL,
    interval_days SMALLINT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_budget_recurring_template PRIMARY KEY (budget_recurring_template_id),
    CONSTRAINT chk_budget_recurring_template_section CHECK (section IN ('income', 'expense', 'savings')),
    CONSTRAINT chk_budget_recurring_template_schedule_type CHECK (schedule_type IN ('monthly', 'biweekly')),
    CONSTRAINT chk_budget_recurring_template_schedule CHECK (
        (schedule_type = 'monthly' AND day_of_month IS NOT NULL)
        OR (schedule_type = 'biweekly' AND anchor_date IS NOT NULL)
    ),
    CONSTRAINT chk_budget_recurring_template_day_of_month CHECK (day_of_month IS NULL OR (day_of_month BETWEEN 1 AND 31)),
    CONSTRAINT chk_budget_recurring_template_weekday CHECK (weekday IS NULL OR (weekday BETWEEN 0 AND 6)),
    CONSTRAINT chk_budget_recurring_template_week_of_month CHECK (week_of_month IS NULL OR (week_of_month BETWEEN 1 AND 5))
);

/* Migration { "title": "02:dropCashflowRenameAmount" } */
ALTER TABLE budget_recurring_template
    DROP COLUMN include_in_cashflow;
-- NewStatement
ALTER TABLE budget_recurring_template
    RENAME COLUMN estimated_amount TO amount;

/* Migration { "title": "03:expandScheduleTypes" } */
ALTER TABLE budget_recurring_template
    DROP CONSTRAINT chk_budget_recurring_template_schedule_type;
-- NewStatement
ALTER TABLE budget_recurring_template
    ADD CONSTRAINT chk_budget_recurring_template_schedule_type
    CHECK (schedule_type IN ('monthly', 'weekly', 'biweekly', 'interval'));
-- NewStatement
ALTER TABLE budget_recurring_template
    DROP CONSTRAINT chk_budget_recurring_template_schedule;
-- NewStatement
ALTER TABLE budget_recurring_template
    ADD CONSTRAINT chk_budget_recurring_template_schedule CHECK (
        (schedule_type = 'monthly' AND day_of_month IS NOT NULL)
        OR (schedule_type IN ('weekly', 'biweekly') AND anchor_date IS NOT NULL)
        OR (schedule_type = 'interval' AND anchor_date IS NOT NULL AND interval_days IS NOT NULL AND interval_days BETWEEN 1 AND 365)
    );
-- NewStatement
ALTER TABLE budget_recurring_template
    ADD CONSTRAINT chk_budget_recurring_template_interval_days
    CHECK (interval_days IS NULL OR (interval_days BETWEEN 1 AND 365));
