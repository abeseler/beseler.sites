/* Migration { "title": "00:createTable" } */
CREATE TABLE role_permission (
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
    CONSTRAINT pk_role_permission PRIMARY KEY (role_id, permission_id)
);

/* Migration { "title": "01:seedMemberAccount" } */
INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r
JOIN permission p ON p.resource = 'account' AND p.action IN ('read', 'update', 'delete')
WHERE r.name = 'member';
