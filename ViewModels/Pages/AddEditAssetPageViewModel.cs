using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddEditAssetPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        private int _assetId = 0;            // 0 = New
        private bool _isDisposalMode = false; // special disposal flow

        public string PageTitle => _assetId == 0 ? "Register New Asset" : (_isDisposalMode ? "Dispose Asset" : "Edit Asset");
        public string SaveButtonText => _isDisposalMode ? "Post Disposal" : (_assetId == 0 ? "Register Asset" : "Save Changes");
        public bool IsDisposalMode => _isDisposalMode;
        public bool IsNotDisposalMode => !_isDisposalMode;

        [ObservableProperty] private bool _isLoading;

        // Asset Detail fields
        [ObservableProperty] private string _assetCode = string.Empty;
        [ObservableProperty] private string _assetName = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _purchaseDate = DateTime.Today;
        [ObservableProperty] private decimal _purchaseCost;
        [ObservableProperty] private decimal _residualValue;
        [ObservableProperty] private decimal _usefulLifeYears = 5;

        // Depreciation method
        [ObservableProperty] private string _selectedDepreciationMethod = "STRAIGHT_LINE";
        public string[] DepreciationMethods { get; } = { "STRAIGHT_LINE", "REDUCING_BALANCE" };
        public string DepreciationMethodLabel => SelectedDepreciationMethod == "STRAIGHT_LINE" ? "Straight Line (SLM)" : "Reducing Balance (Diminishing Value)";

        // GL Account selections
        public ObservableCollection<ChartOfAccount> FixedAssetAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> AccumDepnAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> DepnExpenseAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> BankCashAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> AcquisitionOffsetAccounts { get; } = new();
        public ObservableCollection<AssetCategory> Categories { get; } = new();

        [ObservableProperty] private ChartOfAccount _selectedAssetAccount;
        [ObservableProperty] private ChartOfAccount _selectedAccumDepnAccount;
        [ObservableProperty] private ChartOfAccount _selectedDepnExpenseAccount;
        [ObservableProperty] private ChartOfAccount _selectedAcquisitionOffsetAccount;
        [ObservableProperty] private AssetCategory _selectedCategory;

        // CWIP staging
        public ObservableCollection<ChartOfAccount> CwipAccounts { get; } = new();
        [ObservableProperty] private bool _isStagedToCwip;
        [ObservableProperty] private ChartOfAccount _selectedCwipAccount;

        /// <summary>True when the CWIP account selector should be visible.</summary>
        public bool IsCwipAccountVisible => IsStagedToCwip && IsNotDisposalMode;

        partial void OnIsStagedToCwipChanged(bool value) => OnPropertyChanged(nameof(IsCwipAccountVisible));

        // Depreciation schedule preview
        public ObservableCollection<ScheduleRow> DepreciationSchedule { get; } = new();

        // Disposal fields (shown only in disposal mode)
        [ObservableProperty] private DateTime _disposalDate = DateTime.Today;
        [ObservableProperty] private decimal _disposalProceeds;
        [ObservableProperty] private string _disposalType = "SALE";
        [ObservableProperty] private string _disposalNotes = string.Empty;
        [ObservableProperty] private ChartOfAccount _selectedProceedsAccount;
        public string[] DisposalTypes { get; } = { "SALE", "SCRAP" };

        // Preview display
        [ObservableProperty] private decimal _annualDepreciation;
        [ObservableProperty] private string _previewText = string.Empty;

        public class ScheduleRow
        {
            public int Year { get; set; }
            public decimal Depreciation { get; set; }
            public decimal BookValue { get; set; }
        }

        public AddEditAssetPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync(object parameter)
        {
            _isDisposalMode = false;
            _assetId = 0;

            if (parameter is string strParam && strParam.StartsWith("DISPOSE:"))
            {
                _isDisposalMode = true;
                _assetId = int.Parse(strParam.Replace("DISPOSE:", ""));
            }
            else if (parameter is int id && id > 0)
            {
                _assetId = id;
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(IsDisposalMode));
            OnPropertyChanged(nameof(IsNotDisposalMode));

            await LoadAccountsAndCategories();

            if (_assetId > 0)
                await LoadAsset(_assetId);
            else
                UpdatePreview();
        }

        private async Task LoadAccountsAndCategories()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assetService = scope.ServiceProvider.GetRequiredService<AssetService>();

                var accounts = await context.ChartOfAccounts.Where(a => a.IsActive).ToListAsync();

                FixedAssetAccounts.Clear();
                foreach (var a in accounts.Where(a => a.AccountSubtype == "FIXED_ASSET" && a.NormalBalance == "DEBIT"))
                    FixedAssetAccounts.Add(a);

                AccumDepnAccounts.Clear();
                foreach (var a in accounts.Where(a => a.AccountSubtype == "FIXED_ASSET" && a.NormalBalance == "CREDIT"))
                    AccumDepnAccounts.Add(a);

                DepnExpenseAccounts.Clear();
                foreach (var a in accounts.Where(a => a.AccountType == "EXPENSE" && a.AccountSubtype == "OPERATING_EXPENSE"
                    && (a.AccountName.Contains("Depreciation") || a.AccountName.Contains("Amortization"))))
                    DepnExpenseAccounts.Add(a);

                AcquisitionOffsetAccounts.Clear();
                foreach (var a in accounts.Where(a =>
                    (a.AccountType == "ASSET" && a.AccountSubtype == "CURRENT_ASSET" && a.NormalBalance == "DEBIT") ||
                    (a.AccountType == "LIABILITY" && a.NormalBalance == "CREDIT") ||
                    (a.AccountType == "EQUITY" && a.NormalBalance == "CREDIT")))
                    AcquisitionOffsetAccounts.Add(a);

                BankCashAccounts.Clear();
                foreach (var a in accounts.Where(a => a.AccountSubtype == "CURRENT_ASSET" && a.NormalBalance == "DEBIT"))
                    BankCashAccounts.Add(a);

                // CWIP accounts: any active FIXED_ASSET DEBIT account
                // (includes 1470 Capital Work in Progress and similar staging accounts)
                CwipAccounts.Clear();
                foreach (var a in accounts.Where(a =>
                    a.AccountType == "ASSET" &&
                    a.AccountSubtype == "FIXED_ASSET" &&
                    a.NormalBalance == "DEBIT"))
                    CwipAccounts.Add(a);

                // Seed default categories automatically if the table is empty
                await assetService.EnsureDefaultCategoriesAsync();

                var cats = await assetService.GetAllCategoriesAsync();
                Categories.Clear();
                foreach (var c in cats) Categories.Add(c);

                // Defaults for new asset
                if (_assetId == 0)
                {
                    SelectedAssetAccount = FixedAssetAccounts.FirstOrDefault(a => a.AccountNumber == "1430");
                    SelectedAccumDepnAccount = AccumDepnAccounts.FirstOrDefault(a => a.AccountNumber == "1500");
                    SelectedDepnExpenseAccount = DepnExpenseAccounts.FirstOrDefault(a => a.AccountNumber == "5400");
                    SelectedAcquisitionOffsetAccount =
                        AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3020") ??
                        AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3100") ??
                        AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3000") ??
                        AcquisitionOffsetAccounts.FirstOrDefault();
                    SelectedCategory = Categories.FirstOrDefault();

                    // Default CWIP account to 1470 if available
                    SelectedCwipAccount = CwipAccounts.FirstOrDefault(a => a.AccountNumber == "1470")
                        ?? CwipAccounts.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading accounts: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        private async Task LoadAsset(int id)
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();
                var asset = await service.GetAssetByIdAsync(id);

                if (asset == null) return;

                AssetCode = asset.AssetCode;
                AssetName = asset.AssetName;
                Description = asset.Description ?? string.Empty;
                Notes = asset.Notes ?? string.Empty;
                PurchaseDate = asset.PurchaseDate.ToLocalTime();
                PurchaseCost = asset.PurchaseCost;
                ResidualValue = asset.ResidualValue;
                UsefulLifeYears = asset.UsefulLifeYears;
                SelectedDepreciationMethod = asset.DepreciationMethod;
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == asset.CategoryId);
                SelectedAssetAccount = FixedAssetAccounts.FirstOrDefault(a => a.AccountId == asset.AssetAccountId);
                SelectedAccumDepnAccount = AccumDepnAccounts.FirstOrDefault(a => a.AccountId == asset.AccumDepnAccountId);
                SelectedDepnExpenseAccount = DepnExpenseAccounts.FirstOrDefault(a => a.AccountId == asset.DepnExpenseAccountId);
                SelectedAcquisitionOffsetAccount =
                    AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3020") ??
                    AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3100") ??
                    AcquisitionOffsetAccounts.FirstOrDefault(a => a.AccountNumber == "3000") ??
                    AcquisitionOffsetAccounts.FirstOrDefault();

                // Restore CWIP state if asset is staged
                if (asset.Status == "CWIP" && asset.CwipAccountId.HasValue)
                {
                    IsStagedToCwip = true;
                    SelectedCwipAccount = CwipAccounts.FirstOrDefault(a => a.AccountId == asset.CwipAccountId.Value)
                        ?? CwipAccounts.FirstOrDefault(a => a.AccountNumber == "1470")
                        ?? CwipAccounts.FirstOrDefault();
                }
                else
                {
                    IsStagedToCwip = false;
                    SelectedCwipAccount = CwipAccounts.FirstOrDefault(a => a.AccountNumber == "1470")
                        ?? CwipAccounts.FirstOrDefault();
                }

                // In disposal mode pre-fill
                if (_isDisposalMode)
                    DisposalDate = DateTime.Today;

                UpdateSchedulePreview(asset);
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading asset: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(DepreciationMethodLabel));
            }
        }

        partial void OnPurchaseCostChanged(decimal value) => UpdatePreview();
        partial void OnResidualValueChanged(decimal value) => UpdatePreview();
        partial void OnUsefulLifeYearsChanged(decimal value) => UpdatePreview();
        partial void OnSelectedDepreciationMethodChanged(string value)
        {
            OnPropertyChanged(nameof(DepreciationMethodLabel));
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PurchaseCost <= 0 || UsefulLifeYears <= 0) return;

            var tempAsset = new FixedAsset
            {
                PurchaseCost = PurchaseCost,
                ResidualValue = ResidualValue,
                UsefulLifeYears = UsefulLifeYears,
                DepreciationMethod = SelectedDepreciationMethod,
                AccumulatedDepreciation = 0,
                BookValue = PurchaseCost,
                PurchaseDate = DateTime.UtcNow
            };

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<AssetService>();
            UpdateSchedulePreview(tempAsset);
        }

        private void UpdateSchedulePreview(FixedAsset asset)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();
                var schedule = service.GetDepreciationSchedule(asset);

                DepreciationSchedule.Clear();
                foreach (var (year, depn, bv) in schedule)
                    DepreciationSchedule.Add(new ScheduleRow { Year = year, Depreciation = depn, BookValue = bv });

                if (schedule.Any())
                    AnnualDepreciation = schedule.First().depreciation;

                var depnAmt = AnnualDepreciation;
                PreviewText = $"Annual depreciation ≈ {depnAmt:N2}  |  {schedule.Count} years to fully depreciate";
            }
            catch { /* Ignore preview errors */ }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (_isDisposalMode)
            {
                await PostDisposal();
                return;
            }

            if (string.IsNullOrWhiteSpace(AssetName))
            {
                _boxServices.ShowMessage("Asset Name is required.", "Validation", "Warning");
                return;
            }
            if (PurchaseCost <= 0)
            {
                _boxServices.ShowMessage("Purchase Cost must be greater than zero.", "Validation", "Warning");
                return;
            }
            if (UsefulLifeYears <= 0)
            {
                _boxServices.ShowMessage("Useful Life must be greater than zero.", "Validation", "Warning");
                return;
            }
            if (SelectedAssetAccount == null || SelectedAccumDepnAccount == null || SelectedDepnExpenseAccount == null)
            {
                _boxServices.ShowMessage("Please select all GL accounts.", "Validation", "Warning");
                return;
            }
            if (_assetId == 0 && SelectedAcquisitionOffsetAccount == null)
            {
                _boxServices.ShowMessage("Please select an acquisition offset account.", "Validation", "Warning");
                return;
            }
            if (IsStagedToCwip && SelectedCwipAccount == null)
            {
                _boxServices.ShowMessage("Please select a CWIP staging account.", "Validation", "Warning");
                return;
            }
            if (SelectedCategory == null)
            {
                _boxServices.ShowMessage("Please select an asset category.", "Validation", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();

                var asset = new FixedAsset
                {
                    AssetId = _assetId,
                    AssetCode = AssetCode,
                    AssetName = AssetName,
                    Description = Description,
                    Notes = Notes,
                    CategoryId = SelectedCategory.CategoryId,
                    PurchaseDate = PurchaseDate,
                    PurchaseCost = PurchaseCost,
                    ResidualValue = ResidualValue,
                    UsefulLifeYears = UsefulLifeYears,
                    DepreciationMethod = SelectedDepreciationMethod,
                    AssetAccountId = SelectedAssetAccount.AccountId,
                    AccumDepnAccountId = SelectedAccumDepnAccount.AccountId,
                    DepnExpenseAccountId = SelectedDepnExpenseAccount.AccountId
                };

                if (_assetId == 0)
                    await service.CreateAssetAsync(
                        asset,
                        SelectedAcquisitionOffsetAccount.AccountId,
                        isStaged: IsStagedToCwip,
                        cwipAccountId: IsStagedToCwip ? SelectedCwipAccount?.AccountId : null);
                else
                    await service.UpdateAssetAsync(asset);

                _boxServices.ShowMessage("Asset saved successfully.", "Success", "CheckCircleOutline");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error saving asset: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PostDisposal()
        {
            if (_assetId == 0) return;

            if (DisposalType == "SALE" && DisposalProceeds > 0 && SelectedProceedsAccount == null)
            {
                _boxServices.ShowMessage("Please select a bank/cash account to receive the sale proceeds.", "Validation", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();

                await service.DisposeAssetAsync(
                    assetId: _assetId,
                    disposalDate: DisposalDate,
                    saleProceeds: DisposalProceeds,
                    disposalType: DisposalType,
                    proceedsAccountId: SelectedProceedsAccount?.AccountId,
                    notes: DisposalNotes
                );

                _boxServices.ShowMessage("Asset disposed and journal entry posted.", "Success", "CheckCircleOutline");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error posting disposal: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel() => _navigationService.GoBack();
    }
}
