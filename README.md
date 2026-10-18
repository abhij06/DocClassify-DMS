# DocClassify-DMS

Document-management webtool for BPO workflows: upload a document, **auto-classify into buckets with a local LLM**, index it for **RAG semantic search**, and track every workflow transition.

**Stack:** .NET 8 Web API · EF Core (SQLite) · Swagger · LM Studio (chat + embeddings) · cosine-similarity vector search

## Flow
Upload → `Uploaded` → LLM classify (`Classified`) → chunk + embed (`Indexed`) → human review/override (`Reviewed`). Each step is stored in `Events` (transaction history).

## Run
```bash
cd src/DocClassify.Api
dotnet run
```
Swagger: `/swagger`. Start LM Studio server at `http://localhost:1234/v1` and set model names in `appsettings.json`.
**Works offline too:** keyword classifier + hashed bag-of-words embeddings are used if the LLM is unreachable.

## Try
```bash
curl -F "file=@samples/invoice.txt" http://localhost:5000/api/documents
curl http://localhost:5000/api/documents/1/history
curl -X POST http://localhost:5000/api/search -H "Content-Type: application/json" -d '{"query":"GST amount due","topK":3}'
```
Note: if you switch between LLM and fallback embeddings, re-upload documents (vectors of different sizes are skipped).
