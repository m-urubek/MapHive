# MapHive

MapHive is a .NET Core 8 MVC application for managing and displaying map locations. It provides a simple user interface to view, add, edit, and delete map pins with associated information.

## Features

- Interactive map display using Leaflet.js
- Add new locations with precise coordinates
- View detailed information about each location
- Edit and delete existing locations
- Clean, responsive user interface

## Technologies Used

- ASP.NET Core 8 MVC
- Entity Framework Core with SQLite database
- Leaflet.js for map rendering
- Bootstrap 5 for the UI

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository or download the source code.
2. Open the solution in Visual Studio or the project folder in VS Code.
3. Restore NuGet packages:
   ```
   dotnet restore
   ```
4. Run the application:
   ```
   dotnet run
   ```
5. The application should automatically create the SQLite database file (maphive.db) in the project root.

## Project Structure

- **Models**: Contains the data models for the application
- **Controllers**: Contains the controllers handling user requests
- **Views**: Contains the views for displaying data to users
- **Repositories**: Contains the repository pattern implementation for data access
- **Data**: Contains database context and migration files

## Database

The application uses SQLite for data storage. The database file (maphive.db) is created automatically when the application runs for the first time. Initial seed data is also provided.

## Contributing

1. Fork the repository
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 