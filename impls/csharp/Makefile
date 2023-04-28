PROJECT_FILE = csharp.csproj
TARGET_FRAMEWORK = net7.0
CONFIGURATION = Debug

FORCE:

%: %.cs FORCE
	dotnet build $(PROJECT_FILE) -c $(CONFIGURATION) -f $(TARGET_FRAMEWORK) -o ./$* /p:StartupObject=Mal.$*