/* Migration { "title": "00:createSequence" } */
CREATE SEQUENCE role_id_seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    NO CYCLE;

/* Migration { "title": "01:createTable" } */
CREATE TABLE role (
    role_id INT NOT NULL DEFAULT (nextval('role_id_seq')),
    name TEXT NOT NULL,
    CONSTRAINT pk_role PRIMARY KEY (role_id),
    CONSTRAINT uq_role_name UNIQUE (name)
);

/* Migration { "title": "02:seedMember" } */
INSERT INTO role (name) VALUES ('member');
