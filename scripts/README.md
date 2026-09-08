# scripts

Manual maintenance for a **development** machine. Nothing here runs automatically, and nothing
here should ever be pointed at a production database.

## dev-cleanup.sql

Removes the leftover verification accounts (`devlink-`, `diag-`, `rctest-`, `smtpfail-`,
`verify-`, all `@stmark.local`) and clears the test data a diagnostic run wrote over
`staff@stmark.local`'s profile.

It is written to be run twice on purpose. As committed it ends in `ROLLBACK`, so the first run
shows you what would be deleted and changes nothing. Read the output, then change the last line
to `COMMIT` and run it again.

```bash
sqlcmd -S "(localdb)\mssqllocaldb" \
       -d "aspnet-TestUserLogIn-c1619119-eda2-46f7-8ee2-b9caec1f4b17" \
       -i scripts/dev-cleanup.sql
```

The database name comes from `ConnectionStrings:DefaultConnection` in
`StewardshipSurvey/appsettings.json`. Check it matches before running - that connection string
is the only thing standing between this script and the wrong database.

The delete order in the script is not arbitrary: `AspNetUsers.MemberID` is a `NO ACTION`
foreign key, so the user row has to go before the profile it points at. Deleting the profile
then cascades to that member's interest, involvement and service-role answers. This mirrors
`Services/DeactivatedUserPurgeService`, which had to solve the same problem.

## Rotating the JWT signing key

`Jwt:Key` lives in user secrets, never in a committed file. To rotate it, from the
`StewardshipSurvey` project folder:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

Any value of at least 32 bytes works; HMAC-SHA256 needs 256 bits and the application refuses a
shorter one with a message naming the setting.

**This now does something it did not use to.** Until bearer authentication was implemented, the
key only *signed* tokens and nothing ever validated them, so rotating it had no observable
effect. It is now the key the bearer handler validates against, which means:

- Every outstanding access token stops working immediately.
- Every outstanding refresh token stops working too, so clients must sign in again rather than
  refreshing.

That is the intended behaviour of a key rotation. It is worth doing on a development machine
whose key has been sitting in user secrets unrotated, and worth planning for before anything is
deployed.
