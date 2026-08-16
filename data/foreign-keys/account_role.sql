/* Migration { "title": "00:add" } */
ALTER TABLE account_role
    ADD CONSTRAINT fk_account_role_account
        FOREIGN KEY (account_id) REFERENCES account (account_id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_account_role_role
        FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE;
