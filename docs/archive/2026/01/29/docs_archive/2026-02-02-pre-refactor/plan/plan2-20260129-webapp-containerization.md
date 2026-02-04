# Containerization Plan: Synaxis WebApp

**Date:** 2026-01-29
**Goal:** Containerize `Synaxis.WebApp` and integrate into `docker-compose`.
**Strategy:** Install Node.js in the .NET SDK Docker image so `dotnet publish` can execute the existing MSBuild targets (`npm install`, `npm run build`) directly.

## 1. Project Configuration (`Synaxis.WebApp.csproj`)
*   **Action:** Clean up duplicate MSBuild targets.
*   **Verification:** Ensure `NpmInstall` and `NpmBuild` targets are present and configured to run `BeforeTargets="Build"`.

## 2. Code Configuration (`Program.cs`)
*   **Action:** Refactor YARP configuration to use `builder.Configuration["GatewayUrl"]` instead of hardcoded `localhost`.
*   **Default:** `http://localhost:5000` (for local dev).
*   **Docker:** `http://inference-gateway:8080` (via env var).

## 3. Dockerfile (`src/Synaxis.WebApp/Dockerfile`)
*   **Base Image:** `mcr.microsoft.com/dotnet/sdk:10.0`
*   **Pre-requisites:** Run `apt-get install -y nodejs npm` in the build stage.
*   **Build Flow:**
    1.  `COPY` project files.
    2.  `RUN dotnet restore`.
    3.  `COPY` source code.
    4.  `RUN dotnet publish` -> Triggers `npm build` -> Outputs to `/app/publish`.
*   **Runtime Stage:** `mcr.microsoft.com/dotnet/aspnet:10.0`.

## 4. Orchestration (`docker-compose.yml`)
*   **Service:** `webapp`
*   **Build Context:** `.`
*   **Dockerfile:** `src/Synaxis.WebApp/Dockerfile`
*   **Ports:** `5002:8080`
*   **Environment:**
    *   `GatewayUrl=http://inference-gateway:8080`
    *   `ASPNETCORE_URLS=http://+:8080`
*   **Depends On:** `inference-gateway`

## 5. Verification
*   `docker-compose build webapp`
*   `docker-compose up -d`
*   Verify access at `http://localhost:5002`.
