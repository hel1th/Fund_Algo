









CREATE TABLE users (
id SERIAL PRIMARY KEY,
name TEXT NOT NULL
);

CREATE TABLE orders (
id SERIAL PRIMARY KEY,
user_id INT REFERENCES users(id),
amount NUMERIC,
created_at TIMESTAMP DEFAULT now()
);

Напиши запрос который возвращает всех пользователей и сумму их заказов за последние 30 дней.
Пользователи без заказов тоже должны быть в результате с суммой 0.

SELECT u.id, u.name, COALESCE(SUM(o.amount), 0)
FROM users u
LEFT JOIN orders o ON o.user_id = u.id
WHERE o.created_at > NOW() - INTERVAL '30 days'
GROUP BY (u.id, u.name)
HAVING COALESCE(SUM(o.amount), 0) > 500;



type RateLimiter interface {
	Allow(ctx context.Context, userID int) bool
}

// < N RPS per user

type rateLim struct {
	nRPSPerUser int
	userRPS map[int][]time.Time
	mu sync.Mutex
}


func New(n int) *RateLimiter{
	return &RateLimiter{}
}


func (r *rateLim) Allow(ctx context.Context, userID int) bool {
	if err := ctx.Err(); err != nil {
		return false
	}

	now := time.Now()
	r.mu.Lock()
	usersReqs, ok := r.userRPS[userID]
	r.mu.Unlock()
	
	
	trimedReqLog := r.trimReqs(usersReqs, now)
	
	if len(trimedReqLog) >= r.nRPSPerUser {
		return false
	}
	
	trimedReqLog = append(trimedReqLog, now)
	
	r.mu.Lock()
	defer r.mu.Unlock()
	r.userRPS[userID] = trimedReqLog


	return true
}

func (r *rateLim) cleanUp() {
	
}



func (r *rateLim) trimReqs(reqs []time.Time, now time.Time) []time.Time {
	res := make([]time.Time, 0, r.nRPSPerUser)


	for _, req := range reqs {
		if req.Before(now.Add(-time.Second)) {
			continue
		}
		res = append(res, req)
	}

	return res
}












































