/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE permission_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "01:createTable" } */
CREATE TABLE permission (
    permission_id INT NOT NULL DEFAULT (nextval('permission_id_seq')),
    resource TEXT NOT NULL,
    action TEXT NOT NULL,
    CONSTRAINT pk_permission PRIMARY KEY (permission_id),
    CONSTRAINT uq_permission_resource_action UNIQUE (resource, action)
);

/* Migration { "title": "02:seedAccount" } */
INSERT INTO permission (resource, action) VALUES
    ('account', 'read'),
    ('account', 'update'),
    ('account', 'delete');
