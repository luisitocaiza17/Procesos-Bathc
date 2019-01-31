using System;
using System.Data;
using System.Data.Odbc;
using System.Reflection;
using System.Configuration;

namespace SW.Common
{
    public class TableAdapterHelper
    {
        public static OdbcTransaction BeginTransaction(object tableAdapter)
        {
            return BeginTransaction(tableAdapter, IsolationLevel.ReadCommitted);
        }

        public static OdbcTransaction BeginTransaction(object tableAdapter, IsolationLevel isolationLevel)
        {
            // get the table adapter's type
            Type type = tableAdapter.GetType();

            // get the connection on the adapter
            //OdbcConnection connection = GetConnection(tableAdapter);
            System.Data.Odbc.OdbcConnection connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);

            // make sure connection is open to start the transaction
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            // start a transaction on the connection
            OdbcTransaction transaction = connection.BeginTransaction(isolationLevel);

            // set the transaction on the table adapter
            SetTransaction(tableAdapter, transaction);

            return transaction;
        }

        /// <summary>
        /// Gets the connection from the specified table adapter.
        /// </summary>
        private static OdbcConnection GetConnection(object tableAdapter)
        {
            Type type = tableAdapter.GetType();
            PropertyInfo connectionProperty = type.GetProperty("Connection", BindingFlags.NonPublic | BindingFlags.Instance);
            OdbcConnection connection = (OdbcConnection)connectionProperty.GetValue(tableAdapter, null);
            return connection;
        }

        /// <summary>
        /// Sets the connection on the specified table adapter.
        /// </summary>
        private static void SetConnection(object tableAdapter, OdbcConnection connection)
        {
            Type type = tableAdapter.GetType();
            PropertyInfo connectionProperty = type.GetProperty("Connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionProperty.SetValue(tableAdapter, connection, null);
        }

        /// <summary>
        /// Enlists the table adapter in a transaction.
        /// </summary>
        public static void SetTransaction(object tableAdapter, OdbcTransaction transaction)
        {
            // get the table adapter's type
            Type type = tableAdapter.GetType();
            PropertyInfo adaptersProperty = type.GetProperty("Adapter", BindingFlags.NonPublic | BindingFlags.Instance);
            OdbcDataAdapter adaptador = (OdbcDataAdapter)adaptersProperty.GetValue(tableAdapter, null);

           

            if (transaction == null)
            {
                if (adaptador.SelectCommand != null)
                    adaptador.SelectCommand.Connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);
                if (adaptador.InsertCommand != null)
                    adaptador.InsertCommand.Connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);
                if (adaptador.UpdateCommand != null)
                    adaptador.UpdateCommand.Connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);
                if (adaptador.DeleteCommand != null)
                    adaptador.DeleteCommand.Connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);

                PropertyInfo connectionProperty = type.GetProperty("Connection", BindingFlags.NonPublic | BindingFlags.Instance);
                connectionProperty.SetValue(tableAdapter, new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString), null);
                return;
            }

            //Conection
            PropertyInfo connectionProperty2 = type.GetProperty("Connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionProperty2.SetValue(tableAdapter, transaction.Connection, null);

            // set the transaction on each command in the adapter
            PropertyInfo commandsProperty = type.GetProperty("CommandCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            OdbcCommand[] commands = (OdbcCommand[])commandsProperty.GetValue(tableAdapter, null);
            foreach (OdbcCommand command in commands)
            {
                command.Transaction = transaction;
                command.CommandTimeout = 3 * 60;
            }

            //PropertyInfo adaptersProperty = type.GetProperty("Adapter", BindingFlags.NonPublic | BindingFlags.Instance);
            //OdbcDataAdapter adaptador = (OdbcDataAdapter)adaptersProperty.GetValue(tableAdapter, null);
            if (!(adaptador.InsertCommand == null)) { adaptador.InsertCommand.Transaction = transaction; adaptador.InsertCommand.CommandTimeout = 3 * 60; }
            if (!(adaptador.UpdateCommand == null)) { adaptador.UpdateCommand.Transaction = transaction; adaptador.UpdateCommand.CommandTimeout = 3 * 60; }
            if (!(adaptador.DeleteCommand == null)) { adaptador.DeleteCommand.Transaction = transaction; adaptador.DeleteCommand.CommandTimeout = 3 * 60; }
            if (!(adaptador.SelectCommand == null)) { adaptador.SelectCommand.Transaction = transaction; adaptador.SelectCommand.CommandTimeout = 3 * 60; }
            // set the connection on the table adapter

            //System.Data.SqlClient.OdbcConnection connection = new OdbcConnection(ConfigurationManager.ConnectionStrings["SW.Salud.DataAccess.Properties.Settings.ConnectionString"].ConnectionString);
            //SetConnection(tableAdapter, transaction.Connection); //connection); //
            //Type type = tableAdapter.GetType();
            
        }
    }
}