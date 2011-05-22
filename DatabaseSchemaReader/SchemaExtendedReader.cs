﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using DatabaseSchemaReader.Conversion;
using DatabaseSchemaReader.DataSchema;


namespace DatabaseSchemaReader
{
    /// <summary>
    /// Extended schema information beyond that included in GetSchema.
    /// </summary>
    internal class SchemaExtendedReader : SchemaReader
    {
        /// <summary>
        /// Constructor with connectionString and ProviderName
        /// </summary>
        /// <param name="connectionString">Eg "Data Source=localhost;Integrated Security=SSPI;Initial Catalog=Northwind;"</param>
        /// <param name="providerName">ProviderInvariantName for the provider (eg System.Data.SqlClient or System.Data.OracleClient)</param>
        public SchemaExtendedReader(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
        }

        /// <summary>
        /// Get all data for a specified table name.
        /// </summary>
        /// <param name="tableName">Name of the table. Oracle names can be case sensitive.</param>
        /// <returns>A dataset containing the tables: Columns, Primary_Keys, Foreign_Keys, Unique_Keys (only filled for Oracle), Indexes, IndexColumns, Triggers</returns>
        public override DataSet Table(string tableName)
        {
            //if (ProviderType != SqlType.SqlServer && ProviderType != SqlType.SqlServerCe && ProviderType != SqlType.Oracle && ProviderType != SqlType.MySql)
            //    return base.Table(tableName);
            //more information from sqlserver, oracle and mysql
            var ds = new DataSet();
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                //uses friend access to schemaReader
                LoadTable(tableName, ds, conn);
                if (ds.Tables.Count == 0) return null; //no data found
                if (string.IsNullOrEmpty(Owner))
                {
                    //we need schema for constraint look ups
                    Owner = SchemaConverter.FindSchema(ds.Tables["Columns"]);
                }

                ds.Tables.Add(PrimaryKeys(tableName, conn));
                ds.Tables.Add(ForeignKeys(tableName, conn));
                ds.Tables.Add(UniqueKeys(tableName, conn));
                ds.Tables.Add(CheckConstraints(tableName, conn));

                ds.Tables.Add(IdentityColumns(tableName, conn));

            }
            return ds;
        }

        public virtual DataTable IdentityColumns(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                return IdentityColumns(tableName, conn);
            }
        }
        protected virtual DataTable IdentityColumns(string tableName, DbConnection conn)
        {
            //override this if provider has identity
            DataTable dt = CreateDataTable("IdentityColumns");
            return dt;
        }

        protected override DataTable Triggers(string tableName, DbConnection conn)
        {
            return GenericCollection("Triggers", conn, tableName);
        }


        public virtual DataTable ProcedureSource(string name)
        {
            return CreateDataTable("ProcedureSource");
        }

        public virtual void PostProcessing(DatabaseTable databaseTable)
        {
            //override this if required
        }

        /// <summary>
        /// If there are no datatypes, call this to load them directly
        /// </summary>
        /// <returns></returns>
        public virtual List<DataType> SchemaDataTypes()
        {
            return new List<DataType>();
        }

        #region Constraints

        /// <summary>
        /// The Unique Key columns for a specific table  (if tableName is null or empty, all constraints are returned).
        /// </summary>
        public DataTable UniqueKeys(string tableName)
        {
            using (DbConnection connection = Factory.CreateConnection())
            {
                connection.ConnectionString = ConnectionString;
                connection.Open();
                return UniqueKeys(tableName, connection);
            }
        }
        protected virtual DataTable UniqueKeys(string tableName, DbConnection connection)
        {
            return GenericCollection("UniqueKeys", connection, tableName);
        }

        /// <summary>
        /// The check constraints for a specific table (if tableName is null or empty, all check constraints are returned)
        /// </summary>
        public virtual DataTable CheckConstraints(string tableName)
        {
            using (DbConnection connection = Factory.CreateConnection())
            {
                connection.ConnectionString = ConnectionString;
                connection.Open();
                return CheckConstraints(tableName, connection);
            }
        }
        protected virtual DataTable CheckConstraints(string tableName, DbConnection connection)
        {
            return GenericCollection("CheckConstraints", connection, tableName);
        }
        #endregion

        #region protected helpers

        protected virtual DataTable CommandForTable(string tableName, DbConnection conn, string collectionName, string sqlCommand)
        {
            DataTable dt = CreateDataTable(collectionName);

            //create a dataadaptor and fill it
            using (DbDataAdapter da = Factory.CreateDataAdapter())
            {
                da.SelectCommand = conn.CreateCommand();
                da.SelectCommand.CommandText = sqlCommand;
                AddTableNameSchemaParameters(da.SelectCommand, tableName);

                da.Fill(dt);
                return dt;
            }
        }
        protected DataTable CreateDataTable(string tableName)
        {
            DataTable dt = new DataTable(tableName);
            dt.Locale = CultureInfo.InvariantCulture;
            return dt;
        }
        protected DbParameter AddDbParameter(string parameterName, object value)
        {
            DbParameter parameter = Factory.CreateParameter();
            parameter.ParameterName = parameterName;
            //C# null should be DBNull
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        protected void AddTableNameSchemaParameters(DbCommand cmd, string tableName)
        {
            var parameter = AddDbParameter("tableName", tableName);
            //sqlserver ce is picky about null parameter types
            parameter.DbType = DbType.String;
            cmd.Parameters.Add(parameter);

            var schemaParameter = AddDbParameter("schemaOwner", Owner);
            schemaParameter.DbType = DbType.String;
            cmd.Parameters.Add(schemaParameter);
        }
        #endregion

    }
}

