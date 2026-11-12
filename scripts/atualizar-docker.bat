@echo off
REM Recompila a imagem da API (backend + frontend) e recria os containers com o codigo atual.
cd /d "%~dp0.."
echo === Atualizando containers do Consultorio (pode levar alguns minutos) ===
docker compose up -d --build
if errorlevel 1 (
  echo.
  echo FALHOU. Verifique se o Docker Desktop esta aberto e rodando.
  pause
  exit /b 1
)
echo.
docker compose ps
echo.
echo Pronto: sistema em http://localhost:8080  ^|  Portal do paciente em http://localhost:8080/portal
pause
