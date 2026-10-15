-- Remove the abandoned App Catalog store and retain only the identity fields
-- required by Product Health authentication. Safe to apply more than once.
DROP TABLE IF EXISTS apps CASCADE;

DO $$
BEGIN
    IF to_regclass('public.app_users') IS NOT NULL
       AND to_regclass('public.product_health_users') IS NULL THEN
        ALTER TABLE app_users DROP COLUMN IF EXISTS ping_id;
        ALTER TABLE app_users DROP COLUMN IF EXISTS last_login;
        ALTER TABLE app_users RENAME TO product_health_users;
    END IF;
END $$;

DO $$
BEGIN
    IF to_regclass('public.ux_app_users_email_lower') IS NOT NULL
       AND to_regclass('public.ux_product_health_users_email_lower') IS NULL THEN
        ALTER INDEX ux_app_users_email_lower RENAME TO ux_product_health_users_email_lower;
    END IF;
END $$;

INSERT INTO schema_migrations(version) VALUES ('002_product_health_only')
ON CONFLICT (version) DO NOTHING;
