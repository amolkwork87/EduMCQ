# EduMCQ Backend - Codex Agent Instructions

## Project Context

This repository contains the EduMCQ ASP.NET Core Web API.

Current status:
- The API is approximately 75% complete.
- This is an existing application, not a greenfield project.
- The frontend is maintained in a separate Git repository.
- The project uses ADO.NET for database access.

## Core Principles

1. Inspect and understand existing code before modifying it.
2. Preserve existing working functionality.
3. Do not rewrite the application unnecessarily.
4. Prefer incremental, focused changes.
5. Reuse existing patterns and abstractions when they are sound.
6. Do not introduce new libraries without explaining why they are needed.
7. Do not change public API contracts or database schema unnecessarily.
8. Do not modify unrelated files.
9. Never claim a build or test passed unless it was actually executed.

## Workflow for Significant Tasks

For large changes:
1. Inspect relevant code.
2. Explain the current implementation.
3. Identify problems and risks.
4. Propose a phased solution.
5. List files expected to change.
6. Implement a small logical batch.
7. Build the API.
8. Run relevant tests.
9. Review the git diff.
10. Continue to the next batch.

For security, architecture, database, or major refactoring tasks, audit and propose the plan before making broad changes.

## ASP.NET Core

Follow modern ASP.NET Core practices.

Prefer:
- Dependency Injection
- async/await
- CancellationToken where appropriate
- thin controllers
- business logic in services
- DTOs for API contracts
- centralized exception handling
- structured logging
- configuration/options patterns
- proper HTTP status codes
- clear separation of responsibilities
- input validation
- authentication and authorization

Avoid:
- business logic in controllers
- duplicated logic
- unnecessary static state
- hard-coded configuration
- exposing database entities directly when DTOs are appropriate
- swallowed exceptions
- sensitive information in logs

## ADO.NET and Database Access

ADO.NET is the existing database-access technology.

Do NOT automatically replace ADO.NET with EF Core.

When reviewing or changing data access:
- use parameterized SQL
- prevent SQL injection
- properly dispose connections, commands, and readers
- use async database APIs where appropriate
- avoid unnecessary database round trips
- avoid loading unnecessary data into memory
- consider connection pooling
- use transactions where required
- inspect query performance and indexing
- preserve existing behavior unless a change is justified

For bulk operations, evaluate the actual database and workload. Consider:
- batching
- database-native bulk/COPY mechanisms where supported
- staging tables
- transactions
- duplicate handling
- validation
- memory usage
- failure recovery
- error reporting
- performance

Never assume an entire large upload should be loaded into memory.

## Security

Perform a serious security review when requested.

Check for:
- SQL injection
- broken authentication
- broken authorization
- IDOR/BOLA
- excessive data exposure
- input-validation problems
- insecure file uploads
- path traversal
- JWT security issues
- password handling
- OTP security
- secrets in source control
- secrets in logs
- insecure CORS
- missing security headers
- excessive permissions
- rate limiting where appropriate
- dependency vulnerabilities
- unsafe error responses

For each finding, explain:
1. Vulnerability
2. Severity
3. Why it matters
4. Attack scenario where appropriate
5. Recommended fix

Do not make speculative security claims.

## Authentication and OTP

Authentication is security-sensitive.

Never:
- log passwords
- log OTP values
- return OTP values from APIs
- hard-code secrets
- commit credentials
- bypass authorization for convenience

If Twilio Verify is used, prefer Twilio Verify for OTP generation and verification rather than maintaining a second OTP lifecycle unless there is a documented business requirement.

## Error Handling and Logging

Use centralized exception handling where appropriate.

Use structured logging.

Prefer:

```csharp
_logger.LogError(ex, "Failed to process MCQ upload");
```

Do not log:
- passwords
- OTPs
- JWTs/access tokens
- API keys
- connection strings
- sensitive personal information

Do not expose stack traces or internal implementation details to clients in production responses.

## API Design

Use RESTful conventions where appropriate.

Use meaningful:
- routes
- HTTP verbs
- status codes
- request DTOs
- response DTOs

Validate all client input.

Never trust client-provided values for authorization decisions.

Keep API contracts backward compatible unless a breaking change is explicitly approved.

## Bulk Upload

Bulk upload is an important feature.

When implementing or improving it, consider:
- file type validation
- file size limits
- malformed records
- duplicate records
- database constraints
- batching
- transactions
- memory usage
- partial failures
- rollback behavior
- progress reporting
- error reporting
- security
- performance

Prefer streaming or bounded-memory processing for large files.

## Performance

Measure or identify the actual bottleneck before optimizing.

Backend areas to review:
- database round trips
- SQL query efficiency
- indexes
- connection pooling
- allocations
- large object loading
- pagination
- batching
- async operations
- caching where justified

Do not optimize prematurely.

## Configuration and Secrets

Never hard-code:
- database credentials
- Twilio credentials
- JWT secrets
- API keys
- passwords
- environment-specific secrets

Use configuration/environment variables/secrets.

Never commit secrets to Git.

## Dependencies

Before adding a NuGet package:
1. Check whether the current project already solves the problem.
2. Explain why the dependency is required.
3. Prefer maintained and established packages.
4. Avoid unnecessary dependencies.

Do not upgrade unrelated packages during a feature task.

## Testing

Testing is required for meaningful changes.

Prioritize:
- service/business-logic tests
- validation tests
- authentication tests
- authorization tests
- API behavior
- error handling
- database-related behavior
- bulk-upload scenarios
- regression tests for bugs

When fixing a bug:
1. Reproduce it.
2. Add a regression test where practical.
3. Fix the root cause.
4. Run the relevant tests.

Do not create meaningless tests solely to increase coverage.

## Build Verification

After backend changes, run the appropriate build command, for example:

```bash
dotnet build
```

Run relevant tests, for example:

```bash
dotnet test
```

If a command fails:
1. Investigate the real cause.
2. Fix the root cause.
3. Run it again.
4. Do not suppress or ignore the failure.

## Git Discipline

Before finishing a task:
- inspect `git diff`
- inspect changed files
- remove debug code
- remove temporary files
- remove unused imports
- ensure secrets are not included
- avoid unrelated changes

Do not create commits unless explicitly requested.

## Large Refactoring

Do not refactor the entire repository in one operation.

Use phases such as:
1. Audit
2. Security
3. Backend architecture
4. ADO.NET/database
5. Bulk processing
6. Testing
7. Performance
8. Final cleanup

Build and test after each logical phase.

## Agent Reporting

At the end of a task, report:
- What changed
- Important files changed
- Why important changes were made
- Build/test commands executed
- Results
- Remaining issues or risks

## Most Important Rule

Preserve existing EduMCQ functionality.

Improve:
- security
- maintainability
- architecture
- ADO.NET/database access
- performance
- bulk processing
- testing

Do not turn incremental improvement into an unnecessary rewrite.
