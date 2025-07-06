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

        public List<T> Query<T>(string query, Dictionary<string, object>? parameters = null) where T : new()
        {
            List<T> results = [];

            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;
                connection.Open();
                using SqlDataReader reader = command.ExecuteReader();
                PropertyInfo[] properties = typeof(T).GetProperties();

                while (reader.Read())
                {
                    T item = new();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);

                        PropertyInfo? property = properties.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                        if (property != null && property.CanWrite)
                        {
                            object? value = reader.IsDBNull(i) ? GetDefaultValue(property.PropertyType) : reader.GetValue(i);
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

                    results.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query error: {ex.Message}", ex);
            }

            return results;
        }

        public DataTable Query(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;
                connection.Open();
                using SqlDataAdapter adapter = new(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"Database execution error: {ex.Message}", ex);
            }
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var cnn = CreateConnectionAndCommand(query, parameters);
                using SqlConnection connection = cnn.Connection;
                using SqlCommand command = cnn.Command;
                connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Database execution error: {ex.Message}", ex);
            }
        }

        public T? ExecuteScalar<T>(string query, Dictionary<string, object>? parameters = null)
        {
            using SqlConnection connection = new(_connectionString);
            using SqlCommand command = new(query, connection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            try
            {
                connection.Open();
                object? result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                throw new Exception($"Database scalar execution error: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
            SqlCommand command = new(query, connection);
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
