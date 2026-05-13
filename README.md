# AI Exam Platform

AI-powered exam generation and grading from educational PDFs. Upload a PDF, generate structured exams using Groq AI, let students take the exam, and get automatic grading with feedback.

---

## What it does

- Upload any educational PDF (Arabic or English)
- AI generates MCQ, True/False, Short Answer, and Definition questions from the PDF content
- Questions include source page references traceable back to the original PDF
- Students take the exam online and submit answers
- AI grades open-ended answers and provides feedback
- Export exams as PDF or DOCX
- Filter questions by page range (e.g. Chapter 2 only: pages 45-80)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor United / Auto (.NET 10) — SSR + WASM |
| Backend | .NET 10 Minimal API |
| AI | Groq API (llama-3.3-70b-versatile) |
| Database | PostgreSQL 16 + EF Core 10 |
| PDF Processing | PdfPig + Arabic-aware chunking |
| Export | QuestPDF (PDF) + OpenXML (DOCX) |
| Orchestration | Docker Compose |

---

## Prerequisites

Install these before starting:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Docker Desktop | 20.0+ | https://www.docker.com/products/docker-desktop |
| Git | Any | https://git-scm.com |

Verify installations:

```bash
dotnet --version    # should show 10.x.x
docker --version    # should show 20.x or higher
git --version
```

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ahmedyasir779/exam-platform.git
cd exam-platform
```

### 2. Get a Groq API key

- Go to https://console.groq.com/keys
- Create a new API key
- Copy it — you will need it in the next step

### 3. Configure your API key

Create the local settings file for the API:

```bash
# Windows PowerShell
Copy-Item src\ExamPlatform.Api\appsettings.Development.example.json src\ExamPlatform.Api\appsettings.Development.json

# Mac / Linux
cp src/ExamPlatform.Api/appsettings.Development.example.json src/ExamPlatform.Api/appsettings.Development.json
```

Open `src/ExamPlatform.Api/appsettings.Development.json` and replace `YOUR_GROQ_KEY_HERE` with your actual key:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=examdb;Username=postgres;Password=postgres"
  },
  "Storage": {
    "BasePath": "C:/tmp/examplatform/files"
  },
  "VectorStore": {
    "BasePath": "C:/tmp/examplatform/faiss"
  },
  "Grok": {
    "ApiKey": "YOUR_GROQ_KEY_HERE"
  }
}
```

> **Important:** This file is in `.gitignore` and will never be committed. Your key stays local.

### 4. Start PostgreSQL

```bash
docker compose up -d postgres
```

Wait a few seconds, then verify it is running:

```bash
docker compose ps
# postgres should show "running"
```

### 5. Run the API

Open a terminal and run:

```bash
# Windows
dotnet watch --project src/ExamPlatform.Api/ExamPlatform.Api.csproj --urls http://localhost:5001

# Mac / Linux
dotnet watch --project src/ExamPlatform.Api/ExamPlatform.Api.csproj --urls http://localhost:5001
```

Wait until you see:
Now listening on: http://localhost:5001

> The API automatically applies database migrations on startup.

### 6. Run the frontend

Open a **second terminal** and run:

```bash
dotnet watch --project src/ExamPlatform.Web/ExamPlatform.Web/ExamPlatform.Web.csproj --urls http://localhost:5000
```

Wait until you see:
Now listening on: http://localhost:5000

### 7. Open the app

Go to **http://localhost:5000** in your browser.

---

## How to Use

### Teacher workflow

#### Step 1 — Upload a PDF
1. Click **Upload PDF** in the sidebar
2. Select any educational PDF (English or Arabic)
3. Wait for status to change from `processing` to `Ready`
4. Click **Generate Exam**

#### Step 2 — Generate an exam
1. Select your uploaded document from the library
2. Set the **page range** (e.g. pages 1-50 for Chapter 1 only)
3. Set question counts per type (MCQ, True/False, Short Answer, Definition)
4. Choose difficulty (Easy / Medium / Hard)
5. Click **Generate Exam**
6. Wait ~5-10 seconds per question for Groq to generate

#### Step 3 — Review and export
- Review generated questions with their source page references
- Download as **PDF** or **DOCX** to share with students
- Click **Take Exam** to preview the student experience
- Click **Generate Another** to create a different exam from the same PDF

#### Step 4 — Share with students
Share the exam URL with students:
http://localhost:5000/exams/{exam-id}/take

Or direct them to **http://localhost:5000/exams** to see all available exams.

---

### Student workflow

1. Go to **http://localhost:5000/exams**
2. Click **Take** on any exam
3. Answer all questions (radio buttons for MCQ/True-False, text box for open-ended)
4. Click **Submit Exam**
5. View results with score, per-question feedback, and source page references

---

### Managing documents and exams

- **Documents page** — view all uploaded PDFs, generate exams from them, or delete them
- **All Exams page** — view all generated exams, take them, download them, or delete them
- Deleting a PDF removes all its chunks (but existing exams remain)
- Deleting an exam removes all its questions and student submissions

---

## Arabic PDF Support

The platform automatically detects Arabic content:

- Upload any Arabic PDF — the system detects Arabic characters automatically
- Chunking switches to sentence-based splitting (better for Arabic text)
- Groq prompts switch to Arabic — questions, options, and grading feedback are all in Arabic
- No configuration needed — detection is automatic per document

---

## Project Structure
exam-platform/
+-- src/
¦   +-- ExamPlatform.Api/              # .NET 10 Minimal API
¦   ¦   +-- Endpoints/                 # Document, Exam, Submission, Export endpoints
¦   ¦   +-- Program.cs                 # DI registration, middleware
¦   ¦
¦   +-- ExamPlatform.Web/              # Blazor United server project
¦   ¦   +-- Components/
¦   ¦       +-- App.razor              # HTML shell
¦   ¦       +-- Pages/Error.razor
¦   ¦
¦   +-- ExamPlatform.Web.Client/       # Blazor WASM client project
¦   ¦   +-- Pages/
¦   ¦   ¦   +-- Upload.razor           # PDF upload with status polling
¦   ¦   ¦   +-- Documents.razor        # Document library with delete
¦   ¦   ¦   +-- ExamBuilder.razor      # Exam generation with page range
¦   ¦   ¦   +-- ExamList.razor         # All exams with delete
¦   ¦   ¦   +-- StudentExam.razor      # Exam taking interface
¦   ¦   ¦   +-- Results.razor          # Grading results
¦   ¦   +-- Layout/
¦   ¦       +-- MainLayout.razor
¦   ¦       +-- NavMenu.razor
¦   ¦
¦   +-- ExamPlatform.Application/      # Business logic
¦   ¦   +-- PdfProcessing/             # PDF extraction + Arabic-aware chunking
¦   ¦   +-- ExamGeneration/            # Groq client + RAG generation
¦   ¦   +-- Grading/                   # MCQ exact match + AI semantic grading
¦   ¦   +-- Export/                    # QuestPDF + OpenXML export
¦   ¦
¦   +-- ExamPlatform.Domain/           # Entities + interfaces
¦   +-- ExamPlatform.Infrastructure/   # EF Core, repositories, file storage
¦
+-- tests/
¦   +-- ExamPlatform.UnitTests/        # Chunking strategy tests
¦   +-- ExamPlatform.IntegrationTests/
¦
+-- docker-compose.yml                 # PostgreSQL + full stack
+-- README.md

---

## Running Tests

```bash
dotnet test tests/ExamPlatform.UnitTests/ExamPlatform.UnitTests.csproj
```

---

## Troubleshooting

| Problem | Solution |
|---|---|
| `401 Unauthorized` from Groq | Check your API key in `appsettings.Development.json` — regenerate at https://console.groq.com/keys |
| `429 Too Many Requests` | Groq free tier rate limit hit — wait 60 seconds and try with fewer questions |
| PDF stuck on `processing` | Restart the API with `Ctrl+R` and re-upload the PDF |
| Port 5000/5001 already in use | Run `Get-NetTCPConnection -LocalPort 5000` to find and kill the process |
| Docker postgres not starting | Run `docker compose down -v` then `docker compose up -d postgres` |
| Build errors after git pull | Run `dotnet restore ExamPlatform.slnx` then `dotnet build ExamPlatform.slnx` |

---

## Groq Rate Limits (Free Tier)

The free Groq tier has request limits. To stay within them:

- Keep question counts low (2-4 per exam generation)
- Wait 60 seconds between exam generations if you hit a 429 error
- Upgrade to a paid Groq plan for production use

---

## Roadmap

- [ ] Teacher / Student login (ASP.NET Identity + JWT)
- [ ] PDF viewer with source page highlight overlay
- [ ] Real vector embeddings for smarter chunk retrieval
- [ ] Student performance analytics dashboard
- [ ] LMS integrations (Canvas, Moodle)
- [ ] Question inline edit and regenerate
- [ ] Offline / local AI deployment

---

## License

MIT
