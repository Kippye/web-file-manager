# Web File Manager

This project is a secure web-based file encryption and management system developed using ASP.NET Core MVC.

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

Docker will probably be set up later.

Currently:

``dotnet run --project App/WebApp/``
