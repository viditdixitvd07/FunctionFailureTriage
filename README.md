# FunctionFailureTriage

**A lightweight C# PoC for Azure Function log analysis**

## Overview

FunctionFailureTriage is a rule-based diagnostic utility that scans application log text for common failure patterns and generates a structured JSON report.

## Features

* Detects common authentication, configuration, dependency timeout, database, messaging, memory, and throttling errors.
* Assigns a preliminary severity to each finding.
* Provides suggested diagnostic actions.
* Produces machine-readable JSON output.
* Runs locally without external services or API credentials.

## Technology

* C#
* .NET 8
* System.Text.Json
* Regular expressions

## How to run

1. Create a .NET console application.
2. Replace Program.cs with the supplied implementation.
3. Run `dotnet run`.
4. Paste sample log lines and enter `END` to generate a report.

Alternatively, provide a text file path as the command-line argument.

## Scope and limitations

This is a demonstration PoC, not a production monitoring solution. It uses predefined patterns, can miss unrecognized errors, and may classify unrelated messages incorrectly. Review its recommendations before taking action. Avoid feeding it logs containing credentials, tokens, personal data, or confidential information.

## Future enhancements

* Add automated unit tests.
* Support configurable rules.
* Integrate with approved Application Insights exports.
* Add confidence scores and duplicate-event grouping.

