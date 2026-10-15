CREATE TABLE IF NOT EXISTS schema_migrations (
    version text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS apps (
    id uuid PRIMARY KEY,
    full_name text NOT NULL,
    aliases text[] NOT NULL DEFAULT '{}',
    production boolean NOT NULL DEFAULT false,
    team text[] NOT NULL DEFAULT '{}',
    description text NOT NULL DEFAULT '',
    repos text[] NOT NULL DEFAULT '{}',
    production_branch text NOT NULL DEFAULT '',
    pm_links text[] NOT NULL DEFAULT '{}',
    doc_links text[] NOT NULL DEFAULT '{}',
    website text NOT NULL DEFAULT '',
    hosting text NOT NULL DEFAULT '',
    app_type text NOT NULL DEFAULT '',
    tech_stack text NOT NULL DEFAULT '',
    environments text NOT NULL DEFAULT '',
    identity_management text NOT NULL DEFAULT '',
    integrations text NOT NULL DEFAULT '',
    scale text NOT NULL DEFAULT '',
    growth text NOT NULL DEFAULT '',
    comments text NOT NULL DEFAULT '',
    availability text NOT NULL DEFAULT 'Undecided',
    review text NOT NULL DEFAULT '',
    modified timestamptz NOT NULL,
    created timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_apps_full_name ON apps (full_name);

CREATE TABLE IF NOT EXISTS app_users (
    id uuid PRIMARY KEY,
    email text NOT NULL,
    ping_id uuid NOT NULL,
    last_login timestamptz NULL,
    created timestamptz NOT NULL,
    modified timestamptz NOT NULL,
    role integer NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_app_users_email_lower ON app_users (lower(email));

CREATE TABLE IF NOT EXISTS organizations (
    id uuid PRIMARY KEY,
    name text NOT NULL UNIQUE,
    description text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS portfolios (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    name text NOT NULL,
    description text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (organization_id, name)
);
CREATE TABLE IF NOT EXISTS products (
    id uuid PRIMARY KEY,
    portfolio_id uuid NOT NULL REFERENCES portfolios(id) ON DELETE RESTRICT,
    name text NOT NULL,
    description text NOT NULL DEFAULT '',
    status text NOT NULL DEFAULT 'Active',
    product_type text NOT NULL DEFAULT '',
    product_owner text NOT NULL DEFAULT '',
    product_manager text NOT NULL DEFAULT '',
    global_product_lead text NOT NULL DEFAULT '',
    engineering_manager text NOT NULL DEFAULT '',
    scrum_master text NOT NULL DEFAULT '',
    security_lead text NOT NULL DEFAULT '',
    start_date date NULL,
    target_date date NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (portfolio_id, name)
);
CREATE TABLE IF NOT EXISTS teams (
    id uuid PRIMARY KEY,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    name text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (product_id, name)
);
CREATE TABLE IF NOT EXISTS team_members (
    team_id uuid NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    member_email text NOT NULL,
    PRIMARY KEY (team_id, member_email)
);
CREATE TABLE IF NOT EXISTS product_github_repositories (
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    repository_url text NOT NULL,
    PRIMARY KEY (product_id, repository_url)
);
CREATE TABLE IF NOT EXISTS product_azure_projects (
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    project_id text NOT NULL,
    project_name text NOT NULL DEFAULT '',
    PRIMARY KEY (product_id, project_id)
);
CREATE TABLE IF NOT EXISTS dashboard_modules (
    module_key text PRIMARY KEY,
    display_name text NOT NULL,
    default_enabled boolean NOT NULL DEFAULT true
);
INSERT INTO dashboard_modules(module_key,display_name,default_enabled) VALUES
    ('executive-overview','Executive Overview',true),
    ('sprint-health','Sprint Health',true),
    ('pull-requests','Pull Requests',true),
    ('security','Security',true),
    ('testing','Testing',true),
    ('deployment','Deployment',true),
    ('technical-debt','Technical Debt',true),
    ('ai-insights','AI Insights',true)
ON CONFLICT (module_key) DO NOTHING;

CREATE TABLE IF NOT EXISTS product_dashboard_configuration (
    product_id uuid PRIMARY KEY REFERENCES products(id) ON DELETE CASCADE,
    enabled_modules jsonb NOT NULL DEFAULT '["executive-overview","sprint-health","pull-requests","security","testing","deployment","technical-debt","ai-insights"]'::jsonb,
    module_order jsonb NOT NULL DEFAULT '["executive-overview","sprint-health","pull-requests","security","testing","deployment","technical-debt","ai-insights"]'::jsonb,
    widget_configuration jsonb NOT NULL DEFAULT '{}'::jsonb,
    dashboard_layout jsonb NOT NULL DEFAULT '{}'::jsonb,
    default_filters jsonb NOT NULL DEFAULT '{}'::jsonb,
    default_date_range jsonb NOT NULL DEFAULT '{}'::jsonb,
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS integrations (
    id uuid PRIMARY KEY,
    product_id uuid NULL REFERENCES products(id) ON DELETE CASCADE,
    provider text NOT NULL,
    organization text NOT NULL DEFAULT '',
    repository_or_project text NOT NULL DEFAULT '',
    secret_reference text NOT NULL DEFAULT '',
    sync_frequency_minutes integer NOT NULL DEFAULT 60 CHECK (sync_frequency_minutes > 0),
    last_successful_sync timestamptz NULL,
    last_failed_sync timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_integrations_product_provider_source
    ON integrations (COALESCE(product_id, '00000000-0000-0000-0000-000000000000'::uuid), provider, organization, repository_or_project);

INSERT INTO schema_migrations(version) VALUES ('001_stage0_foundation')
ON CONFLICT (version) DO NOTHING;
