-- Stage 3 normalized GitHub source snapshots. Local development imports
-- snapshots supplied by the operator and makes no outbound GitHub requests.
BEGIN;

CREATE TABLE IF NOT EXISTS github_source_records (
    product_id uuid NOT NULL,
    repository_url text NOT NULL,
    entity_type text NOT NULL CHECK (entity_type IN (
        'repository','branch','commit','pull_request','release','workflow','workflow_run','security_alert'
    )),
    external_id text NOT NULL,
    parent_external_id text NULL,
    record_data jsonb NOT NULL,
    source_updated_at timestamptz NULL,
    synced_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (product_id,repository_url,entity_type,external_id),
    FOREIGN KEY (product_id,repository_url)
        REFERENCES product_github_repositories(product_id,repository_url) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_github_source_records_product_type
    ON github_source_records(product_id,entity_type,synced_at DESC);
CREATE INDEX IF NOT EXISTS ix_github_source_records_repo_type
    ON github_source_records(repository_url,entity_type,external_id);
CREATE INDEX IF NOT EXISTS ix_github_source_records_parent
    ON github_source_records(product_id,repository_url,entity_type,parent_external_id)
    WHERE parent_external_id IS NOT NULL;

INSERT INTO schema_migrations(version) VALUES ('004_stage3_github_source_data')
ON CONFLICT (version) DO NOTHING;
COMMIT;
