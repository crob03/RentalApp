PKG := com.companyname.rentalapp
PREFS_FILE := /data/data/$(PKG)/shared_prefs/$(PKG).preferences_v2.xml

build-debug:
	dotnet build -c Debug /workspace/RentalApp.sln

uninstall:
	adb uninstall $(PKG)

install:
	adb install -r /workspace/RentalApp/bin/Debug/net10.0-android/$(PKG)-Signed.apk

# Configures the app to authenticate against the remote shared API.
# Restart the app after running for the change to take effect.
use-remote-api:
	adb shell run-as $(PKG) sh -c 'echo "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?><map><boolean name=\"UseSharedApi\" value=\"true\" /></map>" > $(PREFS_FILE)'

# Configures the app to authenticate against the local PostgreSQL database.
# Restart the app after running for the change to take effect.
use-local-api:
	adb shell run-as $(PKG) sh -c 'echo "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?><map><boolean name=\"UseSharedApi\" value=\"false\" /></map>" > $(PREFS_FILE)'