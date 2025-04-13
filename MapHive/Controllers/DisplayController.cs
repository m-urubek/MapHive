using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class DisplayController : Controller
    {
        /// <summary>
        /// Displays all data for a specific item from a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="id">ID of the item</param>
        /// <returns>View with item data</returns>
        [HttpGet]
        public IActionResult Item(string tableName, int id)
        {
            try
            {
                // Check if the user is an admin
                string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
                if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
                {
                    return this.RedirectToAction("AccessDenied", "Account");
                }

                // Validate table exists
                if (string.IsNullOrEmpty(tableName))
                {
                    throw new RedUserException("Table name is required");
                }

                // Check if the table exists
                bool tableExists = CurrentRequest.DisplayRepository.TableExistsAsync(tableName).GetAwaiter().GetResult();
                if (!tableExists)
                {
                    throw new RedUserException($"Table '{tableName}' does not exist");
                }

                // Get data for the item
                Dictionary<string, string> itemData = CurrentRequest.DisplayRepository.GetItemDataAsync(tableName, id).GetAwaiter().GetResult();

                // Check if data was found
                if (itemData.Count == 0)
                {
                    throw new BlueUserException($"Item with ID {id} was not found in table '{tableName}'");
                }

                // Set ViewBag data for the view
                this.ViewBag.TableName = tableName;
                this.ViewBag.ItemId = id;

                // Check if this is the Users table
                this.ViewBag.IsUsersTable = tableName.Equals("Users", StringComparison.OrdinalIgnoreCase);

                // If it's the Users table, extract the username for the link
                if (this.ViewBag.IsUsersTable)
                {
                    // Find Username key in the dictionary
                    foreach (string key in itemData.Keys)
                    {
                        if (key.Equals("Username", StringComparison.OrdinalIgnoreCase))
                        {
                            this.ViewBag.Username = itemData[key];
                            break;
                        }
                    }
                }

                // Return view with item data
                return this.View(itemData);
            }
            catch (Exception ex)
            {
                // If it's already a user-friendly exception, rethrow it
                if (ex is UserFriendlyExceptionBase)
                {
                    throw;
                }

                // Otherwise, wrap it in a user-friendly exception
                throw new RedUserException($"Error retrieving data: {ex.Message}");
            }
        }
    }
}