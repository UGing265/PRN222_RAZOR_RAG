CREATE TABLE IF NOT EXISTS system_settings (
    key character varying(100) NOT NULL,
    value character varying(1000) NOT NULL,
    description character varying(500),
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT system_settings_pkey PRIMARY KEY (key)
);
