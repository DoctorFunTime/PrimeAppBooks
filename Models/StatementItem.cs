using System;

namespace PrimeAppBooks.Models
{
    public class StatementItem
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }
}
