-- SQL Script to generate investment history for existing investments
-- This script creates initial history entries for all investments that don't have any history yet

-- Insert initial history entries for investments without any history
INSERT INTO "InvestmentHistories" (
    "InvestmentId",
    "Value",
    "UnitValue",
    "Quantity",
    "RecordedDate",
    "Notes"
)
SELECT
    i."Id" AS "InvestmentId",
    i."CurrentValue" AS "Value",
    i."UnitValue",
    i."Quantity",
    i."CreatedDate" AS "RecordedDate",
    'Initial investment (backfilled)' AS "Notes"
FROM "Investments" i
WHERE NOT EXISTS (
    SELECT 1
    FROM "InvestmentHistories" h
    WHERE h."InvestmentId" = i."Id"
)
ORDER BY i."Id";

-- Optional: Create additional history entries at regular intervals (monthly)
-- from CreatedDate to now with the current value
-- This gives a more complete history visualization
-- Uncomment the section below if you want to create multiple entries:

/*
WITH RECURSIVE date_series AS (
    -- Start from each investment's created date
    SELECT
        i."Id" AS "InvestmentId",
        i."CurrentValue",
        i."UnitValue",
        i."Quantity",
        DATE_TRUNC('month', i."CreatedDate") AS record_date,
        DATE_TRUNC('month', CURRENT_TIMESTAMP AT TIME ZONE 'UTC') AS end_date
    FROM "Investments" i

    UNION ALL

    -- Generate monthly dates until current date
    SELECT
        "InvestmentId",
        "CurrentValue",
        "UnitValue",
        "Quantity",
        record_date + INTERVAL '1 month',
        end_date
    FROM date_series
    WHERE record_date + INTERVAL '1 month' <= end_date
)
INSERT INTO "InvestmentHistories" (
    "InvestmentId",
    "Value",
    "UnitValue",
    "Quantity",
    "RecordedDate",
    "Notes"
)
SELECT
    ds."InvestmentId",
    ds."CurrentValue" AS "Value",
    ds."UnitValue",
    ds."Quantity",
    ds.record_date AS "RecordedDate",
    'Historical value (backfilled)' AS "Notes"
FROM date_series ds
WHERE NOT EXISTS (
    SELECT 1
    FROM "InvestmentHistories" h
    WHERE h."InvestmentId" = ds."InvestmentId"
    AND DATE_TRUNC('day', h."RecordedDate") = DATE_TRUNC('day', ds.record_date)
)
ORDER BY ds."InvestmentId", ds.record_date;
*/

-- Verify the results
SELECT
    i."Name",
    i."CurrentValue",
    COUNT(h."Id") AS history_entries_count,
    MIN(h."RecordedDate") AS first_entry,
    MAX(h."RecordedDate") AS last_entry
FROM "Investments" i
LEFT JOIN "InvestmentHistories" h ON i."Id" = h."InvestmentId"
GROUP BY i."Id", i."Name", i."CurrentValue"
ORDER BY i."Name";
