using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Infrastructure.Persistence.Naming;

/// <summary>
/// Applies snake_case naming to tables, columns, keys, FKs, and indexes in the model.
/// Called after all <c>IEntityTypeConfiguration</c> have run so any explicit
/// <c>ToTable</c>/<c>HasColumnName</c> overrides remain intact.
/// </summary>
public static class SnakeCaseNamingExtensions
{
    public static ModelBuilder ApplySnakeCaseNaming(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (!string.IsNullOrEmpty(table))
                entity.SetTableName(SnakeCaseHelper.ToSnake(table));

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                    property.SetColumnName(SnakeCaseHelper.ToSnake(columnName));
            }

            foreach (var key in entity.GetKeys())
            {
                var name = key.GetName();
                if (!string.IsNullOrEmpty(name))
                    key.SetName(SnakeCaseHelper.ToSnake(name));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.GetConstraintName();
                if (!string.IsNullOrEmpty(name))
                    fk.SetConstraintName(SnakeCaseHelper.ToSnake(name));
            }

            foreach (var index in entity.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(name))
                    index.SetDatabaseName(SnakeCaseHelper.ToSnake(name));
            }
        }
        return modelBuilder;
    }
}
