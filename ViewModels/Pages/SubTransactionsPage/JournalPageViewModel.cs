using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages.SubTransactionsPage
{
    public partial class JournalPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly BoxServices _messageBoxService = new();
        private readonly JournalServices _journalServices;
        private readonly IJournalNavigationService _journalNavigationService;

        private JournalEntry _editingJournalEntry;

        public JournalPageViewModel(INavigationService navigationService, JournalServices journalServices, IJournalNavigationService journalNavigationService)
        {
            _navigationService = navigationService;
            _journalServices = journalServices;
            _journalNavigationService = journalNavigationService;
            _navigationService.PageNavigated += OnPageNavigated;

            // Initialize asynchronously with proper error handling
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadAccountsAsync();
                await LoadTemplatesAsync();

                // Use Dispatcher to update UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Check if there's a journal ID to edit from the injected navigation service
                    var journalId = _journalNavigationService.EditingJournalId;
                    if (journalId > 0)
                    {
                        // Load the journal entry for editing
                        _ = LoadJournalEntryForEditingAsync(journalId);
                        // Clear the journal ID after using it
                        _journalNavigationService.ClearEditingJournalId();
                    }
                    else
                    {
                        AddLine(); // Start with one empty line
                    }
                });
            }
            catch (Exception ex)
            {
                // Show error message to user
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Failed to initialize: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        #region Properties

        private string _entryDescription = string.Empty;

        public string EntryDescription
        {
            get => _entryDescription;
            set
            {
                if (SetProperty(ref _entryDescription, value))
                {
                    OnEntryDescriptionChanged(value);
                }
            }
        }

        private string _referenceNumber = string.Empty;

        public string ReferenceNumber
        {
            get => _referenceNumber;
            set => SetProperty(ref _referenceNumber, value);
        }

        private DateTime _entryDate = DateTime.Today;

        public DateTime EntryDate
        {
            get => _entryDate;
            set
            {
                if (SetProperty(ref _entryDate, value))
                {
                    OnEntryDateChanged(value);
                }
            }
        }

        private string _currentStatus = "DRAFT";

        public string CurrentStatus
        {
            get => _currentStatus;
            set => SetProperty(ref _currentStatus, value);
        }

        private ObservableCollection<JournalLineViewModel> _journalLines = new();

        public ObservableCollection<JournalLineViewModel> JournalLines
        {
            get => _journalLines;
            set => SetProperty(ref _journalLines, value);
        }

        private JournalLineViewModel _selectedLine;

        public JournalLineViewModel SelectedLine
        {
            get => _selectedLine;
            set => SetProperty(ref _selectedLine, value);
        }

        private ObservableCollection<ChartOfAccount> _availableAccounts = new();

        public ObservableCollection<ChartOfAccount> AvailableAccounts
        {
            get => _availableAccounts;
            set => SetProperty(ref _availableAccounts, value);
        }

        private ObservableCollection<ChartOfAccount> _filteredAccounts = new();

        public ObservableCollection<ChartOfAccount> FilteredAccounts
        {
            get => _filteredAccounts;
            set => SetProperty(ref _filteredAccounts, value);
        }

        private string _accountSearchText = string.Empty;

        public string AccountSearchText
        {
            get => _accountSearchText;
            set
            {
                if (SetProperty(ref _accountSearchText, value))
                {
                    OnAccountSearchTextChanged(value);
                }
            }
        }

        private ChartOfAccount _selectedQuickAccount;

        public ChartOfAccount SelectedQuickAccount
        {
            get => _selectedQuickAccount;
            set => SetProperty(ref _selectedQuickAccount, value);
        }

        private ObservableCollection<JournalTemplate> _entryTemplates = new();

        public ObservableCollection<JournalTemplate> EntryTemplates
        {
            get => _entryTemplates;
            set => SetProperty(ref _entryTemplates, value);
        }

        private JournalTemplate _selectedTemplate;

        public JournalTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    OnSelectedTemplateChanged(value);
                }
            }
        }

        private ObservableCollection<ChartOfAccount> _recentAccounts = new();

        public ObservableCollection<ChartOfAccount> RecentAccounts
        {
            get => _recentAccounts;
            set => SetProperty(ref _recentAccounts, value);
        }

        private ChartOfAccount _selectedRecentAccount;

        public ChartOfAccount SelectedRecentAccount
        {
            get => _selectedRecentAccount;
            set => SetProperty(ref _selectedRecentAccount, value);
        }

        private ObservableCollection<string> _validationErrors = new();

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        private string _descriptionValidationError = string.Empty;

        public string DescriptionValidationError
        {
            get => _descriptionValidationError;
            set => SetProperty(ref _descriptionValidationError, value);
        }

        private string _dateValidationError = string.Empty;

        public string DateValidationError
        {
            get => _dateValidationError;
            set => SetProperty(ref _dateValidationError, value);
        }

        private bool _hasDescriptionError = false;

        public bool HasDescriptionError
        {
            get => _hasDescriptionError;
            set => SetProperty(ref _hasDescriptionError, value);
        }

        private bool _hasDateError = false;

        public bool HasDateError
        {
            get => _hasDateError;
            set => SetProperty(ref _hasDateError, value);
        }

        private bool _hasValidationErrors = false;

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        private bool _hasValidLines = false;

        public bool HasValidLines
        {
            get => _hasValidLines;
            set => SetProperty(ref _hasValidLines, value);
        }

        private bool _hasAccountResults = false;

        public bool HasAccountResults
        {
            get => _hasAccountResults;
            set => SetProperty(ref _hasAccountResults, value);
        }

        private bool _hasSelectedTemplate = false;

        public bool HasSelectedTemplate
        {
            get => _hasSelectedTemplate;
            set => SetProperty(ref _hasSelectedTemplate, value);
        }

        private bool _canSaveAsTemplate = false;

        public bool CanSaveAsTemplate
        {
            get => _canSaveAsTemplate;
            set => SetProperty(ref _canSaveAsTemplate, value);
        }

        private bool _canPost = false;

        public bool CanPost
        {
            get => _canPost;
            set => SetProperty(ref _canPost, value);
        }

        private bool _isBalanced = false;

        public bool IsBalanced
        {
            get => _isBalanced;
            set => SetProperty(ref _isBalanced, value);
        }

        private decimal _totalDebits = 0;

        public decimal TotalDebits
        {
            get => _totalDebits;
            set => SetProperty(ref _totalDebits, value);
        }

        private decimal _totalCredits = 0;

        public decimal TotalCredits
        {
            get => _totalCredits;
            set => SetProperty(ref _totalCredits, value);
        }

        private decimal _difference = 0;

        public decimal Difference
        {
            get => _difference;
            set => SetProperty(ref _difference, value);
        }

        private Brush _differenceColor = Brushes.Red;

        public Brush DifferenceColor
        {
            get => _differenceColor;
            set => SetProperty(ref _differenceColor, value);
        }

        private string _balanceStatus = "UNBALANCED";

        public string BalanceStatus
        {
            get => _balanceStatus;
            set => SetProperty(ref _balanceStatus, value);
        }

        private string _balanceIndicator = "❌ UNBALANCED";

        public string BalanceIndicator
        {
            get => _balanceIndicator;
            set => SetProperty(ref _balanceIndicator, value);
        }

        private string _entrySummary = string.Empty;

        public string EntrySummary
        {
            get => _entrySummary;
            set => SetProperty(ref _entrySummary, value);
        }

        private string _entryPreview = string.Empty;

        public string EntryPreview
        {
            get => _entryPreview;
            set => SetProperty(ref _entryPreview, value);
        }

        public bool CanGoBack => _navigationService.CanGoBack;

        #endregion Properties

        #region Commands

        [RelayCommand]
        private void NavigateToJournalPage() => _navigationService.NavigateTo<JournalPage>();

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        [RelayCommand]
        private void AddLine()
        {
            var newLine = new JournalLineViewModel
            {
                LineNumber = JournalLines.Count + 1,
                LineDate = EntryDate,
                Description = string.Empty,
                DebitAmount = 0,
                CreditAmount = 0,
                CreatedBy = 1 // TODO: Get from current user
            };

            // Subscribe to amount changes to update calculations immediately
            newLine.AmountChanged += OnLineAmountChanged;

            JournalLines.Add(newLine);
            SelectedLine = newLine;
            UpdateCalculations();
        }

        [RelayCommand]
        private void RemoveLine(JournalLineViewModel line)
        {
            if (line != null && JournalLines.Contains(line))
            {
                // Unsubscribe from amount changes before removing
                line.AmountChanged -= OnLineAmountChanged;
                JournalLines.Remove(line);
                RenumberLines();
                UpdateCalculations();
            }
        }

        [RelayCommand]
        private async Task SaveDraft()
        {
            if (!ValidateEntry())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Please fix validation errors before saving", "Validation Error", "ErrorOutline");
                });
                return;
            }

            try
            {
                var journalEntry = new JournalEntry
                {
                    JournalId = _editingJournalEntry?.JournalId ?? 0,
                    JournalNumber = string.IsNullOrEmpty(ReferenceNumber) ? null : ReferenceNumber,
                    JournalDate = EntryDate.Kind == DateTimeKind.Local ? EntryDate.ToUniversalTime() : EntryDate,
                    Description = EntryDescription,
                    JournalType = "GENERAL",
                    Status = "DRAFT",
                    CreatedBy = 1, // TODO: Get from current user
                    JournalLines = JournalLines.Where(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0))
                        .Select(l => new JournalLine
                        {
                            AccountId = l.AccountId,
                            LineDate = l.LineDate.Kind == DateTimeKind.Local ? l.LineDate.ToUniversalTime() : l.LineDate,
                            DebitAmount = l.DebitAmount,
                            CreditAmount = l.CreditAmount,
                            Description = l.Description,
                            Reference = l.Reference,
                            CostCenterId = l.CostCenterId,
                            ProjectId = l.ProjectId,
                            CreatedBy = l.CreatedBy
                        }).ToList()
                };

                if (_editingJournalEntry != null)
                {
                    // Update existing journal entry
                    await _journalServices.UpdateJournalEntryAsync(journalEntry);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("Journal entry updated successfully!", "Success", "CheckCircleOutline");
                    });
                }
                else
                {
                    // Create new journal entry
                    await _journalServices.CreateJournalEntryAsync(journalEntry);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("Journal entry saved as draft successfully!", "Success", "CheckCircleOutline");
                    });
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error saving draft: {ex.Message}", "Error", "ErrorOutline");
                });
                System.Diagnostics.Debug.WriteLine($"Error saving draft: {ex}");
            }
        }

        [RelayCommand]
        private async Task PostEntry()
        {
            if (!ValidateEntry())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Please fix validation errors before posting", "Validation Error", "ErrorOutline");
                });
                return;
            }

            if (!IsBalanced)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Journal entry must be balanced before posting", "Balance Error", "ErrorOutline");
                });
                return;
            }

            try
            {
                var journalEntry = new JournalEntry
                {
                    JournalNumber = string.IsNullOrEmpty(ReferenceNumber) ? null : ReferenceNumber,
                    JournalDate = EntryDate.Kind == DateTimeKind.Local ? EntryDate.ToUniversalTime() : EntryDate,
                    Description = EntryDescription,
                    JournalType = "GENERAL",
                    Status = "POSTED",
                    CreatedBy = 1, // TODO: Get from current user
                    PostedBy = 1, // TODO: Get from current user
                    PostedAt = DateTime.UtcNow,
                    JournalLines = JournalLines.Where(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0))
                        .Select(l => new JournalLine
                        {
                            AccountId = l.AccountId,
                            LineDate = l.LineDate.Kind == DateTimeKind.Local ? l.LineDate.ToUniversalTime() : l.LineDate,
                            DebitAmount = l.DebitAmount,
                            CreditAmount = l.CreditAmount,
                            Description = l.Description,
                            Reference = l.Reference,
                            CostCenterId = l.CostCenterId,
                            ProjectId = l.ProjectId,
                            CreatedBy = l.CreatedBy
                        }).ToList()
                };

                var createdEntry = await _journalServices.CreateJournalEntryAsync(journalEntry);
                await _journalServices.PostJournalEntryAsync(createdEntry.JournalId, 1); // TODO: Get from current user

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Journal entry posted successfully!", "Success", "CheckCircleOutline");
                });
                
                ResetEntry();
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error posting entry: {ex.Message}", "Error", "ErrorOutline");
                });
                System.Diagnostics.Debug.WriteLine($"Error posting entry: {ex}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetEntry();
            GoBack();
        }

        [RelayCommand]
        private void ResetEntry()
        {
            EntryDescription = string.Empty;
            ReferenceNumber = string.Empty;
            EntryDate = DateTime.Today;
            CurrentStatus = "DRAFT";
            
            // Unsubscribe from all line events before clearing
            foreach (var line in JournalLines)
            {
                line.AmountChanged -= OnLineAmountChanged;
            }
            
            JournalLines.Clear();
            SelectedLine = null;
            UpdateCalculations();
            
            // Add one empty line to start fresh
            AddLine();
        }

        [RelayCommand]
        private async Task SaveTemplate()
        {
            if (!CanSaveAsTemplate)
                return;

            try
            {
                var template = new JournalTemplate
                {
                    Name = $"Template - {EntryDescription}",
                    Description = $"Template created from journal entry: {EntryDescription}",
                    JournalType = "GENERAL",
                    TemplateData = System.Text.Json.JsonSerializer.Serialize(JournalLines.Select(l => new
                    {
                        l.AccountId,
                        l.Description,
                        l.DebitAmount,
                        l.CreditAmount,
                        l.Reference
                    })),
                    CreatedBy = 1 // TODO: Get from current user
                };

                await _journalServices.CreateTemplateAsync(template);
                await LoadTemplatesAsync();
                // TODO: Show success message
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error saving template: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ApplyTemplate()
        {
            if (SelectedTemplate == null)
                return;

            try
            {
                var templateData = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(SelectedTemplate.TemplateData);
                JournalLines.Clear();

                foreach (var item in templateData)
                {
                    var line = new JournalLineViewModel
                    {
                        LineNumber = JournalLines.Count + 1,
                        LineDate = EntryDate,
                        AccountId = (int)item.GetProperty("AccountId").GetInt32(),
                        Description = item.GetProperty("Description").GetString() ?? string.Empty,
                        DebitAmount = (decimal)item.GetProperty("DebitAmount").GetDecimal(),
                        CreditAmount = (decimal)item.GetProperty("CreditAmount").GetDecimal(),
                        Reference = item.TryGetProperty("Reference", out JsonElement refProp) ? refProp.GetString() ?? string.Empty : string.Empty,
                        CreatedBy = 1 // TODO: Get from current user
                    };

                    // Subscribe to amount changes to update calculations immediately
                    line.AmountChanged += OnLineAmountChanged;
                    JournalLines.Add(line);
                }

                UpdateCalculations();
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error applying template: {ex.Message}");
            }
        }

        #endregion Commands

        #region Methods

        private async Task LoadAccountsAsync()
        {
            try
            {
                var accounts = await _journalServices.GetAllAccountsAsync();

                AvailableAccounts.Clear();

                if (accounts != null && accounts.Any())
                {
                    foreach (var account in accounts)
                    {
                        AvailableAccounts.Add(account);
                    }
                }
                else
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("No accounts found", "Warning", "WarningOutline");
                    });
                }
            }
            catch (Exception ex)
            {
                // Show error message on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Failed to load accounts: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        private async Task LoadTemplatesAsync()
        {
            try
            {
                var templates = await _journalServices.GetAllTemplatesAsync();
                EntryTemplates.Clear();
                foreach (var template in templates)
                {
                    EntryTemplates.Add(template);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading templates: {ex.Message}");
            }
        }

        private void RenumberLines()
        {
            for (int i = 0; i < JournalLines.Count; i++)
            {
                JournalLines[i].LineNumber = i + 1;
            }
        }

        private void UpdateCalculations()
        {
            TotalDebits = JournalLines.Sum(l => l.DebitAmount);
            TotalCredits = JournalLines.Sum(l => l.CreditAmount);
            Difference = TotalDebits - TotalCredits;
            IsBalanced = Math.Abs(Difference) < 0.01m; // Allow for small rounding differences

            DifferenceColor = IsBalanced ? Brushes.Green : Brushes.Red;
            BalanceStatus = IsBalanced ? "BALANCED" : "UNBALANCED";
            BalanceIndicator = IsBalanced ? "✅ BALANCED" : "❌ UNBALANCED";

            HasValidLines = JournalLines.Any(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0));
            CanPost = IsBalanced && HasValidLines && !string.IsNullOrWhiteSpace(EntryDescription);
            CanSaveAsTemplate = HasValidLines && !string.IsNullOrWhiteSpace(EntryDescription);

            UpdateEntrySummary();
            UpdateEntryPreview();
            ValidateEntry();
        }

        private void UpdateEntrySummary()
        {
            var lineCount = JournalLines.Count;
            var accountCount = JournalLines.Select(l => l.AccountId).Distinct().Count();
            EntrySummary = $"{lineCount} lines, {accountCount} accounts";
        }

        private void UpdateEntryPreview()
        {
            if (!HasValidLines)
            {
                EntryPreview = "No valid lines to preview";
                return;
            }

            var preview = new StringBuilder();
            preview.AppendLine($"Journal Entry: {EntryDescription}");
            preview.AppendLine($"Date: {EntryDate:yyyy-MM-dd}");
            preview.AppendLine($"Reference: {ReferenceNumber}");
            preview.AppendLine();

            foreach (var line in JournalLines.Where(l => l.AccountId > 0))
            {
                var account = AvailableAccounts.FirstOrDefault(a => a.AccountId == line.AccountId);
                var accountName = account?.AccountName ?? "Unknown Account";

                preview.AppendLine($"{line.LineNumber:D2}. {accountName}");
                preview.AppendLine($"    Description: {line.Description}");

                if (line.DebitAmount > 0)
                    preview.AppendLine($"    Debit:  {line.DebitAmount:C}");
                if (line.CreditAmount > 0)
                    preview.AppendLine($"    Credit: {line.CreditAmount:C}");

                preview.AppendLine();
            }

            preview.AppendLine($"Total Debits:  {TotalDebits:C}");
            preview.AppendLine($"Total Credits: {TotalCredits:C}");
            preview.AppendLine($"Difference:     {Difference:C}");

            EntryPreview = preview.ToString();
        }

        private bool ValidateEntry()
        {
            ValidationErrors.Clear();
            HasDescriptionError = false;
            HasDateError = false;

            // Validate description
            if (string.IsNullOrWhiteSpace(EntryDescription))
            {
                DescriptionValidationError = "Description is required";
                HasDescriptionError = true;
                ValidationErrors.Add("Entry description is required");
            }
            else
            {
                DescriptionValidationError = string.Empty;
            }

            // Validate date
            if (EntryDate == default)
            {
                DateValidationError = "Entry date is required";
                HasDateError = true;
                ValidationErrors.Add("Entry date is required");
            }
            else
            {
                DateValidationError = string.Empty;
            }

            // Validate lines - only check lines that have data
            var validLines = JournalLines.Where(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0)).ToList();
            
            if (!validLines.Any())
            {
                ValidationErrors.Add("At least one journal line with account and amount is required");
            }

            foreach (var line in validLines)
            {
                if (line.AccountId <= 0)
                {
                    ValidationErrors.Add($"Line {line.LineNumber}: Account is required");
                }

                if (line.DebitAmount <= 0 && line.CreditAmount <= 0)
                {
                    ValidationErrors.Add($"Line {line.LineNumber}: Either debit or credit amount must be greater than zero");
                }

                if (line.DebitAmount > 0 && line.CreditAmount > 0)
                {
                    ValidationErrors.Add($"Line {line.LineNumber}: Cannot have both debit and credit amounts");
                }
            }

            // Validate balance
            if (!IsBalanced)
            {
                ValidationErrors.Add($"Entry is not balanced. Difference: {Difference:C}");
            }

            HasValidationErrors = ValidationErrors.Any();
            return !HasValidationErrors;
        }

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        private void OnLineAmountChanged(object sender, EventArgs e)
        {
            UpdateCalculations();
        }

        public async Task LoadJournalEntryForEditingAsync(int journalId)
        {
            try
            {
                var journalEntry = await _journalServices.GetJournalEntryByIdAsync(journalId);
                if (journalEntry != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        LoadJournalEntryForEditing(journalEntry);
                    });
                }
                else
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("Journal entry not found", "Error", "ErrorOutline");
                    });
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading journal entry: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        private void LoadJournalEntryForEditing(JournalEntry journalEntry)
        {
            // Store the editing journal entry
            _editingJournalEntry = journalEntry;

            // Load journal entry data into the form
            EntryDescription = journalEntry.Description;
            ReferenceNumber = journalEntry.Reference ?? string.Empty;
            EntryDate = journalEntry.JournalDate.Kind == DateTimeKind.Utc ? journalEntry.JournalDate.ToLocalTime() : journalEntry.JournalDate;
            CurrentStatus = journalEntry.Status;

            // Clear existing lines
            foreach (var line in JournalLines)
            {
                line.AmountChanged -= OnLineAmountChanged;
            }
            JournalLines.Clear();

            // Load journal lines
            if (journalEntry.JournalLines?.Any() == true)
            {
                foreach (var line in journalEntry.JournalLines)
                {
                    var lineViewModel = new JournalLineViewModel
                    {
                        LineNumber = JournalLines.Count + 1,
                        AccountId = line.AccountId,
                        LineDate = line.LineDate.Kind == DateTimeKind.Utc ? line.LineDate.ToLocalTime() : line.LineDate,
                        DebitAmount = line.DebitAmount,
                        CreditAmount = line.CreditAmount,
                        Description = line.Description,
                        Reference = line.Reference,
                        CostCenterId = line.CostCenterId,
                        ProjectId = line.ProjectId,
                        CreatedBy = line.CreatedBy
                    };

                    // Subscribe to amount changes
                    lineViewModel.AmountChanged += OnLineAmountChanged;
                    JournalLines.Add(lineViewModel);
                }
            }
            else
            {
                // Add one empty line if no lines exist
                AddLine();
            }

            UpdateCalculations();
        }

        #endregion Methods

        #region Property Change Handlers

        private void OnEntryDescriptionChanged(string value)
        {
            UpdateCalculations();
        }

        private void OnEntryDateChanged(DateTime value)
        {
            UpdateCalculations();
        }

        private void OnAccountSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredAccounts.Clear();
                HasAccountResults = false;
                return;
            }

            var filtered = AvailableAccounts
                .Where(a => a.AccountNumber.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                           a.AccountName.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

            FilteredAccounts.Clear();
            foreach (var account in filtered)
            {
                FilteredAccounts.Add(account);
            }

            HasAccountResults = FilteredAccounts.Any();
        }

        private void OnSelectedTemplateChanged(JournalTemplate value)
        {
            HasSelectedTemplate = value != null;
        }

        #endregion Property Change Handlers
    }

    public partial class JournalLineViewModel : ObservableObject
    {
        public event EventHandler? AmountChanged;

        private int _lineNumber;

        public int LineNumber
        {
            get => _lineNumber;
            set => SetProperty(ref _lineNumber, value);
        }

        private int _accountId;

        public int AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }

        private DateTime _lineDate;

        public DateTime LineDate
        {
            get => _lineDate;
            set => SetProperty(ref _lineDate, value);
        }

        private decimal _debitAmount;

        public decimal DebitAmount
        {
            get => _debitAmount;
            set
            {
                if (SetProperty(ref _debitAmount, value))
                {
                    OnDebitAmountChanged(value);
                }
            }
        }

        private decimal _creditAmount;

        public decimal CreditAmount
        {
            get => _creditAmount;
            set
            {
                if (SetProperty(ref _creditAmount, value))
                {
                    OnCreditAmountChanged(value);
                }
            }
        }

        private string _description = string.Empty;

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _reference = string.Empty;

        public string Reference
        {
            get => _reference;
            set => SetProperty(ref _reference, value);
        }

        private int? _costCenterId;

        public int? CostCenterId
        {
            get => _costCenterId;
            set => SetProperty(ref _costCenterId, value);
        }

        private int? _projectId;

        public int? ProjectId
        {
            get => _projectId;
            set => SetProperty(ref _projectId, value);
        }

        private int _createdBy;

        public int CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        private void OnDebitAmountChanged(decimal value)
        {
            if (value > 0)
            {
                CreditAmount = 0;
            }
            AmountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCreditAmountChanged(decimal value)
        {
            if (value > 0)
            {
                DebitAmount = 0;
            }
            AmountChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}