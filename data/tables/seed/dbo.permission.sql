/* Migration { "title": "00:catalog" } */
INSERT INTO permission (resource, action) VALUES
    ('account', 'read'),
    ('account', 'update'),
    ('account', 'delete'),
    ('role', 'read'),
    ('role', 'update'),
    ('permission', 'read'),
    ('permission', 'update');
