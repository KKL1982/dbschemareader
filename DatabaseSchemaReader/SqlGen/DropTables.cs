﻿using System.Text;
using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.SqlGen
{
    class DropTables
    {
        public static string Write(DatabaseSchema schema, ISqlFormatProvider formatter)
        {
            var sb = new StringBuilder();
            foreach (var table in schema.Tables)
            {
                foreach (var foreignKey in table.ForeignKeys)
                {
                    sb.AppendLine("-- ALTER TABLE " + formatter.Escape(table.Name) + " DROP FOREIGN KEY " + foreignKey.Name + ";");

                }
            }
            foreach (var table in schema.Tables)
            {
                sb.AppendLine("-- DROP TABLE " + formatter.Escape(table.Name) + " CASCADE CONSTRAINTS;");
            }
            return sb.ToString();
        }
    }
}