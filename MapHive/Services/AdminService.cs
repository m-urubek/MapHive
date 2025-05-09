using MapHive.Models;
using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using MapHive.Singletons;
using System.Data;

namespace MapHive.Services
{
    public class AdminService : IAdminService
    {
        private readonly IMapLocationRepository _mapLocationRepository;
        private readonly IDataGridService _dataGridService;
        private readonly ISqlClientSingleton _sqlClientSingleton;
        private readonly IConfigurationSingleton _configSingleton;
        private readonly IUserRepository _userRepository;

        public AdminService(
            IMapLocationRepository mapLocationRepository,
            IDataGridService dataGridService,
            ISqlClientSingleton sqlClient,
            IConfigurationSingleton configSingleton,
            IUserRepository userRepository)
        {
            this._mapLocationRepository = mapLocationRepository;
            this._dataGridService = dataGridService;
            this._sqlClientSingleton = sqlClient;
            this._configSingleton = configSingleton;
            this._userRepository = userRepository;
        }

        // Category management
        public Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            return this._mapLocationRepository.GetAllCategoriesAsync();
        }

        public Task AddCategoryAsync(CategoryCreate createDto)
        {
            return Task.Run(() => this._mapLocationRepository.AddCategoryAsync(createDto));
        }

        public Task<CategoryGet?> GetCategoryByIdAsync(int id)
        {
            return this._mapLocationRepository.GetCategoryByIdAsync(id);
        }

        public Task UpdateCategoryAsync(CategoryUpdate updateDto)
        {
            return Task.Run(() => this._mapLocationRepository.UpdateCategoryAsync(updateDto));
        }

        public Task DeleteCategoryAsync(int id)
        {
            return Task.Run(() => this._mapLocationRepository.DeleteCategoryAsync(id));
        }

        // User management grid
        public async Task<DataGridViewModel> GetUsersGridViewModelAsync(string searchTerm, int page, int pageSize, string sortField, string sortDirection)
        {
            List<DataGridColumnGet> columns = await this._dataGridService.GetColumnsForTableAsync("Users");
            return new DataGridViewModel
            {
                TableName = "Users",
                Columns = columns,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortDirection = sortDirection,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public Task UpdateUserTierAsync(int userId, UserTier tier)
        {
            return this._userRepository.UpdateUserTierAsync(new UserTierUpdate { UserId = userId, Tier = tier });
        }

        // SQL execution
        public async Task<SqlQueryViewModel> ExecuteSqlQueryAsync(string query)
        {
            SqlQueryViewModel model = new() { Query = query.Trim() };
            try
            {
                if (query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    DataTable result = await this._sqlClientSingleton.SelectAsync(query);
                    model.HasResults = true;
                    model.DataTable = result;
                    model.RowsAffected = result.Rows.Count;
                    model.Message = $"Query executed successfully. {model.RowsAffected} rows returned.";
                }
                else if (query.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClientSingleton.InsertAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = 1;
                    model.Message = $"Insert executed successfully. ID of inserted row: {result}";
                }
                else if (query.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClientSingleton.UpdateAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Update executed successfully. {result} rows affected.";
                }
                else if (query.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClientSingleton.DeleteAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Delete executed successfully. {result} rows affected.";
                }
                else
                {
                    int result = await this._sqlClientSingleton.AlterAsync(query);
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
            return this._configSingleton.GetAllConfigurationItemsAsync();
        }

        public Task AddConfigurationItemAsync(ConfigurationItem item)
        {
            return this._configSingleton.AddConfigurationItemAsync(item);
        }

        public Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            return this._configSingleton.GetConfigurationItemAsync(key);
        }

        public Task UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            return this._configSingleton.UpdateConfigurationItemAsync(item);
        }

        public Task DeleteConfigurationItemAsync(string key)
        {
            return this._configSingleton.DeleteConfigurationItemAsync(key);
        }

        // Ban management grid
        public async Task<DataGridViewModel> GetBansGridViewModelAsync(string searchTerm, int page, int pageSize, string sortField, string sortDirection)
        {
            List<DataGridColumnGet> columns = await this._dataGridService.GetColumnsForTableAsync("UserBans");
            return new DataGridViewModel
            {
                TableName = "UserBans",
                Columns = columns,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortDirection = sortDirection,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<BanDetailViewModel> GetBanDetailsAsync(int id)
        {
            UserBanGet? ban = await this._userRepository.GetActiveBanByUserIdAsync(id);
            if (ban == null)
            {
                throw new KeyNotFoundException($"Ban {id} not found");
            }

            string bannedUsername = ban.Properties.TryGetValue("BannedUsername", out string? userVal)
                ? userVal
                : ban.UserId.HasValue ? await this._userRepository.GetUsernameByIdAsync(ban.UserId.Value) : string.Empty;
            string bannedByUsername = ban.Properties.TryGetValue("BannedByUsername", out string? adminVal)
                ? adminVal
                : await this._userRepository.GetUsernameByIdAsync(ban.BannedByUserId);
            return new BanDetailViewModel
            {
                Ban = ban,
                BannedUsername = bannedUsername,
                BannedByUsername = bannedByUsername
            };
        }

        public Task<bool> RemoveBanAsync(int id)
        {
            return this._userRepository.UnbanUserAsync(id);
        }

        /// <summary>
        /// Gets data for rendering the Ban User page from Profiles.
        /// </summary>
        public async Task<BanUserPageViewModel> GetBanUserPageViewModelAsync(int adminId, string username)
        {
            // Ensure caller is admin
            UserGet? admin = await this._userRepository.GetUserByIdAsync(adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                throw new UnauthorizedAccessException("Only administrators can access ban functionality.");
            }

            // Get user to ban
            UserGet? user = await this._userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                throw new KeyNotFoundException($"User '{username}' not found.");
            }

            // Determine registration IP
            string? registrationIp = user.IpAddressHistory?
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
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
            UserGet? admin = await this._userRepository.GetUserByIdAsync(adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                throw new UnauthorizedAccessException("Only administrators can perform bans.");
            }

            // Get target user
            UserGet? user = await this._userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                throw new KeyNotFoundException($"User '{username}' not found.");
            }

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
                banDto.ExpiresAt = DateTime.UtcNow.AddDays(model.BanDurationDays.Value);
            }
            if (model.BanType == BanType.Account)
            {
                banDto.UserId = user.Id;
            }
            else // IP ban
            {
                string? registrationIp = user.IpAddressHistory?
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(registrationIp))
                {
                    throw new InvalidOperationException("Cannot determine registration IP for IP ban.");
                }

                banDto.HashedIpAddress = registrationIp;
            }

            int banId = await this._userRepository.BanUserAsync(banDto);
            return banId > 0;
        }
    }
}