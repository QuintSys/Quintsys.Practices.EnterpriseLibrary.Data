using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Quintsys.System;

namespace Quintsys.Practices.EnterpriseLibrary.Data.Sql
{
    /// <summary>
    ///     Represents a connection to a SQL Server database.
    ///     The connection can be optionally within a database transaction.
    /// </summary>
    public class TransactedConnection : IDisposable
    {
        private SqlConnection _connection;
        private bool _disposed;
        private SqlTransaction _transaction;

        /// <summary>
        ///     Initializes the <see cref="TransactedConnection" /> class.
        ///     Sets the provider factory for the static
        ///     <see cref="T:Microsoft.Practices.EnterpriseLibrary.Data.DatabaseFactory" />.
        /// </summary>
        static TransactedConnection()
        {
            DatabaseFactory.SetDatabaseProviderFactory(new DatabaseProviderFactory());
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Quintsys.Data.TransactedConnection" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public TransactedConnection(string connectionString = null)
        {
            Database = string.IsNullOrWhiteSpace(connectionString)
                ? (SqlDatabase) DatabaseFactory.CreateDatabase()
                : (SqlDatabase) DatabaseFactory.CreateDatabase(connectionString);
        }

        private SqlDatabase Database { get; }

        /// <summary>
        ///     Opens a transacted database connection.
        /// </summary>
        public void Begin(bool useDatabaseTransaction = true)
        {
            _connection = (SqlConnection) Database.CreateConnection();
            _connection.Open();

            if (useDatabaseTransaction)
                _transaction = _connection.BeginTransaction();

            _disposed = false;
        }

        /// <summary>
        ///     Commits and disposes the database transaction.
        /// </summary>
        public void CommitTransaction()
        {
            if (_disposed || _transaction == null)
            {
                return;
            }

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        /// <summary>
        ///     Creates a <see cref="T:System.Data.SqlClient.SqlCommand" /> for a stored procedure.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure.</param>
        /// <returns>
        ///     The <see cref="T:System.Data.SqlClient.SqlCommand" /> for the stored procedure.
        /// </returns>
        public SqlCommand GetStoredProcCommand(string storedProcedureName)
        {
            return (SqlCommand) Database.GetStoredProcCommand(storedProcedureName);
        }

        /// <summary>
        ///     Adds a new In <see cref="T:System.Data.Common.DbParameter" /> object to the given <paramref name="sqlCommand" />.
        /// </summary>
        /// <param name="sqlCommand">The commmand to add the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="sqlDbType">One of the <see cref="T:System.Data.SqlDbType" /> values.</param>
        /// <param name="value">The value of the parameter.</param>
        public void AddInParameter(SqlCommand sqlCommand, string name, SqlDbType sqlDbType, object value)
        {
            Database.AddInParameter(sqlCommand, name, sqlDbType, value);
        }

        /// <summary>
        ///     Adds a new Out <see cref="T:System.Data.Common.DbParameter" /> object to the given <paramref name="sqlCommand" />.
        /// </summary>
        /// <param name="sqlCommand">The command to add the out parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="sqlDbType">One of the <see cref="T:System.Data.SqlDbType" /> values.</param>
        /// <param name="size">The maximum size of the data within the column.</param>
        public void AddOutParameter(SqlCommand sqlCommand, string name, SqlDbType sqlDbType, int size = -1)
        {
            Database.AddOutParameter(sqlCommand, name, sqlDbType, size);
        }

        /// <summary>
        /// Gets a parameter value from a given <paramref name="sqlCommand" />.
        /// </summary>
        /// <param name="sqlCommand">The command that contains the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>
        /// The value of the parameter.
        /// </returns>
        public object GetParameterValue(SqlCommand sqlCommand, string name)
        {
            return Database.GetParameterValue(sqlCommand, name);
        }

        /// <summary>
        /// Gets a parameter value from a given <paramref name="sqlCommand" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlCommand">The sql command that contains the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <returns></returns>
        public T GetParameterValue<T>(SqlCommand sqlCommand, string name)
        {
            return GetParameterValue(sqlCommand, name).ChangeType<T>();
        }

        /// <summary>
        ///     Executes the <paramref name="sqlCommand" /> within a given
        ///     <see cref="T:System.Data.SqlClient.SqlTransaction" />, and returns the number of rows affected.
        /// </summary>
        /// <param name="sqlCommand">The SQL command.</param>
        /// <param name="commandTimeOut">
        ///     The wait time before terminating the attempt to execute a command and generating an error. The default is 180
        ///     seconds.
        /// </param>
        public int ExecuteNonQuery(SqlCommand sqlCommand, int commandTimeOut = 180)
        {
            sqlCommand.CommandTimeout = commandTimeOut;
            return _transaction != null
                ? Database.ExecuteNonQuery(sqlCommand, _transaction)
                : Database.ExecuteNonQuery(sqlCommand);
        }

        /// <summary>
        ///     Executes the <paramref name="sqlCommand" /> within a transaction and returns an
        ///     <see cref="T:System.Data.IDataReader" /> through which the result can be read.
        ///     It is the responsibility of the caller to close the connection and reader when finished.
        /// </summary>
        /// <param name="sqlCommand">The command that contains the query to execute.</param>
        /// <param name="commandTimeOut">The command time out.</param>
        /// <returns>
        ///     An <see cref="T:System.Data.IDataReader" /> object.
        /// </returns>
        public IDataReader ExecuteReader(SqlCommand sqlCommand, int commandTimeOut = 180)
        {
            sqlCommand.CommandTimeout = commandTimeOut;
            IDataReader dataReader = _transaction != null
                ? Database.ExecuteReader(sqlCommand, _transaction)
                : Database.ExecuteReader(sqlCommand);
            return dataReader;
        }

        /// <summary>
        ///     Releases the unmanaged resources used by the <see cref="T:Quintsys.Data.TransactedConnection" />.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Rollback();
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }
            }

            _disposed = true;
        }
    }
}