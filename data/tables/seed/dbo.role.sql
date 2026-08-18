/* Migration { "title": "00:system" } */
INSERT INTO role (name, protected, locked_grants) VALUES
    ('admin', true, true),
    ('member', true, false);
