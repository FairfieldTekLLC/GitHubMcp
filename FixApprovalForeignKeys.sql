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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE TABLE [OAuthClientRegistrations] (
        [Id] uniqueidentifier NOT NULL,
        [ClientId] nvarchar(200) NOT NULL,
        [ProtectedClientSecret] nvarchar(4000) NULL,
        [ClientName] nvarchar(200) NULL,
        [SoftwareId] nvarchar(200) NULL,
        [SoftwareVersion] nvarchar(100) NULL,
        [RedirectUrisJson] nvarchar(max) NOT NULL,
        [GrantTypesJson] nvarchar(2000) NOT NULL,
        [ScopesJson] nvarchar(2000) NOT NULL,
        [TokenEndpointAuthMethod] nvarchar(100) NOT NULL,
        [RequirePkceS256] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [UpdatedUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_OAuthClientRegistrations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE TABLE [OpenIddictApplications] (
        [Id] nvarchar(450) NOT NULL,
        [ApplicationType] nvarchar(50) NULL,
        [ClientId] nvarchar(100) NULL,
        [ClientSecret] nvarchar(max) NULL,
        [ClientType] nvarchar(50) NULL,
        [ConcurrencyToken] nvarchar(50) NULL,
        [ConsentType] nvarchar(50) NULL,
        [DisplayName] nvarchar(max) NULL,
        [DisplayNames] nvarchar(max) NULL,
        [JsonWebKeySet] nvarchar(max) NULL,
        [Permissions] nvarchar(max) NULL,
        [PostLogoutRedirectUris] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [RedirectUris] nvarchar(max) NULL,
        [Requirements] nvarchar(max) NULL,
        [Settings] nvarchar(max) NULL,
        CONSTRAINT [PK_OpenIddictApplications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE TABLE [OpenIddictScopes] (
        [Id] nvarchar(450) NOT NULL,
        [ConcurrencyToken] nvarchar(50) NULL,
        [Description] nvarchar(max) NULL,
        [Descriptions] nvarchar(max) NULL,
        [DisplayName] nvarchar(max) NULL,
        [DisplayNames] nvarchar(max) NULL,
        [Name] nvarchar(200) NULL,
        [Properties] nvarchar(max) NULL,
        [Resources] nvarchar(max) NULL,
        CONSTRAINT [PK_OpenIddictScopes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE TABLE [OpenIddictAuthorizations] (
        [Id] nvarchar(450) NOT NULL,
        [ApplicationId] nvarchar(450) NULL,
        [ConcurrencyToken] nvarchar(50) NULL,
        [CreationDate] datetime2 NULL,
        [Properties] nvarchar(max) NULL,
        [Scopes] nvarchar(max) NULL,
        [Status] nvarchar(50) NULL,
        [Subject] nvarchar(400) NULL,
        [Type] nvarchar(50) NULL,
        CONSTRAINT [PK_OpenIddictAuthorizations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [OpenIddictApplications] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE TABLE [OpenIddictTokens] (
        [Id] nvarchar(450) NOT NULL,
        [ApplicationId] nvarchar(450) NULL,
        [AuthorizationId] nvarchar(450) NULL,
        [ConcurrencyToken] nvarchar(50) NULL,
        [CreationDate] datetime2 NULL,
        [ExpirationDate] datetime2 NULL,
        [Payload] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [RedemptionDate] datetime2 NULL,
        [ReferenceId] nvarchar(100) NULL,
        [Status] nvarchar(50) NULL,
        [Subject] nvarchar(400) NULL,
        [Type] nvarchar(50) NULL,
        CONSTRAINT [PK_OpenIddictTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenIddictTokens_OpenIddictApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [OpenIddictApplications] ([Id]),
        CONSTRAINT [FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId] FOREIGN KEY ([AuthorizationId]) REFERENCES [OpenIddictAuthorizations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OAuthClientRegistrations_ClientId] ON [OAuthClientRegistrations] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenIddictApplications_ClientId] ON [OpenIddictApplications] ([ClientId]) WHERE [ClientId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE INDEX [IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type] ON [OpenIddictAuthorizations] ([ApplicationId], [Status], [Subject], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenIddictScopes_Name] ON [OpenIddictScopes] ([Name]) WHERE [Name] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE INDEX [IX_OpenIddictTokens_ApplicationId_Status_Subject_Type] ON [OpenIddictTokens] ([ApplicationId], [Status], [Subject], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    CREATE INDEX [IX_OpenIddictTokens_AuthorizationId] ON [OpenIddictTokens] ([AuthorizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpenIddictTokens_ReferenceId] ON [OpenIddictTokens] ([ReferenceId]) WHERE [ReferenceId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117173113_AddPendingModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117173113_AddPendingModelChanges', N'10.0.0');
END;

COMMIT;
GO

