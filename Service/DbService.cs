using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace quick_sql.Service
{
    public class DbService : IDisposable
    {
        private readonly string _connectionString;
        public string ConnectionMessages { get; private set; } = "";

        public DbService(string server, string database = "")
        {
            SqlConnectionStringBuilder builder = new()
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true
            };

            _connectionString = builder.ConnectionString;
        }

        public async Task<List<T>> QueryAsync<T>(string query, CancellationToken cancellationToken, Dictionary<string, object>? parameters = null) where T : new()
        {
            List<T> results = [];

            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;
                await connection.OpenAsync(cancellationToken);
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                PropertyInfo[] properties = typeof(T).GetProperties();

                while (await reader.ReadAsync(cancellationToken))
                {
                    T item = new();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        FillProperty(reader, properties, item, i, columnName);
                    }

                    results.Add(item);
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Operation cancelled by user"))
                {
                    throw;
                }
            }

            return results;
        }

        public async Task<DataTable> QueryAsync(string query, CancellationToken cancellationToken, Dictionary<string, object>? parameters = null)
        {
            var dataTable = new DataTable();
            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;

                await connection.OpenAsync(cancellationToken);
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                dataTable.Load(reader);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Operation cancelled by user"))
                {
                    throw;
                }
            }

            return dataTable;
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object>? parameters = null)
        {
            int ret = 0;

            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;
                connection.Open();
                ret = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Operation cancelled by user"))
                {
                    throw;
                }
            }

            return ret;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private static void FillProperty<T>(SqlDataReader reader, PropertyInfo[] properties, T item, int fieldIndex, string columnName) where T : new()
        {
            PropertyInfo? property = properties.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (property != null && property.CanWrite)
            {
                object? value = reader.IsDBNull(fieldIndex) ? GetDefaultValue(property.PropertyType) : reader.GetValue(fieldIndex);
                try
                {
                    if (property.PropertyType.IsEnum && value is string stringValue)
                    {
                        property.SetValue(item, System.Enum.Parse(property.PropertyType, stringValue));
                    }
                    else if (value != null && property.PropertyType.Name == "DateOnly" && value is DateTime dateTime)
                    {
                        DateOnly dateOnly = DateOnly.FromDateTime(dateTime);
                        property.SetValue(item, dateOnly);
                    }
                    else if (value != null && property.PropertyType != value.GetType())
                    {
                        property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                    }
                    else
                    {
                        property.SetValue(item, value);
                    }
                }
                catch
                {
                    property.SetValue(item, GetDefaultValue(property.PropertyType));
                }
            }
        }

        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            ConnectionMessages += e.Message + "\n";
        }

        private (SqlConnection Connection, SqlCommand Command) CreateConnectionAndCommand(string query, Dictionary<string, object>? parameters = null)
        {
            ConnectionMessages = string.Empty;
            SqlConnection connection = new(_connectionString);
            connection.InfoMessage += Connection_InfoMessage;
            SqlCommand command = new(query, connection)
            {
                CommandTimeout = 300
            };
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            return (connection, command);
        }
    }
}
