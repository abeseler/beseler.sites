/* Migration { "title": "00:add" } */
ALTER TABLE budget_recurring_template
    ADD CONSTRAINT fk_budget_recurring_template_account
        FOREIGN KEY (account_id) REFERENCES account (account_id) ON DELETE CASCADE;
