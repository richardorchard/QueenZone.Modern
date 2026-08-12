# Member account deletion

Issue [#586](https://github.com/richardorchard/QueenZone.Modern/issues/586) defines the self-service account deletion policy. This note also records the account-deletion decision that affects private-message retention in [#473](https://github.com/richardorchard/QueenZone.Modern/issues/473).

## Lifecycle

1. The signed-in member confirms deletion by typing `DELETE` on `/account/delete`.
2. The request immediately changes the public display name and retained-content attribution to `Deleted member`, hides the member profile and avatar, signs the member out, and starts a 30-day cooling-off period. The original display name and avatar blob path are held in private recovery fields.
3. The member can sign back in during the cooling-off period and cancel deletion from `/account/delete`. Cancellation atomically restores the display name, avatar reference, profile, and retained-content attribution, clears the recovery fields, and clears the pending deletion date.
4. A background service permanently deletes due accounts after 30 days. It suspends the account, deletes the stored avatar, purges the email address, password hash, external login rows, last-login timestamp, and private recovery fields, and makes retained-content anonymisation permanent.
5. A non-personal member tombstone remains so retained content keeps valid database relationships. `LinkedLegacyUserId` remains unchanged under the current policy.

Deletion requests, cancellations, and completed purges are recorded in `MemberAccountDeletionAuditLog`. Audit rows contain the member account ID, action, and timestamp. They do not contain email addresses or other copied personal data.

## Retained content

- Modern forum posts remain visible. Their stored author name changes to `Deleted member`, and `AuthorMemberId` is cleared so they no longer link to a member profile.
- Thread-starter attribution and indexed community-article attribution change to `Deleted member`.
- Published contributions that resolve attribution from `MemberAccounts` display the tombstone name.
- Private-message bodies and conversation rows remain for the other participant. Sender and participant relationships remain, but visible attribution resolves to `Deleted member`.
- Pending and permanently deleted accounts are excluded from recipient search. Existing conversations cannot receive new replies while either participant has a pending or completed deletion.

This policy does not settle the wider moderation and reported-message retention work in #473. Moderator access, access auditing, and legal hold rules remain in that issue.
