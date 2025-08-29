using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Models.APIs
{
    public class CompanyInfoWrapper
    {
        public CompanyInfo CompanyInfo { get; set; }
    }

    public class CompanyInfo
    {
        public string CompanyName { get; set; }
        public string LegalName { get; set; }
        public Address CompanyAddr { get; set; }
    }

    public class Address
    {
        public string Line1 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}