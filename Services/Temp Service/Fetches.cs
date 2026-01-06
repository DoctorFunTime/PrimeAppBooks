using Npgsql;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Models.Temp_Models;
using System;
using System.Collections.Generic;
using System.Data;
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
            using (NpgsqlConnection conn = new NpgsqlConnection($"{AppConfig.GetConnectionString("SecondaryDatabase")}"))
            {
                conn.Open();
                using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter("SELECT " +
                    "std_id AS id, std_name AS Name, std_surname AS Surname, std_gender AS gender, std_class AS Class, std_dob AS DOB, std_address AS address, std_gdn_name AS gname, std_gdn_surname AS gsurname, std_gdn_phone_number AS contacts, std_join_date AS join_date, student_type AS type, is_transferred AS is_transferred, std_id_number As id_number, std_phone_number AS student_contacts, is_enrolled AS is_enrolled, std_discount_amount AS discount_amount FROM students_table ORDER BY std_id", conn))
                {
                    dataAdapter.Fill(table);
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
                };

                student.Add(studentItem);
            }

            return student;
        }
    }
}