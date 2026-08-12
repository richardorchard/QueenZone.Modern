# Member account deletion

Issue [#586](https://github.com/richardorchard/QueenZone.Modern/issues/586) defines the self-service account deletion policy. This note also records the account-deletion decision that affects private-message retention in [#473](https://github.com/richardorchard/QueenZone.Modern/issues/473).

## Lifecycle

1. The signed-in member confirms deletion by typing `DELETE` on `/account/delete`.
2. The request immediately suspends the account, signs the member out, removes the avatar, hides the member profile, and changes retained public attribution to `Deleted member`.
3. A background service purges due personal sign-in data after 30 days. It removes the email address, password hash, external login rows, and last-login timestamp.
4. A non-personal member tombstone remains so retained content keeps valid database relationships. `LinkedLegacyUserId` remains unchanged under the current policy.

Deletion requests and completed purges are recorded in `MemberAccountDeletionAuditLog`. Audit rows contain the member account ID, action, and timestamp. They do not contain email addresses or other copied personal data.

## Retained content

- Modern forum posts remain visible. Their stored author name changes to `Deleted member`, and `AuthorMemberId` is cleared so they no longer link to a member profile.
- Thread-starter attribution and indexed community-article attribution change to `Deleted member`.
- Published contributions that resolve attribution from `MemberAccounts` display the tombstone name.
- Private-message bodies and conversation rows remain for the other participant. Sender and participant relationships remain, but visible attribution resolves to `Deleted member`.
- Deleted accounts are excluded from recipient search. Existing conversations cannot receive new replies after either participant deletes their account.

This policy does not settle the wider moderation and reported-message retention work in #473. Moderator access, access auditing, and legal hold rules remain in that issue.
