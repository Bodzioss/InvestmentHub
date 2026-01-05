-- Migration: AddAIFinancialReports
-- Description: Adds tables for AI Financial Agent feature with pgvector support

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Create FinancialReports table
CREATE TABLE IF NOT EXISTS "FinancialReports" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "InstrumentId" UUID NOT NULL,
    "Year" INT NOT NULL,
    "Quarter" INT,
    "ReportType" VARCHAR(50) NOT NULL,
    "FileName" VARCHAR(500) NOT NULL,
    "FileSize" BIGINT NOT NULL,
    "BlobUrl" VARCHAR(2000) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Processing',
    "ChunkCount" INT NOT NULL DEFAULT 0,
    "UploadedByUserId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_FinancialReports_Instruments" FOREIGN KEY ("InstrumentId") 
        REFERENCES "Instruments"("Id") ON DELETE RESTRICT
);

-- Create unique index to prevent duplicate reports
CREATE UNIQUE INDEX IF NOT EXISTS "IX_FinancialReports_Unique" 
    ON "FinancialReports" ("InstrumentId", "Year", "Quarter", "ReportType");

-- Create DocumentChunks table with vector column
CREATE TABLE IF NOT EXISTS "DocumentChunks" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ReportId" UUID NOT NULL,
    "ChunkIndex" INT NOT NULL,
    "Content" TEXT NOT NULL,
    "Embedding" vector(768) NOT NULL,
    "PageNumber" INT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_DocumentChunks_FinancialReports" FOREIGN KEY ("ReportId") 
        REFERENCES "FinancialReports"("Id") ON DELETE CASCADE
);

-- Create index on ReportId for fast lookups
CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_ReportId" 
    ON "DocumentChunks" ("ReportId");

-- Create vector index for similarity search (IVFFlat)
-- Note: For production with many vectors, consider HNSW index
CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_Embedding" 
    ON "DocumentChunks" USING ivfflat ("Embedding" vector_cosine_ops);
