/* Migration { "title": "00:add" } */
ALTER TABLE budget_line
    ADD CONSTRAINT fk_budget_line_period
        FOREIGN KEY (budget_period_id) REFERENCES budget_period (budget_period_id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_budget_line_template
        FOREIGN KEY (budget_recurring_template_id) REFERENCES budget_recurring_template (budget_recurring_template_id) ON DELETE CASCADE;
