alter table "users" add column "name" text not null;

alter table "users" add column "emailVerified" boolean not null;

alter table "users" add column "image" text;

alter table "users" add column "createdAt" timestamptz default CURRENT_TIMESTAMP not null;

alter table "users" add column "updatedAt" timestamptz default CURRENT_TIMESTAMP not null;

alter table "users" add column "username" text unique;

alter table "users" add column "displayUsername" text;

create table "sessions" ("id" uuid default pg_catalog.gen_random_uuid() not null primary key, "expiresAt" timestamptz not null, "token" text not null unique, "createdAt" timestamptz default CURRENT_TIMESTAMP not null, "updatedAt" timestamptz not null, "ipAddress" text, "userAgent" text, "userId" uuid not null references "users" ("id") on delete cascade);

create table "accounts" ("id" uuid default pg_catalog.gen_random_uuid() not null primary key, "accountId" text not null, "providerId" text not null, "userId" uuid not null references "users" ("id") on delete cascade, "accessToken" text, "refreshToken" text, "idToken" text, "accessTokenExpiresAt" timestamptz, "refreshTokenExpiresAt" timestamptz, "scope" text, "password" text, "createdAt" timestamptz default CURRENT_TIMESTAMP not null, "updatedAt" timestamptz not null);

create table "verifications" ("id" uuid default pg_catalog.gen_random_uuid() not null primary key, "identifier" text not null, "value" text not null, "expiresAt" timestamptz not null, "createdAt" timestamptz default CURRENT_TIMESTAMP not null, "updatedAt" timestamptz default CURRENT_TIMESTAMP not null);

create index "sessions_userId_idx" on "sessions" ("userId");

create index "accounts_userId_idx" on "accounts" ("userId");

create index "verifications_identifier_idx" on "verifications" ("identifier");