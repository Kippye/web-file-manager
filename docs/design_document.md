# 1. System Architecture

![[architecture_diagram.png|Architecture diagram]]

# 2. Threat Model

The main attack categories this kind of app is vulnerable to include:

- Authentication and session attacks.
- Unauthorized file access.
- Path traversal and file inclusion.
- Cryptographic failures.
- Cross Site Request Forgery (CSRF).
- Security misconfiguration.
- JavaScript injection / Cross Site Scripting (XSS).
- HTML injection.
- Information leakage.
- Input tampering.
- Client-side control bypass.
- SQL injection.

The following table maps some possible threats to the OWASP Top 10 2025 categories and describes planned mitigation methods.

| Threat                                         | Class                               | OWASP Top 10                                                            | Mitigation                                                                                                                                                                           |
| ---------------------------------------------- | ----------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Unauthorized file download                     |                                     | **A01 - Broken Access Control**                                         | Verify authentication and ownership for every file operation.                                                                                                                        |
| CSRF                                           | **CSRF**                            | **A01 - Broken Access Control**                                         | Use antiforgery tokens on all state-changing forms. Use POST for operations such as upload, delete and logout.                                                                       |
| Path traversal                                 |                                     | **A01 - Broken Access Control**<br>**A05 - Injection**                  | Never construct filesystem paths directly from user-controlled filenames. Generate server-side storage identifiers and restrict file operations to the designated storage directory. |
| Client-side control bypass                     | **Client-side control bypass**      | **A01 - Broken Access Control**<br>**A06 - Insecure Design**            | Never rely on hidden fields, disabled controls or JavaScript for authorization. Perform all authorization checks on the server.                                                      |
| Insecure application configuration             |                                     | **A02 - Security Misconfiguration**                                     | Disable development features in deployment, prevent directory listing, protect configuration/secrets, configure secure headers and enforce HTTPS.                                    |
| Vulnerable dependency                          |                                     | **A03 - Software Supply Chain Failures**                                | Keep .NET and NuGet dependencies maintained, minimize unnecessary dependencies, monitor for known vulnerabilities.                                                                   |
| Compromised or untrusted dependency            |                                     | **A03 - Software Supply Chain Failures**                                | Use trusted package sources, lock/review dependencies where appropriate.                                                                                                             |
| Weak file encryption                           |                                     | **A04 - Cryptographic Failures**                                        | Use authenticated encryption such as AES-256-GCM with securely generated keys and unique nonces.                                                                                     |
| Cryptographic key exposure                     |                                     | **A04 - Cryptographic Failures**<br>**A02 - Security Misconfiguration** | Keep encryption keys outside source control. Use user secrets/environment variables during development and dedicated secret management for production.                               |
| HTML injection                                 | **HTML Injection**                  | **A05 - Injection**                                                     | Encode all user-controlled output using Razor's default HTML encoding. Never render untrusted input as raw HTML. Validate filenames and other user-supplied values.                  |
| JavaScript injection / XSS                     | **JavaScript Injection**<br>**XSS** | **A05 - Injection**                                                     | Use contextual output encoding, strict input validation, Content Security Policy (CSP), and avoid unsafe rendering of user-controlled HTML or JavaScript.                            |
| SQL injection                                  | **Injection**                       | **A05 - Injection**                                                     | Use parameterized queries. Never construct SQL statements through string concatenation using user input.                                                                             |
| File inclusion                                 | **Injection**                       | **A05 - Injection**                                                     | Do not allow users to specify arbitrary server-side paths. Restrict file operations to application-controlled storage and validate all file-related input.                           |
| Input tampering                                | **Injection<br>Input validation**   | **A05 - Injection**<br>**A06 - Insecure Design**                        | Treat all client-supplied values as untrusted. Validate inputs server-side and use allowlists where appropriate.                                                                     |
| Malicious file upload                          | **Input validation**                | **A05 - Injection**<br>**A06 - Insecure Design**                        | Validate file size and permitted file types, generate server-side storage names, reject dangerous inputs, and store files outside the web root.                                      |
| Session fixation                               | **Session management**              | **A07 - Authentication Failures**                                       | Establish the authenticated session only after successful authentication and invalidate/re-establish authentication state as appropriate.                                            |
| Session hijacking                              | **Session management**              | **A07 - Authentication Failures**<br>**A04 - Cryptographic Failures**   | Use HTTPS, `Secure`, `HttpOnly` and appropriate `SameSite` cookie settings. Limit session lifetime.                                                                                  |
| Weak password storage                          |                                     | **A07 - Authentication Failures**<br>**A04 - Cryptographic Failures**   | Never store passwords as plaintext. Use a modern password-hashing mechanism.                                                                                                         |
| Database/storage inconsistency                 |                                     | **A08 - Software or Data Integrity Failures**                           | Validate relationships between database metadata and stored files, prevent unauthorized modification of metadata, and verify integrity/authentication of encrypted files.            |
| Incomplete security logging                    |                                     | **A09 - Security Logging and Alerting Failures**                        | Log security-relevant events with timestamp, user/session identifier, source address and outcome. Never log passwords, tokens or cryptographic keys.                                 |
| Information leakage through errors             |                                     | **A10 - Mishandling of Exceptional Conditions**                         | Handle exceptions centrally, return generic user-facing errors, and never expose stack traces, internal paths, SQL errors or cryptographic information.                              |
| Authorization failure during exceptional state |                                     | **A10 - Mishandling of Exceptional Conditions**                         | Ensure errors and exceptional states fail securely. Authorization must not be skipped when database/storage operations fail or return unexpected results.                            |

# 4. Technology Stack

## 4.1 ASP.NET Core MVC

MVC (Model-View-Controller) is appropriate because the application primarily consists of server-rendered pages and forms.

The application does not require SPA architecture or extensive client-side state management.

MVC provides:

- Controllers.
- Razor views.
- Model binding.
- Validation.
- Authentication integration.
- Authorization.
- Antiforgery protection.

## 4.2 Database

### 4.2.1 Entity Framework Core

Entity Framework Core will provide the database abstraction layer.

It will be used for:

- User records.
- File metadata.
- Ownership relationships.
- Authentication-related data where applicable.

The application will use parameterized queries through EF Core rather than constructing SQL strings manually. This reduces the SQL injection attack surface.

### 4.2.2 SQLite

SQLite is sufficient for the initial scope of the project.

Advantages:

- No separate database server is required.
- Simple local development.
- Small deployment footprint.
- Full support through Entity Framework Core.

Limitations:

- It is not the intended database architecture for a large multi-instance production deployment.
- Concurrent write-heavy workloads would be better served by a server-based relational database.

## 4.3. .NET `System.Security.Cryptography`

The proposed file encryption algorithm is **AES-256-GCM**, provided by `System.Security.Cryptography.AesGcm` in .NET.

AES-GCM provides authenticated encryption, giving the application integrity of the encrypted data by using the authentication tag to verify that the encrypted data has not changed.

Each encrypted file should use a unique randomly generated nonce. The nonce does not need to be secret and can be stored alongside the encrypted file's metadata.

The encryption key must remain secret. It must be generated using a cryptographically secure random number generator and supplied through protected configuration.

## 4.4. TLS Plan

All normal application traffic will be served over HTTPS / TLS.

Development will use an ASP.NET Core development certificate.

HTTP should not be used for normal authenticated application traffic.

TLS protects:

- Login credentials.
- Authentication cookies.
- Uploaded and downloaded files while being transmitted.
- Other sensitive application data.

TLS does not replace application-level authorization, validation or encryption.

# 5. Authentication and Session Model

## 5.1 Authentication

The application will use username/email and password authentication.

Passwords will never be stored in plaintext. A password hashing mechanism provided by ASP.NET Core will be used.

The application will issue an authenticated cookie after successful authentication. ASP.NET Core cookie authentication protects the authentication ticket using its Data Protection infrastructure.

## 5.2 Authentication Cookie

The authentication cookie will use the following security properties:

| Property   | Planned value                         | Purpose                                                                       |
| ---------- | ------------------------------------- | ----------------------------------------------------------------------------- |
| `HttpOnly` | `true`                                | Prevent JavaScript from reading the authentication cookie                     |
| `Secure`   | `true` outside local HTTP development | Send cookie only over HTTPS                                                   |
| `SameSite` | `Lax`                                 | Reduce cross-site request attacks while retaining normal navigation behaviour |
| `Path`     | `/`                                   | Cookie applies to the application                                             |
| Expiration | Limited                               | Reduce the useful lifetime of a stolen session                                |

The application will not use persistent "remember me" authentication initially unless it is explicitly required.

## 5.3. Session Lifetime

The initial design will use a limited authentication session.

Proposed policy:

- **Absolute session lifetime:** 8 hours.
- **Idle timeout:** 30 minutes.
- **Sliding expiration:** enabled only if required by the final implementation.
- **Logout:** authentication cookie is invalidated immediately.

The most important security property is that sessions should not remain valid indefinitely.

## 5.4. Session Fixation Prevention

Session fixation occurs when an attacker is able to cause a victim to authenticate using a pre-defined session identifier, which then becomes the victim's session identifier that the attacker knows.

The application will mitigate this by establishing the authenticated state with a new authentication cookie only after successful credential verification. The pre-authentication session identifier must not be promoted to an authenticated session.

The authentication middleware will manage the authentication ticket rather than allowing the application to accept an arbitrary client-provided session identifier.