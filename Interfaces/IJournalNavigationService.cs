using System;

namespace PrimeAppBooks.Interfaces
{
    public interface IJournalNavigationService
    {
        int EditingJournalId { get; set; }
        void SetEditingJournalId(int journalId);
        void ClearEditingJournalId();
    }
}
