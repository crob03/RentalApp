PKG := com.companyname.rentalapp
DB_CONN ?= Host=localhost;Port=5432;Database=appdb;Username=app_user;Password=app_password
PREFS_FILE := shared_prefs/$(PKG)_preferences.xml

build-debug:
	dotnet build -c Debug /workspace/RentalApp.sln

test:
	dotnet test /workspace/RentalApp.sln

uninstall:
	adb uninstall $(PKG)

install:
	adb install -r /workspace/RentalApp/bin/Debug/net10.0-android/$(PKG)-Signed.apk

# Switches the running app to the remote API. Requires a debug build.
# Writes directly to SharedPreferences then force-restarts so MauiProgram.cs picks up the change.
use-remote-api:
	adb shell 'run-as $(PKG) sh -c "mkdir -p shared_prefs && printf \"<?xml version=\x221.0\x22 encoding=\x22utf-8\x22 standalone=\x22yes\x22 ?>\n<map>\n    <boolean name=\x22UseSharedApi\x22 value=\x22true\x22 />\n</map>\n\" > $(PREFS_FILE)"'
	adb shell am force-stop $(PKG)
	adb shell monkey -p $(PKG) -c android.intent.category.LAUNCHER 1 > /dev/null

# Applies all pending EF Core migrations to the local database.
migrate:
	CONNECTION_STRING="$(DB_CONN)" dotnet ef database update --project RentalApp.Migrations

# Adds a new EF Core migration. Usage: make migration NAME=YourMigrationName
migration:
	CONNECTION_STRING="$(DB_CONN)" dotnet ef migrations add $(NAME) --project RentalApp.Migrations

use-local-api:
	adb shell 'run-as $(PKG) sh -c "mkdir -p shared_prefs && printf \"<?xml version=\x221.0\x22 encoding=\x22utf-8\x22 standalone=\x22yes\x22 ?>\n<map>\n    <boolean name=\x22UseSharedApi\x22 value=\x22false\x22 />\n</map>\n\" > $(PREFS_FILE)"'
	adb shell am force-stop $(PKG)
	adb shell monkey -p $(PKG) -c android.intent.category.LAUNCHER 1 > /dev/null
