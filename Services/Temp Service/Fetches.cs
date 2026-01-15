using Npgsql;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Models.Temp_Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.Temp_Service
{
    public class Fetches
    {
        private string _username = "Keith";

        public List<StudentSelection> GetAllStudentsTable()
        {
            var table = new DataTable();
            DateTime asOfDate = new DateTime(2026, 1, 1);

            using (NpgsqlConnection conn = new NpgsqlConnection($"{AppConfig.GetConnectionString("SecondaryDatabase")}"))
            {
                conn.Open();
                string query = @"
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
                        ), 0) AS opening_balance
                    FROM students_table s
                    ORDER BY s.std_id";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);
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
            // Implement conversion logic from DataTable to ClassList
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

        public decimal GetCashOpeningBalance(DateTime asOfDate)
        {
            decimal openingBalance = 0;
            using (NpgsqlConnection conn = new NpgsqlConnection($"{AppConfig.GetConnectionString("SecondaryDatabase")}"))
            {
                conn.Open();
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

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        openingBalance = Convert.ToDecimal(result);
                    }
                }
            }
            return openingBalance;
        }

        public decimal GetBankOpeningBalance(DateTime asOfDate)
        {
            decimal openingBalance = 0;
            using (NpgsqlConnection conn = new NpgsqlConnection($"{AppConfig.GetConnectionString("SecondaryDatabase")}"))
            {
                conn.Open();
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

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);
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
            using (NpgsqlConnection conn = new NpgsqlConnection($"{AppConfig.GetConnectionString("SecondaryDatabase")}"))
            {
                conn.Open();
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

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
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
                            
                            // Normalize amount to base currency (simple approach for import)
                            // Or keep original and rate. The prompt said "complete double entry", implying we want the converted values likely.
                            // The opening balance logic used "amount * rate". Let's do the same here to be consistent.
                            
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
    }
}
