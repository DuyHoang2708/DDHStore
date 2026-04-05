CREATE OR REPLACE TRIGGER TRG_USERS_HASH_PASSWORD_SMART
BEFORE INSERT OR UPDATE ON USERS
FOR EACH ROW
DECLARE
    v_password VARCHAR2(4000);
    v_username VARCHAR2(4000);
    v_email    VARCHAR2(100);
    v_phone    VARCHAR2(15);

    FUNCTION is_sha256_hex(p IN VARCHAR2) RETURN BOOLEAN IS
    BEGIN
        RETURN p IS NOT NULL AND REGEXP_LIKE(p, '^[A-F0-9]{64}$');
    END;
BEGIN
    v_password := :NEW.PASSWORD;
    v_username := :NEW.USERNAME;
    v_email    := :NEW.EMAIL;
    v_phone    := :NEW.PHONE;

    -- ===== USERNAME VALIDATION =====
    IF REGEXP_LIKE(v_username, '\s') THEN
        RAISE_APPLICATION_ERROR(-20001, 'Tên đăng nhập không được chứa khoảng trắng.');
    END IF;

    IF REGEXP_LIKE(v_username, '[^a-zA-Z0-9_]') THEN
        RAISE_APPLICATION_ERROR(-20002, 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_).');
    END IF;

    -- ===== PASSWORD: validate + hash only when plaintext and changed =====
    IF :NEW.PASSWORD IS NULL THEN
        RAISE_APPLICATION_ERROR(-20010, 'Mật khẩu không được để trống.');
    END IF;

    IF (INSERTING OR (:NEW.PASSWORD <> :OLD.PASSWORD)) AND NOT is_sha256_hex(:NEW.PASSWORD) THEN
        IF LENGTH(v_password) < 8 THEN
            RAISE_APPLICATION_ERROR(-20003, 'Mật khẩu phải có ít nhất 8 ký tự.');
        END IF;

        IF NOT REGEXP_LIKE(v_password, '[A-Z]') THEN
            RAISE_APPLICATION_ERROR(-20004, 'Mật khẩu phải có ít nhất 1 chữ in hoa.');
        END IF;

        IF NOT REGEXP_LIKE(v_password, '[a-z]') THEN
            RAISE_APPLICATION_ERROR(-20005, 'Mật khẩu phải có ít nhất 1 chữ thường.');
        END IF;

        IF NOT REGEXP_LIKE(v_password, '[0-9]') THEN
            RAISE_APPLICATION_ERROR(-20006, 'Mật khẩu phải có ít nhất 1 số.');
        END IF;

        IF NOT REGEXP_LIKE(v_password, '[!@#$%^&*(),.?":{}|<>]') THEN
            RAISE_APPLICATION_ERROR(-20007, 'Mật khẩu phải có ít nhất 1 ký tự đặc biệt (!@#$%^&*(),.?":{}|<>).');
        END IF;

        :NEW.PASSWORD := RAWTOHEX(
                            DBMS_CRYPTO.HASH(
                                UTL_I18N.STRING_TO_RAW(v_password, 'AL32UTF8'),
                                DBMS_CRYPTO.HASH_SH256
                            )
                         );
    END IF;

    -- ===== EMAIL VALIDATION =====
    IF v_email IS NOT NULL THEN
        IF NOT REGEXP_LIKE(v_email, '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$') THEN
            RAISE_APPLICATION_ERROR(-20008, 'Email không hợp lệ.');
        END IF;
    END IF;

    -- ===== PHONE VALIDATION =====
    IF v_phone IS NOT NULL THEN
        IF NOT REGEXP_LIKE(v_phone, '^\+?\d{9,15}$') THEN
            RAISE_APPLICATION_ERROR(-20009, 'Số điện thoại không hợp lệ (chỉ chứa số, 9-15 ký tự, có thể có dấu +).');
        END IF;
    END IF;
END;
/
