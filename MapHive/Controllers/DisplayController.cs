using MapHive.Models.Exceptions;
using MapHive.Repositories.Interfaces;
using MapHive.Singletons;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapHive.Controllers
{
    public class DisplayController : Controller
    {
        /// <summary>
        /// Displays all data for a specific item from a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="id">ID of the item</param>
        /// <returns>View with item data</returns>
        [HttpGet]
        public async Task<IActionResult> Item(string tableName, int id)
        {
            try
            {
                // Validate table exists
                if (string.IsNullOrEmpty(tableName))
                {
                    throw new RedUserException("Table name is required");
                }

                // Check if the table exists
                bool tableExists = await CurrentRequest.DisplayRepository.TableExistsAsync(tableName);
                if (!tableExists)
                {
                    throw new RedUserException($"Table '{tableName}' does not exist");
                }

                // Get data for the item
                Dictionary<string, string> itemData = await CurrentRequest.DisplayRepository.GetItemDataAsync(tableName, id);
                
                // Check if data was found
                if (itemData.Count == 0)
                {
                    throw new BlueUserException($"Item with ID {id} was not found in table '{tableName}'");
                }

                // Set ViewBag data for the view
                ViewBag.TableName = tableName;
                ViewBag.ItemId = id;
                
                // Check if this is the Users table
                ViewBag.IsUsersTable = tableName.Equals("Users", StringComparison.OrdinalIgnoreCase);
                
                // If it's the Users table, extract the username for the link
                if (ViewBag.IsUsersTable)
                {
                    // Find Username key in the dictionary
                    foreach (var key in itemData.Keys)
                    {
                        if (key.Equals("Username", StringComparison.OrdinalIgnoreCase))
                        {
                            ViewBag.Username = itemData[key];
                            break;
                        }
                    }
                }

                // Return view with item data
                return View(itemData);
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