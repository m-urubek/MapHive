namespace MapHive.Services
{
    using System.Data;
    using MapHive.Models;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;
    using MapHive.Singletons;

    public class AdminService(
        IMapLocationRepository mapLocationRepository,
        ISqlClientSingleton sqlClient,
        IConfigurationService configSingleton,
        IUserRepository userRepository) : IAdminService
    {
        private readonly IMapLocationRepository _mapLocationRepository = mapLocationRepository;
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClient;
        private readonly IConfigurationService _configSingleton = configSingleton;
        private readonly IUserRepository _userRepository = userRepository;
        private static readonly char[] separator = ['\n', '\r'];

        // Category management
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

        public Task UpdateUserTierAsync(int userId, UserTier tier)
        {
            return _userRepository.UpdateUserTierAsync(tierDto: new UserTierUpdate { UserId = userId, Tier = tier });
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

        public async Task<BanDetailViewModel> GetBanDetailsAsync(int id)
        {
            UserBanGet? ban = await _userRepository.GetActiveBanByUserIdAsync(userId: id) ?? throw new KeyNotFoundException($"Ban {id} not found");
            string bannedUsername = ban.Properties.TryGetValue(key: "BannedUsername", value: out string? userVal)
                ? userVal
                : ban.UserId.HasValue ? await _userRepository.GetUsernameByIdAsync(userId: ban.UserId.Value) : string.Empty;
            string bannedByUsername = ban.Properties.TryGetValue(key: "BannedByUsername", value: out string? adminVal)
                ? adminVal
                : await _userRepository.GetUsernameByIdAsync(userId: ban.BannedByUserId);
            return new BanDetailViewModel
            {
                Ban = ban,
                BannedUsername = bannedUsername,
                BannedByUsername = bannedByUsername
            };
        }

        public Task<bool> RemoveBanAsync(int id)
        {
            return _userRepository.UnbanUserAsync(banId: id);
        }

        /// <summary>
        /// Gets data for rendering the Ban User page from Profiles.
        /// </summary>
        public async Task<BanUserPageViewModel> GetBanUserPageViewModelAsync(int adminId, string username)
        {
            // Ensure caller is admin
            UserGet? admin = await _userRepository.GetUserByIdAsync(id: adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                throw new UnauthorizedAccessException("Only administrators can access ban functionality.");
            }

            // Get user to ban
            UserGet? user = await _userRepository.GetUserByUsernameAsync(username: username) ?? throw new KeyNotFoundException($"User '{username}' not found.");

            // Determine registration IP
            string? registrationIp = user.IpAddressHistory?
                .Split(separator: separator, options: StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "N/A";

            return new BanUserPageViewModel
            {
                Username = user.Username,
                UserId = user.Id,
                UserTier = user.Tier,
                RegistrationIp = registrationIp
            };
        }

        /// <summary>
        /// Applies a ban for the specified user or IP as an admin.
        /// </summary>
        public async Task<bool> BanUserAsync(int adminId, string username, BanViewModel model)
        {
            // Ensure caller is admin
            UserGet? admin = await _userRepository.GetUserByIdAsync(id: adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                throw new UnauthorizedAccessException("Only administrators can perform bans.");
            }

            // Get target user
            UserGet? user = await _userRepository.GetUserByUsernameAsync(username: username) ?? throw new KeyNotFoundException($"User '{username}' not found.");

            // Build ban DTO
            UserBanGetCreate banDto = new()
            {
                BannedByUserId = adminId,
                Reason = model.Reason,
                BanType = model.BanType,
                BannedAt = DateTime.UtcNow
            };
            if (!model.IsPermanent && model.BanDurationDays.HasValue)
            {
                banDto.ExpiresAt = DateTime.UtcNow.AddDays(value: model.BanDurationDays.Value);
            }
            if (model.BanType == BanType.Account)
            {
                banDto.UserId = user.Id;
            }
            else // IP ban
            {
                string? registrationIp = user.IpAddressHistory?
                    .Split(separator: separator, options: StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(value: registrationIp))
                {
                    throw new InvalidOperationException("Cannot determine registration IP for IP ban.");
                }

                banDto.HashedIpAddress = registrationIp;
            }

            int banId = await _userRepository.BanUserAsync(banDto: banDto);
            return banId > 0;
        }
    }
}
