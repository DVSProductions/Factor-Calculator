@echo off
setlocal

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

vpk pack --packTitle "Factor Calculator" --packId "DVSProductions.Factor-Calculator" --packAuthors "DVSProductions" --mainExe FactorCalculator.exe --noPortable --packVersion 1.0.0 --packDir bin\Release\net9.0

vpk upload github --repoUrl "https://github.com/DVSProductions/Factor-Calculator" --publish --token %token%

endlocal