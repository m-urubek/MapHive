# MapHive

MapHive is a feature-rich ASP.NET Core 8 MVC application designed for creating and managing a collaborative world map. Users can add places of interest, which are displayed as pins on an interactive map. Each pin links to a detailed page where users can find more information, participate in discussions, and leave reviews.

I created this project to learn to work with .net core mvc razor pages, to have something into my career portfolio, to experiment with AI driven programming (about 50% of the code is written by AI) and because me and my friends want to use this app.

## Key Features

- **Interactive Map Display**: Utilizes Leaflet.js to show an interactive world map where users can explore and add locations.
- **User-Managed Places**:
    - Registered users can add new places to the map.
    - Option for users to submit places anonymously.
- **Admin Approval Workflow**:
    - New places require administrator approval before appearing on the map.
    - Admins can mark users as "trusted," allowing their submissions to bypass the approval queue.
- **User Registration & Authentication**: Secure user registration and login system.
- **IP Blacklisting**: Tracks registration IPs to help manage and prevent abuse.
- **Discussion Threads**: Logged-in users can engage in discussions about each place.
- **Review System**:
    - Users can leave 5-star ratings and written reviews for places.
    - Reviews can be submitted anonymously.
    - Discussions can be initiated from published reviews.
- **Comprehensive Admin Panel**:
    - **Category Management**: Add, delete, and update categories for places.
    - **User Management**: View and filter a grid of registered users.
    - **Direct SQL Execution**: Execute SQL queries directly against the database and view results.
    - **Configuration Management**: Manage application-wide settings stored in the database.
    - **Ban Management**: Manage user and IP bans.
- **Responsive Design**: Clean, Bootstrap-based UI optimized for both desktop and mobile devices with dark/light theme support.

## Technologies Used

- **Backend**: ASP.NET Core 8 MVC, C#
- **Database**: Entity Framework Core with SQLite
- **Frontend**:
    - HTML, CSS, JavaScript
    - Leaflet.js for interactive maps
    - Bootstrap 5 for UI components and styling
- **Authentication**: Cookie-based authentication
- **Security**: reCAPTCHA for registration, IP hashing

## Getting Started

### Prerequisites

- .NET 8 SDK
- A C# IDE (e.g., Visual Studio 2022, JetBrains Rider, or VS Code with C# Dev Kit)

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/MapHive.git
    cd MapHive
    ```
2.  **Restore NuGet packages:**
    Open the solution (`MapHive.sln`) in your IDE, which should automatically restore packages. Alternatively, run from the command line in the project's root directory:
    ```bash
    dotnet restore
    ```

### Running the Application

1.  **Run from your IDE:**
    - Simply press the "Run" or "Debug" button in Visual Studio or Rider.
2.  **Run from the command line:**
    Navigate to the `MapHive/MapHive` directory and execute:
    ```bash
    dotnet run
    ```
    The application will be accessible at `https://localhost:port` or `http://localhost:port` as indicated in the console output.

The SQLite database file (`maphive.db`) will be automatically created in the `MapHive/MapHive` project directory upon first run, and initial schema migrations will be applied.

## Project Structure

-   `MapHive/` - Main ASP.NET Core MVC project.
    -   `Controllers/` - Handles incoming HTTP requests and orchestrates responses.
    -   `Views/` - Contains Razor views (.cshtml files) for the UI.
        -   `Shared/` - Layouts, partial views, and view components.
    -   `Models/` - Defines data structures and view models.
        -   `Data/DbTableModels/` - EF Core entity classes mapping to database tables.
        -   `PageModels/` - ViewModels specifically for Razor Pages/Views.
        -   `Enums/` - Enumeration types used throughout the application.
    -   `Repositories/` - Implements the repository pattern for data access logic.
        -   `Interfaces/` - Defines contracts for the repositories.
    -   `Services/` - Contains business logic and services.
        -   `Interfaces/` - Defines contracts for the services.
    -   `Singletons/` - Singleton services like database updater and configuration manager.
    -   `Middleware/` - Custom middleware components (e.g., error handling).
    -   `wwwroot/` - Static assets (CSS, JavaScript, images, libraries).
    -   `appsettings.json` - Application configuration.
    -   `Program.cs` - Application entry point and service configuration.
-   `CommonAnalyzers/` - Roslyn analyzers for code quality.
-   `README.md` - This file.

## Database

The application uses SQLite for its data storage. The database file (`maphive.db`) is automatically created in the `MapHive/MapHive/` directory when the application first starts. The `DatabaseUpdaterSingleton` service manages schema migrations and updates by applying SQL scripts`.

## Admin Panel

The Admin Panel provides administrators with tools to manage various aspects of the application:

-   **Categories**: Add, edit, and delete categories for map locations.
-   **Users**: View a sortable and filterable grid of all registered users.
-   **SQL Query**: Execute raw SQL queries against the database and view results (for advanced administration).
-   **Configuration**: Modify application settings stored in the `Configuration` table.
-   **Bans**: Manage account and IP-based bans.

Access to the Admin Panel is restricted to users with administrative privileges.

## Contributing

Contributions are welcome! If you'd like to contribute, please follow these steps:

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix: `git checkout -b feature/your-feature-name` or `git checkout -b fix/issue-description`.
3.  Make your changes and commit them with clear, descriptive messages: `git commit -am 'Add some feature'`.
4.  Push your changes to your forked repository: `git push origin feature/your-feature-name`.
5.  Open a pull request to the main repository.

Please ensure your code adheres to the existing coding style and includes any necessary tests.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details. 