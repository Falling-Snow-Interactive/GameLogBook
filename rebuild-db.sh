#!/bin/bash

set -e

DB_FILE="GameLogBook.db"
MIGRATION_NAME="RebuildDatabase"

echo "Dropping database..."
dotnet ef database drop --force

echo "Removing old migrations..."
rm -rf Migrations

echo "Deleting local database file if it exists..."
rm -f "$DB_FILE"

echo "Creating new migration..."
dotnet ef migrations add "$MIGRATION_NAME"

echo "Building new database..."
dotnet ef database update

echo "Database rebuild complete!"