-- Normalized Azure DevOps snapshots are imported locally. This migration does
-- not add an Azure DevOps client or create any outbound integration.
BEGIN;

CREATE TABLE IF NOT EXISTS azure_devops_source_records (
    product_id uuid NOT NULL,
    project_id text NOT NULL,
    entity_type text NOT NULL CHECK (entity_type IN (
        'project','iteration','work_item','pipeline','pipeline_run',
        'test_plan','test_suite','test_case','test_run','test_result'
    )),
    external_id text NOT NULL,
    record_data jsonb NOT NULL,
    source_updated_at timestamptz NULL,
    synced_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (product_id, project_id, entity_type, external_id),
    FOREIGN KEY (product_id, project_id)
        REFERENCES product_azure_projects(product_id, project_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_azure_devops_records_product_project_type
    ON azure_devops_source_records(product_id, project_id, entity_type, synced_at DESC);
CREATE INDEX IF NOT EXISTS ix_azure_devops_records_parent_fields
    ON azure_devops_source_records(product_id, project_id, (record_data->>'parentId'))
    WHERE entity_type = 'work_item';
CREATE INDEX IF NOT EXISTS ix_azure_devops_records_iteration_fields
    ON azure_devops_source_records(product_id, project_id, (record_data->>'iterationId'))
    WHERE entity_type = 'work_item';

CREATE TABLE IF NOT EXISTS azure_devops_source_relationships (
    product_id uuid NOT NULL,
    project_id text NOT NULL,
    source_entity_type text NOT NULL,
    source_external_id text NOT NULL,
    relationship_type text NOT NULL CHECK (relationship_type IN (
        'parent','iteration','pipeline','test_plan','test_suite','test_case','test_run'
    )),
    target_entity_type text NOT NULL,
    target_external_id text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (product_id, project_id, source_entity_type, source_external_id,
                 relationship_type, target_entity_type, target_external_id),
    FOREIGN KEY (product_id, project_id, source_entity_type, source_external_id)
        REFERENCES azure_devops_source_records(product_id, project_id, entity_type, external_id)
        ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY (product_id, project_id, target_entity_type, target_external_id)
        REFERENCES azure_devops_source_records(product_id, project_id, entity_type, external_id)
        ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX IF NOT EXISTS ix_azure_devops_relationship_targets
    ON azure_devops_source_relationships(product_id, project_id, target_entity_type, target_external_id);

INSERT INTO schema_migrations(version) VALUES ('006_stage4_azure_devops_source_data')
ON CONFLICT (version) DO NOTHING;
COMMIT;
