CREATE TABLE IF NOT EXISTS "SystemSettings" (
    "Key" character varying(100) NOT NULL,
    "Value" character varying(1000) NOT NULL,
    "Description" character varying(500),
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SystemSettings" PRIMARY KEY ("Key")
);
