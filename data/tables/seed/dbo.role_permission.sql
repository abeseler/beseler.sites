/* Migration { "title": "grantAdminAll", "run": "always" } */
INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r
CROSS JOIN permission p
WHERE r.name = 'admin'
ON CONFLICT DO NOTHING;

/* Migration { "title": "00:memberAccount" } */
INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r
JOIN permission p ON p.resource = 'account' AND p.action IN ('read', 'update', 'delete')
WHERE r.name = 'member';

/* Migration { "title": "01:memberBudget" } */
INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r
JOIN permission p ON p.resource = 'budget' AND p.action IN ('read', 'update')
WHERE r.name = 'member';
