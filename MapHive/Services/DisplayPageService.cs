using MapHive.Models.Exceptions;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class DisplayPageService : IDisplayPageService
    {
        private readonly IDisplayPageRepository _displayPageRepository;

        public DisplayPageService(IDisplayPageRepository displayPageRepository)
        {
            this._displayPageRepository = displayPageRepository;
        }

        public Task<bool> TableExistsAsync(string tableName)
        {
            return this._displayPageRepository.TableExistsAsync(tableName);
        }

        public Task<Dictionary<string, string>> GetItemDataAsync(string tableName, int id)
        {
            return this._displayPageRepository.GetItemDataAsync(tableName, id);
        }

        /// <summary>
        /// Retrieves detailed item data with metadata for display in views.
        /// </summary>
        public async Task<DisplayItemViewModel> GetItemAsync(string tableName, int id)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new RedUserException("Table name is required");
            }

            // Check if table exists
            bool exists = await this._displayPageRepository.TableExistsAsync(tableName);
            if (!exists)
            {
                throw new RedUserException($"Table '{tableName}' does not exist");
            }

            // Retrieve the data
            Dictionary<string, string> data = await this._displayPageRepository.GetItemDataAsync(tableName, id);
            if (data == null || data.Count == 0)
            {
                throw new BlueUserException($"Item with ID {id} was not found in table '{tableName}'");
            }

            // Build view model
            DisplayItemViewModel vm = new()
            {
                TableName = tableName,
                ItemId = id,
                Data = data,
                IsUsersTable = tableName.Equals("Users", StringComparison.OrdinalIgnoreCase)
            };
            if (vm.IsUsersTable && data.TryGetValue("Username", out string? username))
            {
                vm.Username = username;
            }
            return vm;
        }
    }
}