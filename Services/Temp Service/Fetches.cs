using Npgsql;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Models.Temp_Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.Temp_Service
{
    public class Fetches
    {
        private const int ConnectionTimeoutSeconds = 10;
        private const int CommandTimeoutSeconds = 120;
        private string _username = "Keith";

        /// <summary>
        /// Gets or sets the connection string to use for database operations.
        /// If not set, the default connection string from AppConfig will be used.
        /// </summary>
        public string ConnectionString { get; set; }
        public string LastConnectionErrorMessage { get; private set; } = string.Empty;
        public bool HasConnectionError => !string.IsNullOrWhiteSpace(LastConnectionErrorMessage);

        private string GetConnectionString()
        {
            var connStr = !string.IsNullOrEmpty(ConnectionString) 
                ? ConnectionString 
                : AppConfig.GetConnectionString("SecondaryDatabaseV18");

            return NormalizeConnectionString(connStr);
        }

        public static string NormalizeConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = ConnectionTimeoutSeconds,
                CommandTimeout = CommandTimeoutSeconds
            };

            return builder.ConnectionString;
        }

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(GetConnectionString());

            try
            {
                LastConnectionErrorMessage = string.Empty;
                conn.Open();
                return conn;
            }
            catch (Exception ex) when (IsConnectionFailure(ex))
            {
                conn.Dispose();
                LastConnectionErrorMessage = CreateConnectionFailureMessage();
                Debug.WriteLine($"Academy connection failed: {ex}");
                return null;
            }
        }

        private static NpgsqlCommand CreateCommand(string query, NpgsqlConnection conn)
        {
            return new NpgsqlCommand(query, conn)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
        }

        public bool TryTestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                LastConnectionErrorMessage = string.Empty;
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();
                return true;
            }
            catch (Exception ex) when (IsConnectionFailure(ex))
            {
                errorMessage = CreateConnectionFailureMessage();
                LastConnectionErrorMessage = errorMessage;
                Debug.WriteLine($"Academy connection test failed: {ex}");
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Connection failed: {ex.Message}";
                LastConnectionErrorMessage = errorMessage;
                Debug.WriteLine($"Academy connection test failed: {ex}");
                return false;
            }
        }

        private static string CreateConnectionFailureMessage() =>
            $"Could not connect to the Academy database within {ConnectionTimeoutSeconds} seconds. " +
            "Please check the host, port, database name, username/password, network/VPN, and whether PostgreSQL is running.";

        private static bool IsConnectionFailure(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is TimeoutException ||
                    current is SocketException ||
                    current is IOException ||
                    current is NpgsqlException)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all students with optional opening balance calculation as of a specific date
        /// </summary>
        /// <param name="asOfDate">Date to calculate opening balance. If null, no opening balance is calculated.</param>
        public List<StudentSelection> GetAllStudentsTable(DateTime? asOfDate = null)
        {
            var table = new DataTable();

            using (NpgsqlConnection conn = OpenConnection())
            {
                if (conn == null) return ConvertToStudentList(table);

                // Build query with conditional opening balance calculation
                string openingBalanceQuery = asOfDate.HasValue ? @"
                        COALESCE((
                            SELECT SUM(
                                (CASE WHEN t.fs_debit_credit = 'DR' THEN t.fs_debit ELSE -t.fs_credit END)
                                * COALESCE(c.exchange_rate, 1.0)
                            )
                            FROM fees_statement t
                            LEFT JOIN LATERAL (
                                SELECT c_sub.exchange_rate
                                FROM conversions c_sub
                                WHERE c_sub.currency_code = t.fs_currency_code
                                  AND c_sub.rate_date <= t.fs_date
                                ORDER BY c_sub.rate_date DESC
                                LIMIT 1
                            ) c ON true
                            WHERE t.fs_std_id = s.std_id
                              AND t.fs_date < @AsOfDate
                        ), 0) AS opening_balance" : "0 AS opening_balance";

                string query = $@"
                    SELECT
                        s.std_id AS id,
                        s.std_name AS Name,
                        s.std_surname AS Surname,
                        s.std_gender AS gender,
                        s.std_class AS Class,
                        s.std_dob AS DOB,
                        s.std_address AS address,
                        s.std_gdn_name AS gname,
                        s.std_gdn_surname AS gsurname,
                        s.std_gdn_phone_number AS contacts,
                        s.std_join_date AS join_date,
                        s.student_type AS type,
                        s.is_transferred AS is_transferred,
                        s.std_id_number As id_number,
                        s.std_phone_number AS student_contacts,
                        s.is_enrolled AS is_enrolled,
                        s.std_discount_amount AS discount_amount,
                        {openingBalanceQuery}
                    FROM students_table s
                    ORDER BY s.std_id";

                using (NpgsqlCommand cmd = CreateCommand(query, conn))
                {
                    if (asOfDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@AsOfDate", asOfDate.Value);
                    }

                    using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(cmd))
                    {
                        dataAdapter.Fill(table);
                    }
                }
            }

            return ConvertToStudentList(table);
        }

        private List<StudentSelection> ConvertToStudentList(DataTable table)
        {
            List<StudentSelection> student = new List<StudentSelection>();

            foreach (DataRow row in table.Rows)
            {
                var studentItem = new StudentSelection
                {
                    Id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Surname = row["Surname"]?.ToString() ?? string.Empty,
                    Gender = row["gender"]?.ToString() ?? string.Empty,
                    StudentClass = row["Class"]?.ToString() ?? string.Empty,
                    DOB = row["DOB"] != DBNull.Value ? Convert.ToDateTime(row["DOB"]) : DateTime.MinValue,
                    JoinDate = row["join_date"] != DBNull.Value ? Convert.ToDateTime(row["join_date"]) : DateTime.MinValue,
                    GuardianName = row["gname"]?.ToString() ?? string.Empty,
                    GuardianSurname = row["gsurname"]?.ToString() ?? string.Empty,
                    Address = row["address"]?.ToString() ?? string.Empty,
                    Contacts = row["contacts"]?.ToString() ?? string.Empty,
                    StudentType = row["type"]?.ToString() ?? string.Empty,
                    isTransferred = row["is_transferred"] != DBNull.Value ? Convert.ToBoolean(row["is_transferred"]) : false,
                    IsEnrolled = row["is_enrolled"] != DBNull.Value ? Convert.ToBoolean(row["is_enrolled"]) : true,
                    IDNumber = row["id_number"]?.ToString() ?? string.Empty,
                    StudentContacts = row["student_contacts"]?.ToString() ?? string.Empty,
                    DiscountAmount = row["discount_amount"] != DBNull.Value ? Convert.ToInt32(row["discount_amount"]) : 0,
                    OpeningBalance = row["opening_balance"] != DBNull.Value ? Convert.ToDecimal(row["opening_balance"]) : 0,
                };

                student.Add(studentItem);
            }

            return student;
        }

        /// <summary>
        /// Gets cash opening balance as of a specific date. Returns 0 if asOfDate is null.
        /// </summary>
        public decimal GetCashOpeningBalance(DateTime? asOfDate)
        {
            if (!asOfDate.HasValue) return 0;

            decimal openingBalance = 0;
            using (NpgsqlConnection conn = OpenConnection())
            {
                if (conn == null) return openingBalance;

                string query = @"
                    SELECT
                        COALESCE(SUM(
                            (CASE WHEN t.cb_debit_credit = 'DR' THEN t.cb_debit ELSE -t.cb_credit END)
                            * COALESCE(c.exchange_rate, 1.0)
                        ), 0) AS opening_balance
                    FROM cashbook t
                    LEFT JOIN LATERAL (
                        SELECT c_sub.exchange_rate
                        FROM conversions c_sub
                        WHERE c_sub.currency_code = t.cb_currency_code
                          AND c_sub.rate_date <= t.cb_date
                        ORDER BY c_sub.rate_date DESC
                        LIMIT 1
                    ) c ON true
                    WHERE t.cb_type = 'Cash'
                      AND t.cb_date < @AsOfDate";

                using (NpgsqlCommand cmd = CreateCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate.Value);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        openingBalance = Convert.ToDecimal(result);
                    }
                }
            }
            return openingBalance;
        }

        /// <summary>
        /// Gets bank opening balance as of a specific date. Returns 0 if asOfDate is null.
        /// </summary>
        public decimal GetBankOpeningBalance(DateTime? asOfDate)
        {
            if (!asOfDate.HasValue) return 0;

            decimal openingBalance = 0;
            using (NpgsqlConnection conn = OpenConnection())
            {
                if (conn == null) return openingBalance;

                string query = @"
                    SELECT
                        COALESCE(SUM(
                            (CASE WHEN t.bk_debit_credit = 'DR' THEN t.bk_debit ELSE -t.bk_credit END)
                            * COALESCE(c.exchange_rate, 1.0)
                        ), 0) AS opening_balance
                    FROM bank_book t
                    LEFT JOIN LATERAL (
                        SELECT c_sub.exchange_rate
                        FROM conversions c_sub
                        WHERE c_sub.currency_code = t.bk_currency_code
                          AND c_sub.rate_date <= t.bk_date
                        ORDER BY c_sub.rate_date DESC
                        LIMIT 1
                    ) c ON true
                    WHERE t.bk_type != 'Cash'
                      AND t.bk_date < @AsOfDate";

                using (NpgsqlCommand cmd = CreateCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate.Value);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        openingBalance = Convert.ToDecimal(result);
                    }
                }
            }
            return openingBalance;
        }

        public List<FeesTransaction> GetStudentTransactions(DateTime fromDate)
        {
            var transactions = new List<FeesTransaction>();
            using (NpgsqlConnection conn = OpenConnection())
            {
                if (conn == null) return transactions;

                string query = @"
                    SELECT
                        t.fs_std_id,
                        t.fs_date,
                        t.fs_debit_credit,
                        t.fs_debit,
                        t.fs_credit,
                        t.fs_description,
                        t.fs_doc_number,
                        t.fs_currency_code,
                        COALESCE(c.exchange_rate, 1.0) as exchange_rate
                    FROM fees_statement t
                    LEFT JOIN LATERAL (
                        SELECT c_sub.exchange_rate
                        FROM conversions c_sub
                        WHERE c_sub.currency_code = t.fs_currency_code
                          AND c_sub.rate_date <= t.fs_date
                        ORDER BY c_sub.rate_date DESC
                        LIMIT 1
                    ) c ON true
                    WHERE t.fs_date >= @FromDate
                    ORDER BY t.fs_date";

                using (NpgsqlCommand cmd = CreateCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debit = reader["fs_debit"] != DBNull.Value ? Convert.ToDecimal(reader["fs_debit"]) : 0;
                            var credit = reader["fs_credit"] != DBNull.Value ? Convert.ToDecimal(reader["fs_credit"]) : 0;
                            var type = reader["fs_debit_credit"]?.ToString();
                            var rate = reader["exchange_rate"] != DBNull.Value ? Convert.ToDecimal(reader["exchange_rate"]) : 1.0m;

                            decimal amount = 0;
                            if (type == "DR") amount = debit * rate;
                            else amount = credit * rate;

                            transactions.Add(new FeesTransaction
                            {
                                StudentId = reader["fs_std_id"] != DBNull.Value ? Convert.ToInt32(reader["fs_std_id"]) : 0,
                                TransactionDate = reader["fs_date"] != DBNull.Value ? Convert.ToDateTime(reader["fs_date"]) : DateTime.MinValue,
                                DebitCredit = type,
                                Amount = amount,
                                Description = reader["fs_description"]?.ToString(),
                                DocNumber = reader["fs_doc_number"]?.ToString(),
                                CurrencyCode = reader["fs_currency_code"]?.ToString(),
                                ExchangeRate = rate
                            });
                        }
                    }
                }
            }
            return transactions;
        }
        public List<StudentPlanImport> GetStudentPlans()
        {
            var plans = new List<StudentPlanImport>();
            using (NpgsqlConnection conn = OpenConnection())
            {
                if (conn == null) return plans;

                string query = @"
                    SELECT
                        plan_std_id as student_id,
                        plan_on_plan as on_plan,
                        plan_description as description,
                        plan_follow_up_date as follow_up_date,
                        plan_status as status
                    FROM student_plans
                    WHERE plan_on_plan = true";

                using (NpgsqlCommand cmd = CreateCommand(query, conn))
                {
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            plans.Add(new StudentPlanImport
                            {
                                StudentId = reader.GetInt32(0),
                                OnPlan = !reader.IsDBNull(1) && reader.GetBoolean(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                FollowUpDate = reader.IsDBNull(3) ? null : (DateTime?)DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
                                Status = reader.IsDBNull(4) ? null : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return plans;
        }

        public class StudentPlanImport
        {
            public int StudentId { get; set; }
            public bool OnPlan { get; set; }
            public string Description { get; set; }
            public DateTime? FollowUpDate { get; set; }
            public string Status { get; set; }
        }
    }
}
