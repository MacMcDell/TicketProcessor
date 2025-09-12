-- Create an application role with limited privileges
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_APP_USER}') THEN
      CREATE ROLE ${DB_APP_USER} LOGIN PASSWORD '${DB_APP_PASSWORD}';
   END IF;
END$$;

-- Ensure ownership + privileges on the ticketing database
ALTER DATABASE ticketing OWNER TO ${POSTGRES_USER};

-- Future: You’ll create schemas/tables with EF Migrations,
-- but we grant basic connect+usage now.
GRANT CONNECT ON DATABASE ticketing TO ${DB_APP_USER};

-- Apply default privileges for future objects (when created by the superuser)
ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ${DB_APP_USER};
ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO ${DB_APP_USER};
