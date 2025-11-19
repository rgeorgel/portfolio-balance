# Docker Setup Guide

This guide explains how to run the Portfolio Balance application using Docker and Docker Compose.

## Prerequisites

- Docker 20.10 or higher
- Docker Compose V2 (or docker-compose 1.29+)

## Quick Start

Start all services (database, backend, and frontend) with a single command:

```bash
docker compose up -d
```

Or if using older docker-compose:

```bash
docker-compose up -d
```

The application will be available at:
- **Frontend**: http://localhost:8080
- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **PostgreSQL**: localhost:5432

## Services

### PostgreSQL Database
- **Image**: postgres:15
- **Port**: 5432
- **Database**: portfoliobalance
- **Username**: postgres
- **Password**: postgres

### Backend (.NET API)
- **Technology**: ASP.NET Core 8.0
- **Port**: 5000
- **Health**: Waits for PostgreSQL to be ready before starting

### Frontend (Nginx)
- **Technology**: Nginx serving static HTML/CSS/JS
- **Port**: 8080 (mapped to container port 80)

## Common Commands

### Start Services
```bash
# Start all services in detached mode
docker compose up -d

# Start and rebuild images
docker compose up -d --build

# View logs
docker compose logs -f

# View logs for specific service
docker compose logs -f backend
```

### Stop Services
```bash
# Stop services (keeps data)
docker compose stop

# Stop and remove containers (keeps data)
docker compose down

# Stop, remove containers and volumes (DELETES ALL DATA!)
docker compose down -v
```

### Database Management
```bash
# Access PostgreSQL directly
docker exec -it portfoliobalance-db psql -U postgres -d portfoliobalance

# Backup database
docker exec portfoliobalance-db pg_dump -U postgres portfoliobalance > backup.sql

# Restore database
docker exec -i portfoliobalance-db psql -U postgres portfoliobalance < backup.sql
```

### View Container Status
```bash
# List running containers
docker compose ps

# Check container health
docker inspect portfoliobalance-db --format='{{.State.Health.Status}}'
```

### Rebuilding Services
```bash
# Rebuild backend only
docker compose build backend

# Rebuild frontend only
docker compose build frontend

# Rebuild all services
docker compose build
```

## First Time Setup

When running for the first time, the backend automatically applies Entity Framework migrations. However, if you need to manually manage migrations:

```bash
# Access backend container
docker exec -it portfoliobalance-backend /bin/bash

# Inside the container, you can run dotnet commands
# Note: EF tools need to be installed separately for this
```

It's easier to run migrations from your local development environment:

```bash
cd backend/PortfolioBalance
dotnet ef database update
```

## Network Configuration

All services are connected via a custom bridge network called `portfolio-network`. This allows:
- Backend to connect to PostgreSQL using hostname `postgres`
- Services to communicate securely without exposing unnecessary ports
- Isolation from other Docker networks

## Environment Variables

### Backend
The backend accepts these environment variables (configured in docker-compose.yml):

- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: The URL the app listens on
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string

To customize, edit `docker-compose.yml` or create a `.env` file:

```env
POSTGRES_PASSWORD=your_secure_password
```

## Volumes

The `postgres_data` volume persists database data. To reset the database:

```bash
docker compose down -v
docker compose up -d
```

## Troubleshooting

### Backend won't start
```bash
# Check backend logs
docker compose logs backend

# Ensure PostgreSQL is healthy
docker compose ps
```

### Frontend shows CORS errors
- Ensure backend is running: `docker compose ps`
- Check backend logs: `docker compose logs backend`
- Verify the API URL in browser console

### Database connection errors
```bash
# Check if PostgreSQL is healthy
docker compose logs postgres

# Verify connection from backend
docker exec -it portfoliobalance-backend /bin/bash
# Then try: ping postgres
```

### Port already in use
If you get "port already allocated" errors:

```bash
# Check what's using the port (example for port 5000)
lsof -i :5000
# or
netstat -tuln | grep 5000

# Either stop the conflicting service or change ports in docker-compose.yml
```

### Rebuild from scratch
```bash
# Stop everything and remove volumes
docker compose down -v

# Remove images
docker compose rm -f
docker rmi portfoliobalance-backend portfoliobalance-frontend

# Rebuild and start
docker compose up -d --build
```

## Development Workflow

### Making Backend Changes
1. Update code in `backend/PortfolioBalance/`
2. Rebuild: `docker compose build backend`
3. Restart: `docker compose up -d backend`

### Making Frontend Changes
1. Update code in `frontend/`
2. Rebuild: `docker compose build frontend`
3. Restart: `docker compose up -d frontend`

### Faster Development
For active development, consider running services locally instead of in Docker for faster iteration. Use Docker only for PostgreSQL:

```bash
# Start only PostgreSQL
docker compose up -d postgres

# Run backend locally
cd backend/PortfolioBalance
dotnet run

# Serve frontend locally
cd frontend
python -m http.server 8080
```

## Production Considerations

For production deployment, consider:

1. **Use production-grade secrets**: Don't use default passwords
2. **Configure HTTPS**: Add SSL/TLS certificates and reverse proxy
3. **Resource limits**: Add memory and CPU limits to services
4. **Health checks**: Already configured for PostgreSQL, add for backend
5. **Logging**: Configure centralized logging
6. **Monitoring**: Add monitoring tools like Prometheus/Grafana
7. **Backups**: Implement automated database backups

Example production additions to docker-compose.yml:

```yaml
backend:
  deploy:
    resources:
      limits:
        cpus: '1'
        memory: 512M
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
    interval: 30s
    timeout: 10s
    retries: 3
```

## Architecture

```
┌─────────────────────────────────────────────────┐
│                  Host Machine                    │
│                                                  │
│  Browser ──> localhost:8080 ──> Frontend (Nginx)│
│       │                              │           │
│       │                              │           │
│       └──> localhost:5000 ──> Backend (.NET)    │
│                                      │           │
│                                      ▼           │
│                          postgres:5432 (DB)     │
│                                                  │
│         All connected via portfolio-network     │
└─────────────────────────────────────────────────┘
```

The frontend JavaScript runs in the browser and connects to the backend via localhost:5000, which is exposed from the Docker container.
