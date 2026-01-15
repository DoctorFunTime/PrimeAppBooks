using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.DbServices
{
    public class VendorServices
    {
        private readonly AppDbContext _context;

        public VendorServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vendor>> GetAllVendorsAsync()
        {
            return await _context.Vendors
                .OrderBy(v => v.VendorName)
                .ToListAsync();
        }

        public async Task<Vendor> GetVendorByIdAsync(int id)
        {
            return await _context.Vendors.FindAsync(id);
        }

        public async Task<Vendor> CreateVendorAsync(Vendor vendor)
        {
            vendor.CreatedAt = DateTime.UtcNow;
            vendor.UpdatedAt = DateTime.UtcNow;
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return vendor;
        }

        public async Task<Vendor> UpdateVendorAsync(Vendor vendor)
        {
            var existing = await _context.Vendors.FindAsync(vendor.VendorId);
            if (existing == null) throw new Exception("Vendor not found");

            existing.VendorName = vendor.VendorName;
            existing.VendorCode = vendor.VendorCode;
            existing.ContactPerson = vendor.ContactPerson;
            existing.Email = vendor.Email;
            existing.Phone = vendor.Phone;
            existing.Address = vendor.Address;
            existing.TaxId = vendor.TaxId;
            existing.DefaultExpenseAccountId = vendor.DefaultExpenseAccountId;
            existing.DefaultPaymentTermsId = vendor.DefaultPaymentTermsId;
            existing.Notes = vendor.Notes;
            existing.IsActive = vendor.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteVendorAsync(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return false;

            // Optional: Check if vendor has associated invoices before deleting
            var hasInvoices = await _context.PurchaseInvoices.AnyAsync(i => i.VendorId == id);
            if (hasInvoices) throw new Exception("Cannot delete vendor with associated purchase invoices.");

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateVendorCodeAsync()
        {
            var random = new Random();
            var datePart = DateTime.Now.ToString("yyMMdd");
            var randomPart = random.Next(1000, 9999);
            var code = $"V-{datePart}-{randomPart}";
            
            // Ensure uniqueness
            while (await _context.Vendors.AnyAsync(v => v.VendorCode == code))
            {
                randomPart = random.Next(1000, 9999);
                code = $"V-{datePart}-{randomPart}";
            }
            
            return code;
        }
    }
}
