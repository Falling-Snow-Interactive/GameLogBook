#!/bin/bash

set -e

APP_DB_DIR="$HOME/Library/Containers/com.kiradinan.gamelogbook/Data/Library/Application Support"
DB_FILE="${GAMELOGBOOK_DB_PATH:-$APP_DB_DIR/GameLogBook.db}"

mkdir -p "$(dirname "$DB_FILE")"

echo "Rebuilding database..."
GAMELOGBOOK_DB_PATH="$DB_FILE" dotnet run --project tools/GameLogBook.DbTool -- rebuild

echo "Database rebuild complete!"
