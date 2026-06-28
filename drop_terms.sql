START TRANSACTION;

ALTER TABLE documents DROP CONSTRAINT fk_documents_academic_term;

ALTER TABLE subjects DROP CONSTRAINT fk_subjects_academic_term;

DROP TABLE academic_terms;

DROP INDEX "IX_subjects_academic_term_id";

DROP INDEX "IX_documents_academic_term_id";

ALTER TABLE subjects DROP COLUMN academic_term_id;

ALTER TABLE documents DROP COLUMN academic_term_id;

UPDATE document_types SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028792Z'
WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-111111111111';

UPDATE document_types SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028792Z'
WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-222222222222';

UPDATE document_types SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028792Z'
WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-333333333333';

UPDATE document_types SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028792Z'
WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-444444444444';

UPDATE document_types SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028792Z'
WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-555555555555';

UPDATE languages SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028795Z'
WHERE id = 'bbbbbbbb-bbbb-bbbb-bbbb-111111111111';

UPDATE languages SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028795Z'
WHERE id = 'bbbbbbbb-bbbb-bbbb-bbbb-222222222222';

UPDATE languages SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028796Z'
WHERE id = 'bbbbbbbb-bbbb-bbbb-bbbb-333333333333';

UPDATE subjects SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028607Z'
WHERE id = '55555555-5555-5555-5555-555555555555';

UPDATE subjects SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028607Z'
WHERE id = '66666666-6666-6666-6666-666666666666';

UPDATE subjects SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028607Z'
WHERE id = '77777777-7777-7777-7777-777777777777';

UPDATE subjects SET created_at = TIMESTAMPTZ '2026-06-27T04:54:30.028608Z'
WHERE id = '88888888-8888-8888-8888-888888888888';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260627045433_RemoveAcademicTerms', '8.0.11');

COMMIT;

