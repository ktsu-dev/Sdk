@echo off
:: Wrapper script to run tests with coverage
:: Usage: run-tests-with-coverage.cmd [Configuration] [CoverageOutputPath] [CoverageFormat] [TestProject]

powershell -ExecutionPolicy Bypass -File "%~dp0run-tests-with-coverage.ps1" %*
