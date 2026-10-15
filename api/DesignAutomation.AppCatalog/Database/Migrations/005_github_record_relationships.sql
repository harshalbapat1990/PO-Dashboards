-- Enforce source-record parent links after Stage 3 records are imported.
BEGIN;

ALTER TABLE github_source_records
    ADD COLUMN IF NOT EXISTS parent_entity_type text NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_github_source_records_parent') THEN
        ALTER TABLE github_source_records
            ADD CONSTRAINT fk_github_source_records_parent
            FOREIGN KEY (product_id,repository_url,parent_entity_type,parent_external_id)
            REFERENCES github_source_records(product_id,repository_url,entity_type,external_id)
            ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED;
    END IF;
END $$;

UPDATE github_source_records
SET parent_entity_type = CASE WHEN entity_type='workflow_run' THEN 'workflow' ELSE 'repository' END
WHERE parent_external_id IS NOT NULL AND parent_entity_type IS NULL;

INSERT INTO schema_migrations(version) VALUES ('005_github_record_relationships')
ON CONFLICT (version) DO NOTHING;
COMMIT;
