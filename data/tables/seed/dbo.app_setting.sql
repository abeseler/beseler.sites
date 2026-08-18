/* Migration { "title": "00:publicSignup" } */
INSERT INTO app_setting (key, value)
VALUES ('public_signup', 'false');
