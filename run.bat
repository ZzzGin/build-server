cd /d %~dp0
cd TestHarness\bin\Debug
start TestHarness.exe
cd..
cd..
cd..
cd BuildServer\bin\Debug
start BuildServer.exe localhost:5270 3
cd..
cd..
cd..
cd P4\bin\Debug
start P4.exe
cd..
cd..
cd..
cd RepoMock\bin\Debug
start RepoMock.exe
pause