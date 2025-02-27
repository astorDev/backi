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