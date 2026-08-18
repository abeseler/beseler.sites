/* Migration { "title": "00:add" } */
ALTER TABLE budget_period
    ADD CONSTRAINT fk_budget_period_account
        FOREIGN KEY (account_id) REFERENCES account (account_id) ON DELETE CASCADE;
