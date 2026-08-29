SLN := src/TheOffice.sln
API := src/Presentation/TheOffice.Api
ARCH := tests/TheOffice.ArchitectureTests
WEB := src/Presentation/TheOffice.Web

# CI publishes test results, so the run has to produce a file. This repo runs xUnit v3 on
# Microsoft.Testing.Platform, whose TRX reporter is `--report-xunit-trx` (NOT the VSTest
# `--logger trx`, and not the platform-generic `--report-trx`, which this runner rejects).
# Output lands in <project>/bin/<config>/<tfm>/TestResults/*.trx.
TEST_LOGGER := --report-xunit-trx

.DEFAULT_GOAL := help
.PHONY: help restore restore-locked build test arch format lint secrets audit check ci run clean hooks \
	web web-install web-lint web-build web-test

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

web-install: ## Install frontend dependencies (locked, what CI runs)
	npm ci --prefix $(WEB)

web-lint: ## Verify frontend style
	npm run lint --prefix $(WEB)

web-build: ## Build the frontend
	npm run build --prefix $(WEB)

web-test: ## Run frontend tests (headless, single run)
	npm run test:ci --prefix $(WEB)

web: web-install web-lint web-build web-test ## Every frontend gate
	@echo "OK - the frontend is green"

check: restore lint build test web ## Single confidence signal
	@echo "OK - the repo is green"

ci: restore-locked lint build test web secrets ## What the pipeline runs
	@echo "OK - CI gates passed"

run: ## Run the application
	dotnet run --project $(API)

clean: ## Remove build artifacts
	dotnet clean $(SLN)

hooks: ## Install git hooks (Lefthook)
	lefthook install
