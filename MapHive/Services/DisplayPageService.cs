namespace MapHive.Services
{
    using MapHive.Models.Exceptions;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class DisplayPageService(IDisplayPageRepository displayPageRepository) : IDisplayPageService
    {
        private readonly IDisplayPageRepository _displayPageRepository = displayPageRepository;

        public Task<bool> TableExistsAsync(string tableName)
        {
            return _displayPageRepository.TableExistsAsync(tableName: tableName);
        }

        public Task<Dictionary<string, string>> GetItemDataAsync(string tableName, int id)
        {
            return _displayPageRepository.GetItemDataAsync(tableName: tableName, id: id);
        }

        /// <summary>
        /// Retrieves detailed item data with metadata for display in views.
        /// </summary>
        public async Task<DisplayItemViewModel> GetItemAsync(string tableName, int id)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(value: tableName))
            {
                throw new RedUserException("Table name is required");
            }

            // Check if table exists
            bool exists = await _displayPageRepository.TableExistsAsync(tableName: tableName);
            if (!exists)
            {
                throw new RedUserException($"Table '{tableName}' does not exist");
            }

            // Retrieve the data
            Dictionary<string, string> data = await _displayPageRepository.GetItemDataAsync(tableName: tableName, id: id);
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
                IsUsersTable = tableName.Equals(value: "Users", comparisonType: StringComparison.OrdinalIgnoreCase)
            };
            if (vm.IsUsersTable && data.TryGetValue(key: "Username", value: out string? username))
            {
                vm.Username = username;
            }
            return vm;
        }
    }
}