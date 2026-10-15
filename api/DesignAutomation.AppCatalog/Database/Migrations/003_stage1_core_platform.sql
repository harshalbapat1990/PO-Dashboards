-- Stage 1: scoped access control and integrity constraints.
-- Apply once after 001 and 002. This migration performs no external calls.
BEGIN;

ALTER TABLE products
    ADD CONSTRAINT ck_products_status CHECK (status IN ('Active','Planned','Paused','Retired')),
    ADD CONSTRAINT ck_products_target_after_start CHECK (start_date IS NULL OR target_date IS NULL OR target_date >= start_date);

ALTER TABLE product_health_users
    ADD CONSTRAINT ck_product_health_users_role CHECK (role IN (0,1,20,40,90));

CREATE UNIQUE INDEX ux_organizations_name_ci ON organizations(lower(name));
CREATE UNIQUE INDEX ux_portfolios_org_name_ci ON portfolios(organization_id,lower(name));
CREATE UNIQUE INDEX ux_products_portfolio_name_ci ON products(portfolio_id,lower(name));
CREATE UNIQUE INDEX ux_teams_product_name_ci ON teams(product_id,lower(name));

CREATE TABLE portfolio_user_access (
    portfolio_id uuid NOT NULL REFERENCES portfolios(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES product_health_users(id) ON DELETE CASCADE,
    role integer NOT NULL CHECK (role IN (1,20,40)),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (portfolio_id,user_id)
);
CREATE TABLE product_user_access (
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES product_health_users(id) ON DELETE CASCADE,
    role integer NOT NULL CHECK (role IN (1,20,40)),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (product_id,user_id)
);
CREATE INDEX ix_portfolio_user_access_user ON portfolio_user_access(user_id);
CREATE INDEX ix_product_user_access_user ON product_user_access(user_id);

INSERT INTO schema_migrations(version) VALUES ('003_stage1_core_platform')
ON CONFLICT (version) DO NOTHING;
COMMIT;
