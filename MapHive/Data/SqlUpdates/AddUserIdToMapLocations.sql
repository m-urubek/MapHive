-- Add UserId and IsAnonymous columns to MapLocations table
ALTER TABLE MapLocations ADD COLUMN UserId INTEGER NOT NULL DEFAULT 1;
ALTER TABLE MapLocations ADD COLUMN IsAnonymous INTEGER NOT NULL DEFAULT 0;

-- Add foreign key constraint
PRAGMA foreign_keys = ON;
CREATE TABLE IF NOT EXISTS MapLocations_new (
    Id_MapLocation INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    Latitude REAL NOT NULL,
    Longitude REAL NOT NULL,
    Address TEXT,
    Website TEXT,
    PhoneNumber TEXT,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    UserId INTEGER NOT NULL,
    IsAnonymous INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id_User)
);

-- Copy data from old table to new table
INSERT INTO MapLocations_new (
    Id_MapLocation, Name, Description, Latitude, Longitude, 
    Address, Website, PhoneNumber, CreatedAt, UpdatedAt, 
    UserId, IsAnonymous
)
SELECT 
    Id_MapLocation, Name, Description, Latitude, Longitude, 
    Address, Website, PhoneNumber, CreatedAt, UpdatedAt, 
    UserId, IsAnonymous
FROM MapLocations;

-- Drop old table and rename new table
DROP TABLE MapLocations;
ALTER TABLE MapLocations_new RENAME TO MapLocations;

-- Create an index on UserId for faster queries
CREATE INDEX idx_map_locations_user_id ON MapLocations(UserId);

-- Insert a log entry to indicate this migration was applied
INSERT INTO Logs (Timestamp, SeverityId, Message, Source)
VALUES (datetime('now'), 1, 'Migration v5: Added UserId and IsAnonymous fields to MapLocations table', 'DatabaseUpdater'); 