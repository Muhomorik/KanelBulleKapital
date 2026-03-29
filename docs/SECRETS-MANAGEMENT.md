# Configuration & Secrets Guide

> Complete reference for all environment variables, secrets, and configuration across the PDF Q&A Application.



## Security Best Practices

### Do

- Use User Secrets for local development (never commit API keys)
- Use Azure Key Vault for production (secure, centralized)
- Rotate API keys regularly (every 90 days recommended)
- Use Managed Identity (no credentials in code)
- Grant least privilege access to secrets

### Don't

- Commit secrets to Git (check `.gitignore`)
- Hardcode API keys in source code
- Share User Secrets files between developers
- Use production secrets in local development
- Log secret values (ensure logging doesn't expose sensitive data)
- Put secrets in `NEXT_PUBLIC_` environment variables

---

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Overview](https://docs.microsoft.com/azure/key-vault/general/overview)
- [Managed Identity Best Practices](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/best-practice-recommendations)
- [GitHub Secrets Documentation](https://docs.github.com/actions/security-guides/encrypted-secrets)
- [Next.js Environment Variables](https://nextjs.org/docs/basic-features/environment-variables)
