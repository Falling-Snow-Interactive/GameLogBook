#!/bin/bash

set -u

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_DB_DIR="$HOME/Library/Containers/com.kiradinan.gamelogbook/Data/Library/Application Support"
DB_FILE="${GAMELOGBOOK_DB_PATH:-$APP_DB_DIR/GameLogBook.db}"

make_temp_file() {
    local prefix="$1"
    local temp_dir="${TMPDIR:-/tmp}"

    temp_dir="${temp_dir%/}"

    mktemp "$temp_dir/$prefix.XXXXXX" 2>/dev/null || mktemp -t "$prefix" 2>/dev/null
}

LOG_FILE="$(make_temp_file "gamelogbook-db-migration")" || {
    echo "Could not create a temporary log file."
    exit 1
}
TOOL_SNAPSHOT_FILE="tools/GameLogBook.DbTool/GameLogBook/Migrations/GameLogBookDbContextModelSnapshot.cs"
APP_SNAPSHOT_FILE="Migrations/GameLogBookDbContextModelSnapshot.cs"

cd "$SCRIPT_DIR" || exit 1

show_message() {
    local title="$1"
    local message="$2"

    /usr/bin/osascript - "$title" "$message" <<'APPLESCRIPT' >/dev/null
on run argv
    display dialog (item 2 of argv) with title (item 1 of argv) buttons {"OK"} default button "OK"
end run
APPLESCRIPT
}

show_text_message() {
    local title="$1"
    local message="$2"

    /usr/bin/osascript - "$title" "$message" <<'APPLESCRIPT' >/dev/null
on run argv
    display dialog (item 2 of argv) with title (item 1 of argv) buttons {"OK"} default button "OK" with icon caution
end run
APPLESCRIPT
}

show_scrollable_error() {
    local title="$1"
    local message="$2"
    local details="$3"

    /usr/bin/osascript - "$title" "$message" "$details" <<'APPLESCRIPT' >/dev/null 2>&1
use framework "AppKit"
use scripting additions

on run argv
    set alertTitle to item 1 of argv
    set alertMessage to item 2 of argv
    set errorDetails to item 3 of argv

    current application's NSApplication's sharedApplication()'s activateIgnoringOtherApps:true

    set alert to current application's NSAlert's alloc()'s init()
    alert's setMessageText:alertTitle
    alert's setInformativeText:alertMessage
    alert's setAlertStyle:(current application's NSAlertStyleCritical)
    alert's addButtonWithTitle:"OK"
    alert's addButtonWithTitle:"Copy Error"

    set scrollFrame to current application's NSMakeRect(0, 0, 760, 360)
    set scrollView to current application's NSScrollView's alloc()'s initWithFrame:scrollFrame
    scrollView's setBorderType:(current application's NSBezelBorder)
    scrollView's setHasVerticalScroller:true
    scrollView's setHasHorizontalScroller:false
    scrollView's setAutohidesScrollers:false

    set textView to current application's NSTextView's alloc()'s initWithFrame:scrollFrame
    textView's setString:errorDetails
    textView's setEditable:false
    textView's setSelectable:true
    textView's setFont:(current application's NSFont's userFixedPitchFontOfSize:11)
    textView's setVerticallyResizable:true
    textView's setHorizontallyResizable:false
    textView's setAutoresizingMask:((current application's NSViewWidthSizable) + (current application's NSViewHeightSizable))
    textView's textContainer()'s setContainerSize:(current application's NSMakeSize(760, 1.0E+7))
    textView's textContainer()'s setWidthTracksTextView:true

    scrollView's setDocumentView:textView
    alert's setAccessoryView:scrollView
    set buttonResult to alert's runModal()

    if buttonResult = (current application's NSAlertSecondButtonReturn) then
        set clipboardText to alertTitle & return & return & alertMessage & return & return & errorDetails
        set pasteboard to current application's NSPasteboard's generalPasteboard()
        pasteboard's clearContents()
        pasteboard's setString:clipboardText forType:(current application's NSPasteboardTypeString)
    end if
end run
APPLESCRIPT
}

ask_migration_name() {
    local default_name="$1"

    /usr/bin/osascript - "$default_name" <<'APPLESCRIPT'
on run argv
    try
        set dialogResult to display dialog "Enter a migration name, for example AddGameType or AddGameRating." with title "GameLogBook Database" default answer (item 1 of argv) buttons {"Cancel", "Continue"} default button "Continue" cancel button "Cancel"
        return text returned of dialogResult
    on error number -128
        return "__CANCELLED__"
    end try
end run
APPLESCRIPT
}

ask_database_action() {
    local prompt="$1"

    /usr/bin/osascript - "$prompt" <<'APPLESCRIPT'
on run argv
try
    set actionResult to choose from list {"Migrate existing database", "Rebuild database from scratch"} with title "GameLogBook Database" with prompt (item 1 of argv) OK button name "Run" cancel button name "Cancel"
    if actionResult is false then
        return "__CANCELLED__"
    end if
    return item 1 of actionResult
on error number -128
    return "__CANCELLED__"
end try
end run
APPLESCRIPT
}

tail_log() {
    tail -n 200 "$LOG_FILE"
}

fail_with_log() {
    local title="$1"
    local message="$2"
    local output

    output="$(tail_log)"
    show_scrollable_error "$title" "$message"$'\n\n'"Full log: $LOG_FILE" "$output" || \
        show_text_message "$title" "$message"$'\n\n'"Last output:"$'\n'"$output"$'\n\n'"Full log:"$'\n'"$LOG_FILE"
    echo
    echo "$message"
    echo
    cat "$LOG_FILE"
    exit 1
}

run_step() {
    local label="$1"
    shift

    {
        echo
        echo "==== $label ===="
        printf 'Command:'
        printf ' %q' "$@"
        echo
    } >>"$LOG_FILE"

    echo "$label..."
    "$@" >>"$LOG_FILE" 2>&1
}

sync_generated_snapshot() {
    if [[ -f "$TOOL_SNAPSHOT_FILE" ]]; then
        {
            echo
            echo "==== Moving generated model snapshot ===="
            echo "Moving $TOOL_SNAPSHOT_FILE to $APP_SNAPSHOT_FILE"
        } >>"$LOG_FILE"

        cp "$TOOL_SNAPSHOT_FILE" "$APP_SNAPSHOT_FILE"
        rm -f "$TOOL_SNAPSHOT_FILE"
        rmdir -p "tools/GameLogBook.DbTool/GameLogBook/Migrations" 2>/dev/null || true
    fi
}

run_database_action() {
    local database_action="$1"

    mkdir -p "$(dirname "$DB_FILE")"

    case "$database_action" in
        "Migrate existing database")
            run_step "Applying migration to existing database" \
                env GAMELOGBOOK_DB_PATH="$DB_FILE" dotnet run --project tools/GameLogBook.DbTool -- migrate || \
                fail_with_log "Database Migration Failed" "Applying migrations to the database failed."
            ;;
        "Rebuild database from scratch")
            run_step "Rebuilding database from scratch" \
                env GAMELOGBOOK_DB_PATH="$DB_FILE" dotnet run --project tools/GameLogBook.DbTool -- rebuild || \
                fail_with_log "Database Rebuild Failed" "Rebuilding the database failed."
            ;;
        *)
            fail_with_log "Unknown Choice" "Unknown database action: $database_action"
            ;;
    esac
}

check_for_model_changes() {
    local output_file
    output_file="$(make_temp_file "gamelogbook-pending-model-changes")" || {
        echo "Could not create a temporary model-check log file." >>"$LOG_FILE"
        return 2
    }

    {
        echo
        echo "==== Checking for model changes ===="
        echo "Command: dotnet ef migrations has-pending-model-changes --project tools/GameLogBook.DbTool --startup-project tools/GameLogBook.DbTool --no-build"
    } >>"$LOG_FILE"

    echo "Checking for model changes..."

    dotnet ef migrations has-pending-model-changes \
        --project tools/GameLogBook.DbTool \
        --startup-project tools/GameLogBook.DbTool \
        --no-build >"$output_file" 2>&1

    local exit_code=$?
    cat "$output_file" >>"$LOG_FILE"

    if grep -q "No changes have been made to the model since the last migration" "$output_file"; then
        rm -f "$output_file"
        return 0
    fi

    if grep -q "Changes have been made to the model since the last migration" "$output_file"; then
        rm -f "$output_file"
        return 1
    fi

    rm -f "$output_file"
    return 2
}

check_for_pending_database_migrations() {
    local output_file
    local connection_string

    output_file="$(make_temp_file "gamelogbook-pending-database-migrations")" || {
        echo "Could not create a temporary database-check log file." >>"$LOG_FILE"
        return 2
    }
    connection_string="Data Source=$DB_FILE"

    mkdir -p "$(dirname "$DB_FILE")"

    {
        echo
        echo "==== Checking for pending database migrations ===="
        echo "Command: dotnet ef migrations list --project tools/GameLogBook.DbTool --startup-project tools/GameLogBook.DbTool --connection \"$connection_string\" --json --prefix-output --no-build"
    } >>"$LOG_FILE"

    echo "Checking whether the database is up to date..."

    dotnet ef migrations list \
        --project tools/GameLogBook.DbTool \
        --startup-project tools/GameLogBook.DbTool \
        --connection "$connection_string" \
        --json \
        --prefix-output \
        --no-build >"$output_file" 2>&1

    local exit_code=$?
    cat "$output_file" >>"$LOG_FILE"

    if [[ "$exit_code" -ne 0 ]]; then
        rm -f "$output_file"
        return 2
    fi

    if grep -q '"applied": false' "$output_file"; then
        rm -f "$output_file"
        return 1
    fi

    rm -f "$output_file"
    return 0
}

echo "GameLogBook database migration helper"
echo "Log: $LOG_FILE"

sync_generated_snapshot

run_step "Building migration helper" \
    dotnet build tools/GameLogBook.DbTool/GameLogBook.DbTool.csproj || \
    fail_with_log "Build Failed" "Could not build the migration helper."

check_for_model_changes
pending_check_result=$?

if [[ "$pending_check_result" -eq 0 ]]; then
    check_for_pending_database_migrations
    database_check_result=$?

    if [[ "$database_check_result" -eq 0 ]]; then
        show_message "Nothing Has Changed" "The model already matches the latest migration, and the database is up to date."
        echo "Nothing has changed. The database is up to date."
        echo "Full log: $LOG_FILE"
        exit 0
    fi

    if [[ "$database_check_result" -ne 1 ]]; then
        fail_with_log "Database Check Failed" "Could not determine whether the database has pending migrations."
    fi

    database_action="$(ask_database_action "There are existing migrations that have not been applied to the local database. What should happen now?")"

    if [[ "$database_action" == "__CANCELLED__" ]]; then
        show_message "Database Not Changed" "No new migration was needed, and the database was not changed."
        echo "No new migration was needed, and the database was not changed."
        exit 0
    fi

    run_database_action "$database_action"
    show_message "Success" "$database_action completed successfully."

    echo
    echo "$database_action completed successfully."
    echo "Full log: $LOG_FILE"
    exit 0
fi

if [[ "$pending_check_result" -ne 1 ]]; then
    fail_with_log "Model Check Failed" "Could not determine whether the model has pending changes."
fi

migration_name=""
while true; do
    migration_name="$(ask_migration_name "$migration_name")"

    if [[ "$migration_name" == "__CANCELLED__" ]]; then
        echo "Cancelled."
        exit 0
    fi

    migration_name="${migration_name//[[:space:]]/}"

    if [[ "$migration_name" =~ ^[A-Za-z][A-Za-z0-9_]*$ ]]; then
        break
    fi

    show_message "Invalid Migration Name" "Use letters, numbers, and underscores only. The name must start with a letter, for example AddGameType."
done

run_step "Creating migration $migration_name" \
    dotnet ef migrations add "$migration_name" \
    --project tools/GameLogBook.DbTool \
    --startup-project tools/GameLogBook.DbTool \
    --output-dir ../../Migrations \
    --namespace VGL.Migrations \
    --no-build || \
    fail_with_log "Migration Failed" "Could not create migration $migration_name."

sync_generated_snapshot

database_action="$(ask_database_action "The migration was created. What should happen to the local database now?")"

if [[ "$database_action" == "__CANCELLED__" ]]; then
    show_message "Migration Created" "Created migration $migration_name. Database was not changed."
    echo "Created migration $migration_name. Database was not changed."
    exit 0
fi

run_database_action "$database_action"

show_message "Success" "$database_action completed successfully for migration $migration_name."

echo
echo "$database_action completed successfully for migration $migration_name."
echo "Full log: $LOG_FILE"
