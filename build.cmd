REM other targets are:
REM 'build'
REM 'test'
REM 'test-integration'

@ECHO OFF
cls
tools\nant\bin\nant.exe daily -f:Spring.Amqp.build
