# Security notes

Facet is an academic portfolio project and has not been hardened for production use. Do not upload real personal, financial, medical, or confidential documents. Do not deploy it publicly without a security review.

## Configuration

Database credentials and the JWT signing key must be supplied through environment variables (or a local secrets manager), not committed to the repository. The included `.env.example` values are placeholders for disposable local use only. Use unique, randomly generated values for every deployment.

## Known review items

- The API includes anonymous document operations intended for the demo and API test flow. Review authorization and ownership checks before exposing the API to an untrusted network.
- Registration and role assignment should be reviewed for privilege escalation and account bootstrap behavior before production use.
- Document upload validation, size limits, retention/deletion behavior, malware scanning, and storage encryption need a production policy.
- CORS currently permits arbitrary origins with credentials; restrict allowed origins before deployment.
- Keep production credentials in a dedicated secrets manager. If credentials are ever committed accidentally, revoke or rotate them and remove them from repository history and CI logs before publishing.

The current code makes new registrations standard users and supports an optional administrator bootstrap through `BootstrapAdmin__Username` and `BootstrapAdmin__Password`. Supply both through a secure local or deployment secret store; never commit the password. The upgrade migration removes the unchanged plaintext demo administrator record from an existing database; it leaves records that have already been updated with a salt alone.

Please report suspected vulnerabilities privately to the repository owner rather than posting sensitive exploit details publicly.
