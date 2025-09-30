## Queue Processing SQL example

```sql
CREATE TABLE queue (
    id SERIAL PRIMARY KEY,
    task_name VARCHAR(255),
    status VARCHAR(50)
);

INSERT INTO queue (task_name, status)
SELECT 
    'Task ' || i AS task_name,
    'pending' AS status
FROM generate_series(1, 1000) i;

SELECT COUNT(*) FROM queue WHERE status = 'processing';
SELECT COUNT(*) FROM queue WHERE status = 'pending';

UPDATE queue
SET status = 'processing'
WHERE id IN (
    SELECT id 
    FROM queue
    WHERE status = 'pending'
    ORDER BY id
    LIMIT 100
    FOR UPDATE SKIP LOCKED
)
RETURNING *;
```

## Approaches

### Approach 1 - Update-based acquiring

1. App selects and marks records as processing (e.g. status = processing or acquired_at = now()).
2. App processes records (in app application logic)
3. App marks the records as processed (`status` and `processed_at`)

**Unfreezing:**

A background job resets to pending "frozen" rows - rows which are not processed, but where `acquired_at` was long ago.

### Approach 2 - Transaction-based acquiring

1. User opens a transaction and selects rows with FOR UPDATE (without updating them in any way)
2. App processes records (in app application logic)
3. App marks the records as processes and closes transaction.

**Unfreezing:**

Done by Database transaction completion - either via explicit rollback from `IDisposable` or by DB unfreezing mechanisms.