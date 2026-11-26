# Claude Conversation 1

## URL

https://claude.ai/share/44aadb07-3625-4a50-8815-5a6f785b5384

## Conversation 


I want to use GitHub spec-kit with Claude  for my next project.  But I don’t understand how multiple developers can use it to implement features concurrently then update the master spec.md file.

Searched the web, read a page

There is no master spec.md in spec-kit. Each feature gets its own folder, so there's nothing to merge into one file. The model is:

Per feature: specs/001-leave-approval-workflow/ holds spec.md, plan.md and tasks.md. /speckit.specify creates a git branch named from the feature description and writes spec.md into the matching folder. github
xebia
Shared across features: the constitution. In team contexts it works as the team's architectural and process charter, and it keeps individual developers from making inconsistent decisions. Microsoft's training module suggests naming senior developers or architects as constitution maintainers to review changes. microsoft
microsoft

How concurrent work happens

Each developer works on their own feature branch and folder, so spec files rarely conflict. Spec Kit commands detect the active feature from your current Git branch, so switching branches switches specs. You merge via normal PRs, and the spec folder is reviewed alongside the code. githubusercontent

Gotchas

Feature numbering collides. When a team branches off a common branch, the auto-generated feature numbers will naturally conflict. Agree on a convention, such as ticket IDs or a person's prefix, or have one person allocate numbers. github
Constitution edits are the real contention point. Treat them like architecture changes, with a dedicated PR and review.
Cross-feature consistency is manual. /speckit.analyze checks that specs, plans and tasks are consistent with each other and the constitution, but it's worth running after merging parallel features. microsoft

Keeping specs current after implementation

The repo describes a convergence loop: repeat implement and converge until convergence reports Converged. It also links an "evolving existing specs" guide, which I couldn't read. Practically, specs tend to be a per-feature record, not living documentation. If you want a living "system overview," you'd write and maintain that yourself, for example a top-level doc updated in each feature's PR.

Want me to sketch a branch and PR workflow for a team of 3–5 using Claude Code with this?

Yes

Here's a workflow for 3–5 developers. These are my suggestions, not official spec-kit guidance.

One-time setup (one person)

Run specify init and write the constitution. Include your Python and Azure conventions: testing, logging, auth, IaC.
Add a CODEOWNERS entry so .specify/memory/constitution.md needs an architect's review.
Commit a docs/architecture.md as the hand-maintained system overview. Spec-kit doesn't generate this.

Per feature (each developer)

Claim the number first. Create a ticket or issue, and use its ID as the feature number so branches don't collide.
Branch and specify. Run /speckit.specify, then /speckit.clarify.
Open a spec-only draft PR after clarify. Teammates review the spec before any code is written, which is the cheapest place to catch problems.
Plan and tasks. Run /speckit.plan and /speckit.tasks, and push them to the same PR.
Implement. Run /speckit.implement, then /speckit.analyze to check consistency.
Update the overview. If the feature changed the system's shape, edit docs/architecture.md in the same PR.
Merge the PR. The spec folder and the code land together.

Rules that keep it smooth

Rebase often. Rebase on main at least daily so the agent sees what others merged. Stale branches cause agents to build duplicate or conflicting code.
Constitution changes get their own PR. Merge them first, then everyone rebases.
Split shared work. If two features touch the same module, split out the shared piece as its own small spec and merge it first.
Don't edit merged specs for new work. Write a new feature spec that references the old one. This leaves a clear history.
Add a CI check. Fail the PR if a feature folder has code changes but no tasks.md updates.

Using Claude Code

Use one Claude Code session per branch. Git worktrees let each developer run two features in parallel without switching branches.
Commit CLAUDE.md pointing to the constitution and docs/architecture.md, so every session starts with the same context.

Want a starter CODEOWNERS, PR template and CI check for this?