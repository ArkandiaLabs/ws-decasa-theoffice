# nvm y corepack son funciones/scripts de bash. Con el `/bin/sh` que make usa por defecto,
# `. nvm.sh` no define nada y el arreglo de version de Node de abajo seria inerte.
SHELL := /bin/bash

SLN := src/TheOffice.sln
API := src/Presentation/TheOffice.Api
ARCH := tests/TheOffice.ArchitectureTests
WEB := src/Presentation/TheOffice.Web

# CI publishes test results, so the run has to produce a file. This repo runs xUnit v3 on
# Microsoft.Testing.Platform, whose TRX reporter is `--report-xunit-trx` (NOT the VSTest
# `--logger trx`, and not the platform-generic `--report-trx`, which this runner rejects).
# Output lands in <project>/bin/<config>/<tfm>/TestResults/*.trx.
TEST_LOGGER := --report-xunit-trx

# El Angular CLI 22 RECHAZA un Node anterior al del .nvmrc: no es un aviso, es un error de
# arranque. Y pnpm se instala por version de Node, asi que cambiar de version puede dejarlo
# fuera del PATH. Estas dos lineas se ejecutan dentro de $(WEB) antes de cada comando del
# frontend para que `make dev` y `make check` funcionen en una terminal recien abierta, sin
# tener que acordarse de `nvm use`.
#
# Todo esta condicionado a que nvm exista: en CI no esta, el runner ya trae la version correcta
# y estas lineas no hacen nada.
WEB_ENV = if [ -s "$$HOME/.nvm/nvm.sh" ]; then \
	    . "$$HOME/.nvm/nvm.sh"; \
	    nvm use --silent 2>/dev/null || nvm install; \
	    command -v pnpm >/dev/null 2>&1 || COREPACK_ENABLE_DOWNLOAD_PROMPT=0 corepack enable pnpm; \
	  fi

.DEFAULT_GOAL := help

# Los objetivos de este archivo son secuencias, no un conjunto: `test` asume `build`, y
# `web-design-classes` lee el CSS que produce `web-build`. Con `make -j` los prerequisitos
# arrancarian a la vez y una compuerta leeria un artefacto que aun no existe.
.NOTPARALLEL:
.PHONY: help restore restore-locked build test arch format lint secrets audit check ci run clean hooks \
	web web-install web-design-lint web-design-check web-design-classes web-tokens web-lint web-build web-test web-run dev

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
	  awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

restore: ## Restore NuGet dependencies
	dotnet restore $(SLN)

restore-locked: ## Restore with the lock file enforced (what CI runs)
	dotnet restore $(SLN) --locked-mode

build: ## Build (strict: warnings are errors)
	dotnet build $(SLN) --no-restore

test: build ## Run every test
	dotnet test $(SLN) --no-build $(TEST_LOGGER)

arch: build ## Run architecture tests only
	dotnet test $(ARCH) --no-build $(TEST_LOGGER)

format: ## Fix code style in place
	dotnet format $(SLN) --no-restore

lint: ## Verify code style (gate)
	dotnet format $(SLN) --no-restore --verify-no-changes

secrets: ## Scan the working tree for committed secrets
	gitleaks detect --no-banner --redact

audit: ## Report vulnerable dependencies (report only, never fails)
	dotnet list $(SLN) package --vulnerable --include-transitive

# El frontend usa pnpm. `--dir` es el equivalente de `--prefix`, y `--frozen-lockfile` el de
# `npm ci`: falla si pnpm-lock.yaml no coincide con package.json, en vez de actualizarlo callado.
web-install: ## Install frontend dependencies (locked, what CI runs)
	@cd $(WEB) && $(WEB_ENV) && pnpm install --frozen-lockfile

# El sistema de diseno vive en $(WEB)/DESIGN.md: los tokens del @theme se generan desde ahi.
web-tokens: ## Regenerate the design tokens from DESIGN.md
	@cd $(WEB) && $(WEB_ENV) && pnpm run design:tokens

web-design-lint: ## Verify DESIGN.md is structurally valid
	@cd $(WEB) && $(WEB_ENV) && pnpm run design:lint

web-design-check: ## Verify the generated tokens still match DESIGN.md
	@cd $(WEB) && $(WEB_ENV) && pnpm run design:check

# Necesita el CSS compilado: corre despues de web-build, no antes.
web-design-classes: ## Verify the templates only use classes the design tokens generate
	@cd $(WEB) && $(WEB_ENV) && pnpm run design:classes

web-lint: ## Verify frontend style
	@cd $(WEB) && $(WEB_ENV) && pnpm run lint

web-build: ## Build the frontend
	@cd $(WEB) && $(WEB_ENV) && pnpm run build

web-test: ## Run frontend tests (headless, single run)
	@cd $(WEB) && $(WEB_ENV) && pnpm run test:ci

web: web-install web-design-lint web-design-check web-lint web-build web-design-classes web-test ## Every frontend gate
	@echo "OK - the frontend is green"

check: restore lint build test web ## Single confidence signal
	@echo "OK - the repo is green"

ci: restore-locked lint build test web secrets ## What the pipeline runs
	@echo "OK - CI gates passed"

run: ## Run the API
	dotnet run --project $(API)

web-run: ## Run the frontend dev server (needs the API up)
	@cd $(WEB) && $(WEB_ENV) && pnpm start

# Un solo Ctrl-C tiene que apagar los dos. `trap kill 0` mata el grupo de procesos entero;
# sin el, el servidor de Angular queda huerfano ocupando el 4200 y el siguiente arranque falla.
#
# Si uno de los dos muere al arrancar, el otro tiene que caer con el. Cada rama termina en su
# propio `kill 0`, en vez de `wait -n`: el bash de macOS es 3.2 y no conoce esa opcion.
# Con un `wait` a secas, un frontend caido dejaba la API viva y su fallo se perdia entre los
# logs de la API — que es exactamente como se descubrio esto.
dev: ## Run the API and the frontend together (Ctrl-C stops both)
	@echo "API:     http://localhost:5226/scalar"
	@echo "Web App: http://localhost:4200"
	@trap 'kill 0' EXIT INT TERM; \
	  ( dotnet run --project $(API); kill 0 ) & \
	  ( cd $(WEB) && $(WEB_ENV) && pnpm start; kill 0 ) & \
	  wait

clean: ## Remove build artifacts
	dotnet clean $(SLN)

hooks: ## Install git hooks (Lefthook)
	lefthook install
