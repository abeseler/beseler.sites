/* Migration { "title": "00:add" } */
ALTER TABLE role_permission
    ADD CONSTRAINT fk_role_permission_role
        FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_role_permission_permission
        FOREIGN KEY (permission_id) REFERENCES permission (permission_id) ON DELETE CASCADE;
