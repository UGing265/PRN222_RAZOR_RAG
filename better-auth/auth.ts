import { betterAuth } from "better-auth";
import { username } from "better-auth/plugins";
import pg from "pg";
import dotenv from "dotenv";
import crypto from "crypto";

dotenv.config();

const { Pool } = pg;

// Helper functions for PBKDF2 hashing matching .NET logic
const hashPasswordPbkdf2 = (password: string): Promise<string> => {
  return new Promise((resolve, reject) => {
    const salt = crypto.randomBytes(16);
    crypto.pbkdf2(password, salt, 100000, 32, "sha256", (err, derivedKey) => {
      if (err) return reject(err);
      const saltB64 = salt.toString("base64");
      const hashB64 = derivedKey.toString("base64");
      resolve(`${saltB64}.${hashB64}`);
    });
  });
};

const verifyPasswordPbkdf2 = (password: string, storedHash: string): Promise<boolean> => {
  return new Promise((resolve, reject) => {
    const parts = storedHash.split(".");
    if (parts.length !== 2) {
      return resolve(false);
    }
    const salt = Buffer.from(parts[0], "base64");
    const expectedHash = Buffer.from(parts[1], "base64");
    
    crypto.pbkdf2(password, salt, 100000, expectedHash.length, "sha256", (err, derivedKey) => {
      if (err) return reject(err);
      try {
        if (derivedKey.length !== expectedHash.length) {
          return resolve(false);
        }
        resolve(crypto.timingSafeEqual(derivedKey, expectedHash));
      } catch {
        resolve(false);
      }
    });
  });
};


export const auth = betterAuth({
  baseURL: process.env.BETTER_AUTH_URL || "http://localhost:5000",
  trustedOrigins: ["http://localhost:3000", "http://localhost:5155", "https://localhost:7065"],
  database: new Pool({
    connectionString: process.env.DATABASE_URL,
  }),
  advanced: {
    database: {
      generateId: "uuid",
    },
  },
  user: {
    modelName: "users",
    fields: {
      name: "full_name",
      image: "avatar_url",
      createdAt: "created_at",
      updatedAt: "updated_at",
      emailVerified: "email_verified",
    },
    additionalFields: {
      roleId: {
        type: "number",
        required: true,
        defaultValue: 3,
        fieldName: "role_id",
      },
    },
  },
  session: {
    modelName: "sessions",
    fields: {
      createdAt: "created_at",
      updatedAt: "updated_at",
      ipAddress: "ip_address",
      userAgent: "user_agent",
      userId: "user_id",
      expiresAt: "expires_at",
    },
  },
  account: {
    modelName: "accounts",
    fields: {
      userId: "user_id",
      accountId: "account_id",
      providerId: "provider_id",
      accessToken: "access_token",
      refreshToken: "refresh_token",
      idToken: "id_token",
      accessTokenExpiresAt: "access_token_expires_at",
      refreshTokenExpiresAt: "refresh_token_expires_at",
      createdAt: "created_at",
      updatedAt: "updated_at",
    },
  },
  verification: {
    modelName: "verifications",
    fields: {
      createdAt: "created_at",
      updatedAt: "updated_at",
      expiresAt: "expires_at",
    },
  },
  emailAndPassword: {
    enabled: true,
    requireEmailVerification: false,
    password: {
      hash: async (password) => {
        return hashPasswordPbkdf2(password);
      },
      verify: async ({ hash, password }) => {
        return verifyPasswordPbkdf2(password, hash);
      },
    },
  },
  socialProviders: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
    },
  },
  databaseHooks: {
    user: {
      create: {
        before: async (user) => {
          const email = user.email;
          const allowedDomains = ["fpt.edu.vn", "fe.edu.vn", "gmail.com"];
          const domain = email.split("@")[1];
          if (!allowedDomains.includes(domain)) {
            throw new Error("Chỉ chấp nhận tài khoản email FPT (@fpt.edu.vn hoặc @fe.edu.vn)!");
          }
          // Default roleId is 3 (Student)
          const targetRoleId = user.roleId ? Number(user.roleId) : 3;
          return {
            data: {
              ...user,
              roleId: targetRoleId,
            }
          };
        },
      },
    },
  },
  plugins: [
    username(),
  ],
  secret: process.env.BETTER_AUTH_SECRET!,
});

export type Auth = typeof auth;
