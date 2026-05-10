# OceanSwimmer.Api

The web app behind **[oceanswimmer.com.au](https://oceanswimmer.com.au)** — a
search and personal-profile site for Australian ocean-swim race results.
Search any swim by name or race, claim your own results, and see your career
stats over time.

## Tech stack

- **ASP.NET 8** (Web SDK, controllers + static files)
- **Dapper** for data access — talks to SQL Server directly, no ORM
- **SQL Server 2022** (running in Docker on the droplet)
- **Google + Facebook OAuth** for sign-in (`Microsoft.AspNetCore.Authentication.*`)
- **BCrypt.Net-Next** for password hashing
- **SendGrid** for transactional email
- **Static HTML / vanilla JS** front-end in `wwwroot/` — no framework, no build step
- **Deployed as a systemd service** on a Digital Ocean droplet behind Nginx +
  Let's Encrypt

## Project layout

```
OceanSwimmer.Api/
├── OceanSwimmer.Api/
│   ├── Controllers/                # API endpoints (swims, races, auth, athlete)
│   ├── Data/                       # Dapper queries / repositories
│   ├── Models/                     # DTOs
│   ├── Properties/
│   ├── wwwroot/                    # Static front-end
│   │   ├── images/                 # Logo, banner, favicons, OG image
│   │   ├── index.html              # Race results search (homepage)
│   │   ├── athlete.html            # Personal profile / claimed swims
│   │   ├── account.html            # Account settings
│   │   ├── login.html / register.html / forgot-password.html / reset-password.html
│   │   ├── feedback.html / feedback-thanks.html
│   │   └── privacy.html
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Production.json # NOT committed — secrets live here
│   └── OceanSwimmer.Api.csproj
└── README.md
```

## Local development

**Prerequisites:** .NET 8 SDK, Docker Desktop (for SQL Server), Visual Studio
or `dotnet` CLI.

**1. Start SQL Server in Docker** (one-off):

```bash
docker run -d --name sqlserver-local \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=<your-local-password>" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

**2. Configure the connection string** in `appsettings.Development.json`
(this file *is* gitignored — set it up locally only).

**3. Run the API:**

```bash
cd OceanSwimmer.Api
dotnet run
```

The site is at `http://localhost:5221` (or whatever port `launchSettings.json`
defines). Static files in `wwwroot/` are served at `/`, API endpoints under
`/swims`, `/races`, `/athlete`, `/auth`.

## Production deployment

The droplet runs:

- **OceanSwimmer.Api** as a systemd service (`oceanswimmer.service`), pointed
  at a published `out/` directory in the source tree
- **SQL Server** as a Docker container on the `oceanswimmer_default` network,
  managed via `~/oceanswimmer/docker-compose.yml`
- **Nginx** on the host as a reverse proxy with Let's Encrypt SSL
- *(Sibling sites)* `drinksexpress.com.au` and `icecoldclassic.com.au` run as
  Docker containers on the same network, behind the same Nginx

### Deploy workflow

After pushing changes to `main`:

```bash
ssh markc@oceanswimmer-prod

cd ~/oceanswimmer/oceanswimmer
git pull origin main

cd OceanSwimmer.Api
dotnet publish -c Release -o out

sudo systemctl restart oceanswimmer
sudo systemctl status oceanswimmer
```

`status` should show `active (running)` and the most recent log lines. If it's
`failed`, check `journalctl -u oceanswimmer -n 100 --no-pager` for the
exception.

### Why systemd, not Docker?

Unlike the Docker-based sibling sites, OceanSwimmer.Api runs natively as a
systemd-managed `dotnet` process because:

- The project predates the Docker setup on this droplet
- It needs direct file access to artefacts (publish output)
- Fast in-place restarts after `dotnet publish` are simpler than rebuilding
  a container

The SQL Server it depends on *is* in Docker, reachable on `127.0.0.1:1433`
(bound to localhost only).

### Restart / status / logs

```bash
sudo systemctl restart oceanswimmer
sudo systemctl status oceanswimmer
sudo journalctl -u oceanswimmer -n 200 --no-pager
sudo journalctl -u oceanswimmer -f                 # live tail
```

### Production secrets

`appsettings.Production.json` lives **on the droplet only** and is gitignored.
Contains:

- SQL Server connection string
- Google OAuth client ID / secret
- Facebook OAuth app ID / secret
- SendGrid API key
- Cookie / data-protection keys

If you ever need to rotate any of these, edit the file on the droplet and
restart the service.

## Branding / artwork

All artwork lives in `wwwroot/images/`:

| File | Purpose | Size |
|------|---------|------|
| `oceanswimmer-logo.png` | Square brand mark, transparent | 1024×1024 |
| `oceanswimmer-banner.png` | Horizontal banner (podium + goggles + wordmark), transparent | 600×200 |
| `oceanswimmer-square-navy.png` | Original master from the artist (navy bg) | 1024×1024 |
| `oceanswimmer-banner-navy.png` | Original master from the artist (navy bg) | 600×200 |
| `favicon-16x16.png` / `favicon-32x32.png` / `favicon.ico` | Browser tab icons | 16, 32 |
| `apple-touch-icon.png` | iOS home screen | 180×180 |
| `icon-192.png` / `icon-512.png` | PWA / Android | 192, 512 |
| `icon-1024.png` | Original 1024 master | 1024×1024 |
| `og-image.png` | Open Graph / social share preview | 1200×630 |

The transparent versions are generated by an edge-flood-fill script that
strips the navy background from the artist's masters. If new masters arrive,
re-run the script to regenerate everything.

## Domain & SSL

- **Domain:** `oceanswimmer.com.au` (root + `www.`)
- **DNS A records:** both → droplet IP
- **SSL:** Let's Encrypt via certbot, auto-renewing via the droplet's
  certbot timer
- **Nginx config:** `/etc/nginx/sites-available/oceanswimmer.com.au`
  (proxy_pass to `127.0.0.1:<api-port>`)

## Sibling sites

This droplet hosts three sites; OceanSwimmer is the original and the heaviest:

| Site | Type | Deployment |
|------|------|------------|
| oceanswimmer.com.au | ASP.NET 8 + SQL Server | systemd service + Docker (DB) |
| drinksexpress.com.au | ASP.NET 8 + SQL Server | Docker container |
| icecoldclassic.com.au | Static (nginx:alpine) | Docker container |

All three sit behind the same host Nginx with their own server blocks.
