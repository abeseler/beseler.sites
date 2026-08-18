/* Migration { "title": "00:add" } */
ALTER TABLE app_setting
    ADD CONSTRAINT fk_app_setting_updated_by
        FOREIGN KEY (updated_by_account_id) REFERENCES account (account_id) ON DELETE SET NULL;
