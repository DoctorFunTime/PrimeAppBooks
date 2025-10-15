using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

public class TransactionsServices
{
    private readonly AppDbContext _context;

    public TransactionsServices(AppDbContext context)
    {
        _context = context;
    }

    #region Bills

    public async Task<Bill> CreateBillAsync(Bill bill)
    {
        bill.CreatedAt = DateTime.UtcNow;
        bill.UpdatedAt = DateTime.UtcNow;
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        return bill;
    }

    public async Task<List<Bill>> GetAllBillsAsync()
    {
        return await _context.Bills
            .Include(b => b.Payments)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Bill> GetBillByIdAsync(int billId)
    {
        return await _context.Bills
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == billId);
    }

    public async Task<Bill> UpdateBillAsync(Bill updatedBill)
    {
        var bill = await _context.Bills.FindAsync(updatedBill.BillId);
        if (bill == null) return null;

        bill.BillNumber = updatedBill.BillNumber;
        bill.VendorId = updatedBill.VendorId;
        bill.BillDate = updatedBill.BillDate;
        bill.DueDate = updatedBill.DueDate;
        bill.TotalAmount = updatedBill.TotalAmount;
        bill.TaxAmount = updatedBill.TaxAmount;
        bill.DiscountAmount = updatedBill.DiscountAmount;
        bill.NetAmount = updatedBill.NetAmount;
        bill.AmountPaid = updatedBill.AmountPaid;
        bill.Balance = updatedBill.Balance;
        bill.Status = updatedBill.Status;
        bill.Terms = updatedBill.Terms;
        bill.Notes = updatedBill.Notes;
        bill.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return bill;
    }

    public async Task<bool> DeleteBillAsync(int billId)
    {
        var bill = await _context.Bills.FindAsync(billId);
        if (bill == null) return false;

        _context.Bills.Remove(bill);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteBillsByVendorAsync(int vendorId)
    {
        var bills = await _context.Bills
            .Where(b => b.VendorId == vendorId)
            .ToListAsync();

        _context.Bills.RemoveRange(bills);
        return await _context.SaveChangesAsync();
    }

    public async Task<List<Bill>> GetOverdueBillsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Bills
            .Where(b => b.DueDate < today && b.Balance > 0 && b.Status != "PAID")
            .ToListAsync();
    }

    public async Task<bool> IsBillNumberUniqueAsync(string billNumber)
    {
        return !await _context.Bills.AnyAsync(b => b.BillNumber == billNumber);
    }

    public async Task<bool> ApplyPaymentToBillAsync(int billId, decimal paymentAmount)
    {
        var bill = await _context.Bills.FindAsync(billId);
        if (bill == null) return false;

        bill.AmountPaid += paymentAmount;
        bill.Balance = bill.NetAmount - bill.AmountPaid;
        bill.Status = bill.Balance <= 0 ? "PAID" : "PARTIAL";
        bill.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion Bills

    #region Payments

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<List<Payment>> GetAllPaymentsAsync()
    {
        return await _context.Payments
            .Include(p => p.Bill)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<Payment> GetPaymentByIdAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Bill)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<Payment> UpdatePaymentAsync(Payment updatedPayment)
    {
        var payment = await _context.Payments.FindAsync(updatedPayment.PaymentId);
        if (payment == null) return null;

        payment.PaymentNumber = updatedPayment.PaymentNumber;
        payment.PaymentDate = updatedPayment.PaymentDate;
        payment.VendorId = updatedPayment.VendorId;
        payment.BillId = updatedPayment.BillId;
        payment.PaymentMethod = updatedPayment.PaymentMethod;
        payment.Amount = updatedPayment.Amount;
        payment.ReferenceNumber = updatedPayment.ReferenceNumber;
        payment.Memo = updatedPayment.Memo;
        payment.Status = updatedPayment.Status;
        payment.BankAccountId = updatedPayment.BankAccountId;
        payment.ProcessedBy = updatedPayment.ProcessedBy;
        payment.ProcessedAt = updatedPayment.ProcessedAt;
        payment.CreatedBy = updatedPayment.CreatedBy;

        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> DeletePaymentAsync(int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Payment>> GetPaymentsByVendorAsync(int vendorId)
    {
        return await _context.Payments
            .Where(p => p.VendorId == vendorId)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetPaymentsByBillAsync(int billId)
    {
        return await _context.Payments
            .Where(p => p.BillId == billId)
            .ToListAsync();
    }

    public async Task<bool> MarkPaymentAsProcessedAsync(int paymentId, int userId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        payment.Status = "PROCESSED";
        payment.ProcessedBy = userId;
        payment.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion Payments
}