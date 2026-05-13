# AI Exam Platform

AI-powered exam generation and grading from educational PDFs.

## Stack
- .NET 10 / Blazor United (SSR + WASM)
- Grok API (xAI) for question generation and grading
- PostgreSQL 16, FAISS vector store
- Docker Compose

## Quick start

1. Copy `.env.example` to `.env` and fill in your API keys
2. `docker compose up -d postgres`
3. `dotnet run --project src/ExamPlatform.Api`
4. `dotnet run --project src/ExamPlatform.Web`

## Build phases
1. PDF Pipeline
2. Exam Generation Engine
3. Blazor Frontend
4. Grading Engine
5. Export (PDF + DOCX)