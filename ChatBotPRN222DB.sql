-- ============================================================
--  ChatBotPRN222 - SQL Server Database Script
--  Server : localhost
--  Login  : admin / 123
-- ============================================================

USE master;
GO

-- Tao database neu chua co
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ChatBotPRN222')
BEGIN
    CREATE DATABASE ChatBotPRN222;
END
GO

USE ChatBotPRN222;
GO

-- ============================================================
-- 1. Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id          NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        Username    NVARCHAR(100) NOT NULL,
        Email       NVARCHAR(200) NOT NULL  DEFAULT '',
        PasswordHash NVARCHAR(MAX) NOT NULL DEFAULT '',
        FullName    NVARCHAR(200) NOT NULL  DEFAULT '',
        Role        NVARCHAR(50)  NOT NULL  DEFAULT 'Student',
        CreatedAt   DATETIME2     NOT NULL  DEFAULT GETUTCDATE(),
        AvatarPath  NVARCHAR(500)     NULL,
        Bio         NVARCHAR(MAX)     NULL
    );

    CREATE UNIQUE INDEX UX_Users_Username ON Users (Username);
END
GO

-- ============================================================
-- 2. Subjects
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subjects')
BEGIN
    CREATE TABLE Subjects (
        Id          NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        Code        NVARCHAR(50)  NOT NULL  DEFAULT '',
        Name        NVARCHAR(200) NOT NULL  DEFAULT '',
        Description NVARCHAR(MAX) NOT NULL  DEFAULT '',
        CreatedAt   DATETIME2     NOT NULL  DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================
-- 3. Documents
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Documents')
BEGIN
    CREATE TABLE Documents (
        Id          NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        SubjectId   NVARCHAR(36)  NOT NULL  DEFAULT '',
        UploadedBy  NVARCHAR(36)  NOT NULL  DEFAULT '',
        FileName    NVARCHAR(500) NOT NULL  DEFAULT '',
        ContentType NVARCHAR(100) NOT NULL  DEFAULT '',
        FileSize    BIGINT        NOT NULL  DEFAULT 0,
        ChunkCount  INT           NOT NULL  DEFAULT 0,
        Status      NVARCHAR(20)  NOT NULL  DEFAULT 'Indexed',
        UploadedAt  DATETIME2     NOT NULL  DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_Documents_SubjectId ON Documents (SubjectId);
END
GO

-- ============================================================
-- 4. DocumentChunks
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentChunks')
BEGIN
    CREATE TABLE DocumentChunks (
        Id           NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        DocumentId   NVARCHAR(36)  NOT NULL  DEFAULT '',
        SubjectId    NVARCHAR(36)  NOT NULL  DEFAULT '',
        DocumentName NVARCHAR(500) NOT NULL  DEFAULT '',
        ChunkIndex   INT           NOT NULL  DEFAULT 0,
        Content      NVARCHAR(MAX) NOT NULL  DEFAULT '',
        Page         INT           NOT NULL  DEFAULT 0,
        CreatedAt    DATETIME2     NOT NULL  DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_DocumentChunks_SubjectId   ON DocumentChunks (SubjectId);
    CREATE INDEX IX_DocumentChunks_DocumentId  ON DocumentChunks (DocumentId);
END
GO

-- ============================================================
-- 5. ChatSessions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatSessions')
BEGIN
    CREATE TABLE ChatSessions (
        Id        NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        UserId    NVARCHAR(36)  NOT NULL  DEFAULT '',
        SubjectId NVARCHAR(36)      NULL,
        Title     NVARCHAR(500) NOT NULL  DEFAULT 'New chat',
        CreatedAt DATETIME2     NOT NULL  DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2     NOT NULL  DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_ChatSessions_UserId ON ChatSessions (UserId);
END
GO

-- ============================================================
-- 6. ChatMessages
-- Sources duoc luu dang JSON (nvarchar(max))
-- VD: [{"DocumentId":"...","DocumentName":"...","ChunkIndex":0,"Page":1,"Snippet":"..."}]
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE ChatMessages (
        Id        NVARCHAR(36)  NOT NULL  PRIMARY KEY,
        SessionId NVARCHAR(36)  NOT NULL  DEFAULT '',
        Role      NVARCHAR(20)  NOT NULL  DEFAULT 'user',
        Content   NVARCHAR(MAX) NOT NULL  DEFAULT '',
        Sources   NVARCHAR(MAX) NOT NULL  DEFAULT '[]',
        CreatedAt DATETIME2     NOT NULL  DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_ChatMessages_SessionId ON ChatMessages (SessionId);
END
GO

-- ============================================================
-- Kiem tra ket qua
-- ============================================================
SELECT
    t.name        AS [Table],
    p.rows        AS [Rows]
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
ORDER BY t.name;
GO

PRINT 'ChatBotPRN222 database created successfully.';
GO
