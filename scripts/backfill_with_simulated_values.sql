-- SQL Script to generate investment history with simulated value changes
-- This creates a more realistic history with value fluctuations over time
-- WARNING: This uses the CURRENT value as the END value and simulates backwards

-- OPTION 1: Simple backfill - single entry per investment at creation date
-- (Recommended for real data)
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
);

-- OPTION 2: Generate weekly entries with simulated value fluctuations
-- Uncomment to use this option instead of OPTION 1
/*
WITH RECURSIVE date_series AS (
    SELECT
        i."Id" AS "InvestmentId",
        i."CurrentValue",
        i."UnitValue",
        i."Quantity",
        i."CreatedDate" AS record_date,
        CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AS end_date,
        -- Calculate number of weeks between creation and now
        EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP AT TIME ZONE 'UTC' - i."CreatedDate")) / (7 * 24 * 60 * 60) AS weeks_count
    FROM "Investments" i

    UNION ALL

    SELECT
        "InvestmentId",
        "CurrentValue",
        "UnitValue",
        "Quantity",
        record_date + INTERVAL '1 week',
        end_date,
        weeks_count
    FROM date_series
    WHERE record_date + INTERVAL '1 week' <= end_date
),
numbered_series AS (
    SELECT
        *,
        ROW_NUMBER() OVER (PARTITION BY "InvestmentId" ORDER BY record_date) - 1 AS week_number,
        COUNT(*) OVER (PARTITION BY "InvestmentId") AS total_weeks
    FROM date_series
),
simulated_values AS (
    SELECT
        "InvestmentId",
        record_date,
        -- Simulate a growth pattern with some volatility
        -- Values start at 80% of current and grow to current with random fluctuations
        CASE
            WHEN total_weeks = 1 THEN "CurrentValue"
            ELSE
                "CurrentValue" * (
                    0.8 + (0.2 * week_number::DECIMAL / NULLIF((total_weeks - 1), 0))
                    + (RANDOM() * 0.1 - 0.05) -- +/- 5% random fluctuation
                )
        END AS "Value",
        CASE
            WHEN "UnitValue" IS NOT NULL AND total_weeks > 1 THEN
                "UnitValue" * (
                    0.8 + (0.2 * week_number::DECIMAL / NULLIF((total_weeks - 1), 0))
                    + (RANDOM() * 0.1 - 0.05)
                )
            ELSE "UnitValue"
        END AS "UnitValue",
        "Quantity"
    FROM numbered_series
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
    sv."InvestmentId",
    ROUND(sv."Value"::NUMERIC, 2) AS "Value",
    CASE
        WHEN sv."UnitValue" IS NOT NULL THEN ROUND(sv."UnitValue"::NUMERIC, 2)
        ELSE NULL
    END AS "UnitValue",
    sv."Quantity",
    sv.record_date AS "RecordedDate",
    'Historical value (simulated)' AS "Notes"
FROM simulated_values sv
WHERE NOT EXISTS (
    SELECT 1
    FROM "InvestmentHistories" h
    WHERE h."InvestmentId" = sv."InvestmentId"
    AND h."RecordedDate" = sv.record_date
)
ORDER BY sv."InvestmentId", sv.record_date;
*/

-- OPTION 3: Generate monthly entries with realistic progression
-- This creates a cleaner chart with monthly data points
/*
WITH RECURSIVE monthly_series AS (
    SELECT
        i."Id" AS "InvestmentId",
        i."CurrentValue",
        i."UnitValue",
        i."Quantity",
        DATE_TRUNC('month', i."CreatedDate")::TIMESTAMP WITH TIME ZONE AS record_date,
        DATE_TRUNC('month', CURRENT_TIMESTAMP AT TIME ZONE 'UTC')::TIMESTAMP WITH TIME ZONE AS end_date
    FROM "Investments" i

    UNION ALL

    SELECT
        "InvestmentId",
        "CurrentValue",
        "UnitValue",
        "Quantity",
        (record_date + INTERVAL '1 month')::TIMESTAMP WITH TIME ZONE,
        end_date
    FROM monthly_series
    WHERE record_date + INTERVAL '1 month' <= end_date
),
numbered_monthly AS (
    SELECT
        *,
        ROW_NUMBER() OVER (PARTITION BY "InvestmentId" ORDER BY record_date) - 1 AS month_number,
        COUNT(*) OVER (PARTITION BY "InvestmentId") AS total_months
    FROM monthly_series
),
monthly_values AS (
    SELECT
        "InvestmentId",
        record_date,
        -- Linear growth from 75% to 100% of current value
        CASE
            WHEN total_months = 1 THEN "CurrentValue"
            ELSE
                "CurrentValue" * (
                    0.75 + (0.25 * month_number::DECIMAL / NULLIF((total_months - 1), 0))
                )
        END AS "Value",
        CASE
            WHEN "UnitValue" IS NOT NULL AND total_months > 1 THEN
                "UnitValue" * (
                    0.75 + (0.25 * month_number::DECIMAL / NULLIF((total_months - 1), 0))
                )
            ELSE "UnitValue"
        END AS "UnitValue",
        "Quantity"
    FROM numbered_monthly
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
    mv."InvestmentId",
    ROUND(mv."Value"::NUMERIC, 2) AS "Value",
    CASE
        WHEN mv."UnitValue" IS NOT NULL THEN ROUND(mv."UnitValue"::NUMERIC, 2)
        ELSE NULL
    END AS "UnitValue",
    mv."Quantity",
    mv.record_date AS "RecordedDate",
    'Monthly value (backfilled)' AS "Notes"
FROM monthly_values mv
WHERE NOT EXISTS (
    SELECT 1
    FROM "InvestmentHistories" h
    WHERE h."InvestmentId" = mv."InvestmentId"
    AND DATE_TRUNC('month', h."RecordedDate") = DATE_TRUNC('month', mv.record_date)
)
ORDER BY mv."InvestmentId", mv.record_date;
*/

-- Verify the results
SELECT
    i."Name",
    i."CurrentValue",
    COUNT(h."Id") AS history_entries,
    MIN(h."RecordedDate") AS first_entry,
    MAX(h."RecordedDate") AS last_entry,
    MIN(h."Value") AS min_value,
    MAX(h."Value") AS max_value
FROM "Investments" i
LEFT JOIN "InvestmentHistories" h ON i."Id" = h."InvestmentId"
GROUP BY i."Id", i."Name", i."CurrentValue"
ORDER BY i."Name";
