using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Models.Temp_Models
{
    public class StudentSelection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string StudentClass { get; set; } = string.Empty;
        public string StudentType { get; set; } = string.Empty;
        public string StudentContacts { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public bool isTransferred { get; set; }
        public bool isTransferToExam { get; set; } = false;
        public bool IsEnrolled { get; set; } = true;
        public DateTime? TransferDate { get; set; }
        public DateTime DOB { get; set; }
        public string FullName => $"{Name} {Surname}";
        public string ContactDetails => string.Join(" AND ", new[] { StudentContacts, Contacts }.Where(s => !string.IsNullOrEmpty(s)));
        public DateTime JoinDate { get; set; }

        public string GuardianName { get; set; } = string.Empty;
        public string GuardianSurname { get; set; } = string.Empty;
        public string Contacts { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int DiscountAmount { get; set; } = 0;
    }
}