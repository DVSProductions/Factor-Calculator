@echo off
setlocal

rem Prompt the user for the version
set /p version=Enter version (e.g., 1.0.3): 
if not defined version (
    echo Version is required.
    exit /b 1
)

rem Read token from GithubToken.txt
if not exist "GithubToken.txt" (
    echo GithubToken.txt not found.
    exit /b 1
)

for /f "usebackq delims=" %%A in ("GithubToken.txt") do (
    set "token=%%A"
    goto :token_read
)

:token_read
if not defined token (
    echo Token not found in GithubToken.txt.
    exit /b 1
)
echo %token%
pause

dotnet publish -c Release --no-self-contained --arch x64 -f net9.0-windows10.0.17763.0 --output "bin\Velopack" --nologo --property:DebugSymbols=false --property:WarningLevel=0 --property:AnalysisLevel=0 --property:debug=none --property:PublishReadyToRun=false

vpk pack --packVersion %version% --packTitle "Factor Calculator" --packId "DVSProductions.Factor-Calculator" --packAuthors "DVSProductions" --mainExe FactorCalculator.exe --noPortable --packDir "bin\Velopack" --icon "Assets\Icon.ico"

vpk upload github --repoUrl "https://github.com/DVSProductions/Factor-Calculator" --publish --token %token%

endlocal