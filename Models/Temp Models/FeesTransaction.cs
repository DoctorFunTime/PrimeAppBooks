using System;

namespace PrimeAppBooks.Models.Temp_Models
{
    public class FeesTransaction
    {
        public int StudentId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DebitCredit { get; set; } // "DR" or "CR"
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public string DocNumber { get; set; }
    }
}
