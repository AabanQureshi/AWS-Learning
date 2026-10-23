# AWS learning app

- Target .NET 10 only.
- Keep code simple. No CQRS, MediatR, AutoMapper, or architectural scaffolding.
- Aaban writes AWS configuration and interactions in his IDE for hands-on learning. Guide and review; do not implement exercises unless explicitly asked.
- `main` is a clean, runnable baseline. Keep daily service interactions and resource configuration on exercise branches.
- Create each daily branch directly from `main`; the first branch is named `Day-1`. Do not merge exercises into `main` without a request.
- Existing AWS SDK package references are part of the baseline. Do not add AWS clients, credentials, endpoint configuration, workers, or resource operations to `main`.
- Preserve the existing MVC template unless the user requests a change.
- Never commit AWS credential files, access keys, or secret access keys.
- `/health` is application liveness only. It does not check AWS or Floci connectivity.
