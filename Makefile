PKG := com.companyname.rentalapp

build-debug:
	dotnet build -c Debug /workspace/RentalApp.sln

uninstall:
	adb uninstall $(PKG)

install:
	adb install -r /workspace/RentalApp/bin/Debug/net10.0-android/$(PKG)-Signed.apk
