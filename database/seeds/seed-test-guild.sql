-- Manual guild insert (alternative to Seed:Enabled in appsettings).
-- Replace YOUR_DISCORD_USER_ID with your Discord user id from GET /api/auth/me.

INSERT INTO "Guilds" ("Id", "DiscordGuildId", "Name", "OwnerDiscordUserId", "IsActive", "ResourceSyncRequested", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    '123456789012345678',
    'My Test Server',
    'YOUR_DISCORD_USER_ID',
    TRUE,
    FALSE,
    NOW(),
    NOW()
);

INSERT INTO "GuildSettings" (
    "Id", "GuildId", "WelcomeEnabled", "WelcomeMessage",
    "AutoRoleEnabled", "LogsEnabled", "TicketsEnabled", "CreatedAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    g."Id",
    TRUE,
    'Welcome {user} to {server}!',
    FALSE,
    TRUE,
    FALSE,
    NOW(),
    NOW()
FROM "Guilds" g
WHERE g."DiscordGuildId" = '123456789012345678';
