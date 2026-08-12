# Member account deletion

Issue [#586](https://github.com/richardorchard/QueenZone.Modern/issues/586) defines the self-service account deletion policy. This note also records the account-deletion decision that affects private-message retention in [#473](https://github.com/richardorchard/QueenZone.Modern/issues/473).

## Lifecycle

1. The signed-in member confirms deletion by typing `DELETE` on `/account/delete`.
2. The request signs the member out and starts a 30-day cooling-off period. The account remains active and its data, profile, avatar, and content attribution stay unchanged.
3. The member can sign back in during the cooling-off period and cancel deletion from `/account/delete`. Cancellation clears the pending deletion date.
4. A background service permanently deletes due accounts after 30 days. It suspends the account, removes the avatar, purges the email address, password hash, external login rows, and last-login timestamp, and anonymises retained content.
5. A non-personal member tombstone remains so retained content keeps valid database relationships. `LinkedLegacyUserId` remains unchanged under the current policy.

Deletion requests, cancellations, and completed purges are recorded in `MemberAccountDeletionAuditLog`. Audit rows contain the member account ID, action, and timestamp. They do not contain email addresses or other copied personal data.

## Retained content

- Modern forum posts remain visible. Their stored author name changes to `Deleted member`, and `AuthorMemberId` is cleared so they no longer link to a member profile.
- Thread-starter attribution and indexed community-article attribution change to `Deleted member`.
- Published contributions that resolve attribution from `MemberAccounts` display the tombstone name.
- Private-message bodies and conversation rows remain for the other participant. Sender and participant relationships remain, but visible attribution resolves to `Deleted member`.
- Pending-deletion accounts remain available in recipient search and existing conversations during the cooling-off period.
- Permanently deleted accounts are excluded from recipient search. Existing conversations cannot receive new replies after either participant's deletion becomes permanent.

This policy does not settle the wider moderation and reported-message retention work in #473. Moderator access, access auditing, and legal hold rules remain in that issue.
