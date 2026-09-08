/*
    Development database cleanup - leftover verification accounts and a polluted profile.

    READ THIS BEFORE RUNNING IT.

    This script deletes rows. It is written to be run in two passes: the SELECTs at the top
    show you exactly what will go, and the whole change is wrapped in a transaction that ends
    in ROLLBACK. Run it as-is first, read the output, and only then change the last line to
    COMMIT.

    Target: the LocalDB development database in appsettings.json. Never run this anywhere else.

    Delete order matters. AspNetUsers.MemberID is a foreign key with NO ACTION, so the user row
    must go before the profile row it points at; deleting the profile then cascades to that
    member's interest, involvement and service-role answers. This is the same order
    DeactivatedUserPurgeService uses and for the same reason - see the comment at
    Services/DeactivatedUserPurgeService.cs.
*/

-- QUOTED_IDENTIFIER must be ON to delete from AspNetUsers: Identity's unique indexes on the
-- normalised name and email columns refuse a DELETE otherwise (Msg 1934). sqlcmd defaults it
-- OFF, unlike SSMS and every application connection, so a script that works when pasted into
-- SSMS fails from the command line without this line.
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. The five leftover verification accounts.
-- ---------------------------------------------------------------------------
-- Created by earlier diagnostic runs. All are @stmark.local with a known prefix; the LIKE
-- patterns are deliberately narrow so a real member address cannot match one by accident.

DECLARE @doomed TABLE (UserId NVARCHAR(450) PRIMARY KEY, Email NVARCHAR(256), MemberID INT NULL);

INSERT INTO @doomed (UserId, Email, MemberID)
SELECT Id, Email, MemberID
FROM   AspNetUsers
WHERE  Email LIKE 'devlink-%@stmark.local'
   OR  Email LIKE 'diag-%@stmark.local'
   OR  Email LIKE 'rctest-%@stmark.local'
   OR  Email LIKE 'smtpfail-%@stmark.local'
   OR  Email LIKE 'verify-%@stmark.local';

PRINT '--- Accounts that will be deleted ---';
SELECT Email, MemberID FROM @doomed ORDER BY Email;

PRINT '--- Survey answers that will go with them ---';
SELECT 'MemberInterests' AS TableName, COUNT(*) AS Rows
FROM   MemberInterests WHERE MemberID IN (SELECT MemberID FROM @doomed WHERE MemberID IS NOT NULL)
UNION ALL
SELECT 'MemberInvolvements', COUNT(*)
FROM   MemberInvolvements WHERE MemberID IN (SELECT MemberID FROM @doomed WHERE MemberID IS NOT NULL)
UNION ALL
SELECT 'MemberServiceRoles', COUNT(*)
FROM   MemberServiceRoles WHERE MemberID IN (SELECT MemberID FROM @doomed WHERE MemberID IS NOT NULL);

-- Identity's own child tables have no cascade configured here, so they go explicitly.
DELETE FROM AspNetUserRoles  WHERE UserId IN (SELECT UserId FROM @doomed);
DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT UserId FROM @doomed);
DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT UserId FROM @doomed);
DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT UserId FROM @doomed);

-- The user row first: it is the side holding the NO ACTION foreign key.
DELETE FROM AspNetUsers WHERE Id IN (SELECT UserId FROM @doomed);

-- Then the profile, which cascades to the three answer tables.
DELETE FROM MemberInfos
WHERE  MemberID IN (SELECT MemberID FROM @doomed WHERE MemberID IS NOT NULL);

-- ---------------------------------------------------------------------------
-- 2. staff@stmark.local - a real account with test data written over its profile.
-- ---------------------------------------------------------------------------
-- A verification run left it reading "Test Staffer" with a contact preference of Email. It had
-- no name before, so the honest repair is to clear those fields rather than invent a name.
-- The account itself stays: it is a working staff login.

PRINT '--- staff@stmark.local before ---';
SELECT m.MemberID, m.FirstName, m.LastName, m.PrefersEmail, m.PrefersPhone, m.PrefersText
FROM   MemberInfos m
JOIN   AspNetUsers u ON u.MemberID = m.MemberID
WHERE  u.Email = 'staff@stmark.local';

-- Empty strings, not NULL. The C# properties are `string?`, which is misleading: both carry
-- [Required], so the columns are NOT NULL and a NULL here fails with Msg 515. Empty is the
-- closest the schema allows to the "no name" this account had before.
UPDATE m
SET    m.FirstName    = '',
       m.LastName     = '',
       m.PrefersEmail = 0,
       m.PrefersPhone = 0,
       m.PrefersText  = 0,
       m.UpdatedDate  = SYSUTCDATETIME()
FROM   MemberInfos m
JOIN   AspNetUsers u ON u.MemberID = m.MemberID
WHERE  u.Email = 'staff@stmark.local'
  AND  m.FirstName = 'Test'
  AND  m.LastName  = 'Staffer';   -- only if it still looks like the test data

PRINT '--- staff@stmark.local after ---';
SELECT m.MemberID, m.FirstName, m.LastName, m.PrefersEmail, m.PrefersPhone, m.PrefersText
FROM   MemberInfos m
JOIN   AspNetUsers u ON u.MemberID = m.MemberID
WHERE  u.Email = 'staff@stmark.local';

-- ---------------------------------------------------------------------------
-- Change to COMMIT once the output above is what you expected.
-- ---------------------------------------------------------------------------
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
