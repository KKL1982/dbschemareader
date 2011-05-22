﻿using System.Linq;
using System.Text;
using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.SqlGen.PostgreSql
{
    class TableGenerator : TableGeneratorBase
    {
        private bool _hasBit;
        protected DataTypeWriter DataTypeWriter;

        public TableGenerator(DatabaseTable table)
            : base(table)
        {
            DataTypeWriter = new DataTypeWriter();
        }

        protected override string ConstraintWriter()
        {
            var sb = new StringBuilder();
            var constraintWriter = CreateConstraintWriter();

            if (Table.PrimaryKey != null)
            {
                sb.AppendLine(constraintWriter.WritePrimaryKey());
            }

            sb.AppendLine(constraintWriter.WriteUniqueKeys());
            //looks like a boolean check, skip it
            constraintWriter.CheckConstraintExcluder = check => (_hasBit && check.Expression.Contains(" IN (0, 1)"));
            sb.AppendLine(constraintWriter.WriteCheckConstraints());

            AddIndexes(sb);

            return sb.ToString();
        }
        private ConstraintWriter CreateConstraintWriter()
        {
            return new ConstraintWriter(Table) { IncludeSchema = IncludeSchema };
        }
        //protected virtual IMigrationGenerator CreateMigrationGenerator()
        //{
        //    return new SqlServerMigrationGenerator();
        //}
        private void AddIndexes(StringBuilder sb)
        {
            if (!Table.Indexes.Any()) return;

            //var migration = CreateMigrationGenerator();
            //foreach (var index in Table.Indexes)
            //{
            //    if(index.IsUnqiueKeyIndex(Table)) continue;

            //    sb.AppendLine(migration.AddIndex(Table, index));
            //}
        }

        protected override ISqlFormatProvider SqlFormatProvider()
        {
            return new SqlFormatProvider();
        }

        protected override string WriteDataType(DatabaseColumn column)
        {

            var defaultValue = string.Empty;
            if (!string.IsNullOrEmpty(column.DefaultValue))
            {
                var defaultConstraint = " CONSTRAINT [DF_" + TableName + "_" + column.Name + "] DEFAULT ";
                var dataType = column.DbDataType.ToUpperInvariant();
                if (dataType == "VARCHAR" || dataType == "TEXT" || dataType == "CHAR")
                {
                    defaultValue = defaultConstraint + "'" + column.DefaultValue + "'";
                }
                else //numeric default
                {
                    defaultValue = defaultConstraint + column.DefaultValue;
                }
                defaultValue = " " + defaultValue;
            }

            var sql = DataTypeWriter.DataType(column);
            if (sql == "BIT") _hasBit = true;


            if (column.IsIdentity) sql = " SERIAL";
            if (column.IsPrimaryKey)
                sql += " NOT NULL";
            else
                sql += " " + (!column.Nullable ? " NOT NULL" : string.Empty) + defaultValue;
            return sql;
        }

        protected override string NonNativeAutoIncrementWriter()
        {
            return string.Empty;
        }
    }
}
