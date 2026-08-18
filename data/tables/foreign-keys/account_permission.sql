/* Migration { "title": "00:add" } */
ALTER TABLE account_permission
    ADD CONSTRAINT fk_account_permission_account
        FOREIGN KEY (account_id) REFERENCES account (account_id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_account_permission_permission
        FOREIGN KEY (permission_id) REFERENCES permission (permission_id) ON DELETE CASCADE;
