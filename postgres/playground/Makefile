for-update-test:
	docker compose up -d
	dotnet test --logger "console;verbosity=detailed" --filter FullyQualifiedName=Backi.Postgres.Playground.ForUpdateOperator.$(TEST_NAME)