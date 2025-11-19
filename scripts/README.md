# Database Scripts

## Investment History Backfill Scripts

These scripts help you populate historical data for existing investments in your database.

### Script 1: `backfill_investment_history.sql` (Recommended)

**Purpose:** Creates a single historical entry for each existing investment at its creation date.

**When to use:**
- You have existing investments and want to start tracking history going forward
- You want accurate historical data without simulation

**What it does:**
- Inserts one history entry per investment using its `CreatedDate`
- Uses the current values from the investment
- Marks entries as "Initial investment (backfilled)"

**How to run:**
```bash
# Using psql
psql -h localhost -U postgres -d portfoliobalance -f scripts/backfill_investment_history.sql

# Or copy-paste the SQL directly into your PostgreSQL client
```

### Script 2: `backfill_with_simulated_values.sql` (For Demo/Testing)

**Purpose:** Creates multiple historical entries with simulated value changes over time.

**When to use:**
- You want to see how the charts look with historical data
- You're demoing the application
- You want to visualize investment growth patterns

**Options available:**

#### Option 1: Simple Backfill (Default - ACTIVE)
- Same as Script 1
- One entry per investment at creation date

#### Option 2: Weekly Entries with Simulated Fluctuations (COMMENTED OUT)
- Creates weekly historical entries
- Simulates value growth from 80% to 100% of current value
- Adds random fluctuations (±5%) for realistic charts
- Good for: Recent investments (last few months)

To use: Uncomment the section marked "OPTION 2" in the script

#### Option 3: Monthly Entries with Linear Growth (COMMENTED OUT)
- Creates monthly historical entries
- Simulates linear growth from 75% to 100% of current value
- No random fluctuations - cleaner charts
- Good for: Long-term investments (6+ months old)

To use: Uncomment the section marked "OPTION 3" in the script

**How to run:**
```bash
# 1. Edit the file and uncomment your preferred option (2 or 3)
# 2. Comment out or remove OPTION 1 if using 2 or 3
# 3. Run the script:

psql -h localhost -U postgres -d portfoliobalance -f scripts/backfill_with_simulated_values.sql
```

### Important Notes

⚠️ **Before running these scripts:**
1. Backup your database
2. The scripts check for existing history to avoid duplicates
3. You can run the scripts multiple times safely (they use `NOT EXISTS` checks)

⚠️ **About simulated data:**
- Simulated values are for visualization/demo purposes only
- They work backwards from the current value
- They assume growth - may not reflect actual investment performance
- For production use, use real historical data or just OPTION 1

### Example Workflow

**For Production Use:**
```bash
# 1. Run the simple backfill
psql -h localhost -U postgres -d portfoliobalance -f scripts/backfill_investment_history.sql

# 2. From now on, history is tracked automatically when you update investments
```

**For Demo/Testing:**
```bash
# 1. Edit backfill_with_simulated_values.sql
# 2. Uncomment OPTION 3 (monthly data)
# 3. Comment out OPTION 1
# 4. Run the script
psql -h localhost -U postgres -d portfoliobalance -f scripts/backfill_with_simulated_values.sql
```

### Verification

Both scripts include a verification query at the end that shows:
- Investment name
- Current value
- Number of history entries created
- Date range of entries
- Min/max values in history

Check this output to confirm the backfill worked as expected.

### Future History Tracking

After running these scripts once, you don't need to run them again. The application will automatically:
- Create history entries when new investments are added
- Update history when investment values change
- Track all future changes automatically

You can still use the History page UI to add manual entries if needed.
