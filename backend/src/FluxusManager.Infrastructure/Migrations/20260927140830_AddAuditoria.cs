using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxusManager.Infrastructure.Migrations
{
    /// <summary>
    /// Auditoria por trigger: toda alteração nas tabelas rastreadas vira uma linha em audit.change_log,
    /// com o contexto da aplicação (usuário, tenant, tela) lido de set_config('app.*') pelo trigger.
    /// </summary>
    public partial class AddAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE SCHEMA audit;

                -- Particionada por mês (created_at). A partição default só recebe linhas se a manutenção
                -- (audit.ensure_partitions) ficar parada por meses; o normal é ela ficar vazia.
                CREATE TABLE audit.change_log (
                    id                bigint GENERATED ALWAYS AS IDENTITY,
                    schema_name       text        NOT NULL,
                    table_name        text        NOT NULL,
                    row_id            text,
                    operation         text        NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
                    old_values        jsonb,
                    new_values        jsonb,
                    changed_fields    text[],
                    transaction_id    bigint      NOT NULL,
                    session_user_name text        NOT NULL,
                    application_user  text,
                    client_addr       inet,
                    application_name  text,
                    tenant_id         uuid,
                    frontend_url      text,
                    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
                    PRIMARY KEY (id, created_at)
                ) PARTITION BY RANGE (created_at);

                CREATE TABLE audit.change_log_default PARTITION OF audit.change_log DEFAULT;

                CREATE INDEX ix_change_log_table_name ON audit.change_log (table_name);
                CREATE INDEX ix_change_log_row_id ON audit.change_log (row_id);
                CREATE INDEX ix_change_log_created_at ON audit.change_log (created_at);
                CREATE INDEX ix_change_log_tenant_id ON audit.change_log (tenant_id);
                CREATE INDEX ix_change_log_transaction_id ON audit.change_log (transaction_id);
                CREATE INDEX ix_change_log_old_values ON audit.change_log USING gin (old_values);
                CREATE INDEX ix_change_log_new_values ON audit.change_log USING gin (new_values);

                -- Partição de um mês (UTC), se ainda não existir: audit.change_log_AAAA_MM.
                CREATE FUNCTION audit.create_partition(p_month date) RETURNS void
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_start date := date_trunc('month', p_month)::date;
                    v_end   date := (date_trunc('month', p_month) + interval '1 month')::date;
                    v_name  text := 'change_log_' || to_char(v_start, 'YYYY_MM');
                BEGIN
                    IF to_regclass(format('audit.%I', v_name)) IS NULL THEN
                        EXECUTE format(
                            'CREATE TABLE audit.%I PARTITION OF audit.change_log FOR VALUES FROM (%L) TO (%L)',
                            v_name, to_char(v_start, 'YYYY-MM-DD') || ' 00:00:00+00', to_char(v_end, 'YYYY-MM-DD') || ' 00:00:00+00');
                    END IF;
                END $$;

                -- Mês atual e os próximos p_months_ahead. Rodada na subida e diariamente pela API.
                CREATE FUNCTION audit.ensure_partitions(p_months_ahead int DEFAULT 3) RETURNS void
                LANGUAGE plpgsql AS $$
                BEGIN
                    FOR i IN 0..p_months_ahead LOOP
                        PERFORM audit.create_partition(((now() AT TIME ZONE 'UTC') + make_interval(months => i))::date);
                    END LOOP;
                END $$;

                -- Retenção: apaga as partições mensais que terminaram antes de now() - p_retention.
                CREATE FUNCTION audit.drop_partitions_older_than(p_retention interval) RETURNS int
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_partition record;
                    v_dropped   int := 0;
                BEGIN
                    FOR v_partition IN
                        SELECT c.relname
                        FROM pg_inherits i
                        JOIN pg_class c ON c.oid = i.inhrelid
                        WHERE i.inhparent = 'audit.change_log'::regclass
                          AND c.relname ~ '^change_log_\d{4}_\d{2}$'
                    LOOP
                        IF (to_date(substr(v_partition.relname, 12), 'YYYY_MM') + interval '1 month') AT TIME ZONE 'UTC'
                            <= now() - p_retention THEN
                            EXECUTE format('DROP TABLE audit.%I', v_partition.relname);
                            v_dropped := v_dropped + 1;
                        END IF;
                    END LOOP;
                    RETURN v_dropped;
                END $$;

                -- Função do trigger. TG_ARGV[0]: colunas sensíveis, gravadas só como nome em changed_fields.
                CREATE FUNCTION audit.log_changes() RETURNS trigger
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, pg_temp AS $$
                DECLARE
                    v_ignored text[] := coalesce(TG_ARGV[0], '{}')::text[];
                    v_old     jsonb;
                    v_new     jsonb;
                    v_changed text[];
                    v_row     jsonb;
                BEGIN
                    IF TG_OP IN ('UPDATE', 'DELETE') THEN
                        v_old := to_jsonb(OLD);
                    END IF;
                    IF TG_OP IN ('INSERT', 'UPDATE') THEN
                        v_new := to_jsonb(NEW);
                    END IF;

                    IF TG_OP = 'UPDATE' THEN
                        SELECT array_agg(n.key ORDER BY n.key) INTO v_changed
                        FROM jsonb_each(v_new) n
                        WHERE n.value IS DISTINCT FROM v_old -> n.key;

                        -- UPDATE que não muda nada não é auditado.
                        IF v_changed IS NULL THEN
                            RETURN NULL;
                        END IF;
                    END IF;

                    v_row := coalesce(v_new, v_old);

                    INSERT INTO audit.change_log (
                        schema_name, table_name, row_id, operation, old_values, new_values, changed_fields,
                        transaction_id, session_user_name, application_user, client_addr, application_name,
                        tenant_id, frontend_url)
                    VALUES (
                        TG_TABLE_SCHEMA, TG_TABLE_NAME, v_row ->> 'id', TG_OP, v_old - v_ignored, v_new - v_ignored, v_changed,
                        pg_current_xact_id()::text::bigint, session_user,
                        nullif(current_setting('app.current_user', true), ''),
                        inet_client_addr(), nullif(current_setting('application_name', true), ''),
                        coalesce(nullif(current_setting('app.tenant_id', true), '')::uuid, (v_row ->> 'tenant_id')::uuid),
                        nullif(current_setting('app.frontend_url', true), ''));

                    RETURN NULL;
                END $$;

                CREATE FUNCTION audit.enable_tracking(p_table regclass, p_ignored_columns text[] DEFAULT '{}') RETURNS void
                LANGUAGE plpgsql AS $$
                BEGIN
                    EXECUTE format('DROP TRIGGER IF EXISTS audit_changes ON %s', p_table);
                    EXECUTE format(
                        'CREATE TRIGGER audit_changes AFTER INSERT OR UPDATE OR DELETE ON %s '
                        'FOR EACH ROW EXECUTE FUNCTION audit.log_changes(%L)',
                        p_table, p_ignored_columns);
                END $$;

                CREATE FUNCTION audit.disable_tracking(p_table regclass) RETURNS void
                LANGUAGE plpgsql AS $$
                BEGIN
                    EXECUTE format('DROP TRIGGER IF EXISTS audit_changes ON %s', p_table);
                END $$;

                SELECT audit.ensure_partitions(3);
                """);

            migrationBuilder.EnableAuditTracking("usuarios", "senha_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Os triggers dependem de audit.log_changes() e caem junto com o schema.
            migrationBuilder.Sql("DROP SCHEMA audit CASCADE;");
        }
    }
}
