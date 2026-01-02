using System;

namespace PrimeAppBooks.Models
{
    public class PaymentTerm
    {
        public int PaymentTermId { get; set; }
        public string TermName { get; set; } // e.g. "Net 30"
        public int Days { get; set; } // e.g. 30
        public bool IsActive { get; set; } = true;
    }
}
