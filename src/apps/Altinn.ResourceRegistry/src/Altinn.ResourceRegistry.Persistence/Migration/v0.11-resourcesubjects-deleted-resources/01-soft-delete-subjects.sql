-- Soft-delete the subjects of resources that have already been deleted, so they no longer show
-- up in the subject lookups. Resources deleted from now on get their subjects soft-deleted by
-- the delete itself.
UPDATE resourceregistry.resourcesubjects rs
SET deleted = true,
    updated_at = now()
FROM resourceregistry.resource_identifier ri
WHERE ri.deleted = true
  AND rs.resource_urn = 'urn:altinn:resource:' || ri.identifier
  AND rs.deleted = false;
