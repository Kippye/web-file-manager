# Web File Manager

This project is a secure web-based file encryption and management system developed using ASP.NET Core MVC.

- [Scope](#scope)
- [Technology stack](#technology-stack)
- [Planned features](#planned-features)
- [Planned application routes](#planned-application-routes)
- [Usage instructions](#usage-instructions)

## Scope

The most important security objective is to ensure that uploaded files cannot be accessed or recovered by unauthorised users. The application will therefore implement server-side encryption along with the security features listed in [Security](#security).

The system is designed as a server-side web application. This minimizes the client-side attack surface and allows security-sensitive operations to remain under server-side control.

Some other decisions were made to limit the scope of this project:

- Files cannot be modified after they are uploaded.
- All of the user's files will be displayed in one simple list.
- Only basic authentication options are provided.
- The system does not make backups of files.
- There will be either no admin interface or a limited one.
- SQLite is used over a separate database server.

## Technology Stack

- Framework: ASP.NET Core MVC
- ORM: Entity Framework Core
- Database: SQLite
- File encryption: AES-256-GCM using `System.Security.Cryptography`
- Authentication: ASP.NET Core cookie authentication
- Password hashing: ASP.NET Core password-hashing facilities
- Application data protection: ASP.NET Core Data Protection
- Transport security: HTTPS/TLS
- Logging: Serilog

## Planned Features

### Authentication

- User registration.
- Secure password hashing.
- Login.
- Secure authentication cookies.
- Session expiration.
- Logout.

### File management

- Upload files.
- Encrypt files server-side.
- Store encrypted files outside the web root.
- List files belonging to the current user.
- Check that stored files have not been modified.
- Download decrypted files.
- Delete files.

### Security

- HTTPS/TLS with self-signed certificate.
- CSRF protection.
- XSS protection.
- Server-side input validation.
- Secure file handling.
- SQL injection prevention.
- Per-user authorization.
- Secure error handling.
- Detailed leak-free security logging.
- Secure secret management.

## Planned Application Routes

The initial route structure is:

| Method | Route                  | Purpose                   | Authentication |
| ------ | ---------------------- | ------------------------- | -------------- |
| GET    | `/`                    | Home page                 | No             |
| GET    | `/account/register`    | Registration page         | No             |
| POST   | `/account/register`    | Create account            | No             |
| GET    | `/account/login`       | Login page                | No             |
| POST   | `/account/login`       | Authenticate user         | No             |
| POST   | `/account/logout`      | Log out                   | Yes            |
| GET    | `/files`               | List current user's files | Yes            |
| GET    | `/files/upload`        | Upload page               | Yes            |
| POST   | `/files/upload`        | Upload and encrypt file   | Yes            |
| GET    | `/files/download/{id}` | Decrypt and download file | Yes            |
| POST   | `/files/delete/{id}`   | Delete file               | Yes            |

Every file operation must additionally perform server-side ownership verification.

## Usage Instructions

### HTTP-only

#### On host

You need to pass in an encryption key. Runs at http://localhost:5256.

```bash
dotnet run --project App/WebApp/ -e EncryptionKey=yourSuperSecretKeyWhichIs32Bytes
```

#### In Docker

The default environment (Development) does not redirect HTTP to HTTPS.
You can do the following:

1. Copy `.env.example` to `.env`
2. Set `CERTIFICATE_DIR` to some existing (empty) directory
3. Change the `EncryptionKey` if you want
4. [Launch](#launch), and use the HTTP endpoint

### Generate TLS development certificate

1. Install the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (>= 10.0 should be fine).

2. Create a development certificate and trust it - replace `/path-to-cert/WebApp.pfx` with the output path and `SomePass` with a password:

```bash
dotnet dev-certs https -ep /path-to-cert/WebApp.pfx -p SomePass && dotnet dev-certs https --trust
```

3. Copy `.env.example` to `.env` and set the values from above:

- `CERTIFICATE_DIR`=_/path-to-cert_
- `CERTIFICATE_NAME`=_WebApp.pfx_
- `CERTIFICATE_PASSWORD`=_SomePass_

Optionally set:

- `ASPNETCORE_ENVIRONMENT`=Production

### Launch

```
docker compose up
```

The app is available at http://localhost:8090 and https://localhost:8091.

Logs can be viewed via Seq at http://localhost:5341.
