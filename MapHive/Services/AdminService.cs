namespace MapHive.Services
{
    using System.Data;
    using AutoMapper;
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public class AdminService(
        IMapLocationRepository mapLocationRepository,
        IUserContextService userContextService,
        IRequestContextService requestContextService,
        IConfigurationService configSingleton,
        ISqlClientSingleton sqlClientSingleton,
        IAccountBansService accountBansService,
        IIpBansService ipBansService,
        IAccountsRepository accountRepository,
        IMapper mapper) : IAdminService
    {
        private readonly IMapLocationRepository _mapLocationRepository = mapLocationRepository;
        private readonly IUserContextService _userContextService = userContextService;
        private readonly IMapper _mapper = mapper;
        private readonly IRequestContextService _requestContextService = requestContextService;
        private readonly IConfigurationService _configSingleton = configSingleton;
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly IAccountBansService _accountBansService = accountBansService;
        private readonly IIpBansService _ipBansService = ipBansService;
        private readonly IAccountsRepository _accountsRepository = accountRepository;
        private static readonly char[] separator = ['\n', '\r'];

        // CategoryName management
        public Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            return _mapLocationRepository.GetAllCategoriesAsync();
        }

        public Task AddCategoryAsync(CategoryCreate createDto)
        {
            return Task.Run(function: () => _mapLocationRepository.AddCategoryAsync(category: createDto));
        }

        public Task<CategoryGet?> GetCategoryByIdAsync(int id)
        {
            return _mapLocationRepository.GetCategoryByIdAsync(id: id);
        }

        public Task UpdateCategoryAsync(CategoryUpdate updateDto)
        {
            return Task.Run(function: () => _mapLocationRepository.UpdateCategoryAsync(category: updateDto));
        }

        public Task DeleteCategoryAsync(int id)
        {
            return Task.Run(function: () => _mapLocationRepository.DeleteCategoryAsync(id: id));
        }

        public Task UpdateAccountTierAsync(int accountId, AccountTier tier)
        {
            return _accountsRepository.UpdateAccountTierAsync(tierDto: new AccountTierUpdate { AccountId = accountId, Tier = tier });
        }

        // SQL execution
        public async Task<SqlQueryViewModel> ExecuteSqlQueryAsync(string query)
        {
            SqlQueryViewModel model = new() { Query = query.Trim() };
            try
            {
                if (query.StartsWith(value: "SELECT", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    DataTable result = await _sqlClientSingleton.SelectAsync(query: query);
                    model.HasResults = true;
                    model.DataTable = result;
                    model.RowsAffected = result.Rows.Count;
                    model.Message = $"Query executed successfully. {model.RowsAffected} rows returned.";
                }
                else if (query.StartsWith(value: "INSERT", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    int result = await _sqlClientSingleton.InsertAsync(query: query);
                    model.HasResults = false;
                    model.RowsAffected = 1;
                    model.Message = $"Insert executed successfully. ID of inserted row: {result}";
                }
                else if (query.StartsWith(value: "UPDATE", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    int result = await _sqlClientSingleton.UpdateAsync(query: query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Update executed successfully. {result} rows affected.";
                }
                else if (query.StartsWith(value: "DELETE", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    int result = await _sqlClientSingleton.DeleteAsync(query: query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Delete executed successfully. {result} rows affected.";
                }
                else
                {
                    int result = await _sqlClientSingleton.AlterAsync(query: query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = "Query executed successfully.";
                }
            }
            catch (Exception ex)
            {
                model.HasResults = false;
                model.Message = $"Error executing query: {ex.Message}";
            }

            return model;
        }

        // Configuration management
        public Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            return _configSingleton.GetAllConfigurationItemsAsync();
        }

        public Task AddConfigurationItemAsync(ConfigurationItem item)
        {
            return _configSingleton.AddConfigurationItemAsync(item: item);
        }

        public Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            return _configSingleton.GetConfigurationItemAsync(key: key);
        }

        public Task UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            return _configSingleton.UpdateConfigurationItemAsync(item: item);
        }

        public Task DeleteConfigurationItemAsync(string key)
        {
            return _configSingleton.DeleteConfigurationItemAsync(key: key);
        }

        /// <summary>
        /// Gets data for rendering the Ban User page from Profiles.
        /// </summary>
        public async Task<BanUserPageViewModel> GetBanUserPageViewModelAsync(int accountId)
        {
            _userContextService.EnsureAuthenticatedAndAdmin();
            // Get user to ban
            AccountGet? user = await _accountsRepository.GetAccountByIdOrThrowAsync(id: accountId);

            return new BanUserPageViewModel
            {
                Username = user.Username,
                AccountId = user.Id,
                AccountTier = user.Tier
            };
        }

        public async Task<int> BanAsync(BanViewModel banViewModel)
        {
            AccountGet accountGet = await _accountsRepository.GetAccountByIdOrThrowAsync(id: banViewModel.AccountId);
            if (accountGet.Tier == AccountTier.Admin)
                throw new PublicErrorException("You cannot ban an admin account.");

            if (banViewModel.BanType == BanType.Account)
            {
                return await _accountBansService.BanAccountAsync(
                    accountId: banViewModel.AccountId,
                    isPermanent: banViewModel.IsPermanent,
                    durationInDays: banViewModel.BanDurationDays,
                    reason: banViewModel.Reason
                );
            }
            else
            {
                return await _ipBansService.BanIpAddressAsync(
                    hashedIpAddress: accountGet.IpAddressHistory.Split(separator: Environment.NewLine, options: StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? throw new Exception("Unable to read last login IP of an account from database"),
                    isPermanent: banViewModel.IsPermanent,
                    durationInDays: banViewModel.BanDurationDays,
                    reason: banViewModel.Reason
                );
            }
        }
    }
}
