using PrimeAppBooks.Interfaces;
using System;

namespace PrimeAppBooks.Services
{
    public class JournalNavigationService : IJournalNavigationService
    {
        private int _editingJournalId = 0;

        public int EditingJournalId
        {
            get => _editingJournalId;
            set => _editingJournalId = value;
        }

        public void SetEditingJournalId(int journalId)
        {
            _editingJournalId = journalId;
        }

        public void ClearEditingJournalId()
        {
            _editingJournalId = 0;
        }
    }
}
