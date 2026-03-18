# 🐠 NemoFind

> "Just keep swimming... until you find your file."

A personal file search engine for macOS built entirely in C# on .NET 10.
NemoFind crawls your file system, builds an inverted index in SQLite, and lets you search across all your files in milliseconds.

## Tech Stack
- ASP.NET Core Web API
- Entity Framework Core + SQLite
- FileSystemWatcher (real-time indexing)
- System.CommandLine (CLI)
- Swagger / Swashbuckle

## Projects
- `NemoFind.Core` — Models and interfaces
- `NemoFind.Infrastructure` — Crawler, Indexer, File Watcher, Database
- `NemoFind.API` — REST API + Swagger
- `NemoFind.CLI` — Terminal interface

## Status
🚧 Under active development
