# Secrets Management

## Policy
1. **No Secrets in Code**: All secrets MUST be injected via environment or secure configuration.
2. **Encryption at Rest**: Use Azure Key Vault or AWS KMS for production.
3. **Rotation**: Rotate secrets every 90 days.

## Current Implementation
- **Development**: `appsettings.Development.json` (excluded from git).
- **Production**: Environment variables + Azure Key Vault.

## Never Commit
- `.env`
- `appsettings.*.json` (except `appsettings.json` template)
- `secrets.json`
- Private keys/certificates

## Future Improvements
- Integrate HashiCorp Vault.
- Add secrets scanning in CI.
