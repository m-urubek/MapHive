BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "Categories" (
	"Id_Category"	INTEGER,
	"Name"	TEXT NOT NULL,
	"Description"	TEXT,
	"Icon"	TEXT,
	PRIMARY KEY("Id_Category" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "Configuration" (
	"Id_Configuration"	INTEGER,
	"Key"	TEXT NOT NULL UNIQUE,
	"Value"	INTEGER NOT NULL,
	"Description"	TEXT,
	PRIMARY KEY("Id_Configuration" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "DiscussionThreads" (
	"Id_DiscussionThreads"	INTEGER,
	"LocationId"	INTEGER NOT NULL,
	"UserId"	INTEGER NOT NULL,
	"ThreadName"	TEXT NOT NULL,
	"IsReviewThread"	BOOLEAN NOT NULL DEFAULT 0,
	"ReviewId"	INTEGER,
	"CreatedAt"	DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY("Id_DiscussionThreads" AUTOINCREMENT),
	FOREIGN KEY("LocationId") REFERENCES "MapLocations"("Id_MapLocations") ON DELETE CASCADE,
	FOREIGN KEY("ReviewId") REFERENCES "Reviews"("Id_Reviews") ON DELETE SET NULL,
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Logs" (
	"Id_Log"	INTEGER,
	"Timestamp"	TEXT NOT NULL,
	"SeverityId"	INTEGER NOT NULL,
	"Message"	TEXT NOT NULL,
	"Source"	TEXT,
	"Exception"	TEXT,
	"UserId"	INTEGER,
	"RequestPath"	TEXT,
	"AdditionalData"	TEXT,
	PRIMARY KEY("Id_Log" AUTOINCREMENT),
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS "MapLocations" (
	"Id_MapLocation"	INTEGER,
	"Name"	TEXT NOT NULL,
	"Description"	TEXT NOT NULL,
	"Latitude"	REAL NOT NULL,
	"Longitude"	REAL NOT NULL,
	"Address"	TEXT,
	"Website"	TEXT,
	"PhoneNumber"	TEXT,
	"CreatedAt"	TEXT NOT NULL,
	"UpdatedAt"	TEXT NOT NULL,
	"UserId"	INTEGER NOT NULL,
	"IsAnonymous"	INTEGER NOT NULL DEFAULT 0,
	"CategoryId"	INTEGER,
	PRIMARY KEY("Id_MapLocation" AUTOINCREMENT),
	FOREIGN KEY("CategoryId") REFERENCES "Categories"("Id_Category"),
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Reviews" (
	"Id_Reviews"	INTEGER,
	"LocationId"	INTEGER NOT NULL,
	"UserId"	INTEGER NOT NULL,
	"Rating"	INTEGER NOT NULL CHECK("Rating" BETWEEN 1 AND 5),
	"ReviewText"	TEXT NOT NULL,
	"IsAnonymous"	BOOLEAN NOT NULL DEFAULT 0,
	"CreatedAt"	DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt"	DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY("Id_Reviews" AUTOINCREMENT),
	FOREIGN KEY("LocationId") REFERENCES "MapLocations"("Id_MapLocations") ON DELETE CASCADE,
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "ThreadMessages" (
	"Id_ThreadMessages"	INTEGER,
	"ThreadId"	INTEGER NOT NULL,
	"UserId"	INTEGER NOT NULL,
	"MessageText"	TEXT NOT NULL,
	"IsInitialMessage"	BOOLEAN NOT NULL DEFAULT 0,
	"IsDeleted"	BOOLEAN NOT NULL DEFAULT 0,
	"DeletedByUserId"	INTEGER,
	"DeletedAt"	DATETIME,
	"CreatedAt"	DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY("Id_ThreadMessages" AUTOINCREMENT),
	FOREIGN KEY("DeletedByUserId") REFERENCES "Users"("Id_User") ON DELETE SET NULL,
	FOREIGN KEY("ThreadId") REFERENCES "DiscussionThreads"("Id_DiscussionThreads") ON DELETE CASCADE,
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "IpBans" (
	"Id_IpBan"	INTEGER,
	"HashedIpAddress"	TEXT,
	"Reason"	TEXT NOT NULL,
	"BannedAt"	TEXT NOT NULL,
	"ExpiresAt"	TEXT,
	"BannedByUserId"	INTEGER NOT NULL,
	PRIMARY KEY("Id_IpBan" AUTOINCREMENT),
	FOREIGN KEY("BannedByUserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "UserBans" (
	"Id_UserBan"	INTEGER,
	"UserId"	INTEGER,
	"HashedIpAddress"	TEXT,
	"BanType"	INTEGER NOT NULL,
	"BannedAt"	TEXT NOT NULL,
	"ExpiresAt"	TEXT,
	"Reason"	TEXT NOT NULL,
	"BannedByUserId"	INTEGER NOT NULL,
	PRIMARY KEY("Id_UserBan" AUTOINCREMENT),
	FOREIGN KEY("BannedByUserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE,
	FOREIGN KEY("UserId") REFERENCES "Users"("Id_User") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Users" (
	"Id_User"	INTEGER,
	"Username"	TEXT NOT NULL UNIQUE,
	"PasswordHash"	TEXT NOT NULL,
	"RegistrationDate"	TEXT NOT NULL,
	"Tier"	INTEGER NOT NULL DEFAULT 0,
	"IpAddressHistory"	TEXT DEFAULT '',
	PRIMARY KEY("Id_User" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "VersionNumber" (
	"Id_VersionNumber"	INTEGER,
	"Value"	INTEGER,
	PRIMARY KEY("Id_VersionNumber" AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS "idx_configuration_key" ON "Configuration" (
	"Key"
);
CREATE INDEX IF NOT EXISTS "idx_discussionthreads_locationid" ON "DiscussionThreads" (
	"LocationId"
);
CREATE INDEX IF NOT EXISTS "idx_discussionthreads_reviewid" ON "DiscussionThreads" (
	"ReviewId"
);
CREATE INDEX IF NOT EXISTS "idx_discussionthreads_userid" ON "DiscussionThreads" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "idx_logs_userid" ON "Logs" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "idx_maplocation_category" ON "MapLocations" (
	"CategoryId"
);
CREATE INDEX IF NOT EXISTS "idx_maplocation_userid" ON "MapLocations" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "idx_reviews_locationid" ON "Reviews" (
	"LocationId"
);
CREATE INDEX IF NOT EXISTS "idx_reviews_userid" ON "Reviews" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "idx_threadmessages_threadid" ON "ThreadMessages" (
	"ThreadId"
);
CREATE INDEX IF NOT EXISTS "idx_threadmessages_userid" ON "ThreadMessages" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "idx_IpBans_HashedIpAddress" ON "IpBans" (
	"HashedIpAddress"
);
CREATE INDEX IF NOT EXISTS "idx_UserBans_bannedby" ON "UserBans" (
	"BannedByUserId"
);
CREATE INDEX IF NOT EXISTS "idx_UserBans_ExpiresAt" ON "UserBans" (
	"ExpiresAt"
);
CREATE INDEX IF NOT EXISTS "idx_UserBans_ipaddress" ON "UserBans" (
	"HashedIpAddress"
);
CREATE INDEX IF NOT EXISTS "idx_UserBans_userid" ON "UserBans" (
	"UserId"
);
COMMIT;
