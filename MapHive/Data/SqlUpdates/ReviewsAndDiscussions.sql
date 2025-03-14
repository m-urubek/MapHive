-- SQL script to create tables for the Review and Discussion system

-- Reviews Table
CREATE TABLE IF NOT EXISTS Reviews (
    Id_Reviews INTEGER PRIMARY KEY AUTOINCREMENT,
    LocationId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    Rating INTEGER NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    ReviewText TEXT NOT NULL,
    IsAnonymous BOOLEAN NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (LocationId) REFERENCES MapLocations(Id_MapLocations) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id_User) ON DELETE CASCADE
);

-- Discussion Threads Table
CREATE TABLE IF NOT EXISTS DiscussionThreads (
    Id_DiscussionThreads INTEGER PRIMARY KEY AUTOINCREMENT,
    LocationId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    ThreadName TEXT NOT NULL,
    IsReviewThread BOOLEAN NOT NULL DEFAULT 0,
    ReviewId INTEGER,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (LocationId) REFERENCES MapLocations(Id_MapLocations) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id_User) ON DELETE CASCADE,
    FOREIGN KEY (ReviewId) REFERENCES Reviews(Id_Reviews) ON DELETE SET NULL
);

-- Messages Table for both Discussion and Review threads
CREATE TABLE IF NOT EXISTS ThreadMessages (
    Id_ThreadMessages INTEGER PRIMARY KEY AUTOINCREMENT,
    ThreadId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    MessageText TEXT NOT NULL,
    IsInitialMessage BOOLEAN NOT NULL DEFAULT 0,
    IsDeleted BOOLEAN NOT NULL DEFAULT 0,
    DeletedByUserId INTEGER,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ThreadId) REFERENCES DiscussionThreads(Id_DiscussionThreads) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id_User) ON DELETE CASCADE,
    FOREIGN KEY (DeletedByUserId) REFERENCES Users(Id_User) ON DELETE SET NULL
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_reviews_locationid ON Reviews(LocationId);
CREATE INDEX IF NOT EXISTS idx_reviews_userid ON Reviews(UserId);
CREATE INDEX IF NOT EXISTS idx_discussionthreads_locationid ON DiscussionThreads(LocationId);
CREATE INDEX IF NOT EXISTS idx_discussionthreads_userid ON DiscussionThreads(UserId);
CREATE INDEX IF NOT EXISTS idx_discussionthreads_reviewid ON DiscussionThreads(ReviewId);
CREATE INDEX IF NOT EXISTS idx_threadmessages_threadid ON ThreadMessages(ThreadId);
CREATE INDEX IF NOT EXISTS idx_threadmessages_userid ON ThreadMessages(UserId); 