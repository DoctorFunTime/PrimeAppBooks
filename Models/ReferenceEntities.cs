using System;

namespace PrimeAppBooks.Models
{
    public class Vendor
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string VendorCode { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string TaxId { get; set; }
        public int? DefaultExpenseAccountId { get; set; }
        public int? DefaultPaymentTermsId { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public string TaxId { get; set; }
        public int? DefaultRevenueAccountId { get; set; }
        public int? DefaultPaymentTermsId { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Auto-Invoice Configuration
        public bool IsAutoInvoiceEnabled { get; set; } = false;
        public string AutoInvoiceFrequency { get; set; } // Monthly, Weekly, Daily
        public int AutoInvoiceInterval { get; set; } = 1;
        public decimal AutoInvoiceAmount { get; set; }
        public DateTime? NextAutoInvoiceDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentMethod
    {
        public int MethodId { get; set; }
        public string MethodName { get; set; }
        public string MethodCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Currency
    {
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string Symbol { get; set; }
        public bool IsBaseCurrency { get; set; }
    }

    public class TaxRate
    {
        public int TaxRateId { get; set; }
        public string TaxName { get; set; }
        public string TaxCode { get; set; }
        public decimal Rate { get; set; }
        public string TaxType { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AccountingPeriod
    {
        public int PeriodId { get; set; }
        public string PeriodName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int FiscalYear { get; set; }
        public int PeriodNumber { get; set; }
        public bool IsClosed { get; set; } = false;
        public DateTime? ClosedAt { get; set; }
    }

    public class AccountingSetting
    {
        public int SettingId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
