using MapHive.Models;
using MapHive.Services;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    public class LogTestController : Controller
    {
        private readonly LogManager _logManager;

        public LogTestController(LogManager logManager)
        {
            this._logManager = logManager;
        }

        public IActionResult Index()
        {
            return this.View();
        }

        [HttpGet]
        public IActionResult BlueMessage()
        {
            throw new BlueUserException("This is a blue information message");
        }

        [HttpGet]
        public IActionResult OrangeMessage()
        {
            throw new OrangeUserException("This is an orange warning message");
        }

        [HttpGet]
        public IActionResult RedMessage()
        {
            throw new RedUserException("This is a red error message");
        }

        [HttpGet]
        public IActionResult LogToDatabase()
        {
            try
            {
                // Simulate an operation that might fail
                int zero = 0;
                int result = 10 / zero; // Will throw DivideByZeroException
                return this.Content("This will never be reached");
            }
            catch (Exception ex)
            {
                // Log the exception to the database (and file for Error level)
                this._logManager.Error(
                    message: "An error occurred during calculation",
                    source: "LogTestController.LogToDatabase",
                    exception: ex,
                    additionalData: "User was testing database logging"
                );

                return this.RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult UnhandledException()
        {
            // This will be caught by the middleware and logged to both database and file
            throw new InvalidOperationException("This is an unhandled exception that will be logged");
        }
    }
}