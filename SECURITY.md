# Security policy

## Reporting a vulnerability

Please do not open a public issue for a suspected vulnerability. Use GitHub's private vulnerability reporting feature for this repository, or contact the repository owner privately.

Include the affected endpoint or component, reproduction steps, impact, and any suggested mitigation. Do not include real credentials, access tokens, or personal data.

## Supported version

Security fixes are applied to the latest commit on the default branch. Older commits and forks are not supported.

## Secret handling

- Never commit database passwords, JWT keys, access tokens, `.env` files, local settings, logs, or production data.
- Use .NET User Secrets only for local development and a managed secret store in deployment.
- Treat any secret that ever entered Git history as compromised and rotate it even after the file is deleted.

## Deployment boundary

The repository provides application-level safeguards, tests, and dependency scanning. A secure deployment must additionally provide TLS, restricted database networking, backups, monitoring, centralized rate limiting, and timely platform patching.
