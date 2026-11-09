# 🐠 NemoFind

> "Just keep swimming... until you find your file."

A personal file search engine for macOS built in C# on .NET 10.  
NemoFind crawls your file system, extracts full text from 20+ file types, builds a TF-IDF inverted index in SQLite, and returns keyword-ranked results in ~160 ms — with real-time index updates and Wikipedia search built in.

**648 files indexed · 79,000+ unique words · 216,000+ index entries · ~18 MB database**

---

## What It Does

- **Crawls** directories recursively and indexes 20+ file types (`.txt`, `.md`, `.pdf`, `.docx`, `.cs`, `.py`, `.json`, and more)
- **Extracts** full text content from plain files, PDFs (via iTextSharp), and Word documents (DOCX via ZIP+XML parsing)
- **Builds** an inverted index with TF-IDF relevance scoring, stop-word filtering, and word-frequency tokenisation
- **Ranks** results using TF-IDF score + filename-proximity boost + multi-word coverage bonus
- **Auto-updates** the index in real time using `FileSystemWatcher` — no re-crawl needed when files are created, changed, renamed, or deleted
- **Detects** duplicate files across the index using SHA hash grouping
- **Searches Wikipedia** for the same keyword via the Wikipedia Action API, returning article titles, snippets, and direct URLs
- **Logs** every query with result count and response time (ms) for basic analytics

---

## Tech Stack

| Category | Technologies |
|---|---|
| Language & Runtime | C# 13, .NET 10 |
| Backend / API | ASP.NET Core, REST API, Swagger / OpenAPI |
| Data | Entity Framework Core 10, SQLite, EF Migrations |
| Text Processing | Tokeniser, stop-word filter, TF-IDF scoring, iTextSharp (PDF), DOCX (ZipFile + XML) |
| Search Concepts | Inverted index, TF-IDF ranking, prefix matching, filename-proximity boosting |
| Real-time | `FileSystemWatcher`, `BackgroundService`, event-driven re-indexing |
| Architecture | Clean Architecture (4-project), Dependency Injection, interface-based design |
| External APIs | Wikipedia Action API, `HttpClient` |
| CLI | Custom CLI — `search`, `crawl`, `stats`, `duplicates` commands with `--type` and `--since` filters |

---

## Projects

| Project | Responsibility |
|---|---|
| `NemoFind.Core` | Domain models (`InvertedIndex`, `Word`, `IndexedFile`, `FileHash`, `SearchLog`) and service interfaces — zero external dependencies |
| `NemoFind.Infrastructure` | `CrawlerService`, `IndexerService` (tokeniser + TF-IDF), `SearchService`, `WikipediaSearchService`, EF Core `DbContext` |
| `NemoFind.API` | ASP.NET Core REST API, Swagger docs, `FileWatcherService` (background real-time watcher), CORS, auto-migration on startup |
| `NemoFind.CLI` | Terminal client — calls the REST API and renders ranked results as a formatted table |

---

## CLI Usage

```bash
nemofind search "machine learning"           # keyword search
nemofind search "report" --type pdf          # filter by file type
nemofind search "budget" --since 7           # files modified in last 7 days
nemofind crawl                               # crawl home directory
nemofind crawl --path ~/Documents            # crawl a specific folder
nemofind stats                               # show index statistics
nemofind duplicates                          # find duplicate files
```

---

## Status
🚧 Under active development
