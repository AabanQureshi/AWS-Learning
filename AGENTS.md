# AWS learning app

- Target .NET 10 only.
- User explicitly requested separate class libraries and a standalone .NET Worker Service. Keep the four-project layout: web host, Shared contracts/models, Services configuration/implementations, and Worker host. Shared must not depend on ASP.NET Core or AWS SDK packages.
- Each host registers ordinary singleton AWS clients for its own profile via the shared DI extensions; do not reintroduce keyed API/worker clients inside the web host.
- Keep code simple. No CQRS, MediatR, AutoMapper, or architectural scaffolding.
- Aaban writes AWS configuration and interactions in his IDE for hands-on learning. Guide and review; do not implement exercises unless explicitly asked.
- User has now implemented the services and explicitly requested their review, a daily exercise branch, and full web/controller/Worker integration for debugging. This authorization supersedes the earlier scaffolding-only and no-branch restrictions for this task.
- Keep the reviewed service baseline on `main`. Today's first document-flow exercise uses `day-1-documents`; keep its UI, controllers, and active Worker loop on that branch.
- The repository is intended to be public. Write README content for people learning AWS services with .NET, distinguishing implemented and planned functionality. Exclude personal machine paths and credentials.
- Use the configured local profiles and never embed credentials. Starting the real Worker and exercising the document flow is authorized for the integration exercise.
- Preserve the existing MVC template unless the user requests a change.
- Never commit AWS credential files, access keys, or secret access keys.
- `/health` is application liveness only. It does not check AWS or Floci connectivity.
