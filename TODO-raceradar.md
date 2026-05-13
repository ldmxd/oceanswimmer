# TODO — RaceRadar multi-sport architecture

Migrate the OceanSwimmer back-end to support multiple race disciplines (ocean
swims, running races, triathlons) under a single shared back-end, with
**oceanswimmer.com.au** and **raceradar.com.au** as separate front-ends sharing
the same data.

## Why

- `raceradar.com.au` is registered and ready for a multi-sport platform
- Existing OceanSwimmer infrastructure (DB, athlete claims, auth, droplet,
  SEO setup) can be reused — don't rebuild
- One user identity should claim swims, runs, *and* tris seamlessly

## Architecture

### Single database, discipline-specific tables

Keep everything in the existing `OceanSwimmer` SQL Server database. Pros:
- One auth tier, one athletes table, one droplet, one set of credentials
- Cross-discipline queries become trivial union views
- Backups, monitoring, deploys stay simple

### Schema additions

```
dbo.Races                    -- NEW: master race table
  RaceId         int PK
  Name           nvarchar
  Date           date
  DisciplineType varchar(10)  -- 'swim' | 'run' | 'tri'
  Distance       decimal
  City           nvarchar
  ...

dbo.OceanSwims               -- EXISTING: refactor to FK Races.RaceId
dbo.Runs                     -- NEW: results for running races
dbo.TriathlonRaces           -- NEW: master tri results
  TriResultId   int PK
  RaceId        int FK -> Races
  AthleteId     int FK -> Athletes
  OverallTime, OverallPosition, ...

dbo.TriathlonLegs            -- NEW: leg-level detail
  TriResultId   int FK
  LegType       varchar(10)   -- 'swim' | 't1' | 'bike' | 't2' | 'run'
  Distance, Time, Position

dbo.Athletes                 -- EXISTING: discipline-agnostic
auth.*                       -- EXISTING: shared

staging.RaceResults_Import   -- RENAMED from TypedResult_OceanSwims_History
                             -- discipline-aware import staging

dbo.vw_AllResults_Search     -- NEW: union view across all disciplines
```

### Front-ends

| Domain | Queries | Audience |
|--------|---------|----------|
| `oceanswimmer.com.au` | `dbo.OceanSwims` directly | Ocean swimmers, keeps existing SEO |
| `raceradar.com.au` | `vw_AllResults_Search` + per-discipline drill-downs | Multi-sport athletes |

Both served from the same droplet + DB. Could even share the same systemd
service with domain-aware routing, or run as separate processes — TBD.

## Migration order (pragmatic)

### Phase 1: Foundation (no user-visible changes)
- [ ] Create `dbo.Races` master table
- [ ] Backfill from existing OceanSwims data
- [ ] Refactor `OceanSwims` to FK `Races.RaceId` instead of denormalised race fields
- [ ] Rename `TypedResult_OceanSwims_History` → `staging.RaceResults_Import`
  (update import scripts)
- [ ] Verify OceanSwimmer.com.au still works (regression test)

### Phase 2: Running races
- [ ] Find the City2Surf results source — MultisportAustralia? Their public
      results page? Direct from the organiser?
- [ ] Build the import pipeline (similar to OceanSwims importer)
- [ ] Create `dbo.Runs` table
- [ ] Import City2Surf 2024, 2025, 2026 as proof
- [ ] Stand up basic raceradar.com.au front-end showing runs

### Phase 3: Multi-discipline UX
- [ ] Build `dbo.vw_AllResults_Search` union view
- [ ] Athlete profile shows all disciplines, not just swims
- [ ] Search "Mark Drinkwater" returns swims AND runs
- [ ] Race claim flow works for runs

### Phase 4: Triathlon
- [ ] Design `dbo.TriathlonRaces` + `dbo.TriathlonLegs` schema
- [ ] Find triathlon results source (TriResults? Race-specific?)
- [ ] Build tri import pipeline (needs to split into leg rows)
- [ ] UI shows leg-level breakdown (swim+T1+bike+T2+run)

### Phase 5: Polish
- [ ] OceanSwimmer.com.au gets a "View on RaceRadar" link
- [ ] RaceRadar.com.au gets discipline filters / search facets
- [ ] Shared user account: one login, multi-discipline profile
- [ ] OG images / sitemap / Search Console setup for raceradar.com.au

## Risks / open questions

- **Race ID collisions:** Running races and ocean swims will both have a
  RaceId. Using a single `dbo.Races` master with one ID column avoids this.
- **DisciplineType vs separate FKs:** Should `Athletes` track which disciplines
  they participate in? Probably not necessary — they participate in whatever
  has results claimed.
- **Triathlon distance taxonomy:** Sprint / Olympic / 70.3 / IM are well-known
  categories. Worth a `Distance` and a `Standard` column? Probably yes.
- **Front-end split or unified:** Could `oceanswimmer.com.au` and
  `raceradar.com.au` be the *same* ASP.NET app with different domains routing
  to different controllers? Or two separate apps? Probably the same — simpler
  deploys, shared code.
- **Don't break SEO:** OceanSwimmer has indexed pages + ranking. Whatever the
  migration, the URLs must keep working (or 301 redirect cleanly to new ones).

## Domains already secured

- ✅ `raceradar.com.au` (+ `.au` bundle) — Crazy Domains
- DNS not yet pointed (still "Requires Attention" at registrar)

## When to start

Not yet. Current priority is loading more historical ocean swims into the
existing OceanSwimmer.com.au and watching the brand-refresh / SEO benefits
land. Tackle this when:

- Backlog of historical swim imports is mostly cleared
- OceanSwimmer.com.au organic traffic is stable / growing
- City2Surf or another well-known running race has results worth indexing
- ~A free weekend to do Phase 1 (the schema refactor) properly
