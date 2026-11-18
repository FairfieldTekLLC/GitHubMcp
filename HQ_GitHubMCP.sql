IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [AppUserId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [Outcome] nvarchar(64) NOT NULL,
        [DetailsJson] nvarchar(max) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [Repositories] (
        [Id] uniqueidentifier NOT NULL,
        [Owner] nvarchar(100) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [DefaultBranch] nvarchar(100) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [RegisteredUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Repositories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [EntraObjectId] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [ApprovalRequests] (
        [Id] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedById] uniqueidentifier NOT NULL,
        [RepositoryId] uniqueidentifier NOT NULL,
        [Operation] int NOT NULL,
        [Status] int NOT NULL,
        [SourceBranch] nvarchar(150) NOT NULL,
        [TargetBranch] nvarchar(150) NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [PayloadSha256] nvarchar(64) NOT NULL,
        [SingleUseNonceHash] nvarchar(64) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [ExpiresUtc] datetimeoffset NOT NULL,
        [DecidedByUserId] uniqueidentifier NULL,
        [DecidedUtc] datetimeoffset NULL,
        [ExecutedUtc] datetimeoffset NULL,
        [ResultUrl] nvarchar(500) NULL,
        [FailureReason] nvarchar(2000) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApprovalRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovalRequests_Repositories_RepositoryId] FOREIGN KEY ([RepositoryId]) REFERENCES [Repositories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApprovalRequests_Users_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [GitHubIdentities] (
        [Id] uniqueidentifier NOT NULL,
        [AppUserId] uniqueidentifier NOT NULL,
        [Login] nvarchar(100) NOT NULL,
        [GitHubUserId] bigint NOT NULL,
        [ProtectedAccessToken] nvarchar(4000) NOT NULL,
        [ConnectedUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_GitHubIdentities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GitHubIdentities_Users_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE TABLE [RepositoryGrants] (
        [Id] uniqueidentifier NOT NULL,
        [AppUserId] uniqueidentifier NOT NULL,
        [GitHubRepositoryId] uniqueidentifier NOT NULL,
        [Permission] int NOT NULL,
        CONSTRAINT [PK_RepositoryGrants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RepositoryGrants_Repositories_GitHubRepositoryId] FOREIGN KEY ([GitHubRepositoryId]) REFERENCES [Repositories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RepositoryGrants_Users_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_RepositoryId] ON [ApprovalRequests] ([RepositoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_RequestedById] ON [ApprovalRequests] ([RequestedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedUtc] ON [AuditLogs] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitHubIdentities_AppUserId] ON [GitHubIdentities] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Repositories_Owner_Name] ON [Repositories] ([Owner], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RepositoryGrants_AppUserId_GitHubRepositoryId] ON [RepositoryGrants] ([AppUserId], [GitHubRepositoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RepositoryGrants_GitHubRepositoryId] ON [RepositoryGrants] ([GitHubRepositoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_EntraObjectId] ON [Users] ([EntraObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117143745_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117143745_InitialCreate', N'10.0.0');
END;

COMMIT;
GO

